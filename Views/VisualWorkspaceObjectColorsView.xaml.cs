using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
namespace QksExcelAddIn.Views
{
    public partial class VisualWorkspaceObjectColorsView : Window
    {
        private readonly VisualWorkspaceViewModel vm;
        private ObjectColorRow[] rows=Array.Empty<ObjectColorRow>();
        internal static string ProfileFile=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Viewer","object-colors.json");
        public VisualWorkspaceObjectColorsView(VisualWorkspaceViewModel model)
        {
            vm=model;InitializeComponent();WorkspacePopupChrome.Attach(this);
            MaxHeight=SystemParameters.WorkArea.Height*.9;MaxWidth=SystemParameters.WorkArea.Width*.9;
            ModeBox.ItemsSource=new[]{"Theo loại cấu kiện · Color Objects","Theo Property"};
            LoadFields(new ObjectColorProfile(1,vm.ColorByProperty,vm.ObjectColors.Colors));
        }
        private void LoadFields(ObjectColorProfile profile)
        {
            rows=vm.LabelSettings.Select(setting=>new ObjectColorRow(profile.Colors.FirstOrDefault(c=>c.Kind==setting.Kind)??ObjectColor.Default(setting.Kind),setting.Caption)).ToArray();
            Rows.ItemsSource=rows;ModeBox.SelectedIndex=profile.ColorByProperty?1:0;
        }
        private void Mode_Changed(object sender,SelectionChangedEventArgs e)
        {
            if(ModeNote==null)return;
            ModeNote.Text=ModeBox.SelectedIndex==1?"Theo Property đang ưu tiên màu tiết diện. Màu Object vẫn được lưu để dùng khi chuyển lại chế độ theo loại.":"Chỉnh RGB từng loại bên dưới. Bấm Áp dụng để cập nhật hình và lưu trên máy này.";
        }
        private void Apply_Click(object sender,RoutedEventArgs e)
        {
            try {
                var profile=new ObjectColorProfile(1,ModeBox.SelectedIndex==1,rows.Select(r=>r.ToColor()).ToArray());
                vm.ApplyObjectColors(profile);
                string temp=ProfileFile+"."+Guid.NewGuid().ToString("N")+".tmp";
                try {JsonCodec.WriteAtomic(temp,JsonCodec.Serialize(profile));if(File.Exists(ProfileFile))File.Replace(temp,ProfileFile,null);else File.Move(temp,ProfileFile);}
                catch(Exception ex){ResultText.Text="Đã áp dụng trong phiên này; chưa lưu được: "+ex.Message;return;}
                finally{if(File.Exists(temp))File.Delete(temp);}
                ResultText.Text="Đã áp dụng và lưu Color Objects.";
            } catch(Exception ex){ResultText.Text="Chưa áp dụng: "+ex.Message;}
        }
        private void Reset_Click(object sender,RoutedEventArgs e){LoadFields(ObjectColorProfile.Default);ResultText.Text="Bấm Áp dụng để khôi phục màu mặc định.";}
        private void Close_Click(object sender,RoutedEventArgs e)=>Close();
    }
}
