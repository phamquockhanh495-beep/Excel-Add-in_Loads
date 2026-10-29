using System;
using System.IO;
using System.Windows;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Views
{
    public partial class VisualLoadWorkspaceView
    {
        private ViewerPreferences? loadedPreferences;
        private bool preferencesWritable=true, modelPreferencesRestored, panelsCustomized;
        private static string PreferencesFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Viewer","viewer-settings.json");
        private void LoadViewerPreferences()
        {
            if(!File.Exists(PreferencesFile)) return;
            try {
                loadedPreferences=ViewerPreferencesFile.Read(PreferencesFile);vm.ApplyPreferences(loadedPreferences);
                if(loadedPreferences.LeftPanel.HasValue&&loadedPreferences.RightPanel.HasValue) {
                    panelsCustomized=true;displayVisible=loadedPreferences.LeftPanel.Value;editorVisible=loadedPreferences.RightPanel.Value;
                }
            } catch(Exception ex) {
                preferencesWritable=false;
                MessageBox.Show(this,"Không đọc được cấu hình 2D. Giữ nguyên file để kiểm tra, phiên này dùng cấu hình mặc định.\n"+PreferencesFile+"\n"+ex.Message,"Cấu hình 2D",MessageBoxButton.OK,MessageBoxImage.Warning);
            }
        }
        private void RestoreViewerModelPreferences()
        {
            if(modelPreferencesRestored)return;
            modelPreferencesRestored=true;
            if(loadedPreferences!=null)vm.RestoreModelPreferences(loadedPreferences,sectionModelPath);
        }
        private void SaveViewerPreferences()
        {
            if(!preferencesWritable)return;
            try {
                var profile=vm.CapturePreferences(vm.Snapshot!=null?sectionModelPath:"");
                profile.LeftPanel=displayVisible;profile.RightPanel=editorVisible;
                if(vm.Snapshot==null&&loadedPreferences!=null) {
                    profile.LastModelPath=loadedPreferences.LastModelPath;profile.LastStory=loadedPreferences.LastStory;
                    profile.LastPattern=loadedPreferences.LastPattern;profile.LastSet=loadedPreferences.LastSet;
                }
                ViewerPreferencesFile.Write(PreferencesFile,profile);
            } catch(Exception ex) {
                MessageBox.Show(this,"Chưa lưu được cấu hình 2D lần này; cấu hình đã lưu trước đó được giữ nguyên.\n"+ex.Message,"Cấu hình 2D",MessageBoxButton.OK,MessageBoxImage.Warning);
            }
        }
    }
}
