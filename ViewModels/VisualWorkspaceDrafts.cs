#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public DraftWorkspace? Drafts { get; private set; }
        private string snapshotFingerprint = "";
        public bool DraftContextActive => Snapshot != null && Drafts != null && Drafts.Fingerprint == snapshotFingerprint;
        public IReadOnlyList<DraftLoad> DraftRows => Drafts?.Loads ?? Array.Empty<DraftLoad>();
        public IReadOnlyList<string> AvailablePatterns => Snapshot?.LoadPatterns ?? Array.Empty<string>();
        public IReadOnlyList<DraftOverlay> DraftOverlays { get; private set; } = Array.Empty<DraftOverlay>();
        public IEnumerable<LoadComparison> ComparisonRows => DraftContextActive ? Drafts!.Compare().Where(r => displayIds.Contains(r.Load.ElementId) && !workspaceRemovedIds.Contains(r.Load.ElementId) && (SelectedIds.Count==0 || selectedIdSet.Contains(r.Load.ElementId))) : Enumerable.Empty<LoadComparison>();
        public string DraftStatus => Drafts == null ? "Chưa có bộ nháp." : DraftRows.Count + " dòng · " + (Drafts.IsDirty ? "chưa lưu" : "đã lưu/không đổi") +
            (DraftContextActive ? " · gắn snapshot hiện tại" : " · TÁCH KHỎI SNAPSHOT (vẫn lưu được)");
        public bool CanUndoDraft => !Busy && DraftContextActive && Drafts!.CanUndo;
        public bool CanRedoDraft => !Busy && DraftContextActive && Drafts!.CanRedo;
        public bool CanDeleteDraft => !Busy && DraftContextActive && editingId != null;
        public bool CanEditDraft => !Busy && SingleSelection && DraftContextActive && (draftModuleIndex == 0 ? SelectedElement?.Kind == ElementKind.Beam : draftModuleIndex == 1 && SelectedElement?.Kind == ElementKind.Slab);
        public bool WallInputsEnabled => draftModuleIndex == 0;
        public string DraftValueLabel => draftModuleIndex == 0 ? "q1 / q2 (kN/m); q2 trống = q1" : draftModuleIndex == 1 ? "p (kN/m²)" : "";
        public string EditorCaption => editingId == null ? "Thêm vào nháp" : "Cập nhật dòng nháp";
        public string EditorTarget => SelectedElements.Count>1?"Đã chọn "+SelectedElements.Count+" cấu kiện; chọn một cấu kiện để soạn tải riêng.":SelectedElement == null ? "Chọn dầm/sàn trên hình hoặc bảng." : "Đích: " + SelectedElement.Name + " · " + SelectedElement.Story + (VisibleElements.Any(e => e.Id == SelectedId) ? "" : " · ngoài vùng xem");
        public string[] DraftDirections { get; } = { "Gravity", "Global-X", "Global-Y", "Global-Z" };
        public string[] DraftActions { get; } = { "Thêm tải", "Thay cùng phạm vi" };
        public string DraftPattern { get; set; } = "";
        public string DraftDirection { get; set; } = "Gravity";
        public string DraftActionName { get; set; } = "Thêm tải";
        public string DraftStartMetres { get; set; } = "0";
        public string DraftEndMetres { get; set; } = "0";
        public string DraftValue1 { get; set; } = "";
        public string DraftValue2 { get; set; } = "";
        private string? editingId;
        private int draftModuleIndex;
        public int DraftModuleIndex {
            get => draftModuleIndex;
            set { int next = value >= 0 && value <= 2 ? value : 0; if (draftModuleIndex == next) return; draftModuleIndex = next; editingId = null; NotifyDraftEditor(); }
        }
        private void OnSnapshotChanged(ModelSnapshot? snapshot)
        {
            snapshotFingerprint = snapshot == null ? "" : SnapshotFingerprint.Of(snapshot);
            if (snapshot != null && (Drafts == null || Drafts.Loads.Count == 0 && !Drafts.IsDirty)) Drafts = new DraftWorkspace(snapshot, snapshotFingerprint);
            if (!AvailablePatterns.Contains(DraftPattern, StringComparer.Ordinal)) DraftPattern = AvailablePatterns.FirstOrDefault() ?? "";
            Changed(nameof(AvailablePatterns)); Changed(nameof(DraftPattern)); NotifyDraftState();
        }
        private void OnSelectionChanged()
        {
            editingId = null;
            if (draftModuleIndex < 2) {
                if (SelectedElement?.Kind == ElementKind.Slab) draftModuleIndex = 1;
                else if (SelectedElement?.Kind == ElementKind.Beam) draftModuleIndex = 0;
            }
            DraftStartMetres = "0"; DraftEndMetres = (SelectedElement?.Length3D ?? 0).ToString("R", CultureInfo.InvariantCulture);
            NotifyDraftEditor(); Changed(nameof(ComparisonRows)); NotifyWind();
        }
        private void NotifyDraftEditor()
        {
            foreach (string name in new[] { nameof(DraftModuleIndex), nameof(WallInputsEnabled), nameof(DraftValueLabel), nameof(EditorCaption), nameof(EditorTarget), nameof(CanEditDraft), nameof(CanDeleteDraft), nameof(DraftPattern), nameof(DraftDirection), nameof(DraftActionName), nameof(DraftStartMetres), nameof(DraftEndMetres), nameof(DraftValue1), nameof(DraftValue2) }) Changed(name);
        }
        public void NotifyDraftState()
        {
            foreach (string name in new[] { nameof(DraftContextActive), nameof(DraftRows), nameof(DraftStatus), nameof(CanUndoDraft), nameof(CanRedoDraft), nameof(ComparisonRows) }) Changed(name);
            NotifyDraftEditor(); Refresh();
            NotifyApply();
            NotifyWind();
        }
        public void ResetDraftEditor() { editingId = null; NotifyDraftEditor(); }
        public void NewDraftSet()
        {
            if (Snapshot == null) throw new InvalidOperationException("Mở/đọc snapshot trước.");
            Drafts = new DraftWorkspace(Snapshot); editingId = null; NotifyDraftState();
        }
        public void SaveEditorToDraft()
        {
            if (!CanEditDraft) throw new InvalidOperationException("Chọn đúng dầm/sàn và bộ nháp gắn snapshot hiện tại.");
            var action = DraftActionName == DraftActions[0] ? DraftAction.Add : DraftActionName == DraftActions[1] ? DraftAction.ReplaceMatching : throw new ArgumentException("Thao tác nháp không hợp lệ.");
            int direction = LoadDirections.Code(DraftDirection); double v1 = Parse(DraftValue1);
            DraftLoad load = draftModuleIndex == 0 ? DraftLoad.WallFromMetres(SelectedElement!, DraftPattern, direction,
                Parse(DraftStartMetres), Parse(DraftEndMetres), v1, string.IsNullOrWhiteSpace(DraftValue2) ? v1 : Parse(DraftValue2), action, editingId) :
                new DraftLoad(editingId ?? Guid.NewGuid().ToString("N"), SelectedId, DraftKind.SlabUniform, DraftPattern, direction, 0, 1, v1, v1, action);
            Drafts!.Put(load); editingId = load.Id; NotifyDraftState(); Status = "Đã cập nhật NHÁP; chưa ghi model. Chọn tab Nháp / Trước–Sau để đối chiếu.";
        }
        public void EditDraft(DraftLoad load)
        {
            if (!DraftContextActive || !DraftRows.Any(l => l.Id == load.Id)) throw new InvalidOperationException("Bộ nháp đang tách snapshot hoặc dòng không còn tồn tại.");
            if (workspaceRemovedIds.Contains(load.ElementId)) throw new InvalidOperationException("Cấu kiện đã bỏ khỏi workspace. Bấm Khôi phục trước khi sửa nháp.");
            SelectedElement = Snapshot!.Elements.Single(e => e.Id == load.ElementId);
            draftModuleIndex = load.Kind == DraftKind.WindLine ? 2 : load.Kind == DraftKind.WallLine ? 0 : 1; editingId = load.Id;
            DraftPattern = load.Pattern; DraftDirection = load.DirectionName; DraftActionName = load.Action == DraftAction.Add ? DraftActions[0] : DraftActions[1];
            double length = SelectedElement.Length3D ?? 0;
            DraftStartMetres = (load.Start * length).ToString("R", CultureInfo.InvariantCulture); DraftEndMetres = (load.End * length).ToString("R", CultureInfo.InvariantCulture);
            DraftValue1 = load.Value1.ToString("R", CultureInfo.InvariantCulture); DraftValue2 = load.Value2.ToString("R", CultureInfo.InvariantCulture); NotifyDraftEditor();
        }
        public void DeleteDraft() { if (!CanDeleteDraft) return; Drafts!.Remove(editingId!); editingId = null; NotifyDraftState(); }
        public void UndoDraft() { if (!CanUndoDraft) return; Drafts!.Undo(); editingId = null; NotifyDraftState(); }
        public void RedoDraft() { if (!CanRedoDraft) return; Drafts!.Redo(); editingId = null; NotifyDraftState(); }
        public void ImportDraft(DraftDocument document)
        {
            if (Snapshot == null) throw new InvalidOperationException("Mở đúng snapshot trước khi mở nháp.");
            var candidate = new DraftWorkspace(Snapshot); candidate.Import(document);
            Drafts = candidate; editingId = null; NotifyDraftState();
        }
        public void SetElevationThrough(Vec2 first, Vec2 second)
        {
            ViewPlane.ElevationThrough(first, second, Parse(HalfDepth));
            OriginX = first.X.ToString("R", CultureInfo.InvariantCulture); OriginY = first.Y.ToString("R", CultureInfo.InvariantCulture);
            Angle = (Math.Atan2(second.Y-first.Y, second.X-first.X)*180/Math.PI).ToString("R", CultureInfo.InvariantCulture);
            Changed(nameof(OriginX)); Changed(nameof(OriginY)); Changed(nameof(Angle)); Elevation = true;
        }
        public void PreparePlanPointPicking()
        {
            ViewPlane.Plan(Level?.Elevation ?? 0, Parse(HalfDepth));
            Elevation = false;
        }
        private void UpdateDraftOverlays(ViewPlane plane)
        {
            var overlays = new List<DraftOverlay>();
            if (DraftContextActive) {
                var visible = Scene.ToDictionary(e => e.Source.Id, e => e.Source, StringComparer.Ordinal);
                foreach (var draft in DraftRows) if (visible.TryGetValue(draft.ElementId, out var source)) {
                    var overlay = DraftOverlay.Build(draft, source, plane); if (overlay != null) overlays.Add(overlay);
                }
            }
            DraftOverlays = overlays.AsReadOnly(); Changed(nameof(DraftOverlays)); Changed(nameof(EditorTarget));
        }
    }
}
