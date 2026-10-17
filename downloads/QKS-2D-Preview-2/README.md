# QKS 2D — Preview 2 / Nháp tải

Tiếp tục G3/G4: chọn hai điểm Plan để dựng Elevation, nháp tải tường/sàn, xem trước màu tím, Trước/Sau, Undo/Redo và lưu/mở bộ nháp gắn snapshot. Giữ giao diện Material Design navy của Preview 1.1.

- [Tải XLL và dữ liệu thử Preview 2](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-2/QKS-2D-Preview-2.zip)
- [Tải mã nguồn Preview 2](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-2/QKS-2D-Preview-2-Source.zip)
- [SHA256SUMS.txt](SHA256SUMS.txt)
- [Báo cáo thay đổi, hướng dẫn thử và tiến độ từng giai đoạn](../../docs/VIEWER-PREVIEW-2.md)

Đóng Excel → giải nén → bỏ đăng ký XLL QKS cũ → đăng ký `Excel\QksExcelAddIn-AddIn64-packed.xll`. Kiểm tra header **PREVIEW 2**. Mở snapshot `viewer-readonly.json`, sau đó **Mở nháp** → `viewer-draft.qksdraft.json` để có hai dòng mẫu. Snapshot và nháp đều là dữ liệu nhân tạo, hiển thị nguồn OFFLINE.

Build 0 cảnh báo/0 lỗi; 117/117 core/VM tests đạt. Chưa chạy UI/CSI trên Windows. Chưa có Apply trong viewer và adapter SAFE 18–22/22.5; các thao tác nháp không ghi model. Không cần AutoCAD hoặc deploy-local.ps1 cho viewer.
