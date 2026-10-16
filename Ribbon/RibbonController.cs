using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using QksExcelAddIn.Services;
using QksExcelAddIn.Views;

namespace QksExcelAddIn.Ribbon
{
    [ComVisible(true)]
    public class RibbonController : ExcelRibbon
    {
        private IRibbonUI? _ribbonUi;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        public override string GetCustomUI(string ribbonId)
        {
            return @"
<customUI xmlns='http://schemas.microsoft.com/office/2009/07/customui' onLoad='OnRibbonLoad'>
  <ribbon>
    <tabs>
      <tab id='tabQks' label='Extension QKS' visible='true'>
        <group id='grpApi' label='Hệ Thống'>
          <button id='btnGetApi' label='Kết Nối API' getImage='GetImage' size='large' onAction='ClickButton' />
        </group>
        <group id='grpLoads' label='Gán Tải Trọng'>
          <button id='btnWindLoad' label='Tải Gió' getImage='GetImage' size='large' onAction='ClickButton' />
          <button id='btnWallLoad' label='Tải Tường' getImage='GetImage' size='large' onAction='ClickButton' />
          <button id='btnShellLoad' label='Tải Sàn' getImage='GetImage' size='large' onAction='ClickButton' />
        </group>
      </tab>
    </tabs>
  </ribbon>
</customUI>";
        }

        public Bitmap? GetImage(IRibbonControl control)
        {
            try
            {
                string imageName = control.Id switch
                {
                    "btnGetApi" => "getapi",
                    "btnWindLoad" => "windload",
                    "btnWallLoad" => "wallload",
                    "btnShellLoad" => "shellload",
                    _ => control.Id
                };

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"QksExcelAddIn.Resources.{imageName}.png";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi load ảnh {control.Id}: {ex.Message}");
            }
            return null;
        }

        public void OnRibbonLoad(IRibbonUI ribbon)
        {
            _ribbonUi = ribbon;
        }

        private static readonly object UiLock = new object();
        private static Dispatcher? _uiDispatcher;
        private static readonly ManualResetEventSlim UiReady = new ManualResetEventSlim();
        private void ShowWindowOnSeparateThread<T>() where T : Window, new()
        {
            lock (UiLock)
            {
                if (_uiDispatcher == null)
                {
                    var thread = new Thread(() => {
                        _uiDispatcher = Dispatcher.CurrentDispatcher;
                        if (System.Windows.Application.Current == null)
                            _ = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                        UiReady.Set(); Dispatcher.Run();
                    });
                    thread.SetApartmentState(ApartmentState.STA); thread.IsBackground = true; thread.Start();
                    UiReady.Wait();
                }
            }
            _uiDispatcher!.BeginInvoke(new Action(() => {
                try { var window = new T { ShowInTaskbar = true }; window.Show(); }
                catch (Exception ex) { CustomMessageBox.Show(ex.GetBaseException().Message, "Lỗi mở giao diện", CustomMessageBox.IconType.ErrorType); }
            }));
        }

        public void ClickButton(IRibbonControl control)
        {
            try
            {
                switch (control.Id)
                {
                    case "btnGetApi":
                        ShowWindowOnSeparateThread<GetApiView>();
                        break;
                    case "btnWindLoad":
                        ShowWindowOnSeparateThread<WindLoadView>();
                        break;
                    case "btnWallLoad":
                        ShowWindowOnSeparateThread<WallLoadView>();
                        break;
                    case "btnShellLoad":
                        ShowWindowOnSeparateThread<ShellLoadView>();
                        break;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi thực thi lệnh {control.Id}: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }
    }
}
