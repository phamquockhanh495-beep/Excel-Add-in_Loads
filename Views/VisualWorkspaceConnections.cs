using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services;
using QksExcelAddIn.Services.Modeling;

namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private bool updatingModels, connectionSyncPending, readStartPending;
        private EtabsInstanceInfo? manualModelChoice;
        private readonly WorkspaceReadLifecycle readLifecycle = new WorkspaceReadLifecycle();
        private List<EtabsInstanceInfo> modelChoices = new List<EtabsInstanceInfo>();
        private void RefreshModelList(bool scan)
        {
            updatingModels = true;
            try {
                if (scan) modelChoices = EtabsService.GetRunningEtabsInstances();
                var active = AppConnectionManager.ActiveEtabs;
                if (scan && active != null) {
                    bool valid;
                    try { valid = active.SapModel != null && Protocol.SamePath(active.SapModel.GetModelFilename(), active.ModelPath); } catch { valid = false; }
                    if (!valid) { AppConnectionManager.SetConnections(null, AppConnectionManager.ActiveAutoCad, AppConnectionManager.ActiveExcel); active = null; }
                }
                if (active != null) {
                    modelChoices.RemoveAll(x => x.ProcessId == active.ProcessId && Protocol.SamePath(x.ModelPath, active.ModelPath));
                    modelChoices.Insert(0, active);
                }
                ConnectedModelBox.ItemsSource = modelChoices.ToArray(); ConnectedModelBox.SelectedItem = active;
                ConnectedFileText.Text = AppConnectionManager.GetExcelDisplayName();
                ConnectedFileText.ToolTip = AppConnectionManager.ActiveExcel?.FullPath;
            } catch (Exception ex) { vm.Status = "Không quét được ETABS: " + ex.GetBaseException().Message; }
            finally { updatingModels = false; }
        }
        private void ConnectionChanged()
        {
            Dispatcher.BeginInvoke(new Action(() => {
                if (closed || connectionSyncPending) return; connectionSyncPending = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => { connectionSyncPending = false; if (!closed && IsLoaded) SynchronizeConnection(); }));
            }));
        }
        private void SynchronizeConnection(bool automatic = true)
        {
            RefreshModelList(false);
            var active = AppConnectionManager.ActiveEtabs;
            bool manual = ReferenceEquals(active, manualModelChoice) && manualModelChoice != null; manualModelChoice = null;
            if (readLifecycle.Update(active?.SapModel != null ? active : null, AppConnectionManager.ModelIdentity, automatic && !manual)) {
                StopPlanePicking(); cancellation?.Cancel(); vm.SetSnapshot(null, false);
                vm.Status = active?.SapModel == null ? "Chọn mô hình trong Kết nối API để tự đọc vào workspace." : manual ? "Đã chọn mô hình; bấm Đọc lại để lấy dữ liệu mới." : "Đang lấy mô hình từ kết nối API…";
            }
            if (automatic) ScheduleConnectedRead();
        }
        private void ScheduleConnectedRead()
        {
            if (closed || !IsLoaded || vm.Busy || !readLifecycle.Pending || readStartPending) return; readStartPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(async () => { readStartPending = false; if (!closed) await ReadConnectedModelAsync(); }));
        }
        private async Task ReadConnectedModelAsync()
        {
            if (!readLifecycle.TryBegin(vm.Busy, out long ticket)) return;
            bool Current() => readLifecycle.Accept(ticket, AppConnectionManager.ActiveEtabs, AppConnectionManager.ModelIdentity);
            if (!Current()) { ConnectionChanged(); return; }
            StopPlanePicking(); vm.SetSnapshot(null, false); vm.Busy = true;
            var reading = new CancellationTokenSource(); cancellation = reading;
            try {
                var snapshot = await new EtabsSnapshotReader().ReadAsync(message => { if (Current() && !closed) vm.Status = message; }, reading.Token);
                if (!closed && Current() && !reading.IsCancellationRequested) { vm.SetSnapshot(snapshot, false); Canvas.Fit(); }
            } catch (OperationCanceledException) { if (Current() && !closed) vm.Status = "Đã hủy đọc; bấm Đọc lại khi cần."; }
            catch (Exception ex) { if (Current() && !closed) vm.Status = "Đọc không thành công: " + ex.GetBaseException().Message; }
            finally { if (ReferenceEquals(cancellation, reading)) cancellation = null; reading.Dispose(); vm.Busy = false; }
        }
        private void ModelList_Opened(object sender, EventArgs e) { if (!vm.Busy) RefreshModelList(true); }
        private void ModelList_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (updatingModels || vm.Busy || !(ConnectedModelBox.SelectedItem is EtabsInstanceInfo chosen) || ReferenceEquals(chosen, AppConnectionManager.ActiveEtabs)) return;
            if (!CommitWindGrid() || !SavePendingDrafts()) { RefreshModelList(false); return; }
            manualModelChoice = chosen;
            AppConnectionManager.SetConnections(chosen, AppConnectionManager.ActiveAutoCad, AppConnectionManager.ActiveExcel);
        }
    }
}
