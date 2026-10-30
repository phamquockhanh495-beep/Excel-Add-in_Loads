# QKS 2D — Loading Case Plan: xuất PDF mặt bằng tải trọng

[Tải ZIP](QKS-2D-Loading-Case-Plan-2026-10-30.zip?raw=true) · [PDF minh họa](QKS-Loading-Case-Plan-DEMO.pdf) · [SHA-256](SHA256SUMS.txt)

Source: `decacd632ff573b30ec8664b3dac0f6cee963637`. Build: **30/10/2026 08:04:54 (UTC+07, Việt Nam)**.

- Xuất Load Pattern hoặc Load Sets; tầng đang xem, chọn nhiều tầng hoặc toàn bộ tầng; một PDF nhiều trang hoặc tách file theo tầng.
- Giấy A4–A0 / tùy chỉnh; Portrait/Landscape, margin, tỷ lệ Fit hoặc cố định, ghép trang có chồng mép.
- Header/footer ba vùng, biến tên tầng/Pattern/Set/ngày/số trang, logo, cỡ chữ và RGB.
- Xem trước từng trang, chú giải có trang tiếp, hủy tác vụ; ghi nhớ cấu hình in trên máy.
- Nét và chữ vector, font tiếng Việt nhúng; DLL PDF và font đóng gói trong XLL.

**Cài đặt:** Đóng Excel, bỏ chọn XLL cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Dùng Excel desktop 64-bit trên Windows.

**Sử dụng:** Đọc mô hình → Quản lý tải trọng → Xuất PDF · Loading Case Plan → chọn tầng/tải/giấy → Cập nhật xem trước → Xuất PDF. Chi tiết trong HUONG-DAN-XUAT-PDF.md.

404/404 tests đạt; build 0 lỗi/cảnh báo. PDF mẫu đã kiểm tra bằng trình đọc độc lập, dùng dữ liệu minh họa, không phải mô hình của người dùng. Chưa chạy nghiệm thu WPF/Excel/ETABS trực tiếp trên Windows. Hiện xuất Plan; chưa xuất mặt cắt và chưa có lưới trục ETABS trong PDF vì dữ liệu đọc hiện tại chưa có lưới trục.

BUILD-INFO.txt ghi giờ UTC/VN; timestamp ZIP lưu theo giờ Việt Nam. Font và PDFsharp có giấy phép kèm gói.
