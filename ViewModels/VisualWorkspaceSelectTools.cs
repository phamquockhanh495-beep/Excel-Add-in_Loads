#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        private HashSet<string>? isolatedIds;
        public bool IsIsolated=>isolatedIds!=null;
        public bool CanShowAll=>!Busy&&Snapshot!=null;
        public void IsolateSelection()
        {
            if(!CanRemoveFromWorkspace)return;
            isolatedIds=new HashSet<string>(SelectedIds,StringComparer.Ordinal);NotifyWorkspaceVisibility();Refresh();
            Status="Isolate: chỉ hiện "+isolatedIds.Count+" đối tượng đã chọn. Ctrl+A: hiện lại trong vùng xem.";
        }
        public void ShowAllObjects()
        {
            if(!CanShowAll)return;
            isolatedIds=null;workspaceRemovedIds.Clear();kind="Tất cả";search="";
            foreach(var setting in LabelSettings)setting.Visible=true;
            Changed(nameof(Kind));Changed(nameof(Search));NotifyWorkspaceVisibility();Refresh();
            Status="Đã hiện lại toàn bộ đối tượng trong mặt bằng/mặt cắt hiện tại.";
        }
        private IEnumerable<ModelElement> SelectionCandidates(ElementKind type,bool currentView)
        {
            if(type!=ElementKind.Beam&&type!=ElementKind.Column&&type!=ElementKind.Slab&&type!=ElementKind.Wall)return Array.Empty<ModelElement>();
            return (currentView?VisibleElements:DisplayElements).Where(e=>e.Kind==type&&!workspaceRemovedIds.Contains(e.Id)&&(isolatedIds==null||isolatedIds.Contains(e.Id)));
        }
        private static string SelectionValue(ModelElement e,string attribute)
        {
            switch(attribute){case "Section":return e.Property;case "UniqueName":return e.Name;case "Label":return e.Label;default:throw new ArgumentException("Thuộc tính chọn không hợp lệ.");}
        }
        public string[] SelectionValues(ElementKind type,string attribute,bool currentView,string query)
            =>SelectionCandidates(type,currentView).Select(e=>SelectionValue(e,attribute)).Where(v=>!string.IsNullOrWhiteSpace(v)&&(string.IsNullOrWhiteSpace(query)||v.IndexOf(query.Trim(),StringComparison.OrdinalIgnoreCase)>=0)).Distinct(StringComparer.Ordinal).OrderBy(v=>v,StringComparer.OrdinalIgnoreCase).ToArray();
        public int SelectByAttribute(ElementKind type,string attribute,IEnumerable<string> values,bool currentView,string action)
        {
            if(Busy||Snapshot==null)return 0;
            if(action!="Replace"&&action!="Add"&&action!="Remove")throw new ArgumentException("Thao tác chọn không hợp lệ.");
            var names=new HashSet<string>(values,StringComparer.Ordinal);
            var ids=SelectionCandidates(type,currentView).Where(e=>names.Contains(SelectionValue(e,attribute))).Select(e=>e.Id).ToArray();
            if(action=="Remove")SelectElements(SelectedIds.Except(ids,StringComparer.Ordinal).ToArray());
            else SelectElements(ids,action=="Add"?SelectionChange.Add:SelectionChange.Replace);
            Status="Khớp "+ids.Length+" đối tượng · đang chọn "+SelectedIds.Count+(currentView?" · vùng xem hiện tại.":" · toàn mô hình (trừ đối tượng ẩn).");
            return ids.Length;
        }
    }
}
