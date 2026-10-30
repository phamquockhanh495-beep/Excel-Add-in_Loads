# QKS — Loading Case Plan: xuất PDF mặt bằng tải trọng

## Mở và xuất nhanh

1. Cập nhật XLL, kết nối API và đọc mô hình. Mở **Quản lý tải trọng → Xuất PDF · Loading Case Plan**.
2. Chọn **Load Pattern** hoặc **Load Sets**, tầng đang xem / các tầng / toàn bộ tầng. Ctrl hoặc Shift để chọn nhiều Pattern/Set và nhiều tầng.
3. Mặc định mỗi tổ hợp tầng × Pattern/Set là một mặt bằng. Bật **Gộp các Pattern/Set** nếu muốn hiển thị đồng thời trên một mặt bằng.
4. Cài giấy, margin, header/footer và nội dung bản vẽ. Bấm **Cập nhật xem trước**, kiểm tra danh sách trang, zoom và cảnh báo.
5. Bấm **Xuất PDF**. Có thể xuất một file nhiều trang hoặc tách file theo tầng vào thư mục. Các file tách có tên `QKS_<tầng>.pdf`; file cùng tên được thay sau khi hoàn tất.

Dữ liệu in lấy từ lần đọc API tại lúc mở hộp thoại. Đổi bộ lọc in không đổi workspace gốc. Muốn dùng mô hình hoặc màu/cỡ chữ Display mới, đóng hộp thoại, cập nhật rồi mở lại.

## Giấy, margin và tỷ lệ

- A4, A3, A2, A1, A0 hoặc kích thước tùy chỉnh 50–3000 mm mỗi cạnh. Portrait/Landscape tự sắp cạnh ngắn/dài.
- Margin trái/phải/trên/dưới độc lập, cho phép 0. Khóa margin sẽ dùng giá trị bên trái cho cả bốn cạnh khi lưu/xem trước.
- Header/footer có chiều cao riêng, khoảng cách riêng với bản vẽ. Kiểm tra vùng còn lại ít nhất 20 × 20 mm; vùng quá nhỏ bị chặn trước khi xuất.
- **Vừa vùng in** tự tính tỷ lệ, ghi tỷ lệ thực ở footer `{Scale}`. Tỷ lệ cố định nhập mẫu số `n` cho `1:n`.
- Nếu hình vượt giấy ở tỷ lệ cố định: chương trình yêu cầu Fit hoặc **Ghép trang**, không tự thu nhỏ sai tỷ lệ. Khi ghép có chồng mép mm và chỉ số ô trên mỗi trang.
- Chọn **Chỉ vùng XY đang xem** để xuất vùng Plan hiện tại; đây là lựa chọn crop có chủ đích. Khung cắt giữ cùng tọa độ XY khi chọn nhiều tầng.
- **Áp dụng ẩn/isolate** chỉ dùng với tầng đang xem. Mặc định in đầy đủ đối tượng trong dải Plan, bỏ màu chọn đối tượng; tên/Extrude từng loại dùng setup Display đã chụp lúc mở hộp thoại.

## Header / Footer

Ba vùng trái/giữa/phải; chỉnh nội dung, cỡ 6–36 pt, đậm/nghiêng, RGB `#RRGGBB` hoặc bảng màu trực tiếp, đường phân cách. Hỗ trợ logo PNG/JPEG ≤10 MB, giữ tỷ lệ trong header.

Biến: `{Project}`, `{Model}`, `{Story}`, `{Elevation}`, `{Mode}`, `{Pattern}`, `{LoadSet}`, `{Scale}`, `{Date}`, `{Page}`, `{Pages}`, `{Revision}`. Ngày dùng giờ máy xuất. `{Model}` mặc định chỉ tên file, có tùy chọn hiện đường dẫn đầy đủ. Font DejaVu Sans nhúng hỗ trợ tiếng Việt.

Nội dung dài tự xuống dòng. Header/footer quá thấp sẽ báo lỗi để điều chỉnh; không đè lên mặt bằng. Hai biến trang được tính sau khi đã dàn toàn bộ trang chú giải và trang ghép. Khi tách PDF theo tầng, số trang in trong footer giữ số trang của **toàn lô** để đối chiếu danh sách xem trước.

## Tải, màu sắc và chú giải

