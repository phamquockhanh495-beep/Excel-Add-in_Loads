using System;
using System.IO;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static void ViewerPreferencesTests()
    {
        Check("viewer preferences restore per-kind display and load settings without touching model",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(SelectFixture(),true);string before=JsonCodec.Serialize(vm.Snapshot);
            vm.Extrude=false;vm.ColorByProperty=true;vm.HalfDepth="0,25";vm.Reverse=true;vm.ShowFrameLoads=true;vm.ShowJointLoads=true;vm.ShowZeroLoads=true;vm.ShowLoadValues=false;vm.LoadSetsMode=true;
            var label=vm.LabelSettings.Single(l=>l.Kind==ElementKind.Slab);label.Visible=false;label.Label=false;label.UniqueName=true;label.Section=true;label.Extrude=false;
            var saved=JsonCodec.Deserialize<ViewerPreferences>(JsonCodec.Serialize(vm.CapturePreferences("/model/a.edb")));
            var reopened=new VisualWorkspaceViewModel();reopened.SetSnapshot(vm.Snapshot,true);reopened.ApplyPreferences(saved);
            var restored=reopened.LabelSettings.Single(l=>l.Kind==ElementKind.Slab);
            Assert(!reopened.Extrude&&reopened.ColorByProperty&&reopened.HalfDepth=="0,25"&&reopened.Reverse&&reopened.ShowFrameLoads&&reopened.ShowJointLoads&&reopened.ShowZeroLoads&&!reopened.ShowLoadValues&&reopened.LoadSetsMode);
            Assert(!restored.Visible&&!restored.Label&&restored.UniqueName&&restored.Section&&!restored.Extrude&&JsonCodec.Serialize(reopened.Snapshot)==before);
        });
        Check("viewer restores floor and pattern only for the matching model and drops stale names",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(SelectFixture(),true);var desired=vm.Levels.First();
            var saved=vm.CapturePreferences("/model/a.edb");saved.LastStory=desired.Name;saved.LastPattern="WL";saved.LastSet="missing";
            var initial=vm.Level;vm.RestoreModelPreferences(saved,"/model/b.edb");Assert(ReferenceEquals(initial,vm.Level));
            vm.RestoreModelPreferences(saved,"/model/a.edb");Assert(vm.Level.Name==desired.Name&&vm.ReadPattern=="WL"&&vm.UniformLoadSet==VisualWorkspaceViewModel.AllUniformSets);
            saved.LastStory="deleted";saved.LastPattern="deleted";vm.RestoreModelPreferences(saved,"/model/a.edb");Assert(vm.Level.Name==desired.Name&&vm.ReadPattern==VisualWorkspaceViewModel.AllReadPatterns);
        });
        Check("preferences reject invalid version, duplicate kinds and nonfinite view depth",()=>{
            foreach(var depth in new[]{"NaN","Infinity","-1","bad"})Throws<ArgumentException>(()=>new ViewerPreferences{HalfDepth=depth}.Validate());
            Throws<ArgumentException>(()=>new ViewerPreferences{Version=2}.Validate());
            Throws<ArgumentException>(()=>new ViewerPreferences{Labels=new[]{new ViewerLabelPreference(),new ViewerLabelPreference()}}.Validate());
        });
        Check("preferences atomically replace file and preserve good contents when new config is invalid",()=>{
            string dir=Path.Combine(Path.GetTempPath(),"qks-prefs-"+Guid.NewGuid().ToString("N")),file=Path.Combine(dir,"viewer.json");
            try {
                ViewerPreferencesFile.Write(file,new ViewerPreferences{FrameLoads=true});
                Assert(ViewerPreferencesFile.Read(file).FrameLoads);
                ViewerPreferencesFile.Write(file,new ViewerPreferences{ShellLoads=true,LeftPanel=false});
                Assert(ViewerPreferencesFile.Read(file).ShellLoads&&ViewerPreferencesFile.Read(file).LeftPanel==false);
                string before=File.ReadAllText(file);Throws<ArgumentException>(()=>ViewerPreferencesFile.Write(file,new ViewerPreferences{HalfDepth="bad"}));
                Assert(before==File.ReadAllText(file)&&Directory.GetFiles(dir).Length==1);
                File.WriteAllText(file,"{broken");bool failed=false;try{ViewerPreferencesFile.Read(file);}catch{failed=true;}Assert(failed&&File.ReadAllText(file)=="{broken");
            } finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
        });
        Check("wall plan label contains pattern signed values and units without direction or range",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(SelectFixture(),true);vm.Level=vm.Levels.First();vm.ShowFrameLoads=true;
            Assert(vm.ReadLoadItems.Single().PlanLineLabel=="WL - 2 kN/m");Assert(vm.ReadLoadItems.Single().Text.Contains("Gravity"));
            var f=SelectFixture();var load=new ExistingLoad("Frame:1","WALL","Frame force/length","Global",10,0,1,-2.5,-4,"kN/m");
            vm.SetSnapshot(new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,f.Levels,new[]{load},f.Diagnostics),true);vm.Level=vm.Levels.First();
            Assert(vm.ReadLoadItems.Single().PlanLineLabel=="WALL - -2.5 → -4 kN/m");
        });
    }
}
