using System;
using System.Linq;
using System.Threading;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Modeling.Printing;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static ModelSnapshot GridFixture()
    {
        var m=PdfFixture();var grids=new[]{new ModelGridLine("G1","A",new[]{new Vec2(0,0),new Vec2(0,1)}),new ModelGridLine("G1","B",new[]{new Vec2(12,0),new Vec2(12,1)}),new ModelGridLine("G1","1",new[]{new Vec2(0,0),new Vec2(1,0)}),new ModelGridLine("G1","2",new[]{new Vec2(0,8),new Vec2(1,8)})};
        var col=new ModelElement("Frame:SPAN","SPAN","C","Tầng 2","C400",ElementKind.Column,new[]{new Vec3(2,0,0),new Vec3(2,0,9)},0,section:new SectionGeometry("Rectangle",.4,.4));
        var load=new ExistingLoad(col.Id,"WX","Frame force/length","Global",4,0,1,3,9,"kN/m");
        return new ModelSnapshot(1,m.Source,m.Version,m.Identity,m.SourceUnits,m.CapturedUtc,m.Elements.Concat(new[]{col}),new[]{new ModelLevel("Base",0)}.Concat(m.Levels),m.Loads.Concat(new[]{load}),m.Diagnostics,m.LoadPatterns,grids);
    }
    private static void GridStoryTests()
    {
        Check("grids: local rotated coordinates convert to world metres",()=>{var p=GridGeometry.Transform(new Vec2(1000,2000),10000,20000,90,.001);Near(p.X,8);Near(p.Y,21);});
        Check("grids: snapshot roundtrip preserves labels, paths and story limits; legacy defaults empty",()=>{
            var m=GridFixture();var copy=JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Serialize(m));Assert(copy.GridLines.Count==4&&copy.GridLines[1].Label=="B");Assert(PdfFixture().GridLines.Count==0);
        });
        Check("grids: hidden and out-of-story lines excluded; finite skew line clipped",()=>{
            var box=new Box2(new Vec2(0,0),new Vec2(10,10));var grids=new[]{new ModelGridLine("G","A",new[]{new Vec2(-2,-2),new Vec2(20,20)},false,minZ:3,maxZ:6),new ModelGridLine("G","H",new[]{new Vec2(0,0),new Vec2(1,0)},visible:false)};
            Assert(GridGeometry.Plan(grids,box,0).Count==0);var g=GridGeometry.Plan(grids,box,3).Single();Near(g.Lines[0].A.X,0);Near(g.Lines[0].B.X,10);Assert(g.Bubbles.Length==2);
        });
        Check("grids: elevation intersections respect finite cut and reverse direction",()=>{
            var grids=GridFixture().GridLines;var p=ViewPlane.ElevationThrough(new Vec2(0,0),new Vec2(5,0),.15,true);var draws=GridGeometry.Elevation(grids,p,3,6);Assert(draws.Count==1&&draws[0].Label=="A");Near(draws[0].Lines[0].A.Y,3);Near(draws[0].Lines[0].B.Y,6);
        });
        Check("grids: circle produces two cut intersections and only one coincident bubble",()=>{
            var points=Enumerable.Range(0,361).Select(i=>new Vec2(5*Math.Cos(i*Math.PI/180),5*Math.Sin(i*Math.PI/180))).ToArray();var g=new ModelGridLine("C","R5",points,false,bubbleStart:false,bubbleEnd:true);
            var plan=GridGeometry.Plan(new[]{g},new Box2(new Vec2(-6,-6),new Vec2(6,6)),0);Assert(plan.Single().Bubbles.Length==1);
            var elevation=GridGeometry.Elevation(new[]{g},ViewPlane.Elevation(0,0,0,.15),0,3);Assert(elevation.Count==2);
        });
        Check("story isolation: clips spanning columns, omits lower floor and keeps upper slab thickness",()=>{
            var vm=new VisualWorkspaceViewModel();var model=GridFixture();vm.SetSnapshot(model,true);vm.Level=model.Levels.Last();vm.SetElevationThrough(new Vec2(0,0),new Vec2(12,0));vm.IsolateStory=true;
            Assert(vm.Scene.Count>0&&vm.Scene.All(e=>e.Segments.All(s=>s.A.Y>=3-1e-7&&s.B.Y<=6+1e-7&&s.B.Y>=3-1e-7&&s.A.Y<=6+1e-7)));
            Assert(!vm.Scene.Any(e=>e.Source.Id=="Area:1")&&vm.Scene.Any(e=>e.Source.Id=="Area:2"));var col=vm.Scene.Single(e=>e.Source.Id=="Frame:SPAN");Near(col.Segments.Min(s=>Math.Min(s.A.Y,s.B.Y)),3);Near(col.Segments.Max(s=>Math.Max(s.A.Y,s.B.Y)),6);
            var slab=vm.Scene.Single(e=>e.Source.Id=="Area:2");Near(slab.Segments.Min(s=>Math.Min(s.A.Y,s.B.Y)),6-.175);
            Assert(vm.DisplayLevels.Count==2&&vm.DisplayLevels.Min(l=>l.Elevation)==3);
        });
        Check("story isolation: load segments clipped and original variable values preserved",()=>{
            var vm=new VisualWorkspaceViewModel();var model=GridFixture();string before=JsonCodec.Serialize(model);vm.SetSnapshot(model,true);vm.SetElevationThrough(new Vec2(0,0),new Vec2(12,0));vm.IsolateStory=true;vm.ShowFrameLoads=true;
            var row=vm.ReadLoadItems.Single(r=>r.ElementId=="Frame:SPAN");Near(row.Geometry.Segments[0].A.Y,3);Near(row.Geometry.Segments[0].B.Y,6);Near(LoadGlyphs.ValueAt(row,new Vec2(2,3)),5);Assert(JsonCodec.Serialize(model)==before);
            vm.IsolateStory=false;Assert(vm.Scene.Any(e=>e.Source.Id=="Area:1"));
        });
        Check("story isolation: elevation Page Down moves band, Plan remains unaffected, preferences survive",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(GridFixture(),true);vm.SetElevationThrough(new Vec2(0,0),new Vec2(12,0));vm.IsolateStory=true;Assert(vm.MovePlanLevel(-1));Near(vm.Level.Elevation,3);Assert(vm.Scene.Any(e=>e.Source.Id=="Area:1")&&!vm.Scene.Any(e=>e.Source.Id=="Area:2"));vm.Elevation=false;Assert(vm.Scene.Any(e=>e.Source.Id=="Area:1"));
            vm.ShowGrids=false;var saved=vm.CapturePreferences("test.edb");var restored=new VisualWorkspaceViewModel();restored.ApplyPreferences(saved);Assert(restored.IsolateStory&&!restored.ShowGrids);
        });
        Check("story isolation: reversed finite section clips wall faces and openings in both axes",()=>{
            var wall=new ModelElement("W","W","W","T2","W300",ElementKind.Wall,new[]{new Vec3(0,0,0),new Vec3(8,0,0),new Vec3(8,0,9),new Vec3(0,0,9)},section:new SectionGeometry("Shell",.3,.3));
            var plane=ViewPlane.ElevationThrough(new Vec2(1,0),new Vec2(5,0),.15,true).WithVerticalRange(3,6);
            var projected=SectionExtrusion.Build(wall,plane);Assert(projected!=null);Assert(projected.Segments.All(s=>new[]{s.A,s.B}.All(v=>v.X>=-4-1e-6&&v.X<=1e-6&&v.Y>=3-1e-6&&v.Y<=6+1e-6)));
            var opening=new ModelElement("H","H","","T2","",ElementKind.Opening,new[]{new Vec3(2,0,4),new Vec3(3,0,4),new Vec3(3,0,5),new Vec3(2,0,5)});
            var withHole=SectionExtrusion.WithOpenings(projected,new[]{Projection.Build(opening,plane)});Assert(withHole.Voids.Count>0);
        });
        Check("PDF: grid labels and dashed lines export with independent print toggle",()=>{
            var model=GridFixture();var p=new PrintProfile{Grids=true,Combined=true,Project="DỮ LIỆU MINH HỌA · LƯỚI TRỤC"};var doc=PlanPrintBuilder.Build(PdfRequest(p,new[]{"Tầng 2"},model:model),CancellationToken.None);
            Assert(doc.Pages[0].Marks.Any(m=>m.Kind==PrintMarkKind.Text&&m.Text=="A")&&doc.Pages[0].Marks.Any(m=>m.Kind==PrintMarkKind.Line&&m.Dashed&&m.Color=="#8494A7"));
            PlanPdfWriter.Write(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"QKS-Grid-Plan-DEMO.pdf"),doc.Pages,CancellationToken.None);
            p.Grids=false;var plain=PlanPrintBuilder.Build(PdfRequest(p,new[]{"Tầng 2"},model:model),CancellationToken.None);Assert(!plain.Pages[0].Marks.Any(m=>m.Kind==PrintMarkKind.Line&&m.Color=="#8494A7"));
        });
    }
}
