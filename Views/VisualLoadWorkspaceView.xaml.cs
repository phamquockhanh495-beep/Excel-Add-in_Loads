using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
        private static string ColorFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QKS", "Viewer", "property-colors.json");
        public VisualLoadWorkspaceView()
        {
            InitializeComponent(); DataContext = vm;
            Canvas.Picked += element => vm.SelectedElement = element;
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
            cancellation?.Cancel(); vm.SetSnapshot(null, false); vm.Status = "Kết nối đã đổi. Hãy Đọc mô hình để lấy dữ liệu mới.";
        }));
        private void Connect_Click(object sender, RoutedEventArgs e) => new GetApiView().Show();
        private async void Read_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Busy) return;
            vm.SetSnapshot(null, false); vm.Busy = true; cancellation = new CancellationTokenSource();
            try {
                var snapshot = await new EtabsSnapshotReader().ReadAsync(message => vm.Status = message, cancellation.Token);
                if (!closed) { vm.SetSnapshot(snapshot, false); Canvas.Fit(); }
            } catch (OperationCanceledException) { vm.Status = "Đã hủy đọc; chưa tạo snapshot mới."; }
            catch (Exception ex) { vm.Status = "Đọc không thành công: " + ex.GetBaseException().Message; }
            finally { cancellation.Dispose(); cancellation = null; vm.Busy = false; }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) => cancellation?.Cancel();
        private void Plan_Checked(object sender, RoutedEventArgs e) { if (DataContext != null) vm.Elevation = false; }
        private void Project_Click(object sender, RoutedEventArgs e) { vm.Refresh(); Canvas.Fit(); }
        private void Fit_Click(object sender, RoutedEventArgs e) => Canvas.Fit();
        private void Style_Click(object sender, RoutedEventArgs e) { vm.NotifyStyle(); Canvas.InvalidateVisual(); }
        private void Objects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ObjectsTable.SelectedItem is ModelElement element) vm.SelectedElement = element;
        }
        private void ClearSelection_Click(object sender, RoutedEventArgs e) { ObjectsTable.UnselectAll(); vm.SelectedElement = null; }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "QKS model snapshot (*.json)|*.json" };
            if (dialog.ShowDialog(this) != true) return;
            try { vm.SetSnapshot(JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Read(dialog.FileName)), true); Canvas.Fit(); }
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
        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(vm.PropertyKey)) { vm.Status = "Chọn đối tượng trước khi lưu màu property."; return; }
            if (!Regex.IsMatch(vm.PropertyColor ?? "", "^#[0-9a-fA-F]{6}$")) { vm.Status = "Màu phải có dạng #RRGGBB."; return; }
            try {
                var colors = new Dictionary<string, string>(vm.ColorOverrides, StringComparer.Ordinal) { [vm.PropertyKey] = vm.PropertyColor! };
                SaveAtomic(ColorFile, JsonCodec.Serialize(colors)); vm.ColorOverrides = colors; vm.NotifyStyle(); Canvas.InvalidateVisual(); vm.Status = "Đã lưu màu property vào profile QKS trên máy này.";
            } catch (Exception ex) { vm.Status = "Không lưu được màu: " + ex.Message; }
        }
    }
}
