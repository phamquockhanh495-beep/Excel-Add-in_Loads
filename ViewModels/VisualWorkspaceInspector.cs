#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public string InspectorTitle => SingleSelection ? SelectedElement!.Name : SelectedElements.Count > 1 ? $"{SelectedElements.Count} cấu kiện đang chọn" : "Chưa chọn cấu kiện";
        public string InspectorSubtitle => SingleSelection ? KindCaption(SelectedElement!.Kind) + " · " + SelectedElement.Story :
            SelectedElements.Count > 1 ? "Chọn một cấu kiện để xem tiết diện và tọa độ chi tiết." : "Click hoặc quét chọn cấu kiện trên mặt bằng để xem thông tin.";
        private static string KindCaption(ElementKind kind)
        {
            switch (kind) {
                case ElementKind.Beam: return "Dầm"; case ElementKind.Column: return "Cột";
                case ElementKind.Slab: return "Sàn"; case ElementKind.Wall: return "Vách";
                case ElementKind.Brace: return "Giằng"; case ElementKind.Opening: return "Lỗ mở"; default: return "Khác";
            }
        }
        public IReadOnlyList<KeyValuePair<string,string>> InspectorRows
        {
            get {
                var rows=new List<KeyValuePair<string,string>>();
                void Add(string key,string? value) => rows.Add(new KeyValuePair<string,string>(key,string.IsNullOrWhiteSpace(value)?"—":value!));
                if (SingleSelection) {
                    var e=SelectedElement!;
                    Add("Unique ID",e.Name);Add("Label",e.Label);Add("Tầng",e.Story);Add("Loại",KindCaption(e.Kind));Add("Property",e.Property);
                    if(e.Length3D.HasValue)Add("Chiều dài",e.Length3D.Value.ToString("0.###")+" m");
                    if(e.Kind!=ElementKind.Opening)Add("Tiết diện",e.Section?.ToString()??"Chưa đọc được kích thước");
                    if(e.LocalAxisDegrees.HasValue)Add("Góc local",e.LocalAxisDegrees.Value.ToString("0.###")+"°"+(e.AdvancedLocalAxes?" · Advanced":""));
                    Add("Tải đã đọc",(Snapshot?.Loads.Count(l=>l.ElementId==e.Id)??0)+" bản ghi");
                } else if (SelectedElements.Count > 1) {
                    foreach(var group in SelectedElements.GroupBy(e=>e.Kind).OrderBy(g=>g.Key))Add(KindCaption(group.Key),group.Count()+" cấu kiện");
                    Add("Tải đã đọc",(Snapshot?.Loads.Count(l=>selectedIdSet.Contains(l.ElementId))??0)+" bản ghi");
                }
                return rows;
            }
        }
        public IReadOnlyList<KeyValuePair<string,string>> InspectorCoordinates => SingleSelection ?
            SelectedElement!.Vertices.Select((p,i)=>new KeyValuePair<string,string>("P"+(i+1),$"X: {p.X:0.###}   Y: {p.Y:0.###}   Z: {p.Z:0.###}")).ToArray() : Array.Empty<KeyValuePair<string,string>>();

        public bool MovePlanLevel(int direction)
        {
            if(Busy || Elevation || Snapshot==null || Level==null || direction==0)return false;
            // Use physical elevation, not story name or the order returned by an adapter.
            var next=direction>0 ? Levels.Where(l=>l.Elevation>Level.Elevation+1e-8).OrderBy(l=>l.Elevation).FirstOrDefault() :
                Levels.Where(l=>l.Elevation<Level.Elevation-1e-8).OrderByDescending(l=>l.Elevation).FirstOrDefault();
            if(next==null) { Status=direction>0?"Đang ở mặt bằng cao nhất.":"Đang ở mặt bằng thấp nhất.";return false; }
            ClearSelection();Level=next;return true;
        }
    }
}
