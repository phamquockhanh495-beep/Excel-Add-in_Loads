using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
namespace QksExcelAddIn.Views
{
    internal static class WorkspacePopupChrome
    {
        public static void Attach(Window window)
        {
            var content=(UIElement)window.Content;window.Content=null;
            window.WindowStyle=WindowStyle.None;window.ResizeMode=ResizeMode.CanResize;
            WindowChrome.SetWindowChrome(window,new WindowChrome{CaptionHeight=36,ResizeBorderThickness=new Thickness(5),CornerRadius=new CornerRadius(0),GlassFrameThickness=new Thickness(0),UseAeroCaptionButtons=false});
            var root=new Grid();root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(36)});root.RowDefinitions.Add(new RowDefinition());
            var header=new DockPanel{Background=new SolidColorBrush(Color.FromRgb(15,34,64))};
            var buttons=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Right);
            void Button(string text,string tip,string style,System.Action action){var b=new Button{Content=text,ToolTip=tip,Style=(Style)window.FindResource(style)};WindowChrome.SetIsHitTestVisibleInChrome(b,true);b.Click+=(_,__)=>action();buttons.Children.Add(b);}
            Button("—","Thu nhỏ","QksTitleBarButton",()=>window.WindowState=WindowState.Minimized);
            Button("□","Phóng to / Khôi phục","QksTitleBarButton",()=>window.WindowState=window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized);
            Button("✕","Đóng","QksTitleBarCloseButton",window.Close);
            header.Children.Add(buttons);header.Children.Add(new TextBlock{Text=window.Title,Foreground=Brushes.White,FontWeight=FontWeights.SemiBold,FontSize=12,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,8,0),TextTrimming=TextTrimming.CharacterEllipsis});
            root.Children.Add(header);Grid.SetRow(content,1);root.Children.Add(content);
            window.Content=new Border{BorderBrush=header.Background,BorderThickness=new Thickness(1),Child=root};
        }
    }
}
