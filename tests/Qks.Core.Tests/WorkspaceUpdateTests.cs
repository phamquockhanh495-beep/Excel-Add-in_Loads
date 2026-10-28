using System;
using System.Collections.Generic;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static VisualWorkspaceViewModel WorkspaceFixture()
    {
        var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.Level=vm.Levels.Single(l=>l.Name=="L1");return vm;
    }
    private static void WorkspaceUpdateTests()
    {
        Check("workspace Delete: removes selected beam and slab from view and source table",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1","Area:S1"});vm.RemoveSelectedFromWorkspace();Assert(vm.RemovedFromWorkspaceCount==2&&vm.SelectedIds.Count==0&&vm.CanRestoreWorkspace&&!vm.CanRemoveFromWorkspace);Assert(vm.Scene.All(e=>e.Source.Id!="Frame:B1"&&e.Source.Id!="Area:S1")&&vm.VisibleElements.All(e=>e.Id!="Frame:B1"&&e.Id!="Area:S1")); });
        Check("workspace Delete: snapshot and existing/draft loads are unchanged",()=> { var vm=WorkspaceFixture();vm.Drafts.Put(Slab());string before=JsonCodec.Serialize(vm.Snapshot),draft=JsonCodec.Serialize(vm.Drafts.Document());bool dirty=vm.Drafts.IsDirty,undo=vm.Drafts.CanUndo;vm.SelectElements(new[]{"Frame:B1","Area:S1"});vm.RemoveSelectedFromWorkspace();Assert(JsonCodec.Serialize(vm.Snapshot)==before&&JsonCodec.Serialize(vm.Drafts.Document())==draft&&vm.Drafts.IsDirty==dirty&&vm.Drafts.CanUndo==undo); });
        Check("workspace Delete: restore returns geometry and keeps selection empty",()=> { var vm=WorkspaceFixture();var ids=vm.Scene.Select(e=>e.Source.Id).ToArray();vm.SelectElements(new[]{"Frame:B1","Area:S1"});vm.RemoveSelectedFromWorkspace();vm.RestoreWorkspace();Assert(vm.Scene.Select(e=>e.Source.Id).SequenceEqual(ids)&&vm.RemovedFromWorkspaceCount==0&&!vm.CanRestoreWorkspace&&vm.SelectedIds.Count==0); });
        Check("workspace Delete: removal survives filters and Plan/Elevation changes",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1"});vm.RemoveSelectedFromWorkspace();vm.Elevation=true;vm.Kind="Beam";vm.Search="B1";vm.Refresh();Assert(vm.Scene.Count==0);vm.Search="";vm.Kind="Tất cả";vm.Elevation=false;Assert(vm.VisibleElements.All(e=>e.Id!="Frame:B1")); });
        Check("workspace Delete: hidden IDs cannot be reselected",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1"});vm.RemoveSelectedFromWorkspace();vm.SelectElements(new[]{"Frame:B1","Area:S1"});Assert(vm.SelectedIds.SequenceEqual(new[]{"Area:S1"})); });
        Check("workspace Delete: separate batches restore together and respect current Story",()=> { var vm=WorkspaceFixture();foreach(string id in new[]{"Frame:B1","Area:S1"}) { vm.SelectElements(new[]{id,id});vm.RemoveSelectedFromWorkspace(); }Assert(vm.RemovedFromWorkspaceCount==2);vm.RestoreWorkspace();Assert(vm.RemovedFromWorkspaceCount==0&&vm.VisibleElements.Any(e=>e.Id=="Frame:B1")&&vm.VisibleElements.All(e=>e.Id!="Area:S1"));vm.Level=vm.Levels.Single(l=>l.Name=="Roof");Assert(vm.VisibleElements.Any(e=>e.Id=="Area:S1")); });
        Check("workspace Delete: empty selection is a no-op",()=> { var vm=WorkspaceFixture();int count=vm.Scene.Count;vm.RemoveSelectedFromWorkspace();Assert(vm.Scene.Count==count&&vm.RemovedFromWorkspaceCount==0); });
        Check("workspace Delete: busy operations prevent remove and restore",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1"});vm.Busy=true;vm.RemoveSelectedFromWorkspace();Assert(vm.RemovedFromWorkspaceCount==0&&!vm.CanRemoveFromWorkspace);vm.Busy=false;vm.RemoveSelectedFromWorkspace();vm.Busy=true;vm.RestoreWorkspace();Assert(vm.RemovedFromWorkspaceCount==1&&!vm.CanRestoreWorkspace); });
        Check("workspace Delete: new snapshot starts with a restored workspace",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1"});vm.RemoveSelectedFromWorkspace();vm.SetSnapshot(DraftFixture(),true);Assert(vm.RemovedFromWorkspaceCount==0&&!vm.CanRestoreWorkspace); });
        Check("workspace Delete: loads and comparisons exclude removed members",()=> { var vm=WorkspaceFixture();Assert(vm.VisibleLoads.Any(l=>l.ElementId=="Frame:B1"));vm.SelectElements(new[]{"Frame:B1"});vm.RemoveSelectedFromWorkspace();Assert(vm.VisibleLoads.All(l=>l.ElementId!="Frame:B1")&&vm.ComparisonRows.All(l=>l.Load.ElementId!="Frame:B1")); });
        Check("workspace Delete: draft overlays restore and hidden draft editing requires restore",()=> { var vm=WorkspaceFixture();vm.SelectedElement=vm.Snapshot.Elements.Single(e=>e.Id=="Frame:B1");vm.DraftPattern="SDL";vm.DraftValue1="2";vm.SaveEditorToDraft();var draft=vm.DraftRows.Single();Assert(vm.DraftOverlays.Count==1);vm.RemoveSelectedFromWorkspace();Assert(vm.DraftOverlays.Count==0&&vm.DraftRows.Count==1);Throws<InvalidOperationException>(()=>vm.EditDraft(draft));vm.RestoreWorkspace();vm.EditDraft(draft);Assert(vm.SelectedId=="Frame:B1"&&vm.DraftOverlays.Count==1); });
        Check("selection badge: single uses raw Name, Label, Story and Property",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1"});var e=vm.SelectedElement;Assert(vm.SelectionSummary.Contains(e.Name)&&vm.SelectionSummary.Contains(e.Label)&&vm.SelectionSummary.Contains(e.Story)&&vm.SelectionSummary.Contains(e.Property)&&!vm.SelectionSummary.Contains("Frame:")); });
        Check("selection badge: multiple objects show count by type",()=> { var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1","Area:S1"});Assert(vm.SelectionSummary.Contains("2 cấu kiện")&&vm.SelectionSummary.Contains("Beam: 1")&&vm.SelectionSummary.Contains("Slab: 1"));vm.ClearSelection();Assert(vm.SelectionSummary=="Chưa chọn cấu kiện"); });
        Check("selection badge: removal and restore notify bound properties",()=> { var vm=WorkspaceFixture();var events=new HashSet<string>();vm.PropertyChanged+=(_,e)=>events.Add(e.PropertyName);vm.SelectElements(new[]{"Frame:B1"});vm.RemoveSelectedFromWorkspace();Assert(events.Contains(nameof(vm.SelectionSummary))&&events.Contains(nameof(vm.CanRestoreWorkspace))&&vm.SelectionSummary.Contains("1"));events.Clear();vm.RestoreWorkspace();Assert(events.Contains(nameof(vm.SelectionSummary))&&vm.SelectionSummary=="Chưa chọn cấu kiện"); });

        var view=new Box2(new Vec2(0,0),new Vec2(300,200));
        MemberLabelPlacement Place(Segment2 line)=>MemberLabelLayout.Place(new[]{line},Array.Empty<Vec2>(),false,60,14,view);
        Check("anchored labels: horizontal midpoint independent of I/J order",()=> { foreach(var line in new[]{new Segment2(new Vec2(20,100),new Vec2(280,100)),new Segment2(new Vec2(280,100),new Vec2(20,100))}) { var p=Place(line);Near(p.AngleDegrees,0);Near(p.Center.X,150);Near(p.Center.Y,89);Assert(!p.Intersects(line)); } });
        Check("anchored labels: vertical stays vertical with a rotated footprint",()=> { foreach(var line in new[]{new Segment2(new Vec2(150,10),new Vec2(150,190)),new Segment2(new Vec2(150,190),new Vec2(150,10))}) { var p=Place(line);Near(p.AngleDegrees,-90);Near(p.Bounds.Right-p.Bounds.Left,14);Near(p.Bounds.Top-p.Bounds.Bottom,60);Assert(!p.Intersects(line)); } });
        Check("anchored labels: diagonal follows member and reverse retains orientation",()=> { var a=new Vec2(50,40);var b=new Vec2(180,170);Near(Place(new Segment2(a,b)).AngleDegrees,45);Near(Place(new Segment2(b,a)).AngleDegrees,45);Near(Place(new Segment2(new Vec2(50,170),new Vec2(180,40))).AngleDegrees,-45); });
        Check("anchored labels: angle always stays upright",()=> { for(int i=0;i<360;i++) { double a=i*Math.PI/180;double angle=MemberLabelLayout.ReadableAngle(new Segment2(new Vec2(0,0),new Vec2(Math.Cos(a),Math.Sin(a))));Assert(angle>=-90.00001&&angle<90.00001); } });
        Check("anchored labels: offscreen midpoint is not pulled into view",()=> { Assert(Place(new Segment2(new Vec2(-2000,100),new Vec2(350,100)))==null); });
        Check("anchored labels: font and offset stay constant over zoom",()=> { foreach(double scale in new[]{.001,1,1000.0}) { var p=Place(new Segment2(new Vec2(150-100*scale,100),new Vec2(150+100*scale,100)));Assert(p!=null);Near(p.Width,60);Near(p.Height,14);Near(p.Center.X,150);Near(p.Center.Y,89); } });
        Check("anchored labels: pan and zoom follow model anchor without collision relocation",()=> {
            var largeView=new Box2(new Vec2(-10000,-10000),new Vec2(10000,10000));
            foreach(double zoom in new[]{.1,1,10.0})foreach(double pan in new[]{-200.0,0,200}) {
                var line=new Segment2(new Vec2(pan+zoom*10,zoom*30),new Vec2(pan+zoom*50,zoom*30));
                var p=MemberLabelLayout.Place(new[]{line},Array.Empty<Vec2>(),false,60,14,largeView);
                Near((p.Center.X-pan)/zoom,30);Near((p.Center.Y+11)/zoom,30);
            }
        });
        Check("anchored labels: extruded beam polygon never makes vertical text horizontal",()=> {
            var line=new Segment2(new Vec2(150,10),new Vec2(150,190));var polygon=new[]{new Vec2(145,10),new Vec2(155,10),new Vec2(155,190),new Vec2(145,190)};
            var p=MemberLabelLayout.Place(new[]{line},polygon,false,60,14,view);Near(p.AngleDegrees,-90);Near(p.Center.Y,100);
        });
        Check("anchored labels: short beams and collapsed points retain text",()=> { foreach(var line in new[]{new Segment2(new Vec2(150,100),new Vec2(150.01,100)),new Segment2(new Vec2(150,100),new Vec2(150,100))})Assert(Place(line)!=null); });
        Check("anchored labels: slab text stays at full polygon centroid",()=> { var polygon=new[]{new Vec2(-100,20),new Vec2(220,20),new Vec2(220,180),new Vec2(-100,180)};var p=MemberLabelLayout.Place(Array.Empty<Segment2>(),polygon,true,60,14,view);Near(p.Center.X,60);Near(p.Center.Y,100);Near(p.AngleDegrees,0); });
        Check("anchored labels: offscreen slab centroid is not replaced by visible centroid",()=> { var polygon=new[]{new Vec2(-2000,-2000),new Vec2(1000,-2000),new Vec2(1000,1000),new Vec2(-2000,1000)};Assert(MemberLabelLayout.Place(Array.Empty<Segment2>(),polygon,true,60,14,view)==null); });
        Check("anchored labels: partially visible text is clipped at real position",()=> { var p=Place(new Segment2(new Vec2(-50,100),new Vec2(60,100)));Near(p.Center.X,5);Assert(p.Bounds.Left<0); });
        Check("anchored labels: no geometry means no label",()=> { Assert(MemberLabelLayout.Place(Array.Empty<Segment2>(),Array.Empty<Vec2>(),true,60,14,view)==null); });

        Check("plan navigation: Page Down and Up follow elevation and refresh geometry",()=> {
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);
            Assert(vm.Level.Name=="Roof");Assert(vm.MovePlanLevel(-1));Near(vm.Level.Elevation,3);Assert(vm.Scene.Any(e=>e.Source.Id=="Frame:B1")&&!vm.Scene.Any(e=>e.Source.Id=="Frame:B2"));
            Assert(vm.MovePlanLevel(1));Near(vm.Level.Elevation,6);
        });
        Check("plan navigation: ends never wrap and preserve selection",()=> {
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.SelectElements(new[]{"Frame:B2"});
            Assert(!vm.MovePlanLevel(1)&&vm.SelectedIds.Count==1);Assert(vm.MovePlanLevel(-1)&&vm.SelectedIds.Count==0);Assert(vm.MovePlanLevel(-1));Assert(!vm.MovePlanLevel(-1));Near(vm.Level.Elevation,0);
        });
        Check("plan navigation: Busy, Elevation and disconnected states do not change floor",()=> {
            var vm=new VisualWorkspaceViewModel();Assert(!vm.MovePlanLevel(1));vm.SetSnapshot(DraftFixture(),true);vm.Busy=true;Assert(!vm.MovePlanLevel(-1));vm.Busy=false;vm.Elevation=true;Assert(!vm.MovePlanLevel(-1));Near(vm.Level.Elevation,6);
        });
        Check("plan navigation: unsorted stories and duplicate elevations use physical height",()=> {
            var f=DraftFixture();var vm=new VisualWorkspaceViewModel();
            vm.SetSnapshot(new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,new[]{new ModelLevel("Top",6),new ModelLevel("Middle",3),new ModelLevel("Duplicate",3),new ModelLevel("Bottom",0)},f.Loads,f.Diagnostics),true);
            Assert(vm.MovePlanLevel(1));Near(vm.Level.Elevation,3);Assert(vm.MovePlanLevel(1));Near(vm.Level.Elevation,6);Assert(vm.MovePlanLevel(-1));Near(vm.Level.Elevation,3);
        });
        Check("plan navigation: one story and zero direction are no-ops",()=> {
            var f=DraftFixture();var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,new[]{new ModelLevel("Only",3)},f.Loads,f.Diagnostics),true);
            Assert(!vm.MovePlanLevel(1)&&!vm.MovePlanLevel(-1)&&!vm.MovePlanLevel(0));Near(vm.Level.Elevation,3);
        });
        Check("inspector: beam identity dimensions coordinates and read load count",()=> {
            var vm=WorkspaceFixture();vm.SelectElements(new[]{"Frame:B1"});var rows=vm.InspectorRows.ToDictionary(r=>r.Key,r=>r.Value);
            Assert(vm.InspectorTitle=="B1"&&rows["Unique ID"]=="B1"&&rows["Property"]=="B200x400"&&rows["Chiều dài"]=="6 m"&&rows["Tải đã đọc"]=="1 bản ghi");Assert(vm.InspectorCoordinates.Count==2);
        });
        Check("inspector: slab omits irrelevant frame length and local angle",()=> {
            var vm=WorkspaceFixture();vm.SelectElements(new[]{"Area:S1"});Assert(!vm.InspectorRows.Any(r=>r.Key=="Chiều dài"||r.Key=="Góc local"));Assert(vm.InspectorCoordinates.Count==4);
        });
        Check("inspector: empty and multiple selection do not show stale single properties",()=> {
            var vm=WorkspaceFixture();Assert(vm.InspectorRows.Count==0&&vm.InspectorCoordinates.Count==0);vm.SelectElements(new[]{"Frame:B1","Area:S1"});
            var rows=vm.InspectorRows.ToDictionary(r=>r.Key,r=>r.Value);Assert(rows["Dầm"]=="1 cấu kiện"&&rows["Sàn"]=="1 cấu kiện"&&rows["Tải đã đọc"]=="2 bản ghi"&&vm.InspectorCoordinates.Count==0);vm.ClearSelection();Assert(vm.InspectorRows.Count==0);
        });
        Check("inspector: selection changes notify every displayed property",()=> {
            var vm=WorkspaceFixture();var events=new HashSet<string>();vm.PropertyChanged+=(_,e)=>events.Add(e.PropertyName);vm.SelectElements(new[]{"Frame:B1"});
            foreach(string name in new[]{nameof(vm.InspectorTitle),nameof(vm.InspectorSubtitle),nameof(vm.InspectorRows),nameof(vm.InspectorCoordinates),nameof(vm.SingleSelection)})Assert(events.Contains(name));
        });

        Check("auto read: disconnected viewer never starts a read",()=> { var state=new WorkspaceReadLifecycle();state.Request();Assert(!state.Pending&&!state.TryBegin(false,out _)); });
        Check("auto read: current API model automatically queues exactly one read",()=> { var state=new WorkspaceReadLifecycle();var a=new object();Assert(state.Update(a,"1|a.edb")&&state.Pending);Assert(state.TryBegin(false,out long ticket));Assert(state.Accept(ticket,a,"1|a.edb")&&!state.Pending&&!state.TryBegin(false,out _)); });
        Check("auto read: Excel/CAD-only changes do not reset model snapshot",()=> { var state=new WorkspaceReadLifecycle();var a=new object();state.Update(a,"1|a.edb");state.TryBegin(false,out long ticket);Assert(!state.Update(a,"1|a.edb")&&!state.Pending&&state.Accept(ticket,a,"1|a.edb")); });
        Check("auto read: changed model path invalidates previous completion",()=> { var state=new WorkspaceReadLifecycle();var a=new object();state.Update(a,"1|a.edb");state.TryBegin(false,out long old);state.Update(a,"1|b.edb");Assert(!state.Accept(old,a,"1|a.edb")&&state.Pending); });
        Check("auto read: changed API handle invalidates same-path read",()=> { var state=new WorkspaceReadLifecycle();var a=new object();var b=new object();state.Update(a,"1|a.edb");state.TryBegin(false,out long old);state.Update(b,"1|a.edb");Assert(!state.Accept(old,a,"1|a.edb")&&!state.Accept(old,b,"1|a.edb")&&state.Pending); });
        Check("auto read: global connection change is rejected before event dispatch",()=> { var state=new WorkspaceReadLifecycle();var a=new object();state.Update(a,"1|a.edb");state.TryBegin(false,out long ticket);Assert(!state.Accept(ticket,new object(),"2|b.edb")&&!state.Accept(ticket,a,"1|b.edb")); });
        Check("auto read: busy operation retains pending latest model",()=> { var state=new WorkspaceReadLifecycle();var a=new object();state.Update(a,"1|a.edb");Assert(!state.TryBegin(true,out _)&&state.Pending);Assert(state.TryBegin(false,out long ticket)&&state.Accept(ticket,a,"1|a.edb")); });
        Check("auto read: rapid model changes use latest source only",()=> { var state=new WorkspaceReadLifecycle();var a=new object();var b=new object();var c=new object();state.Update(a,"a");state.TryBegin(false,out long old);state.Update(b,"b");state.Update(c,"c");Assert(state.TryBegin(false,out long current)&&state.Accept(current,c,"c")&&!state.Accept(old,a,"a")); });
        Check("auto read: manual ComboBox selection waits for Read again",()=> { var state=new WorkspaceReadLifecycle();var a=new object();state.Update(a,"a",false);Assert(!state.Pending&&!state.TryBegin(false,out _));state.Request();Assert(state.TryBegin(false,out long ticket)&&state.Accept(ticket,a,"a")); });
        Check("auto read: cancel and disconnect do not requeue a read",()=> { var state=new WorkspaceReadLifecycle();var a=new object();state.Update(a,"a");state.CancelPending();Assert(!state.TryBegin(false,out _));state.Request();state.TryBegin(false,out long ticket);state.Update(null,"");Assert(!state.Pending&&!state.Accept(ticket,a,"a")); });
    }
}
