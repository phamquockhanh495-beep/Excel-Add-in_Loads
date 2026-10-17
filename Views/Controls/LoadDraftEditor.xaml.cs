using System;
using System.Windows;
using System.Windows.Controls;
using QksExcelAddIn.ViewModels;

namespace QksExcelAddIn.Views.Controls
{
    public partial class LoadDraftEditor : UserControl
    {
        public static readonly RoutedEvent DraftCommittedEvent = EventManager.RegisterRoutedEvent(nameof(DraftCommitted),RoutingStrategy.Bubble,typeof(RoutedEventHandler),typeof(LoadDraftEditor));
        public event RoutedEventHandler DraftCommitted { add => AddHandler(DraftCommittedEvent,value); remove => RemoveHandler(DraftCommittedEvent,value); }
        public LoadDraftEditor() { InitializeComponent(); }
        private void Put_Click(object sender,RoutedEventArgs e)
        {
            if (!(DataContext is VisualWorkspaceViewModel vm)) return;
            try { vm.SaveEditorToDraft(); RaiseEvent(new RoutedEventArgs(DraftCommittedEvent)); }
            catch (Exception ex) { vm.Status = "Không cập nhật nháp: " + ex.GetBaseException().Message; }
        }
        private void Reset_Click(object sender,RoutedEventArgs e) => (DataContext as VisualWorkspaceViewModel)?.ResetDraftEditor();
        private void Delete_Click(object sender,RoutedEventArgs e) => (DataContext as VisualWorkspaceViewModel)?.DeleteDraft();
    }
}
