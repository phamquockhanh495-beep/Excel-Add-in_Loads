# QKS 2D Preview 4 — Excel Windows x64

- [Tải gói chạy thử ZIP](QKS-2D-Preview-4.zip?raw=true)
- [Tải mã nguồn ZIP](QKS-2D-Preview-4-Source.zip?raw=true)
- [SHA-256](SHA256SUMS.txt)
- [Báo cáo thay đổi và cách thử](../../docs/VIEWER-PREVIEW-4.md)
- [Định dạng Excel / CSV và nhập gió theo Story](../../docs/WIND-STORY-INPUT.md)

Tải trực tiếp nếu GitHub mở trang xem ZIP:

https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-4/QKS-2D-Preview-4.zip

Đóng toàn bộ Excel, bỏ nạp QKS cũ, giải nén gói vào thư mục cố định. Trong Excel Options → Add-ins → Manage Excel Add-ins → Go → Browse, chọn `Excel/QksExcelAddIn-AddIn64-packed.xll`. Nếu Windows có mục **Unblock** trong Properties của ZIP, bỏ chặn trước khi giải nén. Headbar viewer phải ghi **PREVIEW 4**. Không cần chạy script deploy AutoCAD để dùng viewer ETABS.

**Đã cập nhật:** quét Window/Crossing hai chiều, Ctrl chọn nhiều, Delete hủy chọn; ComboBox mô hình; snap chọn hai điểm và mốc Story trên Elevation; nhãn Label/Section/UniqueName; bảng cao hơn 1,25 lần, dữ liệu chữ thường; nhập gió trực tiếp từ Excel/CSV/popup theo Story. Giữ theme navy/Material Design và luồng Apply có backup/read-back của Preview 3.

Gói gồm XLL, mẫu snapshot/nháp offline, CSV mẫu và workbook trống theo Story; báo cáo, logs kiểm tra/build, benchmark core/VM và manifest SHA-256. CSV có tên/khóa **nhân tạo**, chỉ dùng với mẫu offline; hãy xuất mẫu theo model thật trong popup trước khi điền số liệu dự án.

**Kiểm tra cloud:** 255/255 core/VM tests; build packed XLL không warning/error; mẫu workbook đọc được bằng bộ đọc độc lập. Cloud Linux chưa chạy Excel/WPF/ETABS: cần thử gesture, snap, vị trí chữ, DPI, đọc vùng Excel và Apply trên Windows. Không khẳng định giao diện/thao tác giống ETABS 100%. SAFE live vẫn chưa có adapter xác minh. Đợt này không thay plugin CAD.

Gió không còn quy đổi p→q/F hoặc tự tính TCVN. User Loads sửa các khóa API đã đọc và giữ ánh xạ Story do người dùng đối chiếu; chưa tự tạo khóa hoặc xác định tâm cứng. Khi thử ghi, dùng bản sao model ETABS đã lưu.
