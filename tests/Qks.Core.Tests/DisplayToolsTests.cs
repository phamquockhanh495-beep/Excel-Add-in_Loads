using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static void DisplayToolsTests()
    {
        Check("text appearance: RGB limits, finite size and profile roundtrip",()=>{
            var p=new ViewerTextProfile(new TextAppearance(13.5,0,127,255),new TextAppearance(9,255,0,128,false));
            var copy=JsonCodec.Deserialize<ViewerTextProfile>(JsonCodec.Serialize(p));Near(copy.Structure.Size,13.5);Assert(copy.Structure.Hex=="#007FFF"&&!copy.Loads.UseColor&&copy.Loads.Hex=="#FF0080");
            foreach(double size in new[]{double.NaN,double.PositiveInfinity,5,31})Throws<ArgumentException>(()=>new TextAppearance(size,0,0,0));
            Throws<ArgumentException>(()=>new TextAppearance(12,-1,0,0));Throws<ArgumentException>(()=>new TextAppearance(12,0,256,0));Throws<ArgumentException>(()=>new TextAppearance(12,0,0,256));
        });
        Check("text profile updates both bindings without modifying geometry or loads",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(SelectFixture(),true);string before=JsonCodec.Serialize(vm.Snapshot);var changes=new System.Collections.Generic.List<string>();vm.PropertyChanged+=(_,e)=>changes.Add(e.PropertyName);
            vm.ApplyTextProfile(new ViewerTextProfile(new TextAppearance(14,1,2,3),new TextAppearance(11,4,5,6)));
            Assert(changes.Contains(nameof(vm.StructureText))&&changes.Contains(nameof(vm.LoadText))&&JsonCodec.Serialize(vm.Snapshot)==before);
        });
        Check("inspector converts actual metre dimensions to mm for all four kinds",()=>{
            ModelElement E(ElementKind k,SectionGeometry s)=>new ModelElement("id","n","","","NAME DOES NOT DEFINE DIMENSIONS",k,new[]{new Vec3(),new Vec3(1,0,0)},section:s);
            foreach(var k in new[]{ElementKind.Slab,ElementKind.Wall})Assert(VisualWorkspaceViewModel.SectionDescription(E(k,new SectionGeometry("Shell",.175,.175)))=="Thickness = 175 mm");
            foreach(var k in new[]{ElementKind.Beam,ElementKind.Column})Assert(VisualWorkspaceViewModel.SectionDescription(E(k,new SectionGeometry("Rectangle",.24,.7)))=="(Width × Height) = (240 × 700) mm");
            Assert(VisualWorkspaceViewModel.SectionDescription(E(ElementKind.Column,new SectionGeometry("Circle",.4,.4))).Contains("Tròn"));
        });
        var f=SelectFixture();var loads=f.Loads.Concat(new[]{
            new ExistingLoad("Area:1","SDL","Area uniform set: Residential","Global",10,0,1,2,2,"kN/m²"),
            new ExistingLoad("Area:1","LL","Area uniform set: Residential","Global",10,0,1,3,3,"kN/m²"),
            new ExistingLoad("Area:1","SDL","Area uniform","Global",10,0,1,4,4,"kN/m²"),
            new ExistingLoad("Area:2","SDL","Area uniform set: Core","Global",10,0,1,5,5,"kN/m²")});
        var model=new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,f.Levels,loads,f.Diagnostics);
        VisualWorkspaceViewModel VM(){var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(model,true);vm.Level=vm.Levels.First();return vm;}
        Check("load sets ignore pattern and collapse components into name-only assignments",()=>{var vm=VM();vm.ReadPattern="WL";vm.UniformLoadSet="Residential";Assert(vm.LoadSetsMode&&vm.ReadLoadItems.Count==1&&vm.ReadLoadItems[0].Text=="Residential");Assert(vm.SlabRegions.Single().Label=="Residential");vm.ReadPattern="LL";Assert(vm.ReadLoadItems.Count==1&&vm.ReadLoadItems[0].Text=="Residential");});
        Check("pattern mode ignores saved set filter and respects pattern values",()=>{var vm=VM();vm.UniformLoadSet="Residential";vm.PatternMode=true;vm.ReadPattern="SDL";Assert(vm.ReadLoadItems.Count==3);Assert(vm.ReadLoadItems.Any(r=>r.UniformLoadSet=="Core"));Assert(vm.SlabRegions.Single().Label.Contains("2")&&vm.SlabRegions.Single().Label.Contains("4"));});
        Check("load sets all shows each set once and selection still filters",()=>{var vm=VM();vm.ShowFrameLoads=true;vm.LoadSetsMode=true;Assert(vm.ReadLoadItems.Count==2&&vm.ReadLoadItems.All(r=>r.Category=="Shell"));vm.SelectElements(new[]{"Area:2"});vm.OnlySelectedLoads=true;Assert(vm.ReadLoadItems.Single().Text=="Core");});
        Check("slab color classification is stable across read order",()=>{var vm=VM();vm.ShowShellLoads=true;var a=SlabLoadRegion.Build(vm.ReadLoadItems,false);var b=SlabLoadRegion.Build(vm.ReadLoadItems.Reverse(),false);Assert(a.Single().Key==b.Single().Key&&a.Single().Color==b.Single().Color);string before=JsonCodec.Serialize(vm.Snapshot);vm.LoadSetsMode=true;Assert(vm.SlabRegions.Single().Label=="Residential"&&JsonCodec.Serialize(vm.Snapshot)==before);});
        Check("reread removes stale set choice and does not infer set from property",()=>{var vm=VM();vm.UniformLoadSet="Residential";vm.SetSnapshot(SelectFixture(),true);Assert(vm.UniformLoadSet==VisualWorkspaceViewModel.AllUniformSets&&vm.UniformLoadSets.Count()==1);});
    }
}
