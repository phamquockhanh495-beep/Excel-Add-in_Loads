# QKS 2D Preview 1.1 — Material Design

Cập nhật 17/10/2026 theo phản hồi giao diện: viewer dùng MaterialDesignThemes 5.3.2 đã có trong dự án, cùng resource và cách định dạng của `WallLoadView` / `ShellLoadView`.

## Thay đổi

- Thay template button tự viết bằng style kế thừa **MaterialDesignRaisedButton**. Giữ template thư viện, ripple, focus và trạng thái disabled; thêm elevation Dp2, chiều cao 38 DIP, góc bo 3 DIP. Navy chính `#102A4C`, hover `#163D66`, pressed `#08172B`; headbar vẫn `#0B1F3A`.
- ComboBox kế thừa **MaterialDesignComboBox**, item dropdown kế thừa **MaterialDesignComboBoxItemStyle**. Dùng padding/font gọn như các cửa sổ tải hiện có.
- RadioButton, CheckBox và TextBox kế thừa style Material Design tương ứng.
- Có style **MaterialDesignListBox / MaterialDesignListBoxItem** trong bộ resource của viewer để dùng cho danh sách hiện tại/tương lai; không thêm danh sách hoặc thay đổi luồng chọn đối tượng trong đợt này.
- TabControl/TabItem và DataGrid kế thừa Material Design để đồng bộ với các control còn lại.
- Phần thông tin đối tượng và chẩn đoán căn nội dung lên đầu, tránh khoảng trống do căn giữa theo chiều cao ô.
- Resource mới ở `Views/Styles/VisualWorkspaceStyles.xaml`, nạp lại `MaterialDesignResources.xaml` giống các cửa sổ Tải tường/Tải sàn. Style chỉ áp dụng trong viewer; không sửa bộ resource chung hoặc các cửa sổ tải cũ.

## Tải và cập nhật Excel

Lấy [gói Preview 1.1 trên GitHub](../downloads/QKS-2D-Preview-1.1/). Bộ này thay XLL của Preview 1 khi cài, không cần cài lại plugin AutoCAD.

1. Đóng Excel, giải nén ZIP vào một thư mục cố định mới. Nếu có mục **Properties → Unblock** cho ZIP tải từ GitHub, bỏ chặn trước khi giải nén.
2. Excel → **File → Options → Add-ins → Manage Excel Add-ins → Go**. Bỏ chọn XLL QKS cũ, chọn **Browse** và đăng ký `Excel\QksExcelAddIn-AddIn64-packed.xll` của bộ Preview 1.1.
3. Mở **Extension QKS → Mô Hình 2D**. Header phải ghi **PREVIEW 1.1** để phân biệt với XLL trước.
4. Dùng **Mở snapshot → viewer-readonly.json** để kiểm tra button/ripple/disabled, dropdown chọn tầng, radio Plan/Elevation, checkbox và tab/bảng. Thử phím Tab/Space và cuộn panel ở tỷ lệ DPI của máy.

## Kiểm tra và phạm vi

- Build packed XLL net48 cho Excel Windows 64-bit: **0 warning / 0 error**.
- Bộ core/VM hiện có: **73/73 đạt**, không thêm test mô phỏng lại phần style.
- XML mới hợp lệ và các key style kế thừa đã được đối chiếu với resource thư viện MaterialDesignThemes 5.3.2. Đây không phải kiểm tra render/focus/ripple thực trên Windows.
- Cloud không chạy Excel/WPF/CSI trên Windows; cần kiểm tra hiển thị và DPI bằng XLL mới trên máy sử dụng. Các ảnh thiết kế trước không phải ảnh chụp bản build này.
- Chức năng dữ liệu giữ phạm vi [Preview 1](VIEWER-PREVIEW-1.md): đọc Plan/Elevation ETABS, chưa có draft/Apply; SAFE 18–22 (ưu tiên 22.5) vẫn đang chờ phát triển/kiểm chứng adapter.

Gói tải kèm báo cáo, snapshot mẫu OFFLINE, log build/test, manifest commit nguồn và SHA-256 từng file.
