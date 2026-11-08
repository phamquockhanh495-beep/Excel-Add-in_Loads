#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services;
using QksExcelAddIn.Services.Modeling;
using QksExcelAddIn.ViewModels;

namespace QksExcelAddIn.Views
{
    public partial class WindLoadView : Window
    {
        private readonly WindWorkspaceViewModel vm=new WindWorkspaceViewModel();
        private CancellationTokenSource? cancellation;
        private EtabsInstanceInfo? sourceConnection;
        private string savedProject="",baselineProject="";
        private bool closed;
        private Vec2? sectionStart;
        private WindUiSettings settings=new WindUiSettings();
        private static string SettingsFile=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Wind","ui.json");
        public WindLoadView()
        {
            InitializeComponent();DataContext=vm;WorkspacePopupChrome.Attach(this);WorkspaceWindowPlacement.Attach(this);
            BuildUserColumns();
            Canvas.SelectionRequested+=(ids,change)=>vm.Viewer.SelectElements(ids.Where(id=>vm.Snapshot?.Elements.Any(e=>e.Id==id&&e.Kind==ElementKind.Beam)==true).ToArray(),change);
            Canvas.ViewContextRequested+=OpenCanvasMenu;
            Canvas.PlanePointPicked+=point=>{
                if(!sectionStart.HasValue){sectionStart=point;Canvas.FirstPlanePoint=point;Canvas.InvalidateVisual();return;}
                Try(()=>{vm.Viewer.SetElevationThrough(sectionStart.Value,point);StopSection();Canvas.Fit();});
            };
            vm.Viewer.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(vm.Viewer.Level)||e.PropertyName==nameof(vm.Viewer.IsolateStory))Dispatcher.BeginInvoke(new Action(Canvas.Fit));};
            PreviewKeyDown+=Keys;
            AppConnectionManager.ConnectionChanged+=ConnectionChanged;
            Closing+=(_,e)=>{
                if(vm.Busy){e.Cancel=true;vm.Status="Hủy hoặc chờ tác vụ kết thúc trước khi đóng.";return;}
                if(IsDirty()&&MessageBox.Show(this,"Bộ gió đã thay đổi. Đóng mà không lưu?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes){e.Cancel=true;return;}
                Try(SaveSettings);
            };
            Closed+=(_,__)=>{closed=true;cancellation?.Cancel();AppConnectionManager.ConnectionChanged-=ConnectionChanged;};
            Loaded+=async(_,__)=>{Try(LoadSettings);if(AppConnectionManager.IsEtabsConnected)await ReadModel();};
        }
        private void BuildUserColumns()
        {
            var story=new DataGridComboBoxColumn{Header="Story (đối chiếu)",SelectedItemBinding=new Binding("Story"){UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged},Width=170};UserGrid.Columns.Add(story);
            UserGrid.Columns.Add(new DataGridTextColumn{Header="Khóa diaphragm API",Binding=new Binding("Key"),IsReadOnly=true,Width=180});
            foreach(var c in new[]{("Fx (kN)","Fx"),("Fy (kN)","Fy"),("Mz (kN·m)","Mz"),("X (m)","X"),("Y (m)","Y")})UserGrid.Columns.Add(new DataGridTextColumn{Header=c.Item1,Binding=new Binding(c.Item2){UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged,ValidatesOnDataErrors=true},Width=new DataGridLength(1,DataGridLengthUnitType.Star),MinWidth=95});
        }
        private void ConnectionChanged()=>Dispatcher.BeginInvoke(new Action(()=>{if(closed)return;cancellation?.Cancel();vm.MarkStale();ModelText.Text="Kết nối đã thay đổi · Đọc lại ETABS";}));
        private void GuardSource()
        {
            if(sourceConnection==null||!ReferenceEquals(sourceConnection,AppConnectionManager.ActiveEtabs))throw new InvalidOperationException("Kết nối đã đổi. Đọc lại ETABS.");
            new EtabsLoadApplyHost(s=>vm.Status=s).Guard();
        }
        private void Try(Action action){try{action();}catch(Exception ex){vm.Status=ex.GetBaseException().Message;}}
        private async Task Run(Func<Task> action)
        {
            if(vm.Busy)return;vm.Busy=true;vm.Viewer.Busy=true;cancellation=new CancellationTokenSource();
            try{await action();}catch(OperationCanceledException){vm.Status="Đã hủy tác vụ đọc/chuẩn bị.";}catch(Exception ex){vm.Status=ex.GetBaseException().Message;}
            finally{cancellation.Dispose();cancellation=null;vm.Viewer.Busy=false;vm.Busy=false;}
        }
        private bool IsDirty()=>vm.Snapshot!=null&&JsonCodec.Serialize(vm.Project())!=(savedProject.Length>0?savedProject:baselineProject);
        private async Task ReadModel()
        {
            if(IsDirty()&&MessageBox.Show(this,"Đọc lại sẽ thay bộ dữ liệu đang soạn. Tiếp tục? Lưu bộ gió trước nếu cần giữ lại.","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            await Run(async()=>{
                if(!AppConnectionManager.IsEtabsConnected)throw new InvalidOperationException("Chọn ETABS trong Kết nối API. Module gán gió chưa hỗ trợ SAFE.");
                var connection=AppConnectionManager.ActiveEtabs;
                var snapshot=await new EtabsSnapshotReader().ReadAsync(s=>vm.Status=s,cancellation!.Token,true);
                if(!ReferenceEquals(connection,AppConnectionManager.ActiveEtabs))throw new InvalidOperationException("Kết nối đã đổi trong khi đọc.");
                var patterns=WindHostInspection.Patterns();vm.SetSnapshot(snapshot,patterns);sourceConnection=connection;
                ((DataGridComboBoxColumn)UserGrid.Columns[0]).ItemsSource=snapshot.Levels.OrderByDescending(l=>l.Elevation).Select(l=>l.Name).ToArray();
                ModelText.Text=AppConnectionManager.GetEtabsDisplayName();ApplySettings();
                savedProject="";baselineProject=JsonCodec.Serialize(vm.Project());Canvas.Fit();
                vm.Status=patterns.Length==0?"Model chưa có Load Pattern loại Wind. Tạo pattern trong ETABS rồi đọc lại.":"Đã đọc model. Chọn pattern và nhập giá trị gió đã tính.";
            });
        }
        private async void ReadClick(object sender,RoutedEventArgs e)=>await ReadModel();
        private void ConnectClick(object sender,RoutedEventArgs e)=>new GetApiView().Show();
        private void CancelClick(object sender,RoutedEventArgs e)=>cancellation?.Cancel();
        private void ReadUsersClick(object sender,RoutedEventArgs e)=>Try(()=>{
            GuardSource();
            if(vm.UserRows.Count>0&&MessageBox.Show(this,"Đọc lại User Loads sẽ thay bảng đang chỉnh của Tab 1. Tiếp tục?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            var state=new EtabsLoadApplyHost(s=>vm.Status=s).ReadUserWind(vm.UserPattern);vm.SetUserBaseline(state);
            vm.Status=state.Rows.Count==0?"Pattern chưa có khóa User Loads. Khởi tạo hàng trong ETABS rồi Đọc User Loads lại.":"Đã đọc "+state.Rows.Count+" khóa. Đối chiếu Story trong ETABS trước khi chỉnh/gán.";
        });
        private void ResetUsersClick(object sender,RoutedEventArgs e)=>Try(()=>{
            if(vm.UserBaseline!=null&&MessageBox.Show(this,"Bỏ các chỉnh sửa và khôi phục giá trị lần đọc gần nhất?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)vm.SetUserBaseline(vm.UserBaseline);
        });
        private void UserSettingsClick(object sender,RoutedEventArgs e)=>Try(()=>{
            GuardSource();if(vm.Snapshot==null)return;
            if(vm.UserRows.Count>0&&MessageBox.Show(this,"Bảng nâng cao đọc dữ liệu từ ETABS, không tự chuyển các ô đang sửa trong bảng khóa API. Tiếp tục?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            var view=new WindUserSetsView(vm.Snapshot,vm.UserPattern){Owner=this};view.ShowDialog();if(view.MayHaveChanges)vm.MarkStale();
        });
        private Window Popup(string title,UIElement content,double width,double height)
        {
            var dialog=new Window{Title=title,Content=content,Width=width,Height=height,MinWidth=400,MinHeight=220,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=new SolidColorBrush(Color.FromRgb(248,250,252)),FontFamily=new FontFamily("Segoe UI"),FontSize=12};
            dialog.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/QksExcelAddIn;component/Views/Styles/VisualWorkspaceStyles.xaml")});WorkspacePopupChrome.Attach(dialog);return dialog;
        }
        private bool CommitGrid()=>UserGrid.CommitEdit(DataGridEditingUnit.Cell,true)&&UserGrid.CommitEdit(DataGridEditingUnit.Row,true);
        private void ReviewClick(object sender,RoutedEventArgs e)=>Try(()=>{
            if(!CommitGrid())return;GuardSource();
            object[] rows;string summary;
            if(vm.Tab==0){var doc=vm.UserDocument();rows=doc.Changes.Select(r=>{var b=doc.Before.Rows.Single(x=>x.DiaphragmKey==r.DiaphragmKey);return (object)new{Story=doc.StoryAssignments[r.DiaphragmKey],Key=r.DiaphragmKey,Before=$"Fx={b.Fx}; Fy={b.Fy}; Mz={b.Mz}; X={b.X}; Y={b.Y}",After=$"Fx={r.Fx}; Fy={r.Fy}; Mz={r.Mz}; X={r.X}; Y={r.Y}"};}).ToArray();summary=doc.Before.Pattern+" · "+doc.Changes.Count+" khóa thay đổi · kN / m. Các khóa khác giữ nguyên.";}
            else{var doc=vm.BeamDocument();var host=new EtabsLoadApplyHost(s=>vm.Status=s);host.ValidateDocument(doc);var plan=ApplyPlan.Create(vm.Snapshot!,doc,host.Capabilities);rows=plan.Groups.SelectMany(g=>g.Before.Select(l=>(object)new{Story=g.Element.Story,UniqueName=g.Element.Name,Phase="Trước",l.Pattern,l.Direction,l.Start,l.End,l.Value1,l.Value2,l.Unit}).Concat(g.After.Select(l=>(object)new{Story=g.Element.Story,UniqueName=g.Element.Name,Phase="Sau",l.Pattern,l.Direction,l.Start,l.End,l.Value1,l.Value2,l.Unit}))).ToArray();summary=vm.BeamSummary+". Bảng liệt kê cả tải được giữ lại.";}
            var panel=new DockPanel{Margin=new Thickness(12)};var heading=new TextBlock{Text=summary,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(heading,Dock.Top);panel.Children.Add(heading);
            var button=new Button{Content="Đã xem · Sẵn sàng gán",HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(button,Dock.Bottom);panel.Children.Add(button);
            var grid=new DataGrid{ItemsSource=rows,IsReadOnly=true,AutoGenerateColumns=true,CanUserAddRows=false};panel.Children.Add(grid);
            var dialog=Popup("QKS · KIỂM TRA TRƯỚC / SAU",panel,1120,640);button.Click+=(_,__)=>{vm.AcceptReview();dialog.DialogResult=true;};dialog.ShowDialog();vm.Status=vm.CanApply?"Đã kiểm tra và xem trước. Bấm Gán ETABS để thực hiện.":"Chưa xác nhận bảng trước/sau.";
        });
        private async void ApplyClick(object sender,RoutedEventArgs e)
        {
            if(!CommitGrid())return;
            try{GuardSource();vm.AssertReviewed();}catch(Exception ex){vm.Status=ex.Message;return;}
            // Freeze the reviewed document before Busy disables the editor.
            UserWindDocument? user=null;DraftDocument? beams=null;
            try{if(vm.Tab==0)user=vm.UserDocument();else beams=vm.BeamDocument();}catch(Exception ex){vm.Status=ex.Message;return;}
            vm.Invalidate();
            await Run(async()=>{
                GuardSource();var host=new EtabsLoadApplyHost(s=>vm.Status=s);
                string outcome,error,backup;bool changed,restored;
                if(user!=null){var result=await UserWindApply.ExecuteAsync(user,host,cancellation!.Token);outcome=result.Outcome;error=result.Error;backup=result.BackupPath;changed=result.MayHaveChanges;restored=result.UnitsRestored;}
                else{var result=await LoadApply.ExecuteAsync(beams!,host,cancellation!.Token);outcome=result.Outcome;error=result.Error;backup=result.BackupPath;changed=result.MayHaveChanges;restored=result.UnitsRestored;}
                if(changed||!restored)vm.MarkStale();
                vm.Status=outcome+" · "+error+(backup.Length>0?" · Backup: "+backup:"")+(changed?" · Đọc lại ETABS trước lần ghi tiếp.":"");
            });
        }
        private void StageClick(object sender,RoutedEventArgs e)=>Try(vm.StageSelected);
        private void DeleteBeamClick(object sender,RoutedEventArgs e)=>Try(()=>{var ids=new HashSet<string>(BeamGrid.SelectedItems.Cast<DraftLoad>().Select(l=>l.Id));vm.Beams!.ReplaceLoads(vm.BeamRows.Where(l=>!ids.Contains(l.Id)).ToArray());vm.NotifyBeams();});
        private void UndoClick(object sender,RoutedEventArgs e)=>Try(()=>{vm.Beams?.Undo();vm.NotifyBeams();});
        private void RedoClick(object sender,RoutedEventArgs e)=>Try(()=>{vm.Beams?.Redo();vm.NotifyBeams();});
        private void UndoUsersClick(object sender,RoutedEventArgs e)=>Try(()=>{if(CommitGrid())vm.UndoUsers();});
        private void RedoUsersClick(object sender,RoutedEventArgs e)=>Try(()=>{if(CommitGrid())vm.RedoUsers();});
        private void BeamSelected(object sender,SelectionChangedEventArgs e){if(BeamGrid.SelectedItems.Count==1&&BeamGrid.SelectedItem is DraftLoad row)vm.EditBeam(row);}
        private void FitClick(object sender,RoutedEventArgs e)=>Canvas.Fit();
        private void ClearSelectionClick(object sender,RoutedEventArgs e)=>vm.Viewer.ClearSelection();
        private void IsolateClick(object sender,RoutedEventArgs e)=>vm.Viewer.IsolateSelection();
        private void ShowAllClick(object sender,RoutedEventArgs e)=>vm.Viewer.ShowAllObjects();
        private void SelectClick(object sender,RoutedEventArgs e)=>new VisualWorkspaceSelectView(vm.Viewer){Owner=this}.ShowDialog();
        private void TextClick(object sender,RoutedEventArgs e)=>new VisualWorkspaceTextView(vm.Viewer){Owner=this}.ShowDialog();
        private void ColorsClick(object sender,RoutedEventArgs e)=>new VisualWorkspaceObjectColorsView(vm.Viewer){Owner=this}.ShowDialog();
        private void LoadModeChanged(object sender,RoutedEventArgs e){if(Canvas!=null)Canvas.SetBinding(Controls.StructuralCanvas.ReadLoadItemsProperty,new Binding(ShowExisting.IsChecked==true?"ExistingLoads":"PreviewLoads"));}
        private void SectionClick(object sender,RoutedEventArgs e){vm.Viewer.Elevation=false;StopSection();Canvas.PickingPlanePoints=true;Canvas.Cursor=Cursors.Cross;vm.Status="Chọn điểm đầu A và điểm cuối B của mặt cắt.";}
        private void StopSection(){sectionStart=null;Canvas.PickingPlanePoints=false;Canvas.FirstPlanePoint=null;Canvas.CancelInteraction();Canvas.Cursor=Cursors.Arrow;Canvas.InvalidateVisual();}
        private void PlanClick(object sender,RoutedEventArgs e){StopSection();vm.Viewer.Elevation=false;Canvas.Fit();}
        private void Keys(object sender,KeyEventArgs e)
        {
            if(vm.Busy||Keyboard.FocusedElement is TextBoxBase||UserGrid.IsKeyboardFocusWithin)return;
            if(e.Key==Key.Escape){StopSection();vm.Viewer.ClearSelection();e.Handled=true;}
            if(Keyboard.FocusedElement is ComboBoxItem||Keyboard.FocusedElement is ComboBox combo&&combo.IsDropDownOpen)return;
            if(vm.Tab==1&&(e.Key==Key.PageUp||e.Key==Key.PageDown)){vm.Viewer.MovePlanLevel(e.Key==Key.PageUp?1:-1);e.Handled=true;}
            if(Canvas.IsKeyboardFocusWithin&&Keyboard.Modifiers==ModifierKeys.Control){if(e.Key==Key.I)vm.Viewer.IsolateSelection();else if(e.Key==Key.H)vm.Viewer.RemoveSelectedFromWorkspace();else if(e.Key==Key.A)vm.Viewer.ShowAllObjects();else return;e.Handled=true;}
        }
        private void OpenCanvasMenu()
        {
            var menu=new ContextMenu{PlacementTarget=Canvas,Placement=PlacementMode.MousePoint,Background=new SolidColorBrush(Color.FromRgb(15,23,42)),Foreground=Brushes.White,MinWidth=310};
            void Item(string text,string shortcut,Action action){var item=new MenuItem{Header=text,InputGestureText=shortcut,Style=(Style)FindResource("WorkspaceContextItem")};item.Click+=(_,__)=>action();menu.Items.Add(item);}
            Item("◎  Cô lập đối tượng chọn","Ctrl+I",vm.Viewer.IsolateSelection);Item("◌  Ẩn đối tượng chọn","Ctrl+H",vm.Viewer.RemoveSelectedFromWorkspace);Item("◎  Hiện tất cả","Ctrl+A",vm.Viewer.ShowAllObjects);menu.Items.Add(new Separator());Item("T  Cỡ chữ / màu RGB","",()=>TextClick(this,new RoutedEventArgs()));Item("◈  Màu cấu kiện","",()=>ColorsClick(this,new RoutedEventArgs()));menu.IsOpen=true;
        }
        private string Pattern=>vm.Tab==0?vm.UserPattern:vm.BeamPattern;
        private void ImportClick(object sender,RoutedEventArgs e)
        {
            var dialog=new OpenFileDialog{Filter="Excel / CSV|*.xlsx;*.xlsm;*.csv"};if(dialog.ShowDialog(this)==true)Try(()=>{vm.ImportRows(WindWorkbook.Read(dialog.FileName,Pattern));});
        }
        private void ExcelClick(object sender,RoutedEventArgs e)=>Try(()=>vm.ImportRows(WindExcelSelection.Read(Pattern).Rows));
        private void TemplateClick(object sender,RoutedEventArgs e)
        {
            var dialog=new SaveFileDialog{Filter="Excel|*.xlsx",FileName="QKS-Wind-Input.xlsx"};if(dialog.ShowDialog(this)==true)Try(()=>{WindWorkbook.WriteTemplate(dialog.FileName,vm.Viewer.Levels,Pattern);vm.Status="Đã xuất mẫu. Điền riêng sheet của phương thức cần nhập; các dòng chưa điền sẽ bỏ qua.";});
        }
        private void ExportClick(object sender,RoutedEventArgs e)=>Try(()=>{
            if(!CommitGrid()||vm.Snapshot==null)return;
            string F(double value)=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            var byId=vm.Snapshot.Elements.ToDictionary(x=>x.Id);
            var rows=vm.Tab==0?vm.UserRows.Select(r=>r.Input(vm.UserBaseline?.Pattern??vm.UserPattern)).ToArray():vm.BeamRows.Select(l=>new StoryWindInput{Story=byId[l.ElementId].Story,Pattern=l.Pattern,UniqueName=byId[l.ElementId].Name,Direction=l.DirectionName,Q1=F(l.Value1),Q2=F(l.Value2),R1=F(l.Start),R2=F(l.End),Action=l.Action.ToString()}).ToArray();
            var dialog=new SaveFileDialog{Filter="Excel|*.xlsx",FileName=vm.Tab==0?"QKS-WindStories-Prepared.xlsx":"QKS-WindBeams-Prepared.xlsx"};
            if(dialog.ShowDialog(this)==true){WindReportWorkbook.Write(dialog.FileName,vm.Snapshot,rows,vm.Tab==0);vm.Status="Đã xuất bảng soạn Excel, chưa phải xác nhận tải đã gán ETABS.";}
        });
        private void PdfClick(object sender,RoutedEventArgs e)=>Try(()=>{
            GuardSource();vm.Viewer.ReadPattern=vm.BeamPattern;vm.Viewer.ShowFrameLoads=true;vm.Viewer.ShowShellLoads=false;vm.Viewer.ShowJointLoads=false;
            new LoadingCasePlanView(vm.Viewer,sourceConnection!.ModelPath,vm.Viewer.Elevation?(Box2?)null:Canvas.VisibleWorldBounds){Owner=this}.ShowDialog();
        });
        private void SaveClick(object sender,RoutedEventArgs e)=>Try(()=>{
            if(!CommitGrid()||vm.Snapshot==null)return;var dialog=new SaveFileDialog{Filter="QKS Wind|*.qkswind.json",FileName="QKS-Wind.qkswind.json"};if(dialog.ShowDialog(this)!=true)return;
            string json=JsonCodec.Serialize(vm.Project());WriteFile(dialog.FileName,json);savedProject=json;SaveSettings();vm.Status="Đã lưu bộ gió; không ghi ETABS.";
        });
        private void OpenClick(object sender,RoutedEventArgs e)=>Try(()=>{
            if(IsDirty()&&MessageBox.Show(this,"Thay bộ gió đang soạn bằng file đã lưu?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            var dialog=new OpenFileDialog{Filter="QKS Wind|*.qkswind.json"};if(dialog.ShowDialog(this)!=true)return;
            var project=JsonCodec.Deserialize<WindProject>(JsonCodec.Read(dialog.FileName));
            if(project.UserBaseline!=null){GuardSource();var fresh=new EtabsLoadApplyHost(s=>vm.Status=s).ReadUserWind(project.UserBaseline.Pattern);if(!UserWindApply.Same(fresh,project.UserBaseline))throw new InvalidOperationException("User Loads trong ETABS đã đổi so với file. Đọc lại và nhập lại giá trị cần sửa.");}
            vm.Restore(project);savedProject=JsonCodec.Serialize(vm.Project());vm.Status="Đã mở bộ gió. Đối chiếu lại và xem trước trước khi gán.";
        });
        private void ProbeClick(object sender,RoutedEventArgs e)=>Try(()=>{
            GuardSource();var data=WindHostInspection.Probe();var dialog=new SaveFileDialog{Filter="JSON|*.json",FileName="QKS-ETABS-UserWind-Schema.json"};if(dialog.ShowDialog(this)==true){WriteFile(dialog.FileName,JsonCodec.Serialize(data));vm.Status="Đã xuất schema và dữ liệu User Wind từ host; không thay đổi ETABS.";}
        });
        private static void WriteFile(string path,string text)
        {
            string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllText(temp,text,new System.Text.UTF8Encoding(false));if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
        private void SaveSettings(){settings=new WindUiSettings{Tab=vm.Tab,Story=vm.Viewer.Level?.Name??"",BeamPattern=vm.BeamPattern,UserPattern=vm.UserPattern,Direction=vm.Direction,Q1=vm.Q1,Q2=vm.Q2,Grids=vm.Viewer.ShowGrids,Extrude=vm.Viewer.Extrude};WriteFile(SettingsFile,JsonCodec.Serialize(settings));}
        private void LoadSettings()
        {
            if(File.Exists(SettingsFile))settings=JsonCodec.Deserialize<WindUiSettings>(JsonCodec.Read(SettingsFile));
            if(File.Exists(VisualWorkspaceTextView.ProfileFile))vm.Viewer.ApplyTextProfile(JsonCodec.Deserialize<ViewerTextProfile>(JsonCodec.Read(VisualWorkspaceTextView.ProfileFile)));
            if(File.Exists(VisualWorkspaceObjectColorsView.ProfileFile))vm.Viewer.ApplyObjectColors(JsonCodec.Deserialize<ObjectColorProfile>(JsonCodec.Read(VisualWorkspaceObjectColorsView.ProfileFile)));
        }
        private void ApplySettings()
        {
            vm.Tab=settings.Tab==1?1:0;vm.Viewer.Level=vm.Viewer.Levels.FirstOrDefault(l=>l.Name==settings.Story)??vm.Viewer.Level;
            if(vm.Patterns.Contains(settings.BeamPattern))vm.BeamPattern=settings.BeamPattern;if(vm.Patterns.Contains(settings.UserPattern))vm.UserPattern=settings.UserPattern;
            vm.Direction=vm.Directions.Contains(settings.Direction)?settings.Direction:"Global-X";vm.Q1=settings.Q1;vm.Q2=settings.Q2;vm.Viewer.ShowGrids=settings.Grids;vm.Viewer.Extrude=settings.Extrude;
            // Settings are loaded once per model read; refresh non-notifying editor properties.
            DataContext=null;DataContext=vm;
        }
        private void UserGridKeyDown(object sender,KeyEventArgs e)
        {
            if(e.Key!=Key.V||Keyboard.Modifiers!=ModifierKeys.Control)return;
            e.Handled=true;Try(()=>{
                if(!CommitGrid()||!UserGrid.CurrentCell.IsValid)return;
                int row=UserGrid.Items.IndexOf(UserGrid.CurrentItem),col=UserGrid.CurrentColumn.DisplayIndex;
                string[] lines=Clipboard.GetText().Replace("\r","").TrimEnd('\n').Split('\n');
                var targets=UserGrid.Items.OfType<WindUserEditorRow>().ToArray();
                if(row<0||col<2||row+lines.Length>targets.Length)throw new ArgumentException("Dán vào các cột Fx…Y của những khóa đã đọc; không dán đè Story/khóa API.");
                var cells=lines.Select(l=>l.Split('\t')).ToArray();if(cells.Any(c=>col+c.Length>7))throw new ArgumentException("Vùng dán vượt cột Y.");
                foreach(var c in cells)foreach(string value in c)StoryWindPlan.NumberValue(value,"Ô dán");
                string[] properties={"Fx","Fy","Mz","X","Y"};
                for(int r=0;r<cells.Length;r++)for(int c=0;c<cells[r].Length;c++)typeof(WindUserEditorRow).GetProperty(properties[col+c-2])!.SetValue(targets[row+r],cells[r][c]);
            });
        }
        private void CloseClick(object sender,RoutedEventArgs e)=>Close();
    }
}
