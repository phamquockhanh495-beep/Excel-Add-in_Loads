# QKS — tối ưu Model 2D và Tải gió

Build: **15/11/2026 16:02:50 (UTC+07, Việt Nam)**. Source: `1fc23e5327dee943b90944434672bedc313969ac`.

Đây là XLL chung của Extension QKS, gồm Model 2D, Tải gió và các chức năng hiện có.

- Tab Diaphragms chỉ đọc tầng/pattern; không quét toàn bộ frame/area khi mở ở Tab 1.
- Cache dùng chung cho Model 2D và tab dầm biên trong cùng phiên Excel; giữ bảng gió đang soạn khi chuyển tab.
- Đọc tọa độ và tải theo lô khi host hỗ trợ; tự chuyển về từng đối tượng nếu bulk lỗi/không hỗ trợ.
- Chỉ mục cao độ giảm số cấu kiện phải chiếu trên từng tầng; giữ cấu kiện xuyên tầng và Extrude.
- Đọc lại bỏ cache; thao tác ghi tải QKS làm hết hiệu lực cache. Trước Gán ETABS vẫn kiểm tra nguồn trực tiếp, backup và read-back.
- Log đo thời gian: `%LOCALAPPDATA%\QKS\Logs\model-read.log`.

## Cài đặt

Đóng Excel, bỏ chọn XLL QKS cũ, giải nén gói này. Trong Excel Add-ins, Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS. Dành cho Excel desktop 64-bit trên Windows.

## Sử dụng cache

Để mở nhẹ phần Diaphragms, đóng Wind ở Tab 1. Nếu nhớ cấu hình Tab 2, lần mở sau sẽ chuẩn bị hình học Tab 2. Lần đọc mô hình đầu vẫn cần thời gian. Đóng/mở cửa sổ trong cùng phiên Excel có thể dùng lại dữ liệu; tắt Excel sẽ xóa cache.

Sau khi sửa trong ETABS/SAFE, bấm **Đọc lại** trước khi xem hoặc xuất hồ sơ mới. QKS chưa có revision API toàn cục để tự phát hiện mọi thay đổi ngoài chương trình. Nút Đọc lại hiện tại đồng bộ lại phạm vi đầy đủ của màn hình; chưa tách nút riêng hình học/từng nhóm tải.

## Kiểm chứng

**504/504 kiểm thử tự động đạt; build 0 lỗi, 0 cảnh báo.** Đã kiểm tra tệp ZIP và checksum. Chưa nghiệm thu trực tiếp Excel/ETABS/SAFE/WPF trên Windows; chưa cam kết thời gian mở dưới 1 giây. Xem `HUONG-DAN-TOI-UU-MODEL.md` và log trong `verification`.

XLL SHA-256: `cc3d4bdcd932d098c260419c635fb57be4d5260d5bdd3508a0bcf06de2c2aed7`.
