using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using MediaColor = System.Windows.Media.Color;
using MediaBrush = System.Windows.Media.SolidColorBrush;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class WallLoadView : Window
    {
        public ObservableCollection<WallLoadRow> WallLoads { get; set; }
        public ObservableCollection<BeamSectionInfo> BeamSections { get; set; } = new ObservableCollection<BeamSectionInfo>();

        private readonly EtabsService _etabsService = new EtabsService();
        private readonly AutoCadIpcService _autocadService = new AutoCadIpcService();

        private bool _isLoading = false;

        public WallLoadView()
        {
            InitializeComponent();
            try
            {
                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/QksExcelAddIn;component/Resources/excel.ico", UriKind.Absolute));
            }
            catch
            {
                EtabsIconHelper.SetIcon(this);
            }
            
            WallLoads = new ObservableCollection<WallLoadRow>
            {
                new WallLoadRow { Type = "100", Load = 1.8 },
                new WallLoadRow { Type = "200", Load = 3.3 },
                new WallLoadRow { Type = "Tường lửng/Lan can", Load = 1.2 },
                new WallLoadRow { Type = "Tường kính", Load = 0.8 }
            };
            
            DataContext = this;
            gridBeamSections.ItemsSource = BeamSections;

            AppConnectionManager.ConnectionChanged += UpdateConnectionUI;
            Loaded += (s, e) =>
            {
                _isLoading = true;
                try
                {
                    UpdateConnectionUI();
                    LoadSavedConfiguration();
                    LoadWallLoadsPreset();
                    UpdateLiveCalculation();
                    UpdateSelectedWallLoadBadge();
                    RefreshBeamSections();

                    if (cboTab2WallLoad != null)
                    {
                        cboTab2WallLoad.ItemsSource = WallLoads;
                        cboTab2WallLoad.DisplayMemberPath = "DisplayText";
                        cboTab2WallLoad.SelectedValuePath = "Load";
                        if (cboTab2WallLoad.SelectedIndex < 0 && WallLoads.Count > 1)
                        {
                            cboTab2WallLoad.SelectedIndex = 1;
                        }
                        else if (cboTab2WallLoad.SelectedIndex < 0 && WallLoads.Count > 0)
                        {
                            cboTab2WallLoad.SelectedIndex = 0;
                        }
                    }
                }
                finally
                {
                    _isLoading = false;
                }
            };
            Closed += (s, e) => AppConnectionManager.ConnectionChanged -= UpdateConnectionUI;
            cboTangUpdate.SelectionChanged += (s, e) => { if (!_isLoading) RefreshBeamSections(); };
        }

        private void UpdateConnectionUI()
        {
            Dispatcher.Invoke(() =>
            {
                if (AppConnectionManager.IsEtabsConnected)
                {
                    dotEtabs.Fill = new MediaBrush(MediaColor.FromRgb(16, 124, 65)); // Dark Green
                    lblEtabsModel.Text = AppConnectionManager.GetEtabsDisplayName();
                    lblEtabsModel.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));

                    try
                    {
                        // 1. Nạp danh sách Tầng từ ETABS
                        string[] storyNames;
                        double[] storyHeights;
                        double[] storyElevations;
                        int count = _etabsService.GetStoryList(out storyNames, out storyHeights, out storyElevations);
                        if (count > 0)
                        {
                            string currentSelected = (cboTangUpdate.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
                            cboTangUpdate.Items.Clear();
                            int selectIdx = 0;
                            for (int i = 0; i < count; i++)
                            {
                                var item = new System.Windows.Controls.ComboBoxItem { Content = storyNames[i] };
                                cboTangUpdate.Items.Add(item);
                                if (storyNames[i].Equals(currentSelected, StringComparison.OrdinalIgnoreCase))
                                {
                                    selectIdx = i;
                                }
                            }
                            cboTangUpdate.SelectedIndex = selectIdx;
                        }

                        // 2. Nạp danh sách Load Patterns từ mô hình ETABS
                        string[] patternNames;
                        int patCount = _etabsService.GetLoadPatternList(out patternNames);
                        if (patCount > 0)
                        {
                            string currentPat = (cboLoaiTai.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
                            cboLoaiTai.Items.Clear();
                            int selectPatIdx = 0;
                            for (int i = 0; i < patCount; i++)
                            {
                                var item = new System.Windows.Controls.ComboBoxItem { Content = patternNames[i] };
                                cboLoaiTai.Items.Add(item);
                                if (patternNames[i].Equals(currentPat, StringComparison.OrdinalIgnoreCase))
                                {
                                    selectPatIdx = i;
                                }
                                else if (string.IsNullOrEmpty(currentPat) && 
                                    (patternNames[i].Equals("BW", StringComparison.OrdinalIgnoreCase) || 
                                     patternNames[i].Equals("TUONG", StringComparison.OrdinalIgnoreCase) || 
                                     patternNames[i].Equals("WALL", StringComparison.OrdinalIgnoreCase)))
                                {
                                    selectPatIdx = i;
                                }
                            }
                            cboLoaiTai.SelectedIndex = selectPatIdx;
                        }
                    }
                    catch
                    {
                        // Ignore if ETABS busy
                    }
                }
                else
                {
                    dotEtabs.Fill = new MediaBrush(MediaColor.FromRgb(183, 28, 28)); // Dark Red
                    lblEtabsModel.Text = "Chưa kết nối (Kết Nối API từ Ribbon)";
                    lblEtabsModel.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                }

                if (AppConnectionManager.IsAutoCadConnected)
                {
                    dotCad.Fill = new MediaBrush(MediaColor.FromRgb(16, 124, 65)); // Dark Green
                    lblCadDrawing.Text = AppConnectionManager.GetAutoCadDisplayName();
                    lblCadDrawing.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                }
                else
                {
                    // Tự động phát hiện nếu có bản vẽ AutoCAD đang mở trên máy
                    var autoDocs = AutoCadHelper.GetRunningAutoCadDocuments();
                    if (autoDocs.Count > 0)
                    {
                        AppConnectionManager.ActiveAutoCad = autoDocs[0];
                        dotCad.Fill = new MediaBrush(MediaColor.FromRgb(16, 124, 65)); // Dark Green
                        lblCadDrawing.Text = autoDocs[0].FileName;
                        lblCadDrawing.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                    }
                    else
                    {
                        dotCad.Fill = new MediaBrush(MediaColor.FromRgb(183, 28, 28)); // Dark Red
                        lblCadDrawing.Text = "Chưa kết nối (Kết Nối API từ Ribbon)";
                        lblCadDrawing.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                    }
                }
            });
        }

        private async void OnGanTaiClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kiểm tra kết nối ETABS từ AppConnectionManager
                if (!AppConnectionManager.IsEtabsConnected)
                {
                    CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 2. Kiểm tra trạng thái khóa của mô hình
                if (_etabsService.IsModelLocked())
                {
                    CustomMessageBox.Show("Mô hình ETABS hiện tại đang bị khóa (Locked). Vui lòng mở khóa mô hình trong ETABS trước khi gán tải.", 
                        "Mô Hình Bị Khóa", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 3. Lấy các thông số từ giao diện người dùng (UI)
                string storyName = GetCboText(cboTangUpdate, "Base");
                if (string.IsNullOrEmpty(storyName))
                {
                    CustomMessageBox.Show("Vui lòng chọn Tầng Update bên thanh điều khiển trước khi gán tải.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                    return;
                }

                double klrGach = double.TryParse(txtKlrGach.Text, out double kg) ? kg : 15.0; // kN/m3
                double klrVua = double.TryParse(txtKlrVua.Text, out double kv) ? kv : 18.0;   // kN/m3
                double dayTrat = double.TryParse(txtDayTrat.Text, out double dt) ? dt : 20.0; // mm
                double heSoGan = double.TryParse(txtHeSoGan.Text, out double hsg) ? hsg : 1.0;
                string loaiTai = GetCboText(cboLoaiTai, "WALL");
                string directionName = GetCboText(cboHuongTai, "Gravity");
                int dirCode = EtabsService.GetDirectionCode(directionName);
                bool replaceLoads = GetCboText(cboAddReplace, "Replace").Equals("Replace", StringComparison.OrdinalIgnoreCase);
                
                // Lấy BasePoint
                double baseX = double.TryParse(txtBasePointX.Text, out double bx) ? bx : 0;
                double baseY = double.TryParse(txtBasePointY.Text, out double by) ? by : 0;

                // 4. Đọc dữ liệu Line tường từ AutoCAD (Hybrid Mode)
                List<AutoCadWallLine> wallLines = new List<AutoCadWallLine>();
                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();

                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Không thể kết nối AutoCAD. Vui lòng mở AutoCAD và chọn bản vẽ qua Cấu hình API.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                    return;
                }

                string linesFile = Path.Combine(AutoCadHelper.LoadsDir, "cad_wall_lines.json");
                if (File.Exists(linesFile))
                {
                    try { File.Delete(linesFile); } catch { }
                }

                string formattedDll = AutoCadHelper.DllPath.Replace(@"\", "/");

                if (radMethodPickPoints.IsChecked == true)
                {
                    this.Hide();
                    string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_PICK_LOAD\n";
                    AutoCadHelper.SendCommand(acadDoc, cmdStr);

                    // Chờ file cad_wall_lines.json được ghi từ AutoCAD với polling tốc độ cao (15ms)
                    int maxWaitMs = 120000; // Tối đa 120s
                    int elapsed = 0;
                    while (!File.Exists(linesFile) && elapsed < maxWaitMs)
                    {
                        System.Threading.Thread.Sleep(15);
                        elapsed += 15;
                    }
                    this.Show();

                    if (File.Exists(linesFile))
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(linesFile);
                            var queryRes = JsonHelper.Deserialize<AutoCadWallQuery>(jsonContent);
                            if (queryRes?.Lines != null)
                            {
                                wallLines = queryRes.Lines;
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    this.Hide();
                    string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_GET_LINES\n";
                    AutoCadHelper.SendCommand(acadDoc, cmdStr);

                    int maxWaitMs = 120000;
                    int elapsed = 0;
                    while (!File.Exists(linesFile) && elapsed < maxWaitMs)
                    {
                        System.Threading.Thread.Sleep(15);
                        elapsed += 15;
                    }
                    this.Show();

                    if (File.Exists(linesFile))
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(linesFile);
                            var queryRes = JsonHelper.Deserialize<AutoCadWallQuery>(jsonContent);
                            if (queryRes?.Lines != null)
                            {
                                wallLines = queryRes.Lines;
                            }
                        }
                        catch { }
                    }
                }

                if (wallLines.Count == 0)
                {
                    string msg = radMethodPickPoints.IsChecked == true 
                        ? "Hủy thao tác pick điểm hoặc không thể lấy điểm pick từ AutoCAD." 
                        : "Không tìm thấy đường line tường nào được chọn trong AutoCAD.";
                    CustomMessageBox.Show(msg, "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 5. Chuẩn bị môi trường ETABS và KHÓA CHẶT TẦNG CẦN GÁN (STORY BOUNDARY LOCK)
                _etabsService.EnsureStandardUnits();
                _etabsService.EnsureLoadPatternExists(loaiTai);
                string upperStoryName = "";
                double storyHeightVal = 3.4;
                bool hasUpperStory = _etabsService.GetUpperStoryInfo(storyName, out upperStoryName, out storyHeightVal);

                List<EtabsBeam>? floorBeams = _etabsService.GetCachedBeamsForStory(storyName);
                if (floorBeams == null || floorBeams.Count == 0)
                {
                    floorBeams = _etabsService.GetBeamsForStory(storyName);
                }

                if (floorBeams == null || floorBeams.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy dầm nào thuộc tầng '{storyName}' trong mô hình ETABS.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                var floorBeamsDict = floorBeams.ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);

                // Đọc cache exported beams nếu có từ wall_load_export.json để đối chiếu chính xác tọa độ CAD
                List<CadBeamLoadExportItem> exportedBeamsCache = new List<CadBeamLoadExportItem>();
                string exportJsonPath = Path.Combine(AutoCadHelper.LoadsDir, "wall_load_export.json");
                if (File.Exists(exportJsonPath))
                {
                    try
                    {
                        string json = File.ReadAllText(exportJsonPath);
                        var expPayload = JsonHelper.Deserialize<CadExportPayload>(json);
                        if (expPayload?.Beams != null && expPayload.Beams.Count > 0)
                        {
                            exportedBeamsCache = expPayload.Beams;
                        }
                    }
                    catch { }
                }

                // 6. Tính toán tải trọng và lập danh sách dầm chờ xác nhận
                int failCount = 0;
                double lastWidth = 200, lastWeight = 3.3, lastReduction = 1.0, lastFinalLoad = 9.9;
                string lastWallTypeName = "Tường 200";

                var candidateTasks = new List<WallLoadTask>();
                var processedFullBeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processedSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in wallLines)
                {
                    EtabsBeam? matchedBeam = null;

                    // Tọa độ đoạn line chuyển đổi về hệ mét ETABS sau khi khử BasePoint
                    double etabsX1 = (line.X1 - baseX) / 1000.0;
                    double etabsY1 = (line.Y1 - baseY) / 1000.0;
                    double etabsX2 = (line.X2 - baseX) / 1000.0;
                    double etabsY2 = (line.Y2 - baseY) / 1000.0;

                    // 1. Khớp siêu tốc theo BeamName có sẵn từ XData (CHỈ CHẤP NHẬN NẾU THUỘC TẦNG ĐANG CHỌN)
                    if (!string.IsNullOrEmpty(line.BeamName))
                    {
                        string cleanName = line.BeamName.Split('|')[0].Trim();
                        if (floorBeamsDict.TryGetValue(cleanName, out var fb))
                        {
                            matchedBeam = fb;
                        }
                    }

                    // 2. Khớp theo tọa độ CAD (mm) với danh sách dầm đã xuất (exportedBeamsCache)
                    if (matchedBeam == null && exportedBeamsCache.Count > 0)
                    {
                        double bestScore = double.MaxValue;
                        string bestExpName = "";

                        foreach (var expBeam in exportedBeamsCache)
                        {
                            double score = GeometryHelper.CalculateBeamMatchingScore(
                                line.X1, line.Y1, line.X2, line.Y2,
                                expBeam.X1, expBeam.Y1, expBeam.X2, expBeam.Y2,
                                maxPerpDist: 500.0
                            );

                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestExpName = expBeam.BeamName;
                            }
                        }

                        if (!string.IsNullOrEmpty(bestExpName) && bestScore < 10000.0)
                        {
                            string cleanExp = bestExpName.Split('|')[0].Trim();
                            if (floorBeamsDict.TryGetValue(cleanExp, out var fbExp))
                            {
                                matchedBeam = fbExp;
                            }
                        }
                    }

                    // 3. Khớp theo hệ tọa độ ETABS (m) sau khi khử BasePoint - CHỈ TÌM TRONG TẦNG ĐANG CHỌN (floorBeams)
                    if (matchedBeam == null)
                    {
                        double bestScore = double.MaxValue;

                        foreach (var beam in floorBeams)
                        {
                            double score = GeometryHelper.CalculateBeamMatchingScore(
                                etabsX1, etabsY1, etabsX2, etabsY2,
                                beam.X1, beam.Y1, beam.X2, beam.Y2,
                                maxPerpDist: 0.50
                            );

                            if (score < bestScore)
                            {
                                bestScore = score;
                                matchedBeam = beam;
                            }
                        }

                        if (bestScore >= 10000.0)
                        {
                            matchedBeam = null;
                        }
                    }

                    if (matchedBeam != null)
                    {
                        double wallWeightPerM2 = 0;
                        double thicknessMm = 200;
                        double finalLoad = 0;
                        double beamDepth = 0.0;
                        string currentWallTypeName = "Tường 200";

                        // Lấy chiều cao dầm: Tự nhập hoặc tự động truy xét dầm tầng trên theo tọa độ 3D (X, Y, Z)
                        if (chkCustomBeamDepth.IsChecked == true && double.TryParse(txtCustomBeamDepth.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double customDepth))
                        {
                            beamDepth = customDepth;
                        }
                        else
                        {
                            if (!hasUpperStory || string.IsNullOrEmpty(upperStoryName))
                            {
                                beamDepth = 0.0;
                            }
                            else
                            {
                                var upperBeam = _etabsService.FindUpperBeam(matchedBeam, upperStoryName);
                                if (upperBeam != null)
                                {
                                    beamDepth = _etabsService.GetBeamDepth(upperBeam.Name);
                                }
                                else
                                {
                                    beamDepth = 0.0;
                                }
                            }
                        }

                        double slabDepth = (chkTruSan?.IsChecked == true && double.TryParse(txtBeDaySan?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sd)) ? sd : 0.0;

                        double clearHeight = 0.0;
                        string slabDisplay = "0 mm";
                        if (beamDepth > 0.001)
                        {
                            clearHeight = Math.Max(0.1, storyHeightVal - beamDepth);
                            slabDisplay = "Đã gồm trong dầm";
                        }
                        else
                        {
                            clearHeight = Math.Max(0.1, storyHeightVal - slabDepth);
                            slabDisplay = slabDepth > 0.001 ? $"{slabDepth * 1000:F0} mm" : "Không trừ";
                        }

                        double heSoCuaVal = 1.0;

                        if (radTinhTuDong.IsChecked == true)
                        {
                            thicknessMm = radTuong100.IsChecked == true ? 100 : 200;
                            currentWallTypeName = radTuong100.IsChecked == true ? "Tường 100" : "Tường 200";
                            double tTuongM = thicknessMm / 1000.0;
                            double tTratM = dayTrat / 1000.0;

                            double heSoGach = double.TryParse(txtHeSoGach.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double n1) ? n1 : 1.1;
                            double heSoVua = double.TryParse(txtHeSoVua.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double n2) ? n2 : 1.3;
                            
                            heSoCuaVal = (chkHeSoCua?.IsChecked == true && double.TryParse(txtHeSoCua.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double n3)) ? n3 : 1.0;

                            wallWeightPerM2 = ((klrGach * heSoGach * tTuongM) + (klrVua * heSoVua * tTratM)) * heSoCuaVal;
                            finalLoad = Math.Round(wallWeightPerM2 * clearHeight * heSoGan, 2);
                        }
                        else
                        {
                            string selectedType = "200";
                            double foundLoad = 3.3;

                            if (cboChonTuongGrid.SelectedItem is WallLoadRow row)
                            {
                                selectedType = row.Type;
                                foundLoad = row.Load;
                            }
                            else if (cboChonTuongGrid.SelectedItem is System.Windows.Controls.ComboBoxItem cbi)
                            {
                                selectedType = cbi.Content?.ToString() ?? "200";
                                var match = WallLoads.FirstOrDefault(w => w.Type.Equals(selectedType, StringComparison.OrdinalIgnoreCase));
                                if (match != null) foundLoad = match.Load;
                            }
                            else if (!string.IsNullOrEmpty(cboChonTuongGrid.Text))
                            {
                                selectedType = cboChonTuongGrid.Text.Trim();
                                var match = WallLoads.FirstOrDefault(w => w.Type.Equals(selectedType, StringComparison.OrdinalIgnoreCase));
                                if (match != null) foundLoad = match.Load;
                            }

                            currentWallTypeName = selectedType;
                            thicknessMm = double.TryParse(selectedType, out double th) ? th : (selectedType.Contains("100") ? 100 : 200);
                            wallWeightPerM2 = foundLoad;
                            finalLoad = Math.Round(foundLoad * heSoGan, 2);
                        }

                        // Tính toán độ dài phân đoạn chiếu lên trục dầm
                        double beamDx = matchedBeam.X2 - matchedBeam.X1;
                        double beamDy = matchedBeam.Y2 - matchedBeam.Y1;
                        double beamLen = Math.Sqrt(beamDx * beamDx + beamDy * beamDy);

                        bool isFull = true;
                        double d1 = 0;
                        double d2 = beamLen;

                        if (beamLen > 0.001)
                        {
                            double t1 = ((etabsX1 - matchedBeam.X1) * beamDx + (etabsY1 - matchedBeam.Y1) * beamDy) / beamLen;
                            double t2 = ((etabsX2 - matchedBeam.X1) * beamDx + (etabsY2 - matchedBeam.Y1) * beamDy) / beamLen;

                            d1 = Math.Max(0.0, Math.Min(t1, t2));
                            d2 = Math.Min(beamLen, Math.Max(t1, t2));

                            if ((d2 - d1) >= 0.85 * beamLen || (d1 < 0.1 && d2 > beamLen - 0.1) || radMethodSelectLines.IsChecked == true || radChonTuongGrid.IsChecked == true)
                            {
                                isFull = true;
                            }
                            else
                            {
                                isFull = false;
                            }
                        }

                        // Chống gán trùng lặp dầm (Deduplication) khi quét vùng chọn chứa nhiều đường
                        if (isFull)
                        {
                            if (processedFullBeams.Contains(matchedBeam.Name))
                            {
                                continue;
                            }
                            processedFullBeams.Add(matchedBeam.Name);
                        }
                        else
                        {
                            string segKey = $"{matchedBeam.Name}_{d1:F2}_{d2:F2}";
                            if (processedSegments.Contains(segKey))
                            {
                                continue;
                            }
                            processedSegments.Add(segKey);
                        }

                        lastWidth = thicknessMm;
                        lastWeight = wallWeightPerM2;
                        lastReduction = heSoGan;
                        lastFinalLoad = finalLoad;
                        lastWallTypeName = currentWallTypeName;

                        var reportItem = new WallLoadAssignedItem
                        {
                            Stt = candidateTasks.Count + 1,
                            BeamName = matchedBeam.Name,
                            Section = matchedBeam.Section,
                            StoryHeight = storyHeightVal,
                            UpperBeamDepth = beamDepth,
                            SlabDepth = slabDepth,
                            SlabText = slabDisplay,
                            ClearHeight = Math.Round(clearHeight, 2),
                            DoorCoeff = heSoCuaVal,
                            FinalLoad = finalLoad
                        };

                        candidateTasks.Add(new WallLoadTask
                        {
                            Beam = matchedBeam,
                            IsFullBeam = isFull,
                            D1 = Math.Round(d1, 3),
                            D2 = Math.Round(d2, 3),
                            FinalLoad = finalLoad,
                            ReportItem = reportItem
                        });
                    }
                    else
                    {
                        failCount++;
                    }
                }

                if (candidateTasks.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy dầm nào trên tầng '{storyName}' khớp với các đường chọn trên CAD.\n(Số đối tượng không khớp: {failCount})", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 7. HIỂN THỊ HỘP THOẠI XÁC NHẬN (CONFIRMATION DIALOG) TRƯỚC KHI GÁN VÀO ETABS
                bool isGridMode = radChonTuongGrid.IsChecked == true;
                var assignedBeamsList = candidateTasks.Select(t => t.ReportItem).ToList();

                var confirmDialog = new NotificationView(
                    assignedBeamsList,
                    lastWallTypeName,
                    lastWeight,
                    lastReduction,
                    isGridMode: isGridMode,
                    isConfirmMode: true,
                    storyName: storyName
                );
                confirmDialog.Owner = this;
                bool? isConfirmed = confirmDialog.ShowDialog();

                if (isConfirmed != true)
                {
                    SetStatusMessage("Đã hủy thao tác gán tải vào ETABS.", false);
                    return;
                }

                // 8. NGƯỜI DÙNG ĐÃ XÁC NHẬN -> TIẾN HÀNH GÁN TẢI VÀO ETABS
                int successCount = 0;
                foreach (var task in candidateTasks)
                {
                    bool ok = false;
                    if (task.IsFullBeam)
                    {
                        ok = _etabsService.AssignDistributedLoad(task.Beam.Name, loaiTai, dirCode, task.FinalLoad, replaceLoads);
                    }
                    else
                    {
                        ok = _etabsService.AssignSubSegmentDistributedLoad(task.Beam.Name, loaiTai, dirCode, task.D1, task.D2, task.FinalLoad, replaceLoads);
                    }

                    if (ok) successCount++;
                }

                // Làm mới cửa sổ ETABS
                _etabsService.RefreshView();

                SetStatusMessage($"Đã gán thành công {successCount}/{candidateTasks.Count} dầm vào tầng {storyName} trong ETABS.", true);
                CustomMessageBox.Show($"Đã gán thành công {successCount} dầm tại tầng '{storyName}' vào mô hình ETABS!", "Gán Tải Hoàn Tất", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Có lỗi xảy ra trong quá trình gán tải tường: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void OnPickBasePointClick(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Chưa kết nối AutoCAD. Vui lòng mở AutoCAD và chọn bản vẽ qua Cấu hình API.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                    return;
                }

                this.Hide();
                double[]? point = null;
                try
                {
                    point = AutoCadHelper.AcadGetPoint(acadDoc, "\n[QKS Loads] Vui lòng pick điểm gốc BasePoint trên AutoCAD: ");
                }
                finally
                {
                    this.Show();
                    this.Activate();
                }

                if (point != null && point.Length >= 2)
                {
                    txtBasePointX.Text = point[0].ToString("F4", CultureInfo.InvariantCulture);
                    txtBasePointY.Text = point[1].ToString("F4", CultureInfo.InvariantCulture);

                    CustomMessageBox.Show($"Đã lấy tọa độ BasePoint từ AutoCAD:\nX = {txtBasePointX.Text}\nY = {txtBasePointY.Text}", "Thành Công", CustomMessageBox.IconType.Success);
                }
            }
            catch (Exception ex)
            {
                this.Show();
                CustomMessageBox.Show($"Lỗi pick điểm BasePoint: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void OnXuatAutoCadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kiểm tra kết nối ETABS
                if (!AppConnectionManager.IsEtabsConnected)
                {
                    CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                    return;
                }

                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Chưa kết nối với bản vẽ AutoCAD. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập bản vẽ.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 2. Lấy thông số từ UI
                string storyName = GetCboText(cboTangUpdate, "Base");
                string loadPattern = GetCboText(cboLoaiTai, "WALL");
                string directionName = GetCboText(cboHuongTai, "Gravity");
                int dirCode = EtabsService.GetDirectionCode(directionName);

                double baseX = double.TryParse(txtBasePointX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double bx) ? bx : 0;
                double baseY = double.TryParse(txtBasePointY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double by) ? by : 0;

                // 3. Đọc cấu hình hiển thị xuất CAD
                double textHeight = double.TryParse(txtCadTextHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double th) ? th : 200.0;
                int textColorIdx = GetCadColorIndex(cboCadTextColor, 1); // 1 = Red
                int beamColorIdx = 8;

                var visualSettings = new CadExportVisualSettings
                {
                    TextHeight = textHeight,
                    TextColorIndex = textColorIdx,
                    BeamColorIndex = beamColorIdx,
                    DotColorIndex = 2,
                    DotRadius = 40.0,
                    UseSectionRgbColor = true
                };

                // 4. Lấy danh sách dầm: Ưu tiên quét chọn trong ETABS, fallback theo tầng
                bool isSelectedMode = false;
                var exportBeams = _etabsService.GetSelectedOrStoryBeamsWithDistributedLoads(storyName, loadPattern, dirCode, baseX, baseY, out isSelectedMode);

                if (exportBeams.Count == 0)
                {
                    CustomMessageBox.Show("Không tìm thấy dầm nào được quét chọn trong ETABS (hoặc tầng hiện tại không có dầm).\n\n• Vui lòng quét chọn các cấu kiện dầm trên màn hình ETABS trước khi xuất.", "Chưa Chọn Dầm Trong ETABS", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 5. Lưu dữ liệu sang file JSON tại C:\QKS_JSON_Data\Loads\wall_load_export.json
                var payload = new CadExportPayload
                {
                    Action = "EXPORT_WALL_LOADS_TO_CAD",
                    Story = isSelectedMode ? "Selected in ETABS" : storyName,
                    LoadPattern = loadPattern,
                    DirectionName = directionName,
                    DirectionCode = dirCode,
                    UnitLabel = _etabsService.GetUnitLabel(),
                    BasePointX = baseX,
                    BasePointY = baseY,
                    Settings = visualSettings,
                    Beams = exportBeams
                };

                if (!Directory.Exists(AutoCadHelper.LoadsDir))
                {
                    Directory.CreateDirectory(AutoCadHelper.LoadsDir);
                }

                string jsonPath = Path.Combine(AutoCadHelper.LoadsDir, "wall_load_export.json");

                // 6. Ẩn form và cho người dùng click chọn điểm dán mặt bằng trên CAD
                this.Hide();
                double[]? pastePoint = AutoCadHelper.AcadGetPoint(acadDoc, "\n[QKS Loads] Chọn gốc tọa độ cần dán mặt bằng dầm trên AutoCAD: ");
                
                if (pastePoint == null || pastePoint.Length < 2)
                {
                    this.Show();
                    return;
                }

                // Không bắt buộc chọn Legend cùng lúc (tách riêng thành nút Xuất Bảng Tiết Diện)
                payload.DrawLegend = false;

                // Ghi file JSON với đầy đủ thông tin BasePoint
                string jsonString = JsonHelper.Serialize(payload);
                File.WriteAllText(jsonPath, jsonString, System.Text.Encoding.UTF8);

                // 7. Gửi lệnh In-Process sang AutoCAD Plugin C# - Vẽ ngay lập tức mặt bằng dầm
                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string formattedJson = jsonPath.Replace("\\", "/");
                string pasteXStr = pastePoint[0].ToString(CultureInfo.InvariantCulture);
                string pasteYStr = pastePoint[1].ToString(CultureInfo.InvariantCulture);
                string textHeightStr = textHeight.ToString(CultureInfo.InvariantCulture);

                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_DRAW_BEAMS \"{formattedJson}\" {pasteXStr} {pasteYStr} {textHeightStr} {textColorIdx} {beamColorIdx}\n";
                AutoCadHelper.SendCommand(acadDoc, cmdStr);

                this.Show();

                int totalSegments = 0;
                foreach (var b in exportBeams)
                {
                    totalSegments += b.Segments.Count;
                }

                string sourceText = isSelectedMode ? "Quét chọn trực tiếp trong ETABS" : $"Theo tầng: {storyName}";

                CustomMessageBox.Show(
                    $"Đã xuất thành công mặt bằng tải dầm sang AutoCAD:\n\n" +
                    $"• Nguồn dầm: {sourceText}\n" +
                    $"• Trường hợp tải: {loadPattern} ({directionName})\n" +
                    $"• Số lượng dầm: {exportBeams.Count}\n" +
                    $"• Số phân đoạn tải: {totalSegments}\n" +
                    $"• Chế độ hiển thị: Line chia tải (100mm) theo chuẩn ETABS & TrueColor RGB", 
                    "Xuất CAD Thành Công", 
                    CustomMessageBox.IconType.Success
                );
            }
            catch (Exception ex)
            {
                this.Show();
                CustomMessageBox.Show($"Có lỗi xảy ra khi xuất dầm sang AutoCAD: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        /// <summary>
        /// Tự động nạp và cập nhật danh mục tiết diện dầm từ ETABS hoặc Cache
        /// </summary>
        public async void RefreshBeamSections(string? targetStory = null)
        {
            try
            {
                string story = targetStory ?? GetCboText(cboTangUpdate, "Base");
                if (string.IsNullOrWhiteSpace(story)) story = "Base";

                string loadPat = GetCboText(cboLoaiTai, "WALL");
                string dirName = GetCboText(cboHuongTai, "Gravity");
                int dirCode = EtabsService.GetDirectionCode(dirName);
                double bx = double.TryParse(txtBasePointX?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double x) ? x : 0;
                double by = double.TryParse(txtBasePointY?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double y) ? y : 0;

                txtCadSectionSummary.Text = "Đang nạp tiết diện dầm...";

                List<CadBeamLoadExportItem> beams = await System.Threading.Tasks.Task.Run(() =>
                {
                    List<CadBeamLoadExportItem> list = new List<CadBeamLoadExportItem>();
                    if (AppConnectionManager.IsEtabsConnected)
                    {
                        try
                        {
                            bool isSel = false;
                            list = _etabsService.GetSelectedOrStoryBeamsWithDistributedLoads(story, loadPat, dirCode, bx, by, out isSel);
                        }
                        catch { }
                    }

                    if (list.Count == 0)
                    {
                        string jsonPath = Path.Combine(AutoCadHelper.LoadsDir, "wall_load_export.json");
                        if (File.Exists(jsonPath))
                        {
                            try
                            {
                                string cached = File.ReadAllText(jsonPath);
                                var payload = JsonHelper.Deserialize<CadExportPayload>(cached);
                                if (payload?.Beams != null && payload.Beams.Count > 0)
                                {
                                    list = payload.Beams;
                                }
                            }
                            catch { }
                        }
                    }
                    return list;
                });

                BeamSections.Clear();

                if (beams.Count > 0)
                {
                    var groups = beams
                        .Where(b => !string.IsNullOrWhiteSpace(b.Section))
                        .GroupBy(b => b.Section.Trim(), StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.Key);

                    int totalDams = 0;
                    foreach (var g in groups)
                    {
                        string secName = g.Key;
                        int count = g.Count();
                        double totalLen = g.Sum(b => b.Length);
                        totalDams += count;

                        var rgb = SectionColorHelper.GetSectionRgb(secName);
                        BeamSections.Add(new BeamSectionInfo
                        {
                            SectionName = secName,
                            Count = count,
                            TotalLength = totalLen,
                            ColorR = rgb.R,
                            ColorG = rgb.G,
                            ColorB = rgb.B
                        });
                    }

                    txtCadSectionSummary.Text = $"Tổng: {BeamSections.Count} loại tiết diện ({totalDams} dầm)";
                }
                else
                {
                    // Mẫu hiển thị trực quan khi chưa có dữ liệu nạp từ ETABS
                    var sampleSections = new[]
                    {
                        ("B25x50", 18, 84.6),
                        ("B20x35", 12, 48.2),
                        ("B30x60", 6, 36.0),
                        ("B20x40", 8, 32.8),
                        ("SB15x30", 5, 16.5)
                    };

                    int totalDams = 0;
                    foreach (var s in sampleSections)
                    {
                        var rgb = SectionColorHelper.GetSectionRgb(s.Item1);
                        BeamSections.Add(new BeamSectionInfo
                        {
                            SectionName = s.Item1,
                            Count = s.Item2,
                            TotalLength = s.Item3,
                            ColorR = rgb.R,
                            ColorG = rgb.G,
                            ColorB = rgb.B
                        });
                        totalDams += s.Item2;
                    }
                    txtCadSectionSummary.Text = $"Mẫu: {BeamSections.Count} loại tiết diện ({totalDams} dầm)";
                }
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Lỗi cập nhật tiết diện dầm: {ex.Message}");
            }
        }

        private void OnGridBeamSectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridBeamSections.SelectedItem is BeamSectionInfo sec)
            {
                HighlightSectionInAutoCad(sec.SectionName, sec.ColorHex);
            }
        }

        private void OnSelectSectionOnCadButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is BeamSectionInfo sec)
            {
                gridBeamSections.SelectedItem = sec;
                HighlightSectionInAutoCad(sec.SectionName, sec.ColorHex);
            }
        }

        public void HighlightSectionInAutoCad(string sectionName, string colorHex)
        {
            try
            {
                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    SetStatusMessage("Chưa kết nối AutoCAD. Vui lòng mở AutoCAD để chọn dầm.");
                    return;
                }

                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_SELECT_SECTION \"{sectionName}\"\n";
                AutoCadHelper.SendCommand(acadDoc, cmdStr);

                txtCadSectionStatus.Text = $"[AutoCAD Sync] Đã quét chọn dầm tiết diện {sectionName} ({colorHex}) trên bản vẽ CAD!";
                SetStatusMessage($"Đã phát lệnh chọn dầm tiết diện '{sectionName}' sang AutoCAD.");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Lỗi chọn dầm trên CAD: {ex.Message}");
            }
        }

        private void OnBoChonCadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                gridBeamSections.SelectedItem = null;
                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc != null)
                {
                    string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                    string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_CLEAR_SELECTION\n";
                    AutoCadHelper.SendCommand(acadDoc, cmdStr);
                }

                txtCadSectionStatus.Text = "💡 Đã hủy chọn trên AutoCAD. Nhấp vào bất kỳ hàng tiết diện nào để chọn lại.";
                SetStatusMessage("Đã gửi lệnh hủy chọn (Deselect / ESC) sang AutoCAD.");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Lỗi hủy chọn trên CAD: {ex.Message}");
            }
        }

        private void OnGanTaiDinhMucTab2Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kiểm tra kết nối ETABS
                if (!AppConnectionManager.IsEtabsConnected)
                {
                    CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                    return;
                }

                if (_etabsService.IsModelLocked())
                {
                    CustomMessageBox.Show("Mô hình ETABS đang bị khóa (Locked). Vui lòng Unlock mô hình trước khi gán tải.", "Mô Hình Bị Khóa", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 2. Lấy loại tường & giá trị tải định mức đã chọn
                WallLoadRow? selectedRow = cboTab2WallLoad.SelectedItem as WallLoadRow;
                if (selectedRow == null && WallLoads.Count > 0)
                {
                    selectedRow = WallLoads[0];
                }

                string wallTypeName = selectedRow?.Type ?? "200";
                double wallLoadValue = selectedRow?.Load ?? 3.3;

                // 3. Đọc các thiết lập chung từ thanh Sidebar - Khóa chặt theo tầng (storyName)
                string storyName = GetCboText(cboTangUpdate, "Base");
                if (string.IsNullOrEmpty(storyName))
                {
                    CustomMessageBox.Show("Vui lòng chọn Tầng Update bên thanh điều khiển trước khi gán tải.", "Chưa Chọn Tầng", CustomMessageBox.IconType.Warning);
                    return;
                }

                string loaiTai = GetCboText(cboLoaiTai, "WALL");
                string huongTai = GetCboText(cboHuongTai, "Gravity");
                int dirCode = EtabsService.GetDirectionCode(huongTai);
                bool replaceLoads = GetCboText(cboAddReplace, "Replace").Equals("Replace", StringComparison.OrdinalIgnoreCase);
                double heSoGan = double.TryParse(txtHeSoGan?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double hsg) ? hsg : 1.0;
                double finalLoad = Math.Round(wallLoadValue * heSoGan, 2);

                _etabsService.EnsureStandardUnits();
                _etabsService.EnsureLoadPatternExists(loaiTai);

                List<EtabsBeam>? floorBeams = _etabsService.GetCachedBeamsForStory(storyName);
                if (floorBeams == null || floorBeams.Count == 0)
                {
                    floorBeams = _etabsService.GetBeamsForStory(storyName);
                }

                if (floorBeams == null || floorBeams.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy dầm nào thuộc tầng '{storyName}' trong mô hình ETABS.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                var floorBeamsDict = floorBeams.ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);

                // =========================================================================
                // TRƯỜNG HỢP 1: Đã chọn 1 hàng Tiết Diện trên Grid
                // =========================================================================
                BeamSectionInfo? selectedSec = gridBeamSections.SelectedItem as BeamSectionInfo;
                if (selectedSec != null && !string.IsNullOrWhiteSpace(selectedSec.SectionName))
                {
                    string targetSection = selectedSec.SectionName.Trim();
                    var targetBeams = floorBeams.Where(b => string.Equals(b.Section?.Trim(), targetSection, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (targetBeams == null || targetBeams.Count == 0)
                    {
                        CustomMessageBox.Show($"Không tìm thấy dầm nào mang tiết diện '{targetSection}' tại tầng {storyName} trong ETABS.", "Không Tìm Thấy Dầm", CustomMessageBox.IconType.Warning);
                        return;
                    }

                    var assignedBeamsList = targetBeams.Select((b, idx) => new WallLoadAssignedItem
                    {
                        Stt = idx + 1,
                        BeamName = b.Name,
                        Section = b.Section,
                        StoryHeight = 0,
                        FinalLoad = finalLoad
                    }).ToList();

                    // Xác nhận trước khi gán
                    var confirmDialog = new NotificationView(
                        assignedBeamsList,
                        wallTypeName,
                        wallLoadValue,
                        heSoGan,
                        isGridMode: true,
                        isConfirmMode: true,
                        storyName: storyName
                    );
                    confirmDialog.Owner = this;
                    if (confirmDialog.ShowDialog() != true)
                    {
                        SetStatusMessage("Đã hủy thao tác gán tải vào ETABS.", false);
                        return;
                    }

                    int successCount = 0;
                    foreach (var beam in targetBeams)
                    {
                        bool ok = _etabsService.AssignDistributedLoad(beam.Name, loaiTai, dirCode, finalLoad, replaceLoads);
                        if (ok) successCount++;
                    }

                    _etabsService.RefreshView();

                    txtCadSectionStatus.Text = $"💡 [Hoàn tất] Đã gán tải định mức {finalLoad:F2} kN/m ({wallTypeName}) cho {successCount} dầm tiết diện '{targetSection}' tại tầng {storyName}.";
                    SetStatusMessage($"Đã gán tải định mức {finalLoad:F2} kN/m cho {successCount} dầm tiết diện '{targetSection}'.", true);
                    CustomMessageBox.Show($"Đã gán thành công {successCount} dầm tiết diện '{targetSection}' tại tầng '{storyName}' vào ETABS!", "Gán Tải Hoàn Tất", CustomMessageBox.IconType.Success);
                    return;
                }

                // =========================================================================
                // TRƯỜNG HỢP 2: Quét chọn dầm trên AutoCAD
                // =========================================================================
                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Vui lòng chọn 1 hàng tiết diện trên bảng hoặc kết nối AutoCAD để gán tải.", "Chưa Chọn Tiết Diện", CustomMessageBox.IconType.Warning);
                    return;
                }

                double baseX = double.TryParse(txtBasePointX?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double bx) ? bx : 0.0;
                double baseY = double.TryParse(txtBasePointY?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double by) ? by : 0.0;

                string linesFile = Path.Combine(AutoCadHelper.LoadsDir, "cad_wall_lines.json");
                if (File.Exists(linesFile))
                {
                    try { File.Delete(linesFile); } catch { }
                }

                string formattedDll = AutoCadHelper.DllPath.Replace(@"\", "/");
                this.Hide();

                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_GET_SELECTED_BEAMS\n";
                AutoCadHelper.SendCommand(acadDoc, cmdStr);

                int maxWaitMs = 120000;
                int elapsed = 0;
                while (!File.Exists(linesFile) && elapsed < maxWaitMs)
                {
                    System.Threading.Thread.Sleep(15);
                    elapsed += 15;
                }
                this.Show();

                List<AutoCadWallLine> wallLines = new List<AutoCadWallLine>();
                if (File.Exists(linesFile))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(linesFile);
                        var queryRes = JsonHelper.Deserialize<AutoCadWallQuery>(jsonContent);
                        if (queryRes?.Lines != null)
                        {
                            wallLines = queryRes.Lines;
                        }
                    }
                    catch { }
                }

                if (wallLines.Count == 0)
                {
                    CustomMessageBox.Show("Không có dầm nào được chọn trên AutoCAD để gán tải.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                List<CadBeamLoadExportItem> exportedBeamsCache = new List<CadBeamLoadExportItem>();
                string exportJsonPath = Path.Combine(AutoCadHelper.LoadsDir, "wall_load_export.json");
                if (File.Exists(exportJsonPath))
                {
                    try
                    {
                        string json = File.ReadAllText(exportJsonPath);
                        var expPayload = JsonHelper.Deserialize<CadExportPayload>(json);
                        if (expPayload?.Beams != null && expPayload.Beams.Count > 0)
                        {
                            exportedBeamsCache = expPayload.Beams;
                        }
                    }
                    catch { }
                }

                var assignedBeamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var candidateBeams = new List<EtabsBeam>();

                foreach (var line in wallLines)
                {
                    EtabsBeam? matchedBeam = null;

                    if (!string.IsNullOrEmpty(line.BeamName))
                    {
                        string cleanName = line.BeamName.Split('|')[0].Trim();
                        if (floorBeamsDict.TryGetValue(cleanName, out var fb))
                        {
                            matchedBeam = fb;
                        }
                    }

                    if (matchedBeam == null && exportedBeamsCache.Count > 0)
                    {
                        double bestScore = double.MaxValue;
                        string bestExpName = "";
                        foreach (var expBeam in exportedBeamsCache)
                        {
                            double score = GeometryHelper.CalculateBeamMatchingScore(
                                line.X1, line.Y1, line.X2, line.Y2,
                                expBeam.X1, expBeam.Y1, expBeam.X2, expBeam.Y2,
                                maxPerpDist: 500.0
                            );
                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestExpName = expBeam.BeamName;
                            }
                        }
                        if (!string.IsNullOrEmpty(bestExpName) && bestScore < 10000.0)
                        {
                            string cleanExp = bestExpName.Split('|')[0].Trim();
                            if (floorBeamsDict.TryGetValue(cleanExp, out var fbExp))
                            {
                                matchedBeam = fbExp;
                            }
                        }
                    }

                    if (matchedBeam == null)
                    {
                        double etabsX1 = (line.X1 - baseX) / 1000.0;
                        double etabsY1 = (line.Y1 - baseY) / 1000.0;
                        double etabsX2 = (line.X2 - baseX) / 1000.0;
                        double etabsY2 = (line.Y2 - baseY) / 1000.0;

                        double bestScore = double.MaxValue;
                        foreach (var beam in floorBeams)
                        {
                            double score = GeometryHelper.CalculateBeamMatchingScore(
                                etabsX1, etabsY1, etabsX2, etabsY2,
                                beam.X1, beam.Y1, beam.X2, beam.Y2,
                                maxPerpDist: 0.50
                            );
                            if (score < bestScore)
                            {
                                bestScore = score;
                                matchedBeam = beam;
                            }
                        }

                        if (bestScore >= 10000.0) matchedBeam = null;
                    }

                    if (matchedBeam != null)
                    {
                        if (assignedBeamNames.Contains(matchedBeam.Name))
                        {
                            continue; // Chống gán trùng lặp dầm
                        }
                        assignedBeamNames.Add(matchedBeam.Name);
                        candidateBeams.Add(matchedBeam);
                    }
                }

                if (candidateBeams.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy dầm nào trên tầng '{storyName}' khớp với các đối tượng quét chọn.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi gán
                var cadAssignedList = candidateBeams.Select((b, idx) => new WallLoadAssignedItem
                {
                    Stt = idx + 1,
                    BeamName = b.Name,
                    Section = b.Section,
                    StoryHeight = 0,
                    FinalLoad = finalLoad
                }).ToList();

                var cadConfirmDialog = new NotificationView(
                    cadAssignedList,
                    wallTypeName,
                    wallLoadValue,
                    heSoGan,
                    isGridMode: true,
                    isConfirmMode: true,
                    storyName: storyName
                );
                cadConfirmDialog.Owner = this;
                if (cadConfirmDialog.ShowDialog() != true)
                {
                    SetStatusMessage("Đã hủy thao tác gán tải vào ETABS.", false);
                    return;
                }

                // Gán vào ETABS
                int cadSuccessCount = 0;
                foreach (var beam in candidateBeams)
                {
                    bool ok = _etabsService.AssignDistributedLoad(beam.Name, loaiTai, dirCode, finalLoad, replaceLoads);
                    if (ok) cadSuccessCount++;
                }

                _etabsService.RefreshView();

                SetStatusMessage($"Đã gán tải định mức {finalLoad:F2} kN/m cho {cadSuccessCount}/{candidateBeams.Count} dầm tại tầng {storyName}.", true);
                CustomMessageBox.Show($"Đã gán thành công {cadSuccessCount} dầm tại tầng '{storyName}' vào mô hình ETABS!", "Gán Tải Hoàn Tất", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi khi gán tải định mức: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private async void OnDongBoCadLiveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kiểm tra kết nối ETABS & AutoCAD
                if (!AppConnectionManager.IsEtabsConnected)
                {
                    CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                    return;
                }

                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Chưa kết nối với bản vẽ AutoCAD. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập bản vẽ.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 2. Lấy thông số từ UI
                string storyName = GetCboText(cboTangUpdate, "Base");
                string loadPattern = GetCboText(cboLoaiTai, "WALL");
                string directionName = GetCboText(cboHuongTai, "Gravity");
                int dirCode = EtabsService.GetDirectionCode(directionName);

                double baseX = double.TryParse(txtBasePointX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double bx) ? bx : 0;
                double baseY = double.TryParse(txtBasePointY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double by) ? by : 0;

                // 3. Thông báo xác nhận trước khi đồng bộ Live
                bool? confirm = CustomMessageBox.ShowConfirm(
                    $"Bạn có chắc chắn muốn Đồng Bộ CAD Live cho dầm tại tầng '{storyName}'?\n\n" +
                    $"• Trường hợp tải: {loadPattern}\n" +
                    $"• Hệ thống sẽ tự động đọc dữ liệu tải trọng dầm thực tế mới nhất từ ETABS và cập nhật đồng loạt các Text tải tường và màu sắc tương ứng trên AutoCAD trong thời gian thực.\n\n" +
                    $"Nhấn 'Xác nhận' để tiến hành đồng bộ.",
                    "Xác Nhận Đồng Bộ CAD Live"
                );
                if (confirm != true) return;

                SetStatusMessage($"Đang đọc dữ liệu tải dầm tầng {storyName} từ ETABS để đồng bộ Live CAD...");

                // 4. Lấy dữ liệu tải dầm từ ETABS
                var beams = await Task.Run(() =>
                {
                    bool isSel;
                    return _etabsService.GetSelectedOrStoryBeamsWithDistributedLoads(storyName, loadPattern, dirCode, baseX, baseY, out isSel);
                });
                if (beams == null || beams.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy dầm nào tại tầng '{storyName}' để đồng bộ.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                var livePayload = new CadWallLiveUpdatePayload
                {
                    Story = storyName,
                    LoadPattern = loadPattern,
                    BeamLoads = new Dictionary<string, CadWallLiveBeamItem>(StringComparer.OrdinalIgnoreCase)
                };

                foreach (var b in beams)
                {
                    if (string.IsNullOrEmpty(b.BeamName)) continue;

                    string summaryText = "0";
                    double maxVal = 0.0;

                    if (b.Segments != null && b.Segments.Count > 0)
                    {
                        var nonZeroSegs = b.Segments.Where(s => Math.Abs(s.Val1) > 0.001 || Math.Abs(s.Val2) > 0.001).ToList();
                        if (nonZeroSegs.Count > 0)
                        {
                            maxVal = nonZeroSegs.Max(s => Math.Max(s.Val1, s.Val2));
                            summaryText = nonZeroSegs[0].Text;
                        }
                    }

                    livePayload.BeamLoads[b.BeamName] = new CadWallLiveBeamItem
                    {
                        BeamName = b.BeamName,
                        Section = b.Section,
                        LoadText = summaryText,
                        MaxLoadValue = maxVal,
                        Segments = b.Segments ?? new List<CadLoadSegment>()
                    };
                }

                // 5. Ghi file JSON và gọi lệnh CAD In-Place Update
                string outDir = AutoCadHelper.LoadsDir;
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                string jsonFile = Path.Combine(outDir, "cad_wall_live_update.json");
                File.WriteAllText(jsonFile, JsonHelper.Serialize(livePayload));

                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string formattedJson = jsonFile.Replace("\\", "/");
                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_UPDATE_TEXT \"{formattedJson}\"\n";
                AutoCadHelper.SendCommand(acadDoc, cmdStr);

                SetStatusMessage($"Đã đồng bộ Live CAD thành công cho {livePayload.BeamLoads.Count} dầm tầng {storyName}.", true);
                CustomMessageBox.Show(
                    $"Đã đồng bộ CAD Live thành công cho tầng '{storyName}':\n\n" +
                    $"• Tổng số dầm được kiểm tra: {livePayload.BeamLoads.Count}\n" +
                    $"• Trường hợp tải: {loadPattern}\n\n" +
                    $"Toàn bộ Text và màu sắc tải tường trên AutoCAD đã được cập nhật thời gian thực theo mô hình ETABS!",
                    "Đồng Bộ CAD Live Thành Công",
                    CustomMessageBox.IconType.Success
                );
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi đồng bộ CAD Live: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void OnNhapTaiTuCadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Nhắc nhở người dùng xác nhận BasePoint và Tầng Update
                bool? confirm = CustomMessageBox.ShowConfirm(
                    "Vui lòng xác nhận đã thực hiện chọn điểm BasePoint và Tầng Update để thực hiện nhập tải.",
                    "Xác Nhận Nhập Tải Từ CAD"
                );
                if (confirm != true)
                {
                    return;
                }

                // 2. Kiểm tra kết nối ETABS & AutoCAD
                if (!AppConnectionManager.IsEtabsConnected)
                {
                    CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                    return;
                }

                if (_etabsService.IsModelLocked())
                {
                    CustomMessageBox.Show("Mô hình ETABS đang bị khóa (Locked). Vui lòng mở khóa mô hình trước khi import tải.", "Mô Hình Bị Khóa", CustomMessageBox.IconType.Warning);
                    return;
                }

                dynamic? acadDoc = AutoCadHelper.GetActiveAutoCadDoc();
                if (acadDoc == null)
                {
                    CustomMessageBox.Show("Chưa kết nối với bản vẽ AutoCAD. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập bản vẽ.", "Chưa Kết Nối CAD", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 3. Lấy thông số từ UI
                string storyName = GetCboText(cboTangUpdate, "Base");
                string loadPattern = GetCboText(cboLoaiTai, "WALL");
                string directionName = GetCboText(cboHuongTai, "Gravity");
                int dirCode = EtabsService.GetDirectionCode(directionName);
                bool replaceLoads = GetCboText(cboAddReplace, "Replace") == "Replace";

                double baseX = double.TryParse(txtBasePointX.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double bx) ? bx : 0;
                double baseY = double.TryParse(txtBasePointY.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double by) ? by : 0;

                // 4. Gửi lệnh quét Text & Dot tải trọng từ AutoCAD
                this.Hide();
                string formattedDll = AutoCadHelper.DllPath.Replace("\\", "/");
                string cmdStr = $"(command \"_NETLOAD\" \"{formattedDll}\") QKS_WALL_SCAN_LOADS\n";
                AutoCadHelper.SendCommand(acadDoc, cmdStr);
                this.Show();

                string scanFile = Path.Combine(AutoCadHelper.LoadsDir, "cad_edited_loads.json");
                if (!File.Exists(scanFile))
                {
                    CustomMessageBox.Show("Không tìm thấy file kết quả quét từ AutoCAD.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                string scanJson = File.ReadAllText(scanFile);
                var importResult = JsonHelper.Deserialize<CadImportPayload>(scanJson);

                if (importResult == null || importResult.Texts == null || importResult.Texts.Count == 0)
                {
                    CustomMessageBox.Show("Không tìm thấy Text tải trọng nào trên bản vẽ AutoCAD (hoặc layer QKS_WALL_LOAD_TEXT rỗng).", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                // 5. Lấy danh sách dầm của tầng hiện tại và toàn mô hình từ ETABS
                var floorBeams = _etabsService.GetBeamsForStory(storyName);
                var allBeams = _etabsService.GetBeamsList();
                if (floorBeams.Count == 0) floorBeams = allBeams;

                if (allBeams.Count == 0)
                {
                    CustomMessageBox.Show($"Không tìm thấy dầm nào trong ETABS.", "Lỗi Dữ Liệu", CustomMessageBox.IconType.Warning);
                    return;
                }

                // Đọc cache exported beams nếu có
                List<CadBeamLoadExportItem> exportedBeamsCache = new List<CadBeamLoadExportItem>();
                string exportJsonPath = Path.Combine(AutoCadHelper.LoadsDir, "wall_load_export.json");
                if (File.Exists(exportJsonPath))
                {
                    try
                    {
                        string json = File.ReadAllText(exportJsonPath);
                        var expPayload = JsonHelper.Deserialize<CadExportPayload>(json);
                        if (expPayload?.Beams != null && expPayload.Beams.Count > 0)
                        {
                            exportedBeamsCache = expPayload.Beams;
                        }
                    }
                    catch { }
                }

                // 6. Ánh xạ Tick Lines và Text tới Dầm từ dữ liệu quét AutoCAD
                var beamTicksMap = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
                var beamTextsMap = new Dictionary<string, List<(double pos, double val, double origX, double origY)>>(StringComparer.OrdinalIgnoreCase);
                var regexNumber = new System.Text.RegularExpressions.Regex(@"[-+]?[0-9]*[.,]?[0-9]+");
                int parsedCount = 0;
                int parsedTicksCount = 0;

                // 6A. Xử lý các Line chia tải (Tick Lines)
                if (importResult.Ticks != null && importResult.Ticks.Count > 0)
                {
                    foreach (var tick in importResult.Ticks)
                    {
                        double etabsTickX = (tick.MidX - baseX) / 1000.0;
                        double etabsTickY = (tick.MidY - baseY) / 1000.0;

                        EtabsBeam? matchedBeam = null;
                        if (!string.IsNullOrEmpty(tick.XDataBeamName))
                        {
                            matchedBeam = allBeams.Find(b => b.Name.Equals(tick.XDataBeamName, StringComparison.OrdinalIgnoreCase));
                        }

                        if (matchedBeam == null)
                        {
                            double minD = double.MaxValue;
                            foreach (var b in floorBeams)
                            {
                                double d = GeometryHelper.SegmentToSegmentDistance(etabsTickX, etabsTickY, etabsTickX, etabsTickY, b.X1, b.Y1, b.X2, b.Y2);
                                if (d < minD)
                                {
                                    minD = d;
                                    matchedBeam = b;
                                }
                            }
                            if (minD > 0.6) matchedBeam = null;
                        }

                        if (matchedBeam != null)
                        {
                            double bDx = matchedBeam.X2 - matchedBeam.X1;
                            double bDy = matchedBeam.Y2 - matchedBeam.Y1;
                            double bLen = Math.Sqrt(bDx * bDx + bDy * bDy);
                            double pos = 0.0;
                            if (bLen > 0.001)
                            {
                                pos = ((etabsTickX - matchedBeam.X1) * bDx + (etabsTickY - matchedBeam.Y1) * bDy) / bLen;
                            }
                            pos = Math.Max(0.0, Math.Min(bLen, pos));

                            if (!beamTicksMap.ContainsKey(matchedBeam.Name))
                            {
                                beamTicksMap[matchedBeam.Name] = new List<double>();
                            }
                            beamTicksMap[matchedBeam.Name].Add(pos);
                            parsedTicksCount++;
                        }
                    }
                }

                // 6B. Xử lý các Text tải trọng
                foreach (var item in importResult.Texts)
                {
                    if (string.IsNullOrWhiteSpace(item.TextValue)) continue;

                    // Parse số từ chuỗi (hỗ trợ cả 8.5 và 8,5 và 8.5 kN/m và 1.2 T/m)
                    var match = regexNumber.Match(item.TextValue);
                    if (!match.Success) continue;

                    string numStr = match.Value.Replace(',', '.');
                    if (!double.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedVal))
                    {
                        continue;
                    }

                    // Tọa độ Text (mm) chuyển về hệ ETABS (m)
                    double etabsTextX = (item.X - baseX) / 1000.0;
                    double etabsTextY = (item.Y - baseY) / 1000.0;

                    // Tìm dầm ETABS tương ứng
                    EtabsBeam? matchedBeam = null;

                    // 1. Khớp theo XData BeamName và Section nếu có
                    if (!string.IsNullOrEmpty(item.XDataBeamName))
                    {
                        if (!string.IsNullOrEmpty(item.Section))
                        {
                            matchedBeam = allBeams.Find(b => b.Name.Equals(item.XDataBeamName, StringComparison.OrdinalIgnoreCase) && b.Section.Equals(item.Section, StringComparison.OrdinalIgnoreCase));
                        }
                        if (matchedBeam == null)
                        {
                            matchedBeam = allBeams.Find(b => b.Name.Equals(item.XDataBeamName, StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    // 2. Khớp qua BeamLines quét được trên CAD
                    if (matchedBeam == null && importResult.BeamLines != null && importResult.BeamLines.Count > 0)
                    {
                        double minCadDist = double.MaxValue;
                        string bestBeamName = "";
                        string bestSec = "";

                        foreach (var bl in importResult.BeamLines)
                        {
                            double distCad = GeometryHelper.SegmentToSegmentDistance(
                                item.X, item.Y, item.X, item.Y,
                                bl.X1, bl.Y1, bl.X2, bl.Y2
                            );
                            if (distCad < minCadDist)
                            {
                                minCadDist = distCad;
                                bestBeamName = bl.BeamName;
                                bestSec = bl.Section;
                            }
                        }

                        if (minCadDist <= 1500.0 && !string.IsNullOrEmpty(bestBeamName))
                        {
                            if (!string.IsNullOrEmpty(bestSec))
                            {
                                matchedBeam = allBeams.Find(b => b.Name.Equals(bestBeamName, StringComparison.OrdinalIgnoreCase) && b.Section.Equals(bestSec, StringComparison.OrdinalIgnoreCase));
                            }
                            if (matchedBeam == null)
                            {
                                matchedBeam = allBeams.Find(b => b.Name.Equals(bestBeamName, StringComparison.OrdinalIgnoreCase));
                            }
                        }
                    }

                    // 3. Khớp qua exportedBeamsCache (đối chiếu tọa độ dầm đã xuất ra CAD)
                    if (matchedBeam == null && exportedBeamsCache.Count > 0)
                    {
                        double minCadDist = double.MaxValue;
                        string bestExpName = "";
                        string bestSec = "";

                        foreach (var expBeam in exportedBeamsCache)
                        {
                            double distCad = GeometryHelper.SegmentToSegmentDistance(
                                item.X, item.Y, item.X, item.Y,
                                expBeam.X1, expBeam.Y1, expBeam.X2, expBeam.Y2
                            );
                            if (distCad < minCadDist)
                            {
                                minCadDist = distCad;
                                bestExpName = expBeam.BeamName;
                                bestSec = expBeam.Section;
                            }
                        }

                        if (minCadDist <= 1500.0 && !string.IsNullOrEmpty(bestExpName))
                        {
                            if (!string.IsNullOrEmpty(bestSec))
                            {
                                matchedBeam = allBeams.Find(b => b.Name.Equals(bestExpName, StringComparison.OrdinalIgnoreCase) && b.Section.Equals(bestSec, StringComparison.OrdinalIgnoreCase));
                            }
                            if (matchedBeam == null)
                            {
                                matchedBeam = allBeams.Find(b => b.Name.Equals(bestExpName, StringComparison.OrdinalIgnoreCase));
                            }
                        }
                    }

                    // 4. Khớp theo hình học trong floorBeams / allBeams sau khi khử BasePoint
                    if (matchedBeam == null)
                    {
                        double minDistance = double.MaxValue;
                        foreach (var beam in floorBeams)
                        {
                            double dist = GeometryHelper.SegmentToSegmentDistance(
                                etabsTextX, etabsTextY, etabsTextX, etabsTextY,
                                beam.X1, beam.Y1, beam.X2, beam.Y2
                            );
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                matchedBeam = beam;
                            }
                        }

                        if (minDistance > 1.0)
                        {
                            foreach (var beam in allBeams)
                            {
                                double dist = GeometryHelper.SegmentToSegmentDistance(
                                    etabsTextX, etabsTextY, etabsTextX, etabsTextY,
                                    beam.X1, beam.Y1, beam.X2, beam.Y2
                                );
                                if (dist < minDistance)
                                {
                                    minDistance = dist;
                                    matchedBeam = beam;
                                }
                            }
                        }

                        if (minDistance > 1.0) matchedBeam = null;
                    }

                    if (matchedBeam != null)
                    {
                        double bDx = matchedBeam.X2 - matchedBeam.X1;
                        double bDy = matchedBeam.Y2 - matchedBeam.Y1;
                        double bLen = Math.Sqrt(bDx * bDx + bDy * bDy);
                        double pos = 0.5 * bLen;
                        if (bLen > 0.001)
                        {
                            pos = ((etabsTextX - matchedBeam.X1) * bDx + (etabsTextY - matchedBeam.Y1) * bDy) / bLen;
                        }
                        pos = Math.Max(0.0, Math.Min(bLen, pos));

                        if (!beamTextsMap.ContainsKey(matchedBeam.Name))
                        {
                            beamTextsMap[matchedBeam.Name] = new System.Collections.Generic.List<(double pos, double val, double origX, double origY)>();
                        }

                        beamTextsMap[matchedBeam.Name].Add((pos, parsedVal, item.X, item.Y));
                        parsedCount++;
                    }
                }

                // 7. Xây dựng phân đoạn tải (qua Ticks & Texts) và gán vào ETABS
                int updateSuccessCount = 0;
                int clearedCount = 0;

                // Tập hợp danh sách dầm của tầng hoặc phiên xuất CAD hiện tại
                var candidateBeams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (exportedBeamsCache.Count > 0)
                {
                    foreach (var eb in exportedBeamsCache) candidateBeams.Add(eb.BeamName);
                }
                foreach (var fb in floorBeams)
                {
                    candidateBeams.Add(fb.Name);
                }
                foreach (var bName in beamTextsMap.Keys)
                {
                    candidateBeams.Add(bName);
                }
                foreach (var bName in beamTicksMap.Keys)
                {
                    candidateBeams.Add(bName);
                }

                foreach (var bName in candidateBeams)
                {
                    var targetBeam = allBeams.Find(b => b.Name.Equals(bName, StringComparison.OrdinalIgnoreCase));
                    if (targetBeam == null) continue;

                    double bLength = Math.Sqrt((targetBeam.X2 - targetBeam.X1) * (targetBeam.X2 - targetBeam.X1) + (targetBeam.Y2 - targetBeam.Y1) * (targetBeam.Y2 - targetBeam.Y1));
                    var segments = new System.Collections.Generic.List<CadLoadSegment>();

                    bool hasTicks = beamTicksMap.TryGetValue(bName, out var tickPositions) && tickPositions.Count > 0;
                    bool hasTexts = beamTextsMap.TryGetValue(bName, out var textList) && textList.Count > 0;

                    if (hasTicks && tickPositions != null)
                    {
                        tickPositions.Sort();

                        // Phân cụm các vạch chia tải liên tiếp (bước nhảy > 650mm coi là phân đoạn mới)
                        var clusters = new List<List<double>>();
                        var curCluster = new List<double> { tickPositions[0] };

                        for (int i = 1; i < tickPositions.Count; i++)
                        {
                            if (tickPositions[i] - tickPositions[i - 1] <= 0.65)
                            {
                                curCluster.Add(tickPositions[i]);
                            }
                            else
                            {
                                clusters.Add(curCluster);
                                curCluster = new List<double> { tickPositions[i] };
                            }
                        }
                        clusters.Add(curCluster);

                        // Xử lý từng cluster phân đoạn tải
                        // Ngưỡng dung sai mép gối: 250mm (0.25m) là khoảng cách offset của vạch chia tải hoặc sai số bắt điểm CAD
                        // Tuyệt đối KHÔNG ép cứng (>= 60% -> Full) để bảo toàn 100% các phân đoạn tải lẻ (như 70%) và tránh lỗi DƯ TẢI
                        double endSnapTol = 0.25; // 250mm

                        var refinedClusters = new List<(double start, double end)>();
                        foreach (var cl in clusters)
                        {
                            if (cl.Count == 0) continue;
                            double cStart = cl[0];
                            double cEnd = cl[cl.Count - 1];
                            if (cl.Count == 1 && (cEnd - cStart) < 0.05) continue;
                            refinedClusters.Add((cStart, cEnd));
                        }

                        if (refinedClusters.Count > 0)
                        {
                            // 1. Chỉ snap đầu mút gần mép gối (<= 250mm) về đúng 0.0 hoặc bLength
                            for (int k = 0; k < refinedClusters.Count; k++)
                            {
                                double cStart = refinedClusters[k].start;
                                double cEnd = refinedClusters[k].end;

                                if (cStart <= endSnapTol) cStart = 0.0;
                                if (cEnd >= bLength - endSnapTol) cEnd = bLength;

                                refinedClusters[k] = (cStart, cEnd);
                            }

                            // 2. Nối khít các phân đoạn tải liên tiếp nếu khe hở nhỏ do sai số bắt điểm (<= 150mm)
                            for (int k = 0; k < refinedClusters.Count - 1; k++)
                            {
                                double gap = refinedClusters[k + 1].start - refinedClusters[k].end;
                                if (gap > 0 && gap <= 0.15)
                                {
                                    double mid = (refinedClusters[k].end + refinedClusters[k + 1].start) / 2.0;
                                    refinedClusters[k] = (refinedClusters[k].start, mid);
                                    refinedClusters[k + 1] = (mid, refinedClusters[k + 1].end);
                                }
                            }
                        }

                        foreach (var (sStart, sEnd) in refinedClusters)
                        {
                            if (sEnd <= sStart + 0.01) continue;

                            double clMid = (sStart + sEnd) / 2.0;

                            // Tìm Text tải khớp nhất trong phân đoạn này
                            double assignedVal = 0.0;
                            bool textFound = false;

                            if (hasTexts && textList != null)
                            {
                                double minTextDist = double.MaxValue;
                                foreach (var tItem in textList)
                                {
                                    double d = Math.Abs(tItem.pos - clMid);
                                    if (tItem.pos >= sStart - 0.35 && tItem.pos <= sEnd + 0.35)
                                    {
                                        if (d < minTextDist)
                                        {
                                            minTextDist = d;
                                            assignedVal = tItem.val;
                                            textFound = true;
                                        }
                                    }
                                }

                                if (!textFound && textList.Count == 1)
                                {
                                    assignedVal = textList[0].val;
                                    textFound = true;
                                }
                            }

                            if (textFound && assignedVal > 0)
                            {
                                segments.Add(new CadLoadSegment
                                {
                                    StartDist = Math.Round(sStart, 3),
                                    EndDist = Math.Round(sEnd, 3),
                                    Val1 = assignedVal,
                                    Val2 = assignedVal,
                                    Text = assignedVal.ToString("0.##")
                                });
                            }
                        }
                    }
                    else if (hasTexts && textList != null)
                    {
                        // Fallback: Dầm không có Tick chia đoạn nhưng có Text
                        if (textList.Count == 1)
                        {
                            double v = textList[0].val;
                            if (v > 0)
                            {
                                segments.Add(new CadLoadSegment
                                {
                                    StartDist = 0,
                                    EndDist = bLength,
                                    Val1 = v,
                                    Val2 = v,
                                    Text = v.ToString("0.##")
                                });
                            }
                        }
                        else
                        {
                            textList.Sort((a, b) => a.pos.CompareTo(b.pos));
                            for (int i = 0; i < textList.Count; i++)
                            {
                                double startDist = (i == 0) ? 0.0 : (textList[i - 1].pos + textList[i].pos) / 2.0;
                                double endDist = (i == textList.Count - 1) ? bLength : (textList[i].pos + textList[i + 1].pos) / 2.0;
                                double v = textList[i].val;

                                if (v > 0 && endDist > startDist + 0.01)
                                {
                                    segments.Add(new CadLoadSegment
                                    {
                                        StartDist = Math.Round(startDist, 3),
                                        EndDist = Math.Round(endDist, 3),
                                        Val1 = v,
                                        Val2 = v,
                                        Text = v.ToString("0.##")
                                    });
                                }
                            }
                        }
                    }

                    // Gán vào ETABS
                    if (segments.Count > 0)
                    {
                        if (replaceLoads)
                        {
                            _etabsService.DeleteFrameDistributedLoads(bName, loadPattern);
                        }

                        bool ok = _etabsService.AssignSegmentedDistributedLoads(bName, loadPattern, dirCode, segments, replace: false);
                        if (ok) updateSuccessCount++;
                    }
                    else
                    {
                        if (replaceLoads)
                        {
                            _etabsService.DeleteFrameDistributedLoads(bName, loadPattern);
                            clearedCount++;
                        }
                    }
                }

                // 8. Làm mới hiển thị ETABS
                _etabsService.RefreshView();

                CustomMessageBox.Show(
                    $"Đã hoàn tất nhập tải từ AutoCAD vào ETABS:\n\n" +
                    $"• Tầng cập nhật: {storyName}\n" +
                    $"• Trường hợp tải: {loadPattern} ({directionName})\n" +
                    $"• Chế độ: {(replaceLoads ? "Ghi đè (Replace)" : "Cộng thêm (Add)")}\n" +
                    $"• Số Text tải nhận diện: {parsedCount}\n" +
                    $"• Số vạch chia tải (Ticks): {parsedTicksCount}\n" +
                    $"• Số dầm cập nhật tải: {updateSuccessCount}\n" +
                    $"• Số dầm đã xóa tải / trả về 0: {clearedCount}",
                    "Nhập Tải Thành Công",
                    CustomMessageBox.IconType.Success
                );
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Có lỗi xảy ra khi nhập tải từ AutoCAD: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private static string GetCboText(System.Windows.Controls.ComboBox cbo, string defaultVal = "")
        {
            if (cbo == null) return defaultVal;
            if (cbo.SelectedItem is System.Windows.Controls.ComboBoxItem cbi)
            {
                return cbi.Content?.ToString()?.Trim() ?? defaultVal;
            }
            if (cbo.SelectedItem is WallLoadRow row)
            {
                return row.Type?.Trim() ?? defaultVal;
            }
            if (cbo.SelectedItem != null)
            {
                return cbo.SelectedItem.ToString()?.Trim() ?? defaultVal;
            }
            if (!string.IsNullOrWhiteSpace(cbo.Text))
            {
                return cbo.Text.Trim();
            }
            return defaultVal;
        }

        private static int GetCadColorIndex(System.Windows.Controls.ComboBox cbo, int defaultIndex)
        {
            if (cbo == null) return defaultIndex;
            string text = "";
            if (cbo.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                text = item.Content?.ToString() ?? "";
            }
            else if (cbo.SelectedItem != null)
            {
                text = cbo.SelectedItem.ToString() ?? "";
            }
            else if (!string.IsNullOrWhiteSpace(cbo.Text))
            {
                text = cbo.Text;
            }

            var parts = text.Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && int.TryParse(parts[0], out int code))
            {
                return code;
            }
            return defaultIndex;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCalcModeChanged(object sender, RoutedEventArgs e)
        {
            if (pnlCalcAuto == null || pnlCalcGrid == null) return;

            if (radTinhTuDong?.IsChecked == true)
            {
                pnlCalcAuto.Visibility = Visibility.Visible;
                pnlCalcGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlCalcAuto.Visibility = Visibility.Collapsed;
                pnlCalcGrid.Visibility = Visibility.Visible;
                UpdateSelectedWallLoadBadge();
            }
        }

        private void OnCboChonTuongGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedWallLoadBadge();
        }

        private void OnGridWallLoadsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridWallLoads?.SelectedItem is WallLoadRow row)
            {
                if (cboChonTuongGrid != null)
                {
                    cboChonTuongGrid.SelectedItem = row;
                }
                UpdateSelectedWallLoadBadge();
            }
        }

        private void UpdateSelectedWallLoadBadge()
        {
            if (lblSelectedWallType == null || lblSelectedWallLoad == null) return;

            if (cboChonTuongGrid?.SelectedItem is WallLoadRow row)
            {
                lblSelectedWallType.Text = row.Type;
                lblSelectedWallLoad.Text = $"{row.Load:F2} kN/m";
            }
            else if (gridWallLoads?.SelectedItem is WallLoadRow gridRow)
            {
                lblSelectedWallType.Text = gridRow.Type;
                lblSelectedWallLoad.Text = $"{gridRow.Load:F2} kN/m";
            }
        }

        private void OnCustomBeamDepthChecked(object sender, RoutedEventArgs e)
        {
            if (txtCustomBeamDepth != null && chkCustomBeamDepth != null)
            {
                txtCustomBeamDepth.IsEnabled = chkCustomBeamDepth.IsChecked == true;
                UpdateLiveCalculation();
            }
        }

        private void OnTruSanChecked(object sender, RoutedEventArgs e)
        {
            if (txtBeDaySan != null && chkTruSan != null)
            {
                txtBeDaySan.IsEnabled = chkTruSan.IsChecked == true;
                UpdateLiveCalculation();
            }
        }

        private void OnHeSoCuaChecked(object sender, RoutedEventArgs e)
        {
            if (txtHeSoCua != null && chkHeSoCua != null)
            {
                txtHeSoCua.IsEnabled = chkHeSoCua.IsChecked == true;
                UpdateLiveCalculation();
            }
        }

        private void OnLiveCalcParamChanged(object sender, RoutedEventArgs e)
        {
            UpdateLiveCalculation();
        }

        private void UpdateLiveCalculation()
        {
            if (lblGtuongPreview == null || lblHthPreview == null || lblQoutPreview == null) return;

            try
            {
                double klrGach = double.TryParse(txtKlrGach?.Text, out double kg) ? kg : 15.0;
                double n1 = double.TryParse(txtHeSoGach?.Text, out double hn1) ? hn1 : 1.1;
                double klrVua = double.TryParse(txtKlrVua?.Text, out double kv) ? kv : 18.0;
                double n2 = double.TryParse(txtHeSoVua?.Text, out double hn2) ? hn2 : 1.3;
                double dayTratMm = double.TryParse(txtDayTrat?.Text, out double dt) ? dt : 20.0;
                
                // Hệ số cửa: chỉ áp dụng khi Checkbox được tích, nếu không thì n3 = 1.0 (tường đặc)
                double n3 = 1.0;
                if (chkHeSoCua?.IsChecked == true)
                {
                    n3 = double.TryParse(txtHeSoCua?.Text, out double hn3) ? hn3 : 0.8;
                }
                
                double heSoGan = double.TryParse(txtHeSoGan?.Text, out double hsg) ? hsg : 1.0;

                bool is200 = radTuong200?.IsChecked == true;
                double tWall = is200 ? 0.20 : 0.10;
                double tTrat = dayTratMm / 1000.0;

                double gTuong = ((klrGach * n1 * tWall) + (klrVua * n2 * tTrat)) * n3;
                lblGtuongPreview.Text = $"{gTuong:F2} kN/m²";

                // Chiều cao tầng & tầng trên
                double storyHeight = 3.40;
                string upperStory = "";
                bool hasUpper = false;
                try
                {
                    string storyName = (cboTangUpdate?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(storyName) && AppConnectionManager.IsEtabsConnected)
                    {
                        hasUpper = _etabsService.GetUpperStoryInfo(storyName, out upperStory, out double h);
                        if (h > 0.1) storyHeight = h;
                    }
                }
                catch { }

                double beamH = 0.0;
                if (chkCustomBeamDepth?.IsChecked == true)
                {
                    beamH = double.TryParse(txtCustomBeamDepth?.Text, out double bh) ? bh : 0.0;
                }
                else if (hasUpper && !string.IsNullOrEmpty(upperStory))
                {
                    try
                    {
                        var sampleUpperBeams = _etabsService.GetBeamsForStory(upperStory);
                        if (sampleUpperBeams.Count > 0)
                        {
                            beamH = _etabsService.GetBeamDepth(sampleUpperBeams[0].Name);
                        }
                    }
                    catch { }
                }

                // Chiều dày sàn tầng trên trừ (m)
                double slabDepth = (chkTruSan?.IsChecked == true && double.TryParse(txtBeDaySan?.Text, out double sd)) ? sd : 0.0;

                // Thuật toán trừ Dầm - Sàn toàn khối:
                // - Khi có dầm đè lên: dầm sàn toàn khối, H dầm đã gồm sàn -> chỉ trừ dầm, không trừ sàn
                // - Khi không có dầm đè lên: trừ sàn nếu được bật
                double hTh = 0.0;
                if (beamH > 0.001)
                {
                    hTh = Math.Max(0.1, storyHeight - beamH);
                    if (lblPreviewHdam != null) lblPreviewHdam.Text = $"{beamH * 1000:F0} mm";
                    if (lblPreviewHsan != null)
                    {
                        lblPreviewHsan.Text = "Đã gồm trong dầm";
                        lblPreviewHsan.Opacity = 0.6;
                    }
                }
                else
                {
                    hTh = Math.Max(0.1, storyHeight - slabDepth);
                    if (lblPreviewHdam != null) lblPreviewHdam.Text = "0 mm (Không dầm)";
                    if (lblPreviewHsan != null)
                    {
                        if (chkTruSan?.IsChecked == true && slabDepth > 0.001)
                        {
                            lblPreviewHsan.Text = $"{slabDepth * 1000:F0} mm";
                            lblPreviewHsan.Opacity = 1.0;
                        }
                        else
                        {
                            lblPreviewHsan.Text = "Không trừ sàn";
                            lblPreviewHsan.Opacity = 0.6;
                        }
                    }
                }

                if (lblPreviewHtang != null)
                {
                    lblPreviewHtang.Text = !hasUpper ? $"{storyHeight:F2} m (Tầng mái)" : $"{storyHeight:F2} m";
                }

                if (lblPreviewCua != null)
                {
                    if (chkHeSoCua?.IsChecked == true)
                    {
                        lblPreviewCua.Text = $"n₃ = {n3:F2} (Có cửa)";
                        lblPreviewCua.Opacity = 1.0;
                    }
                    else
                    {
                        lblPreviewCua.Text = "n₃ = 1.00 (Tường đặc)";
                        lblPreviewCua.Opacity = 0.7;
                    }
                }

                lblHthPreview.Text = $"{hTh:F2} m";

                double q = gTuong * hTh * heSoGan;
                lblQoutPreview.Text = $"{q:F2} kN/m";
            }
            catch
            {
                // Ignore parse preview errors
            }
        }

        private void SetStatusMessage(string message, bool isSuccess = true)
        {
            Dispatcher.Invoke(() =>
            {
                if (lblStatusBarText != null)
                {
                    lblStatusBarText.Text = message;
                    lblStatusBarText.Foreground = isSuccess 
                        ? new MediaBrush(MediaColor.FromRgb(71, 85, 105)) 
                        : new MediaBrush(MediaColor.FromRgb(220, 38, 38));
                }
            });
        }

        private void OnSaveConfigClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QKS_Loads");
                if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
                string configFile = Path.Combine(configDir, "wall_load_config.json");

                var cfg = new Dictionary<string, string>
                {
                    { "KlrGach", txtKlrGach?.Text ?? "15" },
                    { "HeSoGach", txtHeSoGach?.Text ?? "1.1" },
                    { "KlrVua", txtKlrVua?.Text ?? "18" },
                    { "HeSoVua", txtHeSoVua?.Text ?? "1.3" },
                    { "DayTrat", txtDayTrat?.Text ?? "20" },
                    { "HeSoCua", txtHeSoCua?.Text ?? "1.0" },
                    { "HeSoGan", txtHeSoGan?.Text ?? "1" },
                    { "BasePointX", txtBasePointX?.Text ?? "0" },
                    { "BasePointY", txtBasePointY?.Text ?? "0" },
                    { "CustomBeamDepth", txtCustomBeamDepth?.Text ?? "0.40" },
                    { "IsCustomBeam", (chkCustomBeamDepth?.IsChecked == true).ToString() },
                    { "CadTextHeight", txtCadTextHeight?.Text ?? "200" },
                    { "CadTextColorIndex", cboCadTextColor?.SelectedIndex.ToString() ?? "0" }
                };

                string json = JsonHelper.Serialize(cfg);
                File.WriteAllText(configFile, json);

                SaveWallLoadsPreset();
                SetStatusMessage("Đã lưu cấu hình thiết kế (Design Config) thành công.", true);
                CustomMessageBox.Show("Đã lưu các thông số thiết kế vào cấu hình mặc định.", "Lưu Thiết Kế Thành Công", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi khi lưu cấu hình: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void LoadSavedConfiguration()
        {
            try
            {
                string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QKS_Loads");
                string configFile = Path.Combine(configDir, "wall_load_config.json");
                if (!File.Exists(configFile)) return;

                string json = File.ReadAllText(configFile);
                var cfg = JsonHelper.Deserialize<Dictionary<string, string>>(json);
                if (cfg == null) return;

                if (cfg.TryGetValue("KlrGach", out var kg) && txtKlrGach != null) txtKlrGach.Text = kg;
                if (cfg.TryGetValue("HeSoGach", out var hsg) && txtHeSoGach != null) txtHeSoGach.Text = hsg;
                if (cfg.TryGetValue("KlrVua", out var kv) && txtKlrVua != null) txtKlrVua.Text = kv;
                if (cfg.TryGetValue("HeSoVua", out var hsv) && txtHeSoVua != null) txtHeSoVua.Text = hsv;
                if (cfg.TryGetValue("DayTrat", out var dt) && txtDayTrat != null) txtDayTrat.Text = dt;
                if (cfg.TryGetValue("HeSoCua", out var hsc) && txtHeSoCua != null) txtHeSoCua.Text = hsc;
                if (cfg.TryGetValue("HeSoGan", out var hgan) && txtHeSoGan != null) txtHeSoGan.Text = hgan;
                // BasePoint giữ giá trị mặc định 0, 0 khi mở form theo yêu cầu người dùng
                if (txtBasePointX != null) txtBasePointX.Text = "0";
                if (txtBasePointY != null) txtBasePointY.Text = "0";
                if (cfg.TryGetValue("CustomBeamDepth", out var cbd) && txtCustomBeamDepth != null) txtCustomBeamDepth.Text = cbd;
                if (cfg.TryGetValue("IsCustomBeam", out var icb) && chkCustomBeamDepth != null && bool.TryParse(icb, out bool bVal))
                {
                    chkCustomBeamDepth.IsChecked = bVal;
                    if (txtCustomBeamDepth != null) txtCustomBeamDepth.IsEnabled = bVal;
                }
                if (cfg.TryGetValue("CadTextHeight", out var cth) && txtCadTextHeight != null) txtCadTextHeight.Text = cth;
                if (cfg.TryGetValue("CadTextColorIndex", out var ctci) && cboCadTextColor != null && int.TryParse(ctci, out int idxC))
                {
                    if (idxC >= 0 && idxC < cboCadTextColor.Items.Count) cboCadTextColor.SelectedIndex = idxC;
                }
            }
            catch
            {
                // Ignore config load error on startup
            }
        }



        private void OnSearchSectionTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string query = txtSearchSection?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(query))
                {
                    gridBeamSections.ItemsSource = BeamSections;
                }
                else
                {
                    gridBeamSections.ItemsSource = BeamSections.Where(s => s.SectionName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
            }
            catch { }
        }

        #region EXCEL EXPORT & IMPORT BẢNG ĐỊNH MỨC TƯỜNG
        private void OnExportWallLoadsExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic? excelApp = ExcelDna.Integration.ExcelDnaUtil.Application;
                if (excelApp == null)
                {
                    CustomMessageBox.Show("Không thể kết nối với ứng dụng Microsoft Excel.", "Lỗi Excel", CustomMessageBox.IconType.Warning);
                    return;
                }

                dynamic activeWb = excelApp.ActiveWorkbook;
                if (activeWb == null) activeWb = excelApp.Workbooks.Add();

                dynamic ws = activeWb.Sheets.Add();
                ws.Name = "QKS_DinhMucTuong_" + DateTime.Now.ToString("HHmmss");

                // Headers
                ws.Cells[1, 1].Value = "Loại Tường";
                ws.Cells[1, 2].Value = "Tải Trọng Định Mức (kN/m)";
                ws.Cells[1, 3].Value = "Quy Cách Hiển Thị";

                dynamic headerRange = ws.Range["A1:C1"];
                headerRange.Font.Bold = true;
                headerRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(15, 34, 64)); // Navy #0F2240
                headerRange.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);
                headerRange.HorizontalAlignment = -4108; // xlCenter

                int row = 2;
                foreach (var wl in WallLoads)
                {
                    ws.Cells[row, 1].Value = wl.Type;
                    ws.Cells[row, 2].Value = wl.Load;
                    ws.Cells[row, 3].Value = wl.DisplayText;
                    ws.Cells[row, 1].HorizontalAlignment = -4108;
                    ws.Cells[row, 2].HorizontalAlignment = -4152; // xlRight
                    row++;
                }

                dynamic dataRange = ws.Range[$"A1:C{row - 1}"];
                dataRange.Borders.LineStyle = 1; // xlContinuous

                ws.Columns["A:C"].AutoFit();
                CustomMessageBox.Show($"Đã xuất thành công {WallLoads.Count} loại tường định mức ra Sheet '{ws.Name}'.", "Xuất Excel Thành Công", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void OnImportWallLoadsExcelClick(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic? excelApp = ExcelDna.Integration.ExcelDnaUtil.Application;
                if (excelApp == null)
                {
                    CustomMessageBox.Show("Không thể kết nối với ứng dụng Microsoft Excel.", "Lỗi Excel", CustomMessageBox.IconType.Warning);
                    return;
                }

                dynamic activeSheet = excelApp.ActiveSheet;
                if (activeSheet == null)
                {
                    CustomMessageBox.Show("Không có Worksheet nào đang mở để đọc dữ liệu.", "Lỗi Excel", CustomMessageBox.IconType.Warning);
                    return;
                }

                int row = 2;
                var importedRows = new List<WallLoadRow>();

                while (true)
                {
                    string typeVal = activeSheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(typeVal))
                    {
                        string nextType = activeSheet.Cells[row + 1, 1].Value?.ToString()?.Trim() ?? "";
                        if (string.IsNullOrWhiteSpace(nextType)) break;
                        row++;
                        continue;
                    }

                    string loadStr = activeSheet.Cells[row, 2].Value?.ToString() ?? "0";
                    if (!double.TryParse(loadStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double loadVal))
                    {
                        double.TryParse(loadStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out loadVal);
                    }

                    importedRows.Add(new WallLoadRow
                    {
                        Type = typeVal,
                        Load = Math.Round(loadVal, 2)
                    });
                    row++;
                }

                if (importedRows.Count == 0)
                {
                    CustomMessageBox.Show("Không tìm thấy dữ liệu định mức tải tường trên Sheet hiện tại.\nQuy cách: Cột A là 'Loại Tường', Cột B là 'Tải Trọng (kN/m)' bắt đầu từ dòng 2.", "Thông Báo", CustomMessageBox.IconType.Warning);
                    return;
                }

                WallLoads.Clear();
                foreach (var r in importedRows)
                {
                    WallLoads.Add(r);
                }

                if (cboTab2WallLoad != null)
                {
                    cboTab2WallLoad.ItemsSource = null;
                    cboTab2WallLoad.ItemsSource = WallLoads;
                    if (WallLoads.Count > 0) cboTab2WallLoad.SelectedIndex = 0;
                }
                if (cboChonTuongGrid != null)
                {
                    cboChonTuongGrid.ItemsSource = null;
                    cboChonTuongGrid.ItemsSource = WallLoads;
                    if (WallLoads.Count > 0) cboChonTuongGrid.SelectedIndex = 0;
                }

                SaveWallLoadsPreset();
                CustomMessageBox.Show($"Đã nạp thành công {importedRows.Count} loại tường định mức từ Excel và lưu vào cấu hình!", "Nạp Excel Thành Công", CustomMessageBox.IconType.Success);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Lỗi nạp Excel: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private void SaveWallLoadsPreset()
        {
            try
            {
                string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QKS_Loads");
                if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
                string presetFile = Path.Combine(configDir, "wall_loads_preset.json");
                string json = JsonHelper.Serialize(WallLoads.ToList());
                File.WriteAllText(presetFile, json);
            }
            catch { }
        }

        private void LoadWallLoadsPreset()
        {
            try
            {
                string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QKS_Loads");
                string presetFile = Path.Combine(configDir, "wall_loads_preset.json");
                if (File.Exists(presetFile))
                {
                    string json = File.ReadAllText(presetFile);
                    var list = JsonHelper.Deserialize<List<WallLoadRow>>(json);
                    if (list != null && list.Count > 0)
                    {
                        WallLoads.Clear();
                        foreach (var item in list)
                        {
                            WallLoads.Add(item);
                        }
                    }
                }
            }
            catch { }
        }
        #endregion

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Thuật toán hình học hỗ trợ so khớp chính xác dầm theo góc phương vị, khoảng cách vuông góc và độ chồng lấn
    /// </summary>
    public static class GeometryHelper
    {
        public static double CalculateBeamMatchingScore(
            double px1, double py1, double px2, double py2,
            double bx1, double by1, double bx2, double by2,
            double maxPerpDist = 500.0)
        {
            double pux = px2 - px1;
            double puy = py2 - py1;
            double pLen = Math.Sqrt(pux * pux + puy * puy);

            double bvx = bx2 - bx1;
            double bvy = by2 - by1;
            double bLen = Math.Sqrt(bvx * bvx + bvy * bvy);

            if (pLen < 0.001 || bLen < 0.001) return double.MaxValue;

            // 1. Kiểm tra góc hợp giữa 2 vector (Cosine)
            double dot = pux * bvx + puy * bvy;
            double cosTheta = Math.Abs(dot) / (pLen * bLen);
            if (cosTheta > 1.0) cosTheta = 1.0;

            // BẮT BUỘC: cos(theta) >= 0.85 (góc lệch tối đa ~30 độ). Bỏ qua dầm vuông góc/xiên khác phương
            if (cosTheta < 0.85) return double.MaxValue;

            // 2. Khoảng cách vuông góc từ 2 điểm pick tới đường thẳng chứa dầm
            double A = bvy;
            double B = -bvx;
            double C = bx2 * by1 - by2 * bx1;

            double distPerp1 = Math.Abs(A * px1 + B * py1 + C) / bLen;
            double distPerp2 = Math.Abs(A * px2 + B * py2 + C) / bLen;
            double maxDistPerp = Math.Max(distPerp1, distPerp2);

            if (maxDistPerp > maxPerpDist) return double.MaxValue;

            // 3. Chiếu 2 điểm pick lên trục dầm để tính độ dài chồng lấn (overlap)
            double t1 = ((px1 - bx1) * bvx + (py1 - by1) * bvy) / bLen;
            double t2 = ((px2 - bx1) * bvx + (py2 - by1) * bvy) / bLen;

            double s = Math.Min(t1, t2);
            double e = Math.Max(t1, t2);

            double overlapStart = Math.Max(0.0, s);
            double overlapEnd = Math.Min(bLen, e);
            double overlapLen = Math.Max(0.0, overlapEnd - overlapStart);

            double minLen = Math.Min(pLen, bLen);
            double overlapRatio = minLen > 0.001 ? overlapLen / minLen : 0.0;

            // BẮT BUỘC: Có độ dài chồng lấn dọc trục dầm >= 15% (loại bỏ trường hợp chỉ chạm tại nút giao)
            if (overlapRatio < 0.15) return double.MaxValue;

            // Điểm phạt càng nhỏ thì mức độ khớp càng cao và chính xác
            double score = maxDistPerp + (1.0 - cosTheta) * 500.0 + (1.0 - overlapRatio) * 300.0;
            return score;
        }

        public static double SegmentToSegmentDistance(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
        {
            double d1 = PointToSegmentDistance(ax, ay, cx, cy, dx, dy);
            double d2 = PointToSegmentDistance(bx, by, cx, cy, dx, dy);
            double d3 = PointToSegmentDistance(cx, cy, ax, ay, bx, by);
            double d4 = PointToSegmentDistance(dx, dy, ax, ay, bx, by);
            
            return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
        }

        private static double PointToSegmentDistance(double px, double py, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            
            if (dx == 0 && dy == 0)
            {
                return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            }
            
            double t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
            
            if (t < 0)
            {
                return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            }
            else if (t > 1)
            {
                return Math.Sqrt((px - x2) * (px - x2) + (py - y2) * (py - y2));
            }
            
            double projX = x1 + t * dx;
            double projY = y1 + t * dy;
            return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
        }
    }

    public class WallLoadRow
    {
        public string Type { get; set; } = string.Empty;
        public double Load { get; set; }
        public string DisplayText => $"{Type} ({Load:F2} kN/m)";
    }

    public class BeamSectionInfo : INotifyPropertyChanged
    {
        private string _sectionName = string.Empty;
        private int _count;
        private double _totalLength;
        private byte _colorR;
        private byte _colorG;
        private byte _colorB;

        public string SectionName
        {
            get => _sectionName;
            set { _sectionName = value; OnPropertyChanged(nameof(SectionName)); }
        }

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(nameof(Count)); OnPropertyChanged(nameof(CountText)); }
        }

        public double TotalLength
        {
            get => _totalLength;
            set { _totalLength = value; OnPropertyChanged(nameof(TotalLength)); OnPropertyChanged(nameof(TotalLengthText)); }
        }

        public byte ColorR
        {
            get => _colorR;
            set { _colorR = value; OnPropertyChanged(nameof(ColorR)); OnPropertyChanged(nameof(ColorHex)); OnPropertyChanged(nameof(ColorBrush)); }
        }

        public byte ColorG
        {
            get => _colorG;
            set { _colorG = value; OnPropertyChanged(nameof(ColorG)); OnPropertyChanged(nameof(ColorHex)); OnPropertyChanged(nameof(ColorBrush)); }
        }

        public byte ColorB
        {
            get => _colorB;
            set { _colorB = value; OnPropertyChanged(nameof(ColorB)); OnPropertyChanged(nameof(ColorHex)); OnPropertyChanged(nameof(ColorBrush)); }
        }

        public string ColorHex => $"#{ColorR:X2}{ColorG:X2}{ColorB:X2}";
        public MediaBrush ColorBrush => new MediaBrush(MediaColor.FromRgb(ColorR, ColorG, ColorB));
        public string CountText => $"{Count} dầm";
        public string TotalLengthText => $"{TotalLength:F1} m";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class WallLoadTask
    {
        public EtabsBeam Beam { get; set; } = null!;
        public bool IsFullBeam { get; set; }
        public double D1 { get; set; }
        public double D2 { get; set; }
        public double FinalLoad { get; set; }
        public WallLoadAssignedItem ReportItem { get; set; } = null!;
    }
}
