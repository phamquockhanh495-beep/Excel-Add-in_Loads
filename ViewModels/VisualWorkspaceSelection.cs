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
        private readonly HashSet<string> workspaceRemovedIds=new HashSet<string>(StringComparer.Ordinal);
        public int RemovedFromWorkspaceCount=>workspaceRemovedIds.Count;
        public bool CanRemoveFromWorkspace=>!Busy&&SelectedIds.Count>0;
        public bool CanRestoreWorkspace=>!Busy&&workspaceRemovedIds.Count>0;
        public string SelectionSummary=>SelectedElements.Count==0?"Chưa chọn cấu kiện"+(workspaceRemovedIds.Count>0?" · Đã bỏ khỏi view: "+workspaceRemovedIds.Count:""):
            SelectedElements.Count==1?"Đang chọn: "+SelectedElement!.Name+" · Label: "+SelectedElement.Label+" · "+SelectedElement.Story+" · "+SelectedElement.Kind+" · "+SelectedElement.Property:
            "Đang chọn "+SelectedElements.Count+" cấu kiện · "+string.Join("; ",SelectedElements.GroupBy(e=>e.Kind).Select(g=>g.Key+": "+g.Count()));
        public void SelectElements(IEnumerable<string> ids,SelectionChange change=SelectionChange.Replace)
        {
            var next=ViewerSelection.Change(SelectedIds,ids.Where(id=>displayById.ContainsKey(id)&&!workspaceRemovedIds.Contains(id)),change);
            if(next.SequenceEqual(SelectedIds,StringComparer.Ordinal))return;
            SelectedIds=Array.AsReadOnly(next);selectedIdSet=new HashSet<string>(next,StringComparer.Ordinal);SelectedElements=Array.AsReadOnly(next.Select(id=>displayById[id]).ToArray());selected=SelectedElements.LastOrDefault();
            foreach(string name in new[]{nameof(SelectedIds),nameof(SelectedElements),nameof(SingleSelection),nameof(SelectedElement),nameof(SelectedId),nameof(VisibleLoads),nameof(PropertyKey),nameof(Inspector),nameof(InspectorTitle),nameof(InspectorSubtitle),nameof(InspectorRows),nameof(InspectorCoordinates),nameof(Counts)})Changed(name);
            PropertyColor=selected==null?"#2563EB":ColorOverrides.TryGetValue(PropertyKey,out var color)?color:PropertyColors.ForProperty(PropertyKey);Changed(nameof(PropertyColor));
            OnSelectionChanged();
            NotifyWorkspaceVisibility();
        }
        public void ClearSelection()=>SelectElements(Array.Empty<string>());
        public void RemoveSelectedFromWorkspace()
        {
            if(!CanRemoveFromWorkspace)return;
            int count=SelectedIds.Count;
            workspaceRemovedIds.UnionWith(SelectedIds);ClearSelection();NotifyWorkspaceVisibility();Refresh();
            Status="Đã bỏ "+count+" cấu kiện khỏi workspace 2D. Bấm Khôi phục để hiện lại; mô hình và tải giữ nguyên.";
        }
        public void RestoreWorkspace()
        {
            if(!CanRestoreWorkspace)return;
            int count=workspaceRemovedIds.Count;workspaceRemovedIds.Clear();NotifyWorkspaceVisibility();Refresh();
            Status="Đã khôi phục "+count+" cấu kiện trong workspace 2D.";
        }
        private void NotifyWorkspaceVisibility()
        {
            foreach(string name in new[]{nameof(RemovedFromWorkspaceCount),nameof(CanRemoveFromWorkspace),nameof(CanRestoreWorkspace),nameof(SelectionSummary),nameof(VisibleLoads),nameof(ComparisonRows)})Changed(name);
        }
    }
}
