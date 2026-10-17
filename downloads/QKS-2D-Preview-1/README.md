# QKS 2D — Preview 1

**Viewer chỉ đọc Plan/Elevation ETABS**, Excel desktop Windows 64-bit. SAFE mục tiêu 18–22 (ưu tiên 22.5), chưa có adapter hoạt động trong gói này.

## Tải trực tiếp

- [Tải QKS-2D-Preview-1.zip](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-1/QKS-2D-Preview-1.zip) — XLL mới, snapshot mẫu, hướng dẫn, script probe và log kiểm tra.
- [Tải QKS-2D-Preview-1-Source.zip](https://raw.githubusercontent.com/phamquockhanh495-beep/Excel-Add-in_Loads/refs/heads/qks/test-build-2018-2026/downloads/QKS-2D-Preview-1/QKS-2D-Preview-1-Source.zip) — source/project/test/script; tự tham chiếu SDK CSI/AutoCAD trên máy build.
- [SHA256SUMS.txt](SHA256SUMS.txt) — hash hai ZIP.

Cũng có thể mở từng file ZIP trong folder GitHub rồi chọn **Download raw file**. Nếu trình duyệt mở link thay vì lưu, dùng **Save link as / Lưu liên kết thành**.

## Cài nhanh

1. Đóng Excel, giải nén bộ Preview vào thư mục cố định. Bỏ chặn ZIP trong Windows Properties nếu có mục Unblock.
2. Excel → File → Options → Add-ins → Manage Excel Add-ins → Go. Bỏ chọn XLL QKS cũ; Browse tới `Excel\QksExcelAddIn-AddIn64-packed.xll` của gói mới.
3. Extension QKS → **Mô Hình 2D** → **Mở snapshot** → `viewer-readonly.json`. Header phải ghi OFFLINE / SYNTHETIC TEST FIXTURE.
4. Đọc thật: mở/lưu ETABS, dùng **Kết Nối API** chọn đúng model, sau đó **Mô Hình 2D → Đọc mô hình**.

Viewer không cần plugin AutoCAD hoặc deploy-local.ps1. Các chức năng CAD cũ dùng bộ cài trước nếu cần.

## Đã kiểm tra và chưa kiểm tra

- 73/73 core/VM tests đạt; build packed XLL 0 warning/0 error.
- Chưa mở Excel/WPF hoặc đọc model qua ETABS/SAFE COM trong môi trường Windows.
- Chưa có draft/Apply trong viewer; chưa đọc mọi loại tải. Gói kèm báo cáo nêu rõ phạm vi và giới hạn.

Xem [báo cáo chi tiết, cách thử và trình tự tiếp theo](../../docs/VIEWER-PREVIEW-1.md). Manifest trong ZIP ghi commit nguồn, hash SDK tham chiếu và hash các file.
