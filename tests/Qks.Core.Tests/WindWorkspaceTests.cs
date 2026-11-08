using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static ModelSnapshot WindWorkspaceFixture()
    {
        var beam=new ModelElement("Frame:01","01","B1","L1","B300x600",ElementKind.Beam,new[]{new Vec3(0,2,3),new Vec3(10,2,3)});
        var other=new ModelElement("Frame:02","02","B2","L1","B300x600",ElementKind.Beam,new[]{new Vec3(0,4,3),new Vec3(10,4,3)});
        return new ModelSnapshot(1,"ETABS","test","wind-model","kN-m",DateTime.UtcNow,new[]{beam,other},new[]{new ModelLevel("L1",3)},new[]{new ExistingLoad(beam.Id,"WX","Frame force/length","Global",4,0,1,2,12,"kN/m")},Array.Empty<string>(),new[]{"WX","WY","WU"});
    }
    private static WindWorkspaceViewModel WindVm()
    {
        var vm=new WindWorkspaceViewModel();vm.SetSnapshot(WindWorkspaceFixture(),new[]{"WX","WY","WU"});vm.Viewer.SelectElements(new[]{"Frame:01"});vm.Q1="5";vm.Q2="";vm.BeamPattern="WX";vm.Tab=1;return vm;
    }
    private static void WindWorkspaceTests()
    {
        Check("wind workspace: stage metres maps exact partial segment",()=>{
            var vm=WindVm();vm.Relative=false;vm.Start="2";vm.End="6";vm.StageSelected();var r=vm.BeamRows.Single();Near(r.Start,.2);Near(r.End,.6);Near(r.Value1,5);Near(r.Value2,5);
        });
        Check("wind workspace: repeated staging updates without duplicating",()=>{
            var vm=WindVm();vm.StageSelected();string id=vm.BeamRows.Single().Id;vm.Q1="-2";vm.StageSelected();Assert(vm.BeamRows.Count==1&&vm.BeamRows[0].Id==id);Near(vm.BeamRows[0].Value1,-2);
        });
        Check("wind workspace: batch failure is atomic",()=>{
            var vm=WindVm();vm.StageSelected();string before=JsonCodec.Serialize(vm.Beams.Document());vm.Viewer.SelectElements(new[]{"Frame:01","Frame:02"});vm.End="2";Throws<ArgumentException>(vm.StageSelected);Assert(before==JsonCodec.Serialize(vm.Beams.Document()));
        });
        Check("wind workspace: mixed or absent targets rejected",()=>{
            var w=new DraftWorkspace(WindWorkspaceFixture());Throws<ArgumentException>(()=>WindBeamEditing.Stage(w,new[]{"Frame:01","missing"},"WX","Global-X",1,1,0,1,true,DraftAction.Add));Throws<ArgumentException>(()=>WindBeamEditing.Stage(w,Array.Empty<string>(),"WX","Global-X",1,1,0,1,true,DraftAction.Add));
        });
        Check("wind workspace: blank and nonfinite q never become zero",()=>{
            var vm=WindVm();vm.Q1="";Throws<ArgumentException>(vm.StageSelected);vm.Q1="NaN";Throws<ArgumentException>(vm.StageSelected);Assert(vm.BeamRows.Count==0);
        });
        Check("wind workspace: replacement preserves outside linear load",()=>{
            var vm=WindVm();vm.Start="0.2";vm.End="0.6";vm.StageSelected();var after=vm.Beams.Compare().Where(r=>r.Phase=="Sau").Select(r=>r.Load).OrderBy(l=>l.Start).ToArray();Assert(after.Length==3);Near(after[0].Start,0);Near(after[0].End,.2);Near(after[0].Value1,2);Near(after[0].Value2,4);Near(after[1].Value1,5);Near(after[2].Start,.6);Near(after[2].Value1,8);Near(after[2].Value2,12);
        });
        Check("wind workspace: separate replacement intervals keep middle",()=>{
            var vm=WindVm();vm.Start="0.1";vm.End="0.2";vm.StageSelected();vm.Start="0.7";vm.End="0.9";vm.StageSelected();var after=vm.Beams.Compare().Where(r=>r.Phase=="Sau").Select(r=>r.Load).ToArray();Assert(after.Length==5);Assert(after.Any(l=>Math.Abs(l.Start-.2)<1e-9&&Math.Abs(l.End-.7)<1e-9));
        });
        Check("wind workspace: append retains existing loads",()=>{
            var vm=WindVm();vm.Append=true;vm.Start="0.2";vm.End="0.6";vm.StageSelected();var after=vm.Beams.Compare().Where(r=>r.Phase=="Sau").Select(r=>r.Load).ToArray();Assert(after.Length==2&&after.Any(l=>l.Start==0&&l.End==1&&l.Value1==2));
        });
        Check("wind workspace: resultant uses real segment length and signed moment",()=>{
            var vm=WindVm();vm.Direction="Global-X";vm.Q1="2";vm.Q2="4";vm.StageSelected();var r=WindBeamEditing.Resultant(vm.Snapshot,vm.BeamRows);Near(r.Fx,30);Near(r.Fy,0);Near(r.Mz,-60);
        });
        Check("wind workspace: zero resultant can retain nonzero moment",()=>{
            var vm=WindVm();vm.Direction="Global-Y";vm.Q1="-3";vm.Q2="3";vm.StageSelected();var r=WindBeamEditing.Resultant(vm.Snapshot,vm.BeamRows);Near(r.Fy,0);Near(r.Mz,50);
        });
        Check("wind workspace: undo restores stage; redo restores values",()=>{
            var vm=WindVm();vm.StageSelected();vm.Q1="8";vm.StageSelected();vm.Beams.Undo();vm.NotifyBeams();Near(vm.BeamRows.Single().Value1,5);vm.Beams.Redo();vm.NotifyBeams();Near(vm.BeamRows.Single().Value1,8);
        });
        Check("wind workspace: review invalidates on staged edit and tab change",()=>{
            var vm=WindVm();vm.StageSelected();vm.AcceptReview();Assert(vm.CanApply);vm.Tab=0;Assert(!vm.CanApply);vm.Tab=1;vm.AcceptReview();vm.Q1="9";vm.StageSelected();Assert(!vm.CanApply);
        });
        Check("wind workspace: stale source cannot write reviewed or stage",()=>{
            var vm=WindVm();vm.StageSelected();vm.AcceptReview();vm.MarkStale();Assert(!vm.CanApply&&!vm.CanEdit);Throws<InvalidOperationException>(vm.AssertReviewed);Throws<InvalidOperationException>(vm.StageSelected);
        });
        Check("wind workspace: document round trip retains exact source",()=>{
            var vm=WindVm();vm.StageSelected();var copy=JsonCodec.Deserialize<WindProject>(JsonCodec.Serialize(vm.Project()));var target=WindVm();target.Restore(copy);Near(target.BeamRows.Single().Value1,5);Assert(!target.CanApply);copy.Identity="other";Throws<ArgumentException>(()=>target.Restore(copy));
        });
        Check("wind workspace: import wrong method cannot mutate tab",()=>{
            var vm=WindVm();Throws<ArgumentException>(()=>vm.ImportRows(new[]{new StoryWindInput{Target="UserLoads",Story="L1",Pattern="WU",DiaphragmKey="D1",Fx="1"}}));Assert(vm.BeamRows.Count==0);
        });
        Check("wind workspace: user identity remains opaque and mapping explicit",()=>{
            var vm=WindVm();vm.Tab=0;vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("host-key",1,0,0,0,0)}));vm.UserRows[0].Fx="3";Throws<InvalidOperationException>(()=>vm.UserDocument());vm.MappingConfirmed=true;Throws<InvalidOperationException>(()=>vm.UserDocument());vm.UserRows[0].Story="L1";vm.MappingConfirmed=true;var doc=vm.UserDocument();Assert(doc.Changes.Single().DiaphragmKey=="host-key");Near(doc.Changes[0].Fx,3);
        });
        Check("wind workspace: user edits invalidate review and X change preserves Mz",()=>{
            var vm=WindVm();vm.Tab=0;vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("key",1,0,8,0,0)}));vm.UserRows[0].Story="L1";vm.MappingConfirmed=true;vm.UserRows[0].Fx="3";vm.AcceptReview();Assert(vm.CanApply);vm.UserRows[0].X="2";Assert(!vm.CanApply);Near(vm.UserDocument().Changes[0].Mz,8);
        });
        Check("wind workspace: pattern switch requires new user baseline",()=>{
            var vm=WindVm();vm.Tab=0;vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("key",1,0,0,0,0)}));vm.UserPattern="WX";vm.MappingConfirmed=true;Throws<InvalidOperationException>(()=>vm.UserDocument());
        });
        Check("wind workspace: two methods retain separate edit state",()=>{
            var vm=WindVm();vm.StageSelected();vm.Tab=0;vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("key",1,0,0,0,0)}));vm.UserRows[0].Fx="7";vm.Tab=1;Assert(vm.BeamRows.Count==1&&vm.UserRows[0].Fx=="7"&&vm.BeamPattern=="WX");
        });
        Check("wind workspace: busy blocks apply without erasing review",()=>{
            var vm=WindVm();vm.StageSelected();vm.AcceptReview();vm.Busy=true;Assert(!vm.CanApply);vm.Busy=false;Assert(vm.CanApply);
        });

        Check("wind workspace: partial segments survive XLSX round trip",()=>{
            string file=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"wind-"+Guid.NewGuid().ToString("N")+".xlsx");
            try{var input=new StoryWindInput{Story="L1",Pattern="WX",UniqueName="01",Q1="-2.5",Q2="3",R1="0.2",R2="0.8"};WindReportWorkbook.Write(file,WindWorkspaceFixture(),new[]{input},false);var read=WindWorkbook.Read(file).Single();Assert(read.UniqueName=="01"&&read.R1=="0.2"&&read.R2=="0.8");var vm=WindVm();vm.ImportRows(new[]{read});Near(vm.BeamRows.Single().Start,.2);Near(vm.BeamRows.Single().End,.8);Near(vm.BeamRows.Single().Value1,-2.5);}
            finally{if(System.IO.File.Exists(file))System.IO.File.Delete(file);}
        });
        Check("wind workspace: user export retains coordinates and text key",()=>{
            string file=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"wind-"+Guid.NewGuid().ToString("N")+".xlsx");
            try{var input=new StoryWindInput{Target="UserLoads",Story="L1",Pattern="WU",DiaphragmKey="001",Fx="0",Fy="-8",Mz="2.5",X="4",Y="-7"};WindReportWorkbook.Write(file,WindWorkspaceFixture(),new[]{input},true);var read=WindWorkbook.Read(file).Single();Assert(read.DiaphragmKey=="001"&&read.Y=="-7"&&read.Fx=="0"&&read.Fy=="-8");}
            finally{if(System.IO.File.Exists(file))System.IO.File.Delete(file);}
        });
        Check("wind workspace: multiple imported segments on same beam remain separate",()=>{
            var vm=WindVm();vm.ImportRows(new[]{new StoryWindInput{Story="L1",Pattern="WX",UniqueName="01",Q1="1",R1="0",R2="0.2"},new StoryWindInput{Story="L1",Pattern="WX",UniqueName="01",Q1="2",R1="0.8",R2="1"}});Assert(vm.BeamRows.Count==2);
        });
        Check("wind workspace: inline errors distinguish blank from zero",()=>{
            var row=new WindUserEditorRow(new UserWindRow("k",0,0,0,0,0));Assert(row["Fx"]=="");row.Fx="";Assert(row["Fx"].Length>0);row.Fx="0";Assert(row["Fx"]=="");
        });
        Check("wind workspace: numerical overflow blocks staging atomically",()=>{
            var vm=WindVm();vm.Q1="1e308";Throws<ArgumentException>(vm.StageSelected);Assert(vm.BeamRows.Count==0);
        });

        Check("wind workspace: changing an edited segment replaces its prior extent",()=>{
            var vm=WindVm();vm.StageSelected();vm.EditBeam(vm.BeamRows.Single());vm.Start="0.2";vm.End="0.7";vm.StageSelected();Assert(vm.BeamRows.Count==1);Near(vm.BeamRows[0].Start,.2);Near(vm.BeamRows[0].End,.7);
        });
        Check("wind workspace: invalid edited segment keeps prior draft",()=>{
            var vm=WindVm();vm.StageSelected();var old=JsonCodec.Serialize(vm.Beams.Document());vm.EditBeam(vm.BeamRows.Single());vm.End="-1";Throws<ArgumentException>(vm.StageSelected);Assert(old==JsonCodec.Serialize(vm.Beams.Document()));
        });
        Check("wind workspace: user undo redo preserves all numeric fields",()=>{
            var vm=WindVm();vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("k",1,2,3,4,5)}));vm.UserRows[0].Fx="8";vm.UndoUsers();Assert(vm.UserRows[0].Fx=="1"&&vm.UserRows[0].Mz=="3");vm.RedoUsers();Assert(vm.UserRows[0].Fx=="8");
        });
        Check("wind workspace: saved user rows retain their original pattern",()=>{
            var vm=WindVm();vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("k",1,2,3,4,5)}));vm.UserPattern="WY";Assert(vm.Project().UserRows.Single().Pattern=="WU");
        });
        Check("wind workspace: incomplete user edit can be saved but cannot be applied",()=>{
            var vm=WindVm();vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("k",1,2,3,4,5)}));vm.UserRows[0].Fx="";var copy=JsonCodec.Deserialize<WindProject>(JsonCodec.Serialize(vm.Project()));var target=WindVm();target.Restore(copy);Assert(target.UserRows[0].Fx==""&&!target.MappingConfirmed);target.UserRows[0].Story="L1";target.MappingConfirmed=true;Throws<ArgumentException>(()=>target.UserDocument());
        });
        Check("wind workspace: model reread clears user undo history",()=>{
            var vm=WindVm();vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("k",1,2,3,4,5)}));vm.UserRows[0].Fx="8";vm.SetSnapshot(WindWorkspaceFixture(),new[]{"WU","WX"});vm.UndoUsers();Assert(vm.UserRows.Count==0);
        });
    }
}
