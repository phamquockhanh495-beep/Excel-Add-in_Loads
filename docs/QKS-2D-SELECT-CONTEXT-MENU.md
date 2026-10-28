# QKS 2D — Select theo thuộc tính và menu chuột phải

## Select

Nút **Select** nằm trên thanh công cụ vùng 2D.

1. Chọn loại: **Cột, Dầm, Sàn hoặc Vách**.
2. Chọn thuộc tính: **Section** (Section/Property của ETABS), **UniqueName** (tên gốc, không thêm Frame:/Area:) hoặc **Label**.
3. Phạm vi mặc định là **Vùng xem hiện tại**, theo tầng/mặt cắt và các bộ lọc đang áp dụng. Có thể đổi sang **Toàn mô hình (trừ đối tượng ẩn)** để chọn qua các tầng. Isolate/Hide vẫn được tôn trọng.
4. Gõ để lọc danh sách giá trị; Ctrl/Shift để chọn nhiều dòng. Việc lọc chữ không tự chọn đối tượng. Các dòng đã chọn được đối chiếu chính xác, phân biệt hoa/thường, không coi B1 là B10. Label có thể trùng ở nhiều tầng, vì vậy cần kiểm tra phạm vi.
5. **Chọn mới** thay nhóm đang chọn; **Thêm chọn** giữ nhóm trước và bổ sung đối tượng khớp; **Bỏ chọn** loại đối tượng khớp khỏi nhóm trước. Không chọn dòng nào sẽ không thay đổi nhóm.

Nếu chọn toàn mô hình, số đang chọn có thể lớn hơn số thấy trên tầng hiện tại. Đổi tầng để xem các đối tượng còn lại. Cửa sổ Select đóng khi dữ liệu nguồn đổi để tránh áp dụng danh sách cũ cho mô hình mới.

## Menu chuột phải

Click phải trong vùng 2D mở menu, giữ nguyên nhóm đang chọn. Kéo giữ chuột phải vẫn pan; chuột giữa vẫn pan.

| Thao tác | Phím tắt | Kết quả |
|---|---|---|
| Isolate | Ctrl+I | Chỉ hiện nhóm đang chọn trong vùng xem. |
| Hide | Ctrl+H | Ẩn nhóm đang chọn; dùng chung trạng thái với Delete và Khôi phục. |
| Show All | Ctrl+A | Bỏ Isolate, khôi phục đối tượng ẩn, bỏ lọc loại/từ khóa và bật lại các lớp cấu kiện. Giữ tầng/mặt cắt, màu, nhãn và Extrude. |
| Settings | Alt+S | Mở bảng cài đặt hiển thị và màu sắc hiện có. |

Ctrl+I/H/A hoạt động khi vùng vẽ nhận focus (click vào vùng vẽ trước). Trong ô nhập liệu, Ctrl+A vẫn là chọn văn bản. Isolate/Hide bị vô hiệu khi chưa chọn đối tượng hoặc đang đọc dữ liệu. Esc bỏ chọn và hủy thao tác đang thực hiện; không thoát Isolate. Ctrl+A hoặc Khôi phục thoát Isolate.

Mọi thao tác trên chỉ tác động workspace 2D; không thay đổi cấu kiện hoặc tải trong ETABS. Đọc lại mô hình xóa trạng thái Hide/Isolate.

## Cài và kiểm tra

Đóng Excel, bỏ chọn XLL QKS cũ, giải nén bản mới và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll` trong Excel Add-ins; chỉ bật một bản QKS.

- Chọn Section cho từng loại cột/dầm/sàn/vách; đối chiếu số và viền chọn trên hình.
- Chọn một Label trùng ở hai tầng: so sánh phạm vi hiện tại/toàn mô hình.
- Thử chọn nhiều giá trị, thêm chọn, bỏ chọn; sau đó Isolate → Hide → Show All.
- Tắt một lớp trong Settings rồi Show All: checkbox lớp và hình vẽ phải cùng bật lại.
- Click phải mở menu, kéo phải/chuột giữa pan; thử Ctrl+I/H/A trong vùng vẽ và Ctrl+A trong ô nhập.

Build WPF và kiểm thử core/VM chạy trong cloud. Chưa nghiệm thu tương tác WPF/Excel/ETABS trực tiếp trên Windows.
