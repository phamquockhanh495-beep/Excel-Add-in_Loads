# QKS 2D Preview 2.1 — Giao diện gọn

- Headbar QKS như Tải tường/Tải sàn, các lệnh file/kết nối chuyển về panel.
- Tab QKS với nội dung Dầm/Tường, Sàn và Gió riêng.
- Không hiển thị cột trong hình/bảng/lọc.
- Kích thước ưu tiên 1180×740 DIP, fit work area/DPI, panel thu gọn khi màn hình hẹp.

[Tải bộ XLL Preview 2.1](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-2.1/QKS-2D-Preview-2.1.zip) · [Tải mã nguồn](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-2.1/QKS-2D-Preview-2.1-Source.zip) · [SHA256SUMS](SHA256SUMS.txt)

Đóng Excel, bỏ XLL QKS cũ và đăng ký `Excel\QksExcelAddIn-AddIn64-packed.xll` của gói mới. Xác nhận title **PREVIEW 2.1**. Bấm **Hiển thị → File snapshot** để mở mẫu; **Tải / thuộc tính → Mở nháp** để mở bộ nháp mẫu.

[Báo cáo thay đổi và checklist Windows/DPI](../../docs/VIEWER-PREVIEW-2.1.md). 130/130 core/VM tests đạt, build 0 cảnh báo/0 lỗi. Chưa kiểm chứng UI/Win32 trên Windows; chưa có Apply/adapter SAFE trong viewer.
