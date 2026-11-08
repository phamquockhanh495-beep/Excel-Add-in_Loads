# Kế hoạch nâng cấp QKS Wind — hai phương thức gán tải

Trạng thái: kế hoạch triển khai, chưa phải tính năng đã hoàn thành hoặc bản build mới.

## 1. Mục tiêu và phạm vi

Xây dựng cửa sổ **QKS | TẢI GIÓ** gồm hai tab độc lập:

1. **Diaphragms · User Loads**: quản lý lực và mô men theo tầng/diaphragm; ghi vào Load Pattern ETABS do người dùng chỉ định, theo cơ chế User Loads.
2. **Dầm biên · Model 2D**: chọn dầm trên mặt bằng, nhập tải đường, xem trước và gán tải vào Load Pattern chỉ định.

Đợt này sử dụng giá trị tải đã được tính từ Excel hoặc nhập tay. Giữ nguyên phạm vi trong `WIND-STORY-INPUT.md`: không tự tính tải theo TCVN, không tự chuyển áp lực thành lực hoặc tải đường. Chức năng tính gió theo tiêu chuẩn là một hạng mục riêng nếu được yêu cầu sau này.

Giao diện quản lý tải trọng 2D hiện tại tiếp tục phục vụ đọc, xem và xuất hồ sơ. Công cụ gán gió nằm trong module Wind riêng, tái sử dụng bộ vẽ và thao tác chọn của Model 2D. Không phụ thuộc AutoCAD để gán tải vào các dầm đã có trong ETABS.

## 2. Hiện trạng đã kiểm tra trong mã nguồn

| Thành phần | Hiện trạng | Hướng xử lý |
|---|---|---|
| `Views/WindLoadView.xaml(.cs)` | Có cửa sổ gió, đọc tầng; nút gán hiện chỉ báo chưa sẵn sàng; danh sách tiết diện có giá trị cố định | Thay nội dung bằng hai tab; lấy lựa chọn từ model thực |
| `Modeling/Core/WindLoads.cs` | Có UserWindRow, baseline, kiểm tra thay đổi, backup, ghi và đọc lại; định danh dòng hiện chỉ bằng DiaphragmKey | Mở rộng định danh sau khi xác minh API; giữ cơ chế kiểm tra hiện có |
| `ViewModels/VisualWorkspaceWind.cs` | Có nhập theo Story, lưu/mở tài liệu gió; phụ thuộc ngữ cảnh nháp của viewer cũ | Tách thành ViewModel riêng cho Wind |
| `Services/Modeling/EtabsLoadApplyHost.cs` | Có GetUserLoad/SetUserLoad; kiểm tra loại Wind, nguồn và dữ liệu; đơn vị kN–m | Bọc bằng adapter chuyên biệt, bổ sung khả năng theo phiên bản |
| `StoryWindInput`, `WindWorkbook`, `WindExcelSelection` | Có nhập Excel/CSV/vùng chọn và kiểm tra dữ liệu | Tái sử dụng parser, nâng schema cho nhiều Load Sets |
| Model 2D | Có chọn cấu kiện, lưới trục, màu sắc, nhãn, tầng và mặt cắt | Tái sử dụng renderer và selection; thêm lớp tải gió xem trước |

**Nút thắt bắt buộc giải quyết:** chữ ký `AutoWind.SetUserLoad(Name, Diaph, FX, FY, MZ, X, Y)` đang dùng không có tham số Story, Load Set hoặc Reversible. Mã hiện tại chỉ cho sửa khóa đã đọc; chưa đủ cơ sở để tự tạo mọi dòng như hộp thoại ETABS. Không thể hoàn thiện yêu cầu chỉ bằng cách bật lại nút gán.

## 3. Bố cục WPF chung

```text
QKS | TẢI GIÓ                                      Thu nhỏ / Phóng to / Đóng
Model ETABS · phiên bản · trạng thái kết nối        Kết nối | Đọc lại
----------------------------------------------------------------------------
[ 1. Diaphragms · User Loads ] [ 2. Dầm biên · Model 2D ]
                        Nội dung riêng của tab
----------------------------------------------------------------------------
Trạng thái / số lỗi           Lưu cấu hình | Kiểm tra | Xem trước | Gán ETABS
```

