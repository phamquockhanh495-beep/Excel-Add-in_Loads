using System;
using System.Diagnostics;
using System.Reflection;
using ExcelDna.Integration;

namespace QksExcelAddIn
{
    /// <summary>
    /// Điểm khởi đầu chính của Excel Add-in được gọi bởi Excel-DNA khi nạp add-in.
    /// </summary>
    public class AddInMain : IExcelAddIn
    {
        static AddInMain()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                var requestedName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(requestedName)) return null;

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(assembly.GetName().FullName, args.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return assembly;
                    }
                }
            }
            catch { }
            return null;
        }

        public void AutoOpen()
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

                // Nạp trước các Type để kích hoạt nạp DLL từ Excel-DNA
                _ = typeof(MaterialDesignThemes.Wpf.Card);
                _ = typeof(MaterialDesignColors.ColorPair);
                _ = typeof(CSiAPIv1.Helper);
                _ = typeof(Microsoft.Xaml.Behaviors.Behavior);

                Debug.WriteLine("QKS Excel Add-In đã được nạp thành công!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi AutoOpen: {ex.Message}");
            }
        }

        public void AutoClose()
        {
            try
            {
                Debug.WriteLine("QKS Excel Add-In đã được giải phóng.");
            }
            catch { }
        }
    }
}
