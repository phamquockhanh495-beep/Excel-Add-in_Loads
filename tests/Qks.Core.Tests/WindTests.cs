using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private sealed class WindFake : IUserWindHost
    {
        public readonly ApplyFake Inner = new ApplyFake();
        public ILoadApplyHost Model => Inner;
        public UserWindState State = WindFixture();
        public int Writes, Result;
        public bool DeleteOtherKeys, WrongReadback, JournalFails;
        public Action OnWrite;
        public WindFake()
        {
            var s=Inner.Source;Inner.Source=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,s.Loads,s.Diagnostics,s.LoadPatterns.Concat(new[]{State.Pattern}));
        }
        public UserWindState ReadUserWind(string pattern) => WrongReadback && Writes > 0 ? new UserWindState(State.Pattern,State.Code,State.Rows.Select(r=>new UserWindRow(r.DiaphragmKey,r.Fx+1,r.Fy,r.Mz,r.X,r.Y))) : State;
        public int SetUserWind(string pattern,UserWindRow row)
        {
            Assert(Inner.Canonical);Writes++;
            State=new UserWindState(State.Pattern,State.Code,DeleteOtherKeys?new[]{row}:State.Rows.Select(r=>r.DiaphragmKey==row.DiaphragmKey?row:r));
            OnWrite?.Invoke();return Result;
        }
        public void SaveUserWindJournal(UserWindReport report) { if(JournalFails)throw new Exception("journal failed"); }
    }
    private static UserWindState WindFixture() => new UserWindState("WX_USER","User Loads",new[]{new UserWindRow("SYNTHETIC-KEY-1",10,0,2,4,5),new UserWindRow("SYNTHETIC-KEY-2",20,0,3,4,5)});
    private static UserWindDocument WindDocument(WindFake h,bool both=false)
    {
        var rows=h.State.Rows.Take(both?2:1).Select(r=>new UserWindRow(r.DiaphragmKey,-15,r.Fy,r.Mz,r.X,r.Y));
        return new UserWindDocument(h.Inner.Source.Identity,SnapshotFingerprint.Of(h.Inner.Source),h.State,rows,"Synthetic test; no TCVN calculation");
    }
    private static UserWindReport ApplyWind(WindFake h,UserWindDocument d=null,CancellationToken token=default) => UserWindApply.ExecuteAsync(d??WindDocument(h),h,token).GetAwaiter().GetResult();
    private static void WindTests()
    {
        Check("wind: packaged line fixture matches original offline snapshot",()=>{var w=new DraftWorkspace(DraftFixture());w.Import(JsonCodec.Deserialize<DraftDocument>(JsonCodec.Read(System.IO.Path.Combine(ViewerTestRoot,"tests/fixtures/viewer-wind-line.qksdraft.json"))));Assert(w.Loads.Single().Kind==DraftKind.WindLine&&w.Loads.Single().Pattern=="WX");});
        Check("wind: packaged User Loads fixture opens offline with matching identity",()=>{var dir=System.IO.Path.Combine(ViewerTestRoot,"tests/fixtures");var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Read(System.IO.Path.Combine(dir,"viewer-wind-source.json"))),true);vm.ImportUserWindDocument(JsonCodec.Deserialize<UserWindDocument>(JsonCodec.Read(System.IO.Path.Combine(dir,"viewer-user-wind.qkswind.json"))));Assert(!vm.CanApplyUserWind&&vm.WindUserRows.Count==2&&vm.WindUserRows.Single(r=>r.DiaphragmKey=="SYNTHETIC-KEY-ROOF").Fx==25);});
        Check("wind: signed pressure interpolates without extrapolation",()=>{var p=new WindPressureBand(0,10,-1,3);Near(p.At(5),1);Throws<ArgumentOutOfRangeException>(()=>p.At(11));});
        Check("wind: pressure area integration returns force, not line load",()=>{Near(new WindPressureBand(0,10,1,3).StoryForce(2,4,5),16);});
        Check("wind: true 3D beam gets p times tributary height",()=>{var b=Frame(new Vec3(0,0,0),new Vec3(3,0,4));var d=new WindPressureBand(0,4,1,3).OnBeam(b,"WX",4,3,DraftAction.Add);Near(d.Value1,3);Near(d.Value2,9);Assert(d.Unit=="kN/m"&&d.Kind==DraftKind.WindLine&&d.AsLoad().Kind=="Frame force/length");});
        Check("wind: invalid bands, nonfinite input and zero tributary width rejected",()=>{Throws<ArgumentException>(()=>new WindPressureBand(3,3,1,2));Throws<ArgumentException>(()=>new WindPressureBand(0,3,double.NaN,2));Throws<ArgumentException>(()=>new WindPressureBand(0,3,1,2).StoryForce(0,2,0));});
        Check("wind: never assigns pressure to column or gravity direction",()=>{var s=DraftFixture();var p=new WindPressureBand(0,10,1,1);Throws<ArgumentException>(()=>p.OnBeam(s.Elements.First(e=>e.Kind==ElementKind.Column),"WX",4,3,DraftAction.Add));Throws<ArgumentException>(()=>p.OnBeam(s.Elements.First(e=>e.Kind==ElementKind.Beam),"WX",10,3,DraftAction.Add));});
        Check("wind: wall/wind drafts cannot overlap same physical direction",()=>{var w=new DraftWorkspace(DraftFixture());w.Put(Wall(w));Throws<ArgumentException>(()=>w.Put(new DraftLoad(Guid.NewGuid().ToString("N"),"Frame:B1",DraftKind.WindLine,"SDL",10,0,1,1,1,DraftAction.Add)));var a=new DraftLoad(Guid.NewGuid().ToString("N"),"Frame:B1",DraftKind.WallLine,"SDL",4,0,1,1,1,DraftAction.Add);var x=new DraftWorkspace(DraftFixture());x.Put(a);Throws<ArgumentException>(()=>x.Put(new DraftLoad(Guid.NewGuid().ToString("N"),a.ElementId,DraftKind.WindLine,a.Pattern,a.Direction,0,1,2,2,DraftAction.Add)));});
        Check("wind: wall/wind actions share one conflict scope",()=>{var w=new DraftWorkspace(DraftFixture());w.Put(new DraftLoad(Guid.NewGuid().ToString("N"),"Frame:B1",DraftKind.WallLine,"SDL",4,0,.5,1,1,DraftAction.Add));Throws<ArgumentException>(()=>w.Put(new DraftLoad(Guid.NewGuid().ToString("N"),"Frame:B1",DraftKind.WindLine,"SDL",4,.5,1,2,2,DraftAction.ReplaceMatching)));});
        Check("wind: load basis and new kind survive old schema draft JSON",()=>{var w=new DraftWorkspace(DraftFixture());w.Put(new DraftLoad(Guid.NewGuid().ToString("N"),"Frame:B1",DraftKind.WindLine,"SDL",4,0,1,-2,-3,DraftAction.Add,"reviewed source"));var copy=JsonCodec.Deserialize<DraftDocument>(JsonCodec.Serialize(w.Document()));var x=new DraftWorkspace(DraftFixture());x.Import(copy);Assert(x.Loads.Single().Basis=="reviewed source"&&x.Loads.Single().Kind==DraftKind.WindLine);});
        Check("wind: line overlay clips its actual segment in Elevation",()=>{var s=DraftFixture();var b=s.Elements.Single(e=>e.Id=="Frame:B1");var d=new DraftLoad(Guid.NewGuid().ToString("N"),b.Id,DraftKind.WindLine,"SDL",4,.2,.8,1,2,DraftAction.Add);var o=DraftOverlay.Build(d,b,ViewPlane.Elevation(0,0,0,.2));Assert(o!=null&&o.Text.Contains("kN/m")&&o.Text.Contains("I→J"));});
        Check("wind: transfer of reference point preserves resultant moment",()=>{var a=new UserWindRow("key",10,20,3,4,5);var b=a.TransferTo(0,0);Near(b.Mz,33);Near(b.TransferTo(4,5).Mz,3);});
        Check("wind: fingerprint ignores API row order but detects application point",()=>{var a=WindFixture();Assert(a.Fingerprint==new UserWindState(a.Pattern,a.Code,a.Rows.Reverse()).Fingerprint);var b=new UserWindState(a.Pattern,a.Code,a.Rows.Select(r=>new UserWindRow(r.DiaphragmKey,r.Fx,r.Fy,r.Mz,r.X+1,r.Y)));Assert(a.Fingerprint!=b.Fingerprint);});
        Check("wind: duplicate or nonfinite User Loads rejected",()=>{var r=WindFixture().Rows[0];Throws<ArgumentException>(()=>new UserWindState("WX","User Loads",new[]{r,r}));Throws<ArgumentException>(()=>new UserWindRow("key",double.NaN,0,0,0,0));});
        Check("wind: unknown key never guessed from story name",()=>{var h=new WindFake();var d=new UserWindDocument(h.Inner.Source.Identity,SnapshotFingerprint.Of(h.Inner.Source),h.State,new[]{new UserWindRow("GUESSED-STORY",1,0,0,0,0)});Throws<InvalidOperationException>(()=>d.Expected());Assert(ApplyWind(h,d).Outcome=="BlockedBeforeWrite"&&h.Inner.Backups==0);});
        Check("wind: cannot overwrite a code-based auto wind pattern",()=>{var h=new WindFake();h.State=new UserWindState("WX","ASCE 7-16",h.State.Rows);Assert(ApplyWind(h).Outcome=="BlockedBeforeWrite"&&h.Writes==0);});
        Check("wind: missing JSON force cannot silently become zero",()=>{var h=new WindFake();var j=JObject.Parse(JsonCodec.Serialize(WindDocument(h)));((JObject)j["Changes"][0]).Remove("Fx");Throws<JsonSerializationException>(()=>JsonCodec.Deserialize<UserWindDocument>(j.ToString()));});
        Check("wind: User Loads JSON round trip and future schema rejection",()=>{var h=new WindFake();var d=JsonCodec.Deserialize<UserWindDocument>(JsonCodec.Serialize(WindDocument(h)));Assert(d.Expected().Rows[0].Fx==-15);Throws<ArgumentException>(()=>new UserWindDocument(d.Identity,d.ModelFingerprint,d.Before,d.Changes,schemaVersion:2).Expected());});
        Check("wind apply: backups, preserves other keys and restores units",()=>{var h=new WindFake();var original=h.State.Rows[1];var r=ApplyWind(h);Assert(r.Outcome=="Succeeded"&&r.VerifiedKeys.Count==1&&h.State.Rows[1]==original&&h.Inner.Backups==1&&h.Inner.Restores==1);});
        Check("wind apply: stale pattern baseline rejected before backup",()=>{var h=new WindFake();var d=WindDocument(h);h.State=new UserWindState(h.State.Pattern,h.State.Code,h.State.Rows.Select(r=>new UserWindRow(r.DiaphragmKey,99,r.Fy,r.Mz,r.X,r.Y)));Assert(ApplyWind(h,d).Outcome=="BlockedBeforeWrite"&&h.Inner.Backups==0);});
        Check("wind apply: geometry/load revision mismatch rejected",()=>{var h=new WindFake();var d=WindDocument(h);h.Inner.Loads.Clear();Assert(ApplyWind(h,d).Outcome=="BlockedBeforeWrite"&&h.Inner.Backups==0);});
        Check("wind apply: absent capability and locked model cannot write",()=>{var h=new WindFake();h.Inner.Capabilities=ApplyCapabilities.ForSource("SAFE");Assert(ApplyWind(h).Outcome=="BlockedBeforeWrite"&&h.Writes==0);var h2=new WindFake();h2.Inner.IsLocked=true;Assert(ApplyWind(h2).Outcome=="BlockedBeforeWrite"&&h2.Writes==0);});
        Check("wind apply: backup failure leaves all keys unchanged",()=>{var h=new WindFake();h.Inner.BackupFails=true;var r=ApplyWind(h);Assert(!r.MayHaveChanges&&h.Writes==0);});
        Check("wind apply: direct member loads in same pattern cannot double wind",()=>{var h=new WindFake();var s=h.Inner.Source;var loads=s.Loads.Concat(new[]{new ExistingLoad("Frame:B1",h.State.Pattern,"Frame force/length","Global",4,0,1,1,1,"kN/m")});h.Inner.Source=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,loads,s.Diagnostics,s.LoadPatterns);h.Inner.Loads=h.Inner.Source.Loads.ToList();var r=ApplyWind(h);Assert(!r.MayHaveChanges&&h.Inner.Backups==0&&r.Error.Contains("pattern riêng"));});
        Check("wind apply: missing source pattern blocks before backup",()=>{var h=new WindFake();var s=h.Inner.Source;h.Inner.Source=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,s.Loads,s.Diagnostics,s.LoadPatterns.Where(p=>p!=h.State.Pattern));var r=ApplyWind(h);Assert(!r.MayHaveChanges&&h.Inner.Backups==0&&r.Error.Contains("Pattern"));});
        Check("wind apply: lost unrelated key is caught by full read-back",()=>{var h=new WindFake{DeleteOtherKeys=true};var r=ApplyWind(h);Assert(r.MayHaveChanges&&r.Outcome=="StoppedMayHaveChanges"&&r.VerifiedKeys.Count==0);});
        Check("wind apply: nonzero setter stops later rows",()=>{var h=new WindFake{Result=6};var r=ApplyWind(h,WindDocument(h,true));Assert(r.MayHaveChanges&&h.Writes==1&&r.Error.Contains("6"));});
        Check("wind apply: read-back mismatch stops after first row",()=>{var h=new WindFake{WrongReadback=true};var r=ApplyWind(h,WindDocument(h,true));Assert(r.Outcome=="StoppedMayHaveChanges"&&h.Writes==1);});
        Check("wind apply: failed journal prevents mutation",()=>{var h=new WindFake{JournalFails=true};Assert(ApplyWind(h).Outcome=="NeedsInspection"&&h.Writes==0);});
        Check("wind apply: cancel mid-batch records partial result",()=>{var h=new WindFake();var c=new CancellationTokenSource();h.OnWrite=()=>c.Cancel();var r=ApplyWind(h,WindDocument(h,true),c.Token);Assert(r.MayHaveChanges&&h.Writes==1&&h.Inner.Restores==1);});
        Check("wind apply: unit restore failure requires inspection",()=>{var h=new WindFake();h.Inner.RestoreFails=true;var r=ApplyWind(h);Assert(!r.UnitsRestored&&r.Outcome=="NeedsInspection");});
        Check("wind apply: zero-change draft does not back up or set",()=>{var h=new WindFake();var d=WindDocument(h);var r=ApplyWind(h,new UserWindDocument(d.Identity,d.ModelFingerprint,d.Before,Array.Empty<UserWindRow>()));Assert(!r.MayHaveChanges&&h.Inner.Backups==0&&h.Writes==0);});
        Check("wind VM: offline cannot Apply but can map and save drafts",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.SetUserWindBaseline(WindFixture());vm.WindUserRows[0].Fx=100;Assert(!vm.CanApplyUserWind&&vm.WindUserDirty&&vm.UserWindDocument().Changes.Single().Fx==100);vm.SetSnapshot(null,false);Assert(vm.UserWindDocument().Changes.Single().Fx==100&&!vm.CanApplyUserWind);});
        Check("wind VM: importing document preserves identity and desired forces",()=>{var vm=new VisualWorkspaceViewModel();var h=new WindFake();vm.SetSnapshot(h.Inner.Source,true);vm.ImportUserWindDocument(WindDocument(h));Assert(vm.WindUserRows[0].Fx==-15&&!vm.WindUserDirty);});
        Check("wind VM: maps beam draft and stores explicit source, not demo pressure",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.SelectedElement=vm.Snapshot.Elements.Single(e=>e.Id=="Frame:B1");vm.DraftModuleIndex=2;vm.DraftPattern="SDL";vm.WindBottom="0";vm.WindTop="10";vm.WindPressureBottom="1";vm.WindPressureTop="1";vm.WindTributaryHeight="3";Throws<ArgumentException>(vm.BuildWindOnBeam);vm.WindReference="reviewed fixture";vm.WindPeriod="1.2";vm.BuildWindOnBeam();Assert(vm.DraftRows.Single().Kind==DraftKind.WindLine&&vm.DraftRows[0].Basis.Contains("1.2"));Near(vm.DraftRows[0].Value1,3);vm.WindPressureBottom="2";vm.WindPressureTop="2";vm.BuildWindOnBeam();Assert(vm.DraftRows.Count==1);Near(vm.DraftRows[0].Value1,6);});
        Check("apply VM: only live ETABS context can apply and any write requires reread",()=>{var f=DraftFixture();var s=new ModelSnapshot(1,"ETABS",f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,f.Levels,f.Loads,f.Diagnostics,f.LoadPatterns);var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(s,true);vm.Drafts.Put(Slab());vm.NotifyDraftState();Assert(!vm.CanApply);vm.SetSnapshot(s,false);Assert(vm.CanApply);var h=new ApplyFake();var r=Apply(h);vm.SetApplyReport(r);Assert(!vm.CanApply&&vm.ApplySummary.Contains("Đọc lại"));});
    }
}