- Header navy đen, góc vuông, icon đồng bộ CES/QKS; popup con cùng mẫu.
- Dùng Grid co giãn, GridSplitter ở các vùng phụ; nội dung chính luôn được ưu tiên diện tích.
- Typography qua resource chung: nội dung 12–13 DIP, heading 14–16 DIP, có kiểm tra DPI 100/125/150/200%. Đây là giá trị khởi đầu để nghiệm thu trên Windows.
- Các thiết lập dài mở bằng nút và popup con có Áp dụng/Hủy; không nhồi nhiều khối ẩn/hiện vào sidebar.
- Hai tab giữ riêng pattern, bộ dữ liệu đang sửa, lựa chọn, viewport và lịch sử Undo/Redo trước khi ghi.
- Nút Gán chỉ hoạt động cho tab hiện tại và bộ thay đổi đã kiểm tra. Đổi dữ liệu, model hoặc pattern làm mất hiệu lực kết quả kiểm tra cũ.
- Khi đổi kết nối/model, hiển thị rõ dữ liệu đang sửa thuộc nguồn nào; không tự chuyển dữ liệu gán sang model mới.
- Các thao tác tính toán/chuẩn bị dữ liệu có thể chạy nền. Lệnh COM ETABS chạy tuần tự trên luồng STA có quyền sở hữu kết nối; không dùng Task.Run tùy ý cho COM.

## 4. Tab 1 — Diaphragms / User Loads

### 4.1 Bố cục và thao tác

**Thanh công cụ trên bảng:** Load Pattern đích, Đọc User Loads, Nhập Excel, Lấy vùng Excel, Xuất mẫu, Thiết lập Load Sets.

**Thiết lập theo Pattern:** số Load Sets, chọn Load Set đang sửa, `Loads are Reversible for Combos`. Việc tăng/giảm số set phải có xem trước dữ liệu thêm/xóa; không âm thầm xóa set đang có dữ liệu.

**Bảng chính:**

| Chọn | Story | Cao độ (m) | Diaphragm | Fx (kN) | Fy (kN) | Mz (kN·m) | X (m) | Y (m) | Trạng thái |
|---|---|---:|---|---:|---:|---:|---:|---:|---|
| ✓ | Tầng từ ETABS | Chỉ đọc | Danh sách đúng tầng | Nhập | Nhập | Nhập | Nhập | Nhập | Mới / Sửa / Lỗi |

Pattern và Load Set nằm ở thanh chọn; tránh lặp lại trên mọi cột. Khi xem tổng hợp nhiều set, bổ sung cột Load Set.

- Tầng sắp theo cao độ, không sắp tên chữ; lọc theo tầng/diaphragm và tìm nhanh.
- Thêm dòng, nhân bản, xóa dòng, dán nhiều ô từ Excel, sửa nhiều dòng đã chọn.
- Một tầng có nhiều diaphragm được hiển thị thành nhiều dòng; không gán cùng lực tầng cho tất cả diaphragm một cách tự động.
- Ô số chưa nhập phải khác 0; báo lỗi tại ô và tổng hợp lỗi để nhảy đến dòng cần sửa.
- Bảng ảo hóa dòng/cột để xử lý model nhiều tầng; không dùng thông báo popup cho từng ô lỗi.
- Vùng dưới bảng cho biết số dòng hợp lệ/thay đổi và tổng Fx/Fy của **Load Set hiện tại**. Không cộng các Load Sets thay thế thành một trường hợp tải.
- Cho phép xem mô men tổng quanh một điểm O được công bố: Σ[Mz + (X−xO)Fy − (Y−yO)Fx]. Không gọi riêng ΣMz là toàn bộ mô men xoắn của hệ tải.

### 4.2 Quy tắc dữ liệu và tọa độ

