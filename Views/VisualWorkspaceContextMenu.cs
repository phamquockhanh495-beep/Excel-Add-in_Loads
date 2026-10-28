using System;
using MaterialDesignThemes.Wpf;
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
        private void DisplayMenu_Click(object sender,RoutedEventArgs e)
        {
            var menu=new ContextMenu{PlacementTarget=(UIElement)sender,Placement=PlacementMode.Bottom};
            var text=new MenuItem{Header="Text · Cỡ chữ và màu RGB",Icon=new PackIcon{Kind=PackIconKind.FormatText,Width=18,Height=18}};
            text.Click+=(_,__)=>{StopPlanePicking();new VisualWorkspaceTextView(vm){Owner=this}.ShowDialog();Canvas.Focus();};
            var pane=new MenuItem{Header="Ẩn / hiện panel vùng xem"};pane.Click+=(_,args)=>ToggleDisplay_Click(sender,args);
            menu.Items.Add(text);menu.Items.Add(pane);menu.IsOpen=true;
        }
        private void OpenDisplaySettings()
        {
            StopPlanePicking();DisplaySettingsButton.IsChecked=true;
        }
        private void OpenViewContextMenu()
        {
            var menu=new ContextMenu{PlacementTarget=Canvas,Placement=PlacementMode.MousePoint,Background=new SolidColorBrush(Color.FromRgb(15,23,42)),Foreground=Brushes.White,MinWidth=380,Padding=new Thickness(6)};
            menu.Items.Add(new MenuItem{Header=vm.Elevation?"Mặt cắt kết cấu 2D":"Mặt bằng kết cấu 2D",IsEnabled=false,Style=(Style)FindResource("WorkspaceContextItem")});
            menu.Items.Add(new Separator());
            void Item(string title,string shortcut,PackIconKind icon,bool enabled,Action action) {
                var item=new MenuItem{Header=title,InputGestureText=shortcut,Icon=new PackIcon{Kind=icon,Width=18,Height=18},IsEnabled=enabled,Style=(Style)FindResource("WorkspaceContextItem")};
                if(icon==PackIconKind.PaletteOutline)((PackIcon)item.Icon).Foreground=Brushes.Gold;
                item.Click+=(_,__)=>{StopPlanePicking();action();Canvas.Focus();};menu.Items.Add(item);
            }
            Item("1. Chỉ hiện đối tượng đang chọn (Isolate)","Ctrl+I",PackIconKind.EyeOutline,vm.CanRemoveFromWorkspace,vm.IsolateSelection);
            Item("2. Ẩn đối tượng đang chọn (Hide)","Ctrl+H",PackIconKind.EyeOffOutline,vm.CanRemoveFromWorkspace,vm.RemoveSelectedFromWorkspace);
            Item("3. Hiện lại toàn bộ đối tượng (Show All)","Ctrl+A",PackIconKind.Eye,vm.CanShowAll,vm.ShowAllObjects);
            menu.Items.Add(new Separator());
            Item("4. Cài đặt hiển thị & Màu sắc (Settings…)","Alt+S",PackIconKind.PaletteOutline,true,()=>Dispatcher.BeginInvoke(new Action(OpenDisplaySettings)));
            menu.IsOpen=true;
        }
    }
}
