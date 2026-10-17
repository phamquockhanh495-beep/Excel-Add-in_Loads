# Kế hoạch giao diện WPF 2D quản lý tải trọng — Extension QKS

**Trạng thái:** bản thiết kế đề xuất, chưa triển khai giao diện hoặc chức năng gán tải mới. Tài liệu này thay kế hoạch phát triển 2D/3D trước đó; phạm vi đợt phát triển này là 2D, gồm **Plan và Elevation từ ETABS**. Mặt đứng là mặt phẳng nhìn 2D dựng từ dữ liệu mô hình; phạm vi này không yêu cầu engine 3D. ETABS và SAFE dùng chung giao diện, dữ liệu nháp và quy tắc hiển thị, với adapter/capabilities riêng được kiểm chứng cho từng phiên bản.

Các ảnh dưới đây là mockup minh họa, không phải ảnh chụp phần mềm đang chạy. Số liệu mô hình trong ảnh là ví dụ. Hai mockup hiện có minh họa Plan; các điều khiển và hình Elevation trong bản cập nhật này mới ở mức đặc tả, chưa có mockup riêng. Bảng quy định trong tài liệu là đặc tả để triển khai, bao gồm cách ghi số theo locale người dùng.

## 1. Bố cục Layout WPF

![Bố cục WPF 2D đề xuất](design-2d/01-layout-wpf-2d.png)

### 1.1. Sáu vùng làm việc

| Vùng | Nội dung | Hành vi và trạng thái |
|---|---|---|
| A — Kết nối trên cùng | ETABS/SAFE, phiên bản, process/model, file, Plan/Elevation, tầng hoặc trục/mặt đứng, đơn vị, Đọc lại | Hiển thị connected/reading/ready/stale/disconnected; đổi nguồn đánh dấu ngữ cảnh mới, không tự gán nháp cũ |
| B — Chọn nghiệp vụ | Tải tường, Tải gió, Tải sàn | Chia sẻ viewport/selection; đổi tab thay editor, lớp tải và legend theo nghiệp vụ |
| C — Cây/lọc bên trái | Tầng/cao độ hoặc danh sách mặt đứng/trục, phạm vi tầng, nhóm, loại đối tượng, tiết diện, vật liệu, pattern, tìm ID/label, danh sách layer | Lọc hiển thị không sửa tập assignment hoặc xóa nháp; có số đang hiện/tổng số |
| D — Viewport 2D trung tâm | Plan hoặc Elevation, trục/cao độ, cấu kiện, tải, nhãn, chọn, pan/zoom, isolate, Hiện có/Nháp/Chênh lệch | Chọn trên hình đồng bộ với bảng và inspector; không gọi API ghi |
| E — Inspector bên phải | Thuộc tính nguồn chỉ đọc; editor tải nháp theo nghiệp vụ; thông tin vùng chọn | Chọn nhiều đối tượng khác giá trị hiển thị Nhiều giá trị; field chưa hỗ trợ có lý do, không để input giả hoạt động |
| F — Bảng dưới và tác vụ | Assignment hiện có, thay đổi nháp, lỗi, kết quả; Undo/Redo, Lưu nháp, Kiểm tra, Gán phần đã duyệt | Ghi API chỉ sau kiểm tra và hành động Apply rõ ràng; báo riêng thành công, thất bại, bỏ qua |

Một cửa sổ dùng cấu trúc WPF Grid, GridSplitter, TabControl, TreeView và DataGrid có virtualization. Ảnh tham khảo thể hiện chủ yếu tab Tải sàn; các tab dùng chung khung và thay panel nghiệp vụ.

### 1.2. Kích thước và responsive

- Kích thước thiết kế ưu tiên khoảng 1600 × 1000 DIP; kiểm tra bố cục trên màn hình 1920×1080 và 1366×768 ở các mức DPI. Không coi độ phân giải vật lý là kích thước khả dụng theo DIP.
- Thanh kết nối khoảng 52–64 DIP; thanh nghiệp vụ khoảng 44–48 DIP; thanh trạng thái khoảng 26–32 DIP.
- Sidebar trái khoảng 220–260 DIP, inspector phải khoảng 300–340 DIP, viewport lấy phần còn lại. Cho kéo GridSplitter và ghi nhớ kích thước.
- Bảng dưới khoảng 180–240 DIP, có nút thu gọn; thanh tác vụ giữ nút Gán phần đã duyệt nhìn thấy.
- Khi vùng làm việc hẹp: thu sidebar/inspector thành drawer, đưa bộ lọc phụ vào menu; không thu viewport đến mức không thể thao tác. Ở DPI cao cần kiểm tra kích thước khả dụng và tự chuyển bố cục.
- Ghi nhớ bố cục theo người dùng; khôi phục phải kiểm tra màn hình hiện tại để cửa sổ không nằm ngoài màn hình.

### 1.3. Thanh công cụ viewport

Chọn; quét Window/Crossing; pan; zoom; Fit all/Fit selection; Hiện tất cả/Isolate; chế độ đường trục/bề rộng thực; tên; tiết diện; tải; trục local; màu theo thuộc tính; legend; tùy chọn hiển thị.

Chuột trái chọn, Ctrl thêm/bớt, Esc hủy công cụ, chuột giữa pan, lăn chuột zoom quanh con trỏ. Quét trái→phải chọn đối tượng nằm hoàn toàn trong khung; phải→trái chọn giao cắt. Escape trong editor kết thúc nhập trước khi tác động tới viewport. Không bắt phím Delete/Undo của viewport khi người dùng đang sửa ô dữ liệu.

### 1.4. Nhận diện QKS Navy — điều chỉnh theo yêu cầu