- Định danh nghiệp vụ dự kiến: `Model + Pattern + LoadSet + Story + Diaphragm`; adapter ánh xạ sang khóa host đã xác minh.
- D1 ở hai tầng khác nhau là hai mục tiêu khác nhau; tuyệt đối không ghép chuỗi Story/D1 để đoán khóa API.
- Fx/Fy theo trục Global và có dấu; Mz theo quy ước ETABS được đối chứng bằng model thử.
- X/Y là tọa độ đặt lực; không tự lấy tâm hình học làm tâm cứng hoặc tâm khối lượng.
- Mặc định sửa X/Y nghĩa là chỉnh vị trí đặt lực, giữ giá trị Mz đã nhập. Nếu bổ sung lệnh riêng “Chuyển điểm đặt, giữ tương đương”, dùng `Mz_new = Mz_old + (X_old−X_new)Fy − (Y_old−Y_new)Fx` và cho xem trước. Không tự cộng mô men lệch tâm hai lần.
- Reversible được lưu đúng tùy chọn của ETABS; không thay thế bằng cách tự sinh hai dòng tải trái dấu.
- Thêm/xóa hàng và ghi nhiều Load Sets chỉ mở khi adapter xác nhận hỗ trợ đầy đủ. Nếu chưa có đường API phù hợp, hiển thị giới hạn chính xác; không báo đã hoàn thành Tab 1 chỉ với sửa khóa cũ.

### 4.3 Chiến lược API cần xác minh đầu tiên

1. Dùng model thử trên ETABS với hai tầng dùng cùng D1, một tầng có D1/D2, hai Load Sets và tùy chọn Reversible.
2. Đọc metadata SDK và các bảng User Wind do chính host công bố; xác định trường Story, Diaphragm, LoadSet, tọa độ, đơn vị và khả năng chỉnh sửa.
3. Ưu tiên API typed nếu đáp ứng đủ. Nếu cần DatabaseTables, xác minh table key, schema, quyền chỉnh sửa và kết quả import trên từng phiên bản hỗ trợ.
4. Kiểm tra ghi một dòng có làm thay đổi hàng khác, số set, Reversible hoặc cấu hình pattern không.
5. Đọc lại qua API và mở hộp thoại ETABS đối chứng. Ghi rõ ma trận phiên bản/tính năng đã kiểm thử.
6. Chốt adapter và migration dữ liệu trước khi hoàn thiện DataGrid có thao tác thêm/xóa set.

Không tự đổi pattern đang dùng auto wind theo tiêu chuẩn thành User Loads. Pattern mới, nếu hỗ trợ tạo trong QKS, phải là thao tác riêng, có tên/loại/cơ chế tải hiển thị rõ trước khi tạo.

## 5. Tab 2 — Dầm biên / Model 2D

### 5.1 Bố cục

```text
Tầng / Pattern / Bộ tải       Model 2D                  Thiết lập tải
Danh sách nhóm dầm            Lưới trục + cấu kiện      Global X / Global Y
Chọn theo thuộc tính         Tải đã có                 q1 / q2 (kN/m)
Lọc Section/Label/ID          Tải chuẩn bị gán          Đoạn bắt đầu / kết thúc
                                                      Dầm đã chọn
                                                      Tổng lực dự kiến
----------------------------------------------------------------------------
Bảng gán: Story | UniqueName | Label | Pattern | Direction | s1 | s2 | q1 | q2
```

Bảng gán thuộc module Wind, có thể thu gọn; không đưa bảng nháp trở lại viewer quản lý tải trọng chỉ đọc.

- Page Up/Down chuyển tầng; zoom/pan, vừa khung, isolate/hide/show all và menu chuột phải dùng cơ chế sẵn có.
- Select theo Section, UniqueName, Label; chỉ các đối tượng Beam hợp lệ được đưa vào danh sách gán.
- Dầm đang chọn màu đỏ đô; phân biệt tải hiện hữu và tải chuẩn bị gán bằng nét, nhãn và chú giải.
- Nhãn tải gọn: `Pattern – giá trị`; popup chi tiết có I/J, đoạn tải, hướng, đơn vị và thao tác thay thế/thêm.
- Hiển thị mũi tên Global X/Y có dấu; dùng cùng phép chiếu Plan/Elevation đã được kiểm tra.
- Có công cụ “Gợi ý dầm biên”, nhưng danh sách đề xuất phải được người dùng kiểm tra. Không mặc định mọi biên lỗ mở hoặc mọi dầm trên tầng đều là dầm đón gió.

