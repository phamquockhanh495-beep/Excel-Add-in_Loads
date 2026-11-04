# QKS — kết nối SAFE và bố cục Loading Case Plan

## Kết nối SAFE

Trong **Kết nối API**, danh sách **Model ETABS / SAFE** quét cả ETABS và SAFE đang mở. Chọn đúng tên sản phẩm, phiên bản, PID và file, cùng workbook Excel cần sử dụng, rồi bấm Kết nối API. Model 2D nhận kết nối này và đọc dữ liệu. Chọn file `.fdb` bằng nút Browse: nếu file đã mở thì chọn phiên tương ứng; nếu chưa mở thì Windows mở bằng SAFE đã liên kết với `.fdb`. Chờ SAFE nạp xong, bấm **Quét lại**, chọn đúng phiên và kết nối.

Adapter nhận phiên bản SAFE **18, 19, 20, 21, 22, 23** và đọc SDK `SAFEv1.dll` (hoặc `CSiAPIv1.dll`) từ chính thư mục SAFE.exe đang chạy. Không cần chép DLL SAFE vào thư mục XLL. Có nhiều phiên SAFE: ưu tiên ROT/PID; không gán kết nối chung cho một PID suy đoán. Kết nối không PID chỉ được thử nếu có đúng một tiến trình SAFE. Thông tin lỗi quét được giữ trong tooltip của dòng trạng thái Kết nối API.

SAFE cần được cài đặt/đăng ký COM API đúng trên Windows. Excel và SAFE nên chạy cùng quyền người dùng. Nếu thư mục SDK không truy cập được, SDK thiếu hoặc API không đăng ký, chương trình nêu lỗi thay vì tự đổi sang phiên ETABS. Các bản ngoài 18–23 không được nhận.

Phạm vi SAFE của đợt này: **đọc Model 2D và xuất báo cáo Excel/PDF**. Các chức năng ghi ETABS cũ không dùng kết nối SAFE; adapter chặn lời gọi ghi trước khi gọi API. Không đổi đơn vị, không tự mở khóa hoặc chạy phân tích SAFE.

Reader ánh xạ các getter có cùng chữ ký từ SDK SAFE: hình học frame/area, property/tiết diện, tải frame/shell/joint, bảng Uniform Load Sets khi API có cung cấp. Enum ánh xạ theo tên, không giả định giá trị enum giữa hai SDK giống nhau. API thiếu hoặc chữ ký không khớp báo mã `-32001`; dữ liệu tải thiếu không coi là 0. Nếu thiếu getter bắt buộc (tọa độ, đơn vị, opening…), dừng đọc và nêu lỗi thay vì dựng hình sai. Khác biệt SDK không được âm thầm bỏ qua.

Khi SAFE không cung cấp Story, các mặt bằng mang tên `Z=<cao độ> m` từ tọa độ hình học. Nhãn tầng tổng hợp này dùng đồng nhất trong chọn mặt bằng và báo cáo. Khi thiếu design orientation, frame ngang được phân loại dầm, frame đứng là cột, frame xiên là giằng; area ngang là sàn và area đứng là vách. Không suy đoán hướng local, hình tiết diện hoặc độ dày bị thiếu.

**Tình trạng kiểm chứng:** đã build net48 WPF và kiểm thử adapter bằng API giả lập (phiên bản, enum, mảng tải có dấu, chặn ghi, cao độ). Chưa có SAFE/Excel trên Windows để nghiệm thu thực tế từng phiên bản 18–23. Việc dùng SDK cài thực và build thành công không thay thế kiểm tra host. Trước khi dùng báo cáo, cần đối chiếu một model SAFE: đường dẫn/PID, đơn vị, số lượng cấu kiện, tải có dấu và Uniform Load Sets. Các loại tải ngoài phạm vi reader hiện tại vẫn không được bổ sung bằng suy diễn.

## Loading Case Plan

Cửa sổ chính giữ **1. Tầng và tải trọng**. Ba nút độc lập:

- **2. Giấy, margin và tỷ lệ…**
- **3. Header / Footer…**: chia tab Chung, Header và Footer trong popup.
- **4. Nội dung bản vẽ…**

Mỗi popup dùng headbar navy, có **Áp dụng / Hủy**. Các chỉnh sửa chỉ được đưa về cửa sổ chính khi Áp dụng; Hủy hoặc đóng popup giữ cấu hình cũ. Sau thay đổi, bấm Cập nhật xem trước. Lưu cấu hình in hoặc xuất thành công vẫn ghi nhớ cấu hình như bản trước.

Trang xem trước tự vừa vùng hiển thị sau khi dựng trang và khi thay đổi kích thước cửa sổ. Thanh zoom cho phép xem chi tiết; nút **Vừa trang** bật lại chế độ tự vừa trang. Zoom xem trước không thay tỷ lệ vật lý của PDF.

Đã bỏ vùng thông tin dữ liệu đã đọc ở dưới preview. Cảnh báo cần thiết vẫn có trong nút **Cảnh báo** khi tồn tại, không chiếm vùng mặt bằng. Quy trình xuất và giới hạn PDF xem [hướng dẫn xuất PDF](QKS-2D-PDF-EXPORT.md).
