# QKS Wind — WPF hai phương thức và User Loads theo bảng

Bản triển khai 08/11/2026. Đây là bản kiểm thử tích hợp: đã build và kiểm thử core tự động; chưa chạy Excel/WPF/ETABS thực tế trong môi trường Linux này.

## Những phần đã triển khai

- Thay cửa sổ tải gió cũ bằng hai tab: **Diaphragms · User Loads** và **Dầm biên · Model 2D**.
- Dữ liệu soạn của Wind độc lập với giao diện quản lý tải trọng 2D chỉ đọc.
- Nhập giá trị gió đã tính từ Excel/CSV/vùng chọn Excel; không tự tính hệ số TCVN hoặc quy đổi áp lực.
- Header navy, popup con đồng bộ, bố cục co giãn, DataGrid ảo hóa, các nút chọn/hiển thị và màu chữ/cấu kiện.
- Lưu bộ gió `.qkswind.json`, cấu hình giao diện `%LocalAppData%\QKS\Wind\ui.json`; không tự ghi ETABS khi mở lại.
- Xem trước trước/sau, backup, journal, ghi và read-back. Thay đổi dữ liệu sau preview làm mất hiệu lực nút Gán.

## Tab 1 — hai đường truy cập dữ liệu

### Bảng khóa API hiện hữu

1. Chọn Load Pattern loại Wind, Auto Lateral Load = User Loads đã có trong ETABS.
2. Đọc User Loads. Chỉ sửa Fx/Fy/Mz/X/Y của các khóa do API trả về.
3. Đối chiếu Story với khóa trong ETABS, nhập hoặc dán Fx…Y; ô trống khác 0.
4. Xác nhận ánh xạ, xem trước rồi Gán ETABS.

SDK hiện có chỉ cung cấp `GetUserLoad`/`SetUserLoad` không có Story/Load Set riêng. Do đó giao diện không đoán D1 thuộc tầng nào và không tự tạo khóa mới. Dán theo thứ tự dòng đang hiển thị, kể cả sau khi sắp xếp. Undo/Redo áp dụng cho các chỉnh sửa trong QKS, không hoàn tác ETABS đã ghi.

### Bảng nâng cao: nút Story / Load Sets

Popup đọc bảng thực từ `DatabaseTables.GetAllTables`, schema từ `GetAllFieldsInTable` và dữ liệu từ `GetTableForEditingArray`.

Điều kiện mở chế độ bảng:

- Tên bảng do host trả về thuộc nhóm User Wind.
- Nhận diện duy nhất các field Pattern, LoadSet, Story, Diaphragm, Fx, Fy, Mz, X, Y.
- Metadata đơn vị của từng field phải khớp hệ đơn vị hiện tại; giá trị giao diện chuyển về kN và m.
- Dữ liệu không có identity thiếu/trùng. QKS không tự suy diễn các ô trống là giá trị tiếp diễn từ dòng trên.

Khả năng có điều kiện:

- Một dòng được định danh bằng **Pattern + Load Set + Story + Diaphragm**; cùng D1 trên nhiều tầng hoặc nhiều set được phân biệt.
- Number of Load Sets và Reversible chỉ cho chỉnh khi schema có đủ cả hai field. Nếu host không cung cấp, giữ nguyên; số set lớn nhất nhìn thấy không được trình bày như số set đã cấu hình.
- Thêm/nhân bản identity mới chỉ mở khi các field của schema đều đã nhận diện và có thể import. Nếu có field bổ sung, giữ nguyên chúng khi sửa identity hiện hữu; không tự tạo giá trị mặc định cho trường chưa biết.
- Story–Diaphragm được kiểm tra từ assignments thực của AreaObj và PointObj theo từng tầng. Kiểm tra lại trước khi ghi.
- Có lọc theo Load Set, thêm/xóa/nhân bản, Undo/Redo, lưu/mở `.qkswindsets.json`, nhập/xuất Excel với LoadSet, NumberOfLoadSets, Reversible.
- Schema chưa nhận diện, đơn vị không khớp hoặc field không importable sẽ báo lý do và không ghi. Nút **Thông tin API** xuất metadata/dữ liệu thực để bổ sung adapter, không chỉnh model.

**Lưu ý về phiên chỉnh sửa bảng:** CSI `ApplyEditedTables` áp dụng toàn bộ edit buffer. QKS yêu cầu người dùng tick rõ việc bắt đầu phiên bảng riêng, đồng ý hủy các chỉnh sửa bảng ETABS khác chưa Apply. Sau đó QKS dùng `CancelTableEditing` trước khi nạp bảng của mình. Không tự thực hiện thao tác này khi chỉ đọc bảng hoặc chưa xác nhận.

