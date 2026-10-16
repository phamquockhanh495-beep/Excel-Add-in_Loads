using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Service kết nối tốc độ cao với AutoCAD Plugin qua .NET Named Pipes.
    /// Giúp truyền tải hàng ngàn dữ liệu dầm/tường trực tiếp trên bộ nhớ mà không thông qua COM.
    /// </summary>
    public class AutoCadIpcService
    {
        private const string PipeName = "QksExcelAutoCadPipe";
        private const int DefaultConnectionTimeout = 3000; // ms

        /// <summary>
        /// Tự động nạp Plugin QKS_Loads_AcadPlugin.dll vào AutoCAD qua COM NETLOAD nếu chưa được nạp
        /// </summary>
        public static bool EnsurePluginLoaded()
        {
            try
            {
                string pluginDll = @"C:\QKS_JSON_Data\Loads\QKS_Loads_AcadPlugin.dll";
                if (!File.Exists(pluginDll)) return false;

                dynamic? doc = AutoCadHelper.GetActiveAutoCadDoc();

                if (doc != null)
                {
                    string dllEscaped = pluginDll.Replace("\\", "\\\\");
                    doc.SendCommand($"(command \"NETLOAD\" \"{dllEscaped}\")\n");
                    System.Threading.Thread.Sleep(1000);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi nạp plugin AutoCAD qua COM: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Gửi yêu cầu truy vấn đến AutoCAD Plugin và nhận lại kết quả dạng chuỗi JSON
        /// </summary>
        public async Task<string> SendQueryAsync(string requestJson, int timeoutMs = 15000)
        {
            // Tự động lưu bản sao JSON vào thư mục C:\QKS_JSON_Data\Loads\
            try
            {
                string loadsDir = @"C:\QKS_JSON_Data\Loads";
                if (!Directory.Exists(loadsDir)) Directory.CreateDirectory(loadsDir);

                if (requestJson.Contains("EXPORT_WALL_LOADS_TO_CAD") || requestJson.Contains("DrawBeams"))
                {
                    File.WriteAllText(Path.Combine(loadsDir, "wall_load_export.json"), requestJson, Encoding.UTF8);
                }
            }
            catch { }

            // Thử kết nối IPC lần 1
            string result = await TryConnectAndSendAsync(requestJson, timeoutMs);
            
            // Nếu lỗi timeout hoặc không kết nối được: Thử tự động gọi NETLOAD plugin và thử lại lần 2
            if (result.Contains("error") && (result.Contains("Timeout") || result.Contains("Lỗi kết nối IPC")))
            {
                bool loaded = EnsurePluginLoaded();
                if (loaded)
                {
                    result = await TryConnectAndSendAsync(requestJson, timeoutMs);
                }
            }

            return result;
        }

        private async Task<string> TryConnectAndSendAsync(string requestJson, int timeoutMs)
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    // Kết nối với server chạy trong AutoCAD
                    await pipeClient.ConnectAsync(timeoutMs);

                    using (var reader = new StreamReader(pipeClient, Encoding.UTF8))
                    using (var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true })
                    {
                        // 1. Gửi yêu cầu truy vấn
                        await writer.WriteLineAsync(requestJson);

                        // 2. Nhận dữ liệu kết quả trả về
                        string? response = await reader.ReadLineAsync();
                        return response ?? "{}";
                    }
                }
            }
            catch (TimeoutException)
            {
                return "{\"error\": \"Timeout: Không thể kết nối hoặc hết thời gian chờ phản hồi từ AutoCAD Plugin. Vui lòng đảm bảo AutoCAD đang mở và đã gõ lệnh NETLOAD file 'C:\\\\QKS_JSON_Data\\\\Loads\\\\QKS_Loads_AcadPlugin.dll'.\"}";
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"Lỗi kết nối IPC: {ex.Message}. Vui lòng nạp plugin 'C:\\\\QKS_JSON_Data\\\\Loads\\\\QKS_Loads_AcadPlugin.dll' trong AutoCAD.\"}}";
            }
        }

        /// <summary>
        /// Kiểm tra nhanh kết nối tới AutoCAD Plugin
        /// </summary>
        public bool PingAutoCad()
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
                {
                    pipeClient.Connect(500);
                    return pipeClient.IsConnected;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Model đại diện cho thông điệp truy vấn/gửi dữ liệu sang AutoCAD IPC
    /// </summary>
    public class AutoCadBeamQuery
    {
        public string Action { get; set; } = string.Empty;
        public System.Collections.Generic.List<AutoCadBeamData> Beams { get; set; } = new System.Collections.Generic.List<AutoCadBeamData>();
    }

    /// <summary>
    /// Model dữ liệu hình học và màu sắc dầm trao đổi với AutoCAD
    /// </summary>
    public class AutoCadBeamData
    {
        public string Id { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double Z1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Z2 { get; set; }
        public int Color { get; set; }
    }

    /// <summary>
    /// Model đại diện cho thông điệp truy vấn/gửi dữ liệu tường sang AutoCAD IPC
    /// </summary>
    public class AutoCadWallQuery
    {
        public string Action { get; set; } = string.Empty;
        public System.Collections.Generic.List<AutoCadWallLine> Lines { get; set; } = new System.Collections.Generic.List<AutoCadWallLine>();
    }

    /// <summary>
    /// Model dữ liệu hình học của đoạn tường từ AutoCAD
    /// </summary>
    public class AutoCadWallLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double Z1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Z2 { get; set; }
        public string Thickness { get; set; } = "100"; // "100" hoặc "200"
        public string BeamName { get; set; } = string.Empty; // Tên dầm lưu trong XData (nếu có)
    }
}
