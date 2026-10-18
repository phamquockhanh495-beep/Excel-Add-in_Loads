using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static int Benchmarks(string output)
    {
        var rows=new List<object>();
        foreach(int count in new[]{1000,10000,50000}) {
            var elements=new List<ModelElement>();var loads=new List<ExistingLoad>();
            for(int i=0;i<count;i++) {
                double x=i%200*6,y=i/200*4;bool column=i%10==0,area=i%5==1;
                string name=i.ToString(),id=(area?"Area:":"Frame:")+name;
                var points=area?new[]{new Vec3(x,y,3),new Vec3(x+6,y,3),new Vec3(x+6,y+4,3),new Vec3(x,y+4,3)}:new[]{new Vec3(x,y,column?0:3),new Vec3(x+(column?0:6),y,3)};
                elements.Add(new ModelElement(id,name,"B"+name,"L1",area?"S150":"B200x400",column?ElementKind.Column:area?ElementKind.Slab:ElementKind.Beam,points));
                if(i%3==0)loads.Add(new ExistingLoad(id,"SDL",area?"Area uniform":"Frame force/length","Global",10,0,1,2,2,area?"kN/m²":"kN/m"));
            }
            var snapshot=new ModelSnapshot(1,"SYNTHETIC BENCHMARK","1","benchmark-"+count,"kN/m/C",DateTime.UtcNow,elements,new[]{new ModelLevel("L1",3)},loads,Array.Empty<string>(),new[]{"SDL"});
            var vm=new VisualWorkspaceViewModel(); var times=new List<double>(); var elevationTimes=new List<double>();
            for(int sample=0;sample<4;sample++) {
                var clock=Stopwatch.StartNew();vm.SetSnapshot(snapshot,true);clock.Stop();if(sample>0)times.Add(clock.Elapsed.TotalMilliseconds);
                Assert(vm.Scene.Count==count-count/10&&vm.Scene.All(e=>e.Source.Kind!=ElementKind.Column));
                clock.Restart();vm.Elevation=true;clock.Stop();if(sample>0)elevationTimes.Add(clock.Elapsed.TotalMilliseconds);
                Assert(vm.Scene.All(e=>e.Source.Kind!=ElementKind.Column));vm.Elevation=false;
            }
            var selectionClock=Stopwatch.StartNew();vm.SelectElements(vm.VisibleElements.Select(e=>e.Id));selectionClock.Stop();Assert(vm.SelectedIds.Count==vm.VisibleElements.Count);vm.ClearSelection();
            long memory=GC.GetTotalMemory(true);GC.KeepAlive(vm);GC.KeepAlive(snapshot);
            rows.Add(new { objects=count, sourceLoads=loads.Count, shownInPlan=vm.Scene.Count, snapshotAndPlanMedianMs=times.OrderBy(t=>t).ElementAt(1), elevationMedianMs=elevationTimes.OrderBy(t=>t).ElementAt(1), selectAllMs=selectionClock.Elapsed.TotalMilliseconds, managedMemoryBytes=memory });
        }
        var report=new {capturedUtc=DateTime.UtcNow, framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, os=System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            scope="Synthetic core/VM timings only, 1 warm-up + 3 samples, median. NOT WPF rendering, COM, FPS, host benchmarks or peak memory. Memory is GC live heap observation.",results=rows};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));File.WriteAllText(output,JsonCodec.Serialize(report));Console.WriteLine("Benchmark saved: "+output);return 0;
    }
}
