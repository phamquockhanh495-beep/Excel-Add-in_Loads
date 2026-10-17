#nullable enable
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public bool Offline { get; private set; } = true;
        private bool applyNeedsRead;
        public ApplyReport? LastApplyReport { get; private set; }
        public object? LastWriteReport { get; private set; }
        private string lastWriteSummary = "";
        public bool CanApply => !Busy && !Offline && !applyNeedsRead && DraftContextActive && DraftRows.Count > 0 && Snapshot?.Source == "ETABS";
        public string ApplySummary => lastWriteSummary.Length == 0 ? "Apply ETABS: kiểm tra nguồn → backup → ghi → đọc lại. SAFE chưa mở ghi." :
            lastWriteSummary + (applyNeedsRead ? " · Đọc lại model trước lần ghi tiếp." : "");
        private void NotifyApply() { Changed(nameof(CanApply)); Changed(nameof(ApplySummary)); Changed(nameof(LastApplyReport)); }
        public void SetApplyReport(ApplyReport report)
        {
            LastApplyReport = report;
            LastWriteReport = report; lastWriteSummary = report.Outcome + " · " + report.Error;
            applyNeedsRead = report.MayHaveChanges || !report.UnitsRestored;
            NotifyApply();
            NotifyWind();
        }
    }
}
