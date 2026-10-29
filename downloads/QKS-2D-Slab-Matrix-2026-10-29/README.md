# QKS 2D — Ma trận Excel và định dạng chú giải tải sàn

[Tải ZIP](QKS-2D-Slab-Matrix-2026-10-29.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `3c1b397a0f5119f32113d5ff9cd764115ef9b0bb`. Build: **29/10/2026 15:46:04 (UTC+07, Việt Nam)**.

- Excel: ma trận STT, Story, Load Sets và các Load Pattern theo cột; header hai hàng navy, gộp ô, viền mảnh, cố định hàng/cột. Tầng xếp từ cao xuống thấp.
- Bỏ sheet Thong tin, cột Step và Số bản ghi. Còn Tong hop và các sheet theo tầng, cùng định dạng ma trận.
- Chú giải: Pattern đầu dòng và tên Load Set in đậm; hệ trục/hướng in nghiêng; giá trị theo màu chữ tải tại Display → Text.
- Nhiều giá trị trong cùng tầng/set/pattern được liệt kê riêng từng dòng trong ô, không tự cộng. Giá trị uniform Gravity đơn lẻ vẫn là ô số có định dạng đơn vị.

Mở Quản lý tải trọng → XUẤT TẢI SÀN · .XLSX. Xuất tầng hiện tại cần ở Plan. Bộ lọc Pattern/Set và phạm vi xuất giữ nguyên.

Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows. Đọc lại mô hình nếu ETABS đã thay đổi.

385/385 tests đạt; build 0 lỗi/cảnh báo. Workbook đã được đọc kiểm tra bằng openpyxl. Chưa chạy nghiệm thu WPF/Excel/ETABS trực tiếp trên Windows. Chi tiết trong HUONG-DAN-KIEM-TRA.md của ZIP.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
