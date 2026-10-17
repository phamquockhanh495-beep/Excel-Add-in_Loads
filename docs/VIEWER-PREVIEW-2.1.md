# QKS 2D Preview 2.1 — Bố cục gọn, tab QKS và ẩn cột

Cập nhật 17/10/2026 theo bốn phản hồi từ ảnh chụp Preview 2. [Phạm vi nháp tải của Preview 2](VIEWER-PREVIEW-2.md) vẫn được sử dụng.

## 1. Headbar theo Tải tường/Tải sàn

Thay headbar 64 DIP bằng thanh tiêu đề **36 DIP**, nền navy `#0F2240`, logo Excel, chữ QKS và tiêu đề nhỏ như hai cửa sổ tải hiện có. Thanh này chỉ có các nút cửa sổ: thu nhỏ, phóng to/khôi phục và đóng; có thể kéo ở vùng tiêu đề, resize viền và nhấp đôi tiêu đề để phóng to.

Các lệnh được đưa về đúng khu vực thao tác:

- **Chọn API / Đọc lại / Hủy đọc:** panel **Hiển thị → Kết nối mô hình**. Hủy đọc chỉ hiện khi đang đọc.
- **Mở/Lưu snapshot:** mục thu gọn **File snapshot** trong panel Hiển thị.
- **Mở/Lưu nháp/Bộ mới:** panel **Tải / thuộc tính**.

Đóng bằng nút X vẫn kiểm tra nháp chưa lưu như trước. Không thay đổi thiết lập DPI toàn tiến trình Excel.

## 2. Tab và nội dung theo kiểu QKS

Dùng lại **QksBeamTabControlStyle / QksBeamTabItemStyle** của WallLoadView/ShellLoadView: nền active xanh nhạt, chữ xanh đậm, gạch đáy và hover giống cửa sổ tải. Padding được giảm để vừa bố cục viewer. Các button/ô nhập vẫn dùng Material Design.

- **Dầm/Tường:** editor đoạn s1/s2, q1/q2 theo dầm.
- **Sàn:** editor uniform p, không hiện các ô đoạn dầm hoặc q2 thừa.
- **Gió:** nội dung riêng ghi rõ chức năng đang phát triển; không còn các ô nhập sàn vô hiệu hóa bên dưới như ảnh phản hồi. Chọn một dầm trên hình không tự đưa người dùng ra khỏi tab Gió.

Editor được tách thành UserControl dùng chung. Chọn một dòng nháp đã lưu vẫn chuyển đến tab đúng loại tải. Tab bảng phía dưới cũng dùng style QKS để đồng bộ.

## 3. Không đưa cột lên giao diện

Cột được loại khỏi Plan/Elevation, picking, danh sách loại cấu kiện, bảng đối tượng, bảng tải hiển thị và bảng Trước/Sau. Bộ đếm phản ánh các đối tượng/tải thuộc phạm vi hiển thị này.

Snapshot nguồn vẫn giữ cột và dữ liệu của chúng để giữ nguyên identity/fingerprint, lưu/mở nháp và thông tin mô hình gốc. Không xóa cấu kiện trong ETABS, không đổi mã đối tượng hoặc dữ liệu tải nguồn. Nháp đã lưu với snapshot trước tiếp tục được kiểm tra theo cùng quy tắc fingerprint.

## 4. Kích thước theo màn hình và DPI

