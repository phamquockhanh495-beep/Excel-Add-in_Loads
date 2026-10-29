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
            var dialog=new Window{Title="QKS · DISPLAY",Width=460,Height=280,MinWidth=360,MinHeight=240,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=new SolidColorBrush(Color.FromRgb(248,250,252))};
            dialog.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/QksExcelAddIn;component/Views/Styles/VisualWorkspaceStyles.xaml")});
            var panel=new StackPanel{Margin=new Thickness(14)};
            void Entry(string title,Action action){var button=new Button{Content=title,Height=40,Margin=new Thickness(0,6,0,6)};button.Click+=(_,__)=>{dialog.Close();action();};panel.Children.Add(button);}
            Entry("Text · Cỡ chữ và màu RGB",()=>new VisualWorkspaceTextView(vm){Owner=this}.ShowDialog());
            Entry("Color Objects · Màu cấu kiện",()=>new VisualWorkspaceObjectColorsView(vm){Owner=this}.ShowDialog());
            Entry("Ẩn / hiện panel vùng xem",()=>ToggleDisplay_Click(sender,e));
            dialog.Content=panel;WorkspacePopupChrome.Attach(dialog);dialog.ShowDialog();Canvas.Focus();
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
