# QKS 2D — Nét tải tường và biên sàn trên Plan

[Tải ZIP](QKS-2D-NetTaiTuong-2026-10-28.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `3531c9d24ad65468315df884bdd091f1d3c545cf`. Build: **28/10/2026 21:50:33 (UTC+07, Việt Nam)**.

Tải phân bố đứng trên dầm (bao gồm tải tường) trên Plan dùng nét đỏ đô chạy đúng chiều dài đoạn gán tải, thay cho ký hiệu ⊗/⊙. Ẩn viền sàn thường trên Plan, vẫn giữ màu nền, nhãn và viền khi chọn. Elevation giữ mũi tên tải và đường bao chiều dày sàn.

Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows. **Đọc lại mô hình**, chọn pattern, tick Frame/Shell/Joint để kiểm tra tải.

353/353 core/VM tests đạt; build 0 lỗi/cảnh báo. Chưa chạy nghiệm thu WPF/Excel/ETABS thực tế trên Windows. SAFE adapter chưa nghiệm thu. Chi tiết sử dụng và giới hạn trong HUONG-DAN-KIEM-TRA.md của ZIP.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam.
