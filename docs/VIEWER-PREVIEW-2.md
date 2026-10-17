# QKS 2D Preview 2 — Nháp tải tường/sàn và Elevation hai điểm

Ngày 17/10/2026. Đợt này tiếp tục phần G3 và triển khai prototype G4: soạn nháp, đối chiếu và lưu nháp trong viewer Material Design. **Chưa có Apply từ viewer vào ETABS/SAFE**. Các cửa sổ tải cũ vẫn là luồng riêng.

## 1. Đã triển khai

| Chức năng | Hành vi |
|---|---|
| Nháp tường | Chọn Beam, nhập s1/s2 theo m từ đầu I dọc chiều dài 3D; nhập q1/q2 kN/m. Bỏ trống q2 có nghĩa q2=q1 như nhãn editor |
| Nháp sàn | Chọn Slab nguồn, nhập một giá trị uniform kN/m²; không đổi thành tải đường khi xem Elevation |
| Pattern | Reader lấy LoadPatterns.GetNameList. Editor chỉ dùng pattern có trong snapshot, không tạo pattern ở host |
| Hướng | Global-X/Y/Z và Gravity; giá trị giữ dấu và số 0 do người dùng nhập. Gravity dương theo -Z, Global-Z dương theo +Z |
| Thêm tải | Giữ các bản ghi tải nguồn và thêm dòng nháp như một thành phần tải riêng |
| Thay cùng phạm vi | Trong preview, thay toàn bộ bản ghi cùng object/pattern/hướng/hệ Global/loại tải được hỗ trợ bằng các đoạn nháp |
| Sửa / xóa | Click dòng trong bảng nháp để nạp lại editor; sửa giữ nguyên ID dòng. Dòng mới tạo ID khác; xóa không sửa snapshot nguồn |
| Undo / Redo | Hoàn tác/phục hồi thao tác nháp, giữ tối đa 100 trạng thái; thao tác thất bại không đổi bộ nháp |
| Trước / Sau | Bảng có mốc, nguồn bản ghi, object, pattern, loại, CSys, Dir API, đoạn r1/r2, giá trị và đơn vị |
| Preview hình | Vẽ đoạn frame nháp đúng vị trí 3D đã khai báo, chiếu/cắt theo view; area vẽ biên nháp. Nét tím và label giá trị/hướng, tối đa 200 label có thể bật/tắt |
| Hai điểm Elevation | Chọn hai điểm trên Plan, snap đỉnh gần nhất trong 8 DIP, tự điền origin/góc và chuyển sang Elevation. Esc hoặc click lại nút để hủy |
| Lưu / mở | Snapshot JSON và bộ nháp `.qksdraft.json` là hai file riêng. Bộ nháp có schema, identity và fingerprint nguồn |
| Giữ dữ liệu | Mất kết nối/đổi snapshot không xóa nháp đang làm. Bộ nháp sai nguồn chuyển sang trạng thái tách, tắt editor/overlay/so sánh nhưng vẫn lưu được |
| Material Design | Giữ bộ style Preview 1.1: button ripple/elevation, navy QKS, góc bo 3 DIP; các control mới dùng cùng resource |

### Ngữ nghĩa thêm và thay

**Thêm tải** thể hiện phép cộng tải: nếu model đã có một dòng 8 kN/m và anh thêm một dòng 2 kN/m cùng phạm vi, bảng Sau giữ cả hai dòng. Bảng này chưa tổng hợp hai dòng thành biểu đồ kết quả 10 kN/m.

**Thay cùng phạm vi** thay cả nhóm tải đã chọn, không chỉ phần giao với một đoạn nháp. Ví dụ dầm có SDL/Gravity trên [0,1], nháp thay chỉ trên [1/6,5/6]: bảng Sau chỉ còn đoạn nháp cho nhóm đó; phần ngoài đoạn không tự giữ lại. Các dòng khác pattern/hướng, local/named CSys và mômen phân bố vẫn được giữ nguyên.

Các đoạn nháp cùng object/pattern/hướng không được chồng nhau; có thể nối tại một đầu mút. Không trộn thao tác thêm và thay trong cùng nhóm. Sàn chỉ có một dòng uniform cho mỗi nhóm. Khi muốn sửa một dòng, chọn đúng dòng đó; dùng **Dòng mới** để rời chế độ sửa.

### Ràng buộc nguồn và file

Fingerprint SHA-256 gồm source/version/identity, geometry/property/local-axis metadata, tầng, pattern và tải đã đọc. Thời điểm chụp và thứ tự danh sách đọc không làm đổi fingerprint; đổi hình học hoặc tải nền sẽ làm đổi fingerprint. Chưa có cơ chế tự rebase nháp lên model đã thay đổi.

