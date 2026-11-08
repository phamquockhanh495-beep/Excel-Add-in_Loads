#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services.Modeling;

namespace QksExcelAddIn.Views
{
    /// <summary>Optional full table path. It opens only fields discovered in the current host schema.</summary>
    public sealed class WindUserSetsView : Window
    {
        private readonly ModelSnapshot model;
        private readonly string pattern;
        private readonly EtabsWindTableHost host;
        private readonly ObservableCollection<WindSetRow> rows=new ObservableCollection<WindSetRow>();
        private WindTableSnapshot? baseline;
        private IReadOnlyDictionary<string,string[]> allowed=new Dictionary<string,string[]>();
        private readonly DataGrid grid=new DataGrid{AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,CanUserReorderColumns=false,EnableRowVirtualization=true,SelectionMode=DataGridSelectionMode.Extended};
        private readonly TextBlock status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)};
        private readonly ComboBox tables=new ComboBox{MinWidth=350};
        private readonly TextBox count=new TextBox{Text="",Width=65};
        private readonly TextBlock countLabel=new TextBlock{Text="Number of Load Sets",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)};
        private readonly CheckBox reversible=new CheckBox{Content="Loads are Reversible for Combos",Margin=new Thickness(12,0,0,0)};
        private readonly ComboBox filter=new ComboBox{MinWidth=100,Margin=new Thickness(10,0,0,0)};
        private readonly CheckBox exclusive=new CheckBox{Content="Dùng phiên bảng riêng của QKS: hủy các chỉnh sửa bảng ETABS khác chưa Apply trước khi gán.",Margin=new Thickness(0,8,0,8)};
        private readonly StackPanel settings=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,8,0,8)};
        private readonly DockPanel editor=new DockPanel();
        private readonly Button apply=new Button{Content="Gán bảng ETABS",IsEnabled=false};
        private WindTableChange? reviewed;
        private string reviewSignature="";
        private CancellationTokenSource? work;
        private bool loaded,needsReload;
        public bool MayHaveChanges {get;private set;}
        private readonly Stack<string> undo=new Stack<string>(),redo=new Stack<string>();
        private string lastState="";
        private string savedState="";
        private bool restoring;
        public WindUserSetsView(ModelSnapshot source,string target)
        {
            model=source;pattern=target;host=new EtabsWindTableHost(s=>status.Text=s);
            Title="QKS | USER WIND · STORY / LOAD SETS · "+pattern;Width=1180;Height=790;MinWidth=850;MinHeight=580;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(248,250,252));FontFamily=new FontFamily("Segoe UI");FontSize=12;
            Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/QksExcelAddIn;component/Views/Styles/VisualWorkspaceStyles.xaml")});
            var root=new DockPanel{Margin=new Thickness(12)};Content=root;
            var top=new DockPanel();DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
            var read=new Button{Content="Đọc schema / dữ liệu"};DockPanel.SetDock(read,Dock.Right);top.Children.Add(read);top.Children.Add(tables);read.Click+=async(_,__)=>await Read();
            DockPanel.SetDock(status,Dock.Bottom);root.Children.Add(status);
            var bottom=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
            void Bottom(string name,Action action){var b=new Button{Content=name};b.Click+=(_,__)=>Try(action);bottom.Children.Add(b);}
            Bottom("Xem trước",Review);bottom.Children.Add(apply);apply.Click+=async(_,__)=>await Apply();Bottom("Hủy tác vụ",()=>work?.Cancel());Bottom("Đóng",Close);
            root.Children.Add(editor);editor.IsEnabled=false;
            settings.Children.Add(countLabel);settings.Children.Add(count);settings.Children.Add(reversible);
            settings.Children.Add(new TextBlock{Text="Hiển thị",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(20,0,0,0)});settings.Children.Add(filter);DockPanel.SetDock(settings,Dock.Top);editor.Children.Add(settings);
            var tools=new WrapPanel{Margin=new Thickness(0,0,0,8)};DockPanel.SetDock(tools,Dock.Top);editor.Children.Add(tools);
            void Tool(string text,Action action){var b=new Button{Content=text};b.Click+=(_,__)=>Try(action);tools.Children.Add(b);}
            Tool("Thêm dòng",()=>Add(false));Tool("Nhân bản",()=>Add(true));Tool("Xóa dòng",()=>{Commit();foreach(var row in grid.SelectedItems.Cast<WindSetRow>().ToArray())rows.Remove(row);Track();});
            Tool("Undo",()=>History(undo,redo));Tool("Redo",()=>History(redo,undo));Tool("Lưu bộ bảng…",Save);Tool("Mở bộ bảng…",Open);Tool("Nhập Excel / CSV…",Import);Tool("Xuất Excel…",Export);
            DockPanel.SetDock(exclusive,Dock.Bottom);editor.Children.Add(exclusive);
            grid.ItemsSource=rows;editor.Children.Add(grid);
            var sCol=new DataGridTextColumn{Header="Load Set",Binding=new Binding("LoadSet"){UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged,ValidatesOnExceptions=true},Width=85};grid.Columns.Add(sCol);
            grid.Columns.Add(new DataGridComboBoxColumn{Header="Story",SelectedItemBinding=new Binding("Story"),Width=150});grid.Columns.Add(new DataGridComboBoxColumn{Header="Diaphragm",SelectedItemBinding=new Binding("Diaphragm"),Width=130});
            foreach(var c in new[]{("Fx (kN)","Fx"),("Fy (kN)","Fy"),("Mz (kN·m)","Mz"),("X (m)","X"),("Y (m)","Y")})grid.Columns.Add(new DataGridTextColumn{Header=c.Item1,Binding=new Binding(c.Item2){UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged},Width=new DataGridLength(1,DataGridLengthUnitType.Star),MinWidth=85});
            grid.BeginningEdit+=(_,__)=>Invalidate();grid.CellEditEnding+=(_,__)=>Dispatcher.BeginInvoke(new Action(Track));
            count.TextChanged+=(_,__)=>{Invalidate();if(loaded&&!restoring)Dispatcher.BeginInvoke(new Action(Track));};reversible.Checked+=(_,__)=>{Invalidate();if(loaded&&!restoring)Track();};reversible.Unchecked+=(_,__)=>{Invalidate();if(loaded&&!restoring)Track();};
            exclusive.Checked+=(_,__)=>Invalidate();exclusive.Unchecked+=(_,__)=>Invalidate();
            tables.SelectionChanged+=(_,__)=>Invalidate();
            filter.SelectionChanged+=(_,__)=>{if(filter.SelectedItem!=null)CollectionViewSource.GetDefaultView(rows).Filter=o=>filter.SelectedIndex==0||o is WindSetRow r&&r.LoadSet.ToString(CultureInfo.InvariantCulture)==filter.SelectedItem.ToString();};
            Closing+=(_,e)=>{if(work!=null){e.Cancel=true;status.Text="Hủy hoặc chờ tác vụ kết thúc trước khi đóng.";return;}if(loaded&&!MayHaveChanges&&State()!=savedState&&MessageBox.Show(this,"Đóng bảng mà không lưu các chỉnh sửa?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)e.Cancel=true;};
            Loaded+=async(_,__)=>{try{host.ValidatePattern(pattern);tables.ItemsSource=host.Candidates();if(tables.Items.Count==1){tables.SelectedIndex=0;await Read();}else status.Text=tables.Items.Count==0?"Host không công bố bảng User Wind. Dùng chế độ khóa hiện hữu hoặc xuất Thông tin API để xác minh.":"Chọn bảng User Wind của host rồi bấm Đọc. Không tự chọn giữa nhiều schema.";}catch(Exception ex){status.Text=ex.Message;}};
            WorkspacePopupChrome.Attach(this);
        }
        private void Try(Action action){try{action();}catch(Exception ex){status.Text=ex.GetBaseException().Message;}}
        private void Invalidate(){reviewed=null;reviewSignature="";apply.IsEnabled=false;}
        private void Commit(){if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true))throw new ArgumentException("Sửa ô lỗi trước khi tiếp tục.");}
        private string State()=>JsonCodec.Serialize(new WindSetEditFile{Pattern=pattern,Rows=rows.Select(r=>r.Copy()).ToArray(),Count=count.Text,Reversible=reversible.IsChecked==true});
        private void Track(){if(!loaded||restoring)return;string state=State();if(lastState!=state){undo.Push(lastState);redo.Clear();if(undo.Count>100){var keep=undo.Take(100).Reverse().ToArray();undo.Clear();foreach(var value in keep)undo.Push(value);}lastState=state;Invalidate();}}
        private void Restore(WindSetEditFile file){restoring=true;try{rows.Clear();foreach(var row in file.Rows)rows.Add(row.Copy());count.Text=file.Count;reversible.IsChecked=file.Reversible;filter.ItemsSource=new[]{"Tất cả"}.Concat(rows.Select(r=>r.LoadSet).Distinct().OrderBy(x=>x).Select(x=>x.ToString())).ToArray();filter.SelectedIndex=0;lastState=State();}finally{restoring=false;Invalidate();}}
        private void History(Stack<string> from,Stack<string> to){Commit();Track();if(from.Count==0)return;to.Push(State());Restore(JsonCodec.Deserialize<WindSetEditFile>(from.Pop()));}
        private async Task Read()
        {
            if(work!=null||tables.SelectedItem==null)return;
            if(needsReload){status.Text="Đã có thao tác ghi. Đóng bảng và đọc lại toàn model trước.";return;}
            if(loaded&&MessageBox.Show(this,"Đọc lại sẽ thay dữ liệu bảng đang soạn. Tiếp tục?","QKS Wind",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            work=new CancellationTokenSource();editor.IsEnabled=false;tables.IsEnabled=false;Invalidate();loaded=false;baseline=null;
            try{
                host.ValidatePattern(pattern);var next=host.Read((string)tables.SelectedItem);var data=next.PatternRows(pattern);if(data.Length==0)throw new ArgumentException("Pattern chưa có hàng; khởi tạo User Loads trong ETABS trước.");
                allowed=await host.ReadAssignments(model.Levels.Select(l=>l.Name),s=>status.Text=s,work.Token);baseline=next;needsReload=false;loaded=false;
                Restore(new WindSetEditFile{Pattern=pattern,Rows=data,Count=next.SetCount(pattern).ToString(),Reversible=next.Schema.Reversible>=0&&next.ReversibleValue(pattern)});undo.Clear();redo.Clear();loaded=true;savedState=State();
                count.IsEnabled=next.Schema.HasSettings;reversible.IsEnabled=next.Schema.HasSettings;countLabel.Text=next.Schema.Count>=0?"Number of Load Sets":"Load Set lớn nhất đã đọc";
                reversible.IsThreeState=next.Schema.Reversible<0;if(next.Schema.Reversible<0)reversible.IsChecked=null;
                ((DataGridComboBoxColumn)grid.Columns[1]).ItemsSource=model.Levels.OrderByDescending(l=>l.Elevation).Select(l=>l.Name).ToArray();((DataGridComboBoxColumn)grid.Columns[2]).ItemsSource=allowed.Values.SelectMany(v=>v).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                status.Text="Đã đọc "+data.Length+" dòng · "+next.Key+" · schema "+next.Version+(next.Schema.HasSettings?" · Có Number of Load Sets / Reversible.":" · Schema thiếu Number of Load Sets/Reversible: giữ nguyên thiết lập này.")+(next.CanAdd?"":" · Có field bổ sung: chưa mở tạo identity mới.");
            }catch(Exception ex){status.Text="Chưa mở chế độ bảng: "+ex.GetBaseException().Message;}
            finally{work.Dispose();work=null;tables.IsEnabled=true;editor.IsEnabled=loaded&&!needsReload;}
        }
        private void Add(bool copy)
        {
            Commit();if(baseline?.CanAdd!=true)throw new InvalidOperationException("Schema có field chưa xác minh hoặc chưa đọc; không tự tạo hàng.");
            var selected=grid.SelectedItem as WindSetRow;var row=copy&&selected!=null?selected.Copy():new WindSetRow{Story=model.Levels.OrderByDescending(l=>l.Elevation).First().Name};
            if(copy)row.LoadSet=Math.Min(1000,row.LoadSet+1);else row.Diaphragm=allowed.TryGetValue(row.Story,out var names)?names.FirstOrDefault()??"":"";
            rows.Add(row);filter.SelectedIndex=0;grid.SelectedItem=row;grid.ScrollIntoView(row);Track();
        }
        private WindTableChange Change()
        {
            Commit();if(baseline==null||needsReload||tables.SelectedItem?.ToString()!=baseline.Key)throw new InvalidOperationException("Đọc lại đúng bảng đang chọn trước.");
            int sets=WindTableSnapshot.Positive(count.Text,"Number of Load Sets");
            var after=baseline.ReplacePattern(pattern,rows,sets,reversible.IsChecked==true,allowed);
            var change=new WindTableChange(model.Identity,SnapshotFingerprint.Of(model),pattern,baseline,after,exclusive.IsChecked==true);change.Validate();return change;
        }
        private void Review()
        {
            var change=Change();if(WindTableSnapshot.Equivalent(change.Before,change.After))throw new ArgumentException("Không có thay đổi.");
            var before=baseline!.PatternRows(pattern);var after=rows.ToArray();
            var view=new Window{Title="QKS · USER WIND · TRƯỚC / SAU",Owner=this,Width=1100,Height=620,WindowStartupLocation=WindowStartupLocation.CenterOwner};view.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/QksExcelAddIn;component/Views/Styles/VisualWorkspaceStyles.xaml")});
            var panel=new DockPanel{Margin=new Thickness(12)};var note=new TextBlock{Text="Pattern: "+pattern+" · "+before.Length+" → "+after.Length+" hàng · "+countLabel.Text+": "+baseline.SetCount(pattern)+" → "+count.Text+" · Reversible: "+(baseline.Schema.Reversible>=0?baseline.ReversibleValue(pattern).ToString():"không có field")+" → "+(baseline.Schema.Reversible>=0?(reversible.IsChecked==true).ToString():"không có field")+"\nCác pattern khác và field bổ sung được giữ nguyên. Đơn vị kN, m.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};DockPanel.SetDock(note,Dock.Top);panel.Children.Add(note);
            var ok=new Button{Content="Đã xem bảng trước/sau",HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(ok,Dock.Bottom);panel.Children.Add(ok);
            panel.Children.Add(new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,CanUserAddRows=false,ItemsSource=before.Select(r=>new{Phase="Trước",r.LoadSet,r.Story,r.Diaphragm,r.Fx,r.Fy,r.Mz,r.X,r.Y}).Concat(after.Select(r=>new{Phase="Sau",r.LoadSet,r.Story,r.Diaphragm,r.Fx,r.Fy,r.Mz,r.X,r.Y})).ToArray()});view.Content=panel;WorkspacePopupChrome.Attach(view);
            ok.Click+=(_,__)=>{reviewed=change;reviewSignature=State();apply.IsEnabled=exclusive.IsChecked==true;view.DialogResult=true;};view.ShowDialog();status.Text=exclusive.IsChecked==true?"Đã xem trước; Gán bảng ETABS sẽ ghi đúng phạm vi này.":"Cần xác nhận phiên bảng riêng trước khi xem trước để mở Gán.";
        }
        private async Task Apply()
        {
            if(work!=null)return;try{Commit();if(reviewed==null||reviewSignature!=State()||exclusive.IsChecked!=true)throw new InvalidOperationException("Dữ liệu hoặc phiên bảng thay đổi. Xem trước lại.");}catch(Exception ex){status.Text=ex.Message;return;}
            var change=reviewed;work=new CancellationTokenSource();editor.IsEnabled=false;Invalidate();
            try{var report=await WindTableApply.Execute(change,host,work.Token);MayHaveChanges|=report.MayHaveChanges;needsReload=report.MayHaveChanges;status.Text=report.Outcome+" · "+report.Error+" · Backup: "+report.BackupPath+(report.Import?.Warnings>0?" · Có cảnh báo import, xem journal.":"");if(needsReload)status.Text+=" · Đóng bảng và đọc lại toàn model trước lần ghi tiếp.";}
            catch(Exception ex){needsReload=true;MayHaveChanges=true;status.Text="Cần kiểm tra ETABS: "+ex.GetBaseException().Message;}
            finally{work.Dispose();work=null;editor.IsEnabled=!needsReload;}
        }
        private void Save()
        {
            Commit();if(baseline==null)return;var file=JsonCodec.Deserialize<WindSetEditFile>(State());file.ModelIdentity=model.Identity;file.Table=baseline.Key;file.SchemaVersion=baseline.Version;file.BaselineFingerprint=baseline.Fingerprint;
            var dialog=new SaveFileDialog{Filter="QKS User Wind Sets|*.qkswindsets.json",FileName="QKS-UserWindSets.qkswindsets.json"};if(dialog.ShowDialog(this)==true){SaveText(dialog.FileName,JsonCodec.Serialize(file));savedState=State();status.Text="Đã lưu bộ soạn bảng. Chưa ghi ETABS.";}
        }
        private static void SaveText(string path,string text){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,text,new System.Text.UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
        private void Open()
        {
            if(baseline==null)return;var dialog=new OpenFileDialog{Filter="QKS User Wind Sets|*.qkswindsets.json"};if(dialog.ShowDialog(this)!=true)return;var file=JsonCodec.Deserialize<WindSetEditFile>(JsonCodec.Read(dialog.FileName));
            if(file.Version!=1||file.ModelIdentity!=model.Identity||file.Pattern!=pattern||file.Table!=baseline.Key||file.SchemaVersion!=baseline.Version||file.BaselineFingerprint!=baseline.Fingerprint)throw new ArgumentException("Bộ bảng khác model/pattern/schema hoặc baseline đã thay đổi.");
            if(file.Rows==null||file.Rows.Length>10000||file.Rows.Any(r=>r==null))throw new ArgumentException("Dữ liệu file không hợp lệ.");undo.Push(State());redo.Clear();Restore(file);savedState=State();
        }
        private void Import()
        {
            var dialog=new OpenFileDialog{Filter="Excel / CSV|*.xlsx;*.xlsm;*.csv"};if(dialog.ShowDialog(this)!=true)return;
            var input=WindWorkbook.Read(dialog.FileName,pattern,true).Where(r=>r.HasInput).ToArray();
            if(input.Length==0||input.Any(r=>r.Target!="UserLoads"||r.Pattern!=pattern))throw new ArgumentException("Chỉ nhập UserLoads của pattern đang mở.");
            var counts=input.Select(r=>r.UserSetCount).Distinct().ToArray();var reverses=input.Select(r=>WindTableSnapshot.Bool(r.UserReversible)).Distinct().ToArray();
            if(counts.Length!=1||reverses.Length!=1)throw new ArgumentException("Number of Load Sets / Reversible phải thống nhất trong cùng pattern.");
            var values=input.Select(r=>new WindSetRow{Story=r.Story,Diaphragm=r.DiaphragmKey,LoadSet=WindTableSnapshot.Positive(r.UserLoadSet,"LoadSet"),Fx=r.Fx,Fy=r.Fy,Mz=r.Mz,X=r.X,Y=r.Y}).ToArray();
            if(baseline==null)throw new ArgumentException("Đọc bảng trước.");baseline.ReplacePattern(pattern,values,WindTableSnapshot.Positive(counts[0],"Number of Load Sets"),reverses[0],allowed);
            undo.Push(State());redo.Clear();Restore(new WindSetEditFile{Pattern=pattern,Rows=values,Count=counts[0],Reversible=reverses[0]});status.Text="Đã nhập bảng theo Load Sets; kiểm tra trước/sau trước khi gán.";
        }
        private void Export()
        {
            Commit();var change=Change();var dialog=new SaveFileDialog{Filter="Excel|*.xlsx",FileName="QKS-UserWindSets.xlsx"};if(dialog.ShowDialog(this)!=true)return;
            WindReportWorkbook.Write(dialog.FileName,model,rows.Select(r=>new StoryWindInput{Target="UserLoads",Story=r.Story,Pattern=pattern,DiaphragmKey=r.Diaphragm,Fx=r.Fx,Fy=r.Fy,Mz=r.Mz,X=r.X,Y=r.Y,UserLoadSet=r.LoadSet.ToString(),UserSetCount=baseline!.Schema.Count>=0?count.Text:"",UserReversible=baseline.Schema.Reversible>=0?(reversible.IsChecked==true?"Yes":"No"):""}),true);status.Text="Đã xuất Excel; trường cấu hình host không cung cấp được để trống, không suy đoán.";
        }
    }
    public sealed class WindSetEditFile
    {
        public int Version {get;set;}=1;public string ModelIdentity {get;set;}="";public string Pattern {get;set;}="";public string Table {get;set;}="";public int SchemaVersion {get;set;}public string BaselineFingerprint {get;set;}="";
        public WindSetRow[] Rows {get;set;}=Array.Empty<WindSetRow>();public string Count {get;set;}="1";public bool Reversible {get;set;}
    }
}
