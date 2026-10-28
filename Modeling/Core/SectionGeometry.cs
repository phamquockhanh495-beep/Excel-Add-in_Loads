#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    // Dimensions in metres, from the API, never inferred from section names.
    public sealed class SectionGeometry
    {
        public string Shape { get; }
        public double Width { get; }
        public double Depth { get; }
        public SectionGeometry(string shape, double width, double depth)
        {
            if ((shape != "Rectangle" && shape != "Circle" && shape != "Shell") ||
                !Number.Finite(width) || !Number.Finite(depth) || width <= 0 || depth <= 0)
                throw new ArgumentException("Kích thước tiết diện không hợp lệ.");
            Shape = shape; Width = width; Depth = depth;
        }
        public override string ToString() => Shape == "Shell" ? $"t = {Depth:0.###} m" : Shape == "Circle" ? $"D = {Depth:0.###} m" : $"b × h = {Width:0.###} × {Depth:0.###} m";
    }

    public static class SectionExtrusion
    {
        // Solids are centred on the analysis axis/reference surface. Physical insertion/end offsets are not applied.
        public static ProjectedElement? Build(ModelElement element, ViewPlane plane)
        {
            var original = Projection.Build(element, plane);
            var section = element.Section;
            if (section == null || element.AdvancedLocalAxes) return original;
            if (element.Vertices.Count == 2 && section.Shape != "Shell") {
                if (!element.LocalAxisDegrees.HasValue) return original;
                var axis = element.Vertices[1] - element.Vertices[0];
                if (axis.Length < 1e-9) return original;
                axis = axis * (1 / axis.Length);
                var z = new Vec3(0, 0, 1);
                var two = z - axis * axis.Dot(z);
                if (two.Length < 1e-8) two = new Vec3(1, 0, 0);
                two = two * (1 / two.Length);
                var three = axis.Cross(two);
                double angle = element.LocalAxisDegrees.Value * Math.PI / 180;
                var rotatedTwo = two * Math.Cos(angle) + three * Math.Sin(angle);
                var rotatedThree = axis.Cross(rotatedTwo);
                var offsets = section.Shape == "Circle"
                    ? Enumerable.Range(0, 48).Select(i => rotatedTwo * (section.Depth / 2 * Math.Cos(i * Math.PI / 24)) + rotatedThree * (section.Depth / 2 * Math.Sin(i * Math.PI / 24))).ToArray()
                    : new[] { rotatedTwo * (-section.Depth / 2) + rotatedThree * (-section.Width / 2), rotatedTwo * (section.Depth / 2) + rotatedThree * (-section.Width / 2), rotatedTwo * (section.Depth / 2) + rotatedThree * (section.Width / 2), rotatedTwo * (-section.Depth / 2) + rotatedThree * (section.Width / 2) };
                var pts = new List<Vec2>();
                void Edge(Vec3 a, Vec3 b) { if (Projection.Clip(ref a, ref b, plane)) { pts.Add(plane.Project(a)); pts.Add(plane.Project(b)); } }
                for (int i = 0; i < offsets.Length; i++) {
                    int next = (i + 1) % offsets.Length;
                    Edge(element.Vertices[0] + offsets[i], element.Vertices[1] + offsets[i]);
                    foreach (var end in element.Vertices) Edge(end + offsets[i], end + offsets[next]);
                }
                var hull = Hull(pts);
                return hull.Length < 3 ? original : Solid(element, hull, original);
            }
            if (section.Shape == "Shell" && element.Kind != ElementKind.Opening && original != null) {
                // Face-on shells retain their original concave outline. Edge-on shells reveal thickness.
                var vertices = element.Vertices;
                var normal = new Vec3(0, 0, 0);
                for (int i = 1; i + 1 < vertices.Count && normal.Length < 1e-9; i++) normal = (vertices[i] - vertices[0]).Cross(vertices[i + 1] - vertices[0]);
                if (normal.Length < 1e-9) return original;
                normal = normal * (1 / normal.Length);
                double facing = Math.Abs(normal.Dot(plane.Normal));
                if (facing > .999999) return original;
                if (facing > .000001) return original; // Inclined shell: keep analytical outline, not a misleading convex fill.
                var delta = new Vec2(normal.Dot(plane.U) * section.Depth / 2, normal.Dot(plane.V) * section.Depth / 2);
                var faces = original.Segments.Where(s => Length(s) > 1e-9).Select(s => new[] {
                    new Vec2(s.A.X-delta.X,s.A.Y-delta.Y), new Vec2(s.B.X-delta.X,s.B.Y-delta.Y),
                    new Vec2(s.B.X+delta.X,s.B.Y+delta.Y), new Vec2(s.A.X+delta.X,s.A.Y+delta.Y) }).ToArray();
                if (faces.Length == 0) return original;
                return new ProjectedElement(element, faces.SelectMany(Edges).ToList(), Array.Empty<Vec2>(), original.IsCut, faces, original.Segments);
            }
            return original;
        }
        public static ProjectedElement WithOpenings(ProjectedElement item, IReadOnlyList<ProjectedElement> openings)
        {
            if (item.Source.Kind == ElementKind.Opening || item.Source.Vertices.Count < 3 || item.Polygon.Count < 3) return item;
            var vertices = item.Source.Vertices;
            var normal = new Vec3(0,0,0);
            for (int i=1; i+1<vertices.Count && normal.Length<1e-9; i++) normal=(vertices[i]-vertices[0]).Cross(vertices[i+1]-vertices[0]);
            if (normal.Length<1e-9) return item;
            normal=normal*(1/normal.Length);
            var bounds=new Box2(new Vec2(item.Polygon.Min(p=>p.X),item.Polygon.Min(p=>p.Y)),new Vec2(item.Polygon.Max(p=>p.X),item.Polygon.Max(p=>p.Y)));
            var holes=openings.Where(o=>o.Polygon.Count>2 && o.Source.Vertices.All(p=>Math.Abs((p-vertices[0]).Dot(normal))<1e-6) &&
                bounds.Intersects(new Box2(new Vec2(o.Polygon.Min(p=>p.X),o.Polygon.Min(p=>p.Y)),new Vec2(o.Polygon.Max(p=>p.X),o.Polygon.Max(p=>p.Y)))))
                .Select(o=>o.Polygon.ToArray()).ToArray();
            if(holes.Length==0)return item;
            return new ProjectedElement(item.Source,item.Segments.ToList(),item.Polygon.ToArray(),item.IsCut,item.Faces.ToArray(),item.AxisSegments,holes);
        }
        private static double Length(Segment2 s) => Math.Sqrt(Math.Pow(s.B.X-s.A.X,2)+Math.Pow(s.B.Y-s.A.Y,2));
        private static IEnumerable<Segment2> Edges(Vec2[] pts) => pts.Select((p,i) => new Segment2(p,pts[(i+1)%pts.Length]));
        private static ProjectedElement Solid(ModelElement e, Vec2[] pts, ProjectedElement? original) =>
            new ProjectedElement(e, Edges(pts).ToList(), pts, original?.IsCut ?? true, new[] { pts }, original?.Segments ?? Array.Empty<Segment2>());
        private static Vec2[] Hull(IEnumerable<Vec2> input)
        {
            var pts = input.GroupBy(p => (p.X,p.Y)).Select(g => g.First()).OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            if (pts.Length < 3) return pts;
            double Cross(Vec2 a, Vec2 b, Vec2 c) => (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
            var hull = new List<Vec2>();
            foreach(var p in pts) { while(hull.Count>=2 && Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=1e-12) hull.RemoveAt(hull.Count-1);hull.Add(p); }
            int lower=hull.Count;
            for(int i=pts.Length-2;i>=0;i--) { var p=pts[i];while(hull.Count>lower && Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=1e-12) hull.RemoveAt(hull.Count-1);hull.Add(p); }
            hull.RemoveAt(hull.Count-1);return hull.ToArray();
        }
    }
}
