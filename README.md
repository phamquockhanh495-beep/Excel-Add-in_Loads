# Extension QKS — Excel / ETABS / AutoCAD

Excel Add-in quản lý tải tường và tải sàn, trao đổi với ETABS và AutoCAD. Target build hiện tại: AutoCAD desktop Windows x64 2018–2026; Excel desktop 64-bit.

- [Build trên Windows, nạp XLL/plugin và checklist test](docs/BUILD-AND-TEST.md)
- [Báo cáo thay đổi, kết quả kiểm tra và giới hạn](docs/CHANGE-REPORT.md)
- [Viewer 2D Preview 1: thay đổi, cài Excel, kiểm thử và tiến độ Plan/Elevation/SAFE](docs/VIEWER-PREVIEW-1.md)
- [Viewer 2D Preview 1.1: cập nhật giao diện Material Design và XLL mới](docs/VIEWER-PREVIEW-1.1.md)
- [Viewer 2D Preview 2: nháp tải tường/sàn, Undo/Redo, Trước/Sau và chọn hai điểm Elevation](docs/VIEWER-PREVIEW-2.md)
- [Viewer 2D Preview 2.1: headbar/tab QKS, ẩn cột và bố cục vừa màn hình/DPI](docs/VIEWER-PREVIEW-2.1.md)
- [Viewer 2D Preview 3: Apply ETABS, gió dầm biên/User Loads và trạng thái G5–G7](docs/VIEWER-PREVIEW-3.md)
- [Viewer 2D Preview 4: quét/chọn nhiều, nhãn, ComboBox kết nối và nhập gió Excel/Story](docs/VIEWER-PREVIEW-4.md)
- [Viewer 2D Preview 5: Delete/Khôi phục workspace, nhãn xoay, badge và tự đọc kết nối API](docs/VIEWER-PREVIEW-5.md)
- [Viewer 2D Preview 5.1: Esc bỏ chọn dầm/sàn và hủy thao tác](docs/VIEWER-PREVIEW-5.1.md)

Đây là bản để kiểm thử: compile SDK không thay thế kiểm thử host. Viewer Preview 5 có luồng gán tải thử và **nhập gió trực tiếp từ Excel/popup theo Story, không quy đổi áp lực**; Delete chỉ bỏ khỏi workspace, không xóa model hoặc tải/nháp. Chưa tự lấy tâm cứng hoặc có adapter live SAFE. Cửa sổ tải gió cũ và tạo sàn ảo từ vùng chữ nhật vẫn tạm khóa. Dùng model test; trước thao tác ghi QKS lưu model và tạo bản sao trong QKS_Backups.

Build từ các project nguồn ở gốc; không lấy DLL/XLL cũ trong Loads làm bản phát hành mới.
