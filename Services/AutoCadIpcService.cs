using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Service kết nối tốc độ cao với AutoCAD Plugin qua .NET Named Pipes.
    /// Giúp truyền tải hàng ngàn dữ liệu dầm/tường trực tiếp trên bộ nhớ mà không thông qua COM.
    /// </summary>
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