- Headbar navy tối **#0B1F3A**; nút primary/tab active **#102A4C**, chữ/icon trắng.
- Hover **#163D66**, pressed **#08172B**. Nút secondary nền trắng, chữ navy, viền mảnh; control checkbox/radio/slider dùng accent navy.
- Nút hình chữ nhật, CornerRadius **3 DIP**, BorderThickness **1 DIP**, chiều cao chuẩn khoảng **38 DIP**. Input radius 3 DIP, panel tối đa 4 DIP; không dùng dạng pill hoặc bo lớn.
- Nền nội dung vẫn sáng; vùng chọn điều hướng có tint **#E8EEF6**. Tông nhận diện không thay thế màu biểu diễn cấu kiện, tải trọng, selection hoặc cảnh báo.
- Ảnh là mockup trực quan; mã màu/kích thước trong đặc tả và [design tokens](design-2d/qks-navy-tokens.json) là cơ sở triển khai chính xác. Chưa tích hợp theme này vào ứng dụng đang chạy.

### 1.5. Điều khiển riêng cho Plan và Elevation

Bộ chọn **Plan / Elevation** độc lập với ba tab Tải tường/Tải gió/Tải sàn. Chuyển góc nhìn giữ snapshot, ID, selection và draft; không sao chép assignment sang một bộ mới.

- **Plan:** chọn tầng/cao độ, mặt phẳng XY và layer như đặc tả hiện có.
- **Elevation ETABS:** chọn mặt đứng lưu sẵn nếu API thật sự cung cấp định nghĩa; hoặc chọn một trục thẳng/hai điểm trên Plan để xác định mặt phẳng đứng.
- Thanh điều khiển Elevation có tên view/trục, tầng bắt đầu–kết thúc, khoảng cao độ, dung sai nằm trong mặt phẳng, dải chiều sâu khi chiếu và nút Đảo hướng nhìn.
- Bật cao độ tầng, tên tầng, kích thước đứng/ngang, local axes, tải và đối tượng tham chiếu. Có Fit mặt đứng/Fit selection và lưu view riêng.
- Một viewport chuyển Plan/Elevation ở bản đầu. Chế độ đặt hai viewport cạnh nhau chỉ bổ sung sau khi benchmark; selection có thể giữ dù đối tượng không nằm trong view, với số lượng ngoài view được thông báo.
- SAFE tiếp tục dùng Plan/cao độ theo capability đã kiểm chứng. Elevation SAFE không nằm trong cam kết bản đầu; lựa chọn chưa hỗ trợ có lý do, không dựng hình giả để coi là đã đọc từ API.

## 2. Chức năng ba nhóm tải

### 2.1. Tải tường

**Chọn và nhập:** chọn dầm đỡ/nhóm dầm, loại tường, chiều dày, chiều cao, lớp hoàn thiện, trọng lượng riêng hoặc tải đường trực tiếp; hệ số/lỗ mở và các điều kiện trừ chiều cao được khai báo rõ. Nhập theo toàn nhịp hoặc từng đoạn, định vị theo đầu I/J và khoảng cách thực/relative có nhãn.

**Hiển thị:** dầm đỡ vẫn giữ màu thuộc tính; tuyến tải là lớp overlay riêng. Tải đều có dải hình chữ nhật; tải biến thiên có hình thang; ranh giới đoạn có tick; text q1/q2 và chiều tác dụng. Khu vực không có tải khác khu vực chưa có dữ liệu.

**Kiểm tra:** công thức và version định mức có thể truy vết; không nhân hệ số hai lần; đoạn không vượt dầm, không chồng ngoài chính sách; xử lý cửa/lỗ mở có đơn vị và nguồn rõ ràng. Chỉ tính q=γ·t·h khi đúng giả thiết; tường nhiều lớp cộng theo lớp, các giảm trừ phải được thể hiện trong bảng thành phần.

**ETABS/SAFE:** tải trên line/frame chỉ được gán nếu adapter xác nhận đối tượng và loại tải tương ứng. Với tường nằm trên sàn mà không có phần tử đỡ tương ứng, giữ tuyến tải nháp và yêu cầu phương án phân phối hợp lệ. Không tự tạo dầm/area hoặc tự đổi tải đường thành tải mặt.

### 2.2. Tải gió

Hai chế độ tách rõ: **Nhập tải đã xác định** và **Tính theo tiêu chuẩn**. Bản đầu có thể phát triển nhập/sửa/xem trước; engine tính phải chốt tiêu chuẩn, phiên bản, các hệ số và bộ số đối chứng trước khi mở.

**Dữ liệu:** pattern/hướng, mặt đón/hút, áp lực p, chiều cao/vùng tác động, bề rộng truyền tải và đối tượng nhận. Nếu chuyển p [kN/m²] sang q [kN/m] theo bề rộng B [m], phải lưu B và giả thiết truyền tải; công thức không tự áp cho mọi hình học.

**Hiển thị 2D:** chọn phương X/Y hoặc vector xác định, vẽ mũi tên trên biên, đánh dấu các dải áp lực, nhãn p/q/F và quy ước dấu. Có bảng tải theo cao độ/tầng; các hướng khác nhau có filter và ký hiệu, không trộn thành một tổng vô hướng không có nghĩa.

**SAFE:** có thể hiển thị dữ liệu tải gió tham chiếu và tải tương đương theo khả năng API/đối tượng. Lực nút hoặc phản lực và mômen nhận từ mô hình khác phải giữ kN/kN·m, vị trí và hệ trục. Không coi chúng là áp lực sàn kN/m². Nếu adapter chưa hỗ trợ loại đích, chỉ xem/soạn nháp và khóa Apply với lý do.

**Kiểm tra:** dữ liệu demo không được trở thành kết quả thiết kế. Pattern, dạng lực/mômen/tải đường/tải mặt, chiều, dấu và hệ trục phải đi cùng giá trị.

### 2.3. Tải sàn

**Chọn và nhập:** chọn area/polygon có sẵn; pattern hoặc load set, q, hướng, chế độ Add/Replace/Delete và phạm vi đối tượng. Chỉnh hàng loạt nhưng hiển thị rõ khác biệt giữa các assignment cũ.