### 5.2 Nhập và gán tải

- Đầu vào đợt đầu là q1/q2 kN/m. q2 trống có thể dùng q1 theo quy tắc nhập đã công bố; các trường bắt buộc khác không tự biến thành 0.
- Global X/Y và dấu phải được xem trước. q1/q2 gắn với thứ tự I→J, không phụ thuộc hướng dầm đang được vẽ trên màn hình.
- Mặc định toàn dầm; popup nâng cao chọn đoạn tải bằng khoảng cách thực hoặc tỷ lệ, với `0 ≤ s1 < s2 ≤ L` sau quy đổi đơn vị.
- Nhập q=5 kN/m trên ba dầm nghĩa là mỗi dầm nhận q=5 trên đoạn được chọn; không tự chia 5 cho ba dầm.
- Khi sao chép cấu hình qua tầng khác, tìm lại dầm theo identity/hình học đã kiểm tra; không chỉ dựa trên Label có thể trùng hoặc layout khác nhau.
- Cho phép lưu nhóm dầm theo tầng và hướng đón gió. Model thay đổi làm nhóm cần kiểm tra lại.
- Giữ riêng `Thay trong phạm vi đã chọn` và `Thêm vào tải hiện hữu`; xem trước kết quả hợp nhất. Không dùng tùy chọn Replace của API để vô tình xóa toàn bộ tải khác trên cùng dầm/pattern.
- Nhấn Gán lại với cùng bộ thay đổi đã ghi phải thành không có thay đổi, không nhân đôi tải. Muốn cộng thêm một lần nữa cần tạo thao tác mới có kết quả trước/sau rõ ràng.

### 5.3 Đối chiếu lực

Với đoạn tải tuyến tính có chiều dài thực L và tải theo trục Global trên đơn vị chiều dài dầm, lực thành phần là `F = (q1 + q2)L/2`. Kiểm tra mô men bằng tích phân theo vị trí trên đoạn; phải xử lý được q đổi dấu và lực tổng bằng 0.

Nếu sau này bổ sung kiểu tải theo chiều dài hình chiếu, phải khai báo thành chế độ riêng và kiểm tra đúng cờ API. Không dùng lẫn công thức chiều dài thực và hình chiếu.

Phân phối một lực tầng tổng xuống nhiều dầm là hạng mục mở rộng riêng: cần người dùng chọn trọng số hoặc bề rộng đón gió, kiểm tra tổng lực và mô men tương đương. Không tự phân phối đều trong phiên bản nhập q trực tiếp.

## 6. Quy trình ghi ETABS dùng chung

1. **Đọc nguồn:** model, phiên bản, đơn vị, trạng thái khóa, pattern, hình học và tải liên quan.
2. **Chuẩn bị:** nhập/chỉnh dữ liệu chỉ trong bộ làm việc của QKS; chưa ghi ETABS.
3. **Kiểm tra:** loại pattern, đích tồn tại, đơn vị, số hữu hạn, trùng dòng, phạm vi ghi và hỗ trợ của adapter.
4. **Xem trước trước/sau:** liệt kê thêm/sửa/xóa, các hàng giữ nguyên, tổng lực và phạm vi tầng/set/dầm.
5. **Gán ETABS:** người dùng chủ động bấm Gán trên bộ thay đổi đã kiểm tra; đọc lại baseline để phát hiện chỉnh sửa bên ngoài, tạo backup đã xác minh, ghi tuần tự và lưu nhật ký.
6. **Đọc lại:** so sánh toàn bộ phạm vi liên quan, kể cả dữ liệu phải giữ nguyên; chỉ báo thành công sau khi khớp.
7. **Đồng bộ giao diện:** cập nhật tải đã đọc, đánh dấu bộ thay đổi đã ghi, phục vụ xuất báo cáo.

Quy tắc chung:

