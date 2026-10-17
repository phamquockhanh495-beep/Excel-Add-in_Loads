# QKS 2D Preview 3 — Apply ETABS và nền tảng gió

Ngày 17/10/2026. Tiếp tục G5–G7 trên nền Preview 2.1. Đây là **bản thử**, chưa hoàn tất G5/G6/G7 hoặc nghiệm thu V1. Giao diện navy/Material Design, kích thước thích ứng và ẩn cột được giữ theo Preview 2.1.

## Đã triển khai

### G5: Apply tải tường, sàn và tải đường gió vào ETABS

Nút **Kiểm tra và Apply ETABS** nằm trong panel tải. Chỉ bật khi có snapshot đọc trực tiếp từ ETABS, bộ nháp còn đúng fingerprint và không có tác vụ đang chạy. Snapshot JSON offline không được ghi vào host.

1. Đọc lại toàn bộ hình học/pattern/frame distributed/area uniform, kiểm tra fingerprint với nguồn của nháp. Lỗi đọc tải không được coi là tải bằng 0. Không tự tạo pattern và không tự mở khóa model.
2. Hiện model, số dòng và các phạm vi THÊM/THAY để người dùng xem trước khi ghi. Sau hộp kiểm tra, đọc nguồn lại để phát hiện thay đổi trong lúc xem.
3. Lưu model hiện tại, tạo file backup duy nhất trong **QKS_Backups** cạnh model; kiểm tra file không rỗng và SHA-256 khớp. Không bắt đầu gán nếu backup/journal thất bại.
4. Đặt đơn vị kN-m trong phạm vi ghi và khôi phục đơn vị ban đầu khi kết thúc, kể cả khi lỗi. Kiểm tra lại khóa, kết nối, hình học đích và baseline tải trước mỗi nhóm.
5. Dùng Objects, không dùng selection/group. Với THÊM, append nháp. Với THAY, CSI xóa cả pattern trên cấu kiện, nên phải dựng lại toàn bộ tập Sau: giữ các hướng, hệ tọa độ và tải mô men ngoài phạm vi thay. Các lệnh SetLoad* đều dùng Replace=false.
6. Đọc lại **toàn bộ** distributed/uniform của cấu kiện, gồm cả tải phải giữ. So sánh phân bố tuyến tính/áp lực có dấu, loại tải, hướng, hệ tọa độ và đơn vị. Cho phép CSI gộp/tách bản ghi có phân bố vật lý tương đương, không chỉ kiểm tra có một dòng mới xuất hiện. Dung sai giá trị: 1e-6 tuyệt đối + 1e-6 tương đối; vị trí hình học: 1e-6 m.
7. Nếu có mã lỗi, read-back sai hoặc hủy giữa chừng: dừng các nhóm còn lại. Journal đánh dấu nhóm chưa chạy, đang chạy, đã xác nhận hoặc lỗi; ghi cả tập Trước/Sau. Cờ `MayHaveChanges` được đặt trước lệnh có thể thay đổi host. Không tự rollback hoặc thử lại Add. Nút Apply bị khóa sau lần có thể đã ghi; phải đọc lại model. Có nút **Lưu báo cáo Apply**.

Backup/journal không tạo giao dịch nguyên tử cho COM. ETABS không cung cấp revision/transaction cho luồng này; không sửa model bằng cửa sổ khác khi đọc/ghi. Nếu lỗi giữa Delete và các Set, model có thể chỉ còn một phần tải. Mở backup để kiểm tra/phục hồi bằng ETABS; không bấm chạy lại trên nguồn cũ. Sau Apply thành công, kiểm tra và lưu model bằng ETABS.

### G6: hai đích gán gió

Yêu cầu mới: [TCVN 2737:2023, chu kỳ T, dầm biên và User Loads theo diaphragm](WIND-TCVN2737-2023.md).

**Đã có mã thử:**

