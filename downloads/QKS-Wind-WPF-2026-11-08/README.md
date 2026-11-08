# QKS Wind — WPF hai tab và User Loads theo Story / Load Sets

Build: **08/11/2026 21:16:22 (UTC+07, Việt Nam)**. Source: `73b2a483c6366f99bbc73d10cd20f498b1a8c146`.

- Tab 1: User Loads theo khóa API đã đọc; nhập/dán Excel, chỉnh giá trị và gán có read-back.
- Tab 1 → Story / Load Sets: đọc schema host, phân biệt Load Set/Story/Diaphragm, thêm/sửa/xóa, Number of Load Sets và Reversible khi schema hỗ trợ.
- Tab 2: Model 2D chọn dầm biên, q1/q2 có dấu, đoạn tải theo mét hoặc tỷ lệ I→J, preview, thay/cộng tải, Undo/Redo.
- Giữ tải ngoài đoạn thay; tách tổng lực/mô men theo pattern; lưu bộ soạn và cấu hình.
- Excel bộ soạn có header navy, border và màu theo tầng. PDF dùng tải đã đọc từ ETABS.

## Cài đặt

Đóng Excel, bỏ chọn XLL QKS cũ, giải nén và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll` trong Excel Add-ins. Chỉ bật một bản QKS. Dành cho Excel desktop 64-bit trên Windows. Mở QKS → Tải gió, kết nối ETABS và **Đọc lại ETABS**.

## Phạm vi kiểm thử

**475/475 kiểm thử tự động đạt; build 0 lỗi, 0 cảnh báo.** Chưa nghiệm thu WPF và ETABS trên Windows. Đây là bản kiểm thử tích hợp, chưa chứng nhận hỗ trợ mọi phiên bản/schema ETABS.

Bảng nâng cao chỉ mở ghi khi nhận diện được schema và đơn vị. Các field thiếu/mơ hồ không được suy đoán. Dùng **Thông tin API** để xuất schema thực nếu host chưa được nhận diện.

API `ApplyEditedTables` áp dụng edit buffer toàn cục. Chế độ bảng yêu cầu xác nhận hủy các chỉnh sửa bảng ETABS khác chưa Apply trước khi bắt đầu phiên QKS. Ghi xong hoặc có lỗi giữa chừng phải đọc lại model; journal lưu bên cạnh backup.

Chưa bổ sung tính gió theo tiêu chuẩn, tự phân phối lực tầng xuống dầm, tự nhận diện dầm biên hoặc PDF lực diaphragm. Xem `HUONG-DAN-QKS-WIND.md` để biết phạm vi và bộ nghiệm thu host.

XLL SHA-256: `ed1c8957a030b5b60d0dbc7af4b7422e464f6777f3a988814a79fbf76d254ca2`.
