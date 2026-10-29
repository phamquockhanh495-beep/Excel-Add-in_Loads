#nullable enable
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public TextAppearance StructureText { get; private set; }=TextAppearance.StructureDefault;
        public TextAppearance LoadText { get; private set; }=TextAppearance.LoadsDefault;
        public string SlabLegendValueColor => LoadText.UseColor ? LoadText.Hex : "#4169E1";
        public void ApplyTextProfile(ViewerTextProfile profile)
        {
            StructureText=profile.Structure;LoadText=profile.Loads;Changed(nameof(StructureText));Changed(nameof(LoadText));Changed(nameof(SlabLegendValueColor));
        }
    }
}
