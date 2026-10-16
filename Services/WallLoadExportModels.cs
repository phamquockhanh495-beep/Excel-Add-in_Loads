using System;
using System.Collections.Generic;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Cấu hình hiển thị màu sắc và kích thước khi xuất sang AutoCAD
    /// </summary>
    public class CadExportVisualSettings
    {
        public double TextHeight { get; set; } = 200.0;
        public int TextColorIndex { get; set; } = 1; // 1 = Red, 2 = Yellow, 3 = Green, 4 = Cyan, 7 = White
        public int BeamColorIndex { get; set; } = 8; // 8 = Dark Gray, 9 = Light Gray, 3 = Green, 5 = Blue
        public int DotColorIndex { get; set; } = 2;  // 2 = Yellow (Chấm tròn ngưng tải)
        public double DotRadius { get; set; } = 40.0; // Bán kính chấm tròn (mm)
        public string TextLayerName { get; set; } = "QKS_WALL_LOAD_TEXT";
        public string BeamLayerName { get; set; } = "QKS_WALL_LOAD_BEAM";
        public string DotLayerName { get; set; } = "QKS_WALL_LOAD_DOT";
        public bool UseSectionRgbColor { get; set; } = true; // True: Tô màu RGB đa sắc theo tiết diện dầm
    }

    /// <summary>
    /// Một phân đoạn tải trọng phân bố trên dầm
    /// </summary>
    public class CadLoadSegment
    {
        public double StartDist { get; set; } // Khoảng cách từ đầu dầm (m)
        public double EndDist { get; set; }   // Khoảng cách đến cuối phân đoạn (m)
        public double Val1 { get; set; }      // Giá trị tải đầu phân đoạn (kN/m)
        public double Val2 { get; set; }      // Giá trị tải cuối phân đoạn (kN/m)
        public double MidX { get; set; }      // Tọa độ X điểm đặt Text trên CAD
        public double MidY { get; set; }      // Tọa độ Y điểm đặt Text trên CAD
        public double AngleDeg { get; set; }  // Góc xoay Text theo phương dầm (độ)
        public string Text { get; set; } = string.Empty; // Chuỗi hiển thị (ví dụ: "8.5" hoặc "8.5 kN/m")
    }

    /// <summary>
    /// Chấm tròn (Dot) định vị vị trí ngưng tải / chuyển đổi tải trọng trên dầm
    /// </summary>
    public class CadBreakDot
    {
        public double X { get; set; } // Tọa độ X trên CAD
        public double Y { get; set; } // Tọa độ Y trên CAD
        public double DistAlongBeam { get; set; } // Khoảng cách dọc theo dầm từ đầu I (m)
    }

    /// <summary>
    /// Thông tin hình học và toàn bộ các phân đoạn tải của một thanh dầm xuất sang CAD
    /// </summary>
    public class CadBeamLoadExportItem
    {
        public string BeamName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public int ColorR { get; set; } = -1; // Mã màu TrueColor Red (-1: Chưa gán/Dùng ACI)
        public int ColorG { get; set; } = -1; // Mã màu TrueColor Green
        public int ColorB { get; set; } = -1; // Mã màu TrueColor Blue
        public string ColorHex { get; set; } = string.Empty; // Mã Hex ví dụ "#00CED1"
        public double X1 { get; set; } // Tọa độ CAD điểm đầu
        public double Y1 { get; set; }
        public double X2 { get; set; } // Tọa độ CAD điểm cuối
        public double Y2 { get; set; }
        public double Length { get; set; } // Chiều dài dầm (m)
        public double AngleDeg { get; set; } // Góc xoay dầm
        public List<CadLoadSegment> Segments { get; set; } = new List<CadLoadSegment>();
        public List<CadBreakDot> BreakDots { get; set; } = new List<CadBreakDot>();
    }

    /// <summary>
    /// Gói tin dữ liệu xuất toàn bộ mặt bằng tải sang AutoCAD IPC
    /// </summary>
    public class CadExportPayload
    {
        public string Action { get; set; } = "EXPORT_WALL_LOADS_TO_CAD";
        public string Story { get; set; } = string.Empty;
        public string LoadPattern { get; set; } = "BW";
        public string DirectionName { get; set; } = "Gravity";
        public int DirectionCode { get; set; } = 10;
        public string UnitLabel { get; set; } = "kN/m";
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public bool DrawLegend { get; set; } = false;
        public double LegendPointX { get; set; }
        public double LegendPointY { get; set; }
        public CadExportVisualSettings Settings { get; set; } = new CadExportVisualSettings();
        public List<CadBeamLoadExportItem> Beams { get; set; } = new List<CadBeamLoadExportItem>();
    }

    /// <summary>
    /// Một phần tử Text tải trọng được đọc từ AutoCAD khi Import
    /// </summary>
    public class CadImportedTextItem
    {
        public string TextValue { get; set; } = string.Empty;
        public double X { get; set; } // Tọa độ chèn Text trên CAD
        public double Y { get; set; }
        public string XDataBeamName { get; set; } = string.Empty; // Tên dầm lưu trong XData (nếu có)
        public string Section { get; set; } = string.Empty;       // Tiết diện dầm (nếu có, VD: B20x50)
    }

    /// <summary>
    /// Một điểm Dot ngưng tải được đọc từ AutoCAD khi Import
    /// </summary>
    public class CadImportedDotItem
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Một đường gạch chia tải (Tick line) được đọc từ AutoCAD khi Import
    /// </summary>
    public class CadImportedTickItem
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double MidX { get; set; }
        public double MidY { get; set; }
        public string XDataBeamName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
    }

    /// <summary>
    /// Đường line trục dầm được đọc từ AutoCAD khi Import
    /// </summary>
    public class CadImportedBeamLineItem
    {
        public string BeamName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }

    /// <summary>
    /// Gói tin phản hồi từ AutoCAD khi yêu cầu lấy dữ liệu Text/Tick/Dot tải đã sửa
    /// </summary>
    public class CadImportPayload
    {
        public string Action { get; set; } = string.Empty;
        public List<CadImportedTextItem> Texts { get; set; } = new List<CadImportedTextItem>();
        public List<CadImportedDotItem> Dots { get; set; } = new List<CadImportedDotItem>();
        public List<CadImportedTickItem> Ticks { get; set; } = new List<CadImportedTickItem>();
        public List<CadImportedBeamLineItem> BeamLines { get; set; } = new List<CadImportedBeamLineItem>();
    }

    /// <summary>
    /// Model đại diện cho một phân đoạn tải trọng phân bố đọc từ ETABS
    /// </summary>
    public class EtabsFrameDistributedLoad
    {
        public string FrameName { get; set; } = string.Empty;
        public string LoadPat { get; set; } = string.Empty;
        public string CSys { get; set; } = "Global";
        public int Dir { get; set; } = 6; // 6 = Gravity
        public double Dist1 { get; set; }
        public double Dist2 { get; set; }
        public double Val1 { get; set; }
        public double Val2 { get; set; }
        public double RelDist1 { get; set; }
        public double RelDist2 { get; set; }
    }

    /// <summary>
    /// Model chứa kết quả gán tải cho từng dầm phục vụ báo cáo Multi-beam Assignment Report
    /// </summary>
    public class WallLoadAssignedItem
    {
        public int Stt { get; set; }
        public string BeamName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public double StoryHeight { get; set; }
        public double UpperBeamDepth { get; set; }
        public string UpperBeamText => UpperBeamDepth > 0.001 ? $"{UpperBeamDepth * 1000:F0} mm" : "0 mm (Không dầm)";
        public double SlabDepth { get; set; }
        public string SlabText { get; set; } = "0 mm";
        public double ClearHeight { get; set; }
        public double DoorCoeff { get; set; } = 1.0;
        public double FinalLoad { get; set; }
    }

    /// <summary>
    /// Gói tin cập nhật Live Text và nét tải tường trên AutoCAD
    /// </summary>
    public class CadWallLiveUpdatePayload
    {
        public string Action { get; set; } = "UPDATE_WALL_LOAD_LIVE";
        public string Story { get; set; } = string.Empty;
        public string LoadPattern { get; set; } = string.Empty;
        public Dictionary<string, CadWallLiveBeamItem> BeamLoads { get; set; } = new Dictionary<string, CadWallLiveBeamItem>(StringComparer.OrdinalIgnoreCase);
    }

    public class CadWallLiveBeamItem
    {
        public string BeamName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string LoadText { get; set; } = "0";
        public double MaxLoadValue { get; set; } = 0.0;
        public List<CadLoadSegment> Segments { get; set; } = new List<CadLoadSegment>();
    }
}
