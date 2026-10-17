# QKS 2D — Preview 1.1 / Material Design

Bản cập nhật style theo Tải tường/Tải sàn: button có ripple/elevation, ComboBox/RadioButton/CheckBox/TextBox/tab/bảng dùng Material Design; chuẩn bị style ListBox/ListBoxItem. Giữ navy QKS và góc button bo nhẹ 3 DIP.

- [Tải bộ XLL Preview 1.1](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-1.1/QKS-2D-Preview-1.1.zip)
- [Tải mã nguồn Preview 1.1](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-1.1/QKS-2D-Preview-1.1-Source.zip)
- [SHA256SUMS.txt](SHA256SUMS.txt)
- [Báo cáo thay đổi và cách cập nhật Excel](../../docs/VIEWER-PREVIEW-1.1.md)

Đóng Excel → giải nén → bỏ đăng ký XLL QKS cũ → đăng ký `Excel\QksExcelAddIn-AddIn64-packed.xll` mới. Mở **Mô Hình 2D**, kiểm tra header **PREVIEW 1.1**. Không cần chạy deploy-local.ps1 cho viewer.

Build 0 cảnh báo/0 lỗi, 73/73 core/VM tests đạt. Chưa kiểm tra render/ripple/DPI trên Windows. Viewer vẫn chỉ đọc; adapter SAFE 18–22/22.5 và draft/Apply chưa được triển khai trong gói.
