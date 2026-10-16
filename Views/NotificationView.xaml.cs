using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QksExcelAddIn.Services;

namespace QksExcelAddIn.Views
{
    public partial class NotificationView : Window
    {
        public NotificationView()
        {
            InitializeComponent();
            EtabsIconHelper.SetIcon(this);
        }

        public NotificationView(string wallTypeName, double weight, double reduction, double storyHeight, double beamDepth, double finalLoad, bool isGridMode = false, double slabDepth = 0.0, double doorCoeff = 1.0) : this()
        {
            gridSingleBeam.Visibility = Visibility.Visible;
            gridMultiBeams.Visibility = Visibility.Collapsed;

            lblWallType.Text = $"Loại tường: {wallTypeName}";
            if (isGridMode)
            {
                this.Width = 400;
                this.Height = 195;
                lblWallWeight.Text = $"Tải trọng định mức: {weight:F2} kN/m";
                lblFinalLoad.Text = $"Tải gán trên dầm: {finalLoad:F2} kN/m";
                lblFinalLoad.Visibility = Visibility.Visible;

                lblStoryHeight.Visibility = Visibility.Collapsed;
                lblBeamDepth.Visibility = Visibility.Collapsed;
                lblSlabDepth.Visibility = Visibility.Collapsed;
                lblDoorReduction.Visibility = Visibility.Collapsed;
                lblReduction.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.Width = 430;
                this.Height = 280;
                lblWallWeight.Text = $"Tải trọng tường: {weight:F2} kN/m²";
                lblStoryHeight.Text = $"Chiều cao tầng: {storyHeight:F2} m";
                if (beamDepth > 0.001)
                {
                    lblBeamDepth.Text = $"Trừ Chiều cao dầm trên: {beamDepth * 1000:F0} mm";
                    lblSlabDepth.Text = "Trừ Chiều dày sàn: Đã gồm trong dầm";
                }
                else
                {
                    lblBeamDepth.Text = "Trừ Chiều cao dầm: 0 mm (Không dầm)";
                    lblSlabDepth.Text = slabDepth > 0.001 
                        ? $"Trừ Chiều dày sàn: {slabDepth * 1000:F0} mm" 
                        : "Trừ Chiều dày sàn: 0 mm (Không trừ)";
                }

                lblDoorReduction.Text = doorCoeff < 0.999 
                    ? $"Hệ số cửa (n₃): {doorCoeff:F2} (Đã giảm trừ)" 
                    : "Hệ số cửa (n₃): 1.00 (Tường đặc)";

                lblReduction.Text = $"Hệ số gán tải: {reduction:F2}";
                lblFinalLoad.Text = $"Tải gán trên dầm: {finalLoad:F2} kN/m";

                lblStoryHeight.Visibility = Visibility.Visible;
                lblBeamDepth.Visibility = Visibility.Visible;
                lblSlabDepth.Visibility = Visibility.Visible;
                lblDoorReduction.Visibility = Visibility.Visible;
                lblReduction.Visibility = Visibility.Visible;
                lblFinalLoad.Visibility = Visibility.Visible;
            }
        }

