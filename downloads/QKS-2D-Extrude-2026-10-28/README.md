# QKS 2D — Extrude và thiết lập hiển thị

[Tải ZIP](QKS-2D-Extrude-2026-10-28.zip?raw=true) · [SHA-256](SHA256SUMS.txt)

Source commit: `ec4d357a26bb497547496f16ced8108a20c7c9c0`. Excel desktop 64-bit / Windows / .NET Framework 4.8.

Hiển thị cột, dầm, sàn, vách; Extrude chữ nhật/tròn và shell phẳng có chiều dày cố định. Settings độc lập theo loại cấu kiện, button navy và combobox có viền MaterialDesign.

Cài: đóng Excel, bỏ add-in QKS cũ trong Excel Add-ins, giải nén ZIP, Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ nạp một bản QKS. Kết nối API và mở Mô hình 2D để đọc kích thước mới; nếu cửa sổ đang mở thì chọn Đọc lại. Bánh răng → bật Extrude và các lớp/nhãn muốn xem.

Extrude quanh trục/mặt phân tích, chưa xét lệch tâm/end offsets hoặc hình tiết diện đặc biệt. Xem tài liệu kèm theo để biết phạm vi và các bước test. Workspace chỉ đọc; SAFE live chưa nghiệm thu.

Build: 0 warning / 0 error. Core/VM: 306/306 đạt. Chưa chạy WPF/Excel/ETABS trên Windows trong cloud.
