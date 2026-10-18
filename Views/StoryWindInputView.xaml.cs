using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class StoryWindInputView : Window
    {
        private readonly ObservableCollection<StoryWindInput> beams=new ObservableCollection<StoryWindInput>(),users=new ObservableCollection<StoryWindInput>();
        private readonly IReadOnlyList<ModelLevel> levels;
        private readonly IReadOnlyList<string> patterns;
        private readonly string defaultPattern;
        private readonly Action<IEnumerable<StoryWindInput>,string> accept;
        public string Basis { get; private set; }="Popup nhập trực tiếp theo Story";
        public StoryWindInputView(IReadOnlyList<ModelLevel> levels,IReadOnlyList<string> patterns,IEnumerable<string> diaphragmKeys,string defaultPattern,Action<IEnumerable<StoryWindInput>,string> accept)
        {
            InitializeComponent();WorkspaceWindowPlacement.Attach(this);this.levels=levels;this.patterns=patterns;this.defaultPattern=defaultPattern;this.accept=accept;
            Combo(BeamGrid,"Story","Story",levels.Select(l=>l.Name));Combo(BeamGrid,"Pattern","Pattern",patterns);TextColumn(BeamGrid,"UniqueName","UniqueName");Combo(BeamGrid,"Hướng","Direction",new[]{"Global-X","Global-Y"});TextColumn(BeamGrid,"q1 (kN/m)","Q1");TextColumn(BeamGrid,"q2 (trống = q1)","Q2");Combo(BeamGrid,"Action","Action",new[]{"ReplaceMatching","Add"});
            Combo(UserGrid,"Story (đối chiếu)","Story",levels.Select(l=>l.Name));Combo(UserGrid,"Pattern","Pattern",patterns);Combo(UserGrid,"Khóa diaphragm API","DiaphragmKey",diaphragmKeys);
            foreach(var column in new[]{("Fx (kN)","Fx"),("Fy (kN)","Fy"),("Mz (kN·m)","Mz"),("X (m)","X"),("Y (m)","Y")})TextColumn(UserGrid,column.Item1,column.Item2);
            foreach(var level in levels) { beams.Add(new StoryWindInput{Story=level.Name,Pattern=defaultPattern});users.Add(new StoryWindInput{Target="UserLoads",Story=level.Name,Pattern=defaultPattern}); }
            BeamGrid.ItemsSource=beams;UserGrid.ItemsSource=users;
        }
        private void TextColumn(DataGrid grid,string header,string path)=>grid.Columns.Add(new DataGridTextColumn{Header=header,Binding=new Binding(path){UpdateSourceTrigger=UpdateSourceTrigger.LostFocus},ElementStyle=(Style)FindResource("WorkspaceCellCenter"),MinWidth=90,Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
        private void Combo(DataGrid grid,string header,string path,IEnumerable<string> values)=>grid.Columns.Add(new DataGridComboBoxColumn{Header=header,ItemsSource=values.ToArray(),SelectedItemBinding=new Binding(path){UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged},MinWidth=110,Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
        public void LoadFile(string path)
        {
            try { LoadRows(WindWorkbook.Read(path,defaultPattern));Basis="File: "+path;Help.Text="Đã đọc file. Kiểm tra Story, pattern, đơn vị và khóa API trước khi đưa vào nháp. Formula dùng giá trị cache đã lưu trong workbook."; }
            catch(Exception ex) { Help.Text="Không nhập file: "+ex.GetBaseException().Message; }
        }
        private void LoadRows(IEnumerable<StoryWindInput> source)
        {
            var values=source.ToArray();if(values.Any(r=>!string.Equals(r.Target,"UserLoads",StringComparison.OrdinalIgnoreCase)&&!string.Equals(r.Target,"Beam",StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Target cần Beam hoặc UserLoads.");beams.Clear();users.Clear();
            foreach(var row in values) { if(string.Equals(row.Target,"UserLoads",StringComparison.OrdinalIgnoreCase))users.Add(row);else if(string.Equals(row.Target,"Beam",StringComparison.OrdinalIgnoreCase))beams.Add(row);else throw new ArgumentException("Target cần Beam hoặc UserLoads."); }
        }
        private void ImportFile_Click(object sender,RoutedEventArgs e)
        {
            var dialog=new OpenFileDialog{Filter="Excel / CSV (*.xlsx;*.xlsm;*.csv)|*.xlsx;*.xlsm;*.csv"};if(dialog.ShowDialog(this)==true)LoadFile(dialog.FileName);
        }
        private void Selection_Click(object sender,RoutedEventArgs e)
        {
            try { var result=WindExcelSelection.Read(defaultPattern);LoadRows(result.Rows);Basis=result.Basis;Help.Text="Đã đọc vùng Excel. Kiểm tra dữ liệu trước khi đưa vào nháp."; }
            catch(Exception ex) { Help.Text="Không đọc vùng chọn: "+ex.GetBaseException().Message; }
        }
        private void Template_Click(object sender,RoutedEventArgs e)
        {
            var dialog=new SaveFileDialog{Filter="Excel workbook (*.xlsx)|*.xlsx",FileName="QKS-Wind-Story-Input.xlsx"};
            if(dialog.ShowDialog(this)!=true)return;
            try { WindWorkbook.WriteTemplate(dialog.FileName,levels,defaultPattern);Help.Text="Đã xuất mẫu với Story hiện tại. Điền giá trị và lưu file trước khi nhập lại."; }catch(Exception ex){Help.Text=ex.Message;}
        }
        private void Accept_Click(object sender,RoutedEventArgs e)
        {
            if(!BeamGrid.CommitEdit(DataGridEditingUnit.Cell,true)||!BeamGrid.CommitEdit(DataGridEditingUnit.Row,true)||!UserGrid.CommitEdit(DataGridEditingUnit.Cell,true)||!UserGrid.CommitEdit(DataGridEditingUnit.Row,true))return;
            try { foreach(var row in beams)row.Target="Beam";foreach(var row in users)row.Target="UserLoads";accept(beams.Concat(users),Basis);DialogResult=true; }
            catch(Exception ex) { Help.Text="Chưa đưa vào nháp: "+ex.GetBaseException().Message; }
        }
        private void Cancel_Click(object sender,RoutedEventArgs e)=>DialogResult=false;
    }
}
