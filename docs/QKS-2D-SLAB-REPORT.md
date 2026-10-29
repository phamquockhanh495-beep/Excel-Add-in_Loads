# QKS 2D — Ma trận tải sàn theo tầng và chú giải định dạng

## Chú giải tải sàn

- Tên **Load Pattern** in đậm ở đầu mỗi dòng.
- Tên **Load Set** in đậm; không lặp tiền tố kỹ thuật `Area uniform set:` trước tên set.
- Phần hệ trục / hướng tải in nghiêng.
- Giá trị và đơn vị dùng màu đang thiết lập tại **Display → Text → chữ tải**. Khi bật RGB riêng, màu cập nhật ngay sau Áp dụng; khi tắt, dùng màu mặc định Shell (RoyalBlue).
- Trên ô sàn vẫn chỉ hiện tên Pattern hoặc tên Set. Chú giải giữ đầy đủ Pattern thành phần của set, kể cả giá trị bằng 0. Chú giải cuộn khi có nhiều dòng.

## Xuất `.xlsx`

1. Đọc mô hình qua API, chọn **Load Pattern** hoặc **Load Sets** trong Quản lý tải trọng.
2. Chọn Pattern/Set cụ thể hoặc Tất cả.
3. Mở **XUẤT TẢI SÀN · .XLSX**:
   - **Xuất tầng đang xem:** chuyển về Plan, chọn tầng rồi xuất tải sàn có Story tương ứng.
   - **Xuất toàn bộ tầng:** xuất mọi tầng từ dữ liệu mô hình đã đọc, dùng bộ lọc Pattern/Set hiện tại.
4. Chọn đường dẫn lưu `.xlsx`. Thanh trạng thái thông báo số bản ghi nguồn và đường dẫn.

Phạm vi xuất là **sàn**, gồm cả tải bằng 0; không phụ thuộc zoom, vùng cắt, ẩn/isolate, checkbox Shell hay “Chỉ đối tượng đang chọn”. Tầng dùng trường Story của đối tượng. Load Sets xuất đầy đủ Pattern thành phần và bỏ qua bộ lọc Pattern đang nhớ.

## Bố cục mới

Workbook chỉ còn **Tong hop** và một sheet cho mỗi tầng. Tất cả dùng chung ma trận:

| STT | Story | Load Sets | Pattern 1 | Pattern 2 | … |
|---|---|---|---|---|---|
| 1 | Roof | Balcony | 3 kN/m² | 1 kN/m² | |
| 2 | T39 | Residential | | 2 kN/m² | |

Ví dụ trên minh họa bố cục, không phải tải gán mẫu. File thực tế lấy tên Pattern/Set và giá trị từ mô hình đã đọc.

- Tiêu đề chính nền navy, chữ trắng đậm; `Load Pattern` gộp ngang trên các cột Pattern. STT, Story, Load Sets gộp dọc qua hai hàng tiêu đề.
- Pattern có hàng tiêu đề phụ nền trắng; bảng có đường viền mảnh, căn giữa theo chiều dọc, tự xuống dòng. Cố định hai hàng tiêu đề và ba cột đầu khi cuộn.
- Tầng xếp từ cao xuống thấp. Cột Pattern lấy theo tên thực tế trong phạm vi xuất và sắp theo tên; không có danh sách tải giả định cố định.
- Bỏ sheet **Thong tin** và các cột **Step**, **Số bản ghi**. Không có cột đếm trong ma trận.
- Một dòng cho mỗi cặp Story / Load Set. Tải gán trực tiếp được tách vào dòng `(Gán trực tiếp)`.
- Nếu một Pattern có nhiều giá trị hoặc hướng/loại tải trong cùng tầng/set, ô tương ứng liệt kê từng trường hợp trên dòng riêng, không tự cộng hoặc chỉ lấy giá trị đầu tiên. Các giá trị trùng nhau trên nhiều ô sàn chỉ xuất một lần trong ô phân loại; đây là bảng phân loại, không phải bảng kiểm kê từng UniqueName.
- Tải uniform Gravity một giá trị được lưu dạng số với định dạng đơn vị, ví dụ `3 kN/m²`. Ô nhiều trường hợp dùng văn bản xuống dòng; các trường hợp khác uniform Gravity ghi thêm hướng và loại tải để phân biệt.
- Ô trống là không có gán tải trong phạm vi đã đọc; giá trị 0 thực tế vẫn ghi `0` kèm đơn vị. Không cộng tải từ set với tải trực tiếp, không quy đổi tổng lực.
- Tầng không có tải theo bộ lọc vẫn có sheet với hai hàng tiêu đề. Tên sheet được xử lý ký tự đặc biệt/trùng tên; cột Story giữ tên gốc.

File Excel dùng dữ liệu lần đọc gần nhất; nếu mô hình thay đổi, **Đọc lại** trước khi xuất. Các cảnh báo đọc API xem trong **Phạm vi dữ liệu đã đọc** của chương trình. Ghi file tạm rồi thay file đích khi hoàn tất; lỗi ghi không làm hỏng file có sẵn.

## Kiểm tra

Kiểm tra tự động gồm ma trận một tầng/toàn bộ tầng, bộ lọc độc lập, nhiều giá trị/hướng, số âm/0, đơn vị, tiêu đề gộp, cột sau Z, tên bắt đầu bằng `=`, tên sheet đặc biệt, ghi file lỗi và thay đổi RGB chú giải. Workbook được đọc lại bằng openpyxl độc lập.

Môi trường cloud chưa chạy trực tiếp WPF/Excel/ETABS trên Windows; cần nghiệm thu giao diện, màu RGB chú giải và kết nối API trên máy sử dụng.
