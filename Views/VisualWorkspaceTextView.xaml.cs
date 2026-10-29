using System;
using System.Globalization;
using System.IO;
using System.Windows;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
namespace QksExcelAddIn.Views
{
    public partial class VisualWorkspaceTextView : Window
    {
        private readonly VisualWorkspaceViewModel vm;
        internal static string ProfileFile=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Viewer","text-display.json");
        public VisualWorkspaceTextView(VisualWorkspaceViewModel model)
        {
            vm=model;InitializeComponent();WorkspacePopupChrome.Attach(this);MaxHeight=SystemParameters.WorkArea.Height*.9;LoadFields(new ViewerTextProfile(vm.StructureText,vm.LoadText));
        }
        private void LoadFields(ViewerTextProfile p)
        {
            StructureSize.Text=p.Structure.Size.ToString(CultureInfo.InvariantCulture);StructureRed.Text=p.Structure.Red.ToString();StructureGreen.Text=p.Structure.Green.ToString();StructureBlue.Text=p.Structure.Blue.ToString();
            LoadSize.Text=p.Loads.Size.ToString(CultureInfo.InvariantCulture);LoadRed.Text=p.Loads.Red.ToString();LoadGreen.Text=p.Loads.Green.ToString();LoadBlue.Text=p.Loads.Blue.ToString();LoadCustomColor.IsChecked=p.Loads.UseColor;
        }
        private void PickColor_Click(object sender,RoutedEventArgs e)
        {
            bool structure=(string)((System.Windows.Controls.Button)sender).Tag=="Structure";
            var r=structure?StructureRed:LoadRed;var g=structure?StructureGreen:LoadGreen;var b=structure?StructureBlue:LoadBlue;
            int Channel(string text)=>int.TryParse(text,out int value)&&value>=0&&value<=255?value:0;
            using(var picker=new System.Windows.Forms.ColorDialog{FullOpen=true,Color=System.Drawing.Color.FromArgb(Channel(r.Text),Channel(g.Text),Channel(b.Text))})
                if(picker.ShowDialog()==System.Windows.Forms.DialogResult.OK){r.Text=picker.Color.R.ToString();g.Text=picker.Color.G.ToString();b.Text=picker.Color.B.ToString();if(!structure)LoadCustomColor.IsChecked=true;}
        }
        private void Apply_Click(object sender,RoutedEventArgs e)
        {
            try {
                double Size(string s)=>double.Parse(s.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture);
                int Channel(string s)=>int.Parse(s,NumberStyles.Integer,CultureInfo.InvariantCulture);
                var profile=new ViewerTextProfile(new TextAppearance(Size(StructureSize.Text),Channel(StructureRed.Text),Channel(StructureGreen.Text),Channel(StructureBlue.Text)),new TextAppearance(Size(LoadSize.Text),Channel(LoadRed.Text),Channel(LoadGreen.Text),Channel(LoadBlue.Text),LoadCustomColor.IsChecked==true));
                vm.ApplyTextProfile(profile);
                string temp=ProfileFile+"."+Guid.NewGuid().ToString("N")+".tmp";
                try {JsonCodec.WriteAtomic(temp,JsonCodec.Serialize(profile));if(File.Exists(ProfileFile))File.Replace(temp,ProfileFile,null);else File.Move(temp,ProfileFile);}
                catch(Exception ex){ResultText.Text="Đã áp dụng trong phiên này; chưa lưu được: "+ex.Message;return;}
                finally{if(File.Exists(temp))File.Delete(temp);}
                ResultText.Text="Đã áp dụng và lưu cấu hình chữ trên máy này.";
            } catch(Exception ex){ResultText.Text="Chưa áp dụng: "+ex.Message;}
        }
        private void Reset_Click(object sender,RoutedEventArgs e){LoadFields(new ViewerTextProfile(TextAppearance.StructureDefault,TextAppearance.LoadsDefault));ResultText.Text="Bấm Áp dụng để dùng giá trị mặc định.";}
        private void Close_Click(object sender,RoutedEventArgs e)=>Close();
    }
}