Đường ghi bảng kiểm tra model, pattern, assignments, baseline; backup; kiểm tra lại; nạp bảng và Apply; so sánh read-back cả pattern ngoài phạm vi và field phải giữ. Import lỗi, cảnh báo hoặc kết quả không khớp được ghi vào journal; không giả định rollback nguyên tử. Sau khi có khả năng đã ghi, đóng popup và đọc lại model trước lần ghi tiếp.

Chưa có bằng chứng nghiệm thu trên host cho các alias field hiện hỗ trợ. Nhận diện schema tại runtime không thay thế kiểm thử ETABS thực tế. Không tuyên bố hỗ trợ toàn bộ phiên bản ETABS chỉ dựa trên build thành công.

## Tab 2 — dầm biên

- Dùng Model 2D, lưới trục, Extrude, chọn theo Section/Label/UniqueName, isolate/hide/show all, Page Up/Down, mặt cắt hai điểm.
- Chỉ dầm hợp lệ đang chọn và hiện trong vùng xem mới nhận tải.
- Nhập Global X/Y, q1/q2 kN/m có dấu; q2 trống dùng q1.
- Đoạn tải theo tỷ lệ r1/r2 hoặc mét thực dọc I→J.
- Bảng bộ gán có thể chọn dòng để sửa thông số/đoạn tải, xóa, Undo/Redo.
- Cộng thêm hoặc thay trong đoạn. Với **WindLine**, thay tải giữ nguyên tải cũ ngoài đoạn và nội suy q tại biên cắt; không thay đổi hành vi tải tường cũ.
- Tổng lực và mô men quanh O=(0,0) tách riêng từng pattern, không cộng các phương án tải thay thế.
- Xem tải hiện hữu hoặc tải chuẩn bị gán; lớp nét tím đứt đánh dấu bộ soạn.
- Không tự nhận diện mọi biên ô sàn/lỗ mở là dầm biên. Chưa triển khai phân phối lực tầng tổng, áp lực gió hoặc tính gió theo tiêu chuẩn.

Nếu một pattern trên dầm có loại tải mà writer hiện chưa thể phát lại an toàn, Apply sẽ từ chối trước khi ghi; không xóa tải đó để tiếp tục.

## Excel, PDF và cấu hình

- Mẫu gió dầm bổ sung r1/r2, mặc định 0/1; file cũ không có hai cột vẫn hiểu là toàn dầm.
- Xuất Excel bộ soạn có header navy, border, màu nền theo tầng, cố định header, filter và cột số thực. ID giữ dạng text để không mất số 0 đầu.
- Excel Load Sets chỉ nhập qua bảng nâng cao. Luồng khóa API cũ từ chối các cột LoadSet/Reversible để không bỏ qua chúng rồi gán nhầm set.
- Bộ soạn xuất Excel được ghi rõ **chưa phải xác nhận đã gán ETABS**.
- PDF trong Tab 2 dùng Loading Case Plan hiện có và dữ liệu tải đã đọc từ ETABS, không đưa bộ soạn chưa gán vào hồ sơ chính thức. Đọc lại sau khi gán để xuất dữ liệu mới.
- PDF riêng cho lực diaphragm theo Load Sets, gợi ý tự động dầm biên, phân phối tổng lực tầng xuống nhiều dầm và biểu đồ lực theo chiều cao chưa triển khai trong đợt này.

## Nghiệm thu Windows cần thực hiện

1. Dùng bản sao model thử, kiểm tra WPF ở DPI 100/125/150/200%, các popup, chọn/dán/sắp xếp bảng.
2. Tab khóa API: sửa một khóa, giữ khóa khác; đối chiếu Fx/Fy/Mz/X/Y trong hộp thoại ETABS.
3. Bảng nâng cao: hai tầng cùng D1, hai diaphragm một tầng, hai Load Sets; kiểm tra thêm/xóa/đổi số set và Reversible theo schema thực.
4. Đối chiếu đơn vị khác kN–m; thay đổi units/model/baseline giữa các bước phải chặn ghi.
5. Tab dầm: dầm đảo I–J, tải hình thang đổi dấu, đoạn giữa dầm; giữ tải ngoài đoạn và hướng khác.
6. Kiểm tra lỗi import, hủy trước ghi, thay đổi giữa lô, backup/journal và read-back. Không dùng kết quả test giả lập như bằng chứng host đã chạy đúng.
7. Mở Excel xuất ra, kiểm tra bộ soạn và PDF tải đã đọc lại.

## Tệp liên quan

- `Views/WindLoadView.xaml(.cs)`, `ViewModels/WindWorkspaceViewModel.cs`
- `Views/WindUserSetsView.cs`
- `Modeling/Core/WindWorkspace.cs`, `WindUserTables.cs`, `WindReportWorkbook.cs`
- `Services/Modeling/EtabsWindTableHost.cs`, `WindHostInspection.cs`
- `tests/Qks.Core.Tests/WindWorkspaceTests.cs`, `WindUserTableTests.cs`
