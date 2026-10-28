using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static void LayoutTests()
    {
        Check("layout: columns are available in Plan and Elevation",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);
            Assert(vm.DisplayElements.Count==8&&vm.VisibleElements.Any(e=>e.Kind==ElementKind.Column));
            vm.Elevation=true;Assert(vm.Scene.Any(e=>e.Source.Kind==ElementKind.Column));
        });
        Check("layout: columns can be filtered and selected",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);Assert(vm.Kinds.Contains("Column"));
            vm.Kind="Column";Assert(vm.Scene.All(e=>e.Source.Kind==ElementKind.Column));vm.SelectedElement=vm.Snapshot.Elements.First(e=>e.Kind==ElementKind.Column);Assert(vm.SelectedElement!=null);
        });
        Check("layout: column loads available without mutating source",()=>{
            var f=DraftFixture();var c=new ExistingLoad("Frame:C1","SDL","Frame force/length","Global",10,0,1,2,2,"kN/m");
            var s=new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,f.Levels,f.Loads.Concat(new[]{c}),f.Diagnostics,f.LoadPatterns);
            string before=JsonCodec.Serialize(s);var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(s,true);
            Assert(vm.VisibleLoads.Any(l=>l.ElementId=="Frame:C1"));
            Assert(JsonCodec.Serialize(s)==before&&vm.Drafts.Fingerprint==SnapshotFingerprint.Of(s));
        });
        Check("layout: independent visibility hides only the chosen kind",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);
            vm.LabelSettings.Single(s=>s.Kind==ElementKind.Column).Visible=false;vm.NotifyStyle();
            Assert(vm.Scene.All(e=>e.Source.Kind!=ElementKind.Column)&&vm.DisplayElements.Count==8&&vm.Scene.Any(e=>e.Source.Kind==ElementKind.Beam));
            vm.LabelSettings.Single(s=>s.Kind==ElementKind.Column).Visible=true;vm.NotifyStyle();Assert(vm.Scene.Any(e=>e.Source.Kind==ElementKind.Column));
        });
        Check("layout: Wind tab stays active when inspecting a beam",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.DraftModuleIndex=2;vm.SelectedElement=vm.Snapshot.Elements.First(e=>e.Kind==ElementKind.Beam);Assert(vm.DraftModuleIndex==2&&!vm.CanEditDraft&&vm.DraftValueLabel=="");});
        Check("layout: loading a saved draft selects its actual editor",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.DraftModuleIndex=2;var w=vm.Drafts;w.Put(Slab());vm.EditDraft(w.Loads.Single());Assert(vm.DraftModuleIndex==1&&vm.CanEditDraft);});
        Check("layout: desktop default is compact",()=>{var f=WorkspaceSize.Fit(1920,1040,1,1);Near(f.Width,1180);Near(f.Height,740);Assert(f.MinWidth<=f.Width&&f.MinHeight<=f.Height);});
        Check("layout: 1366x768 work area fits at 125 percent",()=>{var f=WorkspaceSize.Fit(1366,728,1.25,1.25);Assert(f.Width*1.25<=1366&&f.Height*1.25<=728);Near(f.Height,558.4);Assert(f.MinHeight<=f.Height);});
        Check("layout: high DPI clamps minimums instead of overflowing screen",()=>{foreach(double dpi in new[]{1.0,1.25,1.5,2.0}){var f=WorkspaceSize.Fit(1366,728,dpi,dpi);Assert(f.Width*dpi<1366&&f.Height*dpi<728&&f.MinWidth<=f.Width&&f.MinHeight<=f.Height);}});
        Check("layout: DPI scaling for each axis is independent",()=>{var f=WorkspaceSize.Fit(1200,720,1.25,1.5);Assert(f.Width*1.25<1200&&f.Height*1.5<720);Near(f.Height,456);});
        Check("layout: moving a small manually resized window preserves its size",()=>{var f=WorkspaceSize.Fit(1920,1040,1,1,800,480);Near(f.Width,800);Near(f.Height,480);Assert(f.MinWidth<=800&&f.MinHeight<=480);});
        Check("layout: narrow screen modes leave the viewport usable",()=>{Assert(WorkspaceSize.PanelMode(WorkspaceSize.Fit(1920,1040,1.25,1.25).Width)==2);Assert(WorkspaceSize.PanelMode(WorkspaceSize.Fit(1366,728,1.5,1.5).Width)==1);Assert(WorkspaceSize.PanelMode(WorkspaceSize.Fit(1366,728,2,2).Width)==0);});
        Check("layout: invalid work area and DPI rejected",()=>{Throws<ArgumentException>(()=>WorkspaceSize.Fit(0,728,1,1));Throws<ArgumentException>(()=>WorkspaceSize.Fit(1366,728,0,1));Throws<ArgumentException>(()=>WorkspaceSize.Fit(1366,728,double.NaN,1));});
    }
}
