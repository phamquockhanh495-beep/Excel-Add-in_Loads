# QKS 2D — Hướng tải và mặt cắt có tên

[Tải ZIP](QKS-2D-HuongTai-MatCat-2026-10-28.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `ad65e354f123034ee6b8da0c4d632f6c0329a71a`. Build: **28/10/2026 14:56:59 (UTC+07, Việt Nam)**.

Mũi tên Frame/Shell/Joint theo hướng và độ lớn; ký hiệu vào/ra mặt phẳng cho tải đứng trên Plan; Joint dạng x nhỏ. Mặt cắt có tên lưu/mở lại theo file ETABS, ẩn đường cắt khi xem Plan. Bổ sung nguồn tải sàn Uniform Load Sets và Uniform-to-Frame; phân biệt số tải trong nguồn và trong vùng xem.

Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows. **Đọc lại mô hình**, chọn pattern, tick Frame/Shell/Joint để kiểm tra tải.

353/353 core/VM tests đạt; build 0 lỗi/cảnh báo. Chưa chạy nghiệm thu WPF/Excel/ETABS thực tế trên Windows. SAFE adapter chưa nghiệm thu. Chi tiết sử dụng và giới hạn trong HUONG-DAN-KIEM-TRA.md của ZIP.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
