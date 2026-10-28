# QKS 2D — Display / Color Objects

## Sử dụng

Mở **Display → Color Objects · Màu cấu kiện**. Popup có headbar navy vuông theo hệ thống và các nhóm riêng: dầm, cột, sàn, vách, giằng, opening, khác, nút.

- Nhập **R, G, B** (số nguyên 0–255) cho từng loại. Ô màu và mã HEX bên cạnh cập nhật ngay để xem trước; hình mô hình chỉ thay đổi khi bấm **Áp dụng**.
- Chọn **Theo loại cấu kiện · Color Objects** để dùng bảng màu này. Chọn **Theo Property** để dùng màu tiết diện hiện có; bảng màu Object vẫn được lưu để dùng khi chuyển lại. Checkbox màu Property trong Settings phản ánh chế độ vừa áp dụng.
- **Áp dụng** cập nhật hình và lưu cấu hình trên máy. **Mặc định** điền lại bảng màu gốc và chọn chế độ theo loại; bấm Áp dụng để xác nhận. **Đóng** bỏ các chỉnh sửa chưa áp dụng.
- RGB không hợp lệ sẽ báo lỗi và không áp dụng bất kỳ dòng nào. Nếu ghi file thất bại, thông báo nêu rõ màu chỉ áp dụng trong phiên hiện tại.

## Phạm vi màu

Mỗi màu áp dụng cho mọi đối tượng cùng loại trên Plan/Elevation, bao gồm nét và phần tô đang bật theo Extrude. Phần tô giữ độ trong suốt hiện tại nên có thể nhạt hơn ô màu mẫu. Cột/vách vẫn có đường bao khi tắt Extrude; sàn trên Plan vẫn ẩn viền thường. Cấu kiện đang chọn giữ đỏ đô.

Màu chữ tiếp tục điều chỉnh tại **Display → Text**. Ký hiệu/mũi tên tải có màu riêng. Thay đổi Color Objects không sửa ETABS, không sửa tải hoặc gán màu vào mô hình gốc, không đổi màu theo từng UniqueName.

Cấu hình lưu tại `%LocalAppData%\QKS\Viewer\object-colors.json` và dùng lại khi mở viewer. Màu Object và chế độ lưu là trạng thái ở lần bấm Áp dụng gần nhất. Màu Property lưu riêng, không bị ghi đè. Đọc lại/chuyển mô hình vẫn dùng bảng màu Object trên máy.

## Cài và nghiệm thu

1. Đóng Excel, bỏ chọn bản QKS cũ; giải nén ZIP và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS.
2. Display → Color Objects → chọn Theo loại, thử dầm R=255 G=0 B=0; kiểm tra chỉ màu dầm đổi, không đổi màu chữ và tải.
3. Thử riêng cột/sàn/vách; bật/tắt Extrude, chọn/bỏ chọn và chuyển Plan/Elevation để kiểm tra đường bao, phần tô và màu chọn.
4. Đổi sang Theo Property rồi quay lại Theo loại; màu Object đã nhập phải còn. Đóng/mở viewer để kiểm tra khôi phục cấu hình.
5. Nhập RGB=256 hoặc chữ để kiểm tra báo lỗi; bấm Mặc định → Áp dụng để khôi phục.

Build WPF và kiểm thử core/VM chạy trong cloud. Tương tác và màu hiển thị thực tế trên WPF/Excel/ETABS Windows cần nghiệm thu trên máy người dùng.
