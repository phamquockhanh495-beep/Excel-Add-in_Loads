#nullable enable
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public TextAppearance StructureText { get; private set; }=TextAppearance.StructureDefault;
        public TextAppearance LoadText { get; private set; }=TextAppearance.LoadsDefault;
        public void ApplyTextProfile(ViewerTextProfile profile)
        {
            StructureText=profile.Structure;LoadText=profile.Loads;Changed(nameof(StructureText));Changed(nameof(LoadText));
        }
    }
}
