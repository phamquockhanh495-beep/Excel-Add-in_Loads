using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static void Near(double a, double b) { if (Math.Abs(a - b) > 1e-8) throw new Exception($"Expected {b}, got {a}"); }
    private static ModelElement Frame(Vec3 a, Vec3 b, string id = "Frame:1") => new ModelElement(id, "1", "B1", "L1", "B200x400", ElementKind.Beam, new[] { a, b });
    private static ModelSnapshot Snapshot(params ModelElement[] elements) => new ModelSnapshot(1, "ETABS", "test", "42|test.edb", "kN/m/C", DateTime.UtcNow, elements, new[] { new ModelLevel("L1", 3) }, Array.Empty<ExistingLoad>(), new[] { "test fixture" });
    private static void ViewerTests()
    {
        Check("viewer: SI line and area dimensions", () => { var s = new UnitScale("N", "mm"); Near(s.LineForce, 1); Near(s.Pressure, 1000); Near(s.LengthToMetres, .001); });
        Check("viewer: distributed force and moment have distinct dimensions", () => { var s = new UnitScale("N", "mm"); Near(s.DistributedFactor(1), 1); Near(s.DistributedFactor(2), .001); Throws<ArgumentOutOfRangeException>(() => s.DistributedFactor(3)); });
        Check("viewer: imperial force and length conversion", () => { var s = new UnitScale("kip", "ft"); Near(s.LengthToMetres * 10, 3.048); Near(s.ForceToKn, 4.4482216152605); Near(s.LineForce * .3048, s.ForceToKn); });
        Check("viewer: metric gravitational units", () => { Near(new UnitScale("tonf", "cm").Pressure, 98066.5); Near(new UnitScale("kgf", "m").ForceToKn, .00980665); });
        Check("viewer: unknown units fail closed", () => Throws<ArgumentException>(() => new UnitScale("NotApplicable", "m")));
        Check("viewer: Plan uses global XY", () => { var p = ViewPlane.Plan(3, .1).Project(new Vec3(4, 5, 3)); Near(p.X, 4); Near(p.Y, 5); });
        Check("viewer: Plan rejects other level", () => Assert(Projection.Build(Frame(new Vec3(0, 0, 6), new Vec3(5, 0, 6)), ViewPlane.Plan(3, .1)) == null));
        Check("viewer: crossing column becomes plan cut", () => { var item = Projection.Build(Frame(new Vec3(2, 2, 0), new Vec3(2, 2, 6)), ViewPlane.Plan(3, .1))!; Assert(item.IsCut && item.Segments.Count == 1); Near(item.Segments[0].A.X, 2); Near(item.Segments[0].B.Y, 2); });
        Check("viewer: Elevation projects X and global Z", () => { var p = ViewPlane.Elevation(10, 20, 0, .1).Project(new Vec3(14, 20, 6)); Near(p.X, 4); Near(p.Y, 6); });
        Check("viewer: reversed Elevation preserves vertical", () => { var p = ViewPlane.Elevation(0, 0, 0, .1, true); var q = p.Project(new Vec3(5, 0, 3)); Near(q.X, -5); Near(q.Y, 3); Near(p.Normal.Y, 1); });
        Check("viewer: rotated plane world round trip", () => { var p = ViewPlane.Elevation(2, 3, 37, .15); var q = p.Project(p.Unproject(new Vec2(7, 12))); Near(q.X, 7); Near(q.Y, 12); });
        Check("viewer: slab across Elevation produces cut", () => { var e = new ModelElement("Area:1", "1", "S1", "L1", "S150", ElementKind.Slab, new[] { new Vec3(0, -2, 3), new Vec3(5, -2, 3), new Vec3(5, 2, 3), new Vec3(0, 2, 3) }); var p = Projection.Build(e, ViewPlane.Elevation(0, 0, 0, .1))!; Assert(p.IsCut && p.Polygon.Count == 0); Assert(p.Segments.Any(s => Math.Abs(s.A.X - s.B.X) > 4.9)); });
        Check("viewer: concave shell cut keeps gap", () => {
            var v = new[] { new Vec3(-3,-2,0), new Vec3(3,-2,0), new Vec3(3,2,0), new Vec3(1,2,0), new Vec3(1,-1,0), new Vec3(-1,-1,0), new Vec3(-1,2,0), new Vec3(-3,2,0) };
            var e = new ModelElement("Area:2", "2", "", "", "", ElementKind.Slab, v);
            var p = Projection.Build(e, ViewPlane.Elevation(0,0,0,.01))!;
            var lines = p.Segments.Where(s => Math.Abs(s.B.X - s.A.X) > .01).ToArray();
            Assert(lines.Length == 2); Assert(lines.All(s => Math.Abs(s.B.X - s.A.X) <= 2.000001));
        });
        Check("viewer: frame clipped by band", () => { var e = Frame(new Vec3(0, -2, 0), new Vec3(4, 2, 0)); var p = Projection.Build(e, ViewPlane.Elevation(0, 0, 0, .5))!; Near(p.Segments[0].A.X, 1.5); Near(p.Segments[0].B.X, 2.5); });
        Check("viewer: true frame length survives clipping", () => { var e = Frame(new Vec3(0, 0, 0), new Vec3(3, 0, 4)); Projection.Build(e, ViewPlane.Plan(2, .1)); Near(e.Length3D!.Value, 5); });
        Check("viewer: hit testing bounded segment", () => { var s = new Segment2(new Vec2(0, 0), new Vec2(2, 0)); Near(Projection.Distance(new Vec2(3, 0), s), 1); Near(Projection.Distance(new Vec2(1, 2), s), 2); });
        Check("viewer: hit testing point cut", () => Near(Projection.Distance(new Vec2(3, 4), new Segment2(new Vec2(0, 0), new Vec2(0, 0))), 5));
        Check("viewer: no duplicate source identities", () => { var e = Frame(new Vec3(0, 0, 0), new Vec3(1, 0, 0)); Throws<ArgumentException>(() => Snapshot(e, e)); });
        Check("viewer: invalid geometry rejected", () => Throws<ArgumentException>(() => Snapshot(Frame(new Vec3(double.NaN, 0, 0), new Vec3(1, 0, 0)))));
        Check("viewer: invalid plane rejected", () => { Throws<ArgumentException>(() => ViewPlane.Plan(0, 0)); Throws<ArgumentException>(() => ViewPlane.Elevation(0, 0, double.NaN, .1)); });
        Check("viewer: snapshot owns vertex copy", () => { var pts = new[] { new Vec3(1, 2, 3), new Vec3(4, 5, 6) }; var e = new ModelElement("Frame:1", "1", "", "", "", ElementKind.Beam, pts); pts[0] = new Vec3(0, 0, 0); Near(e.Vertices[0].X, 1); });
        Check("viewer: snapshot JSON retains geometry and identity", () => { var original = Snapshot(Frame(new Vec3(1, 2, 3), new Vec3(4, 6, 3))); var copy = JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Serialize(original)); Assert(copy.Identity == original.Identity && copy.Levels[0].Name == "L1"); Near(copy.Elements[0].Vertices[0].Z, 3); Near(copy.Elements[0].Length3D!.Value, 5); });
        Check("viewer: signed zero and moment dimension round trip", () => {
            var e = Frame(new Vec3(0,0,0), new Vec3(2,0,0));
            var s = new ModelSnapshot(1,"ETABS","test","id","N/mm",DateTime.UtcNow,new[]{e},Array.Empty<ModelLevel>(),new[]{new ExistingLoad(e.Id,"WX","Frame moment/length","Global",4,.25,.75,0,-2,"kN·m/m")},Array.Empty<string>());
            var copy = JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Serialize(s)); Assert(copy.Loads[0].Value1 == 0 && copy.Loads[0].Value2 == -2 && copy.Loads[0].Unit == "kN·m/m");
        });
        Check("viewer: future schema rejected", () => Throws<ArgumentException>(() => new ModelSnapshot(2,"ETABS","","id","",DateTime.UtcNow,Array.Empty<ModelElement>(),Array.Empty<ModelLevel>(),Array.Empty<ExistingLoad>(),Array.Empty<string>())));
        Check("viewer: orphan load rejected", () => Throws<ArgumentException>(() => new ModelSnapshot(1,"ETABS","","id","",DateTime.UtcNow,Array.Empty<ModelElement>(),Array.Empty<ModelLevel>(),new[]{new ExistingLoad("missing","D","Area uniform","Global",10,0,1,0,0,"kN/m²")},Array.Empty<string>())));
        Check("viewer: property colors independent of read order", () => { string before = PropertyColors.ForProperty("Beam|B200x400"); foreach(var p in new[]{"Slab|S150","Column|C400","Wall|W200"}) PropertyColors.ForProperty(p); Assert(before == PropertyColors.ForProperty("Beam|B200x400")); Assert(before == "#15803D"); });
        Check("viewer: selection identity survives view switch", () => {
            var e = Frame(new Vec3(0,0,3), new Vec3(4,0,3)); var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(Snapshot(e),false); vm.SelectedElement = e;
            vm.Elevation = true; Assert(vm.SelectedId == e.Id && vm.Scene.Single().Source.Id == e.Id);
            vm.Reverse = true; Assert(vm.SelectedId == e.Id); vm.Elevation = false; Assert(vm.Scene.Single().Source.Id == e.Id);
        });
        Check("viewer: search and kind filters", () => { var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(Snapshot(Frame(new Vec3(0,0,3),new Vec3(4,0,3))),false); vm.Search = "b200"; Assert(vm.Scene.Count == 1); vm.Kind = "Column"; Assert(vm.Scene.Count == 0); vm.Kind = "Tất cả"; vm.Search = "missing"; Assert(vm.Scene.Count == 0); });
        Check("viewer: invalid view input reports error", () => { var vm = new VisualWorkspaceViewModel(); vm.HalfDepth = "NaN"; vm.Refresh(); Assert(vm.Status.StartsWith("Không cập nhật hình:")); });
        Check("viewer: explicit offline provenance", () => { var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(Snapshot(Frame(new Vec3(0,0,3),new Vec3(4,0,3))),true); Assert(vm.SourceText.StartsWith("OFFLINE ·")); });
        Check("viewer: source invalidation clears selection and geometry", () => { var e = Frame(new Vec3(0,0,3),new Vec3(4,0,3)); var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(Snapshot(e),false); vm.SelectedElement = e; vm.SetSnapshot(null,false); Assert(vm.Scene.Count == 0 && vm.SelectedId == "" && !vm.VisibleLoads.Any()); });
        Check("viewer: fixture contains beams columns slabs and signed loads", () => {
            var s = JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Read(System.IO.Path.Combine(ViewerTestRoot, "tests/fixtures/viewer-readonly.json")));
            Assert(s.Elements.Any(e=>e.Kind==ElementKind.Column) && s.Elements.Any(e=>e.Kind==ElementKind.Slab));
            Assert(s.Loads.Any(l=>l.Value1<0)); var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(s,true); Assert(vm.Scene.Count>0); vm.Elevation=true; Assert(vm.Scene.Count>0);
        });
        Check("viewer: opening context survives slab filter", () => {
            var pts = new[]{new Vec3(0,0,3),new Vec3(1,0,3),new Vec3(1,1,3),new Vec3(0,1,3)};
            var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(Snapshot(new ModelElement("Area:hole","hole","","L1","",ElementKind.Opening,pts),new ModelElement("Area:slab","slab","","L1","",ElementKind.Slab,pts)),true);
            vm.Kind = "Slab"; Assert(vm.Scene.Count==1 && vm.HasOpenings);
        });
    }
    private static string ViewerTestRoot = "";
}
