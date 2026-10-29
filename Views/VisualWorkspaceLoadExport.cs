using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private async void ExportSlabCurrent_Click(object sender, RoutedEventArgs e) => await ExportSlabs(false);
        private async void ExportSlabAll_Click(object sender, RoutedEventArgs e) => await ExportSlabs(true);
        private async Task ExportSlabs(bool allStories)
        {
            if (vm.Busy) return;
            var snapshot = vm.Snapshot;
            if (snapshot == null) { vm.Status = "Đọc mô hình trước khi xuất tải sàn."; return; }
            if (!allStories && (vm.Elevation || vm.Level == null)) {
                vm.Status = "Chuyển về Plan và chọn tầng cần xuất; mặt cắt có thể đi qua nhiều tầng.";
                return;
            }
            bool sets = vm.LoadSetsMode;
            string? filter = sets ? (vm.UniformLoadSet == VisualWorkspaceViewModel.AllUniformSets ? null : vm.UniformLoadSet)
                : (vm.ReadPattern == VisualWorkspaceViewModel.AllReadPatterns ? null : vm.ReadPattern);
            string? story = allStories ? null : vm.Level!.Name;
            var dialog = new SaveFileDialog {
                Title = "Xuất tải sàn · " + (sets ? "Load Sets" : "Load Pattern"), Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx", AddExtension = true, OverwritePrompt = true,
                FileName = "QKS_Slab_" + (sets ? "LoadSets_" : "LoadPattern_") + (allStories ? "AllStories_" : "CurrentStory_") + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx"
            };
            if (dialog.ShowDialog(this) != true) return;
            vm.Busy = true; vm.Status = "Đang xuất bảng tải sàn…";
            try {
                int count = await Task.Run(() => SlabLoadWorkbook.Write(dialog.FileName, snapshot, sets, filter, story));
                vm.Status = "Đã xuất " + count + " bản ghi tải sàn · " + (story ?? "toàn bộ tầng") + " · " + dialog.FileName;
                if (count == 0) MessageBox.Show(this, "Không có bản ghi tải sàn trong phạm vi đã chọn. File đã xuất gồm thông tin nguồn và tiêu đề bảng.", "Xuất tải sàn", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                vm.Status = "Xuất tải sàn thất bại: " + ex.Message;
                MessageBox.Show(this, vm.Status, "Xuất tải sàn", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally { vm.Busy = false; }
        }
    }
}
