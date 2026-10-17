using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using QksExcelAddIn.Modeling.Core;
using Screen = System.Windows.Forms.Screen;

namespace QksExcelAddIn.Services
{
    /// <summary>Per-window sizing. Does not change Excel's process DPI settings.</summary>
    internal sealed class WorkspaceWindowPlacement
    {
        private readonly Window window;
        private HwndSource? source;
        private IntPtr handle;
        private WorkspaceWindowPlacement(Window window)
        {
            this.window = window;
            window.SourceInitialized += SourceReady;
            window.Loaded += FirstLayout;
            window.StateChanged += StateChanged;
            window.Closed += Closed;
        }
        public static void Attach(Window window) => _ = new WorkspaceWindowPlacement(window);
        private void SourceReady(object? sender, EventArgs e)
        {
            handle = new WindowInteropHelper(window).Handle;
            source = HwndSource.FromHwnd(handle); source?.AddHook(WndProc);
        }
        private void FirstLayout(object sender, RoutedEventArgs e) { window.Loaded -= FirstLayout; Fit(true); }
        private void Fit(bool initial)
        {
            if (handle == IntPtr.Zero || window.WindowState != WindowState.Normal) return;
            var work = Screen.FromHandle(handle).WorkingArea;
            var transform = source?.CompositionTarget?.TransformToDevice ?? System.Windows.Media.Matrix.Identity;
            double sx = transform.M11 > 0 ? transform.M11 : 1, sy = transform.M22 > 0 ? transform.M22 : 1;
            if (!GetWindowRect(handle,out var rect)) return;
            var fit = WorkspaceSize.Fit(work.Width,work.Height,sx,sy,
                initial ? 1180 : Math.Max(1,rect.Right-rect.Left)/sx,initial ? 740 : Math.Max(1,rect.Bottom-rect.Top)/sy);
            window.MinWidth = fit.MinWidth; window.MinHeight = fit.MinHeight;
            window.Width = fit.Width; window.Height = fit.Height;
            int width = Math.Max(1,(int)Math.Floor(fit.Width*sx)), height = Math.Max(1,(int)Math.Floor(fit.Height*sy));
            int left = initial ? work.Left+(work.Width-width)/2 : Math.Max(work.Left,Math.Min(rect.Left,work.Right-width));
            int top = initial ? work.Top+(work.Height-height)/2 : Math.Max(work.Top,Math.Min(rect.Top,work.Bottom-height));
            SetWindowPos(handle,IntPtr.Zero,left,top,width,height,0x0004|0x0010); // no z-order or activation changes
        }
        private IntPtr WndProc(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
        {
            if (message == 0x0024) { // WM_GETMINMAXINFO: maximize within monitor work area, including non-primary monitors.
                var screen = Screen.FromHandle(hwnd); var work = screen.WorkingArea; var bounds = screen.Bounds;
                var info = (MinMaxInfo)Marshal.PtrToStructure(lParam,typeof(MinMaxInfo));
                info.MaxPosition = new NativePoint(work.Left-bounds.Left,work.Top-bounds.Top);
                info.MaxSize = new NativePoint(work.Width,work.Height); info.MaxTrackSize = info.MaxSize;
                var transform = source?.CompositionTarget?.TransformToDevice ?? System.Windows.Media.Matrix.Identity;
                info.MinTrackSize = new NativePoint(Math.Min(work.Width,Math.Max(1,(int)Math.Ceiling(window.MinWidth*transform.M11))),
                    Math.Min(work.Height,Math.Max(1,(int)Math.Ceiling(window.MinHeight*transform.M22))));
                Marshal.StructureToPtr(info,lParam,false); handled = true;
            }
            if (message == 0x02E0 || message == 0x0232 || message == 0x007E) // DPI, drag/resize end or display change; let WPF process first.
                ScheduleFit();
            return IntPtr.Zero;
        }
        private void StateChanged(object? sender,EventArgs e) { if (window.WindowState == WindowState.Normal) ScheduleFit(); }
        private void ScheduleFit() => window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(() => { if (source != null) Fit(false); }));
        private void Closed(object? sender,EventArgs e)
        {
            source?.RemoveHook(WndProc); source = null;
            window.SourceInitialized -= SourceReady; window.Loaded -= FirstLayout; window.StateChanged -= StateChanged; window.Closed -= Closed;
        }
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint
        {
            public int X,Y;
            public NativePoint(int x,int y) {X=x;Y=y;}
        }
        [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo
        { public NativePoint Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left,Top,Right,Bottom; }
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
    }
}
