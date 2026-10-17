# QKS 2D — Preview 1 (17/10/2026)

Đã có mã WPF và XLL build từ nguồn cho viewer **chỉ đọc** Plan/Elevation ETABS. Đây là prototype đầu của G1–G3, chưa phải bản hoàn tất kế hoạch 50–85 ngày. G0 chưa qua điều kiện nghiệm thu đọc trên cả ETABS/SAFE; môi trường cloud hiện không chạy được Excel/CSI trên Windows.

## 1. Những thay đổi có thể thử

| Phần | Kết quả trong preview |
|---|---|
| Ribbon Excel | Thêm **Extension QKS → Mô Hình 2D**; dùng kết nối ETABS đã chọn ở **Kết Nối API** |
| Layout | Headbar `#0B1F3A`, button `#102A4C`, hover `#163D66`, pressed `#08172B`, bo góc 3 DIP; nền sáng, panel 4 DIP |
| Reader ETABS | Đọc frame/area, cột/vách/brace, opening, story, label, property, góc local frame; phân loại theo DesignOrientation API |
| Snapshot | Dữ liệu bất biến, Unique ID riêng `Frame:<name>` / `Area:<name>`, nguồn/phiên bản/identity/thời điểm/đơn vị nguồn; lưu/mở JSON offline |
| Plan | Mặt bằng XY theo cao độ story; dải xem ±d; cấu kiện đi xuyên dải có ký hiệu cut |
| Elevation | Nhập gốc X/Y, góc phương U và dải xem ±d; trục V luôn +Z; đảo hướng nhìn vẫn giữ chiều đứng |
| Điều hướng | Zoom tại con trỏ, pan chuột giữa/phải, vừa khung; chọn cạnh trên hình hoặc hàng trong bảng; Ctrl + click chuyển các đối tượng chồng cạnh |
| Đồng bộ | Chuyển Plan/Elevation giữ ID chọn; bảng/inspector dùng dữ liệu snapshot, không gọi API khi pan/zoom/lọc |
| Màu | Beam xanh, column đỏ, slab teal, wall tím, brace cam đất, Other xám; chọn cam; màu property xác định bằng SHA-256, không phụ thuộc thứ tự đọc |
| Profile | Chỉnh màu property `#RRGGBB`, lưu tại `%LOCALAPPDATA%\QKS\Viewer\property-colors.json`; áp dụng khi bật **Màu theo property** |
| Tải hiện có | Bảng frame distributed và area uniform; lọc theo đối tượng đang chọn; giữ pattern/CSys/Dir API, dấu, đoạn r1/r2 và hai giá trị |
| Nhận biết lỗi | Lỗi hình học/đơn vị/identity làm dừng lần đọc; lỗi metadata/tải tùy chọn ghi vào **Chẩn đoán**, không tạo giá trị 0 thay dữ liệu thiếu |

Độ dài/độ cao chuẩn hóa về **m**, lực về **kN**; tải đường kN/m, tải diện kN/m², mômen phân bố kN·m/m. r1/r2 tải frame tham chiếu chiều dài 3D thật, không phải chiều dài chiếu. Reader không gọi SetPresentUnits, File.Save, SetLoad hoặc API ghi. Các cửa sổ gán tải cũ vẫn là luồng riêng.

## 2. Cài vào Excel và thử nhanh

Lấy gói tại [folder tải Preview 1](../downloads/QKS-2D-Preview-1/). File ZIP được lưu trực tiếp trên GitHub; mở file rồi chọn **Download raw file**, hoặc dùng link raw trong README folder đó.

