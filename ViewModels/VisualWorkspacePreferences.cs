#nullable enable
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public ViewerPreferences CapturePreferences(string modelPath)
        {
            return new ViewerPreferences {
                Labels = LabelSettings.Select(l => new ViewerLabelPreference {Kind=l.Kind, Visible=l.Visible, Extrude=l.Extrude, Label=l.Label, UniqueName=l.UniqueName, Section=l.Section}).ToArray(),
                Extrude=Extrude, ColorByProperty=ColorByProperty, Kind=Kind, HalfDepth=HalfDepth, Reverse=Reverse,
                LoadSetsMode=LoadSetsMode, FrameLoads=ShowFrameLoads, ShellLoads=ShowShellLoads, JointLoads=ShowJointLoads,
                LoadValues=ShowLoadValues, ZeroLoads=ShowZeroLoads,
                LastModelPath=modelPath, LastStory=Level?.Name??"", LastPattern=ReadPattern, LastSet=UniformLoadSet
            };
        }
        public void ApplyPreferences(ViewerPreferences profile)
        {
            profile.Validate();
            foreach (var saved in profile.Labels) {
                var row=LabelSettings.First(l=>l.Kind==saved.Kind);
                row.Visible=saved.Visible; row.Extrude=saved.Extrude; row.Label=saved.Label; row.UniqueName=saved.UniqueName; row.Section=saved.Section;
            }
            extrude=profile.Extrude; ColorByProperty=profile.ColorByProperty; kind=profile.Kind;
            HalfDepth=profile.HalfDepth; reverse=profile.Reverse; loadSetsMode=profile.LoadSetsMode;
            showFrameLoads=profile.FrameLoads; showShellLoads=profile.ShellLoads; showJointLoads=profile.JointLoads;
            showLoadValues=profile.LoadValues; showZeroLoads=profile.ZeroLoads;
            foreach(var name in new[]{nameof(Extrude),nameof(ColorByProperty),nameof(Kind),nameof(HalfDepth),nameof(Reverse),nameof(LoadSetsMode),nameof(PatternMode),nameof(ShowFrameLoads),nameof(ShowShellLoads),nameof(ShowJointLoads),nameof(ShowLoadValues),nameof(ShowZeroLoads),nameof(LabelSettings)}) Changed(name);
            Refresh();
        }
        public void RestoreModelPreferences(ViewerPreferences profile, string modelPath)
        {
            profile.Validate();
            if (Snapshot==null || string.IsNullOrWhiteSpace(profile.LastModelPath) || !Protocol.SamePath(profile.LastModelPath,modelPath)) return;
            level=Levels.FirstOrDefault(l=>l.Name==profile.LastStory)??level;
            readPattern=ReadPatterns.Contains(profile.LastPattern)?profile.LastPattern:AllReadPatterns;
            uniformLoadSet=UniformLoadSets.Contains(profile.LastSet)?profile.LastSet:AllUniformSets;
            Changed(nameof(Level));Changed(nameof(ReadPattern));Changed(nameof(UniformLoadSet));Refresh();
        }
    }
}