**Hiển thị:** polygon fill có độ đục thấp, lỗ mở trắng/hatch riêng; tải đều có nhãn và ký hiệu phương. Có chế độ màu theo giá trị tải, nhưng phải chỉ rõ pattern, đơn vị, phạm vi và thang màu. Load set hiển thị tên và danh sách thành phần trong tooltip/inspector.

**Kiểm tra:** tọa độ biên, lỗ mở, diện tích, loại phần tử, units, pattern và hướng. Định dạng số không làm thay đổi giá trị lưu. Sàn nghiêng có quy tắc diện tích thực/chiếu theo đúng định nghĩa assignment của nguồn.

**Vùng tải cục bộ:** cho phép vẽ vùng nháp và xem giao với area. Apply vùng cục bộ chỉ mở sau khi chốt thuật toán clipping, loại assignment và cách host truyền tải; không tự tạo sàn có độ cứng hoặc fallback property Default.

### 2.4. Quy tắc capability của hai nguồn

Cả ETABS và SAFE được khảo sát/kiểm thử từ mốc đầu. Danh sách thao tác được cấp bởi adapter: CanReadGeometry, CanReadLineLoads, CanReadAreaLoads, CanWriteLineLoads, CanWriteAreaLoads, CanWritePointForces, CanWritePointMoments, CanReadBack, HasStories, HasGridSystems, HasNativeElevationDefinitions và CanBuildElevationFromGeometry. Tên capability là thiết kế, chưa phải kết luận API của từng phiên bản.

UI dùng tầng ETABS hoặc cao độ/mặt phẳng SAFE theo dữ liệu thực; không dựng tầng giả để che thiếu thông tin. Chọn SAFE vẫn thấy ba nhóm tải và trạng thái hỗ trợ; thao tác chưa có mapping được vô hiệu hóa kèm lý do.

## 3. Hợp đồng thông tin mô hình và định dạng giá trị

| Nhóm | Trường cần có | Quy định |
|---|---|---|
| Nguồn | Application, Version, ProcessId, SessionId, ModelBindingId, file/path, snapshot timestamp/fingerprint | Dùng định danh API khi có; không giả định mọi host có GUID/revision. Save As/đổi model yêu cầu xác nhận binding |
| Đối tượng | ObjectType, UniqueName/source ID, Label, Story/Level, group | Khóa theo model + type + source ID; label chỉ để hiển thị |
| Nút | X/Y/Z, units, connectivity | Đọc chuẩn hóa sang m; giữ double đầy đủ |
| Line/frame | I/J, length, local axes, offsets/insertion, section, material | I/J quyết định định vị đoạn; không suy cột/vách từ reader chỉ lấy dầm ngang |
| Area | outer boundary, openings, normal/local axes, type, thickness, section, material | Tách area object với analysis mesh; giữ thứ tự vòng và lỗ |
| Thuộc tính | source property ID/name, kind, dimensions, source color nếu có | Đọc từ adapter; thiếu thì N/A, không tự gán kích thước thật |
| Grid/level | tên, đường/đường cong khi API có, Z, source system | Không tự đổi tên grid hoặc làm tròn cao độ nguồn |
| Assignment | target ID, pattern, type, direction/coordinate system, interval/region, values, units | Load Pattern khác Load Case/Combination; không dùng combo thay pattern ghi |
| Nháp | ChangeId, source fingerprint, Before/After, Add/Replace/Delete, validation/status | Tách 0, xóa và chưa nhập; chỉ Apply mới viết API |
| Hiển thị | visible, selectable, base/fill/text color, stroke, label template, legend range | Là cấu hình QKS, không tự sửa thuộc tính ETABS/SAFE |

Các field nguồn mặc định chỉ đọc. Việc thay tiết diện/vật liệu là chức năng chỉnh model riêng, không gắn vào thao tác đổi màu hiển thị.

### 3.1. Đơn vị và số

- Nội bộ: m, kN, kN/m, kN/m², kN·m. Áp lực kPa có quan hệ 1 kPa = 1 kN/m²; lực, mômen và áp lực không cộng lẫn.
- Hiển thị: chiều dài m hoặc mm; lực kN hoặc N; hướng hiển thị tên đầy đủ và mũi tên. Không dùng chữ t mơ hồ cho cả khối lượng và lực.
- Đề xuất mặc định vi-VN: tọa độ m 3 chữ số thập phân; kích thước tiết diện mm 0–1; q/p 2; diện tích 2; hệ số 3. Ví dụ `2,50 kN/m²`. Người dùng được chọn locale; ảnh dùng số minh họa.
- Input không cho dấu phân nhóm hàng nghìn mơ hồ; parser xử lý quy ước thập phân theo cấu hình, từ chối chuỗi không rõ nghĩa. JSON lưu số invariant, không lưu chuỗi đã gắn đơn vị để tính.
- Độ chính xác hiển thị không cắt dữ liệu double. Giá trị rất nhỏ có trạng thái/tooltip để phân biệt bị làm tròn với 0 thực.
- N/A/không hỗ trợ/không đọc được khác 0; mỗi trạng thái có text và tooltip. Không hiện 0 như một fallback cho lỗi API.

## 4. Vùng hiển thị, tọa độ và layer

### 4.1. Mặt phẳng và phạm vi