1. Đóng tất cả Excel. Giải nén vào thư mục cố định, ví dụ `D:\QKS\Preview-1`. Nếu Windows hiển thị tùy chọn **Properties → Unblock** cho file ZIP vừa tải, bỏ chặn trước khi giải nén.
2. Excel desktop **64-bit** → **File → Options → Add-ins → Manage: Excel Add-ins → Go**. Bỏ chọn XLL QKS cũ nếu đang được đăng ký. **Browse** tới `Excel\QksExcelAddIn-AddIn64-packed.xll` và chọn file này. Cài hai XLL QKS cùng lúc có thể gây nhầm phiên bản.
3. Chọn **Extension QKS → Mô Hình 2D → Mở snapshot** và mở `viewer-readonly.json` trong gói để thử giao diện trước. Header phải ghi **OFFLINE** và nguồn **SYNTHETIC TEST FIXTURE**. Đây là dữ liệu nhân tạo, không lấy từ mô hình của anh.
4. Thử Plan tầng Roof, Elevation gốc X=0/Y=0/góc=0/d=0.15 m, đảo hướng, zoom/pan, chọn B1, xem bảng tải. Giá trị fixture: B1/SDL=8 kN/m; B2/WX=-2→-3 kN/m; S1/LL=3 kN/m². Giữ r1=0/r2=1.
5. Để đọc thật: mở/lưu mô hình trong ETABS; **Kết Nối API** chọn đúng phiên ETABS; vào **Mô Hình 2D → Đọc mô hình**. Trong lúc đọc, không sửa mô hình hoặc đổi đơn vị ở host. Đối chiếu số đối tượng, label và vài tải với ETABS.
6. Thử lưu snapshot JSON, đóng/mở lại bằng **Mở snapshot**. Snapshot không tự refresh theo mô hình đang mở; dùng **Đọc mô hình** khi cần lấy dữ liệu mới.

Viewer này không cần cài plugin AutoCAD hoặc chạy `deploy-local.ps1`. AutoCAD tiếp tục phục vụ luồng trao đổi CAD riêng trước đó. Máy build cloud không kiểm chứng chính sách XLL của Office trên máy người dùng; nếu Office chặn file, ghi lại chính xác thông báo để xử lý theo cấu hình Excel thực tế.

## 3. Các giới hạn cần biết khi đánh giá

- Chưa có draft editor, Undo/Redo tải, Before/After hoặc Apply trong viewer. Ba tab Tường/Gió/Sàn mới chỉ chỉ dẫn mốc phát triển tiếp theo, không có nút ghi tải.
- Chưa có adapter SAFE chạy thật. Phạm vi mục tiêu mới là **SAFE v18–v22**, ưu tiên **v22.5** người dùng đang dùng; toàn bộ phiên bản này hiện **chưa kiểm chứng**. Không dùng DLL ETABS làm bằng chứng tương thích SAFE.
- Elevation là mặt phẳng do QKS dựng từ tọa độ mô hình, chưa nhập named elevation/grid từ ETABS, chưa có thao tác chọn hai điểm trên Plan. Chưa hỗ trợ mặt đứng cong/trải phẳng.
- Vẽ đường trục frame và biên area; chưa hiển thị bề rộng tiết diện, material/thickness, local triad, load arrows/heatmap, hover, box selection hoặc giải va chạm nhãn. Nhãn giới hạn 500 trong vùng màn hình; bảng vẫn giữ đối tượng.
- Hình học cut hiện dành cho shell phẳng; shell cong/không phẳng và trường hợp tiếp tuyến/đỉnh trùng cần bộ đối chứng Windows. Nét đứt biểu thị bị cắt bởi biên dải xem, chưa có chế độ projection xa riêng.
- Khi snapshot có opening, preview vẽ area bằng viền (kể cả khi lọc ẩn opening) để tránh tô kín lỗ khi chưa có quan hệ slab–opening chính xác. Chưa dùng hình chiếu này để tính diện tích hay phân tải.
- Chưa đọc joint force/moment, frame point load, temperature, pressure khác uniform, tải sinh tự động; chưa tổng hợp load combination. Không suy diễn bảng trống thành mô hình không có tải. Xem chẩn đoán và phạm vi reader.
- Mã màu property ổn định nhưng palette có thể trùng màu giữa hai property; chỉnh override khi cần. Profile hiện dùng chung máy/người dùng theo kind + property, chưa tách model hay đồng bộ nhiều cửa sổ.
- API COM chạy trên WPF STA và nhường UI mỗi 25 đối tượng; nút Hủy có tác dụng giữa các lời gọi API, không ngắt được một COM call đang treo. Chưa benchmark model lớn/DPI. Các phép vẽ/hit-test hiện duyệt tuyến tính.
- Identity/path/units được kiểm tra trong lúc đọc. API không cung cấp revision dùng chung để phát hiện mọi thay đổi hình học cùng file; không coi lần đọc là giao dịch snapshot nguyên tử ở phía CSI.
- JSON snapshot tối đa 32 MiB. Chưa có cơ chế chia phần/cache đĩa cho model lớn.

## 4. Kết quả kiểm tra