- Pattern Tab 1 dùng Wind/User Loads; Tab 2 dùng Wind phù hợp cho tải gán trực tiếp, không đang tự sinh tải gió khác.
- Mặc định không trộn hai phương pháp trong cùng pattern; kiểm tra cả tải trực tiếp và User Loads để tránh cộng trùng.
- Không tự mở khóa model; hiển thị hành động cần thực hiện khi model không cho ghi.
- Khi phải đổi đơn vị host để ghi, khôi phục đơn vị ban đầu trong finally và ghi nhận nếu khôi phục thất bại.
- Không cam kết giao dịch nguyên tử nếu API không hỗ trợ. Nếu lỗi hoặc hủy sau một số lần ghi, báo chính xác phần đã ghi/đã xác minh/chưa ghi; không báo “đã hủy hoàn toàn”.
- Dừng ở ranh giới an toàn giữa các lần gọi API. Khôi phục từ backup là hành động riêng, không tự ghi đè những sửa đổi khác của người dùng.
- Giữ nguyên pattern, tải và hàng ngoài phạm vi đã chọn; không sửa combinations hoặc chạy analysis tự động.

## 7. Lưu cấu hình, nhập/xuất và truy vết

- Lưu tùy chọn giao diện riêng với dữ liệu tải: kích thước cửa sổ, tab cuối, cỡ chữ, màu, bố cục và cách hiển thị.
- Lưu bộ dữ liệu Wind có schema version, model identity, phương thức, pattern, Load Set, dữ liệu đầu vào, nguồn Excel và baseline tham chiếu.
- Di trú tài liệu cũ: giữ dữ liệu nhưng đánh dấu mapping Story–diaphragm chưa xác minh; không tự coi là đủ điều kiện gán.
- Mở lại dữ liệu không tự ghi vào ETABS; đọc lại đích và kiểm tra thay đổi trước lần Gán tiếp theo.
- Import Excel tiếp tục dùng `.xlsx/.xlsm`, CSV hoặc vùng chọn; không chạy macro và không coi công thức chưa có giá trị cache là số 0.
- Mẫu Tab 1 bổ sung LoadSet và trường tọa độ/đơn vị rõ ràng; Reversible và số set thuộc cấu hình pattern.
- Mẫu Tab 2 gồm Story, Pattern, UniqueName, Direction, đoạn tải, q1/q2 và chế độ cập nhật.
- Excel báo cáo tách theo phương thức, tầng và pattern. PDF Loading Case Plan tái sử dụng bộ xuất hiện có cho tải đã đọc lại; nếu cho xuất bản xem trước phải ghi rõ “Chưa gán ETABS”.
- Nhật ký có thời điểm, model/phiên bản, pattern, thao tác, giá trị trước/sau, kết quả read-back và vị trí backup.

## 8. Tổ chức mã đề xuất

Các tên dưới đây là thiết kế dự kiến, chưa phải lớp đã được triển khai:

| Lớp/module | Trách nhiệm |
|---|---|
| WindWorkspaceView / WindWorkspaceViewModel | Cửa sổ chung, kết nối và điều phối hai tab |
| DiaphragmWindViewModel | Bảng tầng, Load Sets, tọa độ và kiểm tra ô |
| BeamWindViewModel | Chọn dầm, nhóm dầm, q1/q2, đoạn tải và overlay |
| WindProject / WindPattern / WindLoadSet | Tài liệu nghiệp vụ có version, độc lập với WPF |
| WindValidation / WindChangePlan | Kiểm tra đầu vào, xây dựng kết quả trước/sau |
| IWindHostAdapter / EtabsWindAdapter | Capability, identity, đọc/ghi theo API hoặc bảng đã xác minh |
| WindApplyService | Backup, kiểm tra baseline, ghi, read-back và nhật ký |
| WindSettingsStore / WindReportService | Lưu cấu hình, nhập/xuất và báo cáo |

Tách core toán học và kiểm tra khỏi COM/WPF để test bằng dữ liệu mẫu. Tái sử dụng code parser, baseline và writer sau khi rà soát, không sao chép nguyên ViewModel cũ có phụ thuộc nháp của viewer.

## 9. Các chặng thực hiện và đầu ra

