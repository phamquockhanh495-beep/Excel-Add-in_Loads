using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static ModelSnapshot CatalogOf(ModelSnapshot s)=>new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,
        Array.Empty<ModelElement>(),s.Levels,Array.Empty<ExistingLoad>(),Array.Empty<string>(),s.LoadPatterns,catalogOnly:true);
    private static ModelSnapshot WithCoverage(ModelSnapshot s,bool complete)=>new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,s.Levels,s.Loads,s.Diagnostics,s.LoadPatterns,s.GridLines,loadBaselineComplete:complete);
    private static void ModelReadPerformanceTests()
    {
        Check("cache: repeated open reuses same immutable snapshot",()=>{
            var cache=new ModelReadCache();var session=new object();var s=WindWorkspaceFixture();int calls=0;
            Func<Task<ModelSnapshot>> read=()=>{calls++;return Task.FromResult(s);};
            var a=cache.ReadAsync(session,"A",ModelReadScope.Viewer,read,default).GetAwaiter().GetResult();
            var b=cache.ReadAsync(session,"A",ModelReadScope.Viewer,read,default).GetAwaiter().GetResult();
            Assert(!a.Reused&&b.Reused&&calls==1&&ReferenceEquals(a.Snapshot,b.Snapshot));
        });
        Check("cache: complete viewer read supplies wind beam baseline",()=>{
            var cache=new ModelReadCache();var session=new object();var s=WindWorkspaceFixture();
            cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>Task.FromResult(s),default).GetAwaiter().GetResult();
            var b=cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>throw new Exception("must reuse"),default).GetAwaiter().GetResult();Assert(b.Reused);
        });
        Check("cache: missing load coverage cannot supply writer baseline",()=>{
            var cache=new ModelReadCache();var session=new object();var s=WindWorkspaceFixture();int calls=0;
            cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>Task.FromResult(WithCoverage(s,false)),default).GetAwaiter().GetResult();
            cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>{calls++;return Task.FromResult(s);},default).GetAwaiter().GetResult();Assert(calls==1);
        });
        Check("cache: rejected verified read is not retained",()=>{
            var cache=new ModelReadCache();var session=new object();var s=WindWorkspaceFixture();
            Throws<InvalidOperationException>(()=>cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>Task.FromResult(WithCoverage(s,false)),default).GetAwaiter().GetResult());
            var r=cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>Task.FromResult(s),default).GetAwaiter().GetResult();Assert(!r.Reused&&r.Snapshot.LoadBaselineComplete);
        });
        Check("cache: catalog never masquerades as zero-load full model",()=>{
            var cache=new ModelReadCache();var session=new object();var s=WindWorkspaceFixture();int fullReads=0;
            var c=cache.ReadAsync(session,"A",ModelReadScope.Catalog,()=>Task.FromResult(CatalogOf(s)),default).GetAwaiter().GetResult();
            Assert(c.Snapshot.CatalogOnly&&!c.Snapshot.LoadBaselineComplete);
            cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>{fullReads++;return Task.FromResult(s);},default).GetAwaiter().GetResult();Assert(fullReads==1);
        });
        Check("cache: source handle, units and identity each isolate data",()=>{
            var cache=new ModelReadCache();var first=new object();var next=new object();int calls=0;
            foreach(var pair in new[]{(first,"model1|m"),(next,"model1|m"),(next,"model1|mm"),(next,"model2|mm")})
                cache.ReadAsync(pair.Item1,pair.Item2,ModelReadScope.Viewer,()=>{calls++;return Task.FromResult(WindWorkspaceFixture());},default).GetAwaiter().GetResult();
            Assert(calls==4);
        });
        Check("cache: manual refresh/write invalidation forces a new read",()=>{
            var cache=new ModelReadCache();var session=new object();int calls=0;
            Func<Task<ModelSnapshot>> read=()=>{calls++;return Task.FromResult(WindWorkspaceFixture());};
            cache.ReadAsync(session,"A",ModelReadScope.Viewer,read,default).GetAwaiter().GetResult();cache.Invalidate();
            cache.ReadAsync(session,"A",ModelReadScope.Viewer,read,default).GetAwaiter().GetResult();Assert(calls==2);
        });
        Check("cache: concurrent windows share one reader without moving its thread",()=>{
            var cache=new ModelReadCache();var session=new object();var gate=new TaskCompletionSource<ModelSnapshot>();int calls=0,thread=Environment.CurrentManagedThreadId;
            var a=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>{Assert(Environment.CurrentManagedThreadId==thread);calls++;return gate.Task;},default);
            var b=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>{calls++;throw new Exception("duplicate");},default);
            gate.SetResult(WindWorkspaceFixture());Task.WhenAll(a,b).GetAwaiter().GetResult();Assert(calls==1&&!a.Result.Reused&&b.Result.Reused);
        });
        Check("cache: concurrent viewer and wind share a successful complete read",()=>{
            var cache=new ModelReadCache();var session=new object();var gate=new TaskCompletionSource<ModelSnapshot>();
            var viewer=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>gate.Task,default);
            var wind=cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>throw new Exception("duplicate full read"),default);
            gate.SetResult(WindWorkspaceFixture());Task.WhenAll(viewer,wind).GetAwaiter().GetResult();
            Assert(wind.Result.Reused&&ReferenceEquals(viewer.Result.Snapshot,wind.Result.Snapshot));
        });
        Check("cache: concurrent wind rejects incomplete viewer data",()=>{
            var cache=new ModelReadCache();var session=new object();var gate=new TaskCompletionSource<ModelSnapshot>();
            var viewer=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>gate.Task,default);
            var wind=cache.ReadAsync(session,"A",ModelReadScope.Verified,()=>throw new Exception("duplicate full read"),default);
            gate.SetResult(WithCoverage(WindWorkspaceFixture(),false));viewer.GetAwaiter().GetResult();
            Throws<InvalidOperationException>(()=>wind.GetAwaiter().GetResult());
        });
        Check("cache: canceling follower does not cancel another window",()=>{
            var cache=new ModelReadCache();var session=new object();var gate=new TaskCompletionSource<ModelSnapshot>();using var cancel=new CancellationTokenSource();
            var a=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>gate.Task,default);
            var b=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>throw new Exception("duplicate"),cancel.Token);
            cancel.Cancel();Throws<OperationCanceledException>(()=>b.GetAwaiter().GetResult());
            gate.SetResult(WindWorkspaceFixture());Assert(a.GetAwaiter().GetResult().Snapshot!=null);
        });
        Check("cache: invalidation rejects in-flight completion and cannot overwrite newer read",()=>{
            var cache=new ModelReadCache();var session=new object();var gate=new TaskCompletionSource<ModelSnapshot>();
            var old=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>gate.Task,default);cache.Invalidate();
            var fresh=WindWorkspaceFixture();cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>Task.FromResult(fresh),default).GetAwaiter().GetResult();
            gate.SetResult(WindWorkspaceFixture());Throws<OperationCanceledException>(()=>old.GetAwaiter().GetResult());
            var actual=cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>throw new Exception("lost cache"),default).GetAwaiter().GetResult();Assert(ReferenceEquals(actual.Snapshot,fresh));
        });
        Check("cache: read exception allows retry",()=>{
            var cache=new ModelReadCache();var session=new object();
            Throws<InvalidOperationException>(()=>cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>throw new InvalidOperationException("API failed"),default).GetAwaiter().GetResult());
            Assert(!cache.ReadAsync(session,"A",ModelReadScope.Viewer,()=>Task.FromResult(WindWorkspaceFixture()),default).GetAwaiter().GetResult().Reused);
        });
        Check("cache: pre-canceled requests cannot return cached success",()=>{
            var cache=new ModelReadCache();using var cancel=new CancellationTokenSource();cancel.Cancel();
            Throws<OperationCanceledException>(()=>cache.ReadAsync(new object(),"A",ModelReadScope.Viewer,()=>throw new Exception("must not read"),cancel.Token).GetAwaiter().GetResult());
        });
        Check("wind catalog: round trip retains explicit missing geometry and load coverage",()=>{
            var s=JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Serialize(CatalogOf(WindWorkspaceFixture())));Assert(s.CatalogOnly&&!s.LoadBaselineComplete&&s.Elements.Count==0);
            Throws<InvalidOperationException>(()=>ApplyPlan.Create(s,new DraftWorkspace(s).Document(),ApplyCapabilities.ForSource("ETABS")));
        });
        Check("wind catalog: geometry hydration retains Tab 1 edits, mapping and selected level",()=>{
            var s=WindWorkspaceFixture();var vm=new WindWorkspaceViewModel();vm.SetSnapshot(CatalogOf(s),new[]{"WU","WX"});
            vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("D1",1,0,0,0,0)}));
            vm.UserRows[0].Story="L1";vm.UserRows[0].Fx="10";vm.MappingConfirmed=true;string baseline=vm.UserFingerprint;
            Assert(vm.Project().Beams==null);vm.InstallGeometry(s);
            Assert(vm.UserRows[0].Fx=="10"&&vm.MappingConfirmed&&vm.UserFingerprint==baseline&&vm.Viewer.Level.Name=="L1");
            Assert(vm.UserDocument().Changes.Single().Fx==10&&!vm.Snapshot.CatalogOnly);
        });
        Check("wind catalog: Excel rows import without geometry",()=>{
            var vm=new WindWorkspaceViewModel();vm.SetSnapshot(CatalogOf(WindWorkspaceFixture()),new[]{"WU"});
            vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("D1",1,0,0,0,0)}));
            vm.ImportRows(new[]{new StoryWindInput{Target="UserLoads",Pattern="WU",DiaphragmKey="D1",Story="L1",Fx="14",Fy="0",Mz="0",X="0",Y="0"}});
            Assert(vm.Snapshot.CatalogOnly&&vm.UserRows.Single().Fx=="14");
        });
        Check("wind catalog: incomplete geometry cannot enable beam editing",()=>{
            var s=WindWorkspaceFixture();var vm=new WindWorkspaceViewModel();vm.SetSnapshot(CatalogOf(s),new[]{"WU"});
            Throws<InvalidOperationException>(()=>vm.InstallGeometry(WithCoverage(s,false)));Assert(vm.Snapshot.CatalogOnly);
        });
        Check("wind catalog: changed story metadata blocks lazy upgrade",()=>{
            var s=WindWorkspaceFixture();var vm=new WindWorkspaceViewModel();vm.SetSnapshot(CatalogOf(s),new[]{"WU"});
            var changed=new ModelSnapshot(1,s.Source,s.Version,s.Identity,s.SourceUnits,s.CapturedUtc,s.Elements,new[]{new ModelLevel("L1",99)},s.Loads,s.Diagnostics,s.LoadPatterns);
            Throws<InvalidOperationException>(()=>vm.InstallGeometry(changed));Assert(vm.Snapshot.CatalogOnly);
        });
        Check("wind catalog: saved user edits reopen on full snapshot",()=>{
            var s=WindWorkspaceFixture();var vm=new WindWorkspaceViewModel();vm.SetSnapshot(CatalogOf(s),new[]{"WU"});
            vm.SetUserBaseline(new UserWindState("WU","User Loads",new[]{new UserWindRow("D1",1,0,0,0,0)}));vm.UserRows[0].Story="L1";vm.UserRows[0].Fy="4";
            var file=JsonCodec.Deserialize<WindProject>(JsonCodec.Serialize(vm.Project()));var next=new WindWorkspaceViewModel();next.SetSnapshot(s,new[]{"WU"});next.Restore(file);
            Assert(next.UserRows.Single().Fy=="4"&&!next.MappingConfirmed);
        });
        Check("wind catalog: User Loads still verifies full live model and backup",()=>{
            var host=new WindFake();var original=WindDocument(host);var doc=new UserWindDocument(original.Identity,WindBaseline.Of(CatalogOf(host.Inner.Source)),original.Before,original.Changes);
            var result=ApplyWind(host,doc);Assert(result.Outcome=="Succeeded"&&host.Inner.Backups==1&&host.Writes==1);
        });
        Check("wind catalog: fresh direct-load conflict blocks write",()=>{
            var host=new WindFake();var original=WindDocument(host);var doc=new UserWindDocument(original.Identity,WindBaseline.Of(CatalogOf(host.Inner.Source)),original.Before,original.Changes);
            host.Inner.Loads.Add(new ExistingLoad(host.Inner.Source.Elements.First().Id,doc.Before.Pattern,"Frame force/length","Global",4,0,1,2,2,"kN/m"));
            Assert(ApplyWind(host,doc).Outcome=="BlockedBeforeWrite"&&host.Writes==0&&host.Inner.Backups==0);
        });
        Check("wind catalog: changed User Loads cannot be overwritten",()=>{
            var host=new WindFake();var original=WindDocument(host);var doc=new UserWindDocument(original.Identity,WindBaseline.Of(CatalogOf(host.Inner.Source)),original.Before,original.Changes);
            host.State=new UserWindState(host.State.Pattern,host.State.Code,new[]{new UserWindRow("SYNTHETIC-KEY-1",99,0,2,4,5)});
            Assert(ApplyWind(host,doc).Outcome=="BlockedBeforeWrite"&&host.Writes==0);
        });
        Check("wind catalog: advanced table retains current diaphragm assignment checks",()=>{
            var host=new UserTableFake{AssignmentsChanged=true};var old=host.Change();
            var change=new WindTableChange(old.Identity,WindBaseline.Of(CatalogOf(host.Inner.Source)),old.Pattern,old.Before,old.After,true);
            Assert(ApplyTable(host,change).Outcome=="BlockedBeforeWrite"&&host.Writes==0);
        });
        Check("bulk: failed batch discards partial rows before fallback",()=>{
            var loads=new List<ExistingLoad>();var one=WindWorkspaceFixture().Loads.First();
            bool ok=LoadBatch.TryRead(rows=>{rows.Add(one);throw new InvalidOperationException("unsupported");},new[]{one.ElementId},loads,out var reason);
            Assert(!ok&&loads.Count==0&&reason=="unsupported");loads.Add(one);Assert(loads.Count==1);
        });
        Check("bulk: malformed or foreign-object responses are rejected",()=>{
            var loads=new List<ExistingLoad>();var one=WindWorkspaceFixture().Loads.First();
            Assert(!LoadBatch.TryRead(rows=>rows.Add(one),new[]{"wrong"},loads,out _)&&loads.Count==0);
            Assert(!LoadBatch.TryRead(rows=>rows.Add(new ExistingLoad(one.ElementId,"WX","Frame force/length","Global",4,0,1,double.NaN,1,"kN/m")),new[]{one.ElementId},loads,out _)&&loads.Count==0);
        });
        Check("bulk: signed values and multiple entries are preserved",()=>{
            var one=new ExistingLoad("Frame:1","WX","Frame force/length","Global",4,.2,.7,-3,-8,"kN/m");var loads=new List<ExistingLoad>();
            Assert(LoadBatch.TryRead(rows=>{rows.Add(one);rows.Add(one);},new[]{one.ElementId},loads,out _));Assert(loads.Count==2&&loads.All(l=>l.Value2==-8&&l.Start==.2));
        });
        Check("elevation index: crossing and extruded geometry matches brute-force projection",()=>{
            var random=new Random(42);var elements=new List<ModelElement>();
            for(int i=0;i<1000;i++) {
                double z=random.Next(40)*3.2;bool vertical=i%3==0;
                elements.Add(new ModelElement("Frame:"+i,i.ToString(),"","unrelated story","",vertical?ElementKind.Column:ElementKind.Beam,
                    new[]{new Vec3(i%20,i/20,z),new Vec3(i%20+(vertical?0:5),i/20,z+(vertical?6.4:i%5==0?2:0))},i%90,false,new SectionGeometry("Rectangle",.3,.8)));
            }
            elements.Add(new ModelElement("Area:W","W","","not a story","",ElementKind.Wall,new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(1,0,130),new Vec3(0,0,130)},section:new SectionGeometry("Shell",.2,.2)));
            var index=new ModelElevationIndex(elements);
            for(int story=0;story<40;story++) {
                double z=story*3.2;var plane=ViewPlane.Plan(z,.15);
                var expected=elements.Where(e=>SectionExtrusion.Build(e,plane)!=null).Select(e=>e.Id);
                var actual=index.Query(z-.150001,z+.150001).Where(e=>SectionExtrusion.Build(e,plane)!=null).Select(e=>e.Id);
                Assert(expected.SequenceEqual(actual));
            }
        });
        Check("elevation index: 50k members restricts per-story projection candidates",()=>{
            var elements=Enumerable.Range(0,50000).Select(i=>new ModelElement("Frame:"+i,i.ToString(),"","","",ElementKind.Beam,new[]{new Vec3(i%100,0,i/1000*3.2),new Vec3(i%100+1,0,i/1000*3.2)})).ToArray();
            var watch=Stopwatch.StartNew();var index=new ModelElevationIndex(elements);double build=watch.Elapsed.TotalMilliseconds;watch.Restart();
            var items=index.Query(31.85,32.15);double query=watch.Elapsed.TotalMilliseconds;Assert(items.Count==1000);
            Console.WriteLine($"MEASURE synthetic Z-index: objects=50000, candidates={items.Count}, build={build:0.00}ms, query={query:0.00}ms; excludes COM/WPF.");
        });
    }
}
