using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static void SectionLoadTests()
    {
        ModelElement Beam(double a,double b,double depth=.6)=>new ModelElement("Frame:B","B","B","L1","B",ElementKind.Beam,new[]{new Vec3(a,0,3),new Vec3(b,0,3)},0,section:new SectionGeometry("Rectangle",.3,depth));
        ModelElement Slab(double thickness)=>new ModelElement("Area:S","S","S","L1","S",ElementKind.Slab,new[]{new Vec3(-5,-2,3),new Vec3(10,-2,3),new Vec3(10,2,3),new Vec3(-5,2,3)},section:new SectionGeometry("Shell",thickness,thickness));
        double MinX(ProjectedElement i)=>i.Segments.SelectMany(s=>new[]{s.A.X,s.B.X}).Min();
        double MaxX(ProjectedElement i)=>i.Segments.SelectMany(s=>new[]{s.A.X,s.B.X}).Max();
        double MinY(ProjectedElement i)=>i.Segments.SelectMany(s=>new[]{s.A.Y,s.B.Y}).Min();
        double MaxY(ProjectedElement i)=>i.Segments.SelectMany(s=>new[]{s.A.Y,s.B.Y}).Max();
        var plane=ViewPlane.ElevationThrough(new Vec2(0,0),new Vec2(4,0),.15);
        Check("section range: excludes members beyond either endpoint",()=>{Assert(Projection.Build(Beam(-5,-1),plane)==null&&Projection.Build(Beam(5,8),plane)==null);});
        Check("section range: clips analysis and extruded edges to A-B",()=>{foreach(var i in new[]{Projection.Build(Beam(-5,10),plane),SectionExtrusion.Build(Beam(-5,10),plane)}){Near(MinX(i),0);Near(MaxX(i),4);Assert(i.AxisSegments.All(s=>s.A.X>=0&&s.B.X<=4));}});
        Check("section range: reverse changes projection sign but not physical extent",()=>{var reversed=ViewPlane.ElevationThrough(new Vec2(0,0),new Vec2(4,0),.15,true);var i=SectionExtrusion.Build(Beam(-5,10),reversed);Near(MinX(i),-4);Near(MaxX(i),0);Assert(SectionExtrusion.Build(Beam(5,8),reversed)==null);});
        Check("section range: swapped endpoints retain same physical members",()=>{var reversed=ViewPlane.ElevationThrough(new Vec2(4,0),new Vec2(0,0),.15);Near(MaxX(SectionExtrusion.Build(Beam(-5,10),reversed)),4);Assert(SectionExtrusion.Build(Beam(5,8),reversed)==null);});
        Check("section range: diagonal cut is bounded by physical length",()=>{var p=ViewPlane.ElevationThrough(new Vec2(1,1),new Vec2(4,5),.1);var e=new ModelElement("b","b","","","",ElementKind.Beam,new[]{new Vec3(-2,-3,3),new Vec3(7,9,3)},0);var i=Projection.Build(e,p);Near(MinX(i),0);Near(MaxX(i),5);});
        Check("section range: slab thickness does not extend beyond cut ends",()=>{var i=SectionExtrusion.Build(Slab(.2),plane);Near(MinX(i),0);Near(MaxX(i),4);Near(MaxY(i),3);Near(MinY(i),2.8);});
        Check("section range: wall polygon surrounding interval still displays clipped face",()=>{var w=new ModelElement("w","w","","","",ElementKind.Wall,new[]{new Vec3(-5,0,0),new Vec3(10,0,0),new Vec3(10,0,6),new Vec3(-5,0,6)},section:new SectionGeometry("Shell",.2,.2));var i=SectionExtrusion.Build(w,plane);Near(MinX(i),0);Near(MaxX(i),4);Near(MaxY(i),6);});
        Check("section range: full wall thickness can cross endpoint even if axis is outside",()=>{var c=new ModelElement("c","c","","","",ElementKind.Column,new[]{new Vec3(4.1,0,0),new Vec3(4.1,0,6)},0,section:new SectionGeometry("Rectangle",.4,.4));var i=SectionExtrusion.Build(c,plane);Assert(i!=null);Near(MaxX(i),4);Near(MinX(i),3.9);});
        Check("section range: wall thickness enters interval even with its analytical face outside",()=>{
            var w=new ModelElement("w","w","","","",ElementKind.Wall,new[]{new Vec3(4.1,-2,0),new Vec3(4.1,2,0),new Vec3(4.1,2,6),new Vec3(4.1,-2,6)},section:new SectionGeometry("Shell",.4,.4));
            var i=SectionExtrusion.Build(w,plane);Assert(i!=null);Near(MinX(i),3.9);Near(MaxX(i),4);
        });
        Check("section range: oblique wall cut preserves actual prism thickness",()=>{
            var w=new ModelElement("w","w","","","",ElementKind.Wall,new[]{new Vec3(-3,-3,0),new Vec3(3,3,0),new Vec3(3,3,6),new Vec3(-3,-3,6)},section:new SectionGeometry("Shell",.2,.2));
            var p=ViewPlane.ElevationThrough(new Vec2(-2,0),new Vec2(2,0),.00001);var i=SectionExtrusion.Build(w,p);
            Near(MaxX(i)-MinX(i),.2*Math.Sqrt(2)+.00002);Near(MaxY(i),6);Assert(i.Faces.Count==1);
        });
        Check("section tops: different slab thicknesses and beam share top, only bottoms vary",()=>{foreach(double t in new[]{.1,.2,.4}){var s=SectionExtrusion.Build(Slab(t),plane);var b=SectionExtrusion.Build(Beam(-5,10,.7),plane);Near(MaxY(s),3);Near(MaxY(b),3);Near(MinY(s),3-t);Near(MinY(b),2.3);}});
        Check("section tops: reversed polygon winding never extrudes slab upwards",()=>{var s=Slab(.2);var flipped=new ModelElement(s.Id,s.Name,s.Label,s.Story,s.Property,s.Kind,s.Vertices.Reverse(),section:s.Section);var i=SectionExtrusion.Build(flipped,plane);Near(MaxY(i),3);Near(MinY(i),2.8);});
        Check("section tops: rotated rectangular beam has no point above assigned cote",()=>{var b=Beam(0,4);b=new ModelElement(b.Id,b.Name,b.Label,b.Story,b.Property,b.Kind,b.Vertices,30,section:b.Section);Near(MaxY(SectionExtrusion.Build(b,ViewPlane.Elevation(0,0,0,2))),3);});
        Check("section range VM: cut persists in Plan and reverse, resets on model change",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.SetElevationThrough(new Vec2(0,0),new Vec2(2,0));Assert(vm.Elevation&&vm.CutStart.HasValue&&vm.CutEnd.HasValue);Assert(vm.Scene.All(i=>MaxX(i)<=2.0000001));vm.Reverse=true;Assert(vm.Scene.All(i=>MinX(i)>=-2.0000001));vm.Elevation=false;Assert(vm.CutEnd.HasValue);vm.SetSnapshot(DraftFixture(),true);Assert(!vm.CutStart.HasValue&&!vm.CutEnd.HasValue);});
        Check("column and wall outline survives both global and per-kind Extrude off",()=>{
            var c=new ModelElement("c","c","","","",ElementKind.Column,new[]{new Vec3(1,0,0),new Vec3(1,0,6)},0,section:new SectionGeometry("Rectangle",.4,.6));
            var w=new ModelElement("w","w","","","",ElementKind.Wall,new[]{new Vec3(0,0,0),new Vec3(4,0,0),new Vec3(4,0,6),new Vec3(0,0,6)},section:new SectionGeometry("Shell",.2,.2));
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(new ModelSnapshot(1,"ETABS","","id","",DateTime.UtcNow,new[]{c,w},new[]{new ModelLevel("L1",3)},Array.Empty<ExistingLoad>(),Array.Empty<string>()),true);
            var before=vm.Scene.Select(i=>i.Segments.Count).ToArray();vm.Extrude=false;Assert(vm.Scene.Select(i=>i.Segments.Count).SequenceEqual(before));foreach(var setting in vm.LabelSettings)setting.Extrude=false;vm.NotifyStyle();Assert(vm.Scene.Select(i=>i.Segments.Count).SequenceEqual(before));
        });
        Check("point load units: force and moment scale independently",()=>{var units=new UnitScale("N","mm");Near(1000*units.PointFactor(false),1);Near(1000000*units.PointFactor(true),1);var imperial=new UnitScale("kip","ft");Near(imperial.PointFactor(true),4.4482216152605*.3048);});
        var f=DraftFixture();var joint=new ModelElement("Joint:J1","J1","J1","L1","",ElementKind.Joint,new[]{new Vec3(2,0,3)});
        var loads=f.Loads.Concat(new[]{new ExistingLoad("Joint:J1","SDL","Joint force","Global",1,0,0,-8,-8,"kN",2),new ExistingLoad("Joint:J1","SDL","Joint moment","Local",6,0,0,4,4,"kN·m",3),new ExistingLoad("Joint:J1","SDL","Joint force","Global",2,0,0,0,0,"kN",2),new ExistingLoad("Frame:B1","SDL","Frame point force","Global",10,.25,.25,5,5,"kN")}).ToArray();
        var model=new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements.Concat(new[]{joint}),f.Levels,loads,f.Diagnostics);
        Check("joint model: one point validates, projects and retains step/sign/unit through JSON",()=>{var copy=JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Serialize(model));Assert(copy.Elements.Single(e=>e.Kind==ElementKind.Joint).Vertices.Count==1);var load=copy.Loads.Single(l=>l.ElementId==joint.Id&&l.Direction==1);Assert(load.Step==2&&load.Value1==-8&&load.Unit=="kN");Assert(Projection.Build(joint,ViewPlane.Plan(3,.1))!=null&&Projection.Build(joint,ViewPlane.Plan(6,.1))==null);});
        Check("read loads: simultaneous frame shell joint without summing patterns",()=>{var p=ViewPlane.Elevation(0,0,0,.1);var scene=model.Elements.Select(e=>Projection.Build(e,p)).Where(e=>e!=null).ToArray();var r=ReadLoadDisplay.Build(model,scene,p,null,true,true,true,false,Array.Empty<string>(),false);Assert(r.Select(i=>i.Category).Distinct().Count()==3&&r.Count==6);Assert(r.Any(i=>i.Text.Contains("-8 kN")&&i.Text.Contains("step 2"))&&r.Any(i=>i.Text.Contains("Local / M3")));});
        Check("read loads: pattern, type, zero and selected filters compose",()=>{var p=ViewPlane.Plan(3,.1);var scene=model.Elements.Select(e=>Projection.Build(e,p)).Where(e=>e!=null).ToArray();var r=ReadLoadDisplay.Build(model,scene,p,"SDL",false,false,true,true,new[]{joint.Id},true);Assert(r.Count==3);Assert(ReadLoadDisplay.Build(model,scene,p,"SDL",true,true,true,true,Array.Empty<string>(),true).Count==0);Assert(ReadLoadDisplay.Build(model,scene,p,"LL",true,true,true,false,Array.Empty<string>(),true).Count==0);});
        Check("read loads: finite cut excludes out-of-segment frame point assignments",()=>{var p=ViewPlane.ElevationThrough(new Vec2(2,0),new Vec2(4,0),.1);var scene=model.Elements.Select(e=>Projection.Build(e,p)).Where(e=>e!=null).ToArray();var r=ReadLoadDisplay.Build(model,scene,p,null,true,false,true,false,Array.Empty<string>(),false);Assert(!r.Any(i=>i.Load.Kind=="Frame point force"));foreach(var item in r){Assert(MinX(item.Geometry)>=-1e-8&&MaxX(item.Geometry)<=2+1e-8);}});
        Check("read loads VM: independent toggles, selection and removal refresh overlays",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(model,true);vm.Level=vm.Levels.Single(l=>l.Name=="L1");Assert(vm.ReadLoadItems.Count==0);vm.ShowFrameLoads=true;vm.ShowJointLoads=true;Assert(vm.ReadLoadItems.Any(i=>i.Category=="Joint")&&vm.ReadLoadItems.Any(i=>i.Category=="Frame"));vm.SelectElements(new[]{joint.Id});vm.OnlySelectedLoads=true;Assert(vm.ReadLoadItems.Count==2);vm.RemoveSelectedFromWorkspace();Assert(vm.ReadLoadItems.Count==0);vm.RestoreWorkspace();vm.OnlySelectedLoads=false;Assert(vm.ReadLoadItems.Any(i=>i.Category=="Joint"));vm.ShowJointLoads=false;Assert(vm.ReadLoadItems.All(i=>i.Category=="Frame"));});
        Check("read loads: reading display never changes source or assignments",()=>{string before=JsonCodec.Serialize(model);var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(model,true);vm.ShowFrameLoads=true;vm.ShowShellLoads=true;vm.ShowJointLoads=true;vm.ReadPattern="SDL";vm.SetElevationThrough(new Vec2(0,0),new Vec2(4,0));Assert(JsonCodec.Serialize(model)==before);});
    }
}
