using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static ModelSnapshot SelectFixture()
    {
        ModelElement E(string id,string name,string label,ElementKind kind,double z)=>new ModelElement(id,name,label,z==3?"L1":"L2","P1",kind,
            kind==ElementKind.Slab||kind==ElementKind.Wall?new[]{new Vec3(0,0,z),new Vec3(5,0,z),new Vec3(5,4,z),new Vec3(0,4,z)}:new[]{new Vec3(0,0,z),new Vec3(5,0,z)});
        return new ModelSnapshot(1,"ETABS","","model","",DateTime.UtcNow,new[]{E("Frame:1","1","B1",ElementKind.Beam,3),E("Frame:2","2","B1",ElementKind.Beam,6),E("Frame:3","3","B10",ElementKind.Beam,3),E("Frame:4","4","C1",ElementKind.Column,3),E("Area:1","1","F1",ElementKind.Slab,3),E("Area:2","2","W1",ElementKind.Wall,3)},new[]{new ModelLevel("L1",3),new ModelLevel("L2",6)},new[]{new ExistingLoad("Frame:1","WL","Frame force/length","Global",10,0,1,2,2,"kN/m")},Array.Empty<string>());
    }
    private static void SelectToolTests()
    {
        VisualWorkspaceViewModel VM(){var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(SelectFixture(),true);vm.Level=vm.Levels.First();return vm;}
        Check("attribute select: section scoped independently for four structural kinds",()=>{var vm=VM();foreach(var type in new[]{ElementKind.Beam,ElementKind.Column,ElementKind.Slab,ElementKind.Wall}){int n=vm.SelectByAttribute(type,"Section",new[]{"P1"},true,"Replace");Assert(n>0&&vm.SelectedElements.All(e=>e.Kind==type));}});
        Check("attribute select: exact Label distinguishes B1 from B10 and view from all floors",()=>{var vm=VM();Assert(vm.SelectByAttribute(ElementKind.Beam,"Label",new[]{"B1"},true,"Replace")==1);Assert(vm.SelectedId=="Frame:1");Assert(vm.SelectByAttribute(ElementKind.Beam,"Label",new[]{"B1"},false,"Replace")==2);Assert(!vm.SelectedIds.Contains("Frame:3"));});
        Check("attribute select: UniqueName never confuses Frame and Area with same name",()=>{var vm=VM();vm.SelectByAttribute(ElementKind.Slab,"UniqueName",new[]{"1"},true,"Replace");Assert(vm.SelectedId=="Area:1");});
        Check("attribute select: multiple values add and remove without toggling unrelated selection",()=>{var vm=VM();vm.SelectByAttribute(ElementKind.Slab,"UniqueName",new[]{"1"},true,"Replace");vm.SelectByAttribute(ElementKind.Beam,"UniqueName",new[]{"1","3"},true,"Add");Assert(vm.SelectedIds.Count==3);vm.SelectByAttribute(ElementKind.Beam,"UniqueName",new[]{"1"},true,"Remove");Assert(vm.SelectedIds.OrderBy(s=>s).SequenceEqual(new[]{"Area:1","Frame:3"}));});
        Check("attribute select: search filters offered values, not partial match selection",()=>{var vm=VM();Assert(vm.SelectionValues(ElementKind.Beam,"Label",true,"b1").Length==2);Assert(vm.SelectByAttribute(ElementKind.Beam,"Label",new[]{"B"},true,"Replace")==0);});
        Check("isolate/hide/show all preserve model and reset load overlays correctly",()=>{var vm=VM();string before=JsonCodec.Serialize(vm.Snapshot);vm.ShowFrameLoads=true;vm.SelectElements(new[]{"Frame:1"});vm.IsolateSelection();Assert(vm.IsIsolated&&vm.Scene.Count==1&&vm.ReadLoadItems.Count==1);vm.RemoveSelectedFromWorkspace();Assert(vm.Scene.Count==0&&vm.ReadLoadItems.Count==0);Assert(vm.SelectionValues(ElementKind.Beam,"UniqueName",false,"").Length==0);vm.ShowAllObjects();Assert(!vm.IsIsolated&&vm.RemovedFromWorkspaceCount==0&&vm.Scene.Count==5&&vm.ReadLoadItems.Count==1);Assert(JsonCodec.Serialize(vm.Snapshot)==before);});
        Check("show all clears kind/search/visibility filters but preserves floor and extrude",()=>{var vm=VM();vm.Kind="Beam";vm.Search="missing";vm.Extrude=false;var setting=vm.LabelSettings.Single(s=>s.Kind==ElementKind.Slab);setting.Visible=false;bool notified=false;setting.PropertyChanged+=(_,e)=>notified|=e.PropertyName=="Visible";vm.ShowAllObjects();Assert(vm.Kind=="Tất cả"&&vm.Search==""&&setting.Visible&&notified&&vm.Level.Name=="L1"&&!vm.Extrude&&vm.Scene.Count==5);});
        Check("selection commands respect busy state; reread clears isolation",()=>{var vm=VM();vm.SelectElements(new[]{"Frame:1"});vm.Busy=true;vm.IsolateSelection();vm.SelectByAttribute(ElementKind.Slab,"UniqueName",new[]{"1"},true,"Replace");Assert(!vm.IsIsolated&&vm.SelectedId=="Frame:1"&&!vm.CanShowAll);vm.Busy=false;vm.IsolateSelection();vm.SetSnapshot(SelectFixture(),true);Assert(!vm.IsIsolated&&vm.SelectedIds.Count==0);});
    }
}
