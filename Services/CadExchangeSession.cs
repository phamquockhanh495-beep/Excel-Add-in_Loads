using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qks.Shared;

namespace QksExcelAddIn.Services
{
    // One export scope per window; model switches invalidate it. No shared result filenames.
    internal sealed class CadExchangeSession
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static bool _uncertain;
        private string _exportId = Guid.NewGuid().ToString("N");
        private string? _modelId;
        private string? _exportStory;
        internal async Task<CadResponse> SendAsync(object document, string legacyCommand, string story, System.Windows.Window owner, int timeoutMs = 180000)
        {
            if (!await Gate.WaitAsync(0)) { if (!owner.IsVisible) owner.Show(); throw new InvalidOperationException("Một lệnh CAD khác đang chạy. Hãy hoàn tất trước khi thao tác tiếp."); }
            try
            {
                if (_uncertain) throw new InvalidOperationException("Phiên CAD trước đã timeout. Hoàn tất/hủy lệnh CAD và khởi động lại Excel trước khi gửi tiếp.");
                dynamic doc = document;
                string documentPath = (string)doc.FullName;
                string modelId = AppConnectionManager.ModelIdentity;
                if (_modelId != modelId) { _modelId = modelId; _exportId = Guid.NewGuid().ToString("N"); }
                string modelFile = AppConnectionManager.ActiveEtabs?.SapModel?.GetModelFilename() ?? "";
                if (!Protocol.SamePath(modelFile, AppConnectionManager.ActiveEtabs?.ModelPath ?? ""))
                    throw new InvalidOperationException("Mô hình ETABS đã thay đổi. Hãy kết nối lại API.");
                int start = legacyCommand.IndexOf(" QKS_", StringComparison.Ordinal);
                if (start < 0) throw new InvalidDataException("Unknown CAD request.");
                string[] tokens = Protocol.Tokenize(legacyCommand.Substring(start + 1));
                bool drawing = tokens[0] == "QKS_WALL_DRAW_BEAMS" || tokens[0] == "QKS_SHELL_DRAW_SLABS";
                if (!drawing && (tokens[0].Contains("UPDATE") || tokens[0].Contains("SCAN") || tokens[0].Contains("SELECT_SECTION")) && _exportStory != story)
                    throw new InvalidOperationException("Hãy xuất mặt bằng tầng đang chọn từ cửa sổ này trước khi nhập hoặc đồng bộ CAD.");
                string scope = drawing ? Guid.NewGuid().ToString("N") : _exportId;
                var request = new CadRequest { DocumentPath = documentPath, ModelId = modelId,
                    ExportId = scope, Action = tokens[0], Arguments = tokens.Skip(1).ToArray() };
                string directory = Path.Combine(Protocol.Root, request.RequestId);
                Directory.CreateDirectory(directory);
                // Freeze JSON payloads before queueing; another window cannot overwrite this request.
                for (int i = 0; i < request.Arguments.Length; i++)
                {
                    if (!request.Arguments[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                    string target = Path.Combine(directory, "payload-" + i + ".json");
                    JsonCodec.WriteAtomic(target, JsonCodec.Read(request.Arguments[i]));
                    request.Arguments[i] = target;
                }
                request.ResponsePath = Path.Combine(directory, "response.json");
                string requestPath = Path.Combine(directory, "request.json");
                Protocol.Validate(request); JsonCodec.WriteAtomic(requestPath, JsonCodec.Serialize(request));
                string dll = AutoCadHelper.ResolvePluginPath(document);
                if (!File.Exists(dll)) throw new FileNotFoundException("Chưa cài plugin đúng phiên bản AutoCAD. Chạy script build và deploy, sau đó khởi động lại CAD.", dll);
                doc.Activate();
                doc.SendCommand("(command \"_NETLOAD\" \"" + dll.Replace('\\', '/') + "\") QKS_EXECUTE \"" + requestPath.Replace('\\', '/') + "\"\n");
                var watch = Stopwatch.StartNew();
                while (!File.Exists(request.ResponsePath))
                {
                    if (watch.ElapsedMilliseconds > timeoutMs)
                    {
                        _uncertain = true;
                        throw new TimeoutException("AutoCAD chưa phản hồi. Hãy hủy/hoàn tất lệnh đang chờ trong CAD; không tự gửi lại thao tác ghi.");
                    }
                    if (AppConnectionManager.ModelIdentity != modelId) throw new InvalidOperationException("Kết nối đã đổi trong lúc CAD xử lý. Bỏ kết quả cũ.");
                    await Task.Delay(100);
                }
                var response = JsonCodec.Deserialize<CadResponse>(JsonCodec.Read(request.ResponsePath));
                Protocol.Validate(request, response);
                if (AppConnectionManager.ModelIdentity != modelId || !Protocol.SamePath((string)doc.FullName, documentPath))
                    throw new InvalidOperationException("Ngữ cảnh đã thay đổi; không dùng kết quả CAD.");
                if (drawing) { _exportId = scope; _exportStory = story; }
                return response;
            }
            finally { Gate.Release(); if (!owner.IsVisible) owner.Show(); }
        }
    }
}