| Chặng | Công việc | Điều kiện hoàn tất |
|---|---|---|
| 1. Xác minh ETABS | Model thử, schema User Loads, Story/Diaphragm, nhiều set, Reversible, cơ chế Replace tải dầm | Có ma trận capability và bằng chứng đọc–ghi–đọc lại trên host |
| 2. Khung WPF và dữ liệu | Hai tab, style CES/QKS, model dữ liệu, import, lưu cấu hình, migration | Hai tab giữ trạng thái riêng; thao tác nhập/lưu không ghi host |
| 3. Tab Diaphragms | Bảng lực, Load Sets, tọa độ, preview và writer đã xác minh | Ghi đúng pattern/set/tầng/diaphragm, không đổi hàng ngoài phạm vi |
| 4. Tab Dầm biên | Tích hợp Model 2D, chọn dầm, q/đoạn tải, preview và gán | Kết quả hướng/dấu/I–J/đoạn tải đúng, bảo toàn tải không liên quan |
| 5. Hồ sơ và trải nghiệm | Báo cáo, cấu hình, hiệu năng, keyboard, DPI và lỗi/hủy | Báo cáo khớp read-back; UI sử dụng được ở kích thước màn hình mục tiêu |
| 6. Nghiệm thu và đóng gói | Regression, kiểm thử Windows/ETABS, build và tài liệu | Gói XLL có version, thời điểm build, hash, hướng dẫn và giới hạn đã xác nhận |

Chặng 1 quyết định khối lượng adapter; chỉ chốt thời lượng sau khi xác minh được cơ chế nhiều Load Sets. Có thể hoàn thiện bố cục WPF trong khi chờ host, nhưng không công bố hỗ trợ gán đầy đủ nếu chưa đạt kiểm thử host.

## 10. Bộ nghiệm thu tối thiểu

**User Loads:**

- Cùng D1 trên hai tầng được gán hai lực khác nhau mà không chồng ghi.
- Hai diaphragm trong một tầng, hai Load Sets, Fx/Fy trái dấu, Mz khác 0 và X/Y lệch gốc.
- Thêm/sửa/xóa hàng, giảm số set và giữ đúng Reversible qua lần đọc lại.
- Giá trị 0 khác ô trống; tên tầng/diaphragm không tồn tại bị chặn trước ghi.
- Tải/mô men tại điểm đặt đối chiếu đúng với hộp thoại ETABS; không cộng lệch tâm hai lần.

**Dầm biên:**

- Dầm thuận/ngược I–J, dầm xiên, đoạn tải một phần, q đều và hình thang, q đổi dấu.
- Chọn nhiều dầm trên các tầng có bố trí khác nhau; không gán nhầm cột/vách hoặc dầm ngoài phạm vi.
- Thay một đoạn tải vẫn giữ các đoạn/hướng/tải hiện hữu không liên quan.
- Gán lại cùng thao tác không nhân đôi; tổng lực/mô men preview khớp dữ liệu đọc lại trong dung sai công bố.

**Tích hợp và UI:**

- Model khóa, đổi model giữa lúc làm việc, ETABS bị đóng, lỗi API giữa lô, hủy giữa lô, lỗi khôi phục đơn vị.
- Pattern auto wind không phù hợp, tải hai phương thức cùng pattern, import lỗi và cấu hình cũ.
- Chuyển tab không mất dữ liệu; undo chỉnh sửa trước ghi không bị hiểu là undo ETABS.
- Kiểm tra DPI, cửa sổ thu nhỏ/phóng to, paste nhiều dòng, đổi tầng nhanh, viewport và điều hướng bàn phím.
- Kiểm thử core tự động và kiểm thử thực tế trên Windows/Excel/ETABS là hai lớp độc lập. Build thành công không thay thế nghiệm thu API trên host.

## 11. Kết quả bàn giao dự kiến

1. Cửa sổ WPF hai tab hoàn chỉnh và popup cấu hình đồng bộ CES/QKS.
2. Mẫu Excel riêng cho Diaphragms và dầm biên; tài liệu đơn vị, hướng và quy tắc cập nhật.
3. Bộ model thử, bảng kết quả kiểm thử và danh sách phiên bản ETABS đã xác minh.
4. Nhật ký/read-back và báo cáo tải gió theo phương thức/tầng/pattern.
5. Bản build mới có thông tin nhận diện để phân biệt với XLL cũ; ghi rõ các capability còn giới hạn nếu có.
