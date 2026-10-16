# Extension QKS — tải bộ kiểm thử

Phạm vi: AutoCAD desktop Windows x64 2018–2026, Excel desktop 64-bit. Các gói được tạo từ mã nguồn đã sửa trong đợt rà soát sau commit 6dc3cc7.

## Tải file

Mở file ZIP bên dưới rồi bấm **Download raw file** (biểu tượng tải xuống ở trang file GitHub):

- [QKS-Test-Builds.zip](QKS-Test-Builds.zip): XLL Excel, plugin riêng cho chín năm CAD, hướng dẫn và script chép file tùy chọn.
- [QKS-Updated-Source.zip](QKS-Updated-Source.zip): mã nguồn đã cập nhật, project, test và script build.
- [SHA256SUMS.txt](SHA256SUMS.txt): hash của hai gói ZIP.

## Cài một lần, dùng qua Ribbon

1. Giải nén QKS-Test-Builds.zip, mở thư mục QKS-Test-Builds.
2. Lưu công việc và đóng Excel/AutoCAD.
3. Chép nguyên thư mục Excel và AutoCAD vào C:\QKS_JSON_Data\Loads, giữ cấu trúc thư mục năm CAD.
4. Trong Excel: File → Options → Add-ins → Manage Excel Add-ins → Go → Browse, chọn C:\QKS_JSON_Data\Loads\Excel\QksExcelAddIn-AddIn64-packed.xll.
5. Mở model ETABS thử nghiệm và bản vẽ CAD đã lưu, đơn vị thực tế mm, UCS World. Chọn các file qua **Kết Nối API** trên Ribbon. Khi gửi lệnh, add-in tự nạp plugin đúng năm CAD.
6. Xuất mặt bằng mới trước khi nhập tải hoặc live sync.

PowerShell deploy-local.ps1 là lựa chọn thay cho chép thủ công; không phải bước kết nối hằng ngày.

## Trạng thái xác minh

- 40 kiểm thử core đạt.
- XLL Excel và chín target SDK AutoCAD build thành công.
- Chưa kiểm thử trực tiếp trong Excel/ETABS/AutoCAD trên Windows.
- Tải gió demo và tạo sàn ảo hình chữ nhật đang tạm khóa.

Chi tiết trong BUILD-AND-TEST.md và CHANGE-REPORT.md bên trong gói. Chỉ thử trên bản sao model; trước ghi QKS lưu model và tạo backup.
