# Kế hoạch xuất PDF mặt bằng tải trọng — Loading Case Plan

Ngày lập: 29/10/2026. Trạng thái: đã triển khai P1–P4 và kiểm tra tự động của P5; nghiệm thu WPF/Excel/ETABS trên Windows còn cần thực hiện. Xem [hướng dẫn bản đã triển khai](QKS-2D-PDF-EXPORT.md). Lưới trục chưa có dữ liệu nguồn nên chưa đưa vào PDF.

## 1. Mục tiêu và phạm vi

Xuất mặt bằng tải trọng từ dữ liệu API đã đọc trong Mô hình 2D thành PDF phục vụ kiểm tra và thuyết minh. Người dùng chọn tầng, chế độ tải, khổ giấy, margin, header/footer và kiểm tra trước khi xuất. Chức năng chỉ đọc dữ liệu, không gán hay sửa tải ETABS.

Phạm vi đầu tiên: Plan, tải Frame/Shell/Joint đang được hỗ trợ đọc; Load Pattern và Load Sets độc lập. Tải tường trên dầm giữ nhãn `Pattern - Giá trị Đơn vị`. Load Sets giữ tên set trên sàn và giá trị các Pattern thành phần trong chú giải. Xuất mặt cắt, ghép nhiều view trên một trang và tự động bố trí khung tên CAD là giai đoạn sau.

## 2. Luồng sử dụng

1. Trong Quản lý tải trọng, mở **Xuất PDF → Loading Case Plan**. Cửa sổ dùng headbar navy, đồng bộ với Display/Select.
2. Chọn phạm vi: tầng đang xem, danh sách tầng hoặc toàn bộ tầng. Chọn chế độ **Load Pattern** hoặc **Load Sets**.
3. Chọn một hoặc nhiều Pattern/Set. Có lựa chọn “từng Pattern/Set một trang” hoặc “hiển thị đồng thời trên cùng mặt bằng”; mặc định từng Pattern/Set để tránh nhầm tải.
4. Chọn Frame/Shell/Joint trong chế độ Pattern; chế độ Sets dùng gán set của Shell. Có tùy chọn hiện nhãn, màu tải, chú giải, lưới trục, tên cấu kiện và Extrude.
5. Chọn vùng xuất: toàn mặt bằng của tầng (mặc định) hoặc vùng đang xem. Lựa chọn có áp dụng ẩn/isolate phải rõ ràng; mặc định xuất đầy đủ cấu kiện của tầng, không mang màu chọn đối tượng vào báo cáo.
6. Cài trang, header/footer; xem danh sách trang và bản xem trước. Hiển thị số trang thực tế trước khi xuất.
7. Xuất một PDF nhiều trang; chọn thêm tách file theo tầng nếu cần. Ghi file tạm, chỉ thay file đích sau khi hoàn tất; có thanh tiến độ và Hủy.

Đổi tầng/Pattern để xuất hàng loạt không được làm thay đổi tầng đang xem, vùng chọn, ẩn/isolate hoặc cấu hình hiện tại của workspace.

## 3. Khổ giấy và bố cục

| Hạng mục | Yêu cầu |
|---|---|
| Khổ chuẩn | A4, A3, A2, A1, A0; khổ rộng × cao theo mm |
| Khổ tùy chỉnh | Nhập Width, Height (mm); kiểm tra số hữu hạn, dương và giới hạn engine PDF đã kiểm chứng |
| Hướng giấy | Portrait / Landscape; xem trước cập nhật đúng chiều |
| Margin | Top, Bottom, Left, Right độc lập (mm), cho phép 0; có khóa đồng nhất bốn cạnh |
| Header / Footer | Bật/tắt độc lập, chiều cao vùng riêng (mm), khoảng cách tới hình |
| Tỷ lệ | Vừa vùng in mặc định; tùy chọn tỷ lệ cố định 1:n, hiển thị tỷ lệ thực |
| Hình lớn hơn giấy | Cảnh báo ở xem trước; chọn Fit hoặc ghép nhiều trang có chồng mép, không âm thầm cắt hình |
| Chú giải | Dành vùng riêng trong trang, không che mặt bằng; có thể đặt dưới hoặc bên phải |

