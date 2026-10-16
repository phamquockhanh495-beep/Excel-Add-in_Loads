using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;

namespace QksExcelAddIn.Views
{
    /// <summary>
    /// Helper trích xuất Icon trực tiếp từ tệp Etabs.exe đang chạy hoặc từ thư mục cài đặt mặc định
    /// </summary>
    public static class EtabsIconHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static void SetIcon(Window window)
        {
            try
            {
                string? etabsPath = FindEtabsPath();
                if (string.IsNullOrEmpty(etabsPath) || !File.Exists(etabsPath))
                {
                    Debug.WriteLine("Không tìm thấy đường dẫn Etabs.exe để trích xuất icon.");
                    return;
                }

                // Trích xuất icon đầu tiên (chứa logo ETABS)
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, etabsPath!, 0);
                if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                {
                    try
                    {
                        var iconSource = Imaging.CreateBitmapSourceFromHIcon(
                            hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        window.Icon = iconSource;
                    }
                    finally
                    {
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi trích xuất Icon ETABS: {ex.Message}");
            }
        }

        private static string? FindEtabsPath()
        {
            // 1. Thử tìm tiến trình ETABS đang chạy trên hệ thống
            var processes = Process.GetProcessesByName("ETABS");
            if (processes.Length > 0)
            {
                try
                {
                    string? path = processes[0].MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path)) return path;
                }
                catch { }
            }

            // 2. Nếu không có tiến trình chạy, quét trong thư mục Program Files mặc định của Computers and Structures
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string csiPath = Path.Combine(programFiles, "Computers and Structures");
            if (Directory.Exists(csiPath))
            {
                try
                {
                    var files = Directory.GetFiles(csiPath, "ETABS.exe", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        return files[0];
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
