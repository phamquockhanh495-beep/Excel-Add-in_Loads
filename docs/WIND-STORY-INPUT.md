# Định dạng nhập tải gió theo Story — Preview 4

Theo yêu cầu 18/10/2026: lấy giá trị có sẵn từ Excel hoặc nhập tay, **không quy đổi áp lực, không tính hệ số gió theo T**. Các mẫu có trong gói; mẫu số chỉ dành cho snapshot nhân tạo, không gán vào model thật.

## Dầm biên

Sheet ưu tiên **WindBeams**:

| Target | Story | Pattern | UniqueName | Direction | q1_kN_m | q2_kN_m | Action |
|---|---|---|---|---|---:|---:|---|
| Beam | Roof | WX | B2 | Global-X | -4 | -4 | ReplaceMatching |

- Story/pattern phải có trong snapshot. UniqueName là **tên gốc** (như 2BV1.4), không phải Label B389 và không thêm Frame:. Dầm phải thuộc Story đã nhập.
- UniqueName trống: áp dụng **nháp** cho các dầm đang chọn thuộc đúng Story. Chọn dầm biên trên view trước khi mở popup; không có dầm phù hợp thì báo lỗi, không tự chọn cả tầng.
- q1 bắt buộc; q2 trống dùng q1. Giá trị là tải đường trên toàn dầm, theo I→J, kN/m, có dấu. Chưa nhập đoạn s1/s2 qua bảng gió này.
- Direction nhận Global-X hoặc Global-Y. Action nhận ReplaceMatching (thay cùng phạm vi) hoặc Add. Trùng dầm/pattern/hướng trong cùng lô bị từ chối. Nhập lại cập nhật dòng nháp gió tương ứng, không nhân bản dòng.
- Khi Apply, pattern phải là loại Wind riêng, không sinh gió tự động hoặc có User Loads khác 0. Không trộn hai phương pháp trong một pattern.

## User Loads / diaphragm

Sheet ưu tiên **WindStories**:

| Target | Story | Pattern | DiaphragmKey | Fx_kN | Fy_kN | Mz_kNm | X_m | Y_m |
|---|---|---|---|---:|---:|---:|---:|---:|
| UserLoads | Roof | WX_USER | khóa API đã đọc | 30 | 0 | 2 | 3 | 2 |

1. Chọn pattern Wind có Auto Lateral Load = User Loads và bấm **Đọc User Loads của pattern** trước. Cần có các khóa trong host; chưa hỗ trợ tự tạo khóa/tầng mới.
2. Chọn đúng Story và khóa diaphragm trong popup. API `Get/SetUserLoad` của SDK đang dùng không có tham số Story riêng, nên quan hệ Story–khóa do người dùng đối chiếu; chương trình không đoán theo tên D1 hoặc ghép chuỗi tên tầng.
3. Nhập đủ Fx/Fy/Mz/X/Y; **0 phải được nhập rõ**, ô trống không tự thành 0. Giữ đúng đơn vị heading; không quy đổi kG/tonf hoặc áp lực sang lực.
4. Mỗi lần chỉ sửa một User Loads pattern đã đọc. Các khóa không có trong lô nhập giữ nguyên. Story được lưu cùng file `.qkswind.json` và kiểm tra tồn tại trong model trước Apply; đây không phải bằng chứng API đã xác nhận quan hệ tầng–diaphragm.
5. X/Y và Mz là giá trị trực tiếp mong muốn tại điểm đặt lực; không tự tìm tâm cứng hoặc tự chuyển mô men khi sửa X/Y.

## Cách đọc file

- Nhận `.xlsx`, `.xlsm`, CSV và vùng đang chọn trong Excel. `.xls` nhị phân dùng qua **Đọc vùng chọn Excel** hoặc lưu thành `.xlsx`.
- Mẫu Excel xuất từ popup có hai sheet trên, điền sẵn danh sách Story/pattern nhưng để trống toàn bộ giá trị tải. Sheet ReadMe có hướng dẫn. Có thể nhập các heading phổ biến: Story/Tầng; Pattern/LoadPattern; UniqueName/Unique ID; q1; Fx/Fy/Mz/X/Y với đơn vị tương ứng.
- Nếu workbook có WindBeams/WindStories, chỉ đọc các sheet này. Nếu không có, tìm heading trong các worksheet (tối đa 64 sheet), tối đa 20 hàng đầu; nhập những bảng nhận diện được. Không bỏ qua một bảng gió lỗi để chỉ lấy các bảng còn lại.
- Formula dùng **giá trị cache đã lưu** trong file; không chạy Excel hoặc tính lại workbook khi đọc `.xlsx/.xlsm`. Mở, tính và lưu workbook bằng Excel trước khi nhập. Cache không có hoặc ô Excel lỗi sẽ không được dùng làm tải. Đọc vùng chọn lấy Value2 hiện tại của một vùng liên tục, có heading.
- Giữ UniqueName, Story và tên pattern ở định dạng Text khi cần giữ số 0 đầu. Dữ liệu số nhận dấu chấm hoặc dấu phẩy thập phân; không dùng dấu phân cách hàng nghìn hay ghép đơn vị vào giá trị.
- Nếu Pattern trống, popup dùng pattern đang chọn ở viewer; phải kiểm tra lại trước khi đưa vào nháp. Heading trùng/cột thuộc tính mơ hồ, trộn q và lực tầng trong cùng dòng, giá trị NaN/Infinity, sai Story/đích hoặc trùng phạm vi đều bị từ chối.
- Giới hạn 10.000 dòng, 64 cột, file 64 MiB và phần XML đọc 32 MiB. Không thực thi macro khi đọc file `.xlsm`.

## Trình tự thao tác

Kết nối/đọc model → tạo bộ nháp đúng nguồn → nếu cần chọn dầm biên hoặc đọc User Loads → mở popup/file → kiểm tra các dòng → **Kiểm tra và đưa vào nháp** → xem lại hình/bảng/Trước-Sau → lưu nháp → Apply riêng khi đã kiểm tra model đích.

Nhập file chỉ cập nhật nháp. Không làm thay đổi mô hình CSI, đơn vị host hoặc file Excel nguồn. Dòng để trống toàn bộ giá trị/đích trong mẫu không tạo tải; muốn tải bằng 0 cần nhập 0 rõ ràng. Undo/Redo dùng cho batch nháp dầm/sàn; User Loads lưu/mở bằng file riêng.
