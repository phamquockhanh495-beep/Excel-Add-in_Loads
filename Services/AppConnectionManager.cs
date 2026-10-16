using System;
using System.Diagnostics;
using CSiAPIv1;

namespace QksExcelAddIn.Services
{
    public class EtabsInstanceInfo
    {
        public int ProcessId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public string ModelFileName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public cOAPI? EtabsObject { get; set; }
        public cSapModel? SapModel { get; set; }
    }

    public class AcadDocInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public object? DocObj { get; set; }
    }

    public class ExcelWorkbookInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public object? WorkbookObj { get; set; }
    }

    /// <summary>
    /// Bộ quản lý kết nối tập trung (Centralized Connection Manager) cho QKS Loads.
    /// Lưu trữ các phiên làm việc đang active của ETABS, AutoCAD và Excel sau khi người dùng cấu hình qua Get API.
    /// </summary>
    public static class AppConnectionManager
    {
        public static EtabsInstanceInfo? ActiveEtabs { get; set; }
        public static AcadDocInfo? ActiveAutoCad { get; set; }
        public static ExcelWorkbookInfo? ActiveExcel { get; set; }

        public static bool IsEtabsConnected => ActiveEtabs != null && ActiveEtabs.SapModel != null;
        public static bool IsAutoCadConnected => ActiveAutoCad != null && !string.IsNullOrEmpty(ActiveAutoCad.FileName);
        public static bool IsExcelConnected => ActiveExcel != null && !string.IsNullOrEmpty(ActiveExcel.Name);

        public static event Action? ConnectionChanged;

        public static void SetConnections(EtabsInstanceInfo? etabs, AcadDocInfo? acad, ExcelWorkbookInfo? excel)
        {
            ActiveEtabs = etabs;
            ActiveAutoCad = acad;
            ActiveExcel = excel;
            NotifyConnectionChanged();
        }

        public static void NotifyConnectionChanged()
        {
            try
            {
                ConnectionChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi phát sự kiện ConnectionChanged: {ex.Message}");
            }
        }

        public static cSapModel? GetSapModel()
        {
            return ActiveEtabs?.SapModel;
        }

        public static string GetEtabsDisplayName()
        {
            if (ActiveEtabs == null || ActiveEtabs.SapModel == null)
            {
                return "Chưa kết nối ETABS";
            }
            if (!string.IsNullOrEmpty(ActiveEtabs.ModelFileName))
            {
                return ActiveEtabs.ModelFileName;
            }
            if (!string.IsNullOrEmpty(ActiveEtabs.ModelPath))
            {
                return System.IO.Path.GetFileNameWithoutExtension(ActiveEtabs.ModelPath);
            }
            return "ETABS Active";
        }

        public static string GetAutoCadDisplayName()
        {
            if (ActiveAutoCad == null || string.IsNullOrEmpty(ActiveAutoCad.FileName))
            {
                return "Chưa kết nối AutoCAD";
            }
            return ActiveAutoCad.FileName;
        }

        public static string GetExcelDisplayName()
        {
            if (ActiveExcel == null || string.IsNullOrEmpty(ActiveExcel.Name))
            {
                return "Chưa chọn file Excel";
            }
            return ActiveExcel.Name;
        }
    }
}
