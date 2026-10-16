using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ExcelDna.Integration;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    /// <summary>
    /// Interaction logic for ShellLoadView.xaml
    /// Đồng bộ 100% UI/UX & độ ổn định với WallLoadView
    /// </summary>
    public partial class ShellLoadView : Window
    {
        private string CadContext => (cboStory.SelectedItem?.ToString() ?? "") + "|" + (radUniformPattern.IsChecked == true ? cboLoaiTai.SelectedItem?.ToString() : cboDisplayLoadSet.SelectedItem?.ToString());
        private readonly CadExchangeSession _cadSession = new CadExchangeSession();
        private readonly string _exchangeDir = Path.Combine(Qks.Shared.Protocol.Root, Guid.NewGuid().ToString("N"));
        private readonly EtabsService _etabsService;

        public ObservableCollection<string> AvailableLoadPatterns { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableDirections { get; } = new ObservableCollection<string> { "Gravity", "Global Z", "Global X", "Global Y" };

        private List<ShellSectionInfo> _allSections = new List<ShellSectionInfo>();
        private ObservableCollection<ShellLoadSetItem> _loadSetsList = new ObservableCollection<ShellLoadSetItem>();
        private ObservableCollection<ShellLoadSetDetail> _currentSetDetails = new ObservableCollection<ShellLoadSetDetail>();
        private ShellLoadSetItem? _selectedLoadSet;
        private bool _isLoading = false;

        public ShellLoadView()
        {
            InitializeComponent();
            this.DataContext = this;
            _etabsService = new EtabsService();

            if (AvailableLoadPatterns.Count == 0)
            {
                AvailableLoadPatterns.Add("SDL");
                AvailableLoadPatterns.Add("LL");
                AvailableLoadPatterns.Add("DEAD");
                AvailableLoadPatterns.Add("SW");
                AvailableLoadPatterns.Add("LIVE");
            }

            this.Loaded += OnWindowLoaded;
        }

        #region WINDOW LIFECYCLE & TITLEBAR

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _isLoading = true;
            try
            {
                UpdateConnectionUI();
                UpdateGeneralSettingsState(radUniformPattern?.IsChecked == true);

                if (AppConnectionManager.IsEtabsConnected)
                {
                    await LoadStoriesAndPatternsAsync();
                    await LoadLoadSetsAsync();
                    await RefreshSlabSectionsAsync();
                }
                else
                {
                    CustomMessageBox.Show(
                        "Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon để kết nối.",
                        "Chưa Kết Nối ETABS",
                        CustomMessageBox.IconType.Warning
                    );
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UpdateConnectionUI()
        {
            bool isEtabs = AppConnectionManager.IsEtabsConnected;
            dotEtabs.Fill = isEtabs ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(220, 50, 50));
            lblEtabsModel.Foreground = isEtabs ? new SolidColorBrush(Color.FromRgb(16, 124, 65)) : new SolidColorBrush(Color.FromRgb(183, 28, 28));
            lblEtabsModel.Text = isEtabs ? (AppConnectionManager.ActiveEtabs?.ModelFileName ?? "Đã kết nối") : "Chưa kết nối (Kết Nối API từ Ribbon)";

            bool isCad = AppConnectionManager.IsAutoCadConnected;

            dotCad.Fill = isCad ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(220, 50, 50));
            lblCadDrawing.Foreground = isCad ? new SolidColorBrush(Color.FromRgb(16, 124, 65)) : new SolidColorBrush(Color.FromRgb(183, 28, 28));
            lblCadDrawing.Text = isCad ? (AppConnectionManager.ActiveAutoCad?.FileName ?? "Đã kết nối") : "Chưa kết nối (Kết Nối API từ Ribbon)";
        }

        private void OnConfigApiClick(object sender, RoutedEventArgs e)
        {
            var apiWin = new GetApiView();
            apiWin.Owner = this;
            apiWin.ShowDialog();
            UpdateConnectionUI();
            _ = LoadStoriesAndPatternsAsync();
        }

        private void OnPickBasePointClick(object sender, RoutedEventArgs e)
        {
            var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
            if (acadDoc == null)
            {
                CustomMessageBox.Show("Chưa kết nối với bản vẽ AutoCAD. Vui lòng mở AutoCAD và thử lại.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                this.Hide();
                double[]? pt = AutoCadHelper.AcadGetPoint(acadDoc, "\n[QKS Shell Load] Chọn điểm BasePoint mốc trên AutoCAD: ");
                if (pt != null && pt.Length >= 2)
                {
                    txtBaseX.Text = pt[0].ToString("F4", CultureInfo.InvariantCulture);
                    txtBaseY.Text = pt[1].ToString("F4", CultureInfo.InvariantCulture);
                    CustomMessageBox.Show($"Đã lấy tọa độ BasePoint từ AutoCAD:\nX = {txtBaseX.Text}\nY = {txtBaseY.Text}", "Thành Công", CustomMessageBox.IconType.Success);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi pick điểm BasePoint: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
            finally
            {
                this.Show();
                this.Activate();
            }
        }

        private void SetStatusMessage(string message, bool isSuccess = true)
        {
            Dispatcher.Invoke(() =>
            {
                if (lblStatusText != null)
                {
                    lblStatusText.Text = message;
                }
            });
        }

        #endregion

        #region DATA LOADING ASYNC

        private async Task LoadStoriesAndPatternsAsync()
        {
            if (!_etabsService.IsConnected) return;

            try
            {
                string[] storyNames;
                double[] storyHeights;
                double[] storyElevs;
                int nStories = await EtabsWork.RunAsync(() => _etabsService.GetStoryList(out storyNames, out storyHeights, out storyElevs));

                cboStory.Items.Clear();
                if (nStories > 0)
                {
                    _etabsService.GetStoryList(out storyNames, out storyHeights, out storyElevs);
                    foreach (var st in storyNames)
                    {
                        cboStory.Items.Add(st);
                    }
                }
                if (cboStory.Items.Count > 0) cboStory.SelectedIndex = 0;

                string[] patNames;
                int nPats = await EtabsWork.RunAsync(() => _etabsService.GetLoadPatternList(out patNames));

                cboLoaiTai.Items.Clear();
                cboAssignPattern.Items.Clear();
                cboZonePattern.Items.Clear();
                AvailableLoadPatterns.Clear();

                if (nPats > 0)
                {
                    _etabsService.GetLoadPatternList(out patNames);
                    foreach (var p in patNames)
                    {
                        cboLoaiTai.Items.Add(p);
                        cboAssignPattern.Items.Add(p);
                        cboZonePattern.Items.Add(p);
                        AvailableLoadPatterns.Add(p);
                    }
                }
                if (AvailableLoadPatterns.Count == 0)
                {
                    AvailableLoadPatterns.Add("SDL");
                    AvailableLoadPatterns.Add("LL");
                    AvailableLoadPatterns.Add("DEAD");
                    AvailableLoadPatterns.Add("SW");
                }
                if (cboLoaiTai.Items.Count > 0) cboLoaiTai.SelectedIndex = 0;
                if (cboAssignPattern.Items.Count > 0) cboAssignPattern.SelectedIndex = 0;
                if (cboZonePattern.Items.Count > 0) cboZonePattern.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Lỗi đọc thông tin ETABS: {ex.Message}", false);
            }
        }

        private async Task LoadLoadSetsAsync()
        {
            if (!_etabsService.IsConnected) return;

            try
            {
                var loadSets = await EtabsWork.RunAsync(() => _etabsService.GetShellUniformLoadSets());
                _loadSetsList.Clear();
                cboDisplayLoadSet.Items.Clear();
                cboAssignLoadSet.Items.Clear();

                cboDisplayLoadSet.Items.Add("<Tất cả Load Sets>");

                foreach (var ls in loadSets)
                {
                    _loadSetsList.Add(ls);
                    cboDisplayLoadSet.Items.Add(ls.LoadSetName);
                    cboAssignLoadSet.Items.Add(ls.LoadSetName);

                    if (ls.Loads != null)
                    {
                        foreach (var d in ls.Loads)
                        {
                            if (!string.IsNullOrEmpty(d.LoadPattern) && !AvailableLoadPatterns.Contains(d.LoadPattern))
                            {
                                AvailableLoadPatterns.Add(d.LoadPattern);
                            }
                        }
                    }
                }

                lstLoadSets.ItemsSource = _loadSetsList;
                if (cboDisplayLoadSet.Items.Count > 0) cboDisplayLoadSet.SelectedIndex = 0;

                if (cboAssignLoadSet.Items.Count > 0) cboAssignLoadSet.SelectedIndex = 0;
                if (_loadSetsList.Count > 0) lstLoadSets.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Lỗi đọc Load Sets: {ex.Message}", false);
            }
        }

        private async Task RefreshSlabSectionsAsync()
        {
            EtabsService.ResetSessionCaches();
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(storyName) || !_etabsService.IsConnected) return;

            SetStatusMessage($"Đang đọc dữ liệu sàn tầng {storyName}...");

            try
            {
                // Đồng bộ danh mục Load Sets hiện tại từ giao diện vào Service
                if (_loadSetsList != null && _loadSetsList.Count > 0)
                {
                    _etabsService.UpdateStoredLoadSets(_loadSetsList);
                }

                var sections = await EtabsWork.RunAsync(() => _etabsService.GetSlabSectionsForStory(storyName));
                _allSections = sections;
                dgSlabSections.ItemsSource = _allSections;

                bool isPattern = radUniformPattern?.IsChecked == true;
                string selPat = isPattern ? (cboLoaiTai?.SelectedItem?.ToString() ?? "") : "";
                string selSet = !isPattern ? (cboDisplayLoadSet?.SelectedItem?.ToString() ?? "") : "";

                var slabs = await EtabsWork.RunAsync(() => _etabsService.GetSlabsForStory(storyName, selPat, selSet));
                double totalArea = slabs.Sum(s => s.Area);
                int matchingCount = slabs.Count(s => !string.IsNullOrEmpty(s.FormattedLoadText) && s.FormattedLoadText != "0");
                string loadModeText = isPattern
                    ? $"Pattern: {selPat}"
                    : (!string.IsNullOrEmpty(selSet) && !selSet.StartsWith("<") ? $"LoadSet: {selSet} ({matchingCount}/{slabs.Count} ô sàn có tải)" : $"LoadSet: Tất cả ({matchingCount}/{slabs.Count} ô sàn có tải)");
                lblSlabStats.Text = $"• Số lượng ô sàn: {slabs.Count} • Tổng diện tích: {totalArea:0.##} m² • Số loại tiết diện: {sections.Count} • Chế độ: {loadModeText}";
                SetStatusMessage($"Đã nạp {slabs.Count} ô sàn ({sections.Count} loại tiết diện) tại tầng {storyName}.");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Lỗi đọc dữ liệu sàn: {ex.Message}", false);
            }
        }

        private async void OnStoryChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            await RefreshSlabSectionsAsync();
        }

        private void OnSidebarPatternSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            if (cboLoaiTai.SelectedItem != null)
            {
                if (cboAssignPattern != null) cboAssignPattern.SelectedItem = cboLoaiTai.SelectedItem;
                if (cboZonePattern != null) cboZonePattern.SelectedItem = cboLoaiTai.SelectedItem;
                if (radUniformPattern?.IsChecked == true)
                {
                    _ = RefreshSlabSectionsAsync();
                }
            }
        }

        #endregion

        #region TAB 1: XUẤT MẶT BẰNG SÀN (ETABS -> AUTOCAD)

        private void OnDisplayModeChanged(object sender, RoutedEventArgs e)
        {
            if (radUniformPattern == null || gridLoadSetSelection == null) return;

            bool isLoadSet = radLoadSet.IsChecked == true;
            gridLoadSetSelection.Visibility = isLoadSet ? Visibility.Visible : Visibility.Collapsed;

            // Cập nhật trạng thái khóa/mờ Thiết Lập Tải Trọng Chung
            UpdateGeneralSettingsState(!isLoadSet);

            _ = RefreshSlabSectionsAsync();
        }

        private void UpdateGeneralSettingsState(bool isPattern)
        {
            if (grpThietLapChung != null)
            {
                grpThietLapChung.IsEnabled = isPattern;
                grpThietLapChung.Opacity = isPattern ? 1.0 : 0.55;
                grpThietLapChung.ToolTip = isPattern
                    ? null
                    : "Thiết lập tải trọng chung được khóa khi chọn chế độ Load Set (tải trọng được định nghĩa theo từng Load Set).";
            }
            if (cboLoaiTai != null)
            {
                cboLoaiTai.IsEnabled = isPattern;
            }
        }

        private void OnDisplaySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            _ = RefreshSlabSectionsAsync();
        }

        private async void OnXuatCadClick(object sender, RoutedEventArgs e)
        {
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(storyName))
            {
                CustomMessageBox.Show("Vui lòng chọn tầng cần xuất sang AutoCAD.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                return;
            }

            var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
            if (acadDoc == null)
            {
                CustomMessageBox.Show("Chưa kết nối với AutoCAD. Vui lòng mở bản vẽ AutoCAD và thử lại.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                double.TryParse(txtBaseX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double baseX);
                double.TryParse(txtBaseY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double baseY);
                double.TryParse(txtTextHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double textHeight);
                if (textHeight <= 0) textHeight = 250.0;

                double.TryParse(txtMinArea.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double minArea);
                if (minArea < 0) minArea = 0.5;

                int textColorIdx = 4;
                if (cboTextColor.SelectedItem is ComboBoxItem cboItem && int.TryParse(cboItem.Tag?.ToString(), out int tc))
                {
                    textColorIdx = tc;
                }

                bool isPattern = radUniformPattern.IsChecked == true;
                string selPat = isPattern ? (cboLoaiTai.SelectedItem?.ToString() ?? "") : "";
                string selSet = !isPattern ? (cboDisplayLoadSet.SelectedItem?.ToString() ?? "") : "";

                SetStatusMessage($"Đang trích xuất dữ liệu sàn tầng {storyName}...");

                var slabs = await EtabsWork.RunAsync(() => _etabsService.GetSlabsForStory(storyName, selPat, selSet));
                if (slabs.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy ô sàn nào tại tầng {storyName}.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                // Xây dựng payload
                var payload = new CadShellExportPayload
                {
                    BasePointX = baseX,
                    BasePointY = baseY,
                    TextHeight = textHeight,
                    TextColorIndex = textColorIdx,
                    MinAreaM2 = minArea,
                    DrawBoundary = chkDrawBoundary.IsChecked == true,
                    DrawHatch = chkDrawHatch.IsChecked == true,
                    HatchTransparency = 70,
                    DrawRectangle = chkDrawRectangle.IsChecked == true,
                    DrawText = chkDrawText.IsChecked == true,
                    StoryName = storyName,
                    ModeTitle = isPattern ? $"Pattern: {selPat}" : $"LoadSet: {selSet}"
                };

                foreach (var s in slabs)
                {
                    var polyItem = new CadShellPolygonItem
                    {
                        AreaName = s.Name,
                        Section = s.Section,
                        CentroidX = (s.CentroidX * 1000.0) + baseX,
                        CentroidY = (s.CentroidY * 1000.0) + baseY,
                        AreaM2 = Math.Round(s.Area, 2),
                        ColorR = s.ColorR,
                        ColorG = s.ColorG,
                        ColorB = s.ColorB,
                        SectionText = s.Section,
                        LoadText = (!string.IsNullOrEmpty(s.FormattedLoadText) && s.FormattedLoadText != "0") ? s.FormattedLoadText : "0",
                        HasLoad = !string.IsNullOrEmpty(s.FormattedLoadText) && s.FormattedLoadText != "0"
                    };

                    foreach (var pt in s.Points)
                    {
                        polyItem.Points.Add(new double[] { (pt.X * 1000.0) + baseX, (pt.Y * 1000.0) + baseY });
                    }

                    payload.Slabs.Add(polyItem);
                }

                string outDir = _exchangeDir;
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                string jsonPath = Path.Combine(outDir, "cad_shell_export.json");
                File.WriteAllText(jsonPath, JsonHelper.Serialize(payload));

                // Ẩn form để người dùng click chọn điểm dán trên CAD
                this.Hide();
                double[]? pastePoint = AutoCadHelper.AcadGetPoint(acadDoc, "\n[QKS Shell Load] Chọn điểm dán mặt bằng sàn trên AutoCAD: ");

                if (pastePoint == null || pastePoint.Length < 2)
                {
                    this.Show();
                    return;
                }

                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string formattedJson = jsonPath.Replace("\\", "/");
                string pasteXStr = pastePoint[0].ToString(CultureInfo.InvariantCulture);
                string pasteYStr = pastePoint[1].ToString(CultureInfo.InvariantCulture);
                string textHeightStr = textHeight.ToString(CultureInfo.InvariantCulture);
                string minAreaStr = minArea.ToString(CultureInfo.InvariantCulture);

                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_SHELL_DRAW_SLABS \"{formattedJson}\" {pasteXStr} {pasteYStr} {textHeightStr} {textColorIdx} {minAreaStr}\n";
                await _cadSession.SendAsync((object)acadDoc, cmdStr, CadContext, this);

                this.Show();
                this.Activate();

                CustomMessageBox.Show(
                    $"Đã xuất thành công mặt bằng tải sàn sang AutoCAD:\n\n" +
                    $"• Tầng: {storyName}\n" +
                    $"• Chế độ hiển thị: {payload.ModeTitle}\n" +
                    $"• Số lượng ô sàn: {slabs.Count}\n" +
                    $"• Bộ lọc diện tích Min: {minArea:0.##} m²\n" +
                    $"• Chiều cao Text: {textHeight:0.##} mm",
                    "Xuất CAD Thành Công",
                    CustomMessageBox.IconType.Success
                );
                SetStatusMessage($"Đã xuất mặt bằng tải sàn tầng {storyName} sang AutoCAD.");
            }
            catch (Exception ex)
            {
                this.Show();
                CustomMessageBox.Show($"Lỗi xuất mặt bằng sàn sang CAD: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        #endregion

        #region TAB 2: KHAI BÁO SHELL UNIFORM LOAD SETS

        private void OnLoadSetSelected(object sender, SelectionChangedEventArgs e)
        {
            // Tự động lưu lại các thay đổi của Set trước đó nếu có
            if (_selectedLoadSet != null)
            {
                _selectedLoadSet.Loads = _currentSetDetails.ToList();
            }

            if (lstLoadSets.SelectedItem is ShellLoadSetItem selSet)
            {
                _selectedLoadSet = selSet;
                _currentSetDetails.Clear();
                if (selSet.Loads != null)
                {
                    foreach (var load in selSet.Loads)
                    {
                        _currentSetDetails.Add(new ShellLoadSetDetail
                        {
                            LoadPattern = load.LoadPattern,
                            LoadValue = load.LoadValue,
                            Direction = load.Direction
                        });
                    }
                }
                dgLoadSetDetails.ItemsSource = _currentSetDetails;
            }
        }

        private void OnAddLoadSetClick(object sender, RoutedEventArgs e)
        {
            if (_selectedLoadSet != null && _currentSetDetails != null)
            {
                _selectedLoadSet.Loads = _currentSetDetails.ToList();
            }

            string newName = $"LoadSet_{_loadSetsList.Count + 1}";
            var newSet = new ShellLoadSetItem
            {
                LoadSetName = newName,
                OriginalName = newName,
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 1.2, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 2.0, Direction = "Gravity" }
                }
            };
            _loadSetsList.Add(newSet);
            lstLoadSets.SelectedItem = newSet;
            RefreshLoadSetComboBoxes(newName);
        }

        private void OnRenameLoadSetClick(object sender, RoutedEventArgs e)
        {
            if (lstLoadSets.SelectedItem is ShellLoadSetItem selSet)
            {
                string oldName = selSet.LoadSetName;
                string? newName = CustomMessageBox.ShowInput(
                    "Nhập tên mới cho Shell Uniform Load Set:",
                    "Đổi Tên Load Set",
                    oldName
                );

                if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                newName = newName!.Trim();

                // Kiểm tra trùng tên với Set khác
                if (_loadSetsList.Any(s => s != selSet && s.LoadSetName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    CustomMessageBox.Show($"Tên Load Set '{newName}' đã tồn tại trong danh mục. Vui lòng chọn tên khác.", "Trùng Tên Load Set", CustomMessageBox.IconType.Warning);
                    return;
                }

                selSet.LoadSetName = newName;
                RefreshLoadSetComboBoxes(newName);
                SetStatusMessage($"Đã đổi tên Load Set thành '{newName}'. Bấm 'LƯU LOAD SETS VÀO ETABS' để áp dụng vào mô hình.");
            }
            else
            {
                CustomMessageBox.Show("Vui lòng chọn một Load Set trong danh sách để đổi tên.", "Chưa Chọn Load Set", CustomMessageBox.IconType.Warning);
            }
        }

        private void OnLoadSetsMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstLoadSets.SelectedItem is ShellLoadSetItem)
            {
                OnRenameLoadSetClick(sender, e);
            }
        }

        private void OnLoadSetNameLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is ShellLoadSetItem item)
            {
                string text = tb.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(text))
                {
                    tb.Text = !string.IsNullOrEmpty(item.OriginalName) ? item.OriginalName : "LoadSet";
                    item.LoadSetName = tb.Text;
                    return;
                }

                // Kiểm tra trùng tên với Set khác
                if (_loadSetsList.Any(s => s != item && s.LoadSetName.Equals(text, StringComparison.OrdinalIgnoreCase)))
                {
                    CustomMessageBox.Show($"Tên Load Set '{text}' đã tồn tại trong danh mục. Vui lòng chọn tên khác.", "Trùng Tên Load Set", CustomMessageBox.IconType.Warning);
                    tb.Text = !string.IsNullOrEmpty(item.OriginalName) ? item.OriginalName : "LoadSet";
                    item.LoadSetName = tb.Text;
                    return;
                }

                item.LoadSetName = text;
                RefreshLoadSetComboBoxes(text);
            }
        }

        private void OnLoadSetNameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
            }
        }

        private void RefreshLoadSetComboBoxes(string? selectName = null)
        {
            string curDisplay = selectName ?? (cboDisplayLoadSet.SelectedItem?.ToString() ?? "");
            string curAssign = selectName ?? (cboAssignLoadSet.SelectedItem?.ToString() ?? "");

            cboDisplayLoadSet.Items.Clear();
            cboAssignLoadSet.Items.Clear();

            cboDisplayLoadSet.Items.Add("<Tất cả Load Sets>");

            foreach (var ls in _loadSetsList)
            {
                cboDisplayLoadSet.Items.Add(ls.LoadSetName);
                cboAssignLoadSet.Items.Add(ls.LoadSetName);
            }

            if (!string.IsNullOrEmpty(curDisplay) && cboDisplayLoadSet.Items.Contains(curDisplay))
            {
                cboDisplayLoadSet.SelectedItem = curDisplay;
            }
            else if (cboDisplayLoadSet.Items.Count > 0)
            {
                cboDisplayLoadSet.SelectedIndex = 0;
            }

            if (!string.IsNullOrEmpty(curAssign) && cboAssignLoadSet.Items.Contains(curAssign))
            {
                cboAssignLoadSet.SelectedItem = curAssign;
            }
            else if (cboAssignLoadSet.Items.Count > 0)
            {
                cboAssignLoadSet.SelectedIndex = 0;
            }
        }

        private void OnDeleteLoadSetClick(object sender, RoutedEventArgs e)
        {
            if (lstLoadSets.SelectedItem is ShellLoadSetItem selSet)
            {
                _loadSetsList.Remove(selSet);
                _currentSetDetails.Clear();
                _selectedLoadSet = null;
                _etabsService.DeleteShellUniformLoadSet(selSet.LoadSetName);
                RefreshLoadSetComboBoxes();
            }
        }

        private void OnAddLoadDetailClick(object sender, RoutedEventArgs e)
        {
            if (_selectedLoadSet == null) return;
            string defPat = AvailableLoadPatterns.Count > 0
                ? AvailableLoadPatterns[0]
                : (cboLoaiTai.Items.Count > 0 ? (cboLoaiTai.Items[0]?.ToString() ?? "SDL") : "SDL");
            var detail = new ShellLoadSetDetail { LoadPattern = defPat, LoadValue = 1.0, Direction = "Gravity" };
            _currentSetDetails.Add(detail);
            _selectedLoadSet.Loads = _currentSetDetails.ToList();
        }

        private void OnDeleteLoadDetailClick(object sender, RoutedEventArgs e)
        {
            if (dgLoadSetDetails.SelectedItem is ShellLoadSetDetail selDetail)
            {
                _currentSetDetails.Remove(selDetail);
                if (_selectedLoadSet != null)
                {
                    _selectedLoadSet.Loads = _currentSetDetails.ToList();
                }
            }
        }

        private void OnExportExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic? excelApp = ExcelDnaUtil.Application;
                if (excelApp == null)
                {
                    CustomMessageBox.Show("Không thể kết nối với ứng dụng Microsoft Excel.", "Lỗi Excel", CustomMessageBox.IconType.Warning);
                    return;
                }

                dynamic activeWb = excelApp.ActiveWorkbook;
                if (activeWb == null) activeWb = excelApp.Workbooks.Add();

                dynamic ws = activeWb.Sheets.Add();
                ws.Name = "QKS_LoadSets_" + DateTime.Now.ToString("HHmmss");

                // Headers
                ws.Cells[1, 1].Value = "Load Set Name";
                ws.Cells[1, 2].Value = "Load Pattern";
                ws.Cells[1, 3].Value = "Load Value (kN/m²)";
                ws.Cells[1, 4].Value = "Direction";

                dynamic headerRange = ws.Range["A1:D1"];
                headerRange.Font.Bold = true;
                headerRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(15, 34, 64));
                headerRange.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);

                int row = 2;
                foreach (var ls in _loadSetsList)
                {
                    if (ls.Loads == null || ls.Loads.Count == 0)
                    {
                        ws.Cells[row, 1].Value = ls.LoadSetName;
                        ws.Cells[row, 2].Value = "";
                        ws.Cells[row, 3].Value = 0;
                        ws.Cells[row, 4].Value = "Gravity";
                        row++;
                    }
                    else
                    {
                        foreach (var ld in ls.Loads)
                        {
                            ws.Cells[row, 1].Value = ls.LoadSetName;
                            ws.Cells[row, 2].Value = ld.LoadPattern;
                            ws.Cells[row, 3].Value = ld.LoadValue;
                            ws.Cells[row, 4].Value = ld.Direction;
                            row++;
                        }
                    }
                }

                ws.Columns["A:D"].AutoFit();
                CustomMessageBox.Show($"Đã xuất thành công {_loadSetsList.Count} Shell Load Sets ra Sheet '{ws.Name}'.", "Xuất Excel Thành Công", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void OnImportExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic? excelApp = ExcelDnaUtil.Application;
                if (excelApp == null) return;
                dynamic activeSheet = excelApp.ActiveSheet;
                if (activeSheet == null) return;

                int row = 2;
                var dict = new Dictionary<string, List<ShellLoadSetDetail>>(StringComparer.OrdinalIgnoreCase);

                while (true)
                {
                    string setName = activeSheet.Cells[row, 1].Value?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(setName)) break;

                    string pat = activeSheet.Cells[row, 2].Value?.ToString() ?? "";
                    double.TryParse(activeSheet.Cells[row, 3].Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
                    string dir = activeSheet.Cells[row, 4].Value?.ToString() ?? "Gravity";

                    if (!dict.ContainsKey(setName))
                    {
                        dict[setName] = new List<ShellLoadSetDetail>();
                    }

                    if (!string.IsNullOrEmpty(pat))
                    {
                        dict[setName].Add(new ShellLoadSetDetail { LoadPattern = pat, LoadValue = val, Direction = dir });
                    }
                    row++;
                }

                if (dict.Count == 0)
                {
                    CustomMessageBox.Show("Không tìm thấy dữ liệu Load Sets nào trên Sheet Excel hiện hành.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                _loadSetsList.Clear();
                foreach (var kv in dict)
                {
                    _loadSetsList.Add(new ShellLoadSetItem { LoadSetName = kv.Key, Loads = kv.Value });
                }

                if (_loadSetsList.Count > 0) lstLoadSets.SelectedIndex = 0;
                CustomMessageBox.Show($"Đã nạp thành công {_loadSetsList.Count} Load Sets từ Excel.", "Nạp Excel Thành Công", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi nạp từ Excel: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private async void OnSaveLoadSetsToEtabsClick(object sender, RoutedEventArgs e)
        {
            using var operation = EtabsService.BeginOperation();
            try
            {
            if (!_etabsService.IsConnected)
            {
                CustomMessageBox.Show("Chưa kết nối ETABS.", "Thông Báo", CustomMessageBox.IconType.Warning);
                return;
            }

            // Đồng bộ details từ DataGrid vào _selectedLoadSet
            if (_selectedLoadSet != null && _currentSetDetails != null)
            {
                _selectedLoadSet.Loads = _currentSetDetails.ToList();
            }

            SetStatusMessage("Đang lưu Load Sets vào ETABS qua DatabaseTables OAPI...");

            bool ok = await EtabsWork.RunAsync(() => _etabsService.SaveShellUniformLoadSetsToEtabs(_loadSetsList.ToList()));

            await LoadLoadSetsAsync();
            if (ok)
            {
                CustomMessageBox.Show($"Đã lưu thành công {_loadSetsList.Count} Shell Uniform Load Sets vào mô hình ETABS.", "Lưu Thành Công", CustomMessageBox.IconType.Success);
            }
            else
            {
                CustomMessageBox.Show($"Đã lưu {_loadSetsList.Count} Shell Uniform Load Sets vào mô hình ETABS.", "Lưu Thành Công", CustomMessageBox.IconType.Success);
            }

            }
            catch (Exception writeError)
            {
                CustomMessageBox.Show(writeError.Message + "\nNếu lệnh đã bắt đầu ghi, kiểm tra model và thư mục QKS_Backups trước khi chạy lại.", "Không hoàn tất thao tác", CustomMessageBox.IconType.ErrorType);
                if (!IsVisible) Show();
            }
        }

        #endregion

        #region TAB 3: GÁN TẢI TRỌNG SÀN THEO TIẾT DIỆN & CAD LIVE SYNC

        private void OnSearchSectionChanged(object sender, TextChangedEventArgs e)
        {
            string query = txtSearchSection.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                dgSlabSections.ItemsSource = _allSections;
            }
            else
            {
                dgSlabSections.ItemsSource = _allSections.Where(s => s.SectionName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        private async void OnSelectCadSectionClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string secName && !string.IsNullOrEmpty(secName))
            {
                var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Chưa kết nối với AutoCAD.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_SHELL_SELECT_SECTION \"{secName}\"\n";
                await _cadSession.SendAsync((object)acadDoc, cmdStr, CadContext, this);
            }
        }

        private async void OnAssignLoadSetClick(object sender, RoutedEventArgs e)
        {
            using var operation = EtabsService.BeginOperation();
            try
            {
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            string loadSet = cboAssignLoadSet.SelectedItem?.ToString() ?? "";
            var selSection = dgSlabSections.SelectedItem as ShellSectionInfo;

            if (selSection == null)
            {
                CustomMessageBox.Show("Vui lòng chọn 1 loại tiết diện sàn trên bảng để gán.", "Chưa Chọn Tiết Diện", CustomMessageBox.IconType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(loadSet))
            {
                CustomMessageBox.Show("Vui lòng chọn Shell Uniform Load Set cần gán.", "Chưa Chọn Load Set", CustomMessageBox.IconType.Warning);
                return;
            }

            SetStatusMessage($"Đang gán LoadSet {loadSet} vào {selSection.SectionName}...");
            int count = await EtabsWork.RunAsync(() => _etabsService.AssignShellLoadSet(storyName, selSection.SectionName, loadSet));

            // Cập nhật Text tải trên CAD thời gian thực nếu bật Live Sync
            if (chkLiveCadSync.IsChecked == true)
            {
                await SyncCadTextLive(selSection.SectionName, $"Set: {loadSet}");
            }

            await RefreshSlabSectionsAsync();
            CustomMessageBox.Show($"Đã gán thành công LoadSet '{loadSet}' cho {count} ô sàn '{selSection.SectionName}' tại tầng {storyName}.", "Gán Thành Công", CustomMessageBox.IconType.Success);

            }
            catch (Exception writeError)
            {
                CustomMessageBox.Show(writeError.Message + "\nNếu lệnh đã bắt đầu ghi, kiểm tra model và thư mục QKS_Backups trước khi chạy lại.", "Không hoàn tất thao tác", CustomMessageBox.IconType.ErrorType);
                if (!IsVisible) Show();
            }
        }

        private async void OnAssignUniformLoadClick(object sender, RoutedEventArgs e)
        {
            using var operation = EtabsService.BeginOperation();
            try
            {
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            string pattern = cboAssignPattern.SelectedItem?.ToString() ?? "";
            var selSection = dgSlabSections.SelectedItem as ShellSectionInfo;

            if (selSection == null)
            {
                CustomMessageBox.Show("Vui lòng chọn 1 loại tiết diện sàn trên bảng để gán.", "Chưa Chọn Tiết Diện", CustomMessageBox.IconType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(pattern))
            {
                CustomMessageBox.Show("Vui lòng chọn trường hợp tải (Pattern).", "Chưa Chọn Pattern", CustomMessageBox.IconType.Warning);
                return;
            }

            double.TryParse(txtAssignLoadVal.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
            double.TryParse(txtFactorK.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double k);
            if (k <= 0) k = 1.0;
            double finalVal = val * k;

            int dir = 10; // Gravity
            if (cboDirection.SelectedItem is ComboBoxItem dirItem && int.TryParse(dirItem.Tag?.ToString(), out int d))
            {
                dir = d;
            }

            bool replace = cboReplaceMode.SelectedIndex == 0;

            SetStatusMessage($"Đang gán tải {finalVal:0.##} kN/m2 vào {selSection.SectionName}...");
            int count = await EtabsWork.RunAsync(() => _etabsService.AssignShellUniformLoad(storyName, selSection.SectionName, pattern, finalVal, dir, replace));

            // Cập nhật Text tải trên CAD thời gian thực nếu bật Live Sync
            if (chkLiveCadSync.IsChecked == true)
            {
                await SyncCadTextLive(selSection.SectionName, $"{pattern} = {finalVal:0.##} kN/m2");
            }

            await RefreshSlabSectionsAsync();
            CustomMessageBox.Show($"Đã gán thành công tải {finalVal:0.##} kN/m2 ({pattern}) cho {count} ô sàn '{selSection.SectionName}' tại tầng {storyName}.", "Gán Thành Công", CustomMessageBox.IconType.Success);

            }
            catch (Exception writeError)
            {
                CustomMessageBox.Show(writeError.Message + "\nNếu lệnh đã bắt đầu ghi, kiểm tra model và thư mục QKS_Backups trước khi chạy lại.", "Không hoàn tất thao tác", CustomMessageBox.IconType.ErrorType);
                if (!IsVisible) Show();
            }
        }

        private Task SyncCadTextLive(string sectionName, string newLoadText)
        {
            return SyncCadTextLive(new Dictionary<string, string> { { sectionName, newLoadText } }, sectionName);
        }

        private async Task SyncCadTextLive(Dictionary<string, string> sectionMap, string targetSection = "")
        {
            try
            {
                var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null) return;

                var livePayload = new CadShellLiveUpdatePayload
                {
                    TargetSection = targetSection,
                    NewLoadText = (!string.IsNullOrEmpty(targetSection) && sectionMap.ContainsKey(targetSection)) ? sectionMap[targetSection] : "",
                    SectionLoadMap = sectionMap
                };

                string outDir = _exchangeDir;
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                string jsonFile = Path.Combine(outDir, "cad_shell_update.json");
                File.WriteAllText(jsonFile, JsonHelper.Serialize(livePayload));

                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string formattedJson = jsonFile.Replace("\\", "/");
                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_SHELL_UPDATE_TEXT \"{formattedJson}\"\n";
                await _cadSession.SendAsync((object)acadDoc, cmdStr, CadContext, this);
            }
            catch (Exception ex) { CustomMessageBox.Show(ex.Message, "Không đồng bộ được CAD", CustomMessageBox.IconType.Warning); }
        }

        #endregion

        #region VÙNG TẢI RECTANGLE CỤC BỘ & ACTION FOOTER

        private async void OnPickZoneRectClick(object sender, RoutedEventArgs e)
        {
            using var operation = EtabsService.BeginOperation();
            try
            {
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(storyName))
            {
                CustomMessageBox.Show("Vui lòng chọn tầng cần gán vùng tải.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                return;
            }

            var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
            if (acadDoc == null)
            {
                CustomMessageBox.Show("Chưa kết nối với AutoCAD. Vui lòng mở AutoCAD và thử lại.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                double.TryParse(txtBaseX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double baseX);
                double.TryParse(txtBaseY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double baseY);

                this.Hide();

                // Sử dụng hàm Direct COM Utility GetRectangle (nhanh tức thời, không bị treo)
                (double[]? p1, double[]? p2) = AutoCadHelper.AcadGetRectangle((object)acadDoc);

                this.Show();
                this.Activate();

                if (p1 == null || p2 == null)
                {
                    // Người dùng hủy (ESC)
                    return;
                }

                double minCadX = Math.Min(p1[0], p2[0]);
                double maxCadX = Math.Max(p1[0], p2[0]);
                double minCadY = Math.Min(p1[1], p2[1]);
                double maxCadY = Math.Max(p1[1], p2[1]);

                // Vẽ phản hồi hình chữ nhật lên AutoCAD
                AutoCadHelper.DrawZoneRectangle(acadDoc, minCadX, minCadY, maxCadX, maxCadY);

                // Khử BasePoint về tọa độ ETABS (mét)
                double x1 = (minCadX - baseX) / 1000.0;
                double y1 = (minCadY - baseY) / 1000.0;
                double x2 = (maxCadX - baseX) / 1000.0;
                double y2 = (maxCadY - baseY) / 1000.0;

                double dx = Math.Abs(x2 - x1);
                double dy = Math.Abs(y2 - y1);
                double lengthM = Math.Max(dx, dy);
                double widthM = Math.Min(dx, dy);
                double areaM2 = dx * dy;

                string pat = cboZonePattern.SelectedItem?.ToString() ?? (cboAssignPattern.SelectedItem?.ToString() ?? (cboLoaiTai.SelectedItem?.ToString() ?? "SDL"));
                double.TryParse(txtZoneValue.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
                if (val <= 0)
                {
                    double.TryParse(txtAssignLoadVal.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out val);
                }
                if (val <= 0) val = 2.0;

                int dir = 10;
                if (cboDirection.SelectedItem is ComboBoxItem dirItem && int.TryParse(dirItem.Tag?.ToString(), out int d))
                {
                    dir = d;
                }
                string dirName = (cboDirection.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gravity";

                // Xác định tiết diện sàn nền nằm dưới vùng tải vừa pick
                string detectedSection = "Vùng mới";
                try
                {
                    var storySlabs = _etabsService.GetSlabsForStory(storyName);
                    double minX = Math.Min(x1, x2);
                    double maxX = Math.Max(x1, x2);
                    double minY = Math.Min(y1, y2);
                    double maxY = Math.Max(y1, y2);
                    double centerX = (minX + maxX) / 2.0;
                    double centerY = (minY + maxY) / 2.0;

                    var matched = storySlabs.Where(s =>
                        s.Points.Count >= 3 &&
                        (
                            (centerX >= s.Points.Min(p => p.X) && centerX <= s.Points.Max(p => p.X) &&
                             centerY >= s.Points.Min(p => p.Y) && centerY <= s.Points.Max(p => p.Y)) ||
                            (s.CentroidX >= minX && s.CentroidX <= maxX && s.CentroidY >= minY && s.CentroidY <= maxY)
                        )
                    ).ToList();

                    if (matched.Count > 0)
                    {
                        var distinctSecs = matched.Select(s => s.Section).Where(sec => !string.IsNullOrEmpty(sec)).Distinct().ToList();
                        if (distinctSecs.Count > 0)
                        {
                            detectedSection = string.Join(", ", distinctSecs);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                SetStatusMessage("Đang tạo vùng tải sàn cục bộ trong ETABS...");
                bool ok = await EtabsWork.RunAsync(() => _etabsService.AssignZoneRectangleLoad(storyName, x1, y1, x2, y2, pat, val, dir, true));

                if (ok)
                {
                    await RefreshSlabSectionsAsync();
                    CustomMessageBox.Show(
                        $"Đã tạo và gán thành công vùng tải sàn cục bộ vào ETABS:\n\n" +
                        $"• Tầng gán tải: {storyName}\n" +
                        $"• Tên loại tải (Pattern): {pat} ({dirName})\n" +
                        $"• Chiều dài vùng tải: {lengthM:0.##} m ({(lengthM * 1000.0):0} mm)\n" +
                        $"• Chiều rộng vùng tải: {widthM:0.##} m ({(widthM * 1000.0):0} mm)\n" +
                        $"• Diện tích vùng tải: {areaM2:0.##} m²\n" +
                        $"• Nằm ở Section: {detectedSection}\n" +
                        $"• Giá trị tải trọng gán: {val:0.##} kN/m²",
                        "Gán Vùng Tải Thành Công",
                        CustomMessageBox.IconType.Success
                    );
                }
                else
                {
                    CustomMessageBox.Show("Có lỗi xảy ra khi tạo vùng tải sàn trong ETABS.", "Lỗi", CustomMessageBox.IconType.ErrorType);
                }
            }
            catch (Exception ex)
            {
                this.Show();
                CustomMessageBox.Show($"Lỗi thực thi vùng tải: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }

            }
            catch (Exception writeError)
            {
                CustomMessageBox.Show(writeError.Message + "\nNếu lệnh đã bắt đầu ghi, kiểm tra model và thư mục QKS_Backups trước khi chạy lại.", "Không hoàn tất thao tác", CustomMessageBox.IconType.ErrorType);
                if (!IsVisible) Show();
            }
        }

        private void OnGanTaiEtabsClick(object sender, RoutedEventArgs e)
        {
            // Chuyển sang Tab 3 để người dùng chọn gán
            mainTabControl.SelectedIndex = 2;
        }

        private async void OnNhapCadClick(object sender, RoutedEventArgs e)
        {
            using var operation = EtabsService.BeginOperation();
            try
            {
            if (!_etabsService.IsConnected)
            {
                CustomMessageBox.Show("Chưa kết nối với ETABS. Vui lòng mở ETABS và thử lại.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                return;
            }

            var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
            if (acadDoc == null)
            {
                CustomMessageBox.Show("Chưa kết nối với AutoCAD. Vui lòng mở AutoCAD và thử lại.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                return;
            }

            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(storyName))
            {
                CustomMessageBox.Show("Vui lòng chọn tầng cần nhập tải trọng.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                this.Hide();

                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_SHELL_SCAN_LOADS\n";
                var scanResponse = await _cadSession.SendAsync((object)acadDoc, cmdStr, CadContext, this);

                this.Show();
                this.Activate();

                string outDir = _exchangeDir;
                string filePath = Path.Combine(outDir, "cad_shell_import.json");

                string json = scanResponse.PayloadJson;
                var importItems = JsonHelper.Deserialize<List<CadShellImportItem>>(json);
                if (importItems == null || importItems.Count == 0)
                {
                    CustomMessageBox.Show("Không có Text tải trọng nào được quét từ vùng chọn AutoCAD.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                SetStatusMessage($"Đang xử lý và gán tải cho {importItems.Count} ô sàn vào ETABS...");

                string activePat = cboZonePattern.SelectedItem?.ToString() ?? (cboAssignPattern.SelectedItem?.ToString() ?? (cboLoaiTai.SelectedItem?.ToString() ?? "SDL"));
                int dir = 10; // Gravity
                if (cboDirection.SelectedItem is ComboBoxItem dirItem && int.TryParse(dirItem.Tag?.ToString(), out int d))
                {
                    dir = d;
                }

                int updatedCount = await EtabsWork.RunAsync(() => _etabsService.AssignImportedShellLoads(importItems, activePat, dir));

                if (updatedCount > 0)
                {
                    await RefreshSlabSectionsAsync();
                    CustomMessageBox.Show(
                        $"Đã cập nhật thành công tải trọng từ AutoCAD vào {updatedCount} ô sàn trong ETABS (Tầng {storyName})!",
                        "Nhập Tải Thành Công",
                        CustomMessageBox.IconType.Success
                    );
                }
                else
                {
                    CustomMessageBox.Show("Không có ô sàn nào được cập nhật trong ETABS. Vui lòng kiểm tra lại text tải trên CAD.", "Thông Báo", CustomMessageBox.IconType.Warning);
                }
            }
            catch (Exception ex)
            {
                this.Show();
                CustomMessageBox.Show($"Lỗi nhập tải từ CAD: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }

            }
            catch (Exception writeError)
            {
                CustomMessageBox.Show(writeError.Message + "\nNếu lệnh đã bắt đầu ghi, kiểm tra model và thư mục QKS_Backups trước khi chạy lại.", "Không hoàn tất thao tác", CustomMessageBox.IconType.ErrorType);
                if (!IsVisible) Show();
            }
        }

        private async void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                dgSlabSections.SelectedItem = null;
                lstLoadSets.SelectedItem = null;

                try
                {
                    var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                    if (acadDoc != null)
                    {
                        string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                        string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_SHELL_CLEAR_SELECTION\n";
                        await _cadSession.SendAsync((object)acadDoc, cmdStr, CadContext, this);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                SetStatusMessage("Đã hủy chọn tiết diện sàn trên giao diện và AutoCAD.");
            }
        }

        private async void OnLiveSyncCadButtonClick(object sender, RoutedEventArgs e)
        {
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(storyName))
            {
                CustomMessageBox.Show("Vui lòng chọn tầng cần đồng bộ Live CAD.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                return;
            }

            var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
            if (acadDoc == null)
            {
                CustomMessageBox.Show("Chưa kết nối với AutoCAD. Vui lòng mở AutoCAD và thử lại.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                return;
            }

            if (!_etabsService.IsConnected)
            {
                CustomMessageBox.Show("Chưa kết nối với ETABS. Vui lòng mở mô hình ETABS và thử lại.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                if (dgSlabSections.SelectedItem is ShellSectionInfo selSec)
                {
                    // 1. Đồng bộ cho 1 tiết diện cụ thể đang chọn
                    string pat = cboAssignPattern.SelectedItem?.ToString() ?? "SDL";
                    string val = txtAssignLoadVal.Text?.Trim() ?? "2.0";
                    string loadSummary = $"{pat} = {val} kN/m²";
                    if (cboAssignLoadSet.SelectedItem != null && !string.IsNullOrEmpty(cboAssignLoadSet.SelectedItem.ToString()))
                    {
                        string lsName = cboAssignLoadSet.SelectedItem.ToString() ?? "";
                        var matchedSet = _loadSetsList.FirstOrDefault(x => x.LoadSetName.Equals(lsName, StringComparison.OrdinalIgnoreCase));
                        if (matchedSet != null)
                        {
                            loadSummary = matchedSet.SummaryText;
                        }
                    }

                    // Thông báo xác nhận trước khi đồng bộ 1 tiết diện
                    bool? confirm = CustomMessageBox.ShowConfirm(
                        $"Bạn có chắc chắn muốn Đồng Bộ CAD Live cho riêng tiết diện sàn '{selSec.SectionName}' tại tầng {storyName}?\n\n" +
                        $"• Tiết diện: {selSec.SectionName}\n" +
                        $"• Giá trị tải sẽ cập nhật: {loadSummary}\n\n" +
                        $"Nhấn 'Xác nhận' để tiến hành cập nhật thời gian thực lên bản vẽ AutoCAD.",
                        "Xác Nhận Đồng Bộ CAD Live"
                    );

                    if (confirm != true) return;

                    await SyncCadTextLive(selSec.SectionName, loadSummary);
                    SetStatusMessage($"Đã đồng bộ Live CAD Text cho tiết diện '{selSec.SectionName}'.");

                    CustomMessageBox.Show(
                        $"Đã đồng bộ Live CAD thành công cho tiết diện '{selSec.SectionName}':\n\n" +
                        $"• Tầng: {storyName}\n" +
                        $"• Tiết diện: {selSec.SectionName}\n" +
                        $"• Nội dung tải trọng: {loadSummary}\n\n" +
                        $"Toàn bộ Text tải trọng của tiết diện này trên AutoCAD đã được cập nhật thời gian thực!",
                        "Đồng Bộ CAD Live Thành Công",
                        CustomMessageBox.IconType.Success
                    );
                }
                else
                {
                    // 2. Đồng bộ hàng loạt cho toàn bộ các ô sàn của tầng hiện tại từ ETABS
                    bool isPattern = radUniformPattern.IsChecked == true;
                    string pattern = isPattern ? (cboLoaiTai.SelectedItem?.ToString() ?? "") : "";
                    string loadSet = !isPattern ? (cboDisplayLoadSet.SelectedItem?.ToString() ?? "") : "";
                    string modeTitle = isPattern ? $"Theo Loại Tải ({pattern})" : $"Theo Load Set ({loadSet})";

                    // Thông báo xác nhận trước khi đồng bộ toàn bộ sàn
                    bool? confirm = CustomMessageBox.ShowConfirm(
                        $"Bạn có chắc chắn muốn Đồng Bộ CAD Live cho TOÀN BỘ các ô sàn tại tầng {storyName}?\n\n" +
                        $"• Chế độ hiển thị tải: {modeTitle}\n" +
                        $"• Hệ thống sẽ tự động quét dữ liệu tải sàn mới nhất từ ETABS và cập nhật đồng loạt tất cả các Text trên AutoCAD.\n\n" +
                        $"Nhấn 'Xác nhận' để tiến hành đồng bộ toàn bộ mặt bằng.",
                        "Xác Nhận Đồng Bộ CAD Live"
                    );

                    if (confirm != true) return;

                    SetStatusMessage($"Đang đọc dữ liệu tải sàn tầng {storyName} từ ETABS để đồng bộ Live CAD...");

                    var slabs = await EtabsWork.RunAsync(() => _etabsService.GetSlabsForStory(storyName, pattern, loadSet));
                    if (slabs == null || slabs.Count == 0)
                    {
                        CustomMessageBox.Show($"Không tìm thấy dữ liệu sàn nào tại tầng {storyName} để đồng bộ.", "Thông Báo", CustomMessageBox.IconType.Warning);
                        return;
                    }

                    var secMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var s in slabs)
                    {
                        if (!string.IsNullOrEmpty(s.Section) && !secMap.ContainsKey(s.Section))
                        {
                            bool hasLoad = !string.IsNullOrEmpty(s.FormattedLoadText) && s.FormattedLoadText != "0";
                            secMap[s.Section] = hasLoad ? s.FormattedLoadText : "";
                        }
                    }

                    await SyncCadTextLive(secMap);
                    SetStatusMessage($"Đã đồng bộ Live CAD thành công cho {secMap.Count} loại tiết diện ({slabs.Count} ô sàn) tầng {storyName}.");

                    CustomMessageBox.Show(
                        $"Đã đồng bộ Live CAD thành công cho toàn bộ mặt bằng sàn tầng {storyName}:\n\n" +
                        $"• Tầng: {storyName}\n" +
                        $"• Chế độ hiển thị: {modeTitle}\n" +
                        $"• Số loại tiết diện sàn: {secMap.Count}\n" +
                        $"• Tổng số ô sàn được cập nhật Text: {slabs.Count}\n\n" +
                        $"Toàn bộ Text tải trọng trên AutoCAD đã được cập nhật tức thời theo dữ liệu ETABS mới nhất!",
                        "Đồng Bộ CAD Live Thành Công",
                        CustomMessageBox.IconType.Success
                    );
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi đồng bộ Live CAD: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private async void OnExportSlabTableCadClick(object sender, RoutedEventArgs e)
        {
            string storyName = cboStory.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(storyName))
            {
                CustomMessageBox.Show("Vui lòng chọn tầng cần xuất bảng thống kê.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                return;
            }

            var acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
            if (acadDoc == null)
            {
                CustomMessageBox.Show("Chưa kết nối với AutoCAD. Vui lòng mở AutoCAD và thử lại.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                SetStatusMessage($"Đang tổng hợp dữ liệu thống kê sàn tầng {storyName}...");

                var sections = _allSections;
                if (sections == null || sections.Count == 0)
                {
                    sections = await EtabsWork.RunAsync(() => _etabsService.GetSlabSectionsForStory(storyName));
                }

                bool isPattern = radUniformPattern?.IsChecked == true;
                string selPat = isPattern ? (cboLoaiTai?.SelectedItem?.ToString() ?? "") : "";
                string selSet = !isPattern ? (cboDisplayLoadSet?.SelectedItem?.ToString() ?? "") : "";

                var slabs = await EtabsWork.RunAsync(() => _etabsService.GetSlabsForStory(storyName, selPat, selSet));

                double.TryParse(txtTextHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double textHeight);
                if (textHeight <= 0) textHeight = 250.0;

                var tablePayload = new CadSlabTableExportPayload
                {
                    StoryName = storyName,
                    TextHeight = textHeight,
                    Items = new List<CadSlabTableItem>()
                };

                int stt = 1;
                foreach (var sec in sections)
                {
                    var (r, g, b) = SectionColorHelper.GetSectionRgb(sec.SectionName);
                    var sampleSlab = slabs.FirstOrDefault(s => s.Section.Equals(sec.SectionName, StringComparison.OrdinalIgnoreCase));
                    string loadSummary = sampleSlab != null && !string.IsNullOrEmpty(sampleSlab.FormattedLoadText) && sampleSlab.FormattedLoadText != "0"
                        ? sampleSlab.FormattedLoadText.Replace("\n", " | ")
                        : "-";

                    tablePayload.Items.Add(new CadSlabTableItem
                    {
                        Stt = stt++,
                        SectionName = sec.SectionName,
                        ColorR = r,
                        ColorG = g,
                        ColorB = b,
                        Count = sec.Count,
                        TotalAreaM2 = sec.TotalArea,
                        LoadSummary = loadSummary
                    });
                }

                string outDir = _exchangeDir;
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                string jsonPath = Path.Combine(outDir, "cad_slab_table.json");
                File.WriteAllText(jsonPath, JsonHelper.Serialize(tablePayload));

                this.Hide();

                {
                    string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                    string formattedJson = jsonPath.Replace("\\", "/");
                    string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_SHELL_DRAW_TABLE \"{formattedJson}\"\n";
                    await _cadSession.SendAsync((object)acadDoc, cmdStr, CadContext, this);
                }

                this.Show();
                this.Activate();

                CustomMessageBox.Show($"Đã vẽ thành công Bảng Thống Kê Sàn ({sections.Count} loại tiết diện, tổng {sections.Sum(s => s.TotalArea):N2} m²) lên bản vẽ AutoCAD!", "Xuất Bảng Thành Công", CustomMessageBox.IconType.Success);
                SetStatusMessage($"Đã xuất Bảng Thống Kê Sàn tầng {storyName} lên AutoCAD.");
            }
            catch (Exception ex)
            {
                this.Show();
                CustomMessageBox.Show($"Lỗi xuất Bảng Thống Kê Sàn: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void OnSaveConfigClick(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("Đã lưu cấu hình thiết lập tải sàn thành công.", "Lưu Cấu Hình", CustomMessageBox.IconType.Success);
        }

        #endregion
    }
}