- Plan mặc định là XY tại tầng/cao độ đã chọn. Elevation ETABS là yêu cầu chính của phiên bản này: mặt đứng XZ/YZ hoặc theo trục thẳng xoay trong XY, dùng hệ cơ sở của view. Xem mục 4.4–4.8; không cần engine 3D.
- Chọn toàn tầng/cao độ, vùng chọn, group, loại đối tượng hoặc viewport hiện tại. Có thống kê visible/filtered/total.
- Dùng transform world→screen và inverse có đảo Y theo WPF; tọa độ trục, nhãn và hit-test dùng cùng transform. Tọa độ model lớn được trừ display origin để ổn định hình vẽ, giữ nguyên tọa độ nguồn.
- Dung sai chọn tầng theo Z là tham số có đơn vị, đề xuất ban đầu 1 mm rồi kiểm tra trên model thực. Dung sai này dùng cho view, không được gộp/đổi source ID hoặc sửa hình học.
- Frame ngoài mặt phẳng thể hiện theo chế độ Hide/Projection/Cut có legend. Cột/vách cắt bởi mặt phẳng có ký hiệu riêng; lỗ mở của area luôn được giữ.
- Fit có margin khoảng 5–8%; zoom quanh con trỏ, pan không đổi model; min/max zoom và xử lý bounds suy biến có kiểm thử.
- Stroke/chữ theo DIP để không phóng to vô hạn khi zoom. Bề rộng hình học theo đơn vị model; đây là hai chế độ khác nhau và có nhãn rõ.

### 4.2. Thứ tự vẽ từ dưới lên

0 nền; 10 grid; 20 fill area; 25 lỗ mở; 30 biên area; 40 frame/cột/vách; 50 assignment hiện có; 60 nháp/chênh lệch; 70 nhãn; 80 selection/hover; 90 cảnh báo/tooltip và thao tác đang kéo.

Tránh area fill che tải hoặc selection. Lớp nhãn được xử lý va chạm, không chỉ dựa vào ZIndex. Giữ bản đồ primitive→source ID để picking trúng đúng đối tượng sau khi batching.

### 4.3. Ma trận layer

Grid; nodes; frames; columns/cuts; walls/cuts; slabs/foundations; openings; supports/springs khi API cung cấp; section labels; object labels; dimensions; existing loads; draft loads; local axes; diagnostics.

Mỗi layer có Visible, Selectable, Opacity và khóa chỉnh hiển thị. Hidden/Filtered/Isolated chỉ ảnh hưởng view; không mang nghĩa Delete. Ghosted hiện mờ để tham chiếu nhưng mặc định không selectable.

### 4.4. Truy xuất Elevation từ ETABS

Khảo sát định nghĩa mặt đứng, grid system và khả năng API trong giai đoạn đầu. Không mặc định tồn tại một lệnh GetElevation trả đúng mọi nội dung đang vẽ trong cửa sổ ETABS.

1. Nếu API cung cấp đầy đủ định nghĩa view, đọc và chuyển thành ViewDefinition của QKS, xác minh với ETABS thực.
2. Nếu không có, đọc tọa độ node/frame/area, grid và story từ ETABS rồi dựng mặt đứng theo trục hoặc hai điểm do người dùng chọn. UI ghi rõ mặt đứng QKS được dựng từ mô hình ETABS.
3. Reader phải lấy cả cột, vách, cấu kiện nghiêng, offsets và lỗ mở khi nguồn cung cấp; không dùng trực tiếp hàm chỉ lọc dầm ngang/sàn ngang để gọi đó là mô hình mặt đứng đầy đủ.
4. Phân biệt object gốc với analysis mesh. Một object có thể tạo nhiều primitive hiển thị nhưng giữ cùng source ID; selection và Apply không được gán lặp.
5. Tham số view thuộc QKS, không tự tạo hoặc sửa view, grid hay hình học trong ETABS.

Bản đầu hỗ trợ trục thẳng và mặt phẳng đứng. Grid cong/gấp khúc yêu cầu người dùng chọn một đoạn/mặt phẳng cụ thể; mặt đứng trải phẳng theo đường cong nằm ngoài phạm vi ước lượng.

### 4.5. Hệ tọa độ và chọn mặt phẳng đứng

ViewDefinition lưu ViewId/Name, SourceModelBinding, Origin O, trục ngang đơn vị U trong XY, trục đứng V=(0,0,1), normal N=U×V, hướng nhìn, khoảng Z, planeTolerance và dải chiều sâu [dMin,dMax] nếu bật projection.

Với điểm P, tọa độ view là `u=dot(P-O,U)`, `v=dot(P-O,V)` và chiều sâu `d=dot(P-O,N)`. Renderer tiếp tục áp dụng view→screen với Y đảo theo WPF. Đảo hướng nhìn đổi U và N, giữ Z hướng lên; nhãn/hướng tải phải đổi nhất quán. Tọa độ nguồn không bị sửa.

Tách ba khái niệm:

- **Trong mặt phẳng:** khoảng cách tới mặt phẳng thỏa planeTolerance. Dung sai có đơn vị m và giá trị hiển thị mm, kiểm chứng trên model thật.
- **Cắt mặt phẳng:** frame/area chỉ giao mặt phẳng, không nằm hoàn toàn trong đó; vẽ giao tuyến/ký hiệu cắt, giữ source ID.
- **Chiếu theo chiều sâu:** chỉ đối tượng trong dải dMin–dMax được chiếu. Mặc định tham chiếu mờ, không chỉnh đoạn bằng kéo chuột; có thể chọn ID để xem thuộc tính với chế độ chọn tham chiếu rõ ràng.

Không gộp frame chỉ vì hình chiếu chồng nhau. Picking có danh sách ứng viên và chiều sâu, filter theo trục/group; tooltip cho biết nguồn, offset tới mặt phẳng và trạng thái chiếu/cắt. Suy biến do frame vuông góc view phải dùng marker, không biến nó thành một đoạn dầm có chiều dài bằng 0.

### 4.6. Hình học và định dạng trên Elevation

