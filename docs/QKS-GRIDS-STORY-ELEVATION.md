# QKS — Lưới trục và cô lập tầng trong Elevation

## Lưới trục trên Model 2D

Sau khi cập nhật XLL, kết nối đúng model rồi bấm **Đọc lại** để lấy lưới trục. Bật **Hiện lưới trục** ở panel vùng hiển thị. Trục dùng nét đứt xám và vòng tròn tên trục; không tham gia chọn/xóa cấu kiện. Tên trục và cỡ vòng tròn giữ kích thước dễ đọc khi zoom. Chế độ hiển thị được ghi nhớ khi đóng viewer.

- Plan: tên, tọa độ gốc, góc xoay, khoảng cách trục lấy từ API; quy đổi về mét giống hình học. Hỗ trợ trục Cartesian, General xiên và Cylindrical khi API cung cấp getter tương ứng. Đường tròn được chia thành đoạn vector mỗi 1 độ; không phải ảnh raster.
- Nét Cartesian kéo dài đến khung vùng kết cấu; vị trí đầu bubble phù hợp vùng xem/bản vẽ, không phải bản sao pixel của cửa sổ ETABS. Vòng tròn tên của trục cong đặt trên điểm đầu/cuối đường biểu diễn, chưa sao chép vị trí bubble tùy biến riêng trong host.
- Tôn trọng trục ẩn và phạm vi tầng đọc được. Có nhiều hệ trục thì tên thể hiện `Hệ:Trục` để tránh nhầm.
- Elevation: hiển thị vị trí giao hệ trục với mặt cắt trong đoạn A–B. Trục song song hoặc trùng đường cắt không tạo một vị trí giao giả. Góc nhìn đảo chiều vẫn giữ đúng vị trí tương đối.
- SDK cũ có thể chỉ hỗ trợ `GetGridSys_2`: dùng Cartesian, kèm cảnh báo vì chưa có dữ liệu phạm vi tầng và General. SAFE dùng cùng bridge đọc SDK đã cài; nếu không có getter tương thích, báo rõ dữ liệu trục chưa đọc được.

## Lưới trục trong PDF

Mở **Xuất PDF · Loading Case Plan → 4. Nội dung bản vẽ → Hiện lưới trục**, bấm Áp dụng rồi Cập nhật xem trước. Tùy chọn PDF độc lập với checkbox hiển thị trục trong Model 2D và được lưu cùng cấu hình in.

PDF dùng chính bộ dữ liệu trục đã đọc, đường nét và tên trục là vector. Hệ trục lọc theo cao độ từng trang. Khi dữ liệu không có lưới trục, nút Cảnh báo giải thích để đọc lại/kiểm tra API; chương trình không vẽ trục suy đoán. Xuất PDF vẫn là Plan; đợt này không bổ sung xuất mặt cắt.

## Chỉ tầng đang chọn trong Elevation

1. Chọn tầng/cao độ ở panel trái, mở mặt cắt đã lưu hoặc chọn hai điểm A–B.
2. Trong Elevation, bật **Chỉ tầng đang chọn**.
3. Phạm vi hiển thị lấy từ cao độ tầng ngay dưới đến cao độ tầng đang chọn. Dầm/sàn tại tầng đang chọn vẫn giữ chiều dày hướng xuống và cote đỉnh hiện có. Cột/vách xuyên tầng bị cắt đúng khoảng này. Dầm/sàn tại tầng dưới và cấu kiện chỉ nằm ở tầng trên không được vẽ thêm.
4. Tải cũng cắt theo phạm vi cấu kiện và đoạn A–B; không sửa giá trị/tọa độ gốc. Giá trị tải biến thiên tại các vị trí mũi tên được nội suy theo đoạn gán gốc.
5. Đổi tầng bằng ComboBox hoặc **Page Up / Page Down**; cửa sổ tự vừa vùng tầng mới. Bỏ tick để trở lại toàn bộ cao độ của mặt cắt. Khi quay về Plan, tùy chọn này không lọc thêm dữ liệu.

Cơ chế dùng cao độ hình học, không chỉ lọc theo trường Story, nên cột/vách dài xuyên nhiều tầng vẫn được giữ phần giao. Tầng thấp nhất không có tầng dưới: lấy biên dưới thực của cấu kiện đi qua cao độ đó, không tự gán chiều cao tầng. Với SAFE không có Story, các cao độ `Z=… m` đã đọc đóng vai trò tầng.

Tùy chọn không thay thế Hide/Isolate theo đối tượng; các bộ lọc này vẫn có hiệu lực. Bỏ tick Chỉ tầng đang chọn không tự khôi phục những đối tượng đã ẩn riêng trước đó.

## Kiểm chứng

Kiểm tra tự động bao gồm chuyển hệ tọa độ/đơn vị, trục ẩn và phạm vi tầng, giao mặt cắt đảo hướng, trục tròn, cột xuyên tầng, chiều dày sàn, đoạn tải biến thiên, đổi tầng, ghi nhớ tùy chọn và PDF có/không có trục. PDF minh họa dùng dữ liệu nhân tạo, được đọc và raster hóa kiểm tra bằng trình đọc độc lập.

Chưa nghiệm thu trực tiếp WPF, ETABS hoặc SAFE trên Windows. Cần đối chiếu một hệ trục xoay và một mặt cắt trên model thực, đặc biệt các phiên bản SAFE/SDK có khác biệt chữ ký. Chữ ký getter dùng khi build được kiểm tra từ CSiAPIv1.dll đi kèm kho nguồn.
