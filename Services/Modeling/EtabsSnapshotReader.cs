using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Services.Modeling
{
    /// <summary>Read-only adapter. All COM access stays on the caller's WPF STA; no Task.Run or unit changes.</summary>
    public sealed class EtabsSnapshotReader
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        public async Task<ModelSnapshot> ReadAsync(Action<string> progress, CancellationToken token)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) throw new InvalidOperationException("Reader cần luồng STA.");
            if (!await Gate.WaitAsync(0, token)) throw new InvalidOperationException("Một cửa sổ khác đang đọc mô hình. Hãy chờ đọc xong.");
            try { return await ReadCoreAsync(progress, token); }
            finally { Gate.Release(); }
        }
        private static void Ok(int code, string call) { if (code != 0) throw new InvalidOperationException(call + " trả mã " + code); }
        private static string[] Names(int code, int count, string[] names, string call)
        {
            Ok(code, call);
            if (count < 0 || names == null && count != 0 || names != null && names.Length < count) throw new InvalidOperationException(call + ": danh sách không hợp lệ.");
            return (names ?? Array.Empty<string>()).Take(count).ToArray();
        }
        private static void Arrays(int count, params Array[] arrays)
        {
            if (count < 0 || arrays.Any(a => a == null ? count != 0 : a.Length < count)) throw new InvalidOperationException("API trả mảng tải không đồng bộ.");
        }
        private async Task<ModelSnapshot> ReadCoreAsync(Action<string> progress, CancellationToken token)
        {
            var connection = AppConnectionManager.ActiveEtabs ?? throw new InvalidOperationException("Hãy dùng Kết nối API để chọn ETABS trước.");
            var sap = AppConnectionManager.GetSapModel() ?? throw new InvalidOperationException("ETABS chưa kết nối.");
            string identity = AppConnectionManager.ModelIdentity;
            if (string.IsNullOrWhiteSpace(connection.ModelPath)) throw new InvalidOperationException("Hãy lưu mô hình ETABS trước khi đọc.");
            eForce force = default; eLength length = default; eTemperature temperature = default;
            Ok(sap.GetPresentUnits_2(ref force, ref length, ref temperature), "GetPresentUnits_2");
            var scale = new UnitScale(force.ToString(), length.ToString());
            void Guard() {
                token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(connection, AppConnectionManager.ActiveEtabs) || identity != AppConnectionManager.ModelIdentity ||
                    !Qks.Shared.Protocol.SamePath(sap.GetModelFilename(), connection.ModelPath)) throw new InvalidOperationException("Kết nối/mô hình đã đổi. Hãy đọc lại.");
                eForce f = default; eLength l = default; eTemperature t = default;
                Ok(sap.GetPresentUnits_2(ref f, ref l, ref t), "GetPresentUnits_2");
                if (f != force || l != length || t != temperature) throw new InvalidOperationException("Đơn vị ETABS đã đổi trong lúc đọc. Hãy đọc lại.");
            }
            async Task Yield(string message) {
                progress(message); await Dispatcher.Yield(DispatcherPriority.Background); Guard();
            }
            var diagnostics = new List<string> {
                "Phạm vi tải đã đọc: frame distributed và area uniform. Chưa gồm joint/point load, frame point, temperature, surface pressure hoặc tải sinh tự động.",
                "Ảnh chụp dữ liệu chỉ đọc; không chỉnh mô hình/đơn vị trong khi đọc. API không cung cấp revision để phát hiện mọi thay đổi hình học cùng file.",
                "Trục lưới/named elevation chưa nhập; mặt đứng hiện dùng tọa độ gốc và góc phương. Hình học hiển thị đường trục/biên, chưa extrusion tiết diện."
            };
            int optionalErrorCount = 0;
            void Optional(int code, string message) {
                if (code == 0) return;
                optionalErrorCount++;
                if (diagnostics.Count < 200) diagnostics.Add(message + " (API " + code + ")");
            }
            var points = new Dictionary<string, Vec3>(StringComparer.Ordinal);
            Vec3 Point(string name) {
                if (points.TryGetValue(name, out var p)) return p;
                double x = 0, y = 0, z = 0;
                Ok(sap.PointObj.GetCoordCartesian(name, ref x, ref y, ref z, "Global"), "GetCoordCartesian " + name);
                p = new Vec3(x * scale.LengthToMetres, y * scale.LengthToMetres, z * scale.LengthToMetres);
                if (!p.IsFinite) throw new InvalidOperationException("Tọa độ không hữu hạn: " + name);
                points.Add(name, p); return p;
            }
            var elements = new List<ModelElement>(); var loads = new List<ExistingLoad>(); var levels = new List<ModelLevel>();
            int count = 0; string[] names = Array.Empty<string>();
            int rc = sap.Story.GetNameList(ref count, ref names);
            foreach (string name in Names(rc, count, names, "Story.GetNameList")) {
                double z = 0; Ok(sap.Story.GetElevation(name, ref z), "Story.GetElevation " + name); levels.Add(new ModelLevel(name, z * scale.LengthToMetres));
            }
            count = 0; names = Array.Empty<string>(); rc = sap.FrameObj.GetNameList(ref count, ref names);
            var frames = Names(rc, count, names, "FrameObj.GetNameList");
            for (int i = 0; i < frames.Length; i++) {
                Guard(); string name = frames[i], a = "", b = "", property = "", auto = "", label = "", story = "";
                Ok(sap.FrameObj.GetPoints(name, ref a, ref b), "FrameObj.GetPoints " + name);
                Optional(sap.FrameObj.GetSection(name, ref property, ref auto), "Không đọc được tiết diện frame " + name);
                Optional(sap.FrameObj.GetLabelFromName(name, ref label, ref story), "Không đọc được label/story frame " + name);
                double angle = 0; bool advanced = false; int axisResult = sap.FrameObj.GetLocalAxes(name, ref angle, ref advanced);
                Optional(axisResult, "Không đọc được local axes frame " + name);
                var pa = Point(a); var pb = Point(b);
                eFrameDesignOrientation orientation = default;
                int orientationResult = sap.FrameObj.GetDesignOrientation(name, ref orientation);
                Optional(orientationResult, "Không đọc được design orientation frame " + name);
                ElementKind kind = orientationResult != 0 ? ElementKind.Other : orientation == eFrameDesignOrientation.Beam ? ElementKind.Beam :
                    orientation == eFrameDesignOrientation.Column ? ElementKind.Column : orientation == eFrameDesignOrientation.Brace ? ElementKind.Brace : ElementKind.Other;
                var element = new ModelElement("Frame:" + name, name, label, story, property, kind, new[] { pa, pb }, axisResult == 0 ? (double?)angle : null, advanced);
                elements.Add(element);
                ReadFrameLoads(sap, element, scale, loads, Optional);
                if (i % 25 == 0) await Yield("Đọc frame " + (i + 1) + "/" + frames.Length);
            }
            count = 0; names = Array.Empty<string>(); rc = sap.AreaObj.GetNameList(ref count, ref names);
            var areas = Names(rc, count, names, "AreaObj.GetNameList");
            for (int i = 0; i < areas.Length; i++) {
                Guard(); string name = areas[i], property = "", label = "", story = "";
                int n = 0; string[] corners = Array.Empty<string>(); bool opening = false;
                rc = sap.AreaObj.GetPoints(name, ref n, ref corners);
                var vertices = Names(rc, n, corners, "AreaObj.GetPoints " + name).Select(Point).ToArray();
                if (vertices.Length < 3) throw new InvalidOperationException("Area thiếu đỉnh: " + name);
                Ok(sap.AreaObj.GetOpening(name, ref opening), "AreaObj.GetOpening " + name);
                Optional(sap.AreaObj.GetProperty(name, ref property), "Không đọc được property area " + name);
                Optional(sap.AreaObj.GetLabelFromName(name, ref label, ref story), "Không đọc được label/story area " + name);
                eAreaDesignOrientation orientation = default;
                int orientationResult = sap.AreaObj.GetDesignOrientation(name, ref orientation);
                Optional(orientationResult, "Không đọc được design orientation area " + name);
                ElementKind kind = opening ? ElementKind.Opening : orientationResult != 0 ? ElementKind.Other :
                    orientation == eAreaDesignOrientation.Floor ? ElementKind.Slab : orientation == eAreaDesignOrientation.Wall ? ElementKind.Wall : ElementKind.Other;
                var element = new ModelElement("Area:" + name, name, label, story, property, kind, vertices);
                elements.Add(element); ReadAreaLoads(sap, element, scale, loads, Optional);
                if (i % 25 == 0) await Yield("Đọc area " + (i + 1) + "/" + areas.Length);
            }
            Guard();
            if (levels.Count == 0) levels.AddRange(points.Values.Select(p => p.Z).Distinct().OrderBy(z => z).Select(z => new ModelLevel("Z=" + z.ToString("0.###"), z)));
            string version = ""; double versionNumber = 0;
            Optional(sap.GetVersion(ref version, ref versionNumber), "Không đọc được phiên bản ETABS");
            if (optionalErrorCount > 0) diagnostics.Add("Tổng số lỗi/không hỗ trợ metadata hoặc tải tùy chọn: " + optionalErrorCount + ". Giới hạn 197 thông báo chi tiết; không coi tải thiếu là 0.");
            progress("Đọc xong " + elements.Count + " đối tượng.");
            return new ModelSnapshot(1, "ETABS", version, identity, force + "/" + length + "/" + temperature,
                DateTime.UtcNow, elements, levels.OrderBy(l => l.Elevation), loads, diagnostics);
        }
        private static void ReadFrameLoads(cSapModel sap, ModelElement e, UnitScale scale, List<ExistingLoad> loads, Action<int, string> optional)
        {
            int n = 0; string[] names = Array.Empty<string>(), patterns = Array.Empty<string>(), systems = Array.Empty<string>();
            int[] types = Array.Empty<int>(), directions = Array.Empty<int>();
            double[] r1 = Array.Empty<double>(), r2 = Array.Empty<double>(), d1 = Array.Empty<double>(), d2 = Array.Empty<double>(), v1 = Array.Empty<double>(), v2 = Array.Empty<double>();
            int rc = sap.FrameObj.GetLoadDistributed(e.Name, ref n, ref names, ref patterns, ref types, ref systems, ref directions, ref r1, ref r2, ref d1, ref d2, ref v1, ref v2, eItemType.Objects);
            if (rc != 0) { optional(rc, "Chưa xác nhận tải distributed " + e.Id); return; }
            Arrays(n, names, patterns, types, systems, directions, r1, r2, d1, d2, v1, v2);
            for (int i = 0; i < n; i++) {
                if (!string.Equals(names[i], e.Name, StringComparison.Ordinal)) throw new InvalidOperationException("API trả tải của frame khác: " + e.Id);
                if (types[i] != 1 && types[i] != 2) { optional(-1, "Loại tải distributed chưa hỗ trợ " + e.Id); continue; }
                // Type 2 is distributed moment: force*length / length = force.
                double factor = scale.DistributedFactor(types[i]);
                loads.Add(new ExistingLoad(e.Id, patterns[i], types[i] == 1 ? "Frame force/length" : "Frame moment/length", systems[i], directions[i], r1[i], r2[i], v1[i] * factor, v2[i] * factor, types[i] == 1 ? "kN/m" : "kN·m/m"));
            }
        }
        private static void ReadAreaLoads(cSapModel sap, ModelElement e, UnitScale scale, List<ExistingLoad> loads, Action<int, string> optional)
        {
            int n = 0; string[] names = Array.Empty<string>(), patterns = Array.Empty<string>(), systems = Array.Empty<string>();
            int[] directions = Array.Empty<int>(); double[] values = Array.Empty<double>();
            int rc = sap.AreaObj.GetLoadUniform(e.Name, ref n, ref names, ref patterns, ref systems, ref directions, ref values, eItemType.Objects);
            if (rc != 0) { optional(rc, "Chưa xác nhận tải uniform " + e.Id); return; }
            Arrays(n, names, patterns, systems, directions, values);
            for (int i = 0; i < n; i++) {
                if (!string.Equals(names[i], e.Name, StringComparison.Ordinal)) throw new InvalidOperationException("API trả tải của area khác: " + e.Id);
                loads.Add(new ExistingLoad(e.Id, patterns[i], "Area uniform", systems[i], directions[i], 0, 1, values[i] * scale.Pressure, values[i] * scale.Pressure, "kN/m²"));
            }
        }
    }
}
