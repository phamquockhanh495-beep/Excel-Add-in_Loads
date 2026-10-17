using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QksExcelAddIn.Modeling.Core;

internal static partial class Program
{
    private sealed class ApplyFake : ILoadApplyHost
    {
        public ModelSnapshot Source = DraftFixture();
        public ApplyCapabilities Capabilities { get; set; } = ApplyCapabilities.ForSource("ETABS");
        public bool IsLocked { get; set; }
        public bool BackupFails, JournalFails, RestoreFails, ReadbackWrong, ConnectionFails, UnitsFail, DocumentInvalid;
        public int DeleteResult, AppendResult, Writes, Backups, Journals, Restores;
        public bool MutateBeforeError, Canonical;
        public List<ExistingLoad> Loads;
        public Action OnAppend, OnBackup;
        public ApplyFake() { Loads = Source.Loads.ToList(); }
        public void Guard() { if (ConnectionFails) throw new InvalidOperationException("connection changed"); }
        public void ValidateDocument(DraftDocument document) { if(DocumentInvalid)throw new InvalidOperationException("pattern invalid"); }
        public void GuardElement(ModelElement expected)
        {
            var current=Source.Elements.Single(e=>e.Id==expected.Id);
            if(current.Kind!=expected.Kind||current.Vertices.Count!=expected.Vertices.Count||current.Vertices.Where((v,i)=>(v-expected.Vertices[i]).Length>1e-6).Any())throw new InvalidOperationException("geometry changed");
        }
        public Task<ModelSnapshot> ReadSnapshotAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); var s = Source;
            return Task.FromResult(new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,Loads,s.Diagnostics,s.LoadPatterns));
        }
        public string CreateVerifiedBackup() { Backups++; if (BackupFails) throw new Exception("backup failed"); OnBackup?.Invoke(); return "verified-backup.edb"; }
        public IDisposable UseCanonicalUnits() { if (UnitsFail) throw new Exception("unit failure"); Canonical = true; return new TestScope(() => { Canonical = false; Restores++; if (RestoreFails) throw new Exception("restore failed"); }); }
        public int DeletePattern(ModelElement element,string pattern) { Assert(Canonical); Writes++; if (DeleteResult == 0 || MutateBeforeError) Loads.RemoveAll(l=>l.ElementId==element.Id&&l.Pattern==pattern); return DeleteResult; }
        public int Append(ModelElement element,ExistingLoad load) { Assert(Canonical); Writes++; if (AppendResult == 0 || MutateBeforeError) Loads.Add(load); OnAppend?.Invoke(); return AppendResult; }
        public IReadOnlyList<ExistingLoad> ReadLoads(ModelElement element) => Loads.Where(l=>l.ElementId==element.Id).Select(l=>ReadbackWrong&&Writes>0?new ExistingLoad(l.ElementId,l.Pattern,l.Kind,l.CoordinateSystem,l.Direction,l.Start,l.End,l.Value1+1,l.Value2+1,l.Unit):l).ToArray();
        public void SaveJournal(ApplyReport report) { Journals++; if (JournalFails) throw new Exception("journal failed"); }
        private sealed class TestScope : IDisposable { private readonly Action action; public TestScope(Action action){this.action=action;} public void Dispose()=>action(); }
    }
    private static DraftDocument ApplyDocument(ApplyFake h, DraftAction action=DraftAction.Add, bool slab=false)
    {
        var w=new DraftWorkspace(h.Source); w.Put(Wall(w,0,3,action)); if(slab)w.Put(Slab()); return w.Document();
    }
    private static ApplyReport Apply(ApplyFake host, DraftDocument document=null,CancellationToken token=default) => LoadApply.ExecuteAsync(document??ApplyDocument(host),host,token).GetAwaiter().GetResult();
    private static void ApplyTests()
    {
        Check("apply: Add backs up once, verifies read-back, restores units",()=>{var h=new ApplyFake();var r=Apply(h);Assert(r.Outcome=="Succeeded"&&h.Backups==1&&h.Writes==1&&h.Restores==1&&!h.Canonical&&r.Groups.Single().Status=="Verified");});
        Check("apply: Replace preserves other directions, moments and patterns",()=>{
            var h=new ApplyFake();var more=new[]{new ExistingLoad("Frame:B1","SDL","Frame force/length","Local",2,0,1,-2,-2,"kN/m"),new ExistingLoad("Frame:B1","SDL","Frame moment/length","Global",6,0,1,5,6,"kN·m/m"),new ExistingLoad("Frame:B1","LL","Frame force/length","Global",10,0,1,3,3,"kN/m")};
            var s=h.Source;h.Source=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,s.Loads.Concat(more),s.Diagnostics,s.LoadPatterns);h.Loads=h.Source.Loads.ToList();
            var r=Apply(h,ApplyDocument(h,DraftAction.ReplaceMatching));Assert(r.Outcome=="Succeeded"&&h.Loads.Contains(more[0])&&h.Loads.Contains(more[1])&&h.Loads.Contains(more[2])&&h.Writes==4);
        });
        Check("apply: nonzero delete stops even if CSI already changed model",()=>{var h=new ApplyFake{DeleteResult=7,MutateBeforeError=true};var r=Apply(h,ApplyDocument(h,DraftAction.ReplaceMatching));Assert(r.MayHaveChanges&&r.Outcome=="StoppedMayHaveChanges"&&h.Writes==1&&h.Restores==1&&r.Groups.Single().Status=="Failed");});
        Check("apply: nonzero setter stops later groups",()=>{var h=new ApplyFake{AppendResult=8};var r=Apply(h,ApplyDocument(h,slab:true));Assert(r.MayHaveChanges&&h.Writes==1&&r.Groups.Count(g=>g.Status=="NotStarted")==1&&r.Error.Contains("8"));});
        Check("apply: read-back mismatch is not success",()=>{var h=new ApplyFake{ReadbackWrong=true};var r=Apply(h);Assert(r.Outcome=="StoppedMayHaveChanges"&&r.Error.Contains("Read-back"));});
        Check("apply: locked model never saves or writes",()=>{var h=new ApplyFake{IsLocked=true};var r=Apply(h);Assert(!r.MayHaveChanges&&h.Backups==0&&h.Writes==0);});
        Check("apply: missing backup blocks all writes",()=>{var h=new ApplyFake{BackupFails=true};var r=Apply(h);Assert(r.Outcome=="BlockedBeforeWrite"&&h.Writes==0);});
        Check("apply: failed initial journal blocks all writes",()=>{var h=new ApplyFake{JournalFails=true};var r=Apply(h);Assert(h.Writes==0&&r.Outcome=="NeedsInspection");});
        Check("apply: failed unit setup blocks all writes and requests unit inspection",()=>{var h=new ApplyFake{UnitsFail=true};var r=Apply(h);Assert(r.Outcome=="NeedsInspection"&&!r.UnitsRestored&&h.Writes==0);});
        Check("apply: failed unit restoration reported separately",()=>{var h=new ApplyFake{RestoreFails=true};var r=Apply(h);Assert(!r.UnitsRestored&&r.Outcome=="NeedsInspection"&&r.Groups.Single().Status=="Verified");});
        Check("apply: changed load baseline blocks before backup",()=>{var h=new ApplyFake();var doc=ApplyDocument(h);h.Loads.RemoveAt(0);var r=Apply(h,doc);Assert(h.Backups==0&&h.Writes==0&&r.Outcome=="BlockedBeforeWrite");});
        Check("apply: changed connection blocks before backup",()=>{var h=new ApplyFake{ConnectionFails=true};Assert(Apply(h).Outcome=="BlockedBeforeWrite"&&h.Backups==0);});
        Check("apply: SAFE is explicitly unsupported without verified capability",()=>{var h=new ApplyFake{Capabilities=ApplyCapabilities.ForSource("SAFE")};Assert(Apply(h).Outcome=="BlockedBeforeWrite"&&h.Backups==0);});
        Check("apply: area-only capability blocks beam draft",()=>{var h=new ApplyFake{Capabilities=new ApplyCapabilities(false,true,"area only")};Assert(Apply(h).Error.Contains("area only")&&h.Writes==0);});
        Check("apply: empty draft blocks before backup",()=>{var h=new ApplyFake();Assert(Apply(h,new DraftWorkspace(h.Source).Document()).Outcome=="BlockedBeforeWrite"&&h.Backups==0);});
        Check("apply: host-specific pattern validation precedes backup",()=>{var h=new ApplyFake{DocumentInvalid=true};Assert(Apply(h).Outcome=="BlockedBeforeWrite"&&h.Backups==0&&h.Writes==0);});
        Check("apply: cancel before write does not create backup",()=>{var h=new ApplyFake();var c=new CancellationTokenSource();c.Cancel();var r=Apply(h,token:c.Token);Assert(!r.MayHaveChanges&&h.Backups==0);});
        Check("apply: cancel after first setter stops later group",()=>{var h=new ApplyFake();var c=new CancellationTokenSource();h.OnAppend=()=>c.Cancel();var r=Apply(h,ApplyDocument(h,slab:true),c.Token);Assert(r.MayHaveChanges&&h.Writes==1&&h.Restores==1);});
        Check("apply: source mutation during backup detected before setter",()=>{var h=new ApplyFake();h.OnBackup=()=>h.Loads.Clear();var r=Apply(h);Assert(h.Writes==0&&r.Outcome=="BlockedBeforeWrite");});
        Check("apply: geometry mutation during backup detected before setter",()=>{var h=new ApplyFake();h.OnBackup=()=>{var s=h.Source;var changed=s.Elements.Select(e=>e.Id=="Frame:B1"?new ModelElement(e.Id,e.Name,e.Label,e.Story,e.Property,e.Kind,new[]{new Vec3(9,0,3),e.Vertices[1]}):e);h.Source=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,changed,s.Levels,s.Loads,s.Diagnostics,s.LoadPatterns);};var r=Apply(h);Assert(h.Writes==0&&r.Outcome=="BlockedBeforeWrite"&&r.Error.Contains("geometry"));});
        Check("apply: second Add attempt cannot replay stale baseline",()=>{var h=new ApplyFake();var doc=ApplyDocument(h);Assert(Apply(h,doc).Outcome=="Succeeded");int n=h.Writes;Assert(Apply(h,doc).Outcome=="BlockedBeforeWrite"&&h.Writes==n);});
        Check("apply: slab Replace keeps another direction",()=>{var h=new ApplyFake();var other=new ExistingLoad("Area:S1","LL","Area uniform","Global",4,0,1,-2,-2,"kN/m²");var s=h.Source;h.Source=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,s.Loads.Concat(new[]{other}),s.Diagnostics,s.LoadPatterns);h.Loads=h.Source.Loads.ToList();var w=new DraftWorkspace(h.Source);w.Put(new DraftLoad(Guid.NewGuid().ToString("N"),"Area:S1",DraftKind.SlabUniform,"LL",10,0,1,5,5,DraftAction.ReplaceMatching));var r=Apply(h,w.Document());Assert(r.Outcome=="Succeeded"&&h.Loads.Contains(other)&&h.Loads.Count(l=>l.ElementId=="Area:S1"&&l.Direction==10)==1);});
        Check("apply: wind line uses the same typed force-per-length writer plan",()=>{var h=new ApplyFake();var w=new DraftWorkspace(h.Source);w.Put(new DraftLoad(Guid.NewGuid().ToString("N"),"Frame:B1",DraftKind.WindLine,"SDL",4,0,1,-2,-3,DraftAction.Add,"synthetic"));var r=Apply(h,w.Document());Assert(r.Outcome=="Succeeded"&&h.Loads.Any(l=>l.ElementId=="Frame:B1"&&l.Direction==4&&l.Unit=="kN/m"&&l.Value1==-2));});
        Check("apply: journal failure after write retains uncertainty",()=>{var h=new ApplyFake();h.OnAppend=()=>h.JournalFails=true;var r=Apply(h);Assert(r.MayHaveChanges&&r.Outcome=="NeedsInspection"&&h.Writes==1);});
        Check("read-back: area aggregation and signed zero are equivalent",()=>{var l=Slab().AsLoad();var both=new[]{l,l};var sum=new ExistingLoad(l.ElementId,l.Pattern,l.Kind,l.CoordinateSystem,l.Direction,0,1,6,6,l.Unit);Assert(LoadEquivalence.Same(both,new[]{sum}));});
        Check("read-back: adjacent linear segments equal unsplit distribution",()=>{var a=new ExistingLoad("Frame:B1","SDL","Frame force/length","Global",4,0,1,-2,2,"kN/m");var split=new[]{new ExistingLoad(a.ElementId,a.Pattern,a.Kind,a.CoordinateSystem,a.Direction,0,.5,-2,0,a.Unit),new ExistingLoad(a.ElementId,a.Pattern,a.Kind,"GLOBAL",a.Direction,.5,1,0,2,a.Unit)};Assert(LoadEquivalence.Same(new[]{a},split));});
        Check("read-back: wrong direction or unit cannot pass",()=>{var l=Slab().AsLoad();Assert(!LoadEquivalence.Same(new[]{l},new[]{new ExistingLoad(l.ElementId,l.Pattern,l.Kind,l.CoordinateSystem,4,0,1,l.Value1,l.Value2,l.Unit)}));Assert(!LoadEquivalence.Same(new[]{l},new[]{new ExistingLoad(l.ElementId,l.Pattern,l.Kind,l.CoordinateSystem,l.Direction,0,1,l.Value1,l.Value2,"kN/m")}));});
        Check("read-back: equal resultant with different distribution rejected",()=>{var l=Wall(new DraftWorkspace(DraftFixture())).AsLoad();var bad=new ExistingLoad(l.ElementId,l.Pattern,l.Kind,l.CoordinateSystem,l.Direction,l.Start,l.End,l.Value2,l.Value1,l.Unit);Assert(!LoadEquivalence.Same(new[]{l},new[]{bad}));});
    }
}
