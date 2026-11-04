# QKS — SAFE và bố cục Loading Case Plan

[Tải ZIP](QKS-SAFE-PDF-Layout-2026-11-04.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source: `df84891bceea968b2348f21cc546778c87253186`. Build: **04/11/2026 21:26:20 (UTC+07, Việt Nam)**.

- Kết nối API liệt kê cả ETABS và SAFE, nhận SAFE v18–v23 qua SDK từ thư mục bản cài đang chạy. Chọn đúng model/PID và workbook Excel. Có Browse file `.fdb`.
- SAFE dùng cho Model 2D / báo cáo chỉ đọc. Không ghi tải, đổi đơn vị hoặc chạy phân tích. Khi thiếu Story, dùng cao độ hình học.
- Xuất PDF: ba nút Giấy / Header-Footer / Nội dung mở popup riêng có Áp dụng/Hủy; header/footer chia tab.
- Preview tự vừa trang theo cửa sổ, có nút Vừa trang và zoom; bỏ panel thông tin dữ liệu đã đọc, giữ cảnh báo trong nút riêng.

**Cài đặt:** Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Excel desktop 64-bit trên Windows.

**SAFE:** mở SAFE và model, vào Kết nối API → Quét lại → chọn phiên SAFE và workbook → Kết nối API → Model 2D. Nếu Browse mở SAFE mới, chờ model nạp xong rồi Quét lại.

**PDF:** Đọc mô hình → Quản lý tải trọng → Xuất PDF · Loading Case Plan → chọn tầng/tải → mở các popup thiết lập → Cập nhật xem trước → Xuất PDF.

412/412 kiểm tra tự động đạt, build 0 lỗi/cảnh báo. **Chưa nghiệm thu trực tiếp SAFE 18–23, WPF hoặc Excel trên Windows.** Adapter được kiểm thử bằng API giả lập, chưa có bằng chứng tất cả getter của từng bản SAFE hoạt động thực tế. API thiếu/khác chữ ký báo -32001; không coi dữ liệu thiếu là 0. Xem HUONG-DAN-SAFE-VA-LAYOUT.md để đối chiếu model thật trước khi dùng báo cáo.

Gói giữ PDF vector/font tiếng Việt, hướng dẫn, giấy phép và PDF mẫu dùng dữ liệu minh họa. PDF hiện xuất Plan, chưa gồm mặt cắt và lưới trục ETABS. BUILD-INFO.txt ghi nguồn, giờ build, SHA-256; timestamp ZIP theo giờ Việt Nam.