- Dầm/cột/frame hiện theo trục hoặc bề rộng đọc được; vách có polygon/biên/lỗ mở. Area ngang khi nhìn cạnh chỉ là giao tuyến/biên, không được tô như một diện tích đứng có thật.
- Hiện đường cao độ tầng với Z nguồn, tên tầng, tên grid và kích thước; label không bị đảo ngược khi đổi hướng nhìn.
- Dùng cùng bảng màu thuộc tính và QKS Navy như Plan. Đối tượng chiếu có nét/opacity riêng; selection/draft/error vẫn là overlay độc lập.
- ID, nhóm và bảng thuộc tính liên kết với Plan. Đối tượng ngoài view có thể còn trong selection nhưng UI phải thông báo, không âm thầm chuyển sang đối tượng gần nhất.
- Khi kéo điểm chia tải, quy đổi về tham số trên frame nguồn và chiều dài thực 3D I→J, không dùng chiều dài nét chiếu làm chiều dài gán. Bản đầu chỉ cho thao tác trực tiếp với frame đủ điều kiện trong mặt phẳng; đối tượng chiếu/cắt xem thuộc tính hoặc chỉnh bằng editor có validation riêng.

### 4.7. Tải tường, gió và sàn trên Elevation

| Nhóm | Giá trị hiển thị/soạn nháp | Điều kiện ghi |
|---|---|---|
| Tường | Chiều cao, cao độ, tuyến đỡ, vùng/lỗ mở do người dùng khai báo, q và phân đoạn trên dầm | Liên kết đúng source frame/assignment; không suy lỗ cửa kiến trúc từ một biên mô hình kết cấu không đủ dữ liệu |
| Gió | p/q/F/M theo cao độ, vùng tác động, mũi tên đón/hút khi phía mặt ngoài đã xác định | Ghi đúng loại tải/hệ trục/capability; không tự suy tiêu chuẩn, bề rộng truyền tải hoặc dấu đón/hút chỉ từ hình vẽ |
| Sàn | Biên nhìn cạnh, độ dày khi có, tải hiện có/nháp và nhận diện area nguồn | Gán đúng area và units tải mặt; không đổi kN/m² thành tải đường chỉ vì đang nhìn cạnh |

Vector tải được đổi sang hệ view để vẽ: thành phần trong mặt phẳng bằng mũi tên; thành phần vuông góc có ký hiệu vào/ra mặt phẳng. Hướng vật lý trong host và giá trị nguồn giữ nguyên, kể cả khi đảo hướng nhìn. Độ dài glyph có scale hiển thị và legend, không dùng làm giá trị tính tải.

### 4.8. Nghiệm thu Elevation

Đối chiếu thủ công một số mặt đứng ETABS trên model có trục thẳng/xoay, nhiều tầng, tầng lệch, dầm/cột nghiêng, vách/lỗ mở và đối tượng sau mặt phẳng. Kiểm tra cao độ, source ID, chiều I/J, hướng local, chiều tải và giao tuyến.

Các ca bắt buộc: đảo hướng nhìn hai lần trả về view tương đương; Plan↔Elevation giữ ID/selection/draft; hai frame chồng hình chiếu vẫn phân biệt; chọn vùng không gán lặp; kéo đoạn không dùng chiều dài chiếu; area nhìn cạnh không đổi đơn vị tải; đổi model không dùng view/draft cũ để ghi.

## 5. Hệ màu cấu kiện, màu thuộc tính và tải

![Hộp quản lý màu và định dạng đề xuất](design-2d/02-display-style-manager.png)

### 5.1. Palette mặc định cho nền sáng

| Thành phần | Mã sRGB | Định dạng mặc định |
|---|---|---|
| Nền viewport | #FFFFFF | Trắng |
| Nền panel | #F8FAFC | Xám rất nhạt |
| Chữ chính | #111827 | Dễ đọc trên nền sáng |
| Chữ phụ | #475569 | Thông tin thứ cấp |
| Grid | #CBD5E1 | Nét đứt mảnh |
| Dầm | #2563EB | Nét liền |
| Cột/cắt cột | #DC2626 | Biên/tô theo footprint |
| Vách/cắt vách | #7C3AED | Biên và hatch nếu cần |
| Sàn/móng area | #0F766E | Biên rõ, fill 10–18% opacity |
| Lỗ mở | #FFFFFF + #94A3B8 | Biên đứt, dấu chéo; không dùng cùng biểu diễn với area thiếu dữ liệu |
| Hover | #06B6D4 | Halo/outline tạm thời |
| Selection | #F59E0B | Outline ngoài 2,5–3 DIP + marker |
| Nháp | #8B5CF6 | Overlay đứt/chấm + badge Nháp |
| Đã xác minh | #15803D | Badge/icon thành công |
| Cảnh báo | #B45309 | Tam giác + text |
| Lỗi/xung đột | #B91C1C | Marker lỗi + text, không chỉ đổi màu hình học |
| Không có/không đọc được dữ liệu | #64748B | Hatch/nhãn N/A khác 0 |

Màu đỏ của cột là màu loại cấu kiện; lỗi luôn có biểu tượng/text riêng. Tương tự, màu tím tiết diện không có nghĩa là nháp nếu không có lớp nét/badge nháp.

### 5.2. Các chế độ tô màu

1. Theo loại cấu kiện.
2. Theo tiết diện/property.
3. Theo vật liệu.
4. Theo chiều dày (area)/kích thước đã chọn.
5. Theo group do người dùng chọn.
6. Theo nguồn/màu API nếu adapter cung cấp và giải mã đúng.
7. Theo giá trị tải của một pattern và một đại lượng xác định.
8. Theo chênh lệch hiện có–nháp.

Tại một thời điểm chỉ có một chế độ màu nền chính; selection/nháp/lỗi dùng lớp dấu hiệu riêng. Không chồng hai heatmap rồi để người dùng đoán ý nghĩa.

### 5.3. Quản lý màu thuộc tính

Bảng điều khiển gồm: Loại đối tượng, Property ID/tên, Giá trị/khoảng giá trị, số lượng, swatch màu, opacity, stroke, màu chữ, Visible và phạm vi áp dụng.

