# Build và nạp thử Extension QKS

Phạm vi cấu hình: AutoCAD desktop đầy đủ trên Windows x64, các năm 2018–2026; Excel desktop 64-bit. Mỗi năm CAD dùng DLL được build với SDK đúng năm đó. Build thành công không thay thế việc NETLOAD và kiểm thử trên host. Không bao gồm LT/Mac/Web hoặc chứng nhận các bản vertical.

## Chuẩn bị trên Windows

- .NET SDK 8 (global.json chọn 8.0.408, cho phép bản feature mới trong major 8).
- Visual Studio 2022/Build Tools có công cụ .NET desktop; targeting packs tương ứng .NET Framework 4.6 (CAD 2018), 4.7 (CAD 2019–2020), 4.8 (CAD 2021–2024 và Excel).
- SDK managed của AutoCAD đúng năm: acmgd.dll, acdbmgd.dll, accoremgd.dll. Có thể trỏ tới thư mục cài CAD hoặc ObjectARX SDK hợp lệ. Không trộn assembly của các năm.
- CAD 2025–2026 dùng .NET 8 Windows Desktop; cài runtime phù hợp hướng dẫn của Autodesk.
- ETABS 22 với CSiAPIv1.dll để build Excel; tương thích các đời ETABS khác chưa được chứng nhận. Phiên bản API dùng khi chạy phải phù hợp phiên bản đã build.
- NuGet.org cho restore, Git để ghi commit trong manifest.

Không cần tạo worktree; dùng checkout hiện có. Chưa thay các DLL cũ trong Loads bằng binary mới trong Git.

## Build gói thử nghiệm

Tại thư mục gốc kho, chạy PowerShell:

```powershell
.\scripts\build-windows.ps1 -AcadVersion 2024 `
  -AcadSdkDir 'C:\Program Files\Autodesk\AutoCAD 2024' `
  -EtabsApiPath 'C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll'
```

Đổi năm và đường dẫn để build CAD khác. Script chạy kiểm thử core, build Excel và plugin; tạo thư mục mới trong artifacts/packages/QKS-<year>-<timestamp>. Gói chứa:

- Excel/QksExcelAddIn-AddIn64-packed.xll — XLL Excel 64-bit.
- AutoCAD/<year>/QKS_Loads_AcadPlugin.dll và Newtonsoft.Json.dll — cùng một thư mục, kèm file .deps.json nếu có.
- manifest.json — commit, trạng thái working tree, thời gian, hashes và target.

Không chép SDK Autodesk vào gói. Không dùng DLL/XLL cũ trong Loads hoặc obj để thử bản sửa.

Để build thủ công:

```powershell
dotnet run --project tests/Qks.Core.Tests/Qks.Core.Tests.csproj -c Release -- .
dotnet build QksExcelAddIn.csproj -c Release '-p:EtabsApiPath=C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll' -p:ExcelDnaCreate32BitAddIn=false
dotnet build QKS_Loads_AcadPlugin.csproj -c Release -p:AcadVersion=2024 '-p:AcadSdkDir=C:\Program Files\Autodesk\AutoCAD 2024'
```

Dùng project path rõ ràng; .slnx chỉ dùng với IDE/MSBuild có hỗ trợ định dạng đó. Các project vẫn ở gốc nhưng đã tách danh sách nguồn, XAML và thư mục trung gian theo project/năm; Excel không biên dịch mã Autodesk.

## Triển khai và nạp lại

1. Đóng toàn bộ Excel và AutoCAD. Assembly đã NETLOAD không được coi là cập nhật chỉ bằng copy đè hoặc NETLOAD lần nữa.
2. Cài một lần bằng cách chép thư mục `Excel` và thư mục `AutoCAD` của gói vào `C:\QKS_JSON_Data\Loads`, giữ nguyên thư mục năm bên trong AutoCAD. Sau đó sử dụng qua Ribbon như trước; không cần chạy PowerShell mỗi lần kết nối.

Script dưới đây là lựa chọn thay cho chép thủ công: kiểm tra hash, sao lưu bản cài cũ và chép file đúng chỗ. Với gói vừa build:

```powershell
.\scripts\deploy-local.ps1 -PackagePath '.\artifacts\packages\QKS-2024-<timestamp>'
```

Gói thử nghiệm chứa nhiều adapter cần thêm `-AcadVersion 2024` (hoặc năm đang dùng) khi gọi deploy-local.ps1. Script kiểm tra hash, sao lưu thư mục cài cũ rồi copy; không nuốt lỗi copy hoặc file đang khóa. Mặc định cài vào C:\QKS_JSON_Data\Loads. Nếu dùng Destination khác, cấu hình biến QKS_HOME trỏ đúng thư mục đó trước khi khởi động Excel. QKS_HOME chứa đường dẫn, không chứa bí mật.

