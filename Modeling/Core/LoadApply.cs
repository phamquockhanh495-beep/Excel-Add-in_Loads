#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class ApplyCapabilities
    {
        public bool FrameDistributed { get; }
        public bool AreaUniform { get; }
        public bool UserWind { get; }
        public string Reason { get; }
        public ApplyCapabilities(bool frameDistributed, bool areaUniform, string reason, bool userWind = false)
        { FrameDistributed = frameDistributed; AreaUniform = areaUniform; Reason = reason; UserWind = userWind; }
        public static ApplyCapabilities ForSource(string source) => source == "ETABS"
            ? new ApplyCapabilities(true, true, "ETABS distributed/uniform/User Loads · bản thử, cần nghiệm thu host", true)
            : new ApplyCapabilities(false, false, "Chưa xác minh SDK/host " + source + "; không mở ghi bằng suy đoán API.");
    }

    public interface ILoadApplyHost
    {
        ApplyCapabilities Capabilities { get; }
        void Guard();
        void GuardElement(ModelElement expected);
        void ValidateDocument(DraftDocument document);
        bool IsLocked { get; }
        Task<ModelSnapshot> ReadSnapshotAsync(CancellationToken token);
        string CreateVerifiedBackup();
        IDisposable UseCanonicalUnits();
        int DeletePattern(ModelElement element, string pattern);
        int Append(ModelElement element, ExistingLoad load);
        IReadOnlyList<ExistingLoad> ReadLoads(ModelElement element);
        void SaveJournal(ApplyReport report);
    }

    public sealed class ApplyGroup
    {
        public ModelElement Element { get; }
        public string Pattern { get; }
        public bool Replace { get; }
        public IReadOnlyList<ExistingLoad> Before { get; }
        public IReadOnlyList<ExistingLoad> After { get; }
        public IReadOnlyList<ExistingLoad> ToAppend { get; }
        public string Status { get; internal set; } = "NotStarted";
        public string Error { get; internal set; } = "";
        internal ApplyGroup(ModelElement element, string pattern, bool replace, ExistingLoad[] before, ExistingLoad[] after, ExistingLoad[] append)
        { Element = element; Pattern = pattern; Replace = replace; Before = before; After = after; ToAppend = append; }
    }

    public sealed class ApplyPlan
    {
        public string Identity { get; }
        public string Fingerprint { get; }
        public int DraftCount { get; }
        public IReadOnlyList<ApplyGroup> Groups { get; }
        private ApplyPlan(ModelSnapshot snapshot, int draftCount, ApplyGroup[] groups)
        { Identity = snapshot.Identity; Fingerprint = SnapshotFingerprint.Of(snapshot); DraftCount = draftCount; Groups = groups; }
        public static ApplyPlan Create(ModelSnapshot fresh, DraftDocument document, ApplyCapabilities capabilities)
        {
            var workspace = new DraftWorkspace(fresh); workspace.Import(document);
            if (document.Loads.Count == 0) throw new InvalidOperationException("Bộ nháp chưa có dòng tải.");
            var elements = fresh.Elements.ToDictionary(e => e.Id, StringComparer.Ordinal);
            var beforeGroups = fresh.Loads.ToLookup(l => (l.ElementId, l.Pattern));
            var expectedGroups = workspace.Compare().Where(r => r.Phase == "Sau").Select(r => r.Load).ToLookup(l => (l.ElementId, l.Pattern));
            var groups = new List<ApplyGroup>();
            foreach (var drafts in document.Loads.GroupBy(l => (l.ElementId, l.Pattern)).OrderBy(g => g.Key.ElementId, StringComparer.Ordinal).ThenBy(g => g.Key.Pattern, StringComparer.Ordinal)) {
                var element = elements[drafts.Key.ElementId];
                if (element.Vertices.Count == 2 ? !capabilities.FrameDistributed : !capabilities.AreaUniform)
                    throw new InvalidOperationException(capabilities.Reason);
                var before = beforeGroups[drafts.Key].ToArray();
                var after = expectedGroups[drafts.Key].ToArray();
                bool replace = drafts.Any(d => d.Action == DraftAction.ReplaceMatching);
                // CSI DeleteLoad* deletes the WHOLE pattern, including other directions/moments.
                // Rebuild it from the preview's full After set; never use SetLoad*(Replace=true).
                foreach (var load in before.Concat(after)) ValidateReplay(element, load);
                groups.Add(new ApplyGroup(element, drafts.Key.Pattern, replace, before, after,
                    replace ? after : drafts.Select(d => d.AsLoad()).ToArray()));
            }
            return new ApplyPlan(fresh, document.Loads.Count, groups.ToArray());
        }
        private static void ValidateReplay(ModelElement element, ExistingLoad load)
        {
            bool frame = element.Vertices.Count == 2;
            if (string.IsNullOrWhiteSpace(element.Name) || element.Id != (frame ? "Frame:" : "Area:") + element.Name ||
                string.IsNullOrWhiteSpace(load.CoordinateSystem) || load.Direction < 1 || load.Direction > 11 ||
                frame && (load.Kind != "Frame force/length" && load.Kind != "Frame moment/length" || load.Start < 0 || load.End > 1 || load.Start >= load.End) ||
                !frame && (load.Kind != "Area uniform" || load.Start != 0 || load.End != 1 || load.Value1 != load.Value2) ||
                load.Unit != (frame ? load.Kind == "Frame force/length" ? "kN/m" : "kN·m/m" : "kN/m²"))
                throw new InvalidOperationException("Không thể đọc lại/giữ nguyên phạm vi tải: " + load.ElementId + "/" + load.Pattern);
        }
    }

    public sealed class ApplyReport
    {
        public string OperationId { get; } = Guid.NewGuid().ToString("N");
        public DateTime StartedUtc { get; } = DateTime.UtcNow;
        public string Identity { get; }
        public string BaselineFingerprint { get; }
        public string BackupPath { get; internal set; } = "";
        public string Outcome { get; internal set; } = "BlockedBeforeWrite";
        public string Error { get; internal set; } = "";
        public bool MayHaveChanges { get; internal set; }
        public bool UnitsRestored { get; internal set; } = true;
        public IReadOnlyList<ApplyGroup> Groups { get; internal set; } = Array.Empty<ApplyGroup>();
        public ApplyReport(DraftDocument document) { Identity = document.SnapshotIdentity; BaselineFingerprint = document.SnapshotFingerprint; }
    }

    public static class LoadApply
    {
        public static async Task<ApplyReport> ExecuteAsync(DraftDocument document, ILoadApplyHost host, CancellationToken token)
        {
            var report = new ApplyReport(document); IDisposable? units = null;
            try {
                host.Guard(); host.ValidateDocument(document); if (host.IsLocked) throw new InvalidOperationException("Mô hình đang khóa; không tự mở khóa để ghi tải.");
                var fresh = await host.ReadSnapshotAsync(token);
                var plan = ApplyPlan.Create(fresh, document, host.Capabilities);
                report.Groups = plan.Groups; token.ThrowIfCancellationRequested(); host.Guard();
                if (host.IsLocked) throw new InvalidOperationException("Mô hình vừa bị khóa.");
                report.BackupPath = host.CreateVerifiedBackup();
                if (string.IsNullOrWhiteSpace(report.BackupPath)) throw new InvalidOperationException("Chưa có backup hợp lệ.");
                report.Outcome = "Prepared"; host.SaveJournal(report);
                report.UnitsRestored = false;
                units = host.UseCanonicalUnits();
                foreach (var group in plan.Groups) {
                    token.ThrowIfCancellationRequested(); host.Guard();
                    host.GuardElement(group.Element);
                    if (host.IsLocked) throw new InvalidOperationException("Mô hình vừa bị khóa.");
                    // Recheck the entire element, not only the target direction, just before mutation.
                    var now = host.ReadLoads(group.Element);
                    if (!LoadEquivalence.Same(group.Before, now.Where(l => l.Pattern == group.Pattern)))
                        throw new InvalidOperationException("Tải nguồn đã đổi sau preflight: " + group.Element.Id + "/" + group.Pattern);
                    group.Status = "InProgress"; host.SaveJournal(report);
                    try {
                        if (group.Replace) {
                            report.MayHaveChanges = true; host.SaveJournal(report);
                            Ok(host.DeletePattern(group.Element, group.Pattern), "DeleteLoad* " + group.Element.Id);
                        }
                        foreach (var load in group.ToAppend) {
                            token.ThrowIfCancellationRequested(); host.Guard(); report.MayHaveChanges = true; host.SaveJournal(report);
                            Ok(host.Append(group.Element, load), "SetLoad* " + group.Element.Id);
                        }
                        var expected = now.Where(l => l.Pattern != group.Pattern).Concat(group.After);
                        if (!LoadEquivalence.Same(expected, host.ReadLoads(group.Element)))
                            throw new InvalidOperationException("Read-back khác preview (gồm cả tải phải giữ).");
                        group.Status = "Verified"; host.SaveJournal(report);
                    } catch (Exception ex) { group.Status = "Failed"; group.Error = ex.Message; throw; }
                }
                report.Outcome = "Succeeded";
            } catch (Exception ex) {
                report.Error = ex.GetBaseException().Message;
                report.Outcome = report.MayHaveChanges ? "StoppedMayHaveChanges" : "BlockedBeforeWrite";
            } finally {
                try { if (units != null) { units.Dispose(); report.UnitsRestored = true; } else if (!report.UnitsRestored) { report.Outcome = "NeedsInspection"; } }
                catch (Exception ex) { report.UnitsRestored = false; report.Error += " | Khôi phục đơn vị: " + ex.Message; report.Outcome = "NeedsInspection"; }
                if (!string.IsNullOrWhiteSpace(report.BackupPath)) {
                    try { host.SaveJournal(report); }
                    catch (Exception ex) { report.Error += " | Lưu báo cáo: " + ex.Message; report.Outcome = "NeedsInspection"; }
                }
            }
            return report;
        }
        private static void Ok(int result, string operation) { if (result != 0) throw new InvalidOperationException(operation + " trả mã " + result); }
    }

    /// <summary>Compare signed physical distributions; tolerate CSI merging/splitting identical load records.</summary>
    public static class LoadEquivalence
    {
        public static bool Same(IEnumerable<ExistingLoad> expected, IEnumerable<ExistingLoad> actual)
        {
            var a = expected.ToLookup(Key); var b = actual.ToLookup(Key);
            foreach (var key in a.Select(g => g.Key).Union(b.Select(g => g.Key))) {
                var left = a[key].ToArray(); var right = b[key].ToArray();
                if (left.Concat(right).Any(l => !Number.Finite(l.Start) || !Number.Finite(l.End) || !Number.Finite(l.Value1) || !Number.Finite(l.Value2))) return false;
                if (key.Kind == "Area uniform") {
                    if (left.Concat(right).Any(l => l.Start != 0 || l.End != 1 || l.Value1 != l.Value2) || !Near(left.Sum(l => l.Value1), right.Sum(l => l.Value1))) return false;
                } else {
                    if (key.Kind != "Frame force/length" && key.Kind != "Frame moment/length" || left.Concat(right).Any(l => l.Start < 0 || l.End > 1 || l.Start >= l.End)) return false;
                    var cuts = left.Concat(right).SelectMany(l => new[] { l.Start, l.End }).Distinct().OrderBy(x => x).ToArray();
                    for (int i = 1; i < cuts.Length; i++) {
                        double start = cuts[i - 1], end = cuts[i], mid = (start + end) / 2;
                        double Sum(ExistingLoad[] loads, double position) => loads.Where(l => l.Start < mid && l.End > mid).Sum(l => l.Value1 + (l.Value2 - l.Value1) * (position - l.Start) / (l.End - l.Start));
                        if (!Near(Sum(left, start), Sum(right, start)) || !Near(Sum(left, end), Sum(right, end))) return false;
                    }
                }
            }
            return true;
        }
        private static (string ElementId, string Pattern, string Kind, string System, int Direction, string Unit) Key(ExistingLoad l)
            => (l.ElementId, l.Pattern, l.Kind, (l.CoordinateSystem ?? "").ToUpperInvariant(), l.Direction, l.Unit);
        private static bool Near(double x, double y) => Number.Finite(x) && Number.Finite(y) && Math.Abs(x - y) <= 1e-6 + 1e-6 * Math.Max(Math.Abs(x), Math.Abs(y));
    }
}
