# QKS 2D — Nhãn tải sàn và báo cáo Excel theo tầng

## Hiển thị

- **Load Pattern:** trên ô sàn chỉ có tên Pattern. Khi xem tất cả, tên các Pattern được ghép bằng dấu `/`, không lặp tên. Giá trị, đơn vị, hướng và nguồn tải giữ trong chú giải màu góc dưới trái.
- **Load Sets:** trên ô sàn chỉ có tên set. Chú giải ghi tên set và từng Load Pattern thành phần với giá trị có dấu, đơn vị, hệ trục/hướng, kể cả thành phần bằng 0. Lựa chọn Load Pattern không lọc các thành phần này.
- Màu phân loại và màu chọn đối tượng giữ cơ chế trước. Hai chế độ hiển thị vẫn độc lập. Chú giải có thanh cuộn khi nhiều nội dung.

## Xuất `.xlsx`

1. Đọc mô hình qua API, chọn **Load Pattern** hoặc **Load Sets** trong Quản lý tải trọng.
2. Chọn Pattern/Set cụ thể hoặc Tất cả.
3. Mở **XUẤT TẢI SÀN · .XLSX**:
   - **Xuất tầng đang xem:** chuyển về Plan, chọn tầng rồi xuất toàn bộ tải sàn có Story tương ứng. Nếu đang xem mặt cắt, chương trình yêu cầu chuyển về Plan vì mặt cắt có nhiều tầng.
   - **Xuất toàn bộ tầng:** lấy mọi tầng từ dữ liệu mô hình đã đọc, dùng bộ lọc Pattern/Set hiện tại.
4. Chọn đường dẫn lưu `.xlsx`. Thanh trạng thái thông báo số bản ghi và đường dẫn.

Phạm vi xuất là **sàn**, gồm cả tải bằng 0; không phụ thuộc zoom, vùng cắt, ẩn/isolate, checkbox Shell hay “Chỉ đối tượng đang chọn”. Mục này ghi rõ ngay trên giao diện. Tầng dùng trường Story của đối tượng. Mỗi chế độ xuất một file riêng; Load Sets xuất đầy đủ các Pattern trong set và bỏ qua bộ lọc Pattern đang nhớ.

## Các sheet

- **Thong tin:** mô hình, phiên bản nguồn, thời điểm đọc/xuất UTC, chế độ, bộ lọc, phạm vi tầng, số bản ghi, quy tắc xuất và cảnh báo đọc API.
- **Tong hop:** phân nhóm theo Story, Load Set, Pattern, loại tải, hệ trục, hướng, giá trị, đơn vị và step; đếm số ô sàn khác nhau và số bản ghi. Đây là bảng thống kê, không phải tổng lực quy đổi từ diện tích sàn.
- **Một sheet cho mỗi tầng:** Story, UniqueName, Label, Section Property, Load Set, Pattern, loại tải, hệ tọa độ, Direction API, Giá trị 1/2, đơn vị, Step. Mỗi dòng tương ứng một bản ghi tải gốc. Tên tầng dài/ký tự đặc biệt/trùng tên sheet được chuẩn hóa; cột Story giữ nguyên tên gốc. Tầng không có tải theo bộ lọc vẫn có tiêu đề bảng.

Giá trị tải là ô số, giữ dấu và độ chính xác gốc. ID/nhãn là ô chữ để giữ số 0 đầu và không biến tên bắt đầu bằng `=` thành công thức. Bảng có tiêu đề navy, cố định hàng đầu và AutoFilter. Không tự cộng tải gán trực tiếp với tải từ Uniform Load Set, không tự suy ra tải còn thiếu. Ghi file tạm rồi thay file đích khi hoàn tất; lỗi ghi không làm hỏng file có sẵn.

## Kiểm tra

- Chọn LLAB: nhãn trên sàn chỉ có LLAB, còn giá trị nằm trong chú giải.
- Chọn Load Sets và một set có SDL/LL: nhãn chỉ có tên set; chú giải có cả SDL và LL ngay cả khi bộ lọc Pattern đang chọn một tên khác.
- Xuất một tầng rồi toàn bộ tầng; đối chiếu Story, UniqueName, Pattern, Set và giá trị với ETABS. Thử set có giá trị 0 và tải có dấu âm.
- File Excel dùng dữ liệu lần đọc gần nhất; nếu mô hình thay đổi, Đọc lại trước khi xuất. Các cảnh báo đọc API được đưa vào sheet Thong tin.

Đã kiểm tra tự động core/view-model và cấu trúc workbook bằng bộ đọc XLSX độc lập. Môi trường cloud chưa chạy trực tiếp WPF/Excel/ETABS trên Windows; cần nghiệm thu giao diện và kết nối API trên máy sử dụng.
