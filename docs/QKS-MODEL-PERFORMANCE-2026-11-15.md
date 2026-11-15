# QKS — mở nhanh Model 2D và Tải gió

Bản nâng cấp tích hợp vào XLL chung của Extension QKS, gồm Model 2D, PDF, tải tường/sàn và giao diện Tải gió hai tab. Không phải chương trình Wind tách rời.

## Thay đổi theo thứ tự triển khai

1. **Tab Diaphragms đọc danh mục nhẹ.** Mở cửa sổ ở Tab 1 chỉ lấy tầng/cao độ và Load Patterns. Bấm Đọc User Loads để lấy bảng của pattern đang chọn. Không quét frame, area, joint và toàn bộ tải cấu kiện khi chỉ dùng Tab 1. Bảng nâng cao Story / Load Sets vẫn đọc schema và kiểm tra diaphragm khi người dùng mở nó.
2. **Cache dùng chung trong phiên Excel.** Đóng/mở lại Model 2D hoặc chuyển sang tab dầm biên có thể tái sử dụng snapshot đã đọc. Nếu Model 2D đã đọc đủ các nhóm tải được hỗ trợ, Wind dùng lại snapshot đó. Các lần mở đồng thời chia sẻ tác vụ đọc mô hình. Chỉ lưu DTO dữ liệu; không lưu control WPF hoặc đối tượng COM vào cache. Fingerprint và chỉ mục cao độ được tái sử dụng theo snapshot bất biến, dùng tham chiếu yếu để không giữ lại mô hình cũ.
3. **Đọc theo lô, có đường dự phòng.** Tọa độ dùng GetAllPoints; tải frame distributed/point và area uniform/uniform-to-frame dùng nhóm ALL. Joint giữ cơ chế đọc theo nhóm có sẵn. Nếu host không hỗ trợ, trả mảng lỗi hoặc ID ngoài danh sách, bỏ toàn bộ batch đó và đọc từng đối tượng. Không cộng trùng phần dữ liệu batch lỗi. SAFE tiếp tục qua SDK của phiên bản đang chạy và proxy chỉ đọc.
4. **Lập chỉ mục cao độ.** Plan và Elevation cô lập tầng chỉ đưa các cấu kiện có khoảng Z liên quan vào bước chiếu hình. Chỉ mục dựa trên hình học, có mở rộng cho tiết diện Extrude; không lọc đơn thuần theo tên Story, nên không bỏ sót cột/vách xuyên tầng hoặc dầm nghiêng. Elevation xem toàn nhà vẫn xét toàn mô hình.
5. **Đồng bộ và đo thời gian.** Đọc lại luôn bỏ cache và đọc CSI. Các đường ghi tải QKS làm hết hiệu lực cache trước khi gọi setter. Trạng thái và tooltip mô hình cho biết dữ liệu được đọc mới hay lấy từ phiên, cùng thời điểm đọc. Log riêng thời gian lấy dữ liệu và thiết lập/chiếu hình ở `%LOCALAPPDATA%\QKS\Logs\model-read.log` (tối đa khoảng 1 MB; không ghi tên file mô hình hoặc giá trị tải).

## Cách sử dụng

- Mở Tải gió → Tab 1 → chọn Load Pattern → Đọc User Loads. Có thể nhập/dán Excel và lưu bộ soạn mà chưa cần đọc hình học.
- Chuyển Tab 2 lần đầu để lấy hình học và tải cho dầm biên. Bảng Tab 1 đang soạn, ánh xạ Story và giá trị chỉnh sửa được giữ lại.
- Khi đóng/mở lại cửa sổ trong cùng phiên Excel, QKS dùng cache nếu phiên kết nối, model, phiên bản adapter và đơn vị vẫn phù hợp.
- Khi đã chỉnh trong ETABS/SAFE, bấm **Đọc lại** trước khi xem/xuất hồ sơ mới. Đọc lại trong Tab 1 lấy lại danh mục; trong Tab 2 lấy lại cả mô hình dầm. Model 2D đọc lại toàn bộ phạm vi đang được hỗ trợ.
- Nếu lần đọc mới lỗi hoặc bị hủy, Model 2D giữ hình đã đọc trước để không mất vùng xem; thông báo lỗi không có nghĩa hình cũ đã được đồng bộ.
- Giữ cấu hình tab cuối cùng: nếu lần trước đóng Wind ở Tab 2, lần mở sau sẽ chuẩn bị Tab 2. Muốn chỉ đọc danh mục nhẹ, đóng cửa sổ ở Tab 1.

