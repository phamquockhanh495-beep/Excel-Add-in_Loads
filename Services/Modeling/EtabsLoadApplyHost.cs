using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CSiAPIv1;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Services.Modeling
{
    /// <summary>Typed ETABS adapter. No guessed SAFE signatures and no implicit model unlocking.</summary>
    public sealed class EtabsLoadApplyHost : ILoadApplyHost, IUserWindHost
    {
        private readonly EtabsInstanceInfo connection;
        private readonly cSapModel sap;
        private readonly string identity;
        private readonly Action<string> progress;
        private string journalPath = "";
        private bool canonical;
        public ApplyCapabilities Capabilities => ApplyCapabilities.ForSource("ETABS");
        public ILoadApplyHost Model => this;
        public EtabsLoadApplyHost(Action<string> progress)
        {
            connection = AppConnectionManager.ActiveEtabs ?? throw new InvalidOperationException("Chưa kết nối ETABS.");
            sap = AppConnectionManager.GetSapModel() ?? throw new InvalidOperationException("Chưa kết nối ETABS.");
            identity = AppConnectionManager.ModelIdentity; this.progress = progress; Guard();
        }
        public void Guard()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) throw new InvalidOperationException("Apply cần luồng STA.");
            if (!ReferenceEquals(connection, AppConnectionManager.ActiveEtabs) || identity != AppConnectionManager.ModelIdentity ||
                !Protocol.SamePath(sap.GetModelFilename(), connection.ModelPath)) throw new InvalidOperationException("Kết nối/mô hình đã đổi. Đọc lại nguồn.");
            if (canonical && sap.GetPresentUnits() != eUnits.kN_m_C) throw new InvalidOperationException("Đơn vị đã đổi trong lúc Apply.");
        }
        public bool IsLocked { get { Guard(); return sap.GetModelIsLocked(); } }
        public void ValidateDocument(DraftDocument document)
        {
            Guard();
            foreach (var pattern in document.Loads.Where(l => l.Kind == DraftKind.WindLine).Select(l => l.Pattern).Distinct(StringComparer.Ordinal)) {
                eLoadPatternType type = default; string code = "";
                if (sap.LoadPatterns.GetLoadType(pattern, ref type) != 0 || type != eLoadPatternType.Wind)
                    throw new InvalidOperationException("Gió dầm biên cần pattern loại Wind: " + pattern);
                if (sap.LoadPatterns.GetAutoWindCode(pattern, ref code) != 0) throw new InvalidOperationException("Không đọc được Auto Wind Code: " + pattern);
                if (string.Equals(code.Trim(), "User Loads", StringComparison.OrdinalIgnoreCase)) {
                    if (ReadUserWind(pattern).Rows.Any(r => r.Fx != 0 || r.Fy != 0 || r.Mz != 0))
                        throw new InvalidOperationException("Pattern đang có User Loads; dùng pattern riêng để tránh cộng gió hai phương pháp: " + pattern);
                } else if (!string.IsNullOrWhiteSpace(code) && !string.Equals(code.Trim(), "None", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Pattern đang sinh tải tự động. Dùng Wind/None riêng cho tải dầm biên: " + pattern);
            }
        }
        public void GuardElement(ModelElement expected)
        {
            Guard(); if (!canonical) throw new InvalidOperationException("Kiểm tra hình học cần đơn vị chuẩn.");
            void Ok(int rc) { if (rc != 0) throw new InvalidOperationException("Không xác nhận được hình học đích " + expected.Id + " (API " + rc + ")"); }
            string[] pointNames;
            if (expected.Vertices.Count == 2) {
                string a = "", b = ""; Ok(sap.FrameObj.GetPoints(expected.Name, ref a, ref b)); pointNames = new[] { a, b };
                eFrameDesignOrientation orientation = default; Ok(sap.FrameObj.GetDesignOrientation(expected.Name, ref orientation));
                if (orientation != eFrameDesignOrientation.Beam) throw new InvalidOperationException("Đích không còn là dầm: " + expected.Id);
            } else {
                int count = 0; string[] names = Array.Empty<string>(); Ok(sap.AreaObj.GetPoints(expected.Name, ref count, ref names));
                if (count != expected.Vertices.Count || names == null || names.Length < count) throw new InvalidOperationException("Số đỉnh area đã đổi.");
                pointNames = names.Take(count).ToArray(); bool opening = false; Ok(sap.AreaObj.GetOpening(expected.Name, ref opening));
                eAreaDesignOrientation orientation = default; Ok(sap.AreaObj.GetDesignOrientation(expected.Name, ref orientation));
                if (opening || orientation != eAreaDesignOrientation.Floor) throw new InvalidOperationException("Đích không còn là sàn: " + expected.Id);
            }
            for (int i = 0; i < pointNames.Length; i++) {
                double x = 0, y = 0, z = 0; Ok(sap.PointObj.GetCoordCartesian(pointNames[i], ref x, ref y, ref z, "Global"));
                var delta = new Vec3(x, y, z) - expected.Vertices[i];
                if (!delta.IsFinite || delta.Length > 1e-6) throw new InvalidOperationException("Hình học đích đã đổi sau preflight: " + expected.Id);
            }
            Guard();
        }
        public async Task<ModelSnapshot> ReadSnapshotAsync(CancellationToken token)
        {
            Guard(); var snapshot = await new EtabsSnapshotReader().ReadAsync(progress, token, true); Guard(); return snapshot;
        }
        public string CreateVerifiedBackup()
        {
            Guard(); string source = sap.GetModelFilename();
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) throw new IOException("Lưu mô hình ETABS trước khi Apply.");
            if (sap.File.Save(source) != 0) throw new IOException("ETABS không lưu được model trước khi backup.");
            Guard();
            string directory = Path.Combine(Path.GetDirectoryName(source)!, "QKS_Backups"); Directory.CreateDirectory(directory);
            string stem = Path.GetFileNameWithoutExtension(source) + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            string backup = Path.Combine(directory, stem + Path.GetExtension(source));
            File.Copy(source, backup, false);
            string Hash(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)); }
            if (new FileInfo(backup).Length == 0 || Hash(source) != Hash(backup)) throw new IOException("Backup không khớp SHA-256; dừng trước khi gán.");
            journalPath = Path.Combine(directory, stem + ".apply-report.json"); return backup;
        }
        public IDisposable UseCanonicalUnits()
        {
            Guard(); var old = sap.GetPresentUnits();
            if (sap.SetPresentUnits(eUnits.kN_m_C) != 0) {
                int restored = sap.SetPresentUnits(old);
                throw new InvalidOperationException("Không đặt được đơn vị kN-m; mã khôi phục " + restored + ". Kiểm tra đơn vị model.");
            }
            canonical = true;
            return new RestoreUnits(() => { canonical = false; if (sap.SetPresentUnits(old) != 0) throw new InvalidOperationException("ETABS không khôi phục được đơn vị ban đầu."); });
        }
        public int DeletePattern(ModelElement element, string pattern)
        {
            Guard(); return element.Vertices.Count == 2 ? sap.FrameObj.DeleteLoadDistributed(element.Name, pattern, eItemType.Objects) :
                sap.AreaObj.DeleteLoadUniform(element.Name, pattern, eItemType.Objects);
        }
        public int Append(ModelElement element, ExistingLoad load)
        {
            Guard(); return element.Vertices.Count == 2 ? sap.FrameObj.SetLoadDistributed(element.Name, load.Pattern,
                load.Kind == "Frame force/length" ? 1 : 2, load.Direction, load.Start, load.End, load.Value1, load.Value2,
                load.CoordinateSystem, true, false, eItemType.Objects) :
                sap.AreaObj.SetLoadUniform(element.Name, load.Pattern, load.Value1, load.Direction, false, load.CoordinateSystem, eItemType.Objects);
        }
        public IReadOnlyList<ExistingLoad> ReadLoads(ModelElement element)
        {
            Guard(); if (!canonical) throw new InvalidOperationException("Read-back cần đơn vị chuẩn.");
            var loads = new List<ExistingLoad>(); var scale = new UnitScale("kN", "m");
            void Error(int code, string message) { if (code != 0) throw new InvalidOperationException(message + " (API " + code + ")"); }
            if (element.Vertices.Count == 2) EtabsSnapshotReader.ReadFrameLoads(sap, element, scale, loads, Error);
            else EtabsSnapshotReader.ReadAreaLoads(sap, element, scale, loads, Error);
            return loads.AsReadOnly();
        }
        public UserWindState ReadUserWind(string pattern)
        {
            Guard(); eLoadPatternType type = default; string code = "";
            void Ok(int rc, string operation) { if (rc != 0) throw new InvalidOperationException(operation + " trả mã " + rc); }
            Ok(sap.LoadPatterns.GetLoadType(pattern, ref type), "GetLoadType");
            if (type != eLoadPatternType.Wind) throw new InvalidOperationException("Chọn Load Pattern loại Wind.");
            Ok(sap.LoadPatterns.GetAutoWindCode(pattern, ref code), "GetAutoWindCode");
            if (!string.Equals(code, "User Loads", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Chọn Auto Lateral Load = User Loads trong ETABS. API trả: " + code);
            eForce force = default; eLength length = default; eTemperature temperature = default;
            Ok(sap.GetPresentUnits_2(ref force, ref length, ref temperature), "GetPresentUnits_2");
            var scale = new UnitScale(force.ToString(), length.ToString());
            int count = 0; string[] keys = Array.Empty<string>();
            double[] fx = Array.Empty<double>(), fy = Array.Empty<double>(), mz = Array.Empty<double>(), x = Array.Empty<double>(), y = Array.Empty<double>();
            Ok(sap.LoadPatterns.AutoWind.GetUserLoad(pattern, ref count, ref keys, ref fx, ref fy, ref mz, ref x, ref y), "AutoWind.GetUserLoad");
            if (count < 0 || new Array[] { keys, fx, fy, mz, x, y }.Any(a => a == null ? count != 0 : a.Length < count)) throw new InvalidOperationException("Mảng User Loads không đồng bộ.");
            return new UserWindState(pattern, code, Enumerable.Range(0, count).Select(i => new UserWindRow(keys[i],
                fx[i] * scale.ForceToKn, fy[i] * scale.ForceToKn, mz[i] * scale.ForceToKn * scale.LengthToMetres, x[i] * scale.LengthToMetres, y[i] * scale.LengthToMetres)));
        }
        public int SetUserWind(string pattern, UserWindRow row)
        {
            Guard(); if (!canonical) throw new InvalidOperationException("SetUserLoad cần đơn vị kN-m.");
            return sap.LoadPatterns.AutoWind.SetUserLoad(pattern, row.DiaphragmKey, row.Fx, row.Fy, row.Mz, row.X, row.Y);
        }
        public void SaveJournal(ApplyReport report) => WriteJournal(report);
        public void SaveUserWindJournal(UserWindReport report) => WriteJournal(report);
        private void WriteJournal(object report)
        {
            if (string.IsNullOrWhiteSpace(journalPath)) throw new IOException("Chưa có đường dẫn journal.");
            string temporary = journalPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                JsonCodec.WriteAtomic(temporary, JsonCodec.Serialize(report));
                if (File.Exists(journalPath)) File.Replace(temporary, journalPath, null); else File.Move(temporary, journalPath);
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private sealed class RestoreUnits : IDisposable
        {
            private Action? restore;
            internal RestoreUnits(Action restore) { this.restore = restore; }
            public void Dispose() { var action = restore; restore = null; action?.Invoke(); }
        }
    }
}