Tính hình chữ nhật khả dụng sau khi trừ margin, header, footer và chú giải. Nếu chiều rộng/cao còn lại không dương, khóa Xuất và chỉ rõ ô cần sửa. Đơn vị nội bộ: mm cho bố cục, m cho mô hình, PDF point = mm × 72 / 25.4. Tỷ lệ 1:n dùng kích thước mô hình thật, không phụ thuộc mức zoom trên màn hình.

## 4. Header và Footer

Ba vùng trái / giữa / phải, chỉnh nội dung, cỡ chữ, màu RGB, kiểu đậm/nghiêng và đường phân cách. Preset đề xuất:

- Header trái: tên dự án; giữa: **LOADING CASE PLAN**; phải: số bản vẽ / revision.
- Footer trái: tầng và cao độ; giữa: chế độ + Pattern/Set; phải: `Trang {Page}/{Pages}`.
- Trường thay thế: `{Project}`, `{Model}`, `{Story}`, `{Elevation}`, `{Mode}`, `{Pattern}`, `{LoadSet}`, `{Scale}`, `{Date}`, `{Page}`, `{Pages}`, `{Revision}`.
- Ngày theo giờ máy người dùng; phần xem trước thể hiện kết quả thay thế thật. Đường dẫn đầy đủ của mô hình chỉ đưa vào nếu người dùng bật tùy chọn riêng.
- Cho phép chọn logo ảnh, giữ tỷ lệ, giới hạn kích thước; xử lý ảnh lỗi trước khi xuất.
- Chữ dài: wrap trong vùng đã cấp hoặc cảnh báo cần tăng chiều cao; không chồng lên vùng bản vẽ. Font phải hỗ trợ tiếng Việt và được phép nhúng.

## 5. Nội dung tải và tính đúng đắn

- Dùng dữ liệu gán tải gốc từ lần đọc gần nhất; ghi thời điểm đọc trong thông tin kiểm tra trước khi xuất. Nếu thiếu nhóm tải hoặc có lỗi API, hiển thị cảnh báo và chỉ rõ phần thiếu; không xuất giá trị suy đoán.
- Pattern/Set, dấu tải, đơn vị, hướng, đoạn gán và cường độ biến thiên phải giữ đúng như view. Không cộng các nguồn tải, không đổi tải uniform-to-frame thành tải dầm suy diễn.
- Mặt bằng sàn: màu phân loại theo cùng khóa dữ liệu; chú giải dùng cùng màu và cấu trúc chữ đậm/nghiêng như workspace.
- Mỗi view được dựng từ toàn bộ dữ liệu của tầng theo phạm vi đã chọn. Không lấy hình bằng screenshot cửa sổ WPF; loại bỏ menu, panel, vùng chọn và thông báo UI.
- Chú giải dài phải chuyển phần tiếp sang trang chú giải có liên kết tầng/Pattern/Set; không thu chữ xuống mức không đọc được. Đánh số trang chỉ chốt sau khi dàn trang xong.
- Tải Joint: hướng mũi tên và ký hiệu lực vuông góc mặt bằng phải đúng; có chú giải ký hiệu. Thang kích thước mũi tên nhất quán trong cùng bộ trang so sánh.

## 6. Kiến trúc đề xuất

1. **PrintProfile**: khổ giấy, margin, vùng header/footer, font/RGB, tỷ lệ, bố cục chú giải; có version, validate và lưu atomically trong `%LocalAppData%/QKS/Viewer/print-profiles.json`.
2. **PrintRequest**: bản chụp bất biến của dữ liệu đã đọc và danh sách tầng/Pattern/Set, tùy chọn hiển thị, phạm vi. Đây là dữ liệu trong bộ nhớ dùng cho tác vụ xuất, không khôi phục tính năng file snapshot đã bỏ.
3. **PageLayoutEngine**: tính vùng giấy, khung vẽ, transform tỷ lệ, vùng chữ, phân trang chú giải. Không phụ thuộc WPF để kiểm tra bằng unit test.
4. **PlanDrawingScene**: danh sách primitive đường, polygon/lỗ mở, text có góc xoay, ký hiệu và load glyph. Dùng chung logic hình học với StructuralCanvas để tránh lệch vị trí/nhãn.
5. **PdfRenderer**: ưu tiên đường nét và text vector, hỗ trợ font Unicode, clip và màu trong suốt. Ảnh chỉ dùng cho logo; raster dự phòng phải công khai DPI và kiểm thử.
6. **PrintPreview**: render từ cùng PageLayout/scene với PDF, không có luồng dàn trang thứ hai. Cho phép zoom, đổi trang, hiển thị vùng margin.

