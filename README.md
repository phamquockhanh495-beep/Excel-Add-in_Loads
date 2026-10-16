# Extension QKS — Excel / ETABS / AutoCAD

Excel Add-in quản lý tải tường và tải sàn, trao đổi với ETABS và AutoCAD. Target build hiện tại: AutoCAD desktop Windows x64 2018–2026; Excel desktop 64-bit.

- [Build trên Windows, nạp XLL/plugin và checklist test](docs/BUILD-AND-TEST.md)
- [Báo cáo thay đổi, kết quả kiểm tra và giới hạn](docs/CHANGE-REPORT.md)

Đây là bản để kiểm thử: compile SDK không thay thế kiểm thử host. Tải gió và tạo sàn ảo từ vùng chữ nhật đang tạm khóa. Dùng model test; trước thao tác ghi QKS lưu model và tạo bản sao trong QKS_Backups.

Build từ các project nguồn ở gốc; không lấy DLL/XLL cũ trong Loads làm bản phát hành mới.
