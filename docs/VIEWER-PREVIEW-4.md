# QKS 2D Preview 4 — Chọn nhiều, nhãn cấu kiện và nhập gió theo Story

Cập nhật 18/10/2026 theo các ảnh chạy thực tế và yêu cầu mới. Giữ theme navy/Material Design, khả năng Apply ETABS có backup/preflight/read-back của Preview 3. Phần gió chuyển sang **nhập trực tiếp giá trị**, thay cho quy đổi áp lực.

## 1. Thao tác vùng 2D

| Thao tác | Hành vi |
|---|---|
| Click trái | Chọn một đối tượng; click vùng trống hủy chọn |
| Giữ Ctrl + click trái | Thêm/bỏ từng đối tượng khỏi tập chọn |
| Kéo trái → phải | Window: chọn đối tượng nằm trọn trong khung xanh liền |
| Kéo phải → trái | Crossing: chọn đối tượng giao khung xanh lá nét đứt, gồm cả khung nằm trong ô sàn |
| Giữ Ctrl khi bắt đầu quét | Thêm kết quả quét vào tập chọn hiện có |
| Delete | Hủy chọn cấu kiện; không xóa đối tượng, tải hoặc nháp. Khi đang nhập TextBox, Delete vẫn sửa chữ bình thường |
| Alt + click | Chuyển đối tượng chồng nhau; thay cơ chế Ctrl + click cũ để Ctrl dành cho chọn nhiều |
| Esc | Hủy khung quét hoặc chọn hai điểm đang thực hiện |
| Chuột giữa/phải, lăn chuột | Pan và zoom tại con trỏ |

Tập chọn được đồng bộ hai chiều với bảng nguồn; giữ qua đổi Plan/Elevation và lọc, reset khi đọc/đổi snapshot. Tập chọn có thể chứa đối tượng ngoài vùng nhìn; bộ đếm và inspector ghi số lượng. Chọn nhiều sẽ khóa editor tải đơn để tránh vô tình soạn chỉ cho một cấu kiện; nhập gió có thể dùng các dầm biên đang chọn theo Story.

Đây là triển khai quy tắc window/crossing và chọn nhiều theo yêu cầu. Chưa thể xác nhận giống ETABS 100% về mọi thao tác/đồ họa vì cloud không chạy WPF/ETABS trên Windows.

## 2. Kết nối và mặt đứng

- Thanh kết nối dưới headbar có **File tính** và **Mô hình kết nối** dạng ComboBox, hiển thị ETABS/version/file/PID. Mở dropdown để quét các ETABS đang chạy; chọn model rồi **Đọc lại**. Đổi model sẽ hủy snapshot cũ, giữ nháp tách nguồn để vẫn lưu được. Không tự chuyển nháp sang model khác.
- Dòng nhỏ bên dưới giữ provenance snapshot/offline. Nếu mở snapshot JSON trong lúc vẫn có kết nối API, phải dựa vào dòng provenance này; snapshot offline không được Apply.
- Ẩn các ô gốc mặt đứng X/Y và góc U. Chọn **Chọn 2 điểm / Hủy** trên Plan để dựng mặt đứng; snap đầu mút, trung điểm hoặc điểm gần nhất trên cạnh cấu kiện trong **8 DIP**. Có dấu snap và tên cấu kiện khi rê chuột. Ngoài khoảng snap vẫn chọn được tọa độ tự do; hai điểm trùng bị từ chối.
- Elevation vẽ trục cao độ Z và các đường/mốc **Story + cao độ m**, dùng `Story.GetNameList/GetElevation` qua reader hiện có, đã đổi về mét. Không suy từ số tầng hoặc tạo cao độ giả. Đây là mốc theo Story, chưa phải nhập lưới XY/named elevation của ETABS.

## 3. Nhãn và màu

- Ba checkbox độc lập: **Label cấu kiện**, **Section / Property**, **UniqueName**. UniqueName lấy `ModelElement.Name`, không thêm Frame:/Area:. Các giá trị giống nhau không lặp lại trong cùng block nhãn.
- Nhãn sàn đặt tại trọng tâm diện tích polygon đang hiển thị; nhãn dầm tại trung điểm đoạn hiển thị. Nhãn được dịch trên/dưới/trái/phải để tránh nét dầm và block nhãn khác. Khối nhãn giữ chữ thường, không in đậm khi chọn.
- Tối đa 500 block nhãn cấu kiện và 200 nhãn tải nháp. Nếu không còn vị trí phù hợp trong vùng nhìn, ẩn nhãn thay vì chồng chữ. Các dầm chiếu thành điểm trên Elevation giữ hình học cắt nhưng không dồn text vào cùng điểm.
- Giữ **Màu theo property** và palette đã lưu trước đây; bỏ ô mã màu HEX và nút lưu mã màu khỏi viewer. Cột vẫn không được đưa lên view.

## 4. Bảng quản lý và font

Heading bảng nguồn: **Unique ID; Label; Story; Type; Property; Length (m)**. Unique ID chỉ hiển thị tên gốc, các cột còn lại căn giữa. Identity nội bộ vẫn giữ Frame:/Area: để không trùng nguồn và không ảnh hưởng fingerprint/Apply.

Chỉ header in đậm; dòng dữ liệu, dòng đang chọn/hover, ComboBox và TextBox dùng chữ thường. Style được giới hạn trong viewer/popup, không thay format hai cửa sổ tải cũ. Tab header dùng format QKS; nội dung tab không còn thừa hưởng Bold xuống mọi control.