- Property rời rạc như Section/Material dùng palette categorical; chiều dày/tải số có thể chọn phân lớp hoặc gradient.
- Lưu mapping theo ModelBindingId + PropertyKind + SourcePropertyId/tên chuẩn do adapter cung cấp. Không dùng duy nhất tên tiết diện xuyên mọi model.
- Màu giữ nguyên khi refresh, đổi thứ tự đọc, ẩn/hiện hoặc mở lại phiên. Tạo property mới chỉ cấp màu cho property mới.
- Tự tạo bảng màu có preview; số nhóm lớn cần phối hợp label/hatch/filter, không hứa vô hạn màu phân biệt rõ.
- Hiện tại SectionColorHelper gán palette theo thứ tự _cache.Count và có fallback GetHashCode. Cơ chế này chưa phù hợp làm mapping bền vững; module mới cần bảng mapping được lưu/version hóa thay vì phụ thuộc thứ tự đọc hoặc hash runtime.
- Chỉnh/khóa từng swatch; sao chép palette; reset property đang chọn hoặc toàn preset; import/export cấu hình. Preset từ model khác phải có bước mapping/xác nhận.
- Lưu SourceColor riêng với OverrideColor. Lệnh Áp dụng hiển thị chỉ sửa style QKS. Ghi màu trở lại ETABS/SAFE không thuộc thao tác này.

### 5.4. Thứ tự giải quyết style

Mặc định loại cấu kiện → preset/theme → chế độ thuộc tính đang chọn → override hiển thị được bật cho đối tượng/nhóm. Khi bật heatmap, override màu nền được tạm ngừng để màu vẫn phản ánh đúng số; UI phải báo trạng thái này.

Sau base style, render overlay hover/selection/nháp/lỗi. Khi vừa chọn vừa lỗi: giữ outline chọn và đặt icon lỗi; không thay màu thuộc tính bằng một màu cuối cùng làm mất các trạng thái khác.

### 5.5. Heatmap và legend

- Cố định Source/Draft/Difference, pattern, đại lượng, đơn vị và tập đối tượng tính thang màu. Không dùng chung scale cho kN/m và kN/m².
- Chọn Auto range hoặc Fixed range. Khi so hiện có với nháp, dùng cùng range; không tự đổi màu theo selection mà không báo.
- Đại lượng không âm: sequential palette; đại lượng có dấu: diverging có tâm 0. Màu chỉ giá trị đại lượng đang chọn; vector/label thể hiện hướng tải.
- All zero hiển thị trạng thái toàn 0; missing/N/A dùng hatch riêng. Out-of-range có dấu hiệu và giá trị thực trong tooltip, không clamp âm thầm.
- Legend có tên chế độ, units, min/mid/max hoặc từng category và số lượng; nếu nhiều category, có tìm kiếm/lọc. Có export legend đi cùng hình.
- Màu nhiệt và màu trạng thái chỉ có ý nghĩa trong phạm vi legend. Có palette thay thế phù hợp người khó phân biệt màu và chế độ in đơn sắc.

## 6. Nét, chữ, nhãn và ký hiệu

### 6.1. Nét theo DIP

Grid 0,6–0,8 đứt; biên slab 1,0–1,2; dầm đường trục 1,5–2,0; cắt cột/vách 1,5–2,0; tuyến tải 1,5–2,0; selection 2,5–3,0 outline ngoài. Đây là giá trị ban đầu để thử DPI, không phải bề rộng cấu kiện theo model.

Có Solid/Dash/Dot/DashDot, scale nét khi in, bật/tắt antialias theo loại primitive. Khóa kích thước mũi tên và marker theo screen trong khoảng đọc được. Vùng fill không bao phủ border/label.

### 6.2. Chữ

Segoe UI mặc định; label cấu kiện 11–12 DIP, tải 12–13, tiêu đề panel 13–14, grid bubble 11–12, text phụ 10–11. Có slider và min/max, hỗ trợ Unicode, chọn font thay thế nếu thiếu glyph. WPF FontSize dùng DIP; 12 DIP tương đương 9 pt ở quy ước 96 DPI.

Màu chữ mặc định #111827, thông tin phụ #475569. Cho chọn chữ trung tính hoặc theo thuộc tính; mọi mode có halo/nền label tùy chọn và kiểm tra tương phản mục tiêu 4,5:1 cho chữ thường. Không chỉ dùng màu nhạt của fill làm màu chữ. Theme tối là preset riêng cần kiểm tra tương phản, không đảo màu tự động.

### 6.3. Mẫu nhãn

| Loại | Template đề xuất |
|---|---|
| Dầm | {Label} · {Section}; tùy chọn {UniqueName} |
| Cột/vách cắt | {Label} · {Section/Thickness} |
| Sàn/móng | {Label} · {Property} · h={Thickness:mm} |
| Tải đều đường | {Pattern}: q={Value} {Unit} |
| Tải biến thiên | {Pattern}: {q1}→{q2} {Unit}; đoạn {s1}–{s2} m |
| Tải mặt | {Pattern}: p={Value} {Unit}; {Direction} |
| Lực/mômen nút | {Pattern}: {Component}={Value} {Unit} |
| Chênh lệch | {Before}→{After}; Δ={Difference} {Unit} |

Template phải validate field theo loại đối tượng và thoát ký tự. Hiện label tải theo pattern đang lọc; nhiều pattern thì summary ngắn và tooltip đầy đủ, không chồng mọi chuỗi lên hình.

Đặt nhãn dầm ở midpoint hoặc từng segment, offset theo screen; sàn ở điểm bên trong polygon, không mặc định centroid nằm trong vùng hợp lệ khi có lỗ/đa giác lõm. Text dọc dầm được xoay để không bị đọc ngược.

Zoom xa: chỉ grid/tầng/nhóm quan trọng; zoom trung bình: tên/tiết diện; zoom gần: tải/đoạn/kích thước. Selection luôn được ưu tiên. Dùng spatial occupancy để xử lý trùng nhãn, cho bật Hiện tất cả và drag label; drag chỉ lưu offset hiển thị.

### 6.4. Hướng và trục

