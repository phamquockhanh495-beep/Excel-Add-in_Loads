using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private void SelectAttributes_Click(object sender,RoutedEventArgs e)
        {
            if(!vm.CanShowAll)return;
            StopPlanePicking();new VisualWorkspaceSelectView(vm){Owner=this}.ShowDialog();Canvas.Focus();
        }
        private void OpenDisplaySettings()
        {
            StopPlanePicking();DisplaySettingsButton.IsChecked=true;
        }
        private void OpenViewContextMenu()
        {
            var menu=new ContextMenu{PlacementTarget=Canvas,Placement=PlacementMode.MousePoint,Background=new SolidColorBrush(Color.FromRgb(15,23,42)),Foreground=Brushes.White,MinWidth=370,Padding=new Thickness(6)};
            menu.Items.Add(new MenuItem{Header=vm.Elevation?"Mặt cắt kết cấu 2D":"Mặt bằng kết cấu 2D",IsEnabled=false,Style=(Style)FindResource("WorkspaceContextItem")});
            menu.Items.Add(new Separator());
            void Item(string title,string shortcut,bool enabled,Action action) {
                var item=new MenuItem{Header=title,InputGestureText=shortcut,IsEnabled=enabled,Style=(Style)FindResource("WorkspaceContextItem")};
                item.Click+=(_,__)=>{StopPlanePicking();action();Canvas.Focus();};menu.Items.Add(item);
            }
            Item("1. Chỉ hiện đối tượng đang chọn (Isolate)","Ctrl+I",vm.CanRemoveFromWorkspace,vm.IsolateSelection);
            Item("2. Ẩn đối tượng đang chọn (Hide)","Ctrl+H",vm.CanRemoveFromWorkspace,vm.RemoveSelectedFromWorkspace);
            Item("3. Hiện lại toàn bộ đối tượng (Show All)","Ctrl+A",vm.CanShowAll,vm.ShowAllObjects);
            menu.Items.Add(new Separator());
            Item("4. Cài đặt hiển thị & Màu sắc (Settings…)","Alt+S",true,()=>Dispatcher.BeginInvoke(new Action(OpenDisplaySettings)));
            menu.IsOpen=true;
        }
    }
}
