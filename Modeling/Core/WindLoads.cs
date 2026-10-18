#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qks.Shared;
using Newtonsoft.Json;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class UserWindRow
    {
        [JsonProperty(Required = Required.Always)]
        public string DiaphragmKey { get; }
        [JsonProperty(Required = Required.Always)]
        public double Fx { get; }
        [JsonProperty(Required = Required.Always)]
        public double Fy { get; }
        [JsonProperty(Required = Required.Always)]
        public double Mz { get; }
        [JsonProperty(Required = Required.Always)]
        public double X { get; }
        [JsonProperty(Required = Required.Always)]
        public double Y { get; }
        public UserWindRow(string diaphragmKey, double fx, double fy, double mz, double x, double y)
        {
            if (string.IsNullOrWhiteSpace(diaphragmKey) || new[] { fx, fy, mz, x, y }.Any(v => !Number.Finite(v))) throw new ArgumentException("Khóa diaphragm và Fx/Fy/Mz/X/Y phải hợp lệ.");
            DiaphragmKey = diaphragmKey; Fx = fx; Fy = fy; Mz = mz; X = x; Y = y;
        }
        public UserWindRow TransferTo(double x, double y) => new UserWindRow(DiaphragmKey, Fx, Fy, Mz + (X - x) * Fy - (Y - y) * Fx, x, y);
    }
    public sealed class UserWindState
    {
        [JsonProperty(Required = Required.Always)]
        public string Pattern { get; }
        [JsonProperty(Required = Required.Always)]
        public string Code { get; }
        [JsonProperty(Required = Required.Always)]
        public IReadOnlyList<UserWindRow> Rows { get; }
        [JsonIgnore]
        public string Fingerprint => SnapshotFingerprint.Hash(JsonCodec.Serialize(new { Pattern, Code, Rows = Rows.OrderBy(r => r.DiaphragmKey, StringComparer.Ordinal) }));
        public UserWindState(string pattern, string code, IEnumerable<UserWindRow> rows)
        {
            Pattern = pattern; Code = (code ?? "").Trim(); Rows = rows.ToArray();
            if (string.IsNullOrWhiteSpace(pattern) || Rows.Any(r => r == null) || Rows.Select(r => r.DiaphragmKey).Distinct(StringComparer.Ordinal).Count() != Rows.Count)
                throw new ArgumentException("Pattern/khóa User Loads thiếu hoặc trùng.");
        }
    }
    public sealed class UserWindDocument
    {
        [JsonProperty(Required = Required.Always)]
        public int SchemaVersion { get; }
        [JsonProperty(Required = Required.Always)]
        public string Identity { get; }
        [JsonProperty(Required = Required.Always)]
        public string ModelFingerprint { get; }
        [JsonProperty(Required = Required.Always)]
        public UserWindState Before { get; }
        [JsonProperty(Required = Required.Always)]
        public IReadOnlyList<UserWindRow> Changes { get; }
        public string Basis { get; }
        public IReadOnlyDictionary<string,string> StoryAssignments { get; }
        public UserWindDocument(string identity, string modelFingerprint, UserWindState before, IEnumerable<UserWindRow> changes, string basis = "", int schemaVersion = 1, IReadOnlyDictionary<string,string>? storyAssignments = null)
        { SchemaVersion = schemaVersion; Identity = identity; ModelFingerprint = modelFingerprint; Before = before; Changes = changes.ToArray(); Basis = basis ?? ""; StoryAssignments=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>((storyAssignments??new Dictionary<string,string>()).ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal)); }
        public UserWindState Expected()
        {
            if (SchemaVersion != 1) throw new ArgumentException("Schema nháp User Loads chưa hỗ trợ.");
            if (!string.Equals(Before.Code, "User Loads", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Pattern phải có Auto Lateral Load = User Loads; không tự chuyển pattern tiêu chuẩn khác.");
            if (Changes.Count > 10000 || Changes.Any(r => r == null) || Changes.Select(r => r.DiaphragmKey).Distinct(StringComparer.Ordinal).Count() != Changes.Count)
                throw new ArgumentException("Chưa có thay đổi User Loads hoặc khóa bị trùng.");
            var known = new HashSet<string>(Before.Rows.Select(r => r.DiaphragmKey), StringComparer.Ordinal);
            if (Changes.Any(r => !known.Contains(r.DiaphragmKey))) throw new InvalidOperationException("Chỉ sửa khóa diaphragm đã đọc từ ETABS; chưa tự tạo khóa theo tên tầng.");
            if(StoryAssignments.Any(p=>!known.Contains(p.Key)||string.IsNullOrWhiteSpace(p.Value)))throw new ArgumentException("Ánh xạ Story–diaphragm không hợp lệ.");
            var updates = Changes.ToDictionary(r => r.DiaphragmKey, StringComparer.Ordinal);
            return new UserWindState(Before.Pattern, Before.Code, Before.Rows.Select(r => updates.TryGetValue(r.DiaphragmKey, out var row) ? row : r));
        }
    }
    public interface IUserWindHost
    {
        ILoadApplyHost Model { get; }
        UserWindState ReadUserWind(string pattern);
        int SetUserWind(string pattern, UserWindRow row);
        void SaveUserWindJournal(UserWindReport report);
    }
    public sealed class UserWindReport
    {
        public string OperationId { get; } = Guid.NewGuid().ToString("N");
        public DateTime StartedUtc { get; } = DateTime.UtcNow;
        public UserWindDocument Document { get; }
        public string BackupPath { get; internal set; } = "";
        public string Outcome { get; internal set; } = "BlockedBeforeWrite";
        public string Error { get; internal set; } = "";
        public bool MayHaveChanges { get; internal set; }
        public bool UnitsRestored { get; internal set; } = true;
        public List<string> VerifiedKeys { get; } = new List<string>();
        public UserWindReport(UserWindDocument document) { Document = document; }
    }
    public static class UserWindApply
    {
        public static async Task<UserWindReport> ExecuteAsync(UserWindDocument document, IUserWindHost host, CancellationToken token)
        {
            var report = new UserWindReport(document); IDisposable? units = null;
            try {
                if (document.Changes.Count == 0) throw new InvalidOperationException("Chưa có thay đổi User Loads để ghi.");
                var expected = document.Expected(); var model = host.Model; model.Guard();
                if (!model.Capabilities.UserWind || model.IsLocked) throw new InvalidOperationException("Host không cho phép ghi User Loads hoặc model đang khóa.");
                var fresh = await model.ReadSnapshotAsync(token);
                if (fresh.Identity != document.Identity || SnapshotFingerprint.Of(fresh) != document.ModelFingerprint) throw new InvalidOperationException("Nguồn hình học/tải đã đổi. Đọc lại User Loads.");
                if (!fresh.LoadPatterns.Contains(document.Before.Pattern, StringComparer.Ordinal)) throw new InvalidOperationException("Pattern User Loads chưa có trong nguồn.");
                if(document.StoryAssignments.Values.Any(story=>!fresh.Levels.Any(l=>l.Name==story)))throw new InvalidOperationException("Story của dữ liệu gió không có trong mô hình hiện tại.");
                if (fresh.Loads.Any(l => l.Pattern == document.Before.Pattern && (l.Value1 != 0 || l.Value2 != 0)))
                    throw new InvalidOperationException("Pattern đang có tải trực tiếp lên cấu kiện; dùng pattern riêng cho User Loads để tránh cộng hai cách gán gió.");
                token.ThrowIfCancellationRequested(); model.Guard();
                if (host.ReadUserWind(document.Before.Pattern).Fingerprint != document.Before.Fingerprint) throw new InvalidOperationException("User Loads đã đổi sau khi đọc.");
                report.BackupPath = model.CreateVerifiedBackup();
                if (string.IsNullOrWhiteSpace(report.BackupPath)) throw new InvalidOperationException("Backup chưa được xác nhận.");
                host.SaveUserWindJournal(report);
                report.UnitsRestored = false; units = model.UseCanonicalUnits();
                // Save/backup may allow CSI-side changes. Recheck before the first assignment.
                if (host.ReadUserWind(document.Before.Pattern).Fingerprint != document.Before.Fingerprint) throw new InvalidOperationException("User Loads đổi trong lúc backup.");
                var working = document.Before;
                foreach (var row in document.Changes) {
                    token.ThrowIfCancellationRequested(); model.Guard();
                    if (model.IsLocked || !Same(working, host.ReadUserWind(document.Before.Pattern))) throw new InvalidOperationException("User Loads/model đổi trong lúc Apply.");
                    report.MayHaveChanges = true; host.SaveUserWindJournal(report);
                    int rc = host.SetUserWind(document.Before.Pattern, row);
                    if (rc != 0) throw new InvalidOperationException("AutoWind.SetUserLoad trả mã " + rc + ": " + row.DiaphragmKey);
                    working = new UserWindState(working.Pattern, working.Code, working.Rows.Select(r => r.DiaphragmKey == row.DiaphragmKey ? row : r));
                    if (!Same(working, host.ReadUserWind(working.Pattern))) throw new InvalidOperationException("Read-back User Loads không khớp; đã dừng.");
                    report.VerifiedKeys.Add(row.DiaphragmKey); host.SaveUserWindJournal(report);
                }
                if (!Same(expected, host.ReadUserWind(expected.Pattern))) throw new InvalidOperationException("Read-back cuối khác preview.");
                report.Outcome = "Succeeded";
            } catch (Exception ex) { report.Error = ex.GetBaseException().Message; report.Outcome = report.MayHaveChanges ? "StoppedMayHaveChanges" : "BlockedBeforeWrite"; }
            finally {
                try { if (units != null) { units.Dispose(); report.UnitsRestored = true; } else if (!report.UnitsRestored) report.Outcome = "NeedsInspection"; }
                catch (Exception ex) { report.Error += " | Đơn vị: " + ex.Message; report.Outcome = "NeedsInspection"; }
                if (report.BackupPath.Length > 0) try { host.SaveUserWindJournal(report); } catch (Exception ex) { report.Error += " | Journal: " + ex.Message; report.Outcome = "NeedsInspection"; }
            }
            return report;
        }
        public static bool Same(UserWindState a, UserWindState b)
        {
            if (a.Pattern != b.Pattern || !string.Equals(a.Code, b.Code, StringComparison.OrdinalIgnoreCase) || a.Rows.Count != b.Rows.Count) return false;
            var rows = b.Rows.ToDictionary(r => r.DiaphragmKey, StringComparer.Ordinal);
            bool Near(double x, double y) => Math.Abs(x - y) <= 1e-6 + Math.Max(Math.Abs(x), Math.Abs(y)) * 1e-6;
            return a.Rows.All(r => rows.TryGetValue(r.DiaphragmKey, out var s) && Near(r.Fx, s.Fx) && Near(r.Fy, s.Fy) && Near(r.Mz, s.Mz) && Near(r.X, s.X) && Near(r.Y, s.Y));
        }
    }
}
