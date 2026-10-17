#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ModelSnapshot? Snapshot { get; private set; }
        public IReadOnlyList<ProjectedElement> Scene { get; private set; } = Array.Empty<ProjectedElement>();
        public IReadOnlyList<ModelElement> VisibleElements { get; private set; } = Array.Empty<ModelElement>();
        public IReadOnlyList<ModelLevel> Levels { get; private set; } = Array.Empty<ModelLevel>();
        public IReadOnlyList<ModelElement> DisplayElements { get; private set; } = Array.Empty<ModelElement>();
        private HashSet<string> displayIds = new HashSet<string>(StringComparer.Ordinal);
        public IEnumerable<ExistingLoad> VisibleLoads => Snapshot?.Loads.Where(l => displayIds.Contains(l.ElementId) && (SelectedElement == null || l.ElementId == SelectedElement.Id)) ?? Enumerable.Empty<ExistingLoad>();
        public string[] Kinds { get; } = new[] { "Tất cả" }.Concat(Enum.GetNames(typeof(ElementKind)).Where(k => k != nameof(ElementKind.Column))).ToArray();
        private string status = "Dùng Kết nối API chọn ETABS, sau đó Đọc mô hình. SAFE 18–22 đang chờ kiểm chứng API.";
        public string Status { get => status; set { status = value; Changed(); } }
        public string SourceText { get; private set; } = "Chưa có snapshot";
        public string Diagnostics => Snapshot == null ? "Chưa đọc dữ liệu." : string.Join(Environment.NewLine + Environment.NewLine, Snapshot.Diagnostics);
        public bool HasOpenings => Snapshot?.Elements.Any(e => e.Kind == ElementKind.Opening) == true;
        public string Counts => Scene.Count + "/" + DisplayElements.Count + " đối tượng · " + (Snapshot?.Loads.Count(l => displayIds.Contains(l.ElementId)) ?? 0) + " tải · m/kN";
        private bool busy;
        public bool Busy { get => busy; set { busy = value; Changed(); Changed(nameof(CanRead)); Changed(nameof(CanEditDraft)); Changed(nameof(CanUndoDraft)); Changed(nameof(CanRedoDraft)); Changed(nameof(CanDeleteDraft)); } }
        public bool CanRead => !Busy;
        private ModelLevel? level;
        public ModelLevel? Level { get => level; set { level = value; Changed(); Refresh(); } }
        private bool elevation;
        public bool Elevation { get => elevation; set { elevation = value; Changed(); Refresh(); } }
        private bool reverse;
        public bool Reverse { get => reverse; set { reverse = value; Changed(); Refresh(); } }
        public string OriginX { get; set; } = "0";
        public string OriginY { get; set; } = "0";
        public string Angle { get; set; } = "0";
        public string HalfDepth { get; set; } = "0.15";
        private string kind = "Tất cả", search = "";
        public string Kind { get => kind; set { kind = Kinds.Contains(value) ? value : "Tất cả"; Changed(); Refresh(); } }
        public string Search { get => search; set { search = value; Changed(); Refresh(); } }
        public bool ShowLabels { get; set; } = true;
        public bool ShowDraftValues { get; set; } = true;
        public bool ColorByProperty { get; set; }
        public Dictionary<string, string> ColorOverrides { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public string PropertyKey => SelectedElement == null ? "" : SelectedElement.Kind + "|" + SelectedElement.Property;
        public string PropertyColor { get; set; } = "#2563EB";
        private ModelElement? selected;
        public ModelElement? SelectedElement {
            get => selected;
            set {
                if (value?.Kind == ElementKind.Column) value = null;
                if (ReferenceEquals(selected, value)) return;
                selected = value; Changed(); Changed(nameof(VisibleLoads)); Changed(nameof(SelectedId)); Changed(nameof(PropertyKey)); Changed(nameof(Inspector));
                PropertyColor = value == null ? "#2563EB" : ColorOverrides.TryGetValue(PropertyKey, out var c) ? c : PropertyColors.ForProperty(PropertyKey);
                Changed(nameof(PropertyColor));
                OnSelectionChanged();
            }
        }
        public string SelectedId => SelectedElement?.Id ?? "";
        public string Inspector => SelectedElement == null ? "Chọn đối tượng trên hình hoặc bảng.\n\nCtrl + click: chuyển lần lượt các đối tượng chồng nhau." :
            "ID: " + SelectedElement.Id + "\nLabel: " + SelectedElement.Label + "\nStory: " + SelectedElement.Story + "\nLoại: " + SelectedElement.Kind +
            "\nProperty: " + SelectedElement.Property + "\nL 3D: " + (SelectedElement.Length3D?.ToString("0.###") ?? "N/A") + " m" +
            "\nLocal angle: " + (SelectedElement.LocalAxisDegrees?.ToString("0.###") ?? "N/A") + "°" + (SelectedElement.AdvancedLocalAxes ? " (advanced)" : "") +
            "\n\n" + string.Join("\n", SelectedElement.Vertices.Select((p, i) => $"P{i + 1}: {p.X:0.###}, {p.Y:0.###}, {p.Z:0.###} m"));
        public void SetSnapshot(ModelSnapshot? snapshot, bool offline)
        {
            snapshot?.Validate(); Snapshot = snapshot;
            DisplayElements = snapshot?.Elements.Where(e => e.Kind != ElementKind.Column).ToArray() ?? Array.Empty<ModelElement>();
            displayIds = new HashSet<string>(DisplayElements.Select(e => e.Id), StringComparer.Ordinal);
            Levels = snapshot?.Levels ?? Array.Empty<ModelLevel>();
            level = Levels.LastOrDefault(); SelectedElement = null;
            OnSnapshotChanged(snapshot);
            SourceText = snapshot == null ? "Chưa có snapshot" : (offline ? "OFFLINE · " : "SNAPSHOT · ") + snapshot.Source + " " + snapshot.Version + " · " + snapshot.CapturedUtc.ToLocalTime().ToString("dd/MM HH:mm:ss") + " · " + snapshot.Identity;
            Changed(nameof(Levels)); Changed(nameof(Level)); Changed(nameof(SourceText)); Changed(nameof(Diagnostics)); Changed(nameof(VisibleLoads)); Changed(nameof(HasOpenings)); Refresh();
        }
        private static double Parse(string value)
        {
            if (!double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double result) || !Number.Finite(result)) throw new ArgumentException("Giá trị phải là số hữu hạn, dùng dấu thập phân . hoặc ,.");
            return result;
        }
        public void Refresh()
        {
            try {
                double depth = Parse(HalfDepth);
                var plane = Elevation ? ViewPlane.Elevation(Parse(OriginX), Parse(OriginY), Parse(Angle), depth, Reverse) : ViewPlane.Plan(Level?.Elevation ?? 0, depth);
                var scene = new List<ProjectedElement>();
                foreach (var element in DisplayElements) {
                    if (Kind != "Tất cả" && element.Kind.ToString() != Kind) continue;
                    if (!string.IsNullOrWhiteSpace(Search) && (element.Id + " " + element.Label + " " + element.Property + " " + element.Story).IndexOf(Search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var projected = Projection.Build(element, plane); if (projected != null) scene.Add(projected);
                }
                Scene = scene.AsReadOnly(); VisibleElements = scene.Select(e => e.Source).ToArray();
                UpdateDraftOverlays(plane);
                Changed(nameof(Scene)); Changed(nameof(VisibleElements)); Changed(nameof(Counts));
                if (!Busy) Status = Elevation ? "Elevation · U ngang, V = +Z · dải ±" + depth + " m. Nét đứt: đối tượng bị cắt bởi dải xem." : "Plan · XY · Z=" + (Level?.Elevation ?? 0) + " m · dải ±" + depth + " m.";
            }
            catch (Exception ex) { Status = "Không cập nhật hình: " + ex.Message; }
        }
        public void NotifyStyle() { Changed(nameof(ColorByProperty)); Changed(nameof(ShowLabels)); Changed(nameof(ShowDraftValues)); Changed(nameof(ColorOverrides)); }
    }
}
