#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    public readonly struct Vec2
    {
        public double X { get; }
        public double Y { get; }
        public Vec2(double x, double y) { X = x; Y = y; }
    }
    public sealed class ViewPlane
    {
        public Vec3 Origin { get; }
        public Vec3 U { get; }
        public Vec3 V { get; }
        public Vec3 Normal { get; }
        public double HalfDepth { get; }
        private ViewPlane(Vec3 origin, Vec3 u, Vec3 v, double halfDepth)
        {
            if (!origin.IsFinite || !Number.Finite(halfDepth) || halfDepth < .000001) throw new ArgumentException("Dải xem phải lớn hơn 0.");
            Origin = origin; U = u; V = v; Normal = u.Cross(v); HalfDepth = halfDepth;
        }
        public static ViewPlane Plan(double z, double halfDepth) => new ViewPlane(new Vec3(0, 0, z), new Vec3(1, 0, 0), new Vec3(0, 1, 0), halfDepth);
        public static ViewPlane Elevation(double x, double y, double angleDegrees, double halfDepth, bool reverse = false)
        {
            if (!Number.Finite(angleDegrees)) throw new ArgumentException("Góc không hợp lệ.");
            double a = angleDegrees * Math.PI / 180, sign = reverse ? -1 : 1;
            return new ViewPlane(new Vec3(x, y, 0), new Vec3(sign * Math.Cos(a), sign * Math.Sin(a), 0), new Vec3(0, 0, 1), halfDepth);
        }
        public static ViewPlane ElevationThrough(Vec2 first, Vec2 second, double halfDepth)
        {
            double dx = second.X - first.X, dy = second.Y - first.Y;
            if (!Number.Finite(dx) || !Number.Finite(dy) || dx * dx + dy * dy < 1e-12) throw new ArgumentException("Hai điểm mặt đứng phải khác nhau và hữu hạn.");
            return Elevation(first.X, first.Y, Math.Atan2(dy, dx) * 180 / Math.PI, halfDepth);
        }
        public Vec2 Project(Vec3 p) { var d = p - Origin; return new Vec2(d.Dot(U), d.Dot(V)); }
        public double Depth(Vec3 p) => (p - Origin).Dot(Normal);
        public Vec3 Unproject(Vec2 p) => Origin + U * p.X + V * p.Y;
    }
    public readonly struct Segment2
    {
        public Vec2 A { get; }
        public Vec2 B { get; }
        public Segment2(Vec2 a, Vec2 b) { A = a; B = b; }
    }
    public sealed class ProjectedElement
    {
        public ModelElement Source { get; }
        public IReadOnlyList<Segment2> Segments { get; }
        public IReadOnlyList<Vec2> Polygon { get; }
        public bool IsCut { get; }
        public IReadOnlyList<Vec2[]> Faces { get; }
        public IReadOnlyList<Vec2[]> Voids { get; }
        public IReadOnlyList<Segment2> AxisSegments { get; }
        public ProjectedElement(ModelElement source, List<Segment2> segments, Vec2[] polygon, bool isCut, Vec2[][]? faces = null, IReadOnlyList<Segment2>? axisSegments = null, Vec2[][]? voids = null)
        { Source = source; Segments = segments.AsReadOnly(); Polygon = Array.AsReadOnly(polygon); IsCut = isCut; Faces = faces ?? Array.Empty<Vec2[]>(); Voids = voids ?? Array.Empty<Vec2[]>(); AxisSegments = axisSegments ?? Segments; }
    }
    public static class Projection
    {
        public static ProjectedElement? Build(ModelElement element, ViewPlane plane)
        {
            var pts = element.Vertices;
            var segments = new List<Segment2>();
            int edges = pts.Count == 2 ? 1 : pts.Count;
            for (int i = 0; i < edges; i++)
            {
                var a = pts[i]; var b = pts[(i + 1) % pts.Count];
                if (Clip(ref a, ref b, plane)) segments.Add(new Segment2(plane.Project(a), plane.Project(b)));
            }
            bool inside = pts.All(p => Math.Abs(plane.Depth(p)) <= plane.HalfDepth);
            // Shells crossing the plane: display exact plane/edge intersections, paired in order.
            // Coplanar shells keep the original polygon; no convex hull that could fill a concavity.
            if (pts.Count > 2 && !inside)
            {
                var cuts = new List<Vec2>();
                for (int i = 0; i < pts.Count; i++) {
                    var a = pts[i]; var b = pts[(i + 1) % pts.Count];
                    double da = plane.Depth(a), db = plane.Depth(b);
                    if ((da <= 0 && db > 0) || (db <= 0 && da > 0)) cuts.Add(plane.Project(a + (b - a) * (da / (da - db))));
                }
                if (cuts.Count >= 2) {
                    double dx = cuts.Max(p => p.X) - cuts.Min(p => p.X), dy = cuts.Max(p => p.Y) - cuts.Min(p => p.Y);
                    var sorted = cuts.OrderBy(p => dx >= dy ? p.X : p.Y).ToArray();
                    for (int i = 0; i + 1 < sorted.Length; i += 2) segments.Add(new Segment2(sorted[i], sorted[i + 1]));
                }
            }
            if (segments.Count == 0) return null;
            return new ProjectedElement(element, segments, inside && pts.Count > 2 ? pts.Select(plane.Project).ToArray() : Array.Empty<Vec2>(), !inside);
        }
        internal static bool Clip(ref Vec3 a, ref Vec3 b, ViewPlane plane)
        {
            double da = plane.Depth(a), db = plane.Depth(b), delta = db - da, lo = 0, hi = 1;
            if (Math.Abs(delta) < 1e-12) return Math.Abs(da) <= plane.HalfDepth;
            double t1 = (-plane.HalfDepth - da) / delta, t2 = (plane.HalfDepth - da) / delta;
            lo = Math.Max(lo, Math.Min(t1, t2)); hi = Math.Min(hi, Math.Max(t1, t2));
            if (lo > hi) return false;
            var start = a; var direction = b - a; a = start + direction * lo; b = start + direction * hi; return true;
        }
        public static double Distance(Vec2 p, Segment2 s)
        {
            double dx = s.B.X - s.A.X, dy = s.B.Y - s.A.Y, length2 = dx * dx + dy * dy;
            double t = length2 < 1e-20 ? 0 : Math.Max(0, Math.Min(1, ((p.X - s.A.X) * dx + (p.Y - s.A.Y) * dy) / length2));
            dx = p.X - (s.A.X + t * dx); dy = p.Y - (s.A.Y + t * dy); return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
