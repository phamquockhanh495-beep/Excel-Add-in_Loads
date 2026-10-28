# QKS 2D — Mặt cắt A–B và quản lý tải đã gán

[Tải ZIP](QKS-2D-MatCat-Tai-2026-10-28.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `51940fb8399bb03a55ee6d19be7c960b8ce15971`. Build: **28/10/2026 13:54:12 (UTC+07, Việt Nam)**.

Đường định vị A–B, mặt cắt hữu hạn, đỉnh dầm/sàn cùng cote mô hình; cột/vách giữ đường bao khi tắt tô Extrude; màu chọn đỏ đô; quản lý và hiển thị tải Frame/Shell/Joint đồng thời.

Đóng Excel, bỏ chọn XLL QKS cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Dùng Excel desktop 64-bit trên Windows. **Đọc lại mô hình** để lấy thêm tải tập trung frame và tải nút. Bên phải → QUẢN LÝ TẢI TRỌNG → chọn Pattern và tick các nhóm cần xem.

335/335 core/VM tests đạt, build 0 lỗi/cảnh báo. Chưa nghiệm thu WPF/Excel/ETABS thực tế trên Windows. Các ký hiệu tải đánh dấu vị trí/phạm vi với dữ liệu gán gốc, chưa phải biểu đồ tỷ lệ lực. Xem tài liệu đi kèm về phạm vi đọc API và giới hạn hình học.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