Có trục Global X/Y, local 1/2/3 và normal area khi đọc được. Tải trong mặt phẳng dùng mũi tên; vuông góc mặt bằng dùng ký hiệu vào/ra mặt phẳng và label dấu/chiều. Gravity ghi rõ -Z theo convention đã chuẩn hóa, không suy từ màu.

Ghi chú rõ s theo chiều I→J; tick dùng đúng khoảng cách. Ký hiệu lực và mômen khác nhau. Đổi đơn vị hoặc camera 2D không đổi hướng vật lý của assignment.

## 7. Inspector, tooltip và DataGrid

Inspector có ba nhóm: Đối tượng nguồn (chỉ đọc), Thiết lập tải nháp (chỉnh), Chẩn đoán/trạng thái. Cho copy UniqueName/giá trị, tìm đối tượng trên hình; thao tác source selection là command riêng có capability.

Tooltip gọn: ID/label, tầng/cao độ, property/material, kích thước/diện tích, pattern/tải đang lọc, giá trị nguồn/nháp, hướng/units và lỗi nếu có. Trường còn lại mở inspector, tránh tooltip che toàn viewport.

DataGrid chung: checkbox phạm vi Apply; trạng thái; object ID/label; level; loại; property; pattern; loại tải; hướng/CSys; đoạn/vùng; Before; After; Unit; Add/Replace/Delete; lỗi. Cột theo module được hiện/ẩn, sort/filter; ID đầy đủ luôn truy cập được.

Dòng nháp có badge tím; verified xanh; failed đỏ có icon; stale/xung đột có lý do. Chọn dòng không đồng nghĩa chọn checkbox để Apply. Select all chỉ áp dụng phạm vi filter đã hiển thị rõ. Có tổng selected/draft/valid/error và báo tập Apply đang gồm cả phần ngoài viewport nếu có.

## 8. Cấu hình lưu và MVVM

### 8.1. ViewModel và renderer

WorkspaceViewModel; ConnectionViewModel; ViewDefinitionViewModel (Plan/Elevation); ElevationSelectorViewModel; LayerManagerViewModel; SelectionViewModel; WallLoadEditorViewModel; WindLoadEditorViewModel; SlabLoadEditorViewModel; DisplayStyleViewModel; DraftTableViewModel.

ModelSnapshot/DraftStore/StyleRegistry tách nhau. Canvas overlay xử lý công cụ/nhãn chọn; custom DrawingVisual host vẽ hình học theo batch. Renderer không gọi COM. Reader/writer đi qua scheduler STA có ownership và cancellation theo batch; dữ liệu thuần mới được chuyển sang worker để lập scene/spatial index.

### 8.2. Cấu hình hiển thị

Profile có schemaVersion, profile ID/name, model binding tùy chọn, theme, background, layer styles, activeColorMode, property palette, label templates, units/precision, view definition (Plan/Elevation, basis O/U/V/N, tầng/cao độ, planeTolerance, projection depth, hướng nhìn), legend options và layout. JSON màu lưu #RRGGBB + opacity 0..1; khi tạo WPF Brush mới chuyển alpha sang dạng thích hợp. UI dùng Độ đục/Opacity %, phân biệt rõ opacity và transparency.

Lưu defaults người dùng trong LocalAppData; preset dự án và draft có thể export thành file riêng. Không lưu RCW/API object hoặc dùng file style làm nguồn tải. Mở profile model khác chỉ nhập style tương thích sau preview, không áp nháp sang model khác.

Nguồn màu API phải được adapter chuẩn hóa đúng sRGB/encoding thực tế; không giả định mọi API đều trả cùng một kiểu RGB/OLE color. Freeze Brush/Pen phù hợp khi dùng đa luồng; cấu hình đổi chỉ invalidation lớp liên quan.

## 9. Giai đoạn phát triển và dự toán thời gian

Phạm vi ước lượng: Plan + Elevation ETABS, Plan SAFE với phiên bản/capability được chốt, ba tab tải và luồng nháp/apply. Tải gió trong phần cơ sở là nhập tải đã xác định, hiển thị và quy đổi theo giả thiết được duyệt; chưa bao gồm engine tính theo tiêu chuẩn chưa chọn, bản đồ gió, mọi phiên bản host hoặc mặt đứng cong trải phẳng.

Giả định một lập trình viên WPF/CSI làm việc tập trung, tuần 5 ngày, có máy Windows với ETABS/SAFE được cấp phép và một kỹ sư nghiệp vụ phản hồi đều. Đã có bản sửa QKS hiện tại để tái sử dụng, nhưng viewer/MVVM mới chưa được triển khai. Thời lượng dưới đây là ước lượng, không phải cam kết.

| Giai đoạn | Công việc và đầu ra | Ngày làm việc | Điều kiện hoàn tất |
|---|---|---:|---|
| G0 — Chốt API và mẫu | Xác nhận ETABS/SAFE version, reader/capability, named elevation/grid API, model đối chứng, quy tắc tải và UI | 3–5 | Có bằng chứng đọc trên cả hai host; chốt phạm vi ghi, không giả định API view |
| G1 — Dữ liệu lõi | Snapshot node/frame/area, units/identity, Plan/Elevation ViewDefinition, projection/picking foundations, scheduler STA | 5–8 | Dữ liệu tổng quát đủ cột/vách; phép biến đổi và filter có test |
| G2 — Shell và Plan | WPF/MVVM QKS Navy, Plan renderer, zoom/pan, selection liên kết, style/nhãn/palette | 5–8 | Xem/lọc/chọn không ghi host; ID và màu bền vững |
| G3 — Elevation ETABS | Chọn trục/hai điểm, tầng/cao độ, direction/depth, dựng mặt đứng, giao tuyến, nhãn và liên kết Plan | 6–10 | Đối chiếu mặt đứng thật, đối tượng chồng chiếu và hướng tải đúng |
| G4 — Draft tường/sàn | Editor, segment theo chiều dài thật, load set, Before/After, validation, undo/redo, lưu nháp dùng chung hai view | 6–9 | Đổi view không nhân bản tải; sửa nháp không thay host |
| G5 — Apply và SAFE | Writer các capability đã chốt của ETABS/SAFE, conflict/preflight, backup, return codes, read-back và partial failure | 7–12 | Test dữ liệu đối chứng trên Windows cho từng nguồn; unsupported có lý do |
| G6 — Gió trực quan | Nhập tải theo phương/cao độ, biểu đồ Elevation, mapping loại đích và quy đổi đã duyệt, validation/read-back | 5–8 | Không có số demo gán thật; p/q/F/M và hướng không bị trộn |
| G7 — Ổn định và bàn giao | Hiệu năng, DPI, model lớn, profile, hồi quy, gói XLL và hướng dẫn | 5–8 | Bộ test host đạt; không coi build pass là đã kiểm thử ứng dụng |
| **Tổng phần việc cơ sở** | | **42–68** | Chưa cộng dự phòng |

