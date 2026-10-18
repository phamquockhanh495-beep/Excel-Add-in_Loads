# QKS 2D Preview 5 — Delete trong workspace, nhãn xoay và tự đọc kết nối API

Cập nhật 18/10/2026 theo ảnh Preview 4 chạy thực tế. Phạm vi Delete đã được xác nhận: **chỉ bỏ cấu kiện khỏi workspace 2D, có thể khôi phục; không xóa cấu kiện trong ETABS**.

## 1. Delete và Khôi phục

- Có nút **Delete** trên toolbar và phím Delete. Áp dụng cho toàn bộ tập chọn, kể cả cấu kiện đã chọn ở tầng khác rồi chuyển vùng nhìn.
- Cấu kiện được bỏ khỏi hình, bảng nguồn, tập chọn, danh sách tải đã đọc/so sánh đang hiển thị và overlay. Không thể chọn lại cấu kiện đó cho tới khi khôi phục.
- **Khôi phục** hiện lại toàn bộ cấu kiện đã bỏ, theo Story/bộ lọc hiện tại. Nếu sàn ở Roof mà đang xem L1, sàn chỉ hiện khi chuyển đúng tầng. Không tự đổi vùng nhìn hoặc chọn lại đối tượng.
- Danh sách bỏ khỏi workspace chỉ tồn tại trong phiên snapshot hiện tại; đọc/mở snapshot mới hoặc mở lại cửa sổ sẽ khôi phục. Trạng thái này không ghi vào file snapshot/nháp.
- Snapshot gốc, hình học và tải trong ETABS, toàn bộ nháp, dirty state và lịch sử Undo tải giữ nguyên. **Delete không loại các dòng nháp khỏi phạm vi Apply**; bảng Dòng nháp vẫn giữ chúng để kiểm tra. Muốn loại tải đã soạn thì xóa dòng nháp riêng. Sửa nháp của một cấu kiện đã bỏ khỏi view sẽ yêu cầu Khôi phục trước.
- Khi đang nhập TextBox/ô bảng, Delete vẫn sửa nội dung chữ. Delete/Khôi phục cấu kiện bị khóa khi đang đọc/Apply. Nút **Bỏ chọn / xem tất cả tải** tiếp tục chỉ hủy chọn.

## 2. Thông tin tập chọn ở góc dưới trái

Thanh trạng thái dưới cùng có vùng độc lập cho tập chọn:

- Một cấu kiện: UniqueName gốc, Label, Story, Type và Property.
- Nhiều cấu kiện: tổng số và số lượng theo Type.
- Chưa chọn: thông báo chưa chọn; kèm số cấu kiện đã bỏ khỏi workspace nếu có.

Tooltip có inspector chi tiết. Thông tin chọn không bị mất khi trạng thái tiến trình/zoom/vùng nhìn thay đổi. Khi cửa sổ hẹp, text rút gọn bằng dấu ba chấm.

## 3. Nhãn theo hướng cấu kiện và giữ hiển thị khi zoom

- Dầm ngang → chữ ngang; dầm dọc → chữ dọc; dầm xiên → chữ xiên theo hướng chiếu trên màn hình. Chuẩn hóa góc trong khoảng −90° đến dưới 90° để chữ không lộn ngược khi đảo đầu I/J hoặc hướng nhìn.
- Label, Section/Property, UniqueName và giá trị tải nháp dùng cùng cơ chế xoay; chữ thường, cỡ chữ cố định theo DIP khi zoom.
- Dầm ưu tiên trung điểm đoạn đang nhìn và dịch vuông góc với trục để tránh nét. Khi zoom/pan làm hai đầu hoặc trung điểm gốc ra ngoài màn hình, lấy trung điểm phần dầm còn trong viewport; nhãn không mất chỉ vì trung điểm cũ khuất.
- Sàn ưu tiên trọng tâm diện tích polygon; khi zoom vào một phần sàn, dùng trọng tâm phần polygon trong viewport. Sàn vẫn có nhãn khi các cạnh và trọng tâm gốc đều ở ngoài màn hình.
- Bỏ giới hạn 500 nhãn cấu kiện / 200 nhãn nháp và bỏ quy tắc ẩn nhãn khi thiếu chỗ. Bộ đặt nhãn thử dịch để tránh nét và nhãn khác; nếu vẫn chật, **vẫn vẽ nhãn**, có nền trắng mờ hỗ trợ đọc. Mặt bằng dày có thể chồng chữ; có thể tắt các lớp nhãn hoặc zoom thêm.
- Chỉ vẽ nhãn thuộc cấu kiện hiện có trong vùng nhìn, theo checkbox đang bật. Không hiện nhãn cho cấu kiện đã bỏ khỏi workspace/lọc khỏi view. Cỡ chữ không tự co xuống để nhét vào viewport rất nhỏ.

Đây là triển khai quy tắc hướng chữ/zoom theo yêu cầu; chưa xác nhận trùng ETABS 100% về vị trí chữ, font hoặc đồ họa trên Windows.

## 4. Kết nối API là nguồn thông tin chính

