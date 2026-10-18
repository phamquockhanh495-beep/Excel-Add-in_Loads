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
        private static readonly SemaphoreSlim ApplyGate = new SemaphoreSlim(1, 1);
        private Vec2? firstPlanePoint;
        private bool displayVisible = true, editorVisible = true;
        private int panelMode = -1, tableMode = -1;
        private bool syncingSelection, selectionSyncPending;
        private static string ColorFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QKS", "Viewer", "property-colors.json");
        public VisualLoadWorkspaceView()
        {
            InitializeComponent(); DataContext = vm;
            WorkspaceWindowPlacement.Attach(this);
            SizeChanged += (_, __) => UpdateResponsiveLayout();
            Loaded += (_, __) => { UpdateResponsiveLayout(); RefreshModelList(false); };
            StateChanged += (_, __) => MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
            Canvas.SelectionRequested += (ids, change) => vm.SelectElements(ids, change);
            Canvas.PlanePointPicked += PlanePointPicked;
            vm.PropertyChanged += (_, e) => {
                if (e.PropertyName == nameof(vm.Elevation) && vm.Elevation && Canvas.PickingPlanePoints) StopPlanePicking();
                if (e.PropertyName == nameof(vm.Scene)) { syncingSelection = true; ScheduleSelectionSync(); }
                if (e.PropertyName == nameof(vm.SelectedIds) || e.PropertyName == nameof(vm.VisibleElements)) ScheduleSelectionSync();
            };
            PreviewKeyDown += Window_KeyDown;
            Closing += (_, e) => { if (vm.Busy) { e.Cancel = true; vm.Status = "Chờ tác vụ kết thúc hoặc Hủy đọc trước khi đóng."; } else if (!CommitWindGrid() || !SavePendingDrafts()) e.Cancel = true; };
            AppConnectionManager.ConnectionChanged += ConnectionChanged;
            Closed += (_, __) => { closed = true; cancellation?.Cancel(); AppConnectionManager.ConnectionChanged -= ConnectionChanged; };
            try {
                if (File.Exists(ColorFile)) {
                    var colors = JsonCodec.Deserialize<Dictionary<string, string>>(JsonCodec.Read(ColorFile));
                    vm.ColorOverrides = colors.Where(p => Regex.IsMatch(p.Value ?? "", "^#[0-9a-fA-F]{6}$")).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
                    vm.NotifyStyle();
                }
            } catch (Exception ex) { vm.Status = "Không đọc được profile màu: " + ex.Message; }
        }
        private void ConnectionChanged() => Dispatcher.BeginInvoke(new Action(() => {
            if (closed) return;
            StopPlanePicking();
            cancellation?.Cancel(); vm.SetSnapshot(null, false); vm.Status = "Kết nối đã đổi. Hãy Đọc mô hình để lấy dữ liệu mới.";
            RefreshModelList(false);
        }));
        private void Connect_Click(object sender, RoutedEventArgs e) => new GetApiView().Show();
        private async void Read_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Busy) return;
            StopPlanePicking();
            vm.SetSnapshot(null, false); vm.Busy = true; cancellation = new CancellationTokenSource();
            try {
                var snapshot = await new EtabsSnapshotReader().ReadAsync(message => vm.Status = message, cancellation.Token);
                if (!closed) { vm.SetSnapshot(snapshot, false); Canvas.Fit(); }
            } catch (OperationCanceledException) { vm.Status = "Đã hủy đọc; chưa tạo snapshot mới."; }
            catch (Exception ex) { vm.Status = "Đọc không thành công: " + ex.GetBaseException().Message; }
            finally { cancellation.Dispose(); cancellation = null; vm.Busy = false; }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) => cancellation?.Cancel();
        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.CanApply || !await ApplyGate.WaitAsync(0)) return;
            var document = vm.Drafts!.Document(); vm.Busy = true; cancellation = new CancellationTokenSource();
            try {
                var host = new EtabsLoadApplyHost(message => vm.Status = "Preflight · " + message);
                var fresh = await host.ReadSnapshotAsync(cancellation.Token);
                var plan = ApplyPlan.Create(fresh, document, host.Capabilities);
                cancellation.Token.ThrowIfCancellationRequested();
                string review = "Model: " + plan.Identity + "\n" + plan.DraftCount + " dòng nháp / " + plan.Groups.Count + " phạm vi cấu kiện–pattern.\n" +
                    string.Join("\n", plan.Groups.Take(12).Select(g => g.Element.Id + " / " + g.Pattern + (g.Replace ? " · THAY" : " · THÊM"))) +
                    "\n\nModel sẽ được lưu và sao lưu SHA-256 vào QKS_Backups trước khi ghi. Đọc lại để kiểm tra toàn bộ tải của cấu kiện. Nếu lỗi: dừng, không tự rollback hoặc tự thử lại.\n\nBản thử chưa nghiệm thu Windows; hãy dùng model thử. Tiến hành ghi?";
                if (System.Windows.MessageBox.Show(this, review, "QKS · Kiểm tra Apply ETABS", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) { vm.Status = "Đã hủy Apply, chưa ghi model."; return; }
                var report = await LoadApply.ExecuteAsync(document, host, cancellation.Token);
                vm.SetApplyReport(report); vm.Status = report.Outcome + " · " + report.Error + (report.BackupPath.Length > 0 ? " · Backup: " + report.BackupPath : "");
            } catch (Exception ex) { vm.Status = "Không Apply: " + ex.GetBaseException().Message; }
            finally { cancellation.Dispose(); cancellation = null; vm.Busy = false; ApplyGate.Release(); }
        }
        private void SaveApplyReport_Click(object sender, RoutedEventArgs e)
        {
            if (vm.LastWriteReport == null) { vm.Status = "Chưa có báo cáo Apply."; return; }
            var dialog = new SaveFileDialog { Filter = "QKS apply report (*.json)|*.json", FileName = "QKS-apply-report.json" };
            if (dialog.ShowDialog(this) != true) return;
            try { SaveAtomic(dialog.FileName, JsonCodec.Serialize(vm.LastWriteReport)); vm.Status = "Đã lưu báo cáo Apply."; }
            catch (Exception ex) { vm.Status = "Không lưu được báo cáo: " + ex.Message; }
        }
        private void Plan_Checked(object sender, RoutedEventArgs e) { if (DataContext != null) vm.Elevation = false; }
        private void Project_Click(object sender, RoutedEventArgs e) { vm.Refresh(); Canvas.Fit(); }
        private void Fit_Click(object sender, RoutedEventArgs e) => Canvas.Fit();
        private void Style_Click(object sender, RoutedEventArgs e) { vm.NotifyStyle(); Canvas.InvalidateVisual(); }
        private void Objects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (syncingSelection) return;
            var ids = ObjectsTable.SelectedItems.OfType<ModelElement>().Select(x => x.Id).ToList();
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0) {
                var visible = new HashSet<string>(vm.VisibleElements.Select(x => x.Id), StringComparer.Ordinal);
                ids.InsertRange(0, vm.SelectedIds.Where(id => !visible.Contains(id)));
            }
            vm.SelectElements(ids);
        }
        private void ScheduleSelectionSync()
        {
            if (selectionSyncPending) return; selectionSyncPending = true;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.DataBind, new Action(() => {
                selectionSyncPending = false; if (closed) return; syncingSelection = true;
                try {
                    var visible = new HashSet<string>(vm.VisibleElements.Select(x => x.Id), StringComparer.Ordinal);
                    var desired = new HashSet<string>(vm.SelectedIds, StringComparer.Ordinal);
                    if(visible.Count>0&&visible.All(desired.Contains)) { ObjectsTable.SelectAll(); return; }
                    foreach (var item in ObjectsTable.SelectedItems.OfType<ModelElement>().ToArray()) if (!desired.Contains(item.Id)) ObjectsTable.SelectedItems.Remove(item);
                    var current = new HashSet<string>(ObjectsTable.SelectedItems.OfType<ModelElement>().Select(x => x.Id), StringComparer.Ordinal);
                    foreach (var item in vm.SelectedElements) if (visible.Contains(item.Id) && !current.Contains(item.Id)) ObjectsTable.SelectedItems.Add(item);
                } finally { syncingSelection = false; }
            }));
        }
        private void ClearSelection_Click(object sender, RoutedEventArgs e) { Canvas.CancelInteraction(); vm.ClearSelection(); }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "QKS model snapshot (*.json)|*.json" };
            if (dialog.ShowDialog(this) != true) return;
            try { StopPlanePicking(); vm.SetSnapshot(JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Read(dialog.FileName)), true); Canvas.Fit(); }
            catch (Exception ex) { vm.Status = "Không mở được snapshot: " + ex.GetBaseException().Message; }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Snapshot == null) { vm.Status = "Chưa có snapshot để lưu."; return; }
            var dialog = new SaveFileDialog { Filter = "QKS model snapshot (*.json)|*.json", FileName = "QKS-model-snapshot.json" };
            if (dialog.ShowDialog(this) != true) return;
            try { SaveAtomic(dialog.FileName, JsonCodec.Serialize(vm.Snapshot)); vm.Status = "Đã lưu snapshot: " + dialog.FileName; }
            catch (Exception ex) { vm.Status = "Không lưu được snapshot: " + ex.Message; }
        }
        private static void SaveAtomic(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { JsonCodec.WriteAtomic(temporary, json); if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private void TryDraft(Action operation)
        {
            try { operation(); }
            catch (Exception ex) { vm.Status = "Không cập nhật nháp: " + ex.GetBaseException().Message; }
        }
        private void DraftCommitted(object sender, RoutedEventArgs e) { WorkspaceTabs.SelectedItem = DraftTab; e.Handled = true; }
        private void UndoDraft_Click(object sender, RoutedEventArgs e) => vm.UndoDraft();
        private void RedoDraft_Click(object sender, RoutedEventArgs e) => vm.RedoDraft();
        private void Draft_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is DraftLoad load) TryDraft(() => vm.EditDraft(load));
        }
        private bool SaveDraftFile()
        {
            if (vm.Drafts == null) { vm.Status = "Chưa có bộ nháp để lưu."; return false; }
            var dialog = new SaveFileDialog { Filter = "QKS load draft (*.qksdraft.json)|*.qksdraft.json", FileName = "QKS-loads.qksdraft.json" };
            if (dialog.ShowDialog(this) != true) return false;
            try {
                SaveAtomic(dialog.FileName, JsonCodec.Serialize(vm.Drafts.Document())); vm.Drafts.MarkSaved(); vm.NotifyDraftState();
                vm.Status = "Đã lưu nháp (chưa ghi model): " + dialog.FileName; return true;
            } catch (Exception ex) { vm.Status = "Không lưu được nháp: " + ex.Message; return false; }
        }
        private bool SavePendingDrafts()
        {
            if (vm.Drafts?.IsDirty != true && !vm.WindUserDirty) return true;
            var answer = System.Windows.MessageBox.Show(this, "Bộ nháp có thay đổi chưa lưu. Lưu nháp trước khi tiếp tục?", "QKS · Nháp tải", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return answer == MessageBoxResult.No || answer == MessageBoxResult.Yes &&
                (vm.Drafts?.IsDirty != true || SaveDraftFile()) && (!vm.WindUserDirty || SaveUserWindFile());
        }
        private void SaveDraft_Click(object sender, RoutedEventArgs e) => SaveDraftFile();
        private void OpenDraft_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "QKS load draft (*.qksdraft.json)|*.qksdraft.json" };
            if (dialog.ShowDialog(this) != true || !SavePendingDrafts()) return;
            TryDraft(() => { vm.ImportDraft(JsonCodec.Deserialize<DraftDocument>(JsonCodec.Read(dialog.FileName))); WorkspaceTabs.SelectedItem = DraftTab; vm.Status = "Đã mở nháp gắn đúng snapshot; chưa ghi model."; });
        }
        private void NewDraft_Click(object sender, RoutedEventArgs e)
        {
            if (!SavePendingDrafts()) return;
            TryDraft(() => { vm.NewDraftSet(); vm.Status = "Đã tạo bộ nháp mới cho snapshot hiện tại."; });
        }
        private void StopPlanePicking()
        {
            Canvas.PickingPlanePoints = false; Canvas.CancelInteraction(); Canvas.FirstPlanePoint = null; firstPlanePoint = null; Canvas.Cursor = Cursors.Arrow; Canvas.InvalidateVisual();
        }
        private void PickElevation_Click(object sender, RoutedEventArgs e)
        {
            if (Canvas.PickingPlanePoints) { StopPlanePicking(); vm.Status = "Đã hủy chọn mặt đứng."; return; }
            if (vm.Snapshot == null || vm.Busy) { vm.Status = "Mở/đọc snapshot trước khi chọn hai điểm."; return; }
            try { vm.PreparePlanPointPicking(); }
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
            if (e.Key == Key.Escape && Canvas.PickingPlanePoints) { StopPlanePicking(); vm.Status = "Đã hủy chọn mặt đứng."; e.Handled = true; }
            if (Keyboard.FocusedElement is TextBoxBase || Keyboard.FocusedElement is PasswordBox) return;
            if (e.Key == Key.Delete) { Canvas.CancelInteraction(); vm.ClearSelection(); vm.Status = "Đã hủy chọn; không xóa cấu kiện hoặc tải."; e.Handled = true; return; }
            if (e.Key == Key.Escape) Canvas.CancelInteraction();
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            if (e.Key == Key.Z) { vm.UndoDraft(); e.Handled = true; }
            if (e.Key == Key.Y) { vm.RedoDraft(); e.Handled = true; }
        }
        private void Minimize_Click(object sender,RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender,RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender,RoutedEventArgs e) => Close();
        private void UpdateResponsiveLayout()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            int next = WorkspaceSize.PanelMode(ActualWidth);
            if (next != panelMode) { panelMode = next; displayVisible = next == 2; editorVisible = next >= 1; }
            int nextTable = ActualHeight < 500 ? 0 : ActualHeight < 680 ? 1 : 2;
            if (nextTable != tableMode) { tableMode = nextTable; TableRow.Height = new GridLength(nextTable == 0 ? 125 : nextTable == 1 ? 175 : 225); }
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
            displayVisible = !displayVisible;
            if (displayVisible && panelMode < 2) editorVisible = false;
            ApplyPanels(); Dispatcher.BeginInvoke(new Action(Canvas.Fit));
        }
        private void ToggleEditor_Click(object sender,RoutedEventArgs e)
        {
            editorVisible = !editorVisible;
            if (editorVisible && panelMode < 2) displayVisible = false;
            ApplyPanels(); Dispatcher.BeginInvoke(new Action(Canvas.Fit));
        }
    }
}
