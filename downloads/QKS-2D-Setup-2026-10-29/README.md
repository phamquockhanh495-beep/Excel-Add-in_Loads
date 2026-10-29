# QKS 2D — Nhãn tải tường, ghi nhớ setup và kế hoạch PDF

[Tải ZIP](QKS-2D-Setup-2026-10-29.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `10f73b9d3ba8c2103212a1978a096c535f9e279c`. Build: **29/10/2026 22:22:03 (UTC+07, Việt Nam)**.

- Nhãn tải phân bố đứng trên Frame ở Plan: Pattern - Giá trị Đơn vị; giữ hai giá trị khi tải biến thiên.
- Lưu setup khi đóng viewer: nhãn từng loại, Extrude, chế độ màu, nhóm tải, dải xem, panel. Khôi phục tầng/Pattern/Set sau đọc lại đúng file mô hình.
- Text/RGB và Color Objects tiếp tục lưu bằng nút Áp dụng như trước.
- Kèm kế hoạch xuất PDF Loading Case Plan: giấy tùy chỉnh, margin, header/footer, preview và xuất nhiều trang. **Chưa có chức năng xuất PDF trong bản build này.**

Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows.

390/390 tests đạt; build 0 lỗi/cảnh báo. Chưa chạy nghiệm thu WPF/Excel/ETABS trực tiếp trên Windows. Xem HUONG-DAN-KIEM-TRA.md và PLAN-LOADING-CASE-PLAN-PDF.md trong ZIP.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