- **73/73 core/VM tests đạt**: 40 hồi quy cũ + 33 test viewer; gồm SI/imperial, phân biệt lực/mômen phân bố, chiều dài 3D, clip band, cut cột/sàn, polygon lõm giữ khe hở, reverse Elevation, picking, identity/selection/filter, JSON round-trip, màu ổn định và opening context.
- Build Excel **net48/x64 packed XLL: 0 warning, 0 error**, dùng SDK .NET 8.0.408 và DLL API ETABS trong kho, NuGet locked mode.
- Hai script PowerShell mới đã qua parser; **chưa chạy chức năng build/probe trên Windows**.
- Chưa chạy Excel/WPF/ETABS/SAFE thật, chưa đo FPS/RAM hoặc kiểm chứng số tải qua COM. Không coi các kiểm tra compile/core là nghiệm thu host.
- Không thay source plugin AutoCAD trong đợt này; không chạy lại matrix 9 AutoCAD build vì không thay phần được plugin biên dịch.

Log đầy đủ kèm trong gói tải. Manifest ghi commit nguồn và hash từng file; `SHA256SUMS.txt` kiểm tra ZIP.

## 5. Tự build riêng viewer trên Windows

Cần .NET SDK 8, targeting support .NET Framework 4.8 và DLL API của bản ETABS đã cài. Không cần SDK AutoCAD cho lệnh này:

```powershell
.\scripts\build-viewer.ps1 -EtabsApiPath 'C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll'
```

Đầu ra: `artifacts\bin\QksExcelAddIn\Release\net48\publish\QksExcelAddIn-AddIn64-packed.xll`.

## 6. SAFE 18–22 và trình tự tiếp theo

| Mốc | Trạng thái / công việc còn lại |
|---|---|
| G0 | Đã khảo sát chữ ký API ETABS trong DLL và chốt SAFE 18–22/22.5. Còn SDK/host SAFE và model đối chứng; chưa nghiệm thu G0 |
| G1 | Có core snapshot/units/ID/projection và tests. Còn node độc lập, grids, property vật lý và kiểm tra reader bằng model thật |
| G2 | Có layout WPF/MVVM và Plan preview. Còn host smoke test, DPI/label placement, hiệu năng/model lớn, style manager đầy đủ |
| G3 | Có Elevation theo origin/angle/depth và giữ selection. Còn grid/hai điểm, projected/cut modes đầy đủ, đối chứng hình học/nhãn trên ETABS |
| G4 | Tiếp theo sau nghiệm thu reader/viewer: draft tường/sàn, editor, Undo/Redo, kiểm tra trùng/đoạn/đơn vị và preview |
| G5 | Adapter đọc/ghi SAFE theo capability, Apply có backup/preflight/return-code/read-back/partial-failure; chỉ mở từng capability sau test |
| G6 | Gió theo phương/cao độ và tải nhập đã xác định; tiêu chuẩn tính gió vẫn cần chốt nghiệp vụ riêng |
| G7 | Host regression, benchmark 1k/10k/50k, DPI, đóng gói và hướng dẫn bàn giao |

Trước mắt cần nghiệm thu preview trên **ETABS đang sử dụng và SAFE 22.5**, rồi mới mở rộng kiểm tra các bản SAFE 18/19/20/21/22 khác. Mỗi bản cần metadata + COM attach + đọc model đối chứng; cùng chữ ký interface không đủ kết luận tương thích.

Để lấy báo cáo API SAFE trên máy có cài SAFE, xác định DLL API thật trong thư mục cài đặt (tên có thể khác giữa phiên bản) rồi chạy:

```powershell
.\scripts\probe-csi.ps1 -Product SAFE -ApiPath '<đường dẫn DLL API trong bộ cài SAFE 22.5>' -OutputPath .\SAFE-22.5-api-report.json
```

Script chỉ lấy metadata assembly/phiên bản/chữ ký phương thức và ghi JSON, không khởi động CSI hoặc sửa model. Báo cáo này cùng kết quả smoke test Excel là đầu vào để triển khai adapter tiếp; chưa chứng minh bản SAFE nào đã được hỗ trợ.

Dự toán tổng trước đó **50–85 ngày làm việc** vẫn là dự toán cho V1 có cả SAFE và Apply, không được thay bằng thời gian tạo prototype này. Sau khi có kết quả Windows/API SAFE sẽ cập nhật khối lượng còn lại.
