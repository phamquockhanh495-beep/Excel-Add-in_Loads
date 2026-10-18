# QKS 2D Preview 5 — Excel Windows x64

- [Tải gói chạy thử ZIP](QKS-2D-Preview-5.zip?raw=true)
- [Tải mã nguồn ZIP](QKS-2D-Preview-5-Source.zip?raw=true)
- [SHA-256](SHA256SUMS.txt)
- [Báo cáo thay đổi và các bước thử](../../docs/VIEWER-PREVIEW-5.md)
- [Định dạng Excel / CSV nhập gió theo Story](../../docs/WIND-STORY-INPUT.md)

Tải trực tiếp:

https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-5/QKS-2D-Preview-5.zip

Đóng toàn bộ Excel, bỏ nạp QKS cũ, giải nén vào thư mục cố định. Nếu có Unblock trong Properties của ZIP, bỏ chặn trước khi giải nén. Excel → Options → Add-ins → Manage Excel Add-ins → Go → Browse tới `Excel/QksExcelAddIn-AddIn64-packed.xll`. Headbar phải ghi **PREVIEW 5**.

Bản này bổ sung Delete/Khôi phục trong workspace (không xóa ETABS hoặc tải/nháp), badge tập chọn dưới trái, chữ xoay theo hướng dầm và giữ nhãn khi zoom. File tính lấy đúng workbook của Kết nối API; model đã kết nối tự đọc khi mở viewer hoặc đổi API. Chọn model khác trong ComboBox viewer thì bấm Đọc lại.

291/291 core/VM tests đạt; packed XLL build không warning/error. Gói có mẫu offline/Excel/CSV, báo cáo, logs và manifest SHA-256. Cloud Linux chưa chạy Excel/WPF/ETABS; cần nghiệm thu gesture, font/DPI, zoom và tự đọc API trên Windows. Chưa khẳng định giống ETABS 100%. SAFE live chưa xác minh; plugin CAD không thay đổi, không cần deploy AutoCAD để dùng viewer ETABS.

Các mẫu có tên/khóa nhân tạo, chỉ dùng với snapshot offline. Gió tiếp tục nhập trực tiếp q/F/M theo Story, không quy đổi; User Loads cần khóa API đã đọc và đối chiếu Story thủ công. Thử Apply trên bản sao model đã lưu.
