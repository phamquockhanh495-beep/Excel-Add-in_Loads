using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services.Modeling;

namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private void DeleteWindDraft_Click(object sender, RoutedEventArgs e) => TryDraft(vm.DeleteDraft);
        private StoryWindInputView WindInputDialog()=>new StoryWindInputView(vm.Levels,vm.AvailablePatterns,vm.WindUserBaseline?.Rows.Select(r=>r.DiaphragmKey)??Array.Empty<string>(),vm.DraftPattern,(rows,basis)=>vm.ImportStoryWind(rows,basis)){Owner=this};
        private void WindInput_Click(object sender,RoutedEventArgs e)
        {
            if(!vm.CanInputStoryWind||!CommitWindGrid())return;
            var dialog=WindInputDialog();if(dialog.ShowDialog()==true)WorkspaceTabs.SelectedItem=vm.WindUserDirty?UserWindTab:DraftTab;
        }
        private void WindExcel_Click(object sender,RoutedEventArgs e)
        {
            if(!vm.CanInputStoryWind||!CommitWindGrid())return;
            var file=new OpenFileDialog{Filter="Excel / CSV (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv"};if(file.ShowDialog(this)!=true)return;
            var dialog=WindInputDialog();dialog.LoadFile(file.FileName);if(dialog.ShowDialog()==true)WorkspaceTabs.SelectedItem=vm.WindUserDirty?UserWindTab:DraftTab;
        }
        private bool CommitWindGrid()
        {
            bool valid = WindUserTable.CommitEdit(DataGridEditingUnit.Cell, true) && WindUserTable.CommitEdit(DataGridEditingUnit.Row, true);
            if (!valid) vm.Status = "Sửa ô User Loads đang lỗi trước khi tiếp tục.";
            return valid;
        }
        private void ReadUserWind_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Busy) return;
            if (vm.Offline || vm.Snapshot?.Source != "ETABS") { vm.Status = "Đọc mô hình ETABS trực tiếp trước khi đọc User Loads."; return; }
            if (!CommitWindGrid() || !SavePendingDrafts()) return;
            try {
                var host = new EtabsLoadApplyHost(message => vm.Status = message);
                vm.SetUserWindBaseline(host.ReadUserWind(vm.DraftPattern)); WorkspaceTabs.SelectedItem = UserWindTab;
                vm.Status = vm.WindUserRows.Count > 0 ? "Đã đọc khóa diaphragm nguyên bản. Kiểm tra tầng/tâm đặt lực trong ETABS; chưa tự suy ra tâm cứng." :
                    "Pattern chưa có User Loads. Cần mẫu khóa diaphragm theo tầng từ ETABS trước khi mở tạo mới.";
            } catch (Exception ex) { vm.Status = "Không đọc User Loads: " + ex.GetBaseException().Message; }
        }
        private bool SaveUserWindFile()
        {
            if (!CommitWindGrid()) return false;
            var dialog = new SaveFileDialog { Filter = "QKS User Loads draft (*.qkswind.json)|*.qkswind.json", FileName = "QKS-user-wind.qkswind.json" };
            if (dialog.ShowDialog(this) != true) return false;
            try { SaveAtomic(dialog.FileName, JsonCodec.Serialize(vm.UserWindDocument())); vm.MarkUserWindSaved(); vm.Status = "Đã lưu nháp User Loads; chưa ghi ETABS."; return true; }
            catch (Exception ex) { vm.Status = "Không lưu nháp gió: " + ex.Message; return false; }
        }
        private void SaveUserWind_Click(object sender, RoutedEventArgs e) => SaveUserWindFile();
        private void OpenUserWind_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Busy) return;
            var dialog = new OpenFileDialog { Filter = "QKS User Loads draft (*.qkswind.json)|*.qkswind.json" };
            if (dialog.ShowDialog(this) != true || !CommitWindGrid() || !SavePendingDrafts()) return;
            TryDraft(() => { vm.ImportUserWindDocument(JsonCodec.Deserialize<UserWindDocument>(JsonCodec.Read(dialog.FileName))); WorkspaceTabs.SelectedItem = UserWindTab; vm.Status = "Đã mở nháp User Loads; chưa ghi ETABS."; });
        }
        private async void ApplyUserWind_Click(object sender, RoutedEventArgs e)
        {
            if (!CommitWindGrid() || !vm.CanApplyUserWind || !await ApplyGate.WaitAsync(0)) return;
            vm.Busy = true; cancellation = new CancellationTokenSource();
            try {
                var document = vm.UserWindDocument(); document.Expected();
                string review = "Model: " + document.Identity + "\nPattern: " + document.Before.Pattern + "\n" + document.Changes.Count + " khóa diaphragm thay đổi.\n" +
                    string.Join("\n", document.Changes.Take(12).Select(r => $"{r.DiaphragmKey}: Fx={r.Fx:0.###}, Fy={r.Fy:0.###} kN; Mz={r.Mz:0.###} kN·m; ({r.X:0.###},{r.Y:0.###}) m")) +
                    "\n\nĐây là giá trị nhập theo Story từ Excel/popup, không thực hiện quy đổi. Kiểm tra ánh xạ Story–diaphragm, đơn vị và X/Y; chương trình chưa tự xác định tâm cứng.\nModel sẽ được backup, giữ các khóa khác và đọc lại toàn bảng sau từng lệnh. Bản thử cần nghiệm thu trên model thử. Tiến hành ghi?";
                if (System.Windows.MessageBox.Show(this, review, "QKS · Apply User Loads gió", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) { vm.Status = "Đã hủy, chưa ghi User Loads."; return; }
                var host = new EtabsLoadApplyHost(message => vm.Status = "Preflight gió · " + message);
                var report = await UserWindApply.ExecuteAsync(document, host, cancellation.Token);
                vm.SetUserWindReport(report); vm.Status = report.Outcome + " · " + report.Error + " · Backup: " + report.BackupPath;
            } catch (Exception ex) { vm.Status = "Không Apply User Loads: " + ex.GetBaseException().Message; }
            finally { cancellation.Dispose(); cancellation = null; vm.Busy = false; ApplyGate.Release(); }
        }
    }
}