Identity hiện có cả PID phiên ETABS. Sau khi khởi động lại ETABS, nháp cũ có thể không khớp phiên live mới; có thể mở **snapshot đã lưu cùng nháp** để kiểm tra offline. Đây chưa phải cơ chế xác định model bền vững giữa mọi phiên CSI.

Khi snapshot mới khớp hoàn toàn, nháp tự gắn lại. Khi không khớp, hãy lưu nháp cũ rồi dùng **Bộ mới** cho snapshot hiện tại. Mở file nháp sai nguồn hoặc file không hợp lệ không ghi đè bộ nháp đang làm. Đóng cửa sổ/mở bộ nháp khác/tạo bộ mới sẽ hỏi lưu khi có thay đổi chưa lưu.

Các trường đầu vào JSON của dòng nháp là bắt buộc, gồm Value1 và Value2; thiếu trường không được tự diễn giải là số 0. Giá trị NaN/Infinity, sai đối tượng, opening nhận tải sàn, cột nhận tải tường, đoạn ngoài [0,1] và pattern không có đều bị chặn.

Snapshot schema 1 được bổ sung trường LoadPatterns theo cách tương thích với file cũ: nếu file cũ không có danh sách này, chỉ suy ra các pattern đã xuất hiện trong bản ghi tải. Muốn chọn pattern chưa từng được gán, đọc lại model bằng Preview 2.

## 2. Tải và cài Excel

Lấy [gói Preview 2 trên GitHub](../downloads/QKS-2D-Preview-2/). Gói có XLL, snapshot offline, bộ nháp mẫu, báo cáo, script probe SAFE và log build/test.

1. Đóng toàn bộ Excel, giải nén vào thư mục cố định mới. Nếu Windows có **Properties → Unblock** cho ZIP vừa tải, bỏ chặn trước khi giải nén.
2. Excel → **File → Options → Add-ins → Manage Excel Add-ins → Go**. Bỏ chọn XLL QKS trước đó, Browse đến `Excel\QksExcelAddIn-AddIn64-packed.xll` của gói Preview 2.
3. Mở **Extension QKS → Mô Hình 2D**. Header phải ghi **PREVIEW 2 · NHÁP, CHƯA GHI MODEL**.

Không cần cài AutoCAD hoặc chạy deploy-local.ps1 để dùng viewer này. Muốn tự build, dùng `scripts/build-viewer.ps1` với EtabsApiPath như hướng dẫn Preview 1.

## 3. Trình tự thử với dữ liệu mẫu

1. **Mở snapshot** → `viewer-readonly.json`; header nguồn phải ghi OFFLINE / SYNTHETIC TEST FIXTURE.
2. **Mở nháp** → `viewer-draft.qksdraft.json`. Có hai dòng: B1/SDL tải tường 8→10 kN/m trên s1=1 m, s2=5 m; S1/LL tải sàn 4 kN/m². Cả hai dùng Gravity và thao tác thay cùng phạm vi.
3. Plan tầng L1: xem đoạn nháp B1; Plan Roof: xem biên nháp S1. Mở **Nháp / So sánh → Dòng nháp** và chọn dòng B1 để xem/sửa giá trị bằng editor bên phải. Khi đối tượng ngoài vùng nhìn, editor ghi rõ trạng thái này.
4. Trong **Trước / Sau**, B1/SDL trước là 8 kN/m toàn dầm, sau là 8→10 kN/m trên đoạn nháp; S1/LL trước 3, sau 4 kN/m². B2/WX vẫn -2→-3 kN/m. Dùng **Bỏ chọn** để xem đầy đủ các đối tượng.
5. Sửa q1 rồi **Cập nhật dòng nháp**; thử Undo/Redo. Khi con trỏ nằm trong TextBox, Ctrl+Z tiếp tục dùng Undo của ô nhập; ngoài ô nhập, Ctrl+Z/Ctrl+Y dùng lịch sử bộ nháp.
6. **Chọn 2 điểm / Hủy**, click theo trục X từ (0,0) đến (6,0), hoặc snap hai đỉnh trên cạnh dầm. Kiểm tra Elevation, đảo hướng rồi quay lại Plan: số dòng nháp và ID không đổi.
7. Thử một đoạn chồng nhau bằng **Dòng mới**; thao tác phải báo lỗi và không thêm dòng. Thử giá trị `NaN` hoặc s2 lớn hơn chiều dài dầm: cũng phải bị chặn.
8. Lưu snapshot và nháp ra hai file, đóng/mở lại, mở đúng snapshot trước rồi mở nháp. Thử mở nháp với snapshot khác: báo sai nguồn, bộ đang làm được giữ lại.

