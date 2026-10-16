using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExcelDna.Integration;
using Microsoft.Win32;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class GetApiView : Window
    {
        private readonly List<EtabsInstanceInfo> _etabsInstances = new List<EtabsInstanceInfo>();
        private readonly List<AcadDocInfo> _acadDocs = new List<AcadDocInfo>();
        private readonly List<ExcelWorkbookInfo> _excelWorkbooks = new List<ExcelWorkbookInfo>();
        private bool _loading = false;

        public GetApiView()
        {
            InitializeComponent();
            EtabsIconHelper.SetIcon(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _loading = true;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    LoadExcelWorkbooks();
                    LoadEtabsInstances();
                    LoadAutoCadDocuments();
                    ForceSetConnections();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi khởi tạo GetApiView: {ex.Message}");
                }
                finally
                {
                    _loading = false;
                }
            }));
        }

        private void LoadExcelWorkbooks()
        {
            _excelWorkbooks.Clear();
            CB_Excel.Items.Clear();

            try
            {
                dynamic? excelApp = ExcelDnaUtil.Application;
                if (excelApp == null)
                {
                    excelApp = AutoCadHelper.GetActiveCOMObject("Excel.Application");
                }

                if (excelApp != null)
                {
                    dynamic workbooks = excelApp.Workbooks;
                    foreach (dynamic wb in workbooks)
                    {
                        try
                        {
                            string name = wb.Name;
                            string path = wb.FullName;

                            _excelWorkbooks.Add(new ExcelWorkbookInfo
                            {
                                Name = name,
                                FullPath = path,
                                WorkbookObj = wb
                            });
                            CB_Excel.Items.Add(name);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc danh sách Excel workbooks: {ex.Message}");
            }

            if (CB_Excel.Items.Count > 0)
            {
                int selectedIdx = 0;
                string targetName = AppConnectionManager.ActiveExcel?.Name ?? "";

                for (int i = 0; i < _excelWorkbooks.Count; i++)
                {
                    if (!string.IsNullOrEmpty(targetName) && _excelWorkbooks[i].Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = i;
                        break;
                    }
                }
                CB_Excel.SelectedIndex = selectedIdx;
            }
            else
            {
                CB_Excel.Items.Add("Không phát hiện file Excel đang mở");
                CB_Excel.SelectedIndex = 0;
            }
        }

        private void LoadEtabsInstances()
        {
            _etabsInstances.Clear();
            CB_CSI.Items.Clear();

            try
            {
                var instances = EtabsService.GetRunningEtabsInstances();
                foreach (var inst in instances)
                {
                    _etabsInstances.Add(inst);
                    CB_CSI.Items.Add(inst.DisplayName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc instances ETABS: {ex.Message}");
            }

            if (CB_CSI.Items.Count > 0)
            {
                int selectedIdx = 0;
                int targetPid = AppConnectionManager.ActiveEtabs?.ProcessId ?? 0;
                string targetFile = AppConnectionManager.ActiveEtabs?.ModelFileName ?? "";

                for (int i = 0; i < _etabsInstances.Count; i++)
                {
                    if (targetPid > 0 && _etabsInstances[i].ProcessId == targetPid)
                    {
                        selectedIdx = i;
                        break;
                    }
                    if (!string.IsNullOrEmpty(targetFile) && _etabsInstances[i].ModelFileName.Equals(targetFile, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = i;
                        break;
                    }
                }
                CB_CSI.SelectedIndex = selectedIdx;
            }
            else
            {
                CB_CSI.Items.Add("Không phát hiện model CSI ETABS đang mở");
                CB_CSI.SelectedIndex = 0;
            }
        }

        private void LoadAutoCadDocuments()
        {
            _acadDocs.Clear();
            CB_AutoCAD.Items.Clear();

            try
            {
                var docs = AutoCadHelper.GetRunningAutoCadDocuments();
                foreach (var doc in docs)
                {
                    _acadDocs.Add(doc);
                    CB_AutoCAD.Items.Add(doc.FileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc AutoCAD documents: {ex.Message}");
            }

            if (CB_AutoCAD.Items.Count > 0)
            {
                int selectedIdx = 0;
                string targetName = AppConnectionManager.ActiveAutoCad?.FileName ?? "";
                for (int i = 0; i < _acadDocs.Count; i++)
                {
                    if (!string.IsNullOrEmpty(targetName) && _acadDocs[i].FileName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = i;
                        break;
                    }
                }
                CB_AutoCAD.SelectedIndex = selectedIdx;
            }
            else
            {
                CB_AutoCAD.Items.Add("Không phát hiện bản vẽ AutoCAD đang mở");
                CB_AutoCAD.SelectedIndex = 0;
            }
        }

        private void ForceSetConnections()
        {
            EtabsInstanceInfo? selectedEtabs = null;
            if (CB_CSI.SelectedIndex >= 0 && CB_CSI.SelectedIndex < _etabsInstances.Count)
            {
                selectedEtabs = _etabsInstances[CB_CSI.SelectedIndex];
            }

            AcadDocInfo? selectedAcad = null;
            if (CB_AutoCAD.SelectedIndex >= 0 && CB_AutoCAD.SelectedIndex < _acadDocs.Count)
            {
                selectedAcad = _acadDocs[CB_AutoCAD.SelectedIndex];
            }

            ExcelWorkbookInfo? selectedExcel = null;
            if (CB_Excel.SelectedIndex >= 0 && CB_Excel.SelectedIndex < _excelWorkbooks.Count)
            {
                selectedExcel = _excelWorkbooks[CB_Excel.SelectedIndex];
            }

            AppConnectionManager.SetConnections(selectedEtabs, selectedAcad, selectedExcel);
        }

        private void CB_Excel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            ForceSetConnections();
        }

        private void CB_CSI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            ForceSetConnections();
        }

        private void CB_AutoCAD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            ForceSetConnections();
        }

        private void Btn_BrowseExcel_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Bảng tính Excel (*.xls;*.xlsx;*.xlsm)|*.xls;*.xlsx;*.xlsm|All files (*.*)|*.*",
                Title = "Chọn bảng tính Excel"
            };

            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                try
                {
                    dynamic? excelApp = ExcelDnaUtil.Application;
                    if (excelApp != null)
                    {
                        excelApp.Workbooks.Open(filePath);
                        LoadExcelWorkbooks();
                        string fileName = Path.GetFileName(filePath);
                        for (int i = 0; i < _excelWorkbooks.Count; i++)
                        {
                            if (_excelWorkbooks[i].Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                            {
                                CB_Excel.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    ForceSetConnections();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Không thể mở file Excel: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
                }
            }
        }

        private void Btn_BrowseCSI_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Model ETABS (*.edb)|*.edb|All files (*.*)|*.*",
                Title = "Chọn file mô hình ETABS (.edb)"
            };

            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                try
                {
                    lblStatus.Text = "Đang khởi chạy ETABS và nạp mô hình...";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Navy;

                    var info = EtabsService.LaunchAndOpenModel(filePath);
                    if (info != null)
                    {
                        _loading = true;
                        LoadEtabsInstances();
                        _loading = false;

                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        for (int i = 0; i < _etabsInstances.Count; i++)
                        {
                            if (_etabsInstances[i].ModelFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                                _etabsInstances[i].ModelPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                CB_CSI.SelectedIndex = i;
                                lblStatus.Text = "Đã mở mô hình ETABS thành công.";
                                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 65));
                                break;
                            }
                        }
                    }
                    ForceSetConnections();
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Lỗi khi mở ETABS.";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(183, 28, 28));
                    CustomMessageBox.Show($"Lỗi khi mở mô hình ETABS: {ex.Message}", "Lỗi Khởi Động", CustomMessageBox.IconType.ErrorType);
                }
            }
        }

        private void Btn_BrowseAutoCAD_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Bản vẽ AutoCAD (*.dwg;*.dxf)|*.dwg;*.dxf|All files (*.*)|*.*",
                Title = "Chọn file bản vẽ AutoCAD"
            };

            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                try
                {
                    lblStatus.Text = "Đang nạp thông tin bản vẽ AutoCAD...";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 34, 64));

                    dynamic? acadApp = null;
                    try { acadApp = AutoCadHelper.GetActiveAutoCadApp(); }
                    catch
                    {
                        var acadType = Type.GetTypeFromProgID("AutoCAD.Application");
                        if (acadType != null)
                        {
                            acadApp = Activator.CreateInstance(acadType);
                            if (acadApp != null) acadApp.Visible = true;
                        }
                    }

                    if (acadApp != null)
                    {
                        acadApp.Documents.Open(filePath);
                        _loading = true;
                        LoadAutoCadDocuments();
                        _loading = false;

                        string fileName = Path.GetFileName(filePath);
                        for (int i = 0; i < _acadDocs.Count; i++)
                        {
                            if (_acadDocs[i].FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                            {
                                CB_AutoCAD.SelectedIndex = i;
                                break;
                            }
                        }
                        lblStatus.Text = "Đã nạp bản vẽ AutoCAD thành công.";
                        lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 65));
                    }
                    ForceSetConnections();
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Lỗi khi nạp bản vẽ AutoCAD.";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(183, 28, 28));
                    CustomMessageBox.Show($"Lỗi khi mở bản vẽ AutoCAD: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
                }
            }
        }

        private void Btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Text = "Đang quét lại danh sách...";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 34, 64));
            _loading = true;
            try
            {
                LoadExcelWorkbooks();
                LoadEtabsInstances();
                LoadAutoCadDocuments();
                ForceSetConnections();
                lblStatus.Text = "Đã làm mới danh sách tài liệu.";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 65));
            }
            finally
            {
                _loading = false;
            }
        }

        private void Btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            ForceSetConnections();

            string msg = "Đã lưu thiết lập kết nối thành công!\n\n" +
                         $"• ETABS: {AppConnectionManager.GetEtabsDisplayName()}\n" +
                         $"• AutoCAD: {AppConnectionManager.GetAutoCadDisplayName()}\n" +
                         $"• Excel: {AppConnectionManager.GetExcelDisplayName()}";

            CustomMessageBox.Show(msg, "Kết Nối Thành Công", CustomMessageBox.IconType.Success);
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