Dự phòng khoảng 20–25% cho khác biệt API, hình học, hiệu năng và vòng sửa sau test: kế hoạch tổng **khoảng 50–85 ngày làm việc, tương đương 10–17 tuần**. Thời gian chờ SDK/model/phản hồi ngoài các giả định làm trễ thêm lịch. Sau G0 và prototype Elevation phải cập nhật lại dự toán theo bằng chứng.

### 9.1. Các mốc có thể dùng thử

- **Plan chỉ đọc:** sau G0–G2, khoảng 3–4 tuần làm việc theo khối lượng cơ sở.
- **Plan + Elevation ETABS chỉ đọc:** sau G0–G3, khoảng 4–6 tuần; chưa gán tải mới.
- **ETABS có nháp tường/sàn và Apply đã kiểm chứng:** sau G4 và phần ETABS của G5, khoảng 6–9 tuần cơ sở; còn phụ thuộc kết quả read-back/host test. Bản này chưa thay toàn bộ nghiệm thu cuối.
- **Bản V1 trong phạm vi cả ETABS/SAFE và ba tab, có dự phòng:** khoảng 10–17 tuần; SAFE chỉ mở những loại ghi đã được xác nhận ở G0/G5.

Bổ sung Elevation làm tăng khoảng 6–10 ngày cho viewer riêng và khoảng 2–5 ngày kiểm thử tích hợp/hướng tải so với Plan-only; đây là phần đã nằm trong tổng trên, không cộng thêm lần nữa.

### 9.2. Phần làm thay đổi dự toán

Engine gió theo một tiêu chuẩn/phạm vi đã chọn cần dự toán riêng sau khi chốt đầu vào và bộ số đối chứng; có thể tạm dành thêm khoảng 2–4 tuần cho một phạm vi giới hạn, không coi đó là ước lượng cho mọi tiêu chuẩn. Các mục như SAFE nhiều phiên bản, mặt đứng cong/trải phẳng, load redistribution phức tạp hoặc tạo area mới không tự nằm trong thời lượng cơ sở.

Tổ chức công việc theo G0→G1→G2→G3→G4→G5→G6→G7, có kiểm thử xuyên suốt. Adapter SAFE được khảo sát ở G0 và đọc dữ liệu từ G1/G2; không đợi cuối dự án mới phát hiện bất tương thích. 3D không thuộc các mốc này.

## 10. Nghiệm thu hiển thị và nghiệp vụ

- Dùng model có trục lệch/xoay, tọa độ lớn, frame nghiêng, nhiều tầng trùng label, vách/cột, sàn có lỗ và nhiều tiết diện; đối chiếu Plan và Elevation với host thực. Bao gồm mặt phẳng chứa cấu kiện, cắt cấu kiện và dải chiếu sâu.
- Giữ đúng world→screen→world, selection tolerance theo pixel, Windows DPI 100/125/150/200%, màn hình nhỏ và nhiều màn hình.
- Kiểm tra palette trước/sau refresh, thứ tự đọc thay đổi, thêm property mới, đổi theme, mở lại profile và import preset model khác.
- Kiểm tra đồng thời property color + hover + selection + draft + error; legend giải thích được và có ký hiệu ngoài màu.
- Test chữ đè nhau, text tiếng Việt, label dài, số âm, thập phân, giá trị rất nhỏ, N/A và 0.
- Test tải tường biến thiên/đoạn, gió nhiều hướng, lực/mômen nút nếu adapter hỗ trợ, sàn có lỗ và nhiều pattern.
- Apply có preflight/conflict, backup, kiểm tra return code và read-back. Không coi Undo nháp là rollback đã ghi; báo partial failure đúng.
- Benchmark 1.000/10.000/50.000 đối tượng, đo riêng API read, dựng scene, FPS pan/zoom, hit-test và RAM. Mục tiêu ban đầu 10.000 đối tượng: pan/zoom khoảng 30 FPS và chọn dưới 100 ms trên cấu hình Windows 16 GB RAM phổ thông; chỉ chốt sau đo thực.
- Chế độ xem, đổi màu, đổi nhãn, lọc, ẩn/hiện và soạn nháp không được ghi thuộc tính/tải hoặc Save model nguồn.

## 11. Phạm vi bàn giao thiết kế đợt này

1. Hai mockup PNG: layout chính và hộp màu/định dạng.
2. Tài liệu mô tả nghiệp vụ ba nhóm tải, hợp đồng dữ liệu, định dạng và palette.
3. Layout/VM/module map, đặc tả Elevation ETABS, các mốc nghiệm thu và dự toán 50–85 ngày làm việc để chuyển thành công việc phát triển.

Chưa có giao diện WPF chạy thực trong đợt lập kế hoạch này. Phiên bản SAFE, tiêu chuẩn gió và model đối chứng sẽ được xác minh/chốt ở mốc 0; không suy diễn từ ảnh tham khảo.
