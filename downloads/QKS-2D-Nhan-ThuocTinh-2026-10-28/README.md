# QKS 2D — Nhãn cố định, thuộc tính và chuyển tầng

[Tải ZIP](QKS-2D-Nhan-ThuocTinh-2026-10-28.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `eced3d326f04b856b3877f0ddd57708044d0fd18`. Build: **28/10/2026 12:21:48 (UTC+07, Việt Nam)**.

Bỏ nền trắng nhãn, neo chữ theo cấu kiện khi zoom/pan; bảng thuộc tính có tiêu đề/giá trị nổi bật; bỏ ba tab dưới vùng vẽ; Page Up/Down chuyển tầng trong Plan.

Đóng Excel, bỏ chọn add-in QKS cũ, giải nén rồi Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ nạp một bản QKS; dùng Excel desktop 64-bit trên Windows. Kết nối API và mở mô hình 2D. Xem hướng dẫn trong ZIP.

313/313 core/VM tests đạt; build 0 lỗi/cảnh báo. Chưa chạy nghiệm thu WPF/Excel/ETABS Windows. Nhãn ra ngoài khung thì bị cắt/ẩn tại vị trí thực, không kéo về giữa hình; có thể chồng chữ khi zoom xa.

BUILD-INFO.txt ghi giờ UTC và Việt Nam. Thời gian entry trong ZIP được lưu theo giờ Việt Nam; ZIP không mang thông tin múi giờ.
