using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using QksExcelAddIn.Modeling.Printing;
namespace QksExcelAddIn.Views
{
    internal sealed class PrintSetupView:Window
    {
        private readonly PrintProfile profile;
        private readonly List<FrameworkElement> fields=new List<FrameworkElement>();
        internal PrintSetupView(string title,int group,PrintProfile draft)
        {
            profile=draft;Title="QKS · "+title;Width=560;Height=Math.Min(690,SystemParameters.WorkArea.Height-32);MinWidth=420;MinHeight=360;WindowStartupLocation=WindowStartupLocation.CenterOwner;ShowInTaskbar=false;FontSize=12;Background=System.Windows.Media.Brushes.White;Foreground=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16,42,76));
            Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/QksExcelAddIn;component/Views/Styles/VisualWorkspaceStyles.xaml")});
            DataContext=profile;var root=new DockPanel{Margin=new Thickness(14)};var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,10,0,0)};
            var apply=new Button{Content="Áp dụng",IsDefault=true};apply.Click+=(_,__)=>{try{foreach(var field in fields)if(Validation.GetHasError(field))throw new ArgumentException("Có ô nhập số không hợp lệ; dùng dấu thập phân '.'.");if(profile.LockMargins)profile.Right=profile.Top=profile.Bottom=profile.Left;profile.Validate();DialogResult=true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Kiểm tra thiết lập",MessageBoxButton.OK,MessageBoxImage.Warning);}};
            var cancel=new Button{Content="Hủy",IsCancel=true};buttons.Children.Add(apply);buttons.Children.Add(cancel);DockPanel.SetDock(buttons,Dock.Bottom);root.Children.Add(buttons);
            var body=new StackPanel{Margin=new Thickness(0,0,10,0)};root.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Content=root;WorkspacePopupChrome.Attach(this);
            if(group==2){var paper=body;
            var presets=new ComboBox{ItemsSource=new[]{"A4","A3","A2","A1","A0","Custom"},SelectedItem=profile.Paper};paper.Children.Add(presets);
            var width=Field(paper,"Rộng (mm)",nameof(profile.Width),true);var height=Field(paper,"Cao (mm)",nameof(profile.Height),true);
            presets.SelectionChanged+=(_,__)=>{string name=presets.SelectedItem as string??"Custom";profile.Paper=name;var sizes=new Dictionary<string,double[]>{{"A4",new[]{210d,297}},{"A3",new[]{297d,420}},{"A2",new[]{420d,594}},{"A1",new[]{594d,841}},{"A0",new[]{841d,1189}}};if(sizes.TryGetValue(name,out var v)){width.Text=v[0].ToString(CultureInfo.InvariantCulture);height.Text=v[1].ToString(CultureInfo.InvariantCulture);}};
            Check(paper,"Landscape (bỏ chọn: Portrait)",nameof(profile.Landscape));Check(paper,"Khóa bốn margin theo giá trị bên trái",nameof(profile.LockMargins));
            Field(paper,"Margin trái (mm)",nameof(profile.Left),true);Field(paper,"Margin phải (mm)",nameof(profile.Right),true);Field(paper,"Margin trên (mm)",nameof(profile.Top),true);Field(paper,"Margin dưới (mm)",nameof(profile.Bottom),true);
            Field(paper,"Khoảng cách vùng chữ / bản vẽ (mm)",nameof(profile.Gap),true);
            Check(paper,"Vừa vùng in (bỏ chọn: tỷ lệ cố định)",nameof(profile.Fit));Field(paper,"Mẫu số tỷ lệ 1:n",nameof(profile.Scale),true);Check(paper,"Ghép trang nếu vượt giấy ở tỷ lệ cố định",nameof(profile.Tile));Field(paper,"Chồng mép trang (mm)",nameof(profile.Overlap),true);
            }
            if(group==3){
                var tabs=new TabControl();body.Children.Add(tabs);
                StackPanel Tab(string title){var panel=new StackPanel{Margin=new Thickness(8)};tabs.Items.Add(new TabItem{Header=title,Content=panel});return panel;}
                var common=Tab("Chung");Field(common,"Tên dự án",nameof(profile.Project));Field(common,"Revision / số bản vẽ",nameof(profile.Revision));
                common.Children.Add(new TextBlock{Text="Biến: {Project}, {Model}, {Story}, {Elevation}, {Mode}, {Pattern}, {LoadSet}, {Scale}, {Date}, {Page}/{Pages}, {Revision}",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});
                Check(common,"Đường phân cách header/footer",nameof(profile.Rules));Check(common,"{Model} gồm đường dẫn đầy đủ",nameof(profile.FullModelPath));
                var logo=Field(common,"Logo PNG/JPEG (để trống để bỏ)",nameof(profile.Logo));var choose=new Button{Content="Chọn logo…"};choose.Click+=(_,__)=>{var dlg=new OpenFileDialog{Filter="Logo PNG/JPEG|*.png;*.jpg;*.jpeg"};if(dlg.ShowDialog(this)==true)logo.Text=dlg.FileName;};common.Children.Add(choose);
                var header=Tab("Header");Check(header,"Hiện Header",nameof(profile.Header));Field(header,"Chiều cao (mm)",nameof(profile.HeaderHeight),true);Field(header,"Trái",nameof(profile.HeaderLeft));Field(header,"Giữa",nameof(profile.HeaderCenter));Field(header,"Phải",nameof(profile.HeaderRight));Field(header,"Cỡ chữ (pt)",nameof(profile.HeaderFont),true);Check(header,"Đậm",nameof(profile.HeaderBold));Check(header,"Nghiêng",nameof(profile.HeaderItalic));PickColor(header,"Màu chữ",nameof(profile.HeaderColor));
                var footer=Tab("Footer");Check(footer,"Hiện Footer",nameof(profile.Footer));Field(footer,"Chiều cao (mm)",nameof(profile.FooterHeight),true);Field(footer,"Trái",nameof(profile.FooterLeft));Field(footer,"Giữa",nameof(profile.FooterCenter));Field(footer,"Phải",nameof(profile.FooterRight));Field(footer,"Cỡ chữ (pt)",nameof(profile.FooterFont),true);Check(footer,"Đậm",nameof(profile.FooterBold));Check(footer,"Nghiêng",nameof(profile.FooterItalic));PickColor(footer,"Màu chữ",nameof(profile.FooterColor));
            }
            if(group==4){var appearance=body;Check(appearance,"Hiện lưới trục",nameof(profile.Grids));Check(appearance,"Frame",nameof(profile.Frames));Check(appearance,"Shell",nameof(profile.Shells));Check(appearance,"Joint",nameof(profile.Joints));Check(appearance,"Kể cả tải bằng 0",nameof(profile.Zero));Check(appearance,"Hiện nhãn tải",nameof(profile.Labels));Check(appearance,"Hiện tên cấu kiện theo setup hiện tại",nameof(profile.MemberNames));Check(appearance,"Extrude theo setup từng loại",nameof(profile.Extrude));Check(appearance,"Chú giải tải",nameof(profile.Legend));Check(appearance,"Đặt chú giải bên phải (bỏ chọn: bên dưới)",nameof(profile.LegendRight));Field(appearance,"Rộng/cao vùng chú giải (mm)",nameof(profile.LegendSize),true);Field(appearance,"Cỡ chữ chú giải (pt)",nameof(profile.LegendFont),true);
            appearance.Children.Add(new TextBlock{Text="Load Sets luôn lấy các thành phần Shell trong set, độc lập Pattern và checkbox Frame/Joint. Màu/cỡ chữ tải và tên cấu kiện lấy từ Display tại lúc mở hộp thoại. Không đưa màu đang chọn đối tượng vào PDF.",TextWrapping=TextWrapping.Wrap,FontSize=10.5});

            }
        }
        private void Check(StackPanel parent,string label,string property){var c=new CheckBox{Content=label,Margin=new Thickness(0,3,0,3)};c.SetBinding(CheckBox.IsCheckedProperty,new Binding(property){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});parent.Children.Add(c);}
        private TextBox Field(StackPanel parent,string label,string property,bool number=false){parent.Children.Add(new TextBlock{Text=label,Margin=new Thickness(0,5,0,0),TextWrapping=TextWrapping.Wrap});var box=new TextBox{Margin=new Thickness(0,2,0,2),AcceptsReturn=!number,TextWrapping=TextWrapping.Wrap};box.SetBinding(TextBox.TextProperty,new Binding(property){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged,ValidatesOnExceptions=true,ConverterCulture=CultureInfo.InvariantCulture});fields.Add(box);parent.Children.Add(box);return box;}
        private void PickColor(StackPanel parent,string label,string property){var box=Field(parent,label+" (#RRGGBB)",property);var b=new Button{Content="Chọn RGB…"};b.Click+=(_,__)=>{using(var picker=new System.Windows.Forms.ColorDialog{FullOpen=true}){try{picker.Color=System.Drawing.ColorTranslator.FromHtml(box.Text);}catch{}if(picker.ShowDialog()==System.Windows.Forms.DialogResult.OK)box.Text="#"+picker.Color.R.ToString("X2")+picker.Color.G.ToString("X2")+picker.Color.B.ToString("X2");}};parent.Children.Add(b);}
    }
}
