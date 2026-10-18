#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        private Dictionary<string,ModelElement> displayById=new Dictionary<string,ModelElement>(StringComparer.Ordinal);
        public IReadOnlyList<string> SelectedIds { get; private set; }=Array.Empty<string>();
        private HashSet<string> selectedIdSet=new HashSet<string>(StringComparer.Ordinal);
        public IReadOnlyList<ModelElement> SelectedElements { get; private set; }=Array.Empty<ModelElement>();
        public bool SingleSelection => SelectedElements.Count==1;
        public void SelectElements(IEnumerable<string> ids,SelectionChange change=SelectionChange.Replace)
        {
            var next=ViewerSelection.Change(SelectedIds,ids.Where(displayById.ContainsKey),change);
            if(next.SequenceEqual(SelectedIds,StringComparer.Ordinal))return;
            SelectedIds=Array.AsReadOnly(next);selectedIdSet=new HashSet<string>(next,StringComparer.Ordinal);SelectedElements=Array.AsReadOnly(next.Select(id=>displayById[id]).ToArray());selected=SelectedElements.LastOrDefault();
            foreach(string name in new[]{nameof(SelectedIds),nameof(SelectedElements),nameof(SingleSelection),nameof(SelectedElement),nameof(SelectedId),nameof(VisibleLoads),nameof(PropertyKey),nameof(Inspector),nameof(Counts)})Changed(name);
            PropertyColor=selected==null?"#2563EB":ColorOverrides.TryGetValue(PropertyKey,out var color)?color:PropertyColors.ForProperty(PropertyKey);Changed(nameof(PropertyColor));
            OnSelectionChanged();
        }
        public void ClearSelection()=>SelectElements(Array.Empty<string>());
    }
}