        public NotificationView(List<WallLoadAssignedItem> assignedBeams, string wallTypeName, double weight, double reduction, bool isGridMode = false, bool isConfirmMode = false, string storyName = "") : this()
        {
            if (assignedBeams == null || assignedBeams.Count == 0)
            {
                gridSingleBeam.Visibility = Visibility.Visible;
                gridMultiBeams.Visibility = Visibility.Collapsed;
                this.Width = 400;
                this.Height = 195;
                return;
            }

            if (isConfirmMode)
            {
                btnCancel.Visibility = Visibility.Visible;
                txtOk.Text = "XÁC NHẬN GÁN";
                btnOk.MinWidth = 125;
            }

            if (!isConfirmMode && assignedBeams.Count == 1)
            {
                var single = assignedBeams[0];
                gridSingleBeam.Visibility = Visibility.Visible;
                gridMultiBeams.Visibility = Visibility.Collapsed;

                lblWallType.Text = $"Loại tường: {wallTypeName} (Dầm: {single.BeamName})";

                if (isGridMode)
                {
                    this.Width = 400;
                    this.Height = 195;
                    lblWallWeight.Text = $"Tải trọng định mức: {weight:F2} kN/m";
                    lblFinalLoad.Text = $"Tải gán trên dầm: {single.FinalLoad:F2} kN/m";
                    lblFinalLoad.Visibility = Visibility.Visible;

                    lblStoryHeight.Visibility = Visibility.Collapsed;
                    lblBeamDepth.Visibility = Visibility.Collapsed;
                    lblSlabDepth.Visibility = Visibility.Collapsed;
                    lblDoorReduction.Visibility = Visibility.Collapsed;
                    lblReduction.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.Width = 430;
                    this.Height = 280;
                    lblWallWeight.Text = $"Tải trọng tường: {weight:F2} kN/m²";
                    lblStoryHeight.Text = $"Chiều cao tầng: {single.StoryHeight:F2} m";
                    lblBeamDepth.Text = single.UpperBeamDepth > 0.001 
                        ? $"Trừ Chiều cao dầm trên: {single.UpperBeamDepth * 1000:F0} mm" 
                        : "Trừ Chiều cao dầm: 0 mm (Không dầm)";
                    lblSlabDepth.Text = $"Trừ Chiều dày sàn: {single.SlabText}";
                    lblDoorReduction.Text = single.DoorCoeff < 0.999 
                        ? $"Hệ số cửa (n₃): {single.DoorCoeff:F2} (Đã giảm trừ)" 
                        : "Hệ số cửa (n₃): 1.00 (Tường đặc)";
                    lblReduction.Text = $"Hệ số gán tải: {reduction:F2}";
                    lblFinalLoad.Text = $"Tải gán trên dầm: {single.FinalLoad:F2} kN/m";

                    lblStoryHeight.Visibility = Visibility.Visible;
                    lblBeamDepth.Visibility = Visibility.Visible;
                    lblSlabDepth.Visibility = Visibility.Visible;
                    lblDoorReduction.Visibility = Visibility.Visible;
                    lblReduction.Visibility = Visibility.Visible;
                    lblFinalLoad.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Hiển thị DataGrid danh sách dầm (1 dầm hoặc nhiều dầm ở chế độ xác nhận)
                gridSingleBeam.Visibility = Visibility.Collapsed;
                gridMultiBeams.Visibility = Visibility.Visible;

                if (isGridMode)
                {
                    // Ẩn các cột hình học khi gán theo định mức
                    if (colStoryHeight != null) colStoryHeight.Visibility = Visibility.Collapsed;
                    if (colUpperBeam != null) colUpperBeam.Visibility = Visibility.Collapsed;
                    if (colSlab != null) colSlab.Visibility = Visibility.Collapsed;
                    if (colClearHeight != null) colClearHeight.Visibility = Visibility.Collapsed;
                    if (colDoorCoeff != null) colDoorCoeff.Visibility = Visibility.Collapsed;

                    this.Width = 520;
                    this.Height = 420;

                    if (isConfirmMode)
                    {
                        lblHeadbarTitle.Text = string.IsNullOrEmpty(storyName) 
                            ? $"XÁC NHẬN GÁN TẢI ĐỊNH MỨC ({assignedBeams.Count} DẦM)" 
                            : $"XÁC NHẬN GÁN TẢI ĐỊNH MỨC ({assignedBeams.Count} DẦM - TẦNG {storyName.ToUpper()})";
                        lblMultiSummaryHeader.Text = $"Tìm thấy {assignedBeams.Count} dầm thỏa mãn tại tầng {storyName} (Loại: {wallTypeName}). Bạn có chắc chắn muốn gán?";
                        lblFooterSummary.Text = $"Tổng số: {assignedBeams.Count} dầm  •  Tầng: {storyName}  •  Tải gán: {assignedBeams[0].FinalLoad:F2} kN/m";
                    }
                    else
                    {
                        lblHeadbarTitle.Text = $"KẾT QUẢ GÁN TẢI ĐỊNH MỨC ({assignedBeams.Count} DẦM)";
                        lblMultiSummaryHeader.Text = $"Đã gán thành công {assignedBeams.Count} dầm theo Bảng Tải Định Mức (Loại: {wallTypeName}).";
                        lblFooterSummary.Text = $"Tổng số: {assignedBeams.Count} dầm  •  Tải gán: {assignedBeams[0].FinalLoad:F2} kN/m";
                    }
                }
                else
                {
                    if (colStoryHeight != null) colStoryHeight.Visibility = Visibility.Visible;
                    if (colUpperBeam != null) colUpperBeam.Visibility = Visibility.Visible;
                    if (colSlab != null) colSlab.Visibility = Visibility.Visible;
                    if (colClearHeight != null) colClearHeight.Visibility = Visibility.Visible;
                    if (colDoorCoeff != null) colDoorCoeff.Visibility = Visibility.Visible;

                    this.Width = 780;
                    this.Height = 460;

                    if (isConfirmMode)
                    {
                        lblHeadbarTitle.Text = string.IsNullOrEmpty(storyName) 
                            ? $"XÁC NHẬN GÁN TẢI TƯỜNG ({assignedBeams.Count} DẦM)" 
                            : $"XÁC NHẬN GÁN TẢI TƯỜNG ({assignedBeams.Count} DẦM - TẦNG {storyName.ToUpper()})";
                        lblMultiSummaryHeader.Text = $"Tìm thấy {assignedBeams.Count} dầm thỏa mãn tại tầng {storyName}. Vui lòng kiểm tra danh sách bên dưới trước khi gán vào ETABS:";
                        lblFooterSummary.Text = $"Tổng số: {assignedBeams.Count} dầm  •  Tầng: {storyName}  •  Tải gán TB: {assignedBeams.Average(b => b.FinalLoad):F2} kN/m";
                    }
                    else
                    {
                        lblHeadbarTitle.Text = $"KẾT QUẢ GÁN TẢI ĐA DẦM ({assignedBeams.Count} DẦM)";
                        int distinctDepths = assignedBeams.Select(b => b.UpperBeamText).Distinct().Count();
                        lblMultiSummaryHeader.Text = $"Đã gán thành công {assignedBeams.Count} dầm trong ETABS (Phát hiện {distinctDepths} trường hợp dầm/sàn tầng trên khác nhau).";
                        lblFooterSummary.Text = $"Tổng: {assignedBeams.Count} dầm  •  Tải gán TB: {assignedBeams.Average(b => b.FinalLoad):F2} kN/m";
                    }
                }

                dgMultiBeams.ItemsSource = assignedBeams;
            }
        }

        public NotificationView(double width, double weight, double reduction, double storyHeight, double beamDepth, double finalLoad)
            : this($"Tường {width:F0}", weight, reduction, storyHeight, beamDepth, finalLoad, false)
        {
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }
}