Engine PDF sẽ được chọn sau bước thử nghiệm tương thích net48, Excel-DNA và giấy phép. Đánh giá PDFsharp bản hỗ trợ net48 hoặc engine tương đương; chưa thêm thư viện hay cam kết một engine khi chưa kiểm chứng Unicode, clipping và mức sử dụng bộ nhớ.

## 7. Các giai đoạn và tiêu chí hoàn tất

| Giai đoạn | Công việc | Điều kiện hoàn tất |
|---|---|---|
| P1 — Nền tảng in | Chọn engine, PrintProfile, đổi mm/point, kiểm tra khổ/margin, PDF một mặt bằng | A4/A3/custom đúng kích thước, tiếng Việt đúng, giá trị tải khớp view |
| P2 — Giao diện và xem trước | Dialog navy, chỉnh giấy/margin/tỷ lệ, chọn vùng xuất, preview | Không chồng vùng bản vẽ với margin; thay cấu hình cập nhật preview |
| P3 — Header/Footer và chú giải | Trường thay thế, font/RGB/logo, bố cục và trang tiếp chú giải | Không mất text, số trang và tỷ lệ đúng, chú giải đầy đủ |
| P4 — Xuất hàng loạt | Chọn nhiều tầng/Pattern/Set, tiến độ/Hủy, một PDF hoặc tách file | Không đổi workspace hiện tại, hủy không để lại file PDF dở |
| P5 — Nghiệm thu và đóng gói | Kiểm tra Windows Excel/ETABS và nhiều trình đọc PDF, mô hình lớn | Không thiếu cấu kiện/tải, trang không trắng, mở/in được, hồ sơ build có bằng chứng |

Ưu tiên P1–P3 trước để có xuất một tầng hoàn chỉnh; P4–P5 hoàn thiện lô bản vẽ. Thời lượng sẽ chốt theo kết quả thử engine và mô hình nghiệm thu, không đặt hạn khi chưa có bằng chứng hiệu năng.

## 8. Ma trận kiểm tra

- Giấy A4/A0/custom, dọc/ngang; margin 0/khác nhau/quá lớn; kiểm tra MediaBox và vùng in.
- Một tầng / toàn bộ tầng; Pattern đơn / nhiều Pattern / Load Sets độc lập; tầng không có tải; tên Unicode dài.
- Tải âm, bằng 0, biến thiên; frame gán một phần chiều dài; slab có lỗ mở; Joint lực/mômen và trục local.
- Chú giải ít/nhiều nhóm, nhãn xoay, nhiều đối tượng trùng vị trí; đối chiếu ID và số thành phần nguồn trước xuất.
- Tỷ lệ cố định đo lại một đoạn biết chiều dài trong PDF; kích thước chữ/đường nét khi in ở 100%.
- Font tiếng Việt, dấu `²`, mũi tên, màu RGB; thử ít nhất hai trình đọc PDF trên Windows.
- Hủy giữa lô, file đích đang mở, quyền ghi thiếu, dữ liệu API đọc dở; giữ file cũ nguyên vẹn.
- Đóng/mở lại khôi phục PrintProfile hợp lệ; cấu hình hỏng/phiên bản mới không ghi đè mất dữ liệu.

## 9. Kết quả bàn giao dự kiến

Mã nguồn, XLL build, hướng dẫn xuất PDF, PDF mẫu có đánh dấu dữ liệu minh họa, checklist nghiệm thu với mô hình ETABS thực tế, thông tin phiên bản thư viện và giấy phép. Chỉ ghi nhận “hoàn thành xuất PDF” khi chức năng đã được triển khai và kiểm tra; tài liệu này là kế hoạch cho bước tiếp theo.