3. Khởi động AutoCAD; tạo/mở bản vẽ có đơn vị thực tế mm, INSUNITS=4 và UCS=World. Không đổi nhãn đơn vị để che một bản vẽ thực tế dùng m/inch: cần chuyển đổi bản vẽ đúng trước.
4. Nếu Windows đánh dấu file tải từ Internet, kiểm tra nguồn/hash trước khi gỡ block theo chính sách máy. Dùng trusted location hoặc ký plugin; giữ SECURELOAD và các kiểm tra chữ ký. Không tắt bảo mật để nạp DLL.
5. Giữ Newtonsoft.Json.dll và file .deps.json nếu có cùng thư mục plugin. Sau khi kết nối qua Ribbon, Excel tự chọn và NETLOAD đúng adapter khi gửi yêu cầu CAD. NETLOAD thủ công chỉ là bước chẩn đoán tùy chọn, không phải yêu cầu cho mỗi lần sử dụng.
6. Trong Excel: File → Options → Add-ins → Manage Excel Add-ins → Go → Browse → chọn Excel/QksExcelAddIn-AddIn64-packed.xll. Bỏ chọn add-in QKS cũ để không nạp hai bản.
7. Mở một bản sao model ETABS, lưu file, tạo sẵn pattern đúng loại, mở khóa. Chọn rõ model và bản vẽ qua Kết Nối API. Không dùng model sản xuất cho lần test đầu.
8. Xuất mặt bằng từ cửa sổ tải tường/sàn trước khi nhập lại hoặc live sync. Phạm vi export mới chỉ tồn tại trong phiên cửa sổ đó. Đổi model/tầng/pattern hoặc mở lại cửa sổ thì xuất lại. Dữ liệu CAD cũ không có scope/segment metadata cần xuất lại; không tự di trú bằng phỏng đoán.

## Các thay đổi hành vi cần biết

- Trước lần ghi đầu của mỗi thao tác, QKS lưu model hiện tại và tạo bản sao EDB trong thư mục QKS_Backups cạnh model. Nếu lưu/sao lưu thất bại thì dừng ghi. Giữ bản sao lưu: lỗi giữa batch có thể để lại thay đổi một phần, chưa có transaction hoặc rollback nguyên tử giữa ba ứng dụng.
- Ghi tải kiểm tra khóa model, pattern và units; không tự tạo SuperDead và không đổi sang hướng tải khác để che API error. Một số đường ghi có read-back; toàn bộ round-trip vẫn cần kiểm tra trên ETABS.
- Global-X/Y/Z dùng mã 4/5/6; Local-1/2/3 dùng 1/2/3; Gravity=10. Các tải đã gán bằng mã cũ cần được kiểm tra, bản sửa không tự chuyển tải lịch sử.
- CAD phải là mm/UCS World trong bản này. Chưa tự động chuyển đổi mọi hệ đơn vị/UCS.
- Gửi lệnh qua COM + file request/response có ID; không dùng Named Pipes chưa có server. Chỉ một yêu cầu CAD chạy cùng lúc trong mỗi Excel. Timeout không tự retry; hoàn tất/hủy lệnh CAD và khởi động lại Excel trước khi gửi lại.
- Text cùng tên dầm/tiết diện ở export khác không được cập nhật cùng nhau. Nếu copy thủ công làm trùng metadata hoặc thay đổi phân đoạn tải, cần xuất mặt bằng mới.
- Tải gió tạm ngừng đọc/gán qua CAD cho tới khi hoàn thiện công thức theo tiêu chuẩn; đã bỏ dữ liệu minh họa tự sinh.
- Tạo sàn ảo từ vùng chữ nhật tạm dừng để tránh tự thay hình học/độ cứng. Gán tải đều và load set lên ô sàn hiện có vẫn giữ.
- Các cửa sổ dùng chung STA; thao tác COM không chạy tùy ý trên thread pool. Đọc mô hình lớn có thể làm UI bận, cần đo hiệu năng trên host.

## Checklist test trên Windows

1. Load XLL và NETLOAD plugin, mở đủ cửa sổ. Không thiếu dependency/icon hoặc xuất hiện ribbon trùng.
2. ETABS → CAD: một tầng nhỏ, basepoint khác 0, dầm xiên, ba đoạn tải khác nhau, sàn có nhiều pattern. Đối chiếu tọa độ và giá trị.
3. Sửa text có tải âm/dấu phẩy thập phân rồi nhập CAD → ETABS. Đọc lại tải và so theo đối tượng/pattern/hướng/đơn vị, không chỉ nhìn thông báo.
4. Hai tầng trùng tiết diện, hai bản export cùng tên dầm; live sync một bộ phải giữ bộ còn lại. COPY bộ export phải được phát hiện khi trùng text metadata.
5. Esc khi chọn không được chọn tất cả; DBText và MText có metadata hợp lệ phải được quét đúng.
6. Đổi model/bản vẽ, đóng host, giữ JSON cũ, mô phỏng lỗi ghi và timeout: không được dùng dữ liệu cũ hoặc báo thành công toàn bộ.
7. Kiểm tra QKS_Backups, phục hồi model test khi batch lỗi; kiểm tra đơn vị sau thao tác.
8. Thực hiện riêng trên từng CAD được công bố hỗ trợ. Hiện có kiểm tra compile SDK; chưa có kết quả chạy host cho chín năm.