- **File tính** lấy `ActiveExcel.Name` và tooltip lấy `ActiveExcel.FullPath` từ Kết nối API. Ví dụ API chọn **Beam.xlsm** thì viewer hiện **Beam.xlsm**, không dùng tên file `.EDB` nữa.
- **Mô hình kết nối** lấy `ActiveEtabs`: phiên bản ETABS, tên file và PID của đúng model API đã chọn. Không tự chọn model đầu tiên trong danh sách đang chạy.
- Kết nối API trước rồi mở workspace → tự đọc hình học/tải, tạo snapshot và vừa khung; không cần bấm Đọc lại lần đầu. Nếu workspace đang mở mà đổi model trong Kết nối API, tự hủy đọc cũ và đọc model mới.
- Đổi riêng workbook Excel hoặc AutoCAD, giữ nguyên ETABS handle/path → cập nhật tên kết nối và giữ snapshot hiện tại. Không đọc lại toàn bộ hình học chỉ vì đổi workbook.
- Muốn lấy các thay đổi vừa chỉnh trong cùng file ETABS: bấm **Đọc lại**. Model không có revision API nên không tự phát hiện mọi thay đổi hình học/tải trong cùng file.
- Trong viewer, mở ComboBox để quét model; chọn model khác cập nhật kết nối chung, hủy snapshot cũ và **chờ bấm Đọc lại**. Workbook Excel vẫn giữ đúng lựa chọn API; muốn đổi workbook, dùng Kết nối API.
- Bộ điều phối giữ một yêu cầu mới nhất, chờ tác vụ đọc/Apply đang kết thúc; kiểm tra handle + PID/path + thế hệ đọc trước khi nhận kết quả. Kết quả của model cũ không được đưa lên viewer mới. Các cửa sổ cùng đọc ETABS được xếp lượt qua gate có thể hủy.
- Khi hủy/lỗi/không có API thì không dùng dữ liệu model khác thay thế. Có thể bấm Đọc lại sau khi sửa kết nối. Nháp cũ khác nguồn vẫn được giữ để lưu và bị tách snapshot như Preview 4.
- Mở snapshot JSON offline vẫn được hỗ trợ. Tên phía trên phản ánh kết nối API hiện tại; dòng provenance ghi rõ **OFFLINE**, và Apply bị khóa. Không dùng tên kết nối để suy rằng snapshot offline đang thuộc model live.

Phần gió tiếp tục nhập trực tiếp Excel/CSV/popup theo Story như [Preview 4](VIEWER-PREVIEW-4.md) và [định dạng file](WIND-STORY-INPUT.md), không quy đổi áp lực.

## 5. Kiểm tra và nghiệm thu

- 291/291 core/VM tests đạt, gồm hồi quy cũ và kiểm tra xóa/khôi phục nhiều đợt, giữ snapshot/nháp, badge, hướng chữ, clipping khi zoom, giữ nhãn trong vùng chật và kết quả đọc sai nguồn.
- Ba bài kiểm tra đặt chữ ngang/ẩn khi thiếu chỗ của Preview 4 được thay cùng thuật toán cũ bằng các bài kiểm tra nhãn xoay/luôn vẽ.
- Packed XLL Windows x64/net48 build không warning/error. Build chỉ kiểm chứng compile/XAML, không chạy WPF hoặc COM trên Linux.
- Gói có logs, mẫu offline/Excel, benchmark core/VM và manifest SHA-256. Benchmark không đo WPF FPS hoặc latency đọc ETABS.
- Cần nghiệm thu Windows: chuột/phím, xoay/nền chữ, clipping/zoom, font/DPI, tự đọc và event kết nối API thật. Chưa xác minh adapter live SAFE 18–22; không thay plugin CAD trong đợt này.

### Trình tự thử trên Windows

1. Đóng toàn bộ Excel, bỏ nạp XLL cũ và nạp `Excel/QksExcelAddIn-AddIn64-packed.xll` từ [gói Preview 5](../downloads/QKS-2D-Preview-5/). Headbar phải ghi **PREVIEW 5**.
2. Trong Kết nối API chọn workbook và ETABS có tên khác nhau, ví dụ Beam.xlsm và dự án.EDB. Mở viewer: kiểm tra hai trường khớp API và snapshot tự xuất hiện mà không bấm Đọc lại.
3. Khi viewer mở, đổi riêng workbook rồi đổi ETABS trong API. Kiểm tra workbook cập nhật mà không mất hình; đổi model thì hình cũ được bỏ và model mới tự đọc. Thử đổi nhanh model trong khi đọc để kiểm tra không nhận nhầm kết quả.
4. Mở snapshot `Samples/viewer-readonly.json` nếu thử offline. Bật UniqueName/Section, xem các dầm dọc/ngang Roof, zoom/pan để đưa trung điểm và các đầu dầm ra ngoài khung. Nhãn của phần còn nhìn thấy phải còn và đúng hướng. Kiểm tra thêm dầm xiên trên model thật.
5. Ctrl chọn nhiều hoặc quét → kiểm tra badge dưới trái → Delete → kiểm tra hình/bảng → Khôi phục. Đổi Story trước khi Khôi phục để xác nhận tôn trọng bộ lọc. Thử Delete trong ô nhập để đảm bảo chỉ xóa chữ.
6. Tạo nháp cho dầm/sàn rồi Delete cấu kiện: nháp vẫn còn, overlay ẩn; Khôi phục thì overlay trở lại. Kiểm tra nháp trước khi Apply trên bản sao model đã lưu.
7. Chọn model khác trong ComboBox viewer: hình chờ Đọc lại. Sau khi chỉnh hình học cùng file ETABS, bấm Đọc lại để lấy dữ liệu mới. Kiểm tra DPI 100/125/150% và cửa sổ thu nhỏ.