- Dải áp lực có dấu, tuyến tính theo Z; không ngoại suy ngoài dải.
- Gió dầm biên do người dùng chọn: q = p × chiều cao đón gió, kN/m. Chiều dài và vị trí dùng hình học 3D thật; Global X/Y. Tạo dòng nháp riêng `WindLine`, preview Plan/Elevation, Undo/Redo, Before/After và dùng luồng Apply G5. Gió và tường cùng hướng/pattern được kiểm tra trùng trên cùng phạm vi vật lý.
- Dầm biên cần pattern loại Wind riêng, không sinh tải gió tự động. Không cho dùng chung pattern đã có User Loads khác 0 với tải gió dầm biên; nhánh User Loads cũng dừng nếu pattern có tải trực tiếp lên cấu kiện. Hai phương pháp không tự cộng vào nhau.
- Quy đổi áp lực sang lực tầng: tích phân p(z) trên dải tầng × bề rộng đón gió, trả kN. Không đưa trực tiếp áp lực hoặc tải đường vào API lực tầng.
- Đọc/sửa **User Loads của pattern Wind đã có**: đọc đúng chuỗi khóa diaphragm từ `AutoWind.GetUserLoad`, giữ nguyên khóa; sửa Fx/Fy (kN), Mz (kN·m), X/Y (m) trong bảng **User Loads gió**. Phải có Auto Lateral Load = `User Loads`; nếu host trả code khác, dừng và báo chuỗi thực tế.
- Apply User Loads riêng: kiểm tra nguồn và toàn bộ baseline của pattern, backup SHA-256, journal, khôi phục đơn vị; gọi typed `AutoWind.SetUserLoad` và đọc lại toàn bảng sau từng hàng để kiểm tra cả khóa không sửa. Không tự chuyển pattern tiêu chuẩn khác, không tự tạo diaphragm hoặc đoán khóa theo tầng. Có lưu/mở `.qkswind.json`, phát hiện nháp chưa lưu và khóa Apply sau lần có thể đã ghi.
- Bảng User Loads có thể soạn offline từ nháp đã lưu, nhưng không Apply khi offline. File nháp kiểm tra schema, identity, fingerprint và đầy đủ các thành phần lực; thiếu Fx không được mặc định thành 0.

**Chưa có:** engine tự tính hệ số/gió theo TCVN 2737:2023; tự chọn mode/chu kỳ từ kết quả phân tích; đọc tâm cứng theo tầng; tự sinh khóa diaphragm/tầng mới; tự nhận diện dầm biên. T1 hiện chỉ lưu tham chiếu, **không tham gia công thức Gf**. Không có hệ số hoặc tải mẫu nào tự được điền vào các ô tính/gán. Nhánh User Loads cần thử trên host để xác minh code name, định dạng khóa và hành vi setter.

### SAFE 18–22, ưu tiên 22.5

Chưa có SDK/host SAFE trong cloud, nên chưa có adapter live SAFE được xác minh. Không tái dùng DLL ETABS để tuyên bố hỗ trợ SAFE. `ApplyCapabilities` trả unsupported có lý do cho nguồn SAFE; `scripts/probe-csi.ps1` đã bổ sung chữ ký Save, units, lock, distributed/uniform writer/delete, User Loads, diaphragm, modal và database tables để đối chiếu SDK từng phiên bản.

## G7: kiểm tra và gói thử

- **194/194 core/VM tests đạt**; gồm fault injection cho backup, journal, unit setup/restore, đổi nguồn/hình học, mã lỗi, read-back sai, mất tải không liên quan và hủy giữa chừng. Không có test nào chạy COM/WPF/Excel thật trong cloud.
- Packed XLL net48/Excel desktop Windows 64-bit: **0 warning, 0 error**.
- Benchmark nhân tạo core/VM tại 1k/10k/50k đối tượng; 1 warm-up, 3 mẫu lấy median. Kèm JSON số đo trong gói. Không đo WPF FPS, COM hoặc peak memory; không suy ra trải nghiệm model thật từ số này.
- Tái sử dụng fingerprint đã tính khi tạo workspace, cache tra cứu đối tượng cho validation nháp; tránh serialize/lập dictionary toàn mô hình lặp lại cho thao tác soạn tải.
- Hai probe PowerShell được kiểm tra cú pháp; chưa chạy với host Windows. [Bảng nghiệm thu Windows](PREVIEW-3-HOST-TESTS.md) phân biệt các mục chưa chạy và kết quả core.

