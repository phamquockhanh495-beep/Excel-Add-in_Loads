# QKS 2D — Nhãn cố định, bảng thuộc tính và chuyển tầng

## Thay đổi

1. Bỏ nền trắng phía sau nhãn. Chỉ vẽ chữ, không phủ hình chữ nhật lên cấu kiện.
2. Nhãn neo ở trung điểm đường trục hoặc trọng tâm polygon đầy đủ; hướng theo cấu kiện dạng đường, chữ mặt sàn nằm ngang. Chữ giữ cỡ màn hình và khoảng cách cố định tới điểm neo. Không tìm vị trí tránh va chạm hoặc kéo về mép màn hình khi zoom/pan. Đã sửa trường hợp polygon Extrude khiến chữ dầm đứng bị xoay ngang, và ngưỡng tính theo pixel khiến dầm ngắn đổi cách đặt nhãn lúc zoom.
3. Bảng thuộc tính: tiêu đề đậm, ô nhận diện nổi bật, các dòng tên–giá trị có khoảng cách và đường phân cách; tọa độ trong mục mở rộng. Đối tượng sàn không có dòng chiều dài dầm/góc local N/A. Khi chọn nhiều, hiển thị thống kê từng loại; bỏ chọn không giữ thông tin cũ.
4. Bỏ cụm Đối tượng nguồn / Tải đã đọc / Chẩn đoán và splitter phía dưới. Vùng vẽ chiếm toàn bộ chiều cao còn lại. Thanh trạng thái chân cửa sổ giữ thông tin chọn và vùng xem.
5. Page Up lên tầng, Page Down xuống tầng trong Plan, theo cao độ thực, không theo thứ tự tên. Không vòng từ tầng trên cùng về dưới cùng. Giữ zoom/pan, bỏ lựa chọn của tầng trước và hủy thao tác chọn hai điểm khi chuyển tầng thành công. Không áp dụng khi đang đọc, ở Elevation, nhập văn bản hoặc mở dropdown.

## Cách kiểm tra Windows

- Đóng Excel, thay XLL bằng bản trong gói mới rồi mở lại; chỉ bật một bản QKS. Kết nối API và mở cửa sổ 2D.
- Chọn Label/UniqueName/Section trong bánh răng. Kiểm tra dầm ngang, dọc, xiên, vách và sàn khi zoom/pan; không còn nền trắng và chữ không nhảy sang vị trí tránh va chạm.
- Khi zoom sâu làm điểm neo ra ngoài khung, nhãn cũng ra ngoài khung thay vì được kéo về giữa màn hình. Khi zoom rất xa, nhãn có thể chồng nhau; tắt bớt loại nhãn trong settings nếu cần.
- Chọn một dầm/sàn, chọn nhiều và Esc; xác nhận tiêu đề, property, tiết diện, tọa độ và số bản ghi tải cập nhật đúng.
- Click vùng vẽ rồi dùng Page Up/Down; kiểm tra dropdown tầng và hình cùng đổi. Thử tầng biên, Elevation, ô tìm kiếm và dropdown đang mở.
- Xác nhận không còn cụm ba tab dưới vùng vẽ, cửa sổ vẫn nằm trong màn hình ở DPI 100/125/150%.

## Phạm vi kiểm chứng

Core/VM: 313 tests đạt, bao gồm neo nhãn qua zoom/pan, hướng dầm Extrude, chuyển tầng và dữ liệu/notification của bảng thuộc tính. Build và log kèm trong ZIP. Cloud không chạy được WPF/Excel/ETABS trên Windows; cần kiểm tra tương tác thực tế theo các bước trên.

Các giới hạn Extrude từ bản trước vẫn áp dụng: tiết diện quanh trục/mặt phân tích, chưa xét insertion/end offsets và hình tiết diện đặc biệt. Workspace chỉ đọc.