## Phạm vi dữ liệu và kiểm tra trước khi gán

Snapshot phân biệt `CatalogOnly` với mô hình có hình học, và `LoadBaselineComplete` cho các nhóm tải adapter hỗ trợ. Không coi nhóm tải chưa đọc là 0. Cache của viewer có cảnh báo thiếu tải không được tự dùng làm baseline gán dầm.

Bộ gió soạn từ danh mục dùng fingerprint riêng cho identity, phiên bản, tầng/cao độ và patterns. Đây không phải fingerprint hình học. Trước khi ghi User Loads, QKS vẫn đọc **mô hình đầy đủ trực tiếp từ CSI**, kiểm tra pattern, tầng, tải trực tiếp đang có, bảng User Loads mới nhất, backup và đọc lại kết quả. Bảng Story / Load Sets còn xác minh lại gán diaphragm thực tế. Bộ gán dầm vẫn đối chiếu fingerprint đầy đủ và hình học đích. Các bước Apply không lấy baseline mới từ cache.

Bộ gió chỉ chứa Tab 1 mới không cần đọc hình học khi mở file. Bộ cũ hoặc bộ có dữ liệu dầm cần mô hình đầy đủ để đối chiếu fingerprint.

## Kiểm chứng và giới hạn

- Kiểm thử tự động: cache reuse và phạm vi, hai cửa sổ dùng chung read, hủy follower, kết nối/đơn vị đổi, invalidation khi read chưa xong, retry khi lỗi, thiếu tải, giữ bản soạn khi đổi tab, import/restore Tab 1, kiểm tra xung đột trước Apply, bulk transaction và đối chiếu chỉ mục với phép chiếu toàn bộ.
- Mô hình giả lập 50.000 dầm trên 50 tầng: query một tầng đưa 1.000 cấu kiện vào bước chiếu. Thời gian cụ thể nằm trong `verification/tests.log`. Đây là phép đo .NET phần lõi, không phải benchmark COM, WPF hoặc máy người dùng.
- Chưa chạy nghiệm thu trực tiếp Excel/ETABS/SAFE trên Windows. Không cam kết thời gian dưới 1 giây cho lần đọc đầu hoặc cho mọi mô hình.
- Cache chỉ sống trong tiến trình Excel, không lưu mô hình lên đĩa. Tắt Excel, đổi kết nối/model hoặc đổi đơn vị sẽ cần đọc lại.
- PID, đường dẫn và số cấu kiện không chứng minh model chưa sửa. Chưa có revision toàn cục đáng tin cậy từ CSI để tự nhận biết mọi thay đổi trong file đang mở; thao tác sửa ngoài QKS cần Đọc lại.
- Lần đọc hình học đầu vẫn lấy metadata/tiết diện theo cấu kiện khi SDK không có đường bulk phù hợp. Chưa tách các nút đồng bộ riêng hình học/từng nhóm tải; nút Đọc lại hiện tại ưu tiên baseline nhất quán.
- Các cuộc gọi COM vẫn chạy trên luồng STA, có cổng tuần tự cho tác vụ đọc. Khi có read khác đang chạy, tác vụ cần API có thể phải chờ; không chạy COM song song bằng Task.Run.

## Nghiệm thu trên Windows

1. So sánh Tab 1 với bản cũ: khi mở từ Tab 1 không xuất hiện bộ đếm đọc frame/area; đọc User Loads đúng pattern.
2. Mở Model 2D lần đầu, đóng/mở lại rồi mở Wind Tab 2. Kiểm tra trạng thái cache, số cấu kiện, tải và log thời gian.
3. Sửa tải trong ETABS; bấm Đọc lại và đối chiếu bảng/nhãn/PDF. Gán tải từ QKS; cửa sổ khác phải yêu cầu đồng bộ.
4. Kiểm tra SAFE v18–v23 trên các máy có SDK tương ứng: bulk hoặc fallback, đúng đơn vị và không âm thầm bỏ tải.
5. So sánh frame point/distributed, area uniform/uniform-to-frame, Uniform Load Sets và joint trước/sau tối ưu; giữ nguyên dấu, chiều dài đoạn tải và load step.
6. Kiểm tra cột/vách xuyên tầng, dầm nghiêng, Extrude, mặt cắt cô lập tầng; không mất cấu kiện tại ranh tầng.
7. Trên model kiểm thử, kiểm tra cả hai cách gán gió, backup và read-back; sửa User Loads hoặc gán diaphragm từ ETABS sau preview để xác nhận trường hợp xung đột bị chặn.
