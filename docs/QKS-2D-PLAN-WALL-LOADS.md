# QKS 2D — Nét tải tường và biên sàn trên Plan

## Thay đổi ngày 28/10/2026

- Tải phân bố đứng trên frame (bao gồm tải tường) hiển thị bằng nét đỏ đô `#800020`, dày 3 DIP trên Plan, đúng vị trí đầu/cuối đoạn tải đã gán. Không dùng chuỗi ký hiệu ⊗/⊙ cho các đoạn này. Không dựa vào tên pattern WL vì người dùng có thể đặt tên khác.
- Giá trị tải vẫn bật/tắt bằng “Hiện giá trị trên hình”; nhãn theo hướng đoạn tải. Độ dài nét là chiều dài đoạn gán tải, không biểu diễn độ lớn hoặc dấu lực; đọc giá trị có dấu ở nhãn/bảng tải. Các đoạn chồng nhau giữ từng bản ghi trong bảng.
- Ẩn nét viền của sàn chưa được chọn trên Plan, giữ màu nền và nhãn sàn. Vẫn chọn được sàn; khi chọn có viền nổi bật. Elevation giữ đường bao để thể hiện chiều dày.
- Tải tập trung, tải ngang, mômen, ký hiệu Joint/Shell và mũi tên trong Elevation giữ cách biểu diễn tương ứng.

## Cài bản mới và kiểm tra

1. Đóng Excel, bỏ chọn bản QKS cũ trong Excel Add-ins. Giải nén ZIP vào thư mục riêng và Browse `Excel/QksExcelAddIn-AddIn64-packed.xll`. Chỉ bật một bản QKS.
2. Kết nối API đúng workbook/model, mở Quản lý tải trọng 2D. Chọn Plan, tầng và load pattern có tải tường; tick Frame.
3. Kiểm tra dầm gán tải một phần chiều dài: nét đỏ đô chỉ phủ đoạn r1–r2 đã gán. Bật/tắt “Hiện giá trị trên hình” để kiểm tra nét tải độc lập với chữ.
4. Kiểm tra sàn: không còn viền màu trong Plan; chọn sàn vẫn có viền, nhấn Esc bỏ chọn. Dầm/cột/vách vẫn hiển thị đường bao.
5. Chuyển Elevation: kiểm tra mũi tên tải và chiều dày sàn vẫn hiển thị.

Build/test trong cloud không thay thế nghiệm thu WPF trên Excel/ETABS Windows. Đây là thay đổi biểu diễn; không sửa dữ liệu tải hoặc mô hình ETABS.