Dữ liệu mẫu là nhân tạo, không phải kết quả đọc ETABS/SAFE. Các nút nháp không gọi API ghi tải.

## 4. Kết quả kiểm tra

- **117/117 core/VM tests đạt**: 73 kiểm thử trước + 44 kiểm thử nháp/Elevation mới.
- Kiểm tra chiều dài 3D, SI/imperial, dấu/số 0, đoạn chồng, thêm/thay nhóm, không xóa mômen ngoài phạm vi, Undo/Redo/dirty state, fingerprint/JSON/import nguyên tử và snapshot đổi nguồn.
- Kiểm tra nháp vẫn giữ khi mất kết nối; gắn lại snapshot đồng nhất; không nhân dòng khi đổi view/sửa; overlay chiếu đúng đoạn I→J; hai điểm không hợp lệ bị chặn; bộ nháp mẫu khớp snapshot mẫu.
- Build packed XLL net48/Excel 64-bit: **0 warning / 0 error**, .NET SDK 8.0.408, NuGet locked mode.
- Không sửa phần source được plugin CAD biên dịch; không chạy lại 9 target AutoCAD trong đợt này.
- Chưa chạy Excel/WPF/ETABS/SAFE COM trên Windows, chưa kiểm chứng render/snap/dialog/DPI hoặc benchmark model lớn. Bộ kiểm thử core/VM không thay cho các kiểm tra đó.

## 5. Phần còn lại và giai đoạn tiếp theo

| Giai đoạn | Trạng thái sau đợt này | Cần tiếp tục |
|---|---|---|
| G0 | Có khảo sát DLL ETABS, mục tiêu SAFE 18–22/22.5 | Metadata/API SAFE cài thực và model đối chứng; chưa qua nghiệm thu hai host |
| G1 | Snapshot/units/ID/pattern/fingerprint có code và tests | Reader đối chứng trên Windows, node độc lập, grids, thuộc tính vật lý đầy đủ |
| G2 | WPF Material Design, Plan, chọn/lọc/màu | DPI, va chạm nhãn, multi-select/box select, model lớn |
| G3 | Elevation origin/góc/dải xem, chọn hai điểm, giữ nháp | Named grid, các mode in-plane/cut/projected đầy đủ, đối chiếu mặt đứng thật |
| G4 | Prototype nháp tường/sàn, thêm/sửa/xóa, Before/After, Undo/Redo, lưu/mở | Nghiệm thu UI/nghiệp vụ bằng model thật, thao tác hàng loạt, rebase/conflict theo nguồn |
| G5 | Chưa mở Apply trong viewer | Writer theo capability, đọc lại nguồn để preflight, backup, return codes, read-back và báo partial failure; adapter SAFE sau khi xác minh SDK |
| G6 | Tab gió mô tả bước tiếp | Tải theo phương/cao độ, quy đổi đã chốt và bộ số đối chứng; chưa có engine tiêu chuẩn gió |
| G7 | Có gói thử và log | Host regression, DPI/hiệu năng và bàn giao |

Preview 2 chưa hoàn tất toàn bộ G4 hoặc kế hoạch V1. Giới hạn hiện tại: một đối tượng mỗi lần soạn; tường lên Beam và sàn uniform lên Slab; Global X/Y/Z/Gravity; tối đa 10.000 dòng nháp, 100 trạng thái Undo, 200 label nháp. Không tự tính q từ vật liệu/chiều cao tường, không tính diện tích/phân tải sàn, không có biểu đồ tổng lực hoặc vector local/projected.

So sánh Trước/Sau chỉ bao gồm dữ liệu reader đã đọc (frame distributed và area uniform); lỗi đọc tùy chọn phải xem ở tab Chẩn đoán. Không suy bảng trống là không có tải. Area có opening vẫn dùng biên thay vì tô kín khi chưa có liên kết slab–opening chính xác.

Bước kiểm chứng tiếp theo là chạy các ca ở mục 3 trên Windows, rồi đối chiếu reader bằng ETABS thực và lấy báo cáo SDK SAFE 22.5 qua `scripts/probe-csi.ps1`. Phạm vi SAFE 18–22 là mục tiêu, chưa phải các phiên bản đã được hỗ trợ. Trước khi mở Apply, writer phải đọc lại nguồn và xử lý xung đột; fingerprint của snapshot offline không tự chứng minh model live chưa thay đổi.
