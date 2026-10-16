# Báo cáo thay đổi — Extension QKS

Cơ sở sửa: commit 6dc3cc7. Các thay đổi hiện nằm trong working tree, chưa commit/push GitHub. AutoCAD 2027 đã loại khỏi phạm vi; các cấu hình build là AutoCAD 2018–2026, Windows x64. Excel thử nghiệm là desktop 64-bit, .NET Framework 4.8.

## Đã thay đổi

1. **Build và phụ thuộc**: hai project có danh sách Compile riêng, output/intermediate riêng; plugin .NET 8 không gom XAML của Excel. Sửa đường dẫn solution, bỏ phụ thuộc AssemblyInfo không tồn tại và bỏ PostBuild nuốt lỗi. Tham số hóa AcadSdkDir, AcadVersion, EtabsApiPath. Thêm global.json, lockfile theo target, .gitignore; bỏ obj đã commit. Chỉ giữ một DNA nguồn và đóng gói Newtonsoft.Json; sửa launch profile theo tên XLL mới, assembly resolver chỉ tái sử dụng đúng full identity.
2. **Runtime CAD**: build plugin riêng từng năm. 2018 dùng net46; 2019–2020 dùng net47; 2021–2024 dùng net48; 2025–2026 dùng net8.0-windows. Không đóng gói SDK Autodesk. SDK NuGet chính thức dùng cho kiểm tra compile; không coi đó là kiểm thử AutoCAD đang chạy.
3. **Giao thức CAD**: Shared/Protocol.cs và Services/CadExchangeSession.cs; Plugin/CadRuntime.cs thêm lệnh QKS_EXECUTE. Yêu cầu/phản hồi có protocol version, request ID, document/model/export context; payload được chụp vào file riêng từng request, output ghi nguyên tử. Phản hồi sai ID/context/canceled/failed bị từ chối. Lệnh chạy trên command context của AutoCAD.
4. **Loại transport chưa hoàn chỉnh**: bỏ client Named Pipes không có server. Tải tường/sàn dùng COM để kích hoạt command và file JSON có mã yêu cầu. Không tự retry thao tác có thể ghi dữ liệu; một yêu cầu CAD tại một thời điểm, timeout khóa gửi tiếp trong phiên Excel cho tới khi khởi động lại.
5. **Chọn đúng bản vẽ/model**: không tự chuyển sang document đầu tiên hoặc chỉ khớp basename. Dùng COM document đã chọn và kiểm tra full path; model ETABS đổi phải kết nối lại. Cache bị xóa khi đổi model, refresh và bắt đầu/hoàn tất thao tác ghi; không mở rộng tầng không tìm thấy thành toàn mô hình.
6. **Phạm vi bản xuất**: các entity mới gắn QKS_SCOPE theo export ID. Quét/cập nhật/chọn theo tiết diện được giới hạn scope của cửa sổ đã xuất. Đổi tầng/pattern/hướng hoặc model thì phải xuất lại. Phát hiện text trùng metadata do COPY; không tự đoán bản nào cần sửa.
7. **Phân đoạn tải**: text và polyline tải tường có metadata khoảng cách phân đoạn. Live update ghép đúng miền; nếu phân đoạn thay đổi thì dừng và yêu cầu xuất lại thay vì thay mọi text bằng tải đầu tiên. Màu polyline cũng được cập nhật khi giá trị về 0.
8. **Quét tải sàn**: phân biệt Esc với Enter; bổ sung DBText bên cạnh MText; không đọc lại các file phản hồi cũ trong thư mục Loads. Hủy/thất bại không được coi là kết quả mới.
9. **Đơn vị/hướng**: sửa Global-X/Y/Z thành 4/5/6 và Local-1/2/3 thành 1/2/3, Gravity=10; cập nhật cả XAML. Dùng hệ tọa độ tương ứng khi ghi tải. Bỏ fallback đổi hướng hoặc biến lỗi xóa thành tải 0. Đoạn quá ngắn không trở thành tải toàn dầm. CAD bản này yêu cầu mm và UCS World, không tự chuyển đổi đơn vị.
10. **Ghi ETABS**: guard kiểm tra model, khóa, pattern, units; không tự tạo SuperDead. Trước ghi, lưu model và tạo backup cạnh EDB, một bản cho mỗi thao tác UI. Kiểm tra return code trong các nhánh load set/import; các đường gán tải chính có read-back. Batch lỗi có thể để lại thay đổi một phần, nhưng không được xem là thành công toàn bộ; dùng backup để phục hồi nếu cần.
11. **Parser**: nhận tải âm, dấu phẩy thập phân, số mũ, tên pattern có khoảng trắng/Unicode; từ chối text mơ hồ, tên pattern lặp và số không hữu hạn. Kiểm tra kế hoạch nhập tải sàn trước khi bắt đầu gán.
12. **COM và UI**: các cửa sổ dùng chung một STA; các lời gọi ETABS được loại khỏi Task.Run tùy ý. Các thao tác async-void ghi tải có xử lý lỗi; live sync sàn được await và báo lỗi CAD riêng.
13. **Chức năng chưa có đặc tả/kiểm chứng**: bỏ dữ liệu gió mẫu, tạm ngừng ghi tải gió và tạo sàn ảo vùng chữ nhật. Đây là giới hạn có chủ đích của bản thử nghiệm, không phải hoàn tất hai tính năng đó.
14. **Đóng gói**: scripts/build-windows.ps1 chạy test/build và tạo gói mới có manifest/hash; scripts/deploy-local.ps1 kiểm tra hash, yêu cầu đóng host và sao lưu bản cài trước khi chép. Không thay DLL cũ trong Loads hoặc tự ghi vào ứng dụng đang chạy.