- Frame, Shell và Joint bật độc lập trong chế độ Pattern. Load Sets lấy các thành phần Shell trong set, không phụ thuộc Pattern đang chọn ở workspace hay checkbox Frame/Joint.
- Tải tường đứng trên Frame: nét đỏ đô theo đoạn gán, nhãn `Pattern - Giá trị Đơn vị`; giữ dấu và hai giá trị nếu biến thiên.
- Sàn tô màu phân loại; giữ lỗ mở, gồm lỗ giao biên và lỗ chồng nhau. Tránh đặt nhãn sàn vào trọng tâm nằm trong lỗ khi tìm được vị trí bên trong sàn.
- Joint dùng dấu x nhỏ và mũi tên theo hướng lực; mômen dùng ký hiệu kép/cung tròn. Thang kích thước dùng cùng cực đại theo nhóm/đơn vị trong toàn lô.
- Hướng local/named chưa xác định không bị suy diễn: có cảnh báo và nhãn giá trị. Phạm vi tải đọc được vẫn theo adapter hiện tại, không bổ sung tải tự sinh hay loại tải API chưa hỗ trợ.
- Chú giải dưới hoặc bên phải, có kích thước/cỡ chữ riêng. Dài quá sẽ tự sinh trang chú giải tiếp theo; không thu chữ xuống dưới 6 pt. Tên Pattern/Set đậm, giá trị theo màu chữ tải, trục/hướng sàn nghiêng.
- Các nhãn cấu kiện chồng nhau trong mô hình lớn vẫn cần kiểm tra ở preview; nếu chạm biên in sẽ có cảnh báo. Có thể đổi giấy, tỷ lệ, cỡ chữ Display hoặc tắt tên cấu kiện. Giá trị tải đầy đủ nằm trong chú giải nếu bật mục này.

## Ghi nhớ, tiến độ và hủy

**Lưu cấu hình in** hoặc xuất thành công sẽ lưu `%LocalAppData%\QKS\Viewer\print-profiles.json`. Lưu giấy, margin, header/footer, logo, tỷ lệ và tùy chọn nội dung; danh sách tầng/Pattern/Set chọn theo mô hình mỗi lần mở. Nếu file hỏng/khác version, thông báo dùng mặc định và giữ file cũ cho tới khi người dùng bấm Lưu cấu hình.

Xuất chạy nền có thông báo tiến độ và Hủy. File PDF đích chỉ được thay khi tạo xong. Xuất tách tầng dựng toàn bộ file trong thư mục tạm trước khi đưa ra đích; nếu lỗi trong bước đưa file ra đích sẽ phục hồi các file đã thay. Không đóng cửa sổ giữa tác vụ: Hủy rồi chờ kết thúc.

Giới hạn lô: 300 mặt bằng, 500 trang sau dàn trang. Với mô hình rất lớn, nên chia theo tầng/nhóm Pattern để giảm bộ nhớ.

## Phạm vi và kiểm chứng

Đã triển khai xuất **Plan**; chưa xuất mặt cắt. Lưới trục ETABS chưa có trong dữ liệu mô hình đang đọc nên **chưa đưa lưới trục vào PDF**; giao diện có ghi rõ giới hạn này. PDF không chứa panel/menu WPF. Đường nét và chữ là vector, font được nhúng; logo là ảnh.

Kiểm tra cloud: build net48 WPF, core/view-model, thông số giấy, tỷ lệ đoạn tải, lỗ mở, nhãn Unicode, Pattern/Set, trang tiếp chú giải, hủy và rollback. PDF mẫu được mở, trích text, kiểm tra kích thước trang/font và raster hóa bằng PyMuPDF độc lập. PDF mẫu dùng **dữ liệu minh họa**, không phải mô hình thực của người dùng.

Chưa nghiệm thu hộp thoại WPF trong Excel/ETABS và in trên Windows. Khi cài bản mới, kiểm tra một tầng với model thực, đối chiếu số tải/hướng/giá trị, rồi thử lô nhiều tầng. Kiểm tra font và tỷ lệ ở 100% trên ít nhất hai trình đọc PDF trước khi phát hành hồ sơ chính thức.

## Thư viện

PDFsharp 6.2.4 (MIT, netstandard2.0) và DejaVu Sans (Bitstream Vera / DejaVu). DLL phụ thuộc và font được đóng gói vào XLL; giấy phép kèm ZIP. Không cần cài máy in PDF hoặc font ngoài để xuất.
