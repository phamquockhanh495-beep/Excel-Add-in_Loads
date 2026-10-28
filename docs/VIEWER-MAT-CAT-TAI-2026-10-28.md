# QKS 2D — Mặt cắt hữu hạn và quản lý tải đã gán

## Hành vi mới

- Chọn 2 điểm: sau điểm A, đường nét đứt đỏ đô bám vị trí con trỏ hoặc điểm snap tới B; đánh dấu A/B. Sau khi dựng mặt cắt, quay lại Plan vẫn thấy tuyến A–B.
- Mặt cắt chỉ có hình học trong đoạn A–B và dải sâu ±d. Cắt cả đường trục, polygon, mặt Extrude, phạm vi tải và điểm tải. Đảo hướng nhìn đổi chiều U nhưng giữ nguyên vùng kết cấu. Thanh trạng thái hiển thị chiều dài A–B.
- Quy ước dựng hình: cote tọa độ mô hình là **đỉnh dầm và đỉnh sàn**; chiều cao/chiều dày đi xuống. Dầm/sàn cùng cote có đỉnh trùng nhau, đáy khác nhau theo tiết diện. Không san phẳng các cao độ vốn khác nhau trong mô hình. Không sửa tọa độ hay insertion point của ETABS.
- Cột/vách luôn giữ đường bao bề rộng nếu đã đọc được tiết diện. Extrude tổng hoặc Extrude riêng của loại này chỉ điều khiển tô màu. Cột giữ hình chiếu tiết diện; vách đứng có polygon lồi được chiếu khối dày khi cắt xiên.
- Màu chọn đổi sang đỏ đô `#800020` cho nét, chữ và phần tô của đối tượng đang chọn.
- Dưới bảng thuộc tính là **QUẢN LÝ TẢI TRỌNG**, thay vùng hướng dẫn chuột: chọn Load Pattern, tick Frame/Shell/Joint độc lập (có thể bật cả ba), bật/tắt giá trị, lọc đối tượng đang chọn, tùy chọn hiển thị giá trị 0 và danh sách chi tiết.

## Dữ liệu tải

- Frame: tải phân bố lực/mômen và tải tập trung lực/mômen, khoảng gán tương đối r từ đầu I theo chiều dài 3D.
- Shell: tải uniform trên area.
- Joint: GetLoadForce, giữ riêng 6 thành phần F1/F2/F3/M1/M2/M3 và load step. Nút được đưa vào viewer khi có bản ghi tải. Mặc định ẩn các thành phần bằng 0; có checkbox để xem lại.
- Chuẩn hóa đơn vị thành m, kN, kN·m, kN/m, kN/m² tương ứng. Giữ dấu, hệ tọa độ và hướng API. Không cộng pattern/step; chọn một pattern để dễ đọc trên mô hình đông đối tượng.
- Màu tải: Frame teal, Shell xanh, Joint tím. Nét đứt và vòng tròn là **ký hiệu phạm vi/vị trí gán**, kèm số liệu; không phải biểu đồ lực tỷ lệ, không biểu diễn vector trong hệ local chưa chuyển đổi. Danh sách bên phải lưu chi tiết từng bản ghi/thành phần.
- Đọc tất cả joint load theo group ALL; nếu API không hỗ trợ, đọc từng nút. Phần thao tác COM vẫn ở STA, có yield/cancel và kiểm tra kết nối/đơn vị như bộ đọc trước.
- Chưa đọc tải nhiệt, surface pressure, area nonuniform, tải tự sinh và các loại tải khác ngoài danh sách trên. Xem phạm vi/cảnh báo đọc API ở mục mở rộng bên dưới bảng tải; dữ liệu thiếu không được coi là tải bằng 0.

## Giới hạn hình học

Giữ giới hạn tiết diện chữ nhật/tròn, shell chiều dày cố định. Tiết diện/axes chưa hỗ trợ giữ hình học phân tích; không suy kích thước từ tên property. Không mô phỏng insertion/end offsets vật lý. Vách lõm cắt xiên, shell nghiêng, lỗ xuyên trong hình cắt cạnh và sàn gân/waffle cần bổ sung hình học riêng. SAFE live vẫn chưa được nghiệm thu.

## Test trên Windows

1. Đóng Excel, bỏ chọn XLL cũ, Browse file XLL mới trong ZIP; chỉ chạy một bản QKS.
2. Kết nối workbook/model đúng API, mở 2D và Đọc lại để có dữ liệu frame point/joint mới.
3. Plan: chọn 2 điểm snap, kiểm tra đường dẫn A–B khi rê chuột. Dựng mặt cắt, zoom hai đầu: không có cấu kiện/tải bên ngoài khoảng. Thử Đảo hướng nhìn và tuyến xiên qua vách.
4. Chọn tầng có slab 100/150/200 mm và dầm 500/700 mm; so sánh cote đỉnh tại cùng cao độ, chỉ đáy thay đổi. Đối chiếu kích thước property với ETABS.
5. Tắt Extrude tổng rồi bật/tắt riêng cột/vách: đường bao giữ nguyên, chỉ phần tô thay đổi. Chọn dầm/sàn/cột/vách, kiểm tra đỏ đô.
6. Chọn pattern có cả tải phân bố, tải tập trung, area uniform, joint lực/mômen. Tick cả ba nhóm, kiểm tra dấu/đơn vị/hệ trục/step và khoảng gán với ETABS. Thử bật giá trị 0, chỉ đối tượng đang chọn, đổi tầng và giới hạn mặt cắt qua điểm tải.
7. Kiểm tra danh sách tải trên mô hình lớn, thao tác Hủy đọc và chuyển mô hình khi đang đọc; không có lệnh ghi tải trong viewer.

Cloud kiểm thử core/VM và build XLL. WPF/Excel/ETABS thực tế cần nghiệm thu trên Windows; không coi kết quả kiểm thử mô phỏng là đã kiểm chứng API trực tiếp.
