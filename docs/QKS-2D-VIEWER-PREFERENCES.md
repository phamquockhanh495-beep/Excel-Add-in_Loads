# QKS 2D — Nhãn tải tường và ghi nhớ setup

## Nhãn tải tường trên Plan

Đối với tải phân bố đứng trên Frame đang vẽ bằng nét đỏ đô, nhãn rút gọn thành **Pattern - Giá trị Đơn vị**, ví dụ `WL - 8.91 kN/m`. Tải biến thiên giữ hai giá trị có dấu, ví dụ `WL - 3 → 5 kN/m`. Hệ trục, hướng và r1/r2 vẫn có trong bảng quản lý tải, không đưa lên nhãn này. Tải Joint, tải mặt đứng và các glyph khác giữ cơ chế hiện tại.

## Ghi nhớ khi đóng/mở

Cấu hình bổ sung được lưu khi đóng cửa sổ Mô hình 2D bình thường vào:
`%LocalAppData%\QKS\Viewer\viewer-settings.json`.

- Hiện/ẩn, Extrude, Label, UniqueName, Section độc lập cho từng loại cấu kiện.
- Extrude tổng, chế độ màu theo Object/Property, bộ lọc loại cấu kiện, nửa dải xem và đảo hướng.
- Chế độ Load Pattern/Load Sets, checkbox Frame/Shell/Joint, hiện nhãn tải và hiện thành phần bằng 0.
- Trạng thái panel trái/phải; cửa sổ hẹp vẫn điều chỉnh để không chèn mất vùng vẽ.
- Tầng, Pattern và Load Set gần nhất được khôi phục sau lần đọc API đầu tiên, chỉ khi đúng đường dẫn file mô hình đã lưu. Tên không còn tồn tại sẽ dùng lựa chọn mặc định hợp lệ.

Cỡ chữ, RGB chữ và Color Objects tiếp tục lưu bằng nút Áp dụng tại các hộp thoại tương ứng. Màu Property, danh mục mặt cắt và vị trí cửa sổ vẫn dùng cơ chế lưu hiện có.

Cấu hình không chứa hình học/tải mô hình, kết nối COM hay dữ liệu gán tải; phải đọc mô hình từ API khi mở lại. Không khôi phục lựa chọn đối tượng, Hide/Isolate, tìm kiếm tạm, checkbox “Chỉ đối tượng đang chọn”, zoom/pan hoặc mặt cắt chưa lưu. Mở lại bắt đầu từ Plan; các mặt cắt đã lưu có thể mở lại từ danh mục. Lưu lúc đóng bình thường, không cam kết lưu khi Excel bị dừng cưỡng bức.

File cấu hình được kiểm tra trước khi dùng và ghi thay thế an toàn. Nếu cấu hình hỏng/khác phiên bản, chương trình thông báo và giữ nguyên file cũ; phiên đó không ghi đè file này. Nếu cần trở về mặc định, đóng viewer và đổi tên file để giữ bản sao trước khi mở lại. Lỗi ghi sẽ được thông báo khi đóng, không xóa cấu hình cũ.

## Xuất PDF

Xem [kế hoạch Loading Case Plan](PLAN-LOADING-CASE-PLAN-PDF.md). Chưa triển khai xuất PDF trong bản build này.

## Kiểm tra trên máy sử dụng

Đổi nhãn từng loại, Extrude, nhóm tải, màu theo Property và panel; chọn tầng/Pattern; đóng viewer rồi mở lại cùng mô hình. Đối chiếu cấu hình sau đọc API. Thử mô hình khác và tên Pattern đã xóa để kiểm tra fallback. Đối chiếu nhãn tải tường và tải biến thiên. Cloud đã chạy kiểm tra core/view-model và build; chưa nghiệm thu WPF/Excel/ETABS trực tiếp trên Windows.
