using System;
using System.Collections.Generic;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Thông tin hình học và tải trọng của 1 ô sàn (Area/Shell element) trong ETABS
    /// </summary>
    public class EtabsSlabItem
    {
        public string Name { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string SlabType { get; set; } = "Slab";
        public double Thickness { get; set; } = 0.12; // mét
        public List<(double X, double Y, double Z)> Points { get; set; } = new List<(double X, double Y, double Z)>();
        public double CentroidX { get; set; }
        public double CentroidY { get; set; }
        public double Area { get; set; } // m2
        public int ColorR { get; set; } = -1;
        public int ColorG { get; set; } = -1;
        public int ColorB { get; set; } = -1;
        public string LoadSetName { get; set; } = string.Empty;
        public string LoadPattern { get; set; } = string.Empty;
        public double LoadValue { get; set; } // kN/m2
        public string FormattedLoadText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thống kê tiết diện sàn hiển thị trên DataGrid
    /// </summary>
    public class ShellSectionInfo
    {
        public string SectionName { get; set; } = string.Empty;
        public string SlabType { get; set; } = "Slab";
        public double ThicknessMm { get; set; } = 120.0;
        public int Count { get; set; }
        public double TotalArea { get; set; } // m2
        public int ColorR { get; set; } = 100;
        public int ColorG { get; set; } = 100;
        public int ColorB { get; set; } = 100;
        public string HexColor { get; set; } = "#646464";
    }

    /// <summary>
    /// Cấu trúc lưu trữ 1 Shell Uniform Load Set
    /// </summary>
    public class ShellLoadSetItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _loadSetName = string.Empty;
        public string OriginalName { get; set; } = string.Empty;

        public string LoadSetName
        {
            get => _loadSetName;
            set
            {
                if (_loadSetName != value)
                {
                    _loadSetName = value;
                    OnPropertyChanged(nameof(LoadSetName));
                }
            }
        }

        public List<ShellLoadSetDetail> Loads { get; set; } = new List<ShellLoadSetDetail>();
        public int LoadCount => Loads?.Count ?? 0;
        public string SummaryText
        {
            get
            {
                if (Loads == null || Loads.Count == 0) return "(Trống)";
                var list = new List<string>();
                foreach (var l in Loads)
                {
                    list.Add($"{l.LoadPattern}: {l.LoadValue:0.##} kN/m²");
                }
                return string.Join(" • ", list);
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propName));
        }
    }

    /// <summary>
    /// Chi tiết 1 thành phần tải trong Load Set
    /// </summary>
    public class ShellLoadSetDetail : System.ComponentModel.INotifyPropertyChanged
    {
        private string _loadPattern = string.Empty;
        private double _loadValue = 1.0;
        private string _direction = "Gravity";

        public string LoadPattern
        {
            get => _loadPattern;
            set
            {
                if (_loadPattern != value)
                {
                    _loadPattern = value;
                    OnPropertyChanged(nameof(LoadPattern));
                }
            }
        }

        public double LoadValue
        {
            get => _loadValue;
            set
            {
                if (Math.Abs(_loadValue - value) > 0.0001)
                {
                    _loadValue = value;
                    OnPropertyChanged(nameof(LoadValue));
                }
            }
        }

        public string Direction
        {
            get => _direction;
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    OnPropertyChanged(nameof(Direction));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Thông tin vùng tải chữ nhật cục bộ (Pick 2 điểm trên CAD)
    /// </summary>
    public class ShellZoneRectangleItem
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public string StoryName { get; set; } = string.Empty;
        public string LoadPattern { get; set; } = string.Empty;
        public double LoadValue { get; set; } // kN/m2
        public string Direction { get; set; } = "Gravity";
        public bool ReplaceExisting { get; set; } = true;
    }

    /// <summary>
    /// Payload gửi sang AutoCAD Plugin để vẽ mặt bằng sàn
    /// </summary>
    public class CadShellExportPayload
    {
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public double TextHeight { get; set; } = 250.0; // mm
        public int TextColorIndex { get; set; } = 4; // Cyan ACI 4
        public double MinAreaM2 { get; set; } = 0.5; // Diện tích tối thiểu để hiện text
        public bool DrawBoundary { get; set; } = true;
        public bool DrawHatch { get; set; } = true;
        public int HatchTransparency { get; set; } = 70; // 70% trong suốt
        public bool DrawRectangle { get; set; } = true;
        public bool DrawText { get; set; } = true;
        public string StoryName { get; set; } = string.Empty;
        public string ModeTitle { get; set; } = string.Empty;
        public List<CadShellPolygonItem> Slabs { get; set; } = new List<CadShellPolygonItem>();
    }

    /// <summary>
    /// Từng ô sàn xuất sang CAD
    /// </summary>
    public class CadShellPolygonItem
    {
        public string AreaName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public List<double[]> Points { get; set; } = new List<double[]>(); // [X, Y] tính theo mm trên CAD
        public double CentroidX { get; set; } // mm
        public double CentroidY { get; set; } // mm
        public double AreaM2 { get; set; } // m2
        public int ColorR { get; set; } = 200;
        public int ColorG { get; set; } = 200;
        public int ColorB { get; set; } = 200;
        public string SectionText { get; set; } = string.Empty;
        public string LoadText { get; set; } = string.Empty;
        public bool HasLoad { get; set; }
    }

    /// <summary>
    /// Payload cập nhật Text tải trên CAD thời gian thực (In-Place Update)
    /// </summary>
    public class CadShellLiveUpdatePayload
    {
        public string TargetSection { get; set; } = string.Empty;
        public string NewLoadText { get; set; } = string.Empty;
        public Dictionary<string, string> SectionLoadMap { get; set; } = new Dictionary<string, string>();
        public int TextColorIndex { get; set; } = 4;
    }

    /// <summary>
    /// Payload vùng tải chữ nhật cục bộ từ CAD
    /// </summary>
    public class CadShellZoneRectPayload
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }

    /// <summary>
    /// Dữ liệu Text tải sàn quét/nhập từ AutoCAD về ETABS
    /// </summary>
    public class CadShellImportItem
    {
        public string AreaName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string RawText { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Payload xuất Bảng thống kê tiết diện sàn sang AutoCAD
    /// </summary>
    public class CadSlabTableExportPayload
    {
        public string StoryName { get; set; } = string.Empty;
        public double InsertX { get; set; }
        public double InsertY { get; set; }
        public double TextHeight { get; set; } = 250.0;
        public List<CadSlabTableItem> Items { get; set; } = new List<CadSlabTableItem>();
    }

    /// <summary>
    /// Từng dòng trong Bảng thống kê sàn trên CAD
    /// </summary>
    public class CadSlabTableItem
    {
        public int Stt { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int ColorR { get; set; } = 200;
        public int ColorG { get; set; } = 200;
        public int ColorB { get; set; } = 200;
        public int Count { get; set; }
        public double TotalAreaM2 { get; set; }
        public string LoadSummary { get; set; } = string.Empty;
    }
}
