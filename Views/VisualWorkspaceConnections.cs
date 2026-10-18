using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Qks.Shared;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private bool updatingModels;
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
                ConnectedFileText.Text = active == null ? "Chưa chọn mô hình" : string.IsNullOrWhiteSpace(active.ModelPath) ? active.ModelFileName : Path.GetFileName(active.ModelPath);
                ConnectedFileText.ToolTip = active?.ModelPath;
            } catch (Exception ex) { vm.Status = "Không quét được ETABS: " + ex.GetBaseException().Message; }
            finally { updatingModels = false; }
        }
        private void ModelList_Opened(object sender, EventArgs e) { if (!vm.Busy) RefreshModelList(true); }
        private void ModelList_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (updatingModels || vm.Busy || !(ConnectedModelBox.SelectedItem is EtabsInstanceInfo chosen) || ReferenceEquals(chosen, AppConnectionManager.ActiveEtabs)) return;
            if (!CommitWindGrid() || !SavePendingDrafts()) { RefreshModelList(false); return; }
            AppConnectionManager.SetConnections(chosen, AppConnectionManager.ActiveAutoCad, AppConnectionManager.ActiveExcel);
        }
    }
}
