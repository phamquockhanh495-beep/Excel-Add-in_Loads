# QKS — Lưới trục và cô lập tầng Elevation

[Tải ZIP](QKS-Grids-Story-Elevation-2026-11-07.zip?raw=true) · [PDF minh họa](QKS-Grid-Plan-DEMO.pdf) · [SHA-256](SHA256SUMS.txt)

Source: `93553fb96e5e50e7f395e9da6f5bfb82c30c7174`. Build: **07/11/2026 22:42:52 (UTC+07, Việt Nam)**.

- Lưới trục từ GridSys: Plan, giao trục trong Elevation và PDF Loading Case Plan. Hỗ trợ tọa độ xoay, trục Cartesian/General và Cylindrical khi API cung cấp getter; tôn trọng trục ẩn và phạm vi tầng đọc được.
- Model 2D: checkbox Hiện lưới trục. PDF: popup 4. Nội dung bản vẽ → Hiện lưới trục. Các tùy chọn độc lập và được ghi nhớ.
- Elevation: checkbox Chỉ tầng đang chọn, cắt hình học và tải từ tầng dưới tới tầng đang chọn, giữ chiều dày sàn và phần giao cột/vách xuyên tầng. Page Up/Down đổi tầng; bỏ tick để xem toàn bộ mặt cắt.

**Cài đặt:** Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS, Excel desktop 64-bit trên Windows. Sau khi cập nhật hãy **Đọc lại** model để nhận lưới trục.

422/422 kiểm tra tự động đạt; build 0 lỗi/cảnh báo. PDF minh họa đã đọc và raster hóa bằng trình đọc độc lập; dữ liệu minh họa không phải model người dùng. Chưa nghiệm thu trực tiếp WPF/ETABS/SAFE trên Windows. Chữ ký GridSys được kiểm tra từ CSiAPIv1.dll; API thiếu dữ liệu có cảnh báo, không tạo trục suy đoán. SDK cũ fallback Cartesian chưa cung cấp phạm vi tầng/General. Trục tròn dùng đoạn vector mỗi 1 độ; vị trí bubble theo vùng bản vẽ, chưa sao chép vị trí bubble tùy biến riêng trong host.

Hướng dẫn đầy đủ trong HUONG-DAN-LUOI-TRUC-VA-TANG.md. PDF vẫn xuất Plan; chưa bổ sung xuất mặt cắt. BUILD-INFO.txt ghi nguồn, giờ build và SHA-256. Timestamp ZIP theo giờ Việt Nam.
