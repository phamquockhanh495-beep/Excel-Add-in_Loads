using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Modeling.Printing;
using QksExcelAddIn.ViewModels;
using Qks.Shared;
namespace QksExcelAddIn.Views
{
    public partial class LoadingCasePlanView:Window
    {
        private readonly VisualWorkspaceViewModel source;
        private readonly ModelSnapshot model;
        private readonly string modelPath;
        private readonly Box2? viewport;
        private readonly ViewerPreferences display;
        private readonly ObjectColorProfile colors;
        private readonly ViewerTextProfile text;
        private readonly Dictionary<string,string> propertyColors;
        private readonly string[] visibleIds;
        private readonly string currentStory;
        private PrintProfile profile=new PrintProfile();
        private PrintRequest? request;
        private PrintDocument? document;
        private CancellationTokenSource? work;
        private readonly List<FrameworkElement> fields=new List<FrameworkElement>();
        private ListBox stories=null!,cases=null!;
        private ComboBox mode=null!,scope=null!;
        private CheckBox crop=null!,hidden=null!;
        private bool ready, profileWritable=true;
        private static string ProfileFile=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Viewer","print-profiles.json");
        public LoadingCasePlanView(VisualWorkspaceViewModel vm,string path,Box2? bounds)
        {
            source=vm;model=vm.Snapshot??throw new ArgumentException("Đọc mô hình trước khi xuất PDF.");modelPath=path;viewport=bounds;
            display=vm.CapturePreferences(path);colors=new ObjectColorProfile(1,vm.ColorByProperty,vm.ObjectColors.Colors);text=new ViewerTextProfile(vm.StructureText,vm.LoadText);propertyColors=new Dictionary<string,string>(vm.ColorOverrides);visibleIds=vm.Scene.Select(e=>e.Source.Id).ToArray();currentStory=vm.Level?.Name??"";
            InitializeComponent();WorkspacePopupChrome.Attach(this);
            string warning="";try{if(File.Exists(ProfileFile))profile=PrintProfile.Read(ProfileFile);else{profile.Extrude=vm.Extrude;profile.Labels=vm.ShowLoadValues;profile.Frames=vm.ShowFrameLoads;profile.Shells=vm.ShowShellLoads;profile.Joints=vm.ShowJointLoads;if(!profile.Frames&&!profile.Shells&&!profile.Joints)profile.Frames=profile.Shells=profile.Joints=true;}}
            catch(Exception ex){profileWritable=false;warning="Không đọc được cấu hình in; đang dùng mặc định. File cũ chỉ thay khi bấm Lưu cấu hình. "+ex.Message;}
            DataContext=profile;BuildOptions();ready=true;
            Notes.Text="Dữ liệu: "+model.CapturedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")+" · "+Path.GetFileName(path)+"\n"+warning+"\n"+string.Join("\n",model.Diagnostics.Take(10))+"\nLưới trục ETABS chưa có trong dữ liệu đọc hiện tại nên chưa đưa vào PDF.";
            Closing+=(_,e)=>{if(work!=null){e.Cancel=true;Status.Text="Hủy tác vụ hoặc chờ hoàn tất trước khi đóng.";}};
        }
        private void Dirty(){if(!ready||work!=null)return;document=null;request=null;Pages.ItemsSource=null;Preview.Show(null,Zoom.Value);ExportButton.IsEnabled=false;Status.Text="Đã đổi thiết lập — cập nhật xem trước.";}
        private StackPanel Group(string title,bool open=false){var body=new StackPanel{Margin=new Thickness(6)};Options.Children.Add(new Expander{Header=title,Content=body,IsExpanded=open,Margin=new Thickness(0,5,0,0)});return body;}
        private void Check(StackPanel parent,string label,string property){var c=new CheckBox{Content=label,Margin=new Thickness(0,3,0,3)};c.SetBinding(CheckBox.IsCheckedProperty,new Binding(property){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});c.Checked+=(_,__)=>Dirty();c.Unchecked+=(_,__)=>Dirty();parent.Children.Add(c);}
        private TextBox Field(StackPanel parent,string label,string property,bool number=false){parent.Children.Add(new TextBlock{Text=label,Margin=new Thickness(0,5,0,0),TextWrapping=TextWrapping.Wrap});var box=new TextBox{Margin=new Thickness(0,2,0,2),AcceptsReturn=!number,TextWrapping=TextWrapping.Wrap};box.SetBinding(TextBox.TextProperty,new Binding(property){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged,ValidatesOnExceptions=true,ConverterCulture=CultureInfo.InvariantCulture});box.TextChanged+=(_,__)=>Dirty();fields.Add(box);parent.Children.Add(box);return box;}
        private void PickColor(StackPanel parent,string label,string property){var box=Field(parent,label+" (#RRGGBB)",property);var b=new Button{Content="Chọn RGB…"};b.Click+=(_,__)=>{using(var picker=new System.Windows.Forms.ColorDialog{FullOpen=true}){try{picker.Color=System.Drawing.ColorTranslator.FromHtml(box.Text);}catch{}if(picker.ShowDialog()==System.Windows.Forms.DialogResult.OK)box.Text="#"+picker.Color.R.ToString("X2")+picker.Color.G.ToString("X2")+picker.Color.B.ToString("X2");}};parent.Children.Add(b);}
        private void BuildOptions()
        {
            var selection=Group("1. Tầng và tải trọng",true);
            mode=new ComboBox{ItemsSource=new[]{"Load Pattern","Load Sets"},SelectedIndex=source.LoadSetsMode?1:0};selection.Children.Add(mode);
            scope=new ComboBox{ItemsSource=new[]{"Tầng đang xem","Chọn các tầng","Toàn bộ tầng"},SelectedIndex=0};selection.Children.Add(scope);
            stories=new ListBox{ItemsSource=model.Levels.OrderByDescending(l=>l.Elevation).Select(l=>l.Name),SelectionMode=SelectionMode.Extended,Height=120};stories.SelectedItem=currentStory;selection.Children.Add(stories);
            selection.Children.Add(new TextBlock{Text="Ctrl / Shift để chọn nhiều tầng hoặc Pattern/Set",FontSize=10.5,TextWrapping=TextWrapping.Wrap});
            cases=new ListBox{SelectionMode=SelectionMode.Extended,Height=135};selection.Children.Add(cases);FillCases();mode.SelectionChanged+=(_,__)=>{FillCases();Dirty();};scope.SelectionChanged+=(_,__)=>{stories.IsEnabled=scope.SelectedIndex==1;Dirty();};stories.IsEnabled=false;stories.SelectionChanged+=(_,__)=>Dirty();cases.SelectionChanged+=(_,__)=>Dirty();
            Check(selection,"Gộp các Pattern / Set đã chọn trên một mặt bằng",nameof(profile.Combined));
            Check(selection,"Tách file PDF theo tầng (cùng một thư mục)",nameof(profile.SplitStories));
            crop=new CheckBox{Content="Chỉ vùng XY đang xem (Plan)",IsEnabled=viewport.HasValue};selection.Children.Add(crop);crop.Checked+=(_,__)=>Dirty();crop.Unchecked+=(_,__)=>Dirty();
            hidden=new CheckBox{Content="Áp dụng ẩn/isolate hiện tại (chỉ tầng đang xem)",IsEnabled=viewport.HasValue};selection.Children.Add(hidden);hidden.Checked+=(_,__)=>Dirty();hidden.Unchecked+=(_,__)=>Dirty();
            var paper=Group("2. Giấy, margin và tỷ lệ",true);
            var presets=new ComboBox{ItemsSource=new[]{"A4","A3","A2","A1","A0","Custom"},SelectedItem=profile.Paper};paper.Children.Add(presets);
            var width=Field(paper,"Rộng (mm)",nameof(profile.Width),true);var height=Field(paper,"Cao (mm)",nameof(profile.Height),true);
            presets.SelectionChanged+=(_,__)=>{string name=presets.SelectedItem as string??"Custom";profile.Paper=name;var sizes=new Dictionary<string,double[]>{{"A4",new[]{210d,297}},{"A3",new[]{297d,420}},{"A2",new[]{420d,594}},{"A1",new[]{594d,841}},{"A0",new[]{841d,1189}}};if(sizes.TryGetValue(name,out var v)){width.Text=v[0].ToString(CultureInfo.InvariantCulture);height.Text=v[1].ToString(CultureInfo.InvariantCulture);}Dirty();};
            Check(paper,"Landscape (bỏ chọn: Portrait)",nameof(profile.Landscape));Check(paper,"Khóa bốn margin theo giá trị bên trái",nameof(profile.LockMargins));
            Field(paper,"Margin trái (mm)",nameof(profile.Left),true);Field(paper,"Margin phải (mm)",nameof(profile.Right),true);Field(paper,"Margin trên (mm)",nameof(profile.Top),true);Field(paper,"Margin dưới (mm)",nameof(profile.Bottom),true);
            Field(paper,"Khoảng cách vùng chữ / bản vẽ (mm)",nameof(profile.Gap),true);
            Check(paper,"Vừa vùng in (bỏ chọn: tỷ lệ cố định)",nameof(profile.Fit));Field(paper,"Mẫu số tỷ lệ 1:n",nameof(profile.Scale),true);Check(paper,"Ghép trang nếu vượt giấy ở tỷ lệ cố định",nameof(profile.Tile));Field(paper,"Chồng mép trang (mm)",nameof(profile.Overlap),true);
            var bands=Group("3. Header / Footer");Field(bands,"Tên dự án",nameof(profile.Project));Field(bands,"Revision / số bản vẽ",nameof(profile.Revision));
            bands.Children.Add(new TextBlock{Text="Biến: {Project}, {Model}, {Story}, {Elevation}, {Mode}, {Pattern}, {LoadSet}, {Scale}, {Date}, {Page}/{Pages}, {Revision}",TextWrapping=TextWrapping.Wrap,FontSize=10.5});
            Check(bands,"Header",nameof(profile.Header));Field(bands,"Chiều cao header (mm)",nameof(profile.HeaderHeight),true);Field(bands,"Trái",nameof(profile.HeaderLeft));Field(bands,"Giữa",nameof(profile.HeaderCenter));Field(bands,"Phải",nameof(profile.HeaderRight));Field(bands,"Cỡ chữ header (pt)",nameof(profile.HeaderFont),true);Check(bands,"Header đậm",nameof(profile.HeaderBold));Check(bands,"Header nghiêng",nameof(profile.HeaderItalic));PickColor(bands,"Màu header",nameof(profile.HeaderColor));
            Check(bands,"Footer",nameof(profile.Footer));Field(bands,"Chiều cao footer (mm)",nameof(profile.FooterHeight),true);Field(bands,"Trái",nameof(profile.FooterLeft));Field(bands,"Giữa",nameof(profile.FooterCenter));Field(bands,"Phải",nameof(profile.FooterRight));Field(bands,"Cỡ chữ footer (pt)",nameof(profile.FooterFont),true);Check(bands,"Footer đậm",nameof(profile.FooterBold));Check(bands,"Footer nghiêng",nameof(profile.FooterItalic));PickColor(bands,"Màu footer",nameof(profile.FooterColor));Check(bands,"Đường phân cách header/footer",nameof(profile.Rules));Check(bands,"{Model} gồm đường dẫn đầy đủ",nameof(profile.FullModelPath));
            var logo=Field(bands,"Logo PNG/JPEG (để trống để bỏ)",nameof(profile.Logo));var choose=new Button{Content="Chọn logo…"};choose.Click+=(_,__)=>{var dlg=new OpenFileDialog{Filter="Logo PNG/JPEG|*.png;*.jpg;*.jpeg"};if(dlg.ShowDialog(this)==true)logo.Text=dlg.FileName;};bands.Children.Add(choose);
            var appearance=Group("4. Nội dung bản vẽ");Check(appearance,"Frame",nameof(profile.Frames));Check(appearance,"Shell",nameof(profile.Shells));Check(appearance,"Joint",nameof(profile.Joints));Check(appearance,"Kể cả tải bằng 0",nameof(profile.Zero));Check(appearance,"Hiện nhãn tải",nameof(profile.Labels));Check(appearance,"Hiện tên cấu kiện theo setup hiện tại",nameof(profile.MemberNames));Check(appearance,"Extrude theo setup từng loại",nameof(profile.Extrude));Check(appearance,"Chú giải tải",nameof(profile.Legend));Check(appearance,"Đặt chú giải bên phải (bỏ chọn: bên dưới)",nameof(profile.LegendRight));Field(appearance,"Rộng/cao vùng chú giải (mm)",nameof(profile.LegendSize),true);Field(appearance,"Cỡ chữ chú giải (pt)",nameof(profile.LegendFont),true);
            appearance.Children.Add(new TextBlock{Text="Load Sets luôn lấy các thành phần Shell trong set, độc lập Pattern và checkbox Frame/Joint. Màu/cỡ chữ tải và tên cấu kiện lấy từ Display tại lúc mở hộp thoại. Không đưa màu đang chọn đối tượng vào PDF.",TextWrapping=TextWrapping.Wrap,FontSize=10.5});
        }
        private void FillCases(){var values=mode.SelectedIndex==1?model.Loads.Select(ReadLoadDisplay.LoadSetName).Where(s=>s.Length>0).Distinct(StringComparer.Ordinal).OrderBy(s=>s).ToArray():model.LoadPatterns.ToArray();cases.ItemsSource=values;string wanted=mode.SelectedIndex==1?source.UniformLoadSet:source.ReadPattern;if(values.Contains(wanted))cases.SelectedItem=wanted;else if(values.Length>0)cases.SelectedIndex=0;}
        private void ValidateFields(){foreach(var field in fields)if(Validation.GetHasError(field))throw new ArgumentException("Có ô nhập số không hợp lệ; dùng dấu thập phân '.'.");if(profile.LockMargins)profile.Right=profile.Top=profile.Bottom=profile.Left;profile.Validate();}
        private PrintRequest Request(){ValidateFields();var floors=scope.SelectedIndex==2?model.Levels.OrderByDescending(l=>l.Elevation).Select(l=>l.Name).ToArray():scope.SelectedIndex==1?stories.SelectedItems.Cast<string>().ToArray():new[]{currentStory};if(hidden.IsChecked==true&&(floors.Length!=1||floors[0]!=currentStory))throw new ArgumentException("Ẩn/isolate hiện tại chỉ áp dụng cho tầng đang xem.");return new PrintRequest(model,profile,floors,cases.SelectedItems.Cast<string>(),mode.SelectedIndex==1,modelPath,display,colors,text,propertyColors,crop.IsChecked==true?viewport:null,hidden.IsChecked==true?visibleIds:null);}
        private void Busy(bool value){Options.IsEnabled=!value;BuildButton.IsEnabled=SaveButton.IsEnabled=!value;ExportButton.IsEnabled=!value&&document!=null;CancelButton.IsEnabled=value;Progress.IsIndeterminate=value;}
        private async void Build_Click(object sender,RoutedEventArgs e)
        {
            if(work!=null)return;document=null;Pages.ItemsSource=null;Preview.Show(null,Zoom.Value);ExportButton.IsEnabled=false;
            try{request=Request();work=new CancellationTokenSource();Busy(true);var token=work.Token;var report=new Progress<string>(s=>Status.Text=s);document=await Task.Run(()=>PlanPrintBuilder.Build(request,token,s=>((IProgress<string>)report).Report(s)));Pages.ItemsSource=document.Pages.Select((p,i)=>(i+1)+". "+p.Title).ToArray();Pages.SelectedIndex=0;Status.Text=document.Pages.Count+" trang · xem trước sẵn sàng";Notes.Text="Dữ liệu đọc: "+model.CapturedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")+"\n"+(document.Warnings.Count==0?"Không có cảnh báo từ nguồn đọc / dàn trang.":string.Join("\n",document.Warnings));}
            catch(OperationCanceledException){Status.Text="Đã hủy dựng trang.";}catch(Exception ex){Status.Text="Chưa dựng được PDF.";MessageBox.Show(this,ex.GetBaseException().Message,"Loading Case Plan",MessageBoxButton.OK,MessageBoxImage.Warning);}
            finally{work?.Dispose();work=null;Busy(false);}
        }
        private void Save_Click(object sender,RoutedEventArgs e){try{ValidateFields();profile.Save(ProfileFile);profileWritable=true;Status.Text="Đã lưu cấu hình in trên máy này.";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Lưu cấu hình in",MessageBoxButton.OK,MessageBoxImage.Warning);}}
        private async void Export_Click(object sender,RoutedEventArgs e)
        {
            if(work!=null||document==null||request==null)return;string path;
            if(request.Profile.SplitStories){using(var folder=new System.Windows.Forms.FolderBrowserDialog{Description="Chọn thư mục xuất PDF theo tầng; các file QKS_<tầng>.pdf trùng tên sẽ được thay sau khi xuất thành công.",ShowNewFolderButton=true}){if(folder.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return;path=folder.SelectedPath;}}
            else{var save=new SaveFileDialog{Filter="PDF (*.pdf)|*.pdf",DefaultExt=".pdf",AddExtension=true,FileName="QKS_Loading_Case_Plan.pdf",OverwritePrompt=true};if(save.ShowDialog(this)!=true)return;path=save.FileName;}
            try{work=new CancellationTokenSource();Busy(true);var token=work.Token;var report=new Progress<string>(s=>Status.Text=s);var doc=document;bool split=request.Profile.SplitStories;await Task.Run(()=>{Action<int,int> progress=(a,b)=>((IProgress<string>)report).Report("Đang xuất "+a+" / "+b+" trang");if(split)PlanPdfWriter.WriteByStory(path,doc,token,progress);else PlanPdfWriter.Write(path,doc.Pages,token,progress);});Status.Text="Đã xuất "+doc.Pages.Count+" trang: "+path;try{if(profileWritable)request.Profile.Save(ProfileFile);}catch(Exception ex){MessageBox.Show(this,"PDF đã xuất. Chưa lưu cấu hình in: "+ex.Message);}}
            catch(OperationCanceledException){Status.Text="Đã hủy xuất; giữ file đích cũ.";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Xuất PDF thất bại",MessageBoxButton.OK,MessageBoxImage.Error);Status.Text="Xuất chưa hoàn tất.";}finally{work?.Dispose();work=null;Busy(false);}
        }
        private void Page_Changed(object sender,SelectionChangedEventArgs e){if(Preview!=null)Preview.Show(document!=null&&Pages.SelectedIndex>=0?document.Pages[Pages.SelectedIndex]:null,Zoom.Value);}
        private void Zoom_Changed(object sender,RoutedPropertyChangedEventArgs<double> e){if(Preview!=null)Preview.Show(Preview.Page,Zoom.Value);}
        private void Cancel_Click(object sender,RoutedEventArgs e)=>work?.Cancel();
        private void Close_Click(object sender,RoutedEventArgs e)=>Close();
    }
}