Chiều cao vùng quản lý tăng **1,25 lần**: mặc định 180→225 DIP; chế độ cửa sổ thấp 140→175 và 100→125 DIP; minimum 92→115 DIP. Vẫn kéo thanh chia được và giữ bố cục thích ứng màn hình/DPI.

## 5. Gió nhập từ Excel/popup, không quy đổi

Trong tab Gió có **Nhập từ file Excel / CSV** và **Nhập giá trị theo Story…**. Popup có hai tab dầm biên và User Loads, dropdown Story/pattern/khóa API, đọc vùng đang chọn trong Excel và xuất mẫu `.xlsx` theo Story hiện tại.

- Dầm: nhập q1/q2 trực tiếp [kN/m], Global-X hoặc Global-Y. q2 trống = q1 (uniform). Ghi UniqueName cụ thể; nếu bỏ trống, chỉ dùng dầm đang chọn thuộc đúng Story. Không tự nhận diện dầm biên hoặc nhân với chiều cao đón gió.
- User Loads: nhập Fx/Fy [kN], Mz [kN·m], X/Y [m]. Phải nhập đủ, có thể ghi 0 rõ ràng. Đọc User Loads của pattern trước để có khóa API và tự đối chiếu tầng–diaphragm. Mỗi lần chỉ cập nhật một User Loads pattern đã đọc; chưa tự tạo khóa mới.
- Bộ nhập kiểm tra toàn bộ lô trước khi cập nhật nháp; lỗi một dòng không ghi một phần vào hai editor. Dòng gió dầm cùng unique name/pattern/hướng được cập nhật giữ ID; cả batch dầm có một bước Undo. User Loads vẫn lưu/mở bằng file nháp riêng, không dùng lịch sử Undo dầm.
- Các ô p(z), chiều cao đón gió, bề rộng mặt đón gió và chu kỳ T đã bỏ; code `WindPressureBand` và các đường quy đổi cũ đã loại bỏ. Không tính theo tiêu chuẩn hoặc đổi p→q/F. Nháp gió cũ đã chứa giá trị q/F vẫn mở được và không bị tính lại.

[Định dạng file, đơn vị và quy trình nhập](WIND-STORY-INPUT.md).

## 6. Kiểm tra và giới hạn

- **255/255 core/VM tests đạt**: chọn khung/crossing/click, chọn nhiều, snap, centroid/va chạm nhãn, batch nhập, đọc Excel/CSV/shared strings/cached formulas, dữ liệu thiếu/sai và hồi quy Apply. Các test quy đổi cũ được bỏ cùng code theo yêu cầu mới.
- Build packed XLL net48/Windows Excel x64: **0 warning / 0 error**.
- Benchmark core/VM 1k/10k/50k đối tượng gồm phép chọn toàn bộ; JSON số đo nằm trong gói. Không đo WPF FPS, COM hoặc thao tác chuột trên Windows.
- Cloud chưa chạy được Excel/WPF/ETABS. Cần kiểm tra font thực tế, gesture, snap, layout/DPI và đọc vùng Excel trên máy Windows. SAFE live vẫn chưa có adapter xác minh; không thay plugin AutoCAD trong đợt này.

## 7. Cài và thử

Tải [gói Preview 4](../downloads/QKS-2D-Preview-4/). Đóng toàn bộ Excel, bỏ nạp XLL cũ, giải nén và Browse tới `Excel/QksExcelAddIn-AddIn64-packed.xll`. Headbar phải ghi **PREVIEW 4**. Không cần deploy AutoCAD để dùng viewer ETABS.

Thử offline trước:

1. Mở `Samples/viewer-readonly.json`. Quét hai chiều quanh các dầm/sàn; Ctrl + click; Delete; kiểm tra tập chọn trên bảng.
2. Bật Section và UniqueName, chọn sàn/dầm, zoom để kiểm tra trọng tâm/trung điểm, tránh nét và font thường. Kiểm tra header mới và chiều cao bảng.
3. Chọn hai điểm ở đầu, giữa và cạnh dầm để dựng Elevation; kiểm tra mốc Base/L1/Roof đúng 0/3/6 m. Thử Esc và đảo hướng nhìn.
4. Vào Gió, nhập `Samples/wind-input-beams.csv` bằng popup. Kiểm tra hai dòng q trên B2/B3 và Undo; không có giá trị nào được nhân/quy đổi.
5. Với User Loads, mở `Samples/viewer-wind-source.json`, tạo bộ nháp mới nếu bộ cũ đang tách nguồn; **Mở gió** `viewer-user-wind.qkswind.json`, rồi nhập `wind-input-user-loads.csv`. Kiểm tra Story thủ công và Fx Roof = 30 kN. Nút Apply vẫn khóa vì offline.
6. Xuất mẫu Excel trong popup để có Story của model đang dùng; điền/lưu workbook, nhập lại và thử một ô thiếu/sai để kiểm tra từ chối cả lô. Thử vùng chọn Excel có heading.

Trên ETABS thật: dùng bản sao model đã lưu, kiểm tra dropdown chọn đúng file/PID rồi Đọc lại. Đối chiếu Story/cao độ và nhãn, thử Apply trên phạm vi nhỏ, kiểm tra backup/read-back. Kết quả core/build không thay việc nghiệm thu này.
