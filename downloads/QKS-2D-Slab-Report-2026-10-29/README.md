# QKS 2D — Nhãn tải sàn và xuất Excel theo tầng

[Tải ZIP](QKS-2D-Slab-Report-2026-10-29.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `eacf01de3bad4aa803c2f96fb3e4385599a7da43`. Build: **29/10/2026 14:02:10 (UTC+07, Việt Nam)**.

- Load Pattern: nhãn sàn chỉ có tên Pattern; giá trị nằm trong chú giải.
- Load Sets: chú giải có đầy đủ Pattern, giá trị, đơn vị và hướng thành phần; nhãn sàn vẫn chỉ tên set.
- Xuất .xlsx tải sàn theo tầng hiện tại hoặc toàn bộ tầng; sheet thông tin, tổng hợp và chi tiết từng tầng. Bộ lọc theo chế độ/Pattern/Set đang chọn, không phụ thuộc ẩn/chọn/zoom.

Mở Quản lý tải trọng → XUẤT TẢI SÀN · .XLSX. Xuất tầng hiện tại cần ở Plan. Tải bằng 0 vẫn xuất. Không tự cộng tải từ các nguồn.

Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows. Đọc lại mô hình nếu ETABS đã thay đổi.

382/382 tests đạt; build 0 lỗi/cảnh báo. Workbook đã được đọc kiểm tra bằng openpyxl. Chưa chạy nghiệm thu WPF/Excel/ETABS trực tiếp trên Windows. Chi tiết trong HUONG-DAN-KIEM-TRA.md của ZIP.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
