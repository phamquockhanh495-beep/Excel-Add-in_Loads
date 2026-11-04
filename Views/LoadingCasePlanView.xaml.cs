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
        private ListBox stories=null!,cases=null!;
        private ComboBox mode=null!,scope=null!;
        private CheckBox crop=null!,hidden=null!;
        private bool ready, profileWritable=true, fitPreview=true, settingZoom;
        private string warnings="";
        private static string ProfileFile=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Viewer","print-profiles.json");
        public LoadingCasePlanView(VisualWorkspaceViewModel vm,string path,Box2? bounds)
        {
            source=vm;model=vm.Snapshot??throw new ArgumentException("Đọc mô hình trước khi xuất PDF.");modelPath=path;viewport=bounds;
            display=vm.CapturePreferences(path);colors=new ObjectColorProfile(1,vm.ColorByProperty,vm.ObjectColors.Colors);text=new ViewerTextProfile(vm.StructureText,vm.LoadText);propertyColors=new Dictionary<string,string>(vm.ColorOverrides);visibleIds=vm.Scene.Select(e=>e.Source.Id).ToArray();currentStory=vm.Level?.Name??"";
            InitializeComponent();WorkspacePopupChrome.Attach(this);
            Width=Math.Min(Width,SystemParameters.WorkArea.Width-24);Height=Math.Min(Height,SystemParameters.WorkArea.Height-24);
            string warning="";try{if(File.Exists(ProfileFile))profile=PrintProfile.Read(ProfileFile);else{profile.Extrude=vm.Extrude;profile.Labels=vm.ShowLoadValues;profile.Frames=vm.ShowFrameLoads;profile.Shells=vm.ShowShellLoads;profile.Joints=vm.ShowJointLoads;if(!profile.Frames&&!profile.Shells&&!profile.Joints)profile.Frames=profile.Shells=profile.Joints=true;}}
            catch(Exception ex){profileWritable=false;warning="Không đọc được cấu hình in; đang dùng mặc định. File cũ chỉ thay khi bấm Lưu cấu hình. "+ex.Message;}
            DataContext=profile;BuildOptions();ready=true;
            warnings=warning;
            WarningsButton.Visibility=warning.Length>0?Visibility.Visible:Visibility.Collapsed;
            Closing+=(_,e)=>{if(work!=null){e.Cancel=true;Status.Text="Hủy tác vụ hoặc chờ hoàn tất trước khi đóng.";}};
        }
        private void Dirty(){if(!ready||work!=null)return;document=null;request=null;Pages.ItemsSource=null;Preview.Show(null,Zoom.Value);ExportButton.IsEnabled=false;Status.Text="Đã đổi thiết lập — cập nhật xem trước.";}
        private StackPanel Group(string title,bool open=false){var body=new StackPanel{Margin=new Thickness(6)};body.Children.Add(new TextBlock{Text=title,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,4,0,10)});Options.Children.Add(body);return body;}
        private void SetupButton(string title,int group){var button=new Button{Content=title,HorizontalContentAlignment=HorizontalAlignment.Left,Margin=new Thickness(6,4,6,4),MinHeight=36};button.Click+=(_,__)=>OpenSetup(title,group);Options.Children.Add(button);}
        private void OpenSetup(string title,int group)
        {
            if(work!=null)return;
            var draft=JsonCodec.Deserialize<PrintProfile>(JsonCodec.Serialize(profile));
            var dialog=new PrintSetupView(title,group,draft){Owner=this};
            if(dialog.ShowDialog()==true){profile=draft;DataContext=profile;Dirty();}
        }
        private void Check(StackPanel parent,string label,string property){var c=new CheckBox{Content=label,Margin=new Thickness(0,3,0,3)};c.SetBinding(CheckBox.IsCheckedProperty,new Binding(property){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});c.Checked+=(_,__)=>Dirty();c.Unchecked+=(_,__)=>Dirty();parent.Children.Add(c);}
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
            SetupButton("2. Giấy, margin và tỷ lệ…",2);
            SetupButton("3. Header / Footer…",3);
            SetupButton("4. Nội dung bản vẽ…",4);
        }
        private void FillCases(){var values=mode.SelectedIndex==1?model.Loads.Select(ReadLoadDisplay.LoadSetName).Where(s=>s.Length>0).Distinct(StringComparer.Ordinal).OrderBy(s=>s).ToArray():model.LoadPatterns.ToArray();cases.ItemsSource=values;string wanted=mode.SelectedIndex==1?source.UniformLoadSet:source.ReadPattern;if(values.Contains(wanted))cases.SelectedItem=wanted;else if(values.Length>0)cases.SelectedIndex=0;}
        private void ValidateFields(){if(profile.LockMargins)profile.Right=profile.Top=profile.Bottom=profile.Left;profile.Validate();}
        private PrintRequest Request(){ValidateFields();var floors=scope.SelectedIndex==2?model.Levels.OrderByDescending(l=>l.Elevation).Select(l=>l.Name).ToArray():scope.SelectedIndex==1?stories.SelectedItems.Cast<string>().ToArray():new[]{currentStory};if(hidden.IsChecked==true&&(floors.Length!=1||floors[0]!=currentStory))throw new ArgumentException("Ẩn/isolate hiện tại chỉ áp dụng cho tầng đang xem.");return new PrintRequest(model,profile,floors,cases.SelectedItems.Cast<string>(),mode.SelectedIndex==1,modelPath,display,colors,text,propertyColors,crop.IsChecked==true?viewport:null,hidden.IsChecked==true?visibleIds:null);}
        private void Busy(bool value){Options.IsEnabled=!value;BuildButton.IsEnabled=SaveButton.IsEnabled=!value;ExportButton.IsEnabled=!value&&document!=null;CancelButton.IsEnabled=value;Progress.IsIndeterminate=value;}
        private async void Build_Click(object sender,RoutedEventArgs e)
        {
            if(work!=null)return;document=null;Pages.ItemsSource=null;Preview.Show(null,Zoom.Value);ExportButton.IsEnabled=false;fitPreview=true;
            try{request=Request();work=new CancellationTokenSource();Busy(true);var token=work.Token;var report=new Progress<string>(s=>Status.Text=s);document=await Task.Run(()=>PlanPrintBuilder.Build(request,token,s=>((IProgress<string>)report).Report(s)));Pages.ItemsSource=document.Pages.Select((p,i)=>(i+1)+". "+p.Title).ToArray();Pages.SelectedIndex=0;Status.Text=document.Pages.Count+" trang · xem trước sẵn sàng";warnings=string.Join("\n",document.Warnings);WarningsButton.Visibility=warnings.Length>0?Visibility.Visible:Visibility.Collapsed;FitPreview();}
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
        private void Page_Changed(object sender,SelectionChangedEventArgs e){if(Preview==null)return;Preview.Show(document!=null&&Pages.SelectedIndex>=0?document.Pages[Pages.SelectedIndex]:null,Zoom.Value);if(fitPreview)Dispatcher.BeginInvoke(new Action(FitPreview));}
        private void Zoom_Changed(object sender,RoutedPropertyChangedEventArgs<double> e){if(settingZoom)return;if(Preview!=null){fitPreview=false;Preview.Show(Preview.Page,Zoom.Value);}}
        private void Fit_Click(object sender,RoutedEventArgs e){fitPreview=true;FitPreview();}
        private void Preview_SizeChanged(object sender,SizeChangedEventArgs e){if(fitPreview)Dispatcher.BeginInvoke(new Action(FitPreview));}
        private void FitPreview(){if(Preview?.Page==null||PreviewScroll==null)return;double w=PreviewScroll.ActualWidth-40,h=PreviewScroll.ActualHeight-40;if(w<=0||h<=0)return;settingZoom=true;try{Zoom.Value=PrintPreviewScale.Fit(Preview.Page.Width,Preview.Page.Height,w,h);Preview.Show(Preview.Page,Zoom.Value);}finally{settingZoom=false;}}
        private void Warnings_Click(object sender,RoutedEventArgs e){if(warnings.Length>0)MessageBox.Show(this,warnings,"Cảnh báo xuất PDF",MessageBoxButton.OK,MessageBoxImage.Information);}
        private void Cancel_Click(object sender,RoutedEventArgs e)=>work?.Cancel();
        private void Close_Click(object sender,RoutedEventArgs e)=>Close();
    }
}
