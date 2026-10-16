using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using MediaColor = System.Windows.Media.Color;
using MediaBrush = System.Windows.Media.SolidColorBrush;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class WindLoadView : Window
    {
        public ObservableCollection<WindLoadRow> WindRows { get; set; }

        private readonly EtabsService _etabsService = new EtabsService();
        private readonly AutoCadIpcService _autocadService = new AutoCadIpcService();
        private System.Collections.Generic.List<AutoCadBeamData> _selectedBeams = new System.Collections.Generic.List<AutoCadBeamData>();

        public WindLoadView()
        {
            InitializeComponent();
            EtabsIconHelper.SetIcon(this);
            
            WindRows = new ObservableCollection<WindLoadRow>
            {
                new WindLoadRow { Name = "TT", Height = 1.2, Elevation = 19.5, WxPush = 12.5, WxPull = 8.3, WyPush = 10.2, WyPull = 6.8 },
                new WindLoadRow { Name = "MAI", Height = 2.7, Elevation = 18.3, WxPush = 15.2, WxPull = 10.1, WyPush = 12.4, WyPull = 8.2 },
                new WindLoadRow { Name = "TUM", Height = 3.2, Elevation = 15.6, WxPush = 18.4, WxPull = 12.3, WyPush = 15.1, WyPull = 10.0 },
                new WindLoadRow { Name = "LAU3", Height = 3.2, Elevation = 12.4, WxPush = 19.2, WxPull = 12.8, WyPush = 15.7, WyPull = 10.5 },
                new WindLoadRow { Name = "LAU2", Height = 3.2, Elevation = 9.2, WxPush = 18.9, WxPull = 12.6, WyPush = 15.5, WyPull = 10.3 },
                new WindLoadRow { Name = "LAU1", Height = 2.8, Elevation = 6.0, WxPush = 17.5, WxPull = 11.7, WyPush = 14.3, WyPull = 9.5 },
                new WindLoadRow { Name = "LUNG", Height = 3.2, Elevation = 3.2, WxPush = 15.0, WxPull = 10.0, WyPush = 12.3, WyPull = 8.2 }
            };
            
            DataContext = this;

            AppConnectionManager.ConnectionChanged += UpdateConnectionUI;
            Loaded += (s, e) => UpdateConnectionUI();
            Closed += (s, e) => AppConnectionManager.ConnectionChanged -= UpdateConnectionUI;
        }

        private void UpdateConnectionUI()
        {
            Dispatcher.Invoke(() =>
            {
                if (AppConnectionManager.IsEtabsConnected)
                {
                    dotEtabs.Fill = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                    lblEtabsStatus.Text = "ETABS: Đã kết nối";
                    lblEtabsStatus.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                    lblModelName.Text = AppConnectionManager.GetEtabsDisplayName();
                    lblModelName.Foreground = new MediaBrush(MediaColor.FromRgb(102, 102, 102));
                }
                else
                {
                    dotEtabs.Fill = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                    lblEtabsStatus.Text = "ETABS: Chưa kết nối";
                    lblEtabsStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                    lblModelName.Text = "Chưa chọn mô hình (Kết Nối API từ Ribbon)";
                    lblModelName.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                }

                if (AppConnectionManager.IsAutoCadConnected)
                {
                    dotCad.Fill = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                    lblCadStatus.Text = "AutoCAD: " + AppConnectionManager.GetAutoCadDisplayName();
                    lblCadStatus.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                }
                else
                {
                    dotCad.Fill = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                    lblCadStatus.Text = "AutoCAD: Chưa kết nối";
                    lblCadStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                }
            });
        }

        private void OnDocTangEtabsClick(object sender, RoutedEventArgs e)
        {
            if (!AppConnectionManager.IsEtabsConnected)
            {
                CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                string[] storyNames;
                double[] storyHeights;
                double[] storyElevations;
                int count = _etabsService.GetStoryList(out storyNames, out storyHeights, out storyElevations);

                if (count > 0)
                {
                    WindRows.Clear();
                    for (int i = count - 1; i >= 0; i--)
                    {
                        WindRows.Add(new WindLoadRow
                        {
                            Name = storyNames[i],
                            Height = Math.Round(storyHeights[i], 2),
                            Elevation = Math.Round(storyElevations[i], 2),
                            WxPush = Math.Round(12.0 + i * 1.2, 1),
                            WxPull = Math.Round(8.0 + i * 0.8, 1),
                            WyPush = Math.Round(10.0 + i * 1.0, 1),
                            WyPull = Math.Round(6.5 + i * 0.7, 1)
                        });
                    }
                    lblStatus.Text = $"Đã cập nhật {count} tầng từ mô hình ETABS.";
                    lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                    CustomMessageBox.Show($"Đã đọc thành công {count} tầng từ mô hình ETABS.", "Thành Công", CustomMessageBox.IconType.Success);
                }
                else
                {
                    lblStatus.Text = "Không tìm thấy dữ liệu tầng trong mô hình ETABS.";
                    lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                    CustomMessageBox.Show("Không tìm thấy dữ liệu tầng trong mô hình ETABS.", "Lỗi", CustomMessageBox.IconType.Warning);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi đọc tầng từ ETABS: " + ex.Message;
                lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                CustomMessageBox.Show($"Lỗi đọc tầng từ ETABS: {ex.Message}", "Lỗi", CustomMessageBox.IconType.ErrorType);
            }
        }

        private async void OnDocFileAutoCadClick(object sender, RoutedEventArgs e)
        {
            if (!AppConnectionManager.IsEtabsConnected)
            {
                CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                return;
            }

            lblStatus.Text = "Đang đọc danh sách dầm từ ETABS...";
            lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(15, 34, 64));
            var beams = _etabsService.GetBeamsList();
            if (beams.Count == 0)
            {
                lblStatus.Text = "Không tìm thấy dầm nào trong ETABS.";
                lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                CustomMessageBox.Show("Không tìm thấy dầm ngang nào trong mô hình ETABS hiện hành.", "Lỗi Dữ Liệu", CustomMessageBox.IconType.Warning);
                return;
            }

            lblStatus.Text = "Đang gửi dữ liệu dầm sang AutoCAD...";
            lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(15, 34, 64));
            var autocadQuery = new AutoCadBeamQuery
            {
                Action = "DrawBeams"
            };
            foreach (var b in beams)
            {
                autocadQuery.Beams.Add(new AutoCadBeamData
                {
                    Id = b.Name,
                    X1 = b.X1,
                    Y1 = b.Y1,
                    Z1 = b.Z1,
                    X2 = b.X2,
                    Y2 = b.Y2,
                    Z2 = b.Z2
                });
            }

            string requestJson = JsonHelper.Serialize(autocadQuery);
            string responseJson = await _autocadService.SendQueryAsync(requestJson);

            lblStatus.Text = "Đang đọc các dầm đã chọn/tô màu từ AutoCAD...";
            lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(15, 34, 64));
            var getQuery = new AutoCadBeamQuery
            {
                Action = "GetSelectedBeams"
            };
            string getRequestJson = JsonHelper.Serialize(getQuery);
            string getResponseJson = await _autocadService.SendQueryAsync(getRequestJson);

            if (getResponseJson.Contains("error"))
            {
                lblStatus.Text = "Không thể kết nối AutoCAD.";
                lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                CustomMessageBox.Show("Vui lòng đảm bảo AutoCAD đang mở và Plugin được nạp (lệnh NETLOAD).", "Lỗi AutoCAD IPC", CustomMessageBox.IconType.Warning);
                return;
            }

            try
            {
                var result = JsonHelper.Deserialize<AutoCadBeamQuery>(getResponseJson);

                if (result != null && result.Beams != null)
                {
                    _selectedBeams = result.Beams;
                    lblStatus.Text = $"Đã đọc thành công {_selectedBeams.Count} dầm biên từ AutoCAD.";
                    lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(16, 124, 65));
                    CustomMessageBox.Show($"Đã đọc thành công {_selectedBeams.Count} dầm biên từ AutoCAD.", "Đồng Bộ Thành Công", CustomMessageBox.IconType.Success);
                }
                else
                {
                    lblStatus.Text = "Không tìm thấy dầm nào được chọn từ AutoCAD.";
                    lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi giải mã JSON từ AutoCAD: " + ex.Message;
                lblStatus.Foreground = new MediaBrush(MediaColor.FromRgb(183, 28, 28));
            }
        }

        private void OnGanTaiGioClick(object sender, RoutedEventArgs e)
        {
            if (_selectedBeams == null || _selectedBeams.Count == 0)
            {
                CustomMessageBox.Show("Vui lòng nhấn nút 'ĐỌC TỪ AUTOCAD' để lấy danh sách dầm biên đã chọn trước khi gán tải.", "Thiếu Dữ Liệu", CustomMessageBox.IconType.Warning);
                return;
            }

            if (!AppConnectionManager.IsEtabsConnected)
            {
                CustomMessageBox.Show("Chưa kết nối với mô hình ETABS. Vui lòng bấm biểu tượng 'Kết Nối API' trên thanh Ribbon của Excel để thiết lập kết nối.", "Chưa Kết Nối ETABS", CustomMessageBox.IconType.Warning);
                return;
            }

            if (_etabsService.IsModelLocked())
            {
                CustomMessageBox.Show("Mô hình ETABS đang bị khóa (Locked). Vui lòng mở khóa mô hình trong ETABS trước khi gán tải.", "Mô Hình Bị Khóa", CustomMessageBox.IconType.Warning);
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var b in _selectedBeams)
            {
                // Tìm tầng tương ứng với dầm
                WindLoadRow? targetRow = null;
                foreach (var row in WindRows)
                {
                    if (Math.Abs(row.Elevation - b.Z1) < 0.1)
                    {
                        targetRow = row;
                        break;
                    }
                }

                if (targetRow == null)
                {
                    failCount++;
                    continue;
                }

                string loadPat = "";
                int dir = 1; // 1 = Global X, 2 = Global Y
                double loadValue = 0.0;

                switch (b.Color)
                {
                    case 1: // Đỏ: Gió đẩy X
                        loadPat = "WXP";
                        dir = 1;
                        loadValue = targetRow.WxPush;
                        break;
                    case 2: // Vàng: Gió hút X
                        loadPat = "WXH";
                        dir = 1;
                        loadValue = targetRow.WxPull;
                        break;
                    case 3: // Xanh lá: Gió đẩy Y
                        loadPat = "WYP";
                        dir = 2;
                        loadValue = targetRow.WyPush;
                        break;
                    case 4: // Xanh nhạt (Cyan): Gió hút Y
                        loadPat = "WYH";
                        dir = 2;
                        loadValue = targetRow.WyPull;
                        break;
                    default:
                        continue;
                }

                bool success = _etabsService.AssignDistributedLoad(b.Id, loadPat, dir, loadValue);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            // Làm mới khung nhìn ETABS
            _etabsService.RefreshView();

            lblStatus.Text = $"Gán tải thành công {successCount} dầm. Thất bại {failCount} dầm.";
            CustomMessageBox.Show($"Hoàn tất gán tải trọng gió biên vào ETABS:\n- Gán thành công: {successCount} dầm\n- Thất bại/Lỗi tầng: {failCount} dầm", "Kết Quả Gán Tải", CustomMessageBox.IconType.Success);
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

    public class WindLoadRow
    {
        public string Name { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Elevation { get; set; }
        public double WxPush { get; set; }
        public double WxPull { get; set; }
        public double WyPush { get; set; }
        public double WyPull { get; set; }
    }
}
