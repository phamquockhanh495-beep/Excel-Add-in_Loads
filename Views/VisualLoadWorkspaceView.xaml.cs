using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services;
using QksExcelAddIn.Services.Modeling;
using QksExcelAddIn.ViewModels;

namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView : Window
    {
        private readonly VisualWorkspaceViewModel vm = new VisualWorkspaceViewModel();
        private CancellationTokenSource? cancellation;
        private bool closed;
        private Vec2? firstPlanePoint;
        private bool displayVisible = true, editorVisible = true;
        private int panelMode = -1;
        private static string ColorFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QKS", "Viewer", "property-colors.json");
        public VisualLoadWorkspaceView()
        {
            InitializeComponent(); DataContext = vm;
            WorkspaceWindowPlacement.Attach(this);
            SizeChanged += (_, __) => UpdateResponsiveLayout();
            Loaded += (_, __) => { UpdateResponsiveLayout(); SynchronizeConnection(); };
            StateChanged += (_, __) => MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
            Canvas.SelectionRequested += (ids, change) => vm.SelectElements(ids, change);
            Canvas.PlanePointPicked += PlanePointPicked;
            Canvas.ViewContextRequested += OpenViewContextMenu;
            vm.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(vm.Elevation) && vm.Elevation && Canvas.PickingPlanePoints) StopPlanePicking();
                if (e.PropertyName == nameof(vm.Busy) && !vm.Busy) ScheduleConnectedRead();
            };
            PreviewKeyDown += Window_KeyDown;
            Closing += (_, e) => { if (vm.Busy) { e.Cancel = true; vm.Status = "Chờ tác vụ kết thúc hoặc Hủy đọc trước khi đóng."; return; } SaveViewerPreferences(); };
            AppConnectionManager.ConnectionChanged += ConnectionChanged;
            Closed += (_, __) => { closed = true; cancellation?.Cancel(); AppConnectionManager.ConnectionChanged -= ConnectionChanged; };
            try {if(File.Exists(VisualWorkspaceObjectColorsView.ProfileFile))vm.ApplyObjectColors(JsonCodec.Deserialize<ObjectColorProfile>(JsonCodec.Read(VisualWorkspaceObjectColorsView.ProfileFile)));}
            catch(Exception ex){vm.Status="Không đọc được Color Objects: "+ex.Message;}
            try { if(File.Exists(VisualWorkspaceTextView.ProfileFile))vm.ApplyTextProfile(JsonCodec.Deserialize<ViewerTextProfile>(JsonCodec.Read(VisualWorkspaceTextView.ProfileFile))); }
            catch(Exception ex){vm.Status="Không đọc được cấu hình chữ: "+ex.Message;}
            try {
                if (File.Exists(ColorFile)) {
                    var colors = JsonCodec.Deserialize<Dictionary<string, string>>(JsonCodec.Read(ColorFile));
                    vm.ColorOverrides = colors.Where(p => Regex.IsMatch(p.Value ?? "", "^#[0-9a-fA-F]{6}$")).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
                    vm.NotifyStyle();
                }
            } catch (Exception ex) { vm.Status = "Không đọc được profile màu: " + ex.Message; }
            LoadViewerPreferences();
        }
        private void Connect_Click(object sender, RoutedEventArgs e) => new GetApiView().Show();
        private async void Read_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Busy) return;
            SynchronizeConnection(false);readLifecycle.Request();
            if (!readLifecycle.Pending) { vm.Status="Chọn ETABS trong Kết nối API trước khi đọc.";return; }
            await ReadConnectedModelAsync();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) { readLifecycle.CancelPending();cancellation?.Cancel(); }
        private void Plan_Checked(object sender, RoutedEventArgs e) { if (DataContext != null) vm.Elevation = false; }
        private void Project_Click(object sender, RoutedEventArgs e) { vm.Refresh(); Canvas.Fit(); }
        private void Fit_Click(object sender, RoutedEventArgs e) => Canvas.Fit();
        private void Style_Click(object sender, RoutedEventArgs e) { vm.NotifyStyle(); Canvas.InvalidateVisual(); }
        private void ReadLoad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var row=e.AddedItems.OfType<ReadLoadDisplay>().FirstOrDefault();
            if(row!=null)vm.SelectElements(new[]{row.ElementId});
        }
        private void ClearSelection_Click(object sender, RoutedEventArgs e) { Canvas.CancelInteraction(); vm.ClearSelection(); }
        private void RemoveSelection_Click(object sender, RoutedEventArgs e) { StopPlanePicking();vm.RemoveSelectedFromWorkspace(); }
        private void RestoreWorkspace_Click(object sender, RoutedEventArgs e) { Canvas.CancelInteraction();vm.RestoreWorkspace(); }
        private void StopPlanePicking()
        {
            Canvas.PickingPlanePoints = false; Canvas.CancelInteraction(); Canvas.FirstPlanePoint = null; firstPlanePoint = null; Canvas.Cursor = Cursors.Arrow; Canvas.InvalidateVisual();
        }
        private void PickElevation_Click(object sender, RoutedEventArgs e)
        {
            if (Canvas.PickingPlanePoints) { StopPlanePicking(); vm.Status = "Đã hủy chọn mặt đứng."; return; }
            if (vm.Snapshot == null || vm.Busy) { vm.Status = "Đọc mô hình trước khi chọn hai điểm."; return; }
            try { vm.BeginNewSection(); }
            catch (Exception ex) { vm.Status = ex.Message; return; }
            PlanRadio.IsChecked = true; Canvas.Fit(); Canvas.PickingPlanePoints = true; Canvas.Cursor = Cursors.Cross;
            vm.Status = "Chọn điểm đầu trên Plan; snap đầu cấu kiện, trung điểm hoặc cạnh trong 8 DIP. Esc để hủy.";
        }
        private void PlanePointPicked(Vec2 point)
        {
            if (!firstPlanePoint.HasValue) { firstPlanePoint = point; Canvas.FirstPlanePoint = point; Canvas.InvalidateVisual(); vm.Status = "Chọn điểm thứ hai xác định hướng U; Esc để hủy."; return; }
            try { vm.SetElevationThrough(firstPlanePoint.Value, point); StopPlanePicking(); Canvas.Fit(); }
            catch (Exception ex) { vm.Status = ex.Message + " Chọn lại điểm thứ hai."; }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBoxBase || Keyboard.FocusedElement is PasswordBox) return;
            var key=e.Key==Key.System?e.SystemKey:e.Key;
            if(Keyboard.Modifiers==ModifierKeys.Alt&&key==Key.S){OpenDisplaySettings();e.Handled=true;return;}
            if(Canvas.IsKeyboardFocusWithin&&Keyboard.Modifiers==ModifierKeys.Control) {
                if(key==Key.I){StopPlanePicking();vm.IsolateSelection();e.Handled=true;return;}
                if(key==Key.H){StopPlanePicking();vm.RemoveSelectedFromWorkspace();e.Handled=true;return;}
                if(key==Key.A){StopPlanePicking();vm.ShowAllObjects();e.Handled=true;return;}
            }
            if ((e.Key == Key.PageUp || e.Key == Key.PageDown) && Keyboard.Modifiers == ModifierKeys.None && !vm.Elevation) {
                if (Keyboard.FocusedElement is ComboBoxItem || Keyboard.FocusedElement is ComboBox list && (list.IsDropDownOpen || list.IsEditable)) return;
                if (vm.MovePlanLevel(e.Key == Key.PageUp ? 1 : -1)) StopPlanePicking();
                e.Handled = true; return;
            }
            if (e.Key == Key.Escape) {
                if (Keyboard.FocusedElement is ComboBox combo && combo.IsDropDownOpen || Keyboard.FocusedElement is ComboBoxItem) return;
                StopPlanePicking(); vm.ClearSelection(); vm.Status = "Đã hủy thao tác và bỏ chọn cấu kiện.";
                e.Handled = true; return;
            }
            if (e.Key == Key.Delete) { if(!e.IsRepeat) { StopPlanePicking();vm.RemoveSelectedFromWorkspace(); }e.Handled = true; return; }
        }
        private void Minimize_Click(object sender,RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender,RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender,RoutedEventArgs e) => Close();
        private void UpdateResponsiveLayout()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            int next = WorkspaceSize.PanelMode(ActualWidth);
            if (next != panelMode) { panelMode = next; if(!panelsCustomized) { displayVisible = next == 2; editorVisible = next >= 1; } else if(next<2&&displayVisible&&editorVisible)displayVisible=false; }
            ApplyPanels();
        }
        private void ApplyPanels()
        {
            DisplayPane.Visibility = displayVisible ? Visibility.Visible : Visibility.Collapsed;
            EditorPane.Visibility = editorVisible ? Visibility.Visible : Visibility.Collapsed;
            DisplayColumn.Width = new GridLength(displayVisible ? (panelMode == 2 ? 200 : 190) : 0);
            DisplayGap.Width = new GridLength(displayVisible ? 6 : 0);
            EditorColumn.Width = new GridLength(editorVisible ? (panelMode == 2 ? 285 : 270) : 0);
            EditorGap.Width = new GridLength(editorVisible ? 6 : 0);
        }
        private void ToggleDisplay_Click(object sender,RoutedEventArgs e)
        {
            panelsCustomized=true;displayVisible = !displayVisible;
            if (displayVisible && panelMode < 2) editorVisible = false;
            ApplyPanels(); Dispatcher.BeginInvoke(new Action(Canvas.Fit));
        }
        private void ToggleEditor_Click(object sender,RoutedEventArgs e)
        {
            panelsCustomized=true;editorVisible = !editorVisible;
            if (editorVisible && panelMode < 2) displayVisible = false;
            ApplyPanels(); Dispatcher.BeginInvoke(new Action(Canvas.Fit));
        }
    }
}
