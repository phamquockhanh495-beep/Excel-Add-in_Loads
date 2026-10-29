# QKS 2D — Định dạng Excel theo tầng và Load Sets

[Tải ZIP](QKS-2D-Excel-Format-2026-10-29.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `2bcefc273abe31240398a8daa839a47302462623`. Build: **29/10/2026 17:21:02 (UTC+07, Việt Nam)**.

- Đường viền dọc xanh xám rõ hơn để phân cách các cột.
- Nền xanh nhạt / kem nhạt xen kẽ theo nhóm tầng, giữ cùng nền cho mọi dòng của tầng và sheet tầng tương ứng.
- Tên Load Sets in đậm, mỗi set một màu chữ; cùng set giữ cùng màu giữa các tầng/sheet và khi lọc trong cùng dữ liệu mô hình.
- Giữ nguyên bố cục ma trận, giá trị tải, kiểu ô số và đơn vị.

Mở Quản lý tải trọng → XUẤT TẢI SÀN · .XLSX để xuất lại bảng mới. File Excel đã xuất trước đó không tự thay đổi.

Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows.

385/385 tests đạt; build 0 lỗi/cảnh báo. Kiểm tra workbook bằng openpyxl xác nhận màu nền theo tầng, màu chữ set, viền và đơn vị số. Chưa chạy nghiệm thu WPF/Excel/ETABS trực tiếp trên Windows. Chi tiết trong HUONG-DAN-KIEM-TRA.md của ZIP.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
