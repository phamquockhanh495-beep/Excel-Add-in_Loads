# QKS 2D — Tô màu tải sàn, chế độ Load Sets độc lập và bảng màu

## Quản lý tải sàn

Chọn radio **Load Pattern** hoặc **Load Sets**. Hai chế độ nhớ lựa chọn riêng; bộ chọn của chế độ không hoạt động sẽ bị khóa.

- **Load Pattern**: chọn pattern, bật Shell. Ô sàn được tô màu theo tổ hợp các bản ghi tải đang hiển thị: pattern, loại nguồn tải, hệ trục, hướng, giá trị có dấu và đơn vị. Nhãn trên ô sàn chỉ thể hiện tên Pattern; giá trị từng bản ghi nằm trong chú giải, không tự cộng tải khác loại. Frame/Joint vẫn bật/tắt riêng.
- **Load Sets**: chọn một set hoặc tất cả. Chỉ hiện gán set của Shell, bỏ qua Load Pattern đang nhớ. Các thành phần trong cùng set được gom thành một gán trên mỗi đối tượng; nhãn và dòng quản lý chỉ hiện tên set; chú giải bổ sung các Pattern thành phần, giá trị, hướng và đơn vị. Thành phần bằng 0 không làm mất gán set. Checkbox Frame/Shell/Joint và lọc zero được khóa trong chế độ này.
- Ô sàn có tải dùng màu phân loại thay màu Object/Property; các cấu kiện khác giữ màu hiện tại. Sàn đang chọn vẫn nổi bật đỏ đô. Phần tô giữ lỗ mở và cách chiếu hình học hiện có.
- Không dùng chuỗi ký hiệu ⊗ cho tải sàn; thay bằng màu ô và nhãn. Góc dưới trái vùng vẽ có chú giải ô màu, tên set hoặc nội dung tải. Chú giải cuộn khi nhiều nhóm. Màu mang tính phân loại, không phải thang đo cường độ. Những màu gần nhau phải đối chiếu nhãn/chú giải.
- Chỉ đối tượng đang chọn vẫn lọc được cả hai chế độ. Tắt “Hiện nhãn tải (sàn: tên Pattern / Set)” chỉ ẩn nhãn, vẫn giữ màu và chú giải.

## Display và chọn màu

Display mở cửa sổ riêng dùng headbar navy, nút dạng Material Design như Select, gồm Text, Color Objects và ẩn/hiện panel vùng xem.

- **Color Objects:** click ô màu của từng dòng để mở bảng màu Windows (màu chuẩn và màu tùy chỉnh RGB). OK đưa màu về các ô R/G/B; Cancel không thay màu. Bấm Áp dụng để cập nhật mô hình và lưu cấu hình.
- **Text:** dùng nút chọn màu chữ cấu kiện hoặc chữ tải; có bảng màu tương tự. Khi chọn màu tải, tùy chọn RGB riêng được bật. Bấm Áp dụng để lưu.
- Bảng chọn màu dùng hộp thoại màu tiêu chuẩn Windows, không phải bảng màu Office trong ảnh tham khảo. Vẫn nhập số RGB trực tiếp được.

## Kiểm tra

Cài XLL mới, kết nối API và Đọc lại. Thử một pattern có nhiều giá trị tải sàn; kiểm tra màu và chú giải. Chuyển Load Sets với một pattern khác đang nhớ: tên set phải vẫn hiện đầy đủ, mỗi ô chỉ một tên cho một set. Chọn nhiều set/đối tượng, zoom, đổi tầng và so sánh với ETABS. Kiểm tra OK/Cancel bảng màu và Áp dụng trong cả Text/Color Objects.

Core/VM tests và biên dịch WPF chạy trong cloud; chưa chạy Excel/ETABS/WPF trên Windows. Tải/API không thay đổi; thiếu gán Load Sets vẫn phải xem thông báo đọc API, không suy đoán từ property.

## Xuất Excel

Xem [hướng dẫn xuất tải sàn theo tầng](QKS-2D-SLAB-REPORT.md).
