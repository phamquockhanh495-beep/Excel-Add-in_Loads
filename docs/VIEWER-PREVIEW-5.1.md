# QKS 2D Preview 5.1 — Esc bỏ chọn

Cập nhật 18/10/2026. Bổ sung Esc theo yêu cầu, giữ các chức năng của [Preview 5](VIEWER-PREVIEW-5.md).

- Nhấn **Esc** khi thao tác trên hình hoặc bảng nguồn để bỏ chọn toàn bộ dầm/sàn, gồm tập chọn nhiều đối tượng. Hình, bảng và badge dưới trái được cập nhật cùng nhau.
- Esc đồng thời hủy khung quét, pan đang thực hiện hoặc chọn hai điểm mặt đứng; xóa điểm đầu/snap tạm. Không làm mất cấu kiện, tải, nháp hoặc trạng thái Delete/Khôi phục trong workspace.
- Khi focus nằm trong TextBox/ô nhập hoặc PasswordBox, giữ xử lý Esc của control. Khi ComboBox đang mở, Esc đóng dropdown; nhấn Esc sau khi quay về hình/bảng để bỏ chọn.
- **Delete** tiếp tục bỏ cấu kiện khỏi workspace; **Khôi phục** hiện lại. Esc chỉ hủy chọn/thao tác.

Gói mới: [QKS-2D-Preview-5.1](../downloads/QKS-2D-Preview-5.1/). Đóng Excel, bỏ nạp XLL cũ, giải nén và nạp `Excel/QksExcelAddIn-AddIn64-packed.xll`. Headbar phải ghi **PREVIEW 5.1**.

Thử một dầm, một sàn và tập chọn Ctrl/quét → Esc → kiểm tra hình/bảng/badge hết chọn. Thử Esc trong lúc kéo khung hoặc sau điểm đầu dựng Elevation; thử Esc khi sửa ô dữ liệu và khi dropdown đang mở. Các thao tác phím/focus WPF cần kiểm tra trên Windows; cloud chỉ chạy kiểm tra core/VM và compile XAML/XLL.
