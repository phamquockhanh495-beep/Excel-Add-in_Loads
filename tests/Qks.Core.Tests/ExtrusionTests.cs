using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static void ExtrusionTests()
    {
        ModelElement Member(Vec3 a,Vec3 b,double angle=0,string shape="Rectangle",bool advanced=false) =>
            new ModelElement("Frame:E","E","B1","L1","arbitrary name",ElementKind.Beam,new[]{a,b},angle,advanced,new SectionGeometry(shape,.3,.6));
        double Width(ProjectedElement e) => e.Segments.SelectMany(s=>new[]{s.A.X,s.B.X}).Max()-e.Segments.SelectMany(s=>new[]{s.A.X,s.B.X}).Min();
        double Height(ProjectedElement e) => e.Segments.SelectMany(s=>new[]{s.A.Y,s.B.Y}).Max()-e.Segments.SelectMany(s=>new[]{s.A.Y,s.B.Y}).Min();
        var beam=Member(new Vec3(0,0,3),new Vec3(5,0,3));
        Check("extrude: beam actual width in plan and depth in elevation",()=>{
            var plan=SectionExtrusion.Build(beam,ViewPlane.Plan(3,.15));Near(Width(plan),5);Near(Height(plan),.3);
            var elevation=SectionExtrusion.Build(beam,ViewPlane.Elevation(0,0,0,.15));Near(Width(elevation),5);Near(Height(elevation),.6);
        });
        Check("extrude: local rotation swaps width and depth",()=>{
            var item=SectionExtrusion.Build(Member(new Vec3(0,0,3),new Vec3(5,0,3),90),ViewPlane.Plan(3,1));Near(Height(item),.6);
        });
        Check("extrude: vertical column footprint and rotation",()=>{
            var column=Member(new Vec3(0,0,0),new Vec3(0,0,6));var plane=ViewPlane.Plan(3,.15);
            var item=SectionExtrusion.Build(column,plane);Near(Width(item),.6);Near(Height(item),.3);Assert(item.Polygon.Count==4);
            item=SectionExtrusion.Build(Member(new Vec3(0,0,0),new Vec3(0,0,6),90),plane);Near(Width(item),.3);Near(Height(item),.6);
        });
        Check("extrude: circular column outline stays circular",()=>{var item=SectionExtrusion.Build(Member(new Vec3(0,0,0),new Vec3(0,0,6),shape:"Circle"),ViewPlane.Plan(3,.15));Near(Width(item),.6);Near(Height(item),.6);Assert(item.Polygon.Count==48);});
        Check("extrude: thin view band clips a sloping prism",()=>{
            var item=SectionExtrusion.Build(Member(new Vec3(0,0,0),new Vec3(4,0,4)),ViewPlane.Plan(2,.1));
            Assert(item!=null&&item.IsCut&&item.Polygon.Count>=4&&Width(item)<2);Near(Height(item),.3);
        });
        Check("extrude: solid intersects band even if axis is outside",()=>{var item=SectionExtrusion.Build(beam,ViewPlane.Plan(2.75,.05));Assert(item!=null&&item.Faces.Count==1&&item.AxisSegments.Count==0);Assert(SectionExtrusion.Build(beam,ViewPlane.Plan(5,.05))==null);});
        Check("extrude: missing section and advanced axes preserve analytical geometry",()=>{
            var plain=new ModelElement("f","f","","","B300x600",ElementKind.Beam,beam.Vertices,0);
            Assert(SectionExtrusion.Build(plain,ViewPlane.Plan(3,.15)).Faces.Count==0);
            Assert(SectionExtrusion.Build(Member(beam.Vertices[0],beam.Vertices[1],advanced:true),ViewPlane.Plan(3,.15)).Faces.Count==0);
        });
        Check("extrude: slab remains concave in plan and shows thickness in elevation",()=>{
            var slab=new ModelElement("s","s","","","S",ElementKind.Slab,new[]{new Vec3(0,0,3),new Vec3(4,0,3),new Vec3(4,1,3),new Vec3(1,1,3),new Vec3(1,4,3),new Vec3(0,4,3)},section:new SectionGeometry("Shell",.2,.2));
            var plan=SectionExtrusion.Build(slab,ViewPlane.Plan(3,.15));Assert(plan.Polygon.Count==6&&!ViewerSelection.Hit(plan,new Vec2(3,3),.01));
            var elevation=SectionExtrusion.Build(slab,ViewPlane.Elevation(0,.5,0,.05));Near(Height(elevation),.2);
        });
        Check("extrude: wall thickness in plan and true face in elevation",()=>{
            var wall=new ModelElement("w","w","","","W",ElementKind.Wall,new[]{new Vec3(0,0,0),new Vec3(4,0,0),new Vec3(4,0,3),new Vec3(0,0,3)},section:new SectionGeometry("Shell",.25,.25));
            Near(Height(SectionExtrusion.Build(wall,ViewPlane.Plan(1.5,.1))),.25);
            var elevation=SectionExtrusion.Build(wall,ViewPlane.Elevation(0,0,0,.1));Near(Height(elevation),3);Near(Width(elevation),4);
        });
        Check("extrude: selection hits physical footprint and snap stays on analysis axis",()=>{
            var item=SectionExtrusion.Build(beam,ViewPlane.Plan(3,.15));Assert(ViewerSelection.Hit(item,new Vec2(2,.14),.001));Assert(!ViewerSelection.Hit(item,new Vec2(2,.2),.001));
            Assert(ViewerSelection.InBox(item,new Box2(new Vec2(1,.1),new Vec2(2,.2)),true));Assert(!ViewerSelection.InBox(item,new Box2(new Vec2(0,0),new Vec2(5,.1)),false));
            var snap=ModelSnap.Find(new[]{item},new Vec2(2.5,.05),.1);Near(snap.Point.Y,0);Assert(snap.Kind==SnapKind.Midpoint);
        });
        Check("extrude: coplanar openings remove slab fill and hit region",()=>{
            ModelElement Area(string id,ElementKind kind,double size,double z)=>new ModelElement(id,id,"","","",kind,new[]{new Vec3(-size,-size,z),new Vec3(size,-size,z),new Vec3(size,size,z),new Vec3(-size,size,z)});
            var plane=ViewPlane.Plan(3,.15);var slab=Projection.Build(Area("slab",ElementKind.Slab,4,3),plane);
            var hole=Projection.Build(Area("hole",ElementKind.Opening,1,3),plane);
            var item=SectionExtrusion.WithOpenings(slab,new[]{hole});Assert(item.Voids.Count==1);
            Assert(!ViewerSelection.Hit(item,new Vec2(0,0),.01)&&ViewerSelection.Hit(item,new Vec2(2,0),.01));
            Assert(!ViewerSelection.InBox(item,new Box2(new Vec2(-.2,-.2),new Vec2(.2,.2)),true));
            var upper=Projection.Build(Area("upper",ElementKind.Opening,1,3.1),plane);Assert(SectionExtrusion.WithOpenings(slab,new[]{upper}).Voids.Count==0);
        });
        Check("extrude: invalid API dimensions are rejected",()=>{Throws<ArgumentException>(()=>new SectionGeometry("Rectangle",0,.2));Throws<ArgumentException>(()=>new SectionGeometry("Shell",.2,double.NaN));});
        Check("extrude: dimensions survive model serialization",()=>{var copy=JsonCodec.Deserialize<ModelElement>(JsonCodec.Serialize(beam));Near(copy.Section.Width,.3);Near(copy.Section.Depth,.6);});
        Check("extrude: global and independent switches rebuild geometry",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(new ModelSnapshot(1,"ETABS","","id","m",DateTime.UtcNow,new[]{beam},new[]{new ModelLevel("L1",3)},Array.Empty<ExistingLoad>(),Array.Empty<string>()),true);
            Assert(vm.Scene.Single().Faces.Count>0);vm.Extrude=false;Assert(vm.Scene.Single().Faces.Count==0);vm.Extrude=true;
            vm.LabelSettings.Single(s=>s.Kind==ElementKind.Beam).Extrude=false;vm.NotifyStyle();Assert(vm.Scene.Single().Faces.Count==0);
        });
    }
}
