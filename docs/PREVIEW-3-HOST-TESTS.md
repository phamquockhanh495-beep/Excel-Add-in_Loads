# Nghiệm thu Preview 3 trên Windows — chưa thực hiện trong cloud

Các mục dưới là test cần chạy, **không phải kết quả đã đạt**. Core/VM và build log nằm trong gói tải. Bắt đầu bằng bản sao model đã lưu; giữ cả backup và journal sau mỗi thử nghiệm.

| Nhóm | Tình huống | Kết quả cần đối chiếu |
|---|---|---|
| Cài XLL | Excel desktop Windows x64, chỉ nạp Preview 3 | Ribbon/cửa sổ mở; Material Design và tab hiển thị đúng |
| Chỉ đọc | Mở snapshot/nháp mẫu offline | Không có Apply khả dụng; model thật không đổi |
| Tường | Add, Replace; uniform/trapezoid, đoạn kề, dầm nghiêng | Vị trí I/J theo chiều dài 3D, đúng kN/m và dấu |
| Giữ tải | Cùng pattern có Global Y/Local và distributed moment | Replace chỉ thay hướng/loại đích; các dòng còn lại giữ nguyên |
| Sàn | Add/Replace, nhiều hướng và nhiều pattern | Áp lực đúng kN/m²; không mất hướng/pattern khác |
| Đơn vị | kN-m, N-mm, tonf-m | Giá trị vật lý và đơn vị ban đầu đều được giữ/khôi phục |
| Xung đột | Đổi hình học/tải/pattern/model sau khi tạo nháp hoặc trong hộp review | Dừng trước khi ghi; đọc lại nguồn |
| Lỗi trước ghi | Model khóa, chưa lưu, folder backup/journal không ghi được | Không gán tải; không tự mở khóa |
| Partial failure | Trên model thử, mô phỏng setter lỗi sau Delete/Set đầu | Dừng phần còn lại, journal rõ trạng thái; không thử lại Add tự động |
| Backup | Mở file backup riêng bằng ETABS | Baseline đúng trước gán; hash khớp, model đọc được |
| Gió dầm | Hai phương, dấu âm/dương, dầm cao độ khác nhau | q=p×h đúng; không dùng p làm q hoặc lực tầng |
| User Loads | Wind/User Loads, tối thiểu 2 tầng/diaphragm; code/keys thực | Không đoán key; read-back FX/FY/MZ/X/Y khớp |
| Giữ User Loads | Sửa một hàng trong nhiều hàng, hoặc setter làm thay đổi hàng khác | Giữ toàn bảng hoặc dừng và báo lỗi; không tuyên bố thành công khi mất hàng |
| Gió điểm đặt | Fx/Fy, Mz khác 0, X/Y khác tâm | Kiểm chứng vị trí/đơn vị/mô men qua UI ETABS; không cộng lệch tâm hai lần |
| Nháp | Lưu/mở, mất kết nối, đổi snapshot, ô nhập đang lỗi | Không mất nháp chưa lưu; nháp tách nguồn không được Apply |
| DPI | 100/125/150/200%, nhiều màn hình và taskbar | Nút cửa sổ/footer truy cập được, panel thu/mở được |
| Hiệu năng | Model thực 1k/10k/50k; zoom/pan/chọn/lọc/đọc | Đo COM/read, WPF frame time và bộ nhớ đỉnh riêng; không dùng benchmark core để thay thế |
| SAFE | 18, 19, 20, 21, 22/22.5, SDK và model tương ứng | Chưa mở ghi cho đến khi adapter từng capability có test host đạt |

Ghi ngày, phiên bản Excel/Windows/CSI, DPI, file model thử, bước tái hiện, số đo và đường dẫn backup/journal cho từng kết quả. Không đưa model hoặc dữ liệu dự án thật lên GitHub công khai khi chưa được chủ sở hữu cho phép.
