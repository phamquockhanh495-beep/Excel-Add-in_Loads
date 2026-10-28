# QKS 2D — Hướng tải và mặt cắt có tên (28/10/2026)

## Những thay đổi

- Frame: mũi tên đặt tại vùng được gán tải; trong Elevation nối chân mũi tên để biểu diễn tải đều/biến thiên. Giá trị tại đoạn bị cắt A–B được nội suy theo vị trí trên dầm gốc, không kéo giãn q1/q2 sang đoạn mới.
- Shell: ký hiệu tải trên Plan tại trọng tâm hình chiếu; trên Elevation hiển thị dãy mũi tên tại đường giao mặt cắt. Vị trí đặt tải lấy từ mặt phân tích (cote đỉnh sàn theo quy ước viewer), không lấy đáy Extrude.
- Joint: dấu x nhỏ tại nút, mũi tên theo các thành phần lực đã gán; mômen dùng cung tròn hoặc mũi tên kép theo hướng trục. Không cộng các pattern/step thành một tải.
- Plan: tải vuông góc mặt bằng dùng ⊗ (vào mặt phẳng) / ⊙ (ra mặt phẳng). Ví dụ Fy dương là hướng +Y, Gravity dương là −Z. Không vẽ Gravity thành mũi tên ngang mặt bằng.
- Tỷ lệ tự động riêng theo Frame/Shell/Joint và đơn vị lực/mômen: thành phần lớn nhất tương ứng 84 DIP; thành phần nhỏ giới hạn tối thiểu 6 DIP để thấy được. Kích thước ký hiệu pháp tuyến giới hạn riêng 3–8 DIP. Đây là tỷ lệ biểu diễn, không phải chiều dài vật lý. Khi đổi pattern/vùng xem, tỷ lệ tự chuẩn hóa lại.
- Hướng local sử dụng ma trận ETABS của đối tượng có tải local. Không xác định được hệ trục thì giữ giá trị và ghi “chưa xác định hướng”, không đoán hướng tải.
- Mặt cắt: đặt tên, lưu/cập nhật, chọn và xem lại, xóa khỏi danh mục. Lưu hai điểm đầu/cuối, nửa dải xem và hướng nhìn. Danh mục và đường cắt đã lưu ẩn khi quay về Plan; nét A–B chỉ hiện trong lúc chọn hai điểm.
- Nguồn tải bổ sung: Uniform Load Sets dựa trên bảng gán area và bảng định nghĩa thực tế; Uniform-to-Frame hiển thị áp lực gốc trên shell kèm DistType. Không suy đoán gán từ tên property và không tự quy đổi thành tải dầm.
- Bảng quản lý phân biệt tổng số thành phần trong nguồn/pattern và số nằm trong vùng xem, giúp kiểm tra trường hợp WY chỉ có Joint.

## Cài và kiểm tra

1. Đóng Excel, bỏ bản QKS cũ, giải nén ZIP vào thư mục mới. Excel desktop Windows 64-bit → Options → Add-ins → Excel Add-ins → Browse → `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS.
2. Kết nối đúng workbook và mô hình ETABS trong Kết nối API. Mở Quản lý tải trọng 2D; nếu đang mở viewer, bấm **Đọc lại** để lấy thêm nguồn tải mới.
3. Chọn tầng có tải. Trong **QUẢN LÝ TẢI TRỌNG**, chọn pattern SDL/LL/WL thực sự có gán tải; tick Frame và Shell. Tick Joint khi kiểm tra tải nút, có thể bật cả ba. “Hiện giá trị trên hình” điều khiển chữ; mũi tên vẫn hiện khi tắt chữ.
4. Nếu không thấy tải: so sánh “Nguồn / pattern” với số trong vùng xem; bỏ “Chỉ đối tượng đang chọn”; kiểm tra tầng, phạm vi A–B và lớp cấu kiện trong Settings. Mở **PHẠM VI DỮ LIỆU ĐÃ ĐỌC** để xem lỗi API/schema thay vì coi phần thiếu là 0.
5. Bấm **Chọn 2 điểm / Hủy**, chọn A rồi B trên Plan. Trong Elevation, nhập tên và bấm **Lưu / cập nhật**. Tên trùng (không phân biệt hoa thường) cập nhật mặt cắt đó. Chuyển Plan để xem mặt bằng sạch; chọn Elevation → danh sách mặt cắt → **Xem** để mở lại.
6. Đóng/mở viewer và kiểm tra lại danh sách. Mặt cắt lưu riêng theo đường dẫn file ETABS tại `%LocalAppData%\QKS\Viewer\sections`. Đổi tên/Save As mô hình tạo danh mục riêng; không tự mang mặt cắt sang mô hình khác. Đây là thiết lập vùng xem trên máy, không lưu vào EDB, không phải file snapshot tải trọng.
7. Đối chiếu một tải đều, một tải biến thiên, tải dương/âm, tải Joint Fx/Fy/Fz và mômen với ETABS. Kiểm tra cả Plan/Elevation, đảo hướng nhìn và Extrude bật/tắt. Tải local/named axes cần đối chiếu với trục thực tế của mô hình.

## Phạm vi kiểm chứng và giới hạn

Build net48 WPF và kiểm thử core/VM chạy trong cloud. Chưa chạy Excel/WPF/ETABS trên Windows; schema bảng Uniform Load Sets và dữ liệu API của mô hình thực cần nghiệm thu bằng bước 7. Đây là mô phỏng các quy tắc biểu diễn tương ứng, chưa khẳng định giống ETABS 100%.

Không bao gồm tải nhiệt, surface pressure, nonuniform shell, tải tự sinh/auto-seismic/auto-wind hoặc tải dầm được tính từ phân phối sàn. Bảng Uniform Load Sets không khớp schema/UniqueName hoặc giá trị không hợp lệ sẽ có cảnh báo, không thay bằng 0. Tải trực tiếp và tải từ set giữ nguồn riêng, không cộng/chuyển đổi. Không nhập/lưu Named Elevations vào ETABS. SAFE adapter vẫn chưa nghiệm thu.