- Kích thước ưu tiên giảm từ **1500×940** xuống **1180×740 DIP**. WPF dùng DIP nên kích thước cố định cũ có thể vượt màn hình khi Windows đặt 125–150%.
- Khi mở, lấy WorkingArea của màn hình chứa cửa sổ và tỷ lệ device/DIP của WPF, giảm kích thước nếu cần và đặt cửa sổ trong vùng làm việc. Minimum cũng giảm theo giới hạn khả dụng, tránh bị minimum ép tràn.
- Hook cửa sổ xử lý vùng phóng to theo WorkingArea để chừa taskbar; kiểm tra lại sau đổi DPI, đổi display, kéo/resize và khôi phục cửa sổ. Phần Win32 này cần nghiệm thu thực trên Windows.
- Hai panel giảm còn khoảng 200/285 DIP; button thân cửa sổ cao 30 DIP, ô nhập và bảng gọn hơn. Thuộc tính đối tượng đặt trong Expander để editor có thêm chỗ.
- Cửa sổ rộng từ 1060 DIP: mặc định hiện hai panel. Rộng 820–1059 DIP: mặc định chỉ hiện panel tải. Hẹp hơn 820 DIP: mặc định ưu tiên vùng vẽ, thu hai panel.
- Dùng **Hiển thị** hoặc **Tải / thuộc tính** trên thanh công cụ vùng vẽ để mở/đóng panel. Ở chế độ hẹp, mở một bên sẽ thu bên còn lại. Panel có thanh cuộn riêng; thanh công cụ tự xuống dòng khi thiếu chỗ.
- Bảng dưới giảm chiều cao theo cửa sổ; vẫn có thanh chia để điều chỉnh. Không dùng Viewbox để thu nhỏ toàn bộ chữ/control.

Ví dụ kiểm tra thuật toán: work area 1366×728 px ở DPI 125% cho kích thước khoảng **1068.8×558.4 DIP**, nằm trong work area; giới hạn minimum không ép chiều cao trở lại 720 DIP như bản cũ.

## 5. Cài và kiểm tra

Lấy [gói Preview 2.1 trên GitHub](../downloads/QKS-2D-Preview-2.1/). Đóng toàn bộ Excel, bỏ đăng ký XLL cũ, giải nén và đăng ký `Excel\QksExcelAddIn-AddIn64-packed.xll` mới. Thanh tiêu đề phải có **PREVIEW 2.1**.

1. Nếu panel Hiển thị đang thu, bấm **Hiển thị** trên vùng vẽ. Mở mục **File snapshot → Mở snapshot**, chọn `viewer-readonly.json` trong gói.
2. Kiểm tra không còn cột C1/C2 trên hình, bảng/lọc; mặc định Roof có 5/6 đối tượng thuộc phạm vi hiển thị. Snapshot mẫu gốc vẫn có 8 đối tượng.
3. Mở panel **Tải / thuộc tính → Mở nháp**, chọn `viewer-draft.qksdraft.json`; thử Dầm/Tường, Sàn, Gió và sửa/Undo/Redo như Preview 2.
4. Thử resize, thu/phóng, kéo tiêu đề, đóng có nháp chưa lưu; kiểm tra footer và nút X vẫn nằm trong vùng màn hình, taskbar còn truy cập được.
5. Kiểm tra ở DPI 100/125/150/200% và nếu có nhiều màn hình, kéo giữa các màn hình khác DPI. Thử các nút Hiển thị/Tải để lấy lại panel bị thu.

## 6. Kết quả kiểm tra

- **130/130 core/VM tests đạt**: 117 hồi quy + 13 test ẩn cột, bảng/count/fingerprint, tab Gió và phép tính kích thước/DPI/panel mode.
- Build packed XLL net48/Excel 64-bit: **0 warning / 0 error**.
- XAML mới biên dịch; key style tab QKS và MaterialDesignFlatButton được đối chiếu với resource hiện có/thư viện 5.3.2.
- Chưa chạy WPF/Excel trên Windows trong cloud: các hook Win32, kéo/phóng to/taskbar và render/DPI cần thử bằng gói mới. Test toán học/VM không chứng minh hành vi cửa sổ thực ở mọi cấu hình.
- Chưa mở Apply hoặc adapter SAFE trong đợt sửa giao diện này. Bộ nháp vẫn không ghi model.

Gói có log kiểm tra, snapshot/nháp mẫu nhân tạo, manifest commit nguồn và hash từng file; ZIP được kiểm tra CRC/SHA-256 và tải lại từ GitHub khi xuất bản.
