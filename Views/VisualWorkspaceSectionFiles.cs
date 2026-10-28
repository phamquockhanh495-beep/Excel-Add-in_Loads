using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private string sectionModelPath="";
        private bool sectionFileWritable;
        private string SectionFile {
            get {
                using(var sha=SHA256.Create()) {
                    string key=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sectionModelPath.Trim().ToUpperInvariant()))).Replace("-","");
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Viewer","sections",key+".json");
                }
            }
        }
        private void LoadSectionFile()
        {
            sectionModelPath=AppConnectionManager.ActiveEtabs?.ModelPath??"";sectionFileWritable=false;vm.LoadSections(Array.Empty<SavedSection>());
            if(string.IsNullOrWhiteSpace(sectionModelPath))return;
            try {
                if(File.Exists(SectionFile)) {
                    var catalog=JsonCodec.Deserialize<SectionCatalog>(JsonCodec.Read(SectionFile));
                    if(!Protocol.SamePath(catalog.ModelPath,sectionModelPath))throw new InvalidDataException("Danh mục thuộc file mô hình khác.");
                    vm.LoadSections(catalog.Sections);
                }
                sectionFileWritable=true;
            } catch(Exception ex) { vm.Status="Không đọc được danh mục mặt cắt; giữ file cũ: "+ex.Message; }
        }
        private void PersistSections()
        {
            if(!sectionFileWritable||vm.Snapshot==null||!Protocol.SamePath(sectionModelPath,AppConnectionManager.ActiveEtabs?.ModelPath??""))throw new InvalidOperationException("Chưa có danh mục mặt cắt hợp lệ cho mô hình hiện tại. Hãy Đọc lại.");
            string file=SectionFile,temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                JsonCodec.WriteAtomic(temp,JsonCodec.Serialize(new SectionCatalog(1,sectionModelPath,vm.SavedSections)));
                if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);
            } finally { if(File.Exists(temp))File.Delete(temp); }
        }
        private void SaveSection_Click(object sender,RoutedEventArgs e)
        {
            var previous=vm.SavedSections.ToArray();
            try { vm.SaveCurrentSection();PersistSections();vm.Status="Đã lưu mặt cắt "+vm.SectionName+" cho file mô hình này."; }
            catch(Exception ex) { vm.LoadSections(previous);vm.Status="Chưa lưu mặt cắt: "+ex.Message; }
        }
        private void OpenSection_Click(object sender,RoutedEventArgs e) { StopPlanePicking();vm.OpenSelectedSection();Canvas.Fit(); }
        private void DeleteSection_Click(object sender,RoutedEventArgs e)
        {
            var previous=vm.SavedSections.ToArray();
            try { vm.RemoveSelectedSection();PersistSections();vm.Status="Đã cập nhật danh mục mặt cắt."; }
            catch(Exception ex) { vm.LoadSections(previous);vm.Status="Chưa xóa mặt cắt: "+ex.Message; }
        }
    }
}
