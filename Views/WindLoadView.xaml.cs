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
        private System.Collections.Generic.List<AutoCadBeamData> _selectedBeams = new System.Collections.Generic.List<AutoCadBeamData>();

        public WindLoadView()
        {
            InitializeComponent();
            EtabsIconHelper.SetIcon(this);

            WindRows = new ObservableCollection<WindLoadRow>();

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
                            WxPush = 0.0,
                            WxPull = 0.0,
                            WyPush = 0.0,
                            WyPull = 0.0
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

        private void OnDocFileAutoCadClick(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("Chức năng tải gió đang chờ hoàn thiện công thức và kiểm thử theo tiêu chuẩn. Chưa hỗ trợ ghi tải vào mô hình.", "Tải gió chưa sẵn sàng", CustomMessageBox.IconType.Warning);
        }

        private void OnGanTaiGioClick(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("Chức năng tải gió đang chờ hoàn thiện công thức và kiểm thử theo tiêu chuẩn. Chưa hỗ trợ ghi tải vào mô hình.", "Tải gió chưa sẵn sàng", CustomMessageBox.IconType.Warning);
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
