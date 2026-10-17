# QKS 2D Preview 3 — Excel Windows x64

- [Tải gói chạy thử ZIP](QKS-2D-Preview-3.zip?raw=true)
- [Tải mã nguồn ZIP](QKS-2D-Preview-3-Source.zip?raw=true)
- [SHA-256](SHA256SUMS.txt)
- [Báo cáo thay đổi và cài đặt](../../docs/VIEWER-PREVIEW-3.md)
- [Phạm vi TCVN 2737:2023 và gió dầm biên/User Loads](../../docs/WIND-TCVN2737-2023.md)
- [Các bài nghiệm thu Windows chưa chạy trong cloud](../../docs/PREVIEW-3-HOST-TESTS.md)

Nếu GitHub mở trang xem file ZIP, chọn **Raw / Download raw file** hoặc dùng liên kết tải trực tiếp:

https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-3/QKS-2D-Preview-3.zip

Giải nén, đóng toàn bộ Excel và bỏ nạp QKS cũ, sau đó Browse tới `Excel/QksExcelAddIn-AddIn64-packed.xll` trong Excel Options → Add-ins → Manage Excel Add-ins → Go. Viewer phải ghi **PREVIEW 3**.

**Đã có mã thử:** Apply ETABS tường/sàn/gió đường với preflight, backup SHA-256, kiểm tra API/read-back và journal khi lỗi; quy đổi áp lực đã tính sang tải dầm biên/lực tầng; sửa User Loads qua khóa diaphragm đã có. Gói có mẫu offline, logs core/build, benchmark và manifest hash.

**Chưa hoàn tất:** engine tự tính TCVN 2737:2023 theo T, tự lấy tâm cứng/khóa tầng, adapter SAFE 18–22 và nghiệm thu WPF/Excel/CSI trên Windows. Không dùng mẫu nhân tạo để gán vào model thật; thử ghi trên bản sao model. Build/test core thành công không chứng minh tương thích host.

Không cần deploy AutoCAD để mở viewer ETABS. Đợt này không thay plugin CAD.