| Đối tượng nguồn | Snapshot + Plan (median, ms) | Chuyển Elevation (median, ms) |
|---:|---:|---:|
| 1.000 | 18,24 | 0,83 |
| 10.000 | 148,78 | 2,28 |
| 50.000 | 369,99 | 8,13 |

Số đo trên .NET 8/Linux, có 10% cột bị ẩn theo yêu cầu. Chỉ phản ánh pipeline core/VM với dữ liệu nhân tạo; XLL chạy .NET Framework 4.8 trên Windows cần đo riêng.

## Cài bản thử

Tải [QKS-2D-Preview-3](../downloads/QKS-2D-Preview-3/), giải nén vào thư mục cố định. Đóng toàn bộ Excel, bỏ đăng ký XLL cũ trong Excel Options → Add-ins → Manage Excel Add-ins → Go, rồi Browse đến **Excel/QksExcelAddIn-AddIn64-packed.xll**. Không nạp đồng thời hai bản. Thanh tiêu đề viewer phải ghi **PREVIEW 3**.

Để thử không ghi: mở snapshot mẫu `Samples/viewer-readonly.json` và nháp mẫu. Với thử ghi: dùng bản sao model ETABS đã lưu, chọn API từ ribbon, đọc mô hình mới và tự tạo nháp trên đối tượng/pattern thực. Nháp mẫu nhân tạo không thể gán vào model thật vì khác identity/fingerprint. Chưa hỗ trợ Excel 32-bit hoặc macOS.

Thử gió offline: với snapshot `viewer-readonly.json`, mở `viewer-wind-line.qksdraft.json` để xem WindLine. Riêng User Loads, mở snapshot `viewer-wind-source.json`, vào tab Gió → User Loads/diaphragm → Mở gió, chọn `viewer-user-wind.qkswind.json`. Hai khóa `SYNTHETIC-KEY-*` là giả lập để thử bảng/lưu nháp, không phải định dạng key ETABS đã được xác minh.

Build lại trên Windows có .NET SDK 8 và SDK ETABS:

```powershell
.\scripts\build-viewer.ps1 -EtabsApiPath 'C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll' -RunBenchmark
```

Script chạy test core/VM, tùy chọn benchmark và build packed XLL. Không chạy ETABS/Excel hay tự nghiệm thu host. XLL xuất vào `artifacts/bin/QksExcelAddIn/Release/net48/publish/`.

Không cần chạy deploy AutoCAD để mở viewer ETABS; không thay đổi plugin AutoCAD trong đợt này.

## Điều kiện tiếp tục hoàn tất

1. Tài liệu công thức/hệ số TCVN 2737:2023 đang dùng và một bảng tính đối chứng có T theo X/Y, tham số công trình, áp lực/lực tầng, đơn vị và cách tổ hợp. Cần xác định rõ tải tĩnh/động/tổng trước khi tạo pattern, tránh cộng hai lần.
2. Model ETABS thử có User Loads qua nhiều tầng và báo cáo read-only từ `probe-etabs-wind.ps1`, xác minh khóa diaphragm, vị trí đặt lực, read-back và nguồn tâm cứng.
3. SDK metadata SAFE 22.5 trước, sau đó 18–22 và host/model đối chứng cho từng capability. Không dùng build thành công thay cho kiểm chứng trên các phiên bản này.
4. Nghiệm thu Windows: read/write/read-back trên model thử, restore backup, DPI/taskbar và benchmark host. Chỉ sau đó mới đánh dấu G5–G7 hoàn tất.
