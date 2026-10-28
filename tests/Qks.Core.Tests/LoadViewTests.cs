using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static void LoadViewTests()
    {
        var axes=new[]{new Vec3(0,1,0),new Vec3(-1,0,0),new Vec3(0,0,1)};
        var beam=new ModelElement("Frame:B","B","B1","L","B300",ElementKind.Beam,new[]{new Vec3(0,0,3),new Vec3(10,0,3)},localAxes:axes);
        var slab=new ModelElement("Area:S","S","F1","L","S200",ElementKind.Slab,new[]{new Vec3(0,0,3),new Vec3(10,0,3),new Vec3(10,5,3),new Vec3(0,5,3)});
        var joint=new ModelElement("Joint:J","J","","L","",ElementKind.Joint,new[]{new Vec3(2,2,3)},localAxes:axes);
        ReadLoadDisplay Row(ModelElement e,int dir,string system,double q1=10,double q2=10,string kind=null,ViewPlane plane=null) {
            plane=plane??ViewPlane.Plan(3,.1);var l=new ExistingLoad(e.Id,"SDL",kind??(e.Kind==ElementKind.Joint?"Joint force":"Frame force/length"),system,dir,0,1,q1,q2,"kN");
            var snapshot=new ModelSnapshot(1,"ETABS","","id","",DateTime.UtcNow,new[]{e},Array.Empty<ModelLevel>(),new[]{l},Array.Empty<string>());
            return ReadLoadDisplay.Build(snapshot,new[]{Projection.Build(e,plane)},plane,null,true,true,true,false,Array.Empty<string>(),true).Single();
        }
        Check("load vectors: gravity normal to Plan, downward on Elevation",()=>{var r=Row(beam,10,"Global");var v=LoadGlyphs.Direction(r).Value;Near(v.Dot(r.Plane.Normal),-1);Near(v.Dot(r.Plane.U),0);Near(v.Dot(ViewPlane.Elevation(0,0,0,.1).V),-1);});
        Check("joint vectors: Fy up on Plan, signed negative value preserved",()=>{var r=Row(joint,2,"Global",-8,-8);var v=LoadGlyphs.Direction(r).Value;Near(v.Y,1);Assert(r.Load.Value1==-8);});
        Check("local matrix rotated axes are used, named system is not guessed",()=>{Near(LoadGlyphs.Direction(Row(joint,1,"Local")).Value.Y,1);Near(LoadGlyphs.Direction(Row(beam,2,"Local")).Value.X,-1);Assert(!LoadGlyphs.Direction(Row(beam,4,"CS1")).HasValue);Assert(!LoadGlyphs.Direction(Row(joint,2,"CS1")).HasValue);});
        Check("invalid or unavailable local matrix has no invented vector",()=>{Assert(!LoadGlyphs.ValidAxes(new[]{new Vec3(1,0,0),new Vec3(1,0,0),new Vec3(0,0,1)}));Assert(!LoadGlyphs.Direction(Row(slab,3,"Local")).HasValue);});
        Check("load scales: proportional signed magnitudes, zero and finite limits",()=>{Near(LoadGlyphs.ArrowLength(50,100),42);Near(LoadGlyphs.ArrowLength(-100,100),84);Near(LoadGlyphs.ArrowLength(0,100),0);Near(LoadGlyphs.ArrowLength(double.NaN,100),0);Near(LoadGlyphs.ArrowLength(100,100,2.5),160);});
        Check("moment glyphs use separate scale and preserve signed axis",()=>{var r=Row(joint,6,"Global",-4,-4,"Joint moment");Assert(LoadGlyphs.IsMoment(r));Near(LoadGlyphs.Direction(r).Value.Z,1);Assert(LoadGlyphs.ScaleGroup(r)!=LoadGlyphs.ScaleGroup(Row(joint,3,"Global")));});
        Check("trapezoid interpolates original stations after finite section clipping",()=>{var r=Row(beam,10,"Global",0,100,plane:ViewPlane.ElevationThrough(new Vec2(2,0),new Vec2(6,0),.1));Near(LoadGlyphs.ValueAt(r,new Vec2(0,3)),20);Near(LoadGlyphs.ValueAt(r,new Vec2(4,3)),60);Assert(LoadGlyphs.Samples(r,50).All(p=>p.X>=0&&p.X<=4));});
        Check("slab Plan glyph is at area centroid; frame Plan retains samples",()=>{var r=Row(slab,10,"Global",kind:"Area uniform");var samples=LoadGlyphs.Samples(r,50);Assert(samples.Length==1);Near(samples[0].X,5);Near(samples[0].Y,2.5);Assert(LoadGlyphs.Samples(Row(beam,10,"Global"),50).Length>2);});
        Check("local axes survive model JSON roundtrip",()=>{var c=JsonCodec.Deserialize<ModelElement>(JsonCodec.Serialize(beam));Assert(LoadGlyphs.ValidAxes(c.LocalAxes));Near(c.LocalAxes[0].Y,1);});
        Check("saved sections JSON retains endpoints, range, reverse and depth",()=>{var c=new SectionCatalog(1,"C:\\Model.edb",new[]{new SavedSection("MC-01",new Vec2(2,3),new Vec2(2,8),.2,true)});var copy=JsonCodec.Deserialize<SectionCatalog>(JsonCodec.Serialize(c));var s=copy.Sections.Single();Near(s.Start.X,2);Near(s.Start.Y,3);Near(s.End.Y,8);Assert(s.Reverse);Near(ViewPlane.ElevationThrough(s.Start,s.End,s.HalfDepth,s.Reverse).MinU.Value,-5);});
        Check("saved section rejects zero length, empty name and future schema",()=>{Throws<ArgumentException>(()=>new SavedSection("MC",new Vec2(0,0),new Vec2(0,0),.1,false));Throws<ArgumentException>(()=>new SavedSection(" ",new Vec2(0,0),new Vec2(1,0),.1,false));Throws<ArgumentException>(()=>new SectionCatalog(2,"model",Array.Empty<SavedSection>()));});
        Check("catalog rejects duplicate names independent of case",()=>{var a=new SavedSection("MC",new Vec2(0,0),new Vec2(1,0),.1,false);var b=new SavedSection("mc",a.Start,a.End,.1,false);Throws<ArgumentException>(()=>new SectionCatalog(1,"model",new[]{a,b}));});
        Check("VM saves, replaces and reopens section without changing model",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);string before=JsonCodec.Serialize(vm.Snapshot);vm.BeginNewSection();vm.SetElevationThrough(new Vec2(1,0),new Vec2(4,0));vm.HalfDepth="0.2";vm.Reverse=true;vm.SaveCurrentSection();Assert(vm.SavedSections.Count==1);vm.SaveCurrentSection();Assert(vm.SavedSections.Count==1);vm.Elevation=false;vm.Reverse=false;vm.HalfDepth="0.5";vm.OpenSelectedSection();Assert(vm.Elevation&&vm.Reverse);Near(vm.CurrentPlane().HalfDepth,.2);Assert(JsonCodec.Serialize(vm.Snapshot)==before);vm.SetSnapshot(DraftFixture(),true);Assert(vm.SavedSections.Count==0);});
        var assignment=new LoadTable(new[]{"UniqueName","LoadSet"},1,new[]{"S","Residential"});
        var definitions=new LoadTable(new[]{"Name","LoadPat","Value"},2,new[]{"Residential","SDL","2.5","Residential","LL","-1.2"});
        Check("Uniform Load Sets expand verified area assignments and signed patterns",()=>{var loads=UniformSetLoads.Expand(assignment,definitions,new[]{slab},new UnitScale("kN","m"));Assert(loads.Count==2&&loads.All(l=>l.ElementId=="Area:S"&&l.Direction==10));Near(loads[1].Value1,-1.2);Assert(loads[0].Kind.Contains("Residential"));});
        Check("Uniform Load Sets do not infer assignments from section property",()=>{var wrong=new LoadTable(new[]{"Area","LoadSet"},1,new[]{"F1","Residential"});Throws<ArgumentException>(()=>UniformSetLoads.Expand(wrong,definitions,new[]{slab},new UnitScale("kN","m")));});
        Check("Uniform Load Sets reject malformed numeric values instead of zero",()=>{foreach(var value in new[]{"","oops","NaN","1,234"}) {var d=new LoadTable(new[]{"Name","LoadPat","Value"},1,new[]{"Residential","SDL",value});Throws<ArgumentException>(()=>UniformSetLoads.Expand(assignment,d,new[]{slab},new UnitScale("kN","m")));}});
        Check("Uniform Load Sets respect direction/system and pressure conversion",()=>{var d=new LoadTable(new[]{"Name","LoadPat","Value","Dir","CSys"},1,new[]{"Residential","SDL","0.001","Local 3","Local"});var r=UniformSetLoads.Expand(assignment,d,new[]{slab},new UnitScale("N","mm")).Single();Near(r.Value1,1);Assert(r.Direction==3&&r.CoordinateSystem=="Local");});
        Check("load tables reject truncated arrays, unknown directions and missing columns",()=>{Throws<ArgumentException>(()=>new LoadTable(new[]{"Name","Load"},2,new[]{"a"}));Throws<ArgumentException>(()=>UniformSetLoads.ParseDirection("oops"));Throws<ArgumentException>(()=>UniformSetLoads.Expand(new LoadTable(new[]{"Label"},1,new[]{"S"}),definitions,new[]{slab},new UnitScale("kN","m")));});
    }
}
