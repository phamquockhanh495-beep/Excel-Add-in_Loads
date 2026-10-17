# Kế hoạch không gian làm việc trực quan QKS — điều chỉnh phạm vi

Theo yêu cầu mới, kế hoạch hiện tại tập trung vào **WPF 2D** cho cả **ETABS và SAFE**, bổ sung **Plan + Elevation từ ETABS**, với ba nhóm tải: tường, gió và sàn.

Xem [kế hoạch 2D chi tiết](PLAN-2D-LOAD-WORKSPACE.md) và các hình minh họa trong [design-2d](design-2d/).

Phạm vi gồm layout, thuộc tính mô hình, viewport/layer, nét/chữ/nhãn, màu theo property, palette/preset, tải nháp, capability từng nguồn, kiểm tra và gán tải.

3D được đưa ra ngoài các mốc phát triển hiện tại. Cập nhật 17/10/2026: đã triển khai [Viewer 2D Preview 1](VIEWER-PREVIEW-1.md) và build XLL; đây là prototype chỉ đọc, chưa nghiệm thu host Windows. SAFE mục tiêu v18–v22, ưu tiên bản v22.5 người dùng đang sử dụng; chưa xác nhận tương thích. Mã nguồn và tài liệu được cập nhật trên nhánh qks/test-build-2018-2026.

Lộ trình hiện tại có 8 giai đoạn G0–G7; tổng dự toán cơ sở 42–68 ngày làm việc, cộng dự phòng thành khoảng 50–85 ngày (10–17 tuần). Chi tiết và giả định trong mục 9 của kế hoạch 2D.