## Kết quả kiểm tra

- Core: 40 trường hợp đã chạy, 40 pass / 0 fail. Bao gồm parser, mã hướng/hệ trục, phản hồi cũ/sai drawing/sai export, cancel, path traversal, ghi nguyên tử không ghi đè và 7 fixture JSON. Có kiểm tra cú pháp toàn bộ nguồn C#.
- Plugin: build Release với bộ API NuGet AutoCAD 2018, 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026; 9/9 target pass, 0 warning / 0 error sau các sửa cuối.
- Excel: build Release net48 và đóng gói XLL 64-bit thành công, 0 warning / 0 error ở lần kiểm tra cuối; dùng CSiAPIv1.dll trong Loads làm tham chiếu biên dịch.
- PowerShell: hai script được parse bởi PowerShell 7.4.6 và kiểm tra cách truyền mảng tham số. Chưa chạy quy trình deploy trên Windows.
- Kiểm tra chữ ký các package SDK vẫn bật. NuGet báo không truy cập được máy chủ CRL ở môi trường hiện tại; không tắt verification hoặc sửa checksum để bỏ qua. Việc build không chứng nhận nguồn nhị phân người dùng upload.
- Chưa chạy Excel/WPF, NETLOAD AutoCAD, COM ETABS hoặc thử round-trip trên Windows. File build là gói thử nghiệm, chưa phải bản được chứng nhận cho sản xuất.

## Phần còn lại sau bản sửa này

- Chạy checklist Windows trong BUILD-AND-TEST.md trên từng năm CAD; kiểm tra COM, giải quyết assembly và giao diện. Biên dịch được SDK không đồng nghĩa nạp được ở mọi host.
- Hoàn thiện tải gió sau khi chốt tiêu chuẩn và bộ dữ liệu đối chứng; thiết kế riêng vùng tải cục bộ trước khi bật tạo hình học.
- Chưa có rollback nguyên tử đa ứng dụng hoặc cơ chế hủy lệnh CAD đã chạy từ Excel. Timeout không cho phép tự gửi lại; cần hoàn tất/hủy ở CAD và mở lại Excel.
- Chưa tự chuyển mọi UCS/đơn vị; dữ liệu lịch sử tạo bằng mã hướng cũ phải kiểm tra riêng.
- Chưa thay toàn bộ code-behind bằng MVVM, chưa gom toàn bộ DTO nghiệp vụ trùng lặp vào một assembly. Protocol/parser đã dùng chung; refactor UI sâu cần đi cùng test host.
- Các helper icon/theme/dialog và các thuật toán hình học hiện có vẫn cần kiểm thử với DPI, bản vẽ và mô hình thực; không khẳng định đã loại bỏ mọi lỗi tiềm ẩn.
- Exchange/backup lưu để chẩn đoán/phục hồi; cần chính sách dọn dẹp sau khi người dùng xác nhận không còn thao tác đang chạy. UI có thể bận khi đọc model lớn vì ưu tiên COM đúng apartment trong bản này.

Hướng dẫn build, nạp và test: [BUILD-AND-TEST.md](BUILD-AND-TEST.md).
