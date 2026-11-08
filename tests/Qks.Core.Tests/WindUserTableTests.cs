using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;

internal static partial class Program
{
    private static WindTableSnapshot UserTableFixture(bool extra=false)
    {
        var fields=new[]{"Name","LoadSet","Story","Diaph","Fx","Fy","Mz","XOrd","YOrd","NumSets","Reversible"};
        var units=new[]{"","","","","kN","kN","kN-m","m","m","",""};
        var rows=new[]{new[]{"WU","1","L1","D1","10","0","2","3","4","2","Yes"},new[]{"WU","2","L1","D1","-10","0","-2","3","4","2","Yes"},new[]{"OTHER","1","L1","D1","99","0","0","3","4","1","No"}};
        if(extra){fields=fields.Concat(new[]{"HostMetadata"}).ToArray();units=units.Concat(new[]{""}).ToArray();rows=rows.Select(r=>r.Concat(new[]{"preserve-me"}).ToArray()).ToArray();}
        return new WindTableSnapshot("Synthetic User Wind",1,fields,units,fields.Select(_=>true).ToArray(),rows,6,1,1);
    }
    private static IReadOnlyDictionary<string,string[]> UserPairs()=>new Dictionary<string,string[]>{{"L1",new[]{"D1","D2"}},{"L2",new[]{"D1"}}};
    private sealed class UserTableFake : IWindUserTableHost
    {
        public ApplyFake Inner=new ApplyFake();public ILoadApplyHost Model=>Inner;
        public WindTableSnapshot State=UserTableFixture();public int Writes;public bool CorruptReadback,ImportError,AssignmentsChanged,PatternInvalid,JournalFails;public int Warnings;
        public UserTableFake(){Inner.Source=WindWorkspaceFixture();Inner.Loads=Inner.Source.Loads.ToList();}
        public WindTableSnapshot Read(string key)=>State;
        public void ValidatePattern(string pattern){if(PatternInvalid)throw new InvalidOperationException("pattern changed");}
        public Task ValidateAssignments(WindTableSnapshot table,string pattern,CancellationToken token){if(AssignmentsChanged)throw new InvalidOperationException("diaphragm changed");return Task.CompletedTask;}
        public WindTableWriteResult Write(WindTableSnapshot table,bool replaceEditingSession){Assert(replaceEditingSession);Writes++;State=table;if(CorruptReadback){var rows=table.Rows.Select(r=>r.ToArray()).ToArray();rows.Last()[4]="123";State=new WindTableSnapshot(table.Key,table.Version,table.Fields,table.Units,table.Importable,rows,table.HostUnits,table.ForceToKn,table.LengthToM);}return new WindTableWriteResult{Errors=ImportError?1:0,Warnings=Warnings,Log=ImportError?"import error":""};}
        public void Journal(WindTableReport report){if(JournalFails)throw new InvalidOperationException("journal failed");}
        public WindTableChange Change(bool session=true){var rows=State.PatternRows("WU");rows[0].Fx="17";var after=State.ReplacePattern("WU",rows,2,true,UserPairs());return new WindTableChange(Inner.Source.Identity,SnapshotFingerprint.Of(Inner.Source),"WU",State,after,session);}
    }
    private static WindTableReport ApplyTable(UserTableFake host,WindTableChange change=null,CancellationToken token=default)=>WindTableApply.Execute(change??host.Change(),host,token).GetAwaiter().GetResult();
    private static void WindUserTableTests()
    {
        Check("user table: story and set disambiguate repeated D1",()=>{
            var table=UserTableFixture();var rows=table.PatternRows("WU").ToList();rows.Add(new WindSetRow{LoadSet=1,Story="L2",Diaphragm="D1",Fx="25"});var result=table.ReplacePattern("WU",rows,2,true,UserPairs());Assert(result.PatternRows("WU").Length==3);Assert(result.PatternRows("WU").Select(r=>r.Key).Distinct().Count()==3);
        });
        Check("user table: duplicates and unknown diaphragm are rejected",()=>{
            var table=UserTableFixture();var rows=table.PatternRows("WU");Throws<ArgumentException>(()=>table.ReplacePattern("WU",rows.Concat(new[]{rows[0]}),2,true,UserPairs()));rows[0].Diaphragm="D9";Throws<ArgumentException>(()=>table.ReplacePattern("WU",rows,2,true,UserPairs()));
        });
        Check("user table: cannot reduce set count over existing rows",()=>{
            var table=UserTableFixture();Throws<ArgumentException>(()=>table.ReplacePattern("WU",table.PatternRows("WU"),1,true,UserPairs()));
        });
        Check("user table: explicit removal allows count reduction and reversible change",()=>{
            var table=UserTableFixture();var after=table.ReplacePattern("WU",table.PatternRows("WU").Take(1),1,false,UserPairs());Assert(after.SetCount("WU")==1&&!after.ReversibleValue("WU"));Assert(after.Rows.Single(r=>r[0]=="OTHER").SequenceEqual(table.Rows.Single(r=>r[0]=="OTHER")));
        });
        Check("user table: unknown fields survive edits and prevent invented new rows",()=>{
            var table=UserTableFixture(true);var rows=table.PatternRows("WU");rows[0].Fx="15";var after=table.ReplacePattern("WU",rows,2,true,UserPairs());Assert(after.Rows.All(r=>r.Last()=="preserve-me"));Throws<ArgumentException>(()=>table.ReplacePattern("WU",rows.Concat(new[]{new WindSetRow{LoadSet=1,Story="L1",Diaphragm="D2"}}),2,true,UserPairs()));
        });
        Check("user table: ambiguous and incomplete schemas fail closed",()=>{
            Throws<ArgumentException>(()=>new WindTableSchema(new[]{"Name","Diaph","Fx"}));Throws<ArgumentException>(()=>new WindTableSchema(UserTableFixture().Fields.Concat(new[]{"LoadPattern"}).ToArray()));
        });
        Check("user table: physical units normalize and invert correctly",()=>{
            var t=UserTableFixture();var scaled=new WindTableSnapshot(t.Key,t.Version,t.Fields,t.Units,t.Importable,t.Rows,t.HostUnits,.001,.001);var rows=scaled.PatternRows("WU");Near(double.Parse(rows[0].Fx,System.Globalization.CultureInfo.InvariantCulture),.01);Near(double.Parse(rows[0].Mz,System.Globalization.CultureInfo.InvariantCulture),.000002);var after=scaled.ReplacePattern("WU",rows,2,true,UserPairs());Assert(WindTableSnapshot.Equivalent(scaled,after));
        });
        Check("user table: readback may reorder rows and numeric formatting",()=>{
            var t=UserTableFixture();var rows=t.Rows.Reverse().Select(r=>r.ToArray()).ToArray();rows[0][4]="99.000000";var after=new WindTableSnapshot(t.Key,t.Version,t.Fields,t.Units,t.Importable,rows,t.HostUnits,1,1);Assert(WindTableSnapshot.Equivalent(t,after));
        });
        Check("user table: changing unrelated pattern is blocked before backup",()=>{
            var h=new UserTableFake();var good=h.Change();var rows=good.After.Rows.Select(r=>r.ToArray()).ToArray();rows.Last()[4]="123";var bad=new WindTableSnapshot(good.After.Key,1,good.After.Fields,good.After.Units,good.After.Importable,rows,6,1,1);var change=new WindTableChange(good.Identity,good.ModelFingerprint,"WU",good.Before,bad,true);var r=ApplyTable(h,change);Assert(!r.MayHaveChanges&&h.Writes==0&&h.Inner.Backups==0);
        });
        Check("user table: successful apply requires backup and verifies all patterns",()=>{
            var h=new UserTableFake();var r=ApplyTable(h);Assert(r.Outcome=="Succeeded"&&h.Writes==1&&h.Inner.Backups==1);Near(double.Parse(h.State.PatternRows("WU")[0].Fx),17);
        });
        Check("user table: session consent is required before any mutation",()=>{
            var h=new UserTableFake();var r=ApplyTable(h,h.Change(false));Assert(!r.MayHaveChanges&&h.Writes==0&&h.Inner.Backups==0);
        });
        Check("user table: modified baseline is detected before backup",()=>{
            var h=new UserTableFake();var change=h.Change();h.State=change.After;var r=ApplyTable(h,change);Assert(!r.MayHaveChanges&&h.Inner.Backups==0);
        });
        Check("user table: assignment changes block write",()=>{
            var h=new UserTableFake{AssignmentsChanged=true};Assert(!ApplyTable(h).MayHaveChanges&&h.Writes==0);
        });
        Check("user table: import failure reports potential partial change",()=>{
            var h=new UserTableFake{ImportError=true};var r=ApplyTable(h);Assert(r.MayHaveChanges&&r.Outcome=="StoppedMayHaveChanges"&&r.Import.Errors==1);
        });
        Check("user table: altered unrelated readback is not success",()=>{
            var h=new UserTableFake{CorruptReadback=true};var r=ApplyTable(h);Assert(r.MayHaveChanges&&r.Outcome=="StoppedMayHaveChanges");
        });
        Check("user table: import warnings are retained after verified readback",()=>{
            var h=new UserTableFake{Warnings=1};var r=ApplyTable(h);Assert(r.Outcome=="VerifiedWithWarnings"&&r.Import.Warnings==1);
        });
        Check("user table: journal failure never reaches setter",()=>{
            var h=new UserTableFake{JournalFails=true};var r=ApplyTable(h);Assert(h.Writes==0&&!r.MayHaveChanges&&r.Outcome=="NeedsInspection");
        });
        Check("user table: cancel before write leaves table unchanged",()=>{
            var h=new UserTableFake();var c=new CancellationTokenSource();c.Cancel();Assert(!ApplyTable(h,null,c.Token).MayHaveChanges&&h.Writes==0);
        });
        Check("user table: changed pattern after backup is rechecked",()=>{
            var h=new UserTableFake();h.Inner.OnBackup=()=>h.PatternInvalid=true;Assert(!ApplyTable(h).MayHaveChanges&&h.Writes==0);
        });
        Check("user table: roundtrip document and all raw fields",()=>{
            var t=UserTableFixture(true);var restored=JsonCodec.Deserialize<WindTableSnapshot>(JsonCodec.Serialize(t));Assert(t.Fingerprint==restored.Fingerprint);
        });
        Check("user table: legacy import rejects load set columns instead of dropping them",()=>{
            var table=new[]{new[]{"Story","Fx","LoadSet"},new[]{"L1","20","2"}};Throws<ArgumentException>(()=>WindInputTable.Parse(table));Assert(WindInputTable.Parse(table,"WU",true).Single().UserLoadSet=="2");
        });
        Check("user table: Excel export retains multiple sets and reversible",()=>{
            string file=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"windsets-"+Guid.NewGuid().ToString("N")+".xlsx");try{
                var rows=UserTableFixture().PatternRows("WU").Select(r=>new StoryWindInput{Target="UserLoads",Story=r.Story,Pattern="WU",DiaphragmKey=r.Diaphragm,Fx=r.Fx,Fy=r.Fy,Mz=r.Mz,X=r.X,Y=r.Y,UserLoadSet=r.LoadSet.ToString(),UserSetCount="2",UserReversible="Yes"});WindReportWorkbook.Write(file,WindWorkspaceFixture(),rows,true);var read=WindWorkbook.Read(file,"WU",true);Assert(read.Count==2&&read.All(r=>r.UserSetCount=="2"&&r.UserReversible=="Yes")&&read.Select(r=>r.UserLoadSet).Distinct().Count()==2);Throws<ArgumentException>(()=>WindWorkbook.Read(file));
            }finally{if(System.IO.File.Exists(file))System.IO.File.Delete(file);}
        });
    }
}
