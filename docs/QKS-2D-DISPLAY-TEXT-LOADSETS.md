# QKS 2D — Display/Text, Uniform Load Sets và giao diện popup

## Các thay đổi

1. Popup Select và Display/Text dùng chung headbar navy đen, vuông, nút thu nhỏ/phóng to/đóng; hỗ trợ kéo và resize. Popup Settings có tiêu đề navy cùng hệ thống.
2. Bỏ dòng trạng thái nguồn bên dưới kết nối, nút Delete/Khôi phục và bộ đếm trên toolbar, nhãn U/V/Chọn ở góc dưới vùng vẽ. Thông báo thao tác và đối tượng đang chọn vẫn nằm trên statusbar đáy cửa sổ. Delete vẫn dùng phím; Hide/Show All nằm trong menu chuột phải.
3. Bánh răng Settings được neo bên phải thanh công cụ của vùng vẽ, tách khỏi nhóm nút bên trái.
4. Menu chuột phải dùng chữ 11.5 DIP, phím tắt 10.5 DIP và icon Material Design (mắt, ẩn, hiện toàn bộ, bảng màu). Icon Settings màu vàng. Các lệnh/phím tắt giữ nguyên.
5. Quản lý tải có combobox **Uniform Load Sets · Shell**. Chọn một set tự bật Shell; lọc tải Shell theo set đồng thời với Load Pattern. Frame/Joint vẫn theo các checkbox riêng. Nhãn tải và tiêu đề dòng ghi tên set, không cộng lẫn các pattern. Chọn “Tất cả tải sàn” để xem cả tải trực tiếp và tải từ set.
6. Thu gọn font/giãn dòng bảng thuộc tính. Sàn/vách: `Thickness = 175 mm`; dầm/cột: `(Width × Height) = (240 × 700) mm`. Lấy kích thước API và đổi từ mét sang mm; không suy từ tên property. Với tiết diện tròn, Width/Height là đường kính và có ghi “Tròn”. Chiều dài cấu kiện và tọa độ vẫn dùng m.
7. **Display → Text** chỉnh riêng chữ cấu kiện (Label/UniqueName/Section) và chữ giá trị tải. Cỡ 6–30 DIP, R/G/B là số nguyên 0–255. Chữ cấu kiện đang chọn giữ đỏ đô. Chữ tải có thể giữ màu theo nhóm hoặc dùng RGB riêng. Điều chỉnh chữ không thay màu nét cấu kiện/mũi tên tải.

## Cách sử dụng

- Cài XLL mới từ thư mục `Excel` trong ZIP, chỉ bật một bản QKS. Kết nối API rồi mở viewer; **Đọc lại** để cập nhật danh mục/tải từ Uniform Load Sets.
- Chọn tầng → Load Pattern → Uniform Load Sets. Nếu không có tên set trong danh sách, mở “Phạm vi dữ liệu đã đọc” để kiểm tra thông báo API/schema. Danh sách chỉ chứa set đã đọc được từ gán area và định nghĩa thực tế, không lấy tên tiết diện để đoán set.
- Display → Text → nhập cỡ/màu → **Áp dụng**. Thay đổi phản ánh trên hình và lưu tại `%LocalAppData%\QKS\Viewer\text-display.json`. **Mặc định** điền lại giá trị gốc; bấm Áp dụng để xác nhận. Dữ liệu sai không được áp dụng. Nếu không ghi được file, thông báo nêu rõ chỉ áp dụng trong phiên hiện tại.
- Chức năng ẩn/hiện panel vùng xem cũ được chuyển vào mục thứ hai của menu Display.

## Kiểm tra trên Windows

- Mở Select, Text và Settings; kiểm tra headbar, kéo/resize, đóng/minimize/maximize và hiển thị ở DPI của máy.
- Chọn sàn/vách và dầm/cột, đối chiếu kích thước mm với ETABS.
- Kiểm tra Uniform Load Sets có nhiều pattern: chọn từng set/pattern và đối chiếu giá trị, vùng sàn, số bản ghi. Tải trực tiếp không xuất hiện khi đang lọc riêng một set.
- Thử RGB (255,0,0), cỡ 14 cho chữ cấu kiện; đổi chữ tải riêng, zoom, chọn/bỏ chọn và mở lại viewer để kiểm tra lưu cấu hình.
- Kiểm tra menu phải có icon, chữ gọn, vẫn Isolate/Hide/Show All và mở Settings đúng.

Build WPF và kiểm thử core/VM chạy trong cloud; chưa chạy nghiệm thu WPF/Excel/ETABS trên Windows. Đọc Uniform Load Sets phụ thuộc schema API của phiên bản ETABS; phần không đọc được có cảnh báo, không tự tạo tải hoặc thay bằng 0. Không thay đổi mô hình/tải ETABS.
