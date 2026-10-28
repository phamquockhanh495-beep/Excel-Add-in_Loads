# TÀI LIỆU KỸ THUẬT TOÀN DIỆN: TỔNG HỢP THUẬT TOÁN TÍNH TOÁN, KIỂM TRA ỔN ĐỊNH CÔNG TRÌNH, ĐIỀU KIỆN GIỚI HẠN & TIÊU CHUẨN TRÍCH DẪN (CES.LIB STABILITY BUILDING COMPENDIUM)

> **Mã tài liệu:** `CES-DOC-STABILITY-2026-REV1`  
> **Hệ thống phần mềm:** Thư viện Kiến thức & Tiện ích Tính toán Kết cấu Xây dựng — CES.Lib  
> **Căn cứ Pháp lý & Tiêu chuẩn viện dẫn cốt lõi:**  
> - **TCVN 2737:2023**: *Tải trọng và tác động — Tiêu chuẩn thiết kế (Điều 6: Các tổ hợp tải trọng cơ bản ULS & tiêu chuẩn SLS; Điều 10 & Phụ lục G: Tác động của tải trọng gió tĩnh và gió động; Điều 7.3: Hệ số giảm tải giữ ổn định $\gamma_f = 0.90$)*.  
> - **TCVN 5574:2018**: *Thiết kế kết cấu bê tông và bê tông cốt thép (Mục 8.2: Tính toán theo trạng thái giới hạn thứ hai SLS — Độ võng, Chuyển vị và Nứt; Mục 8.1.2: Hiệu ứng uốn dọc cấu kiện nén lệch tâm)*.  
> - **TCVN 9386-1:2025**: *Thiết kế công trình chịu động đất — Phần 1: Quy định chung, tác động động đất và kết cấu nhà (Mục 4.3.1: Độ cứng hiệu dụng nứt 50% EI; Mục 4.4.2.2: Hiệu ứng bậc hai P-Δ & Công thức $P_{tot}$; Mục 4.4.3.2: Chuyển vị lệch tầng; Mục 5.4.3.2 & 5.4.3.4: Lực dọc quy đổi cột/vách $\nu_d$)*.  
> - **TCVN 9386-5:2025**: *Thiết kế công trình chịu động đất — Phần 5: Móng, kết cấu chắn giữ và địa kỹ thuật (Mục 5: Ổn định lật, trượt và độ lệch tâm móng)*.  
> - **TCXD 198:1997**: *Nhà cao tầng bê tông cốt thép — Thiết kế kết cấu (Mục 3.2: Độ cứng tổng thể, Giới hạn chuyển vị đỉnh $[u] = H/500$ & Gia tốc đỉnh $a \le 150\text{ mm/s}^2$)*.  
> - **Tham chiếu Quốc tế**: **EN 1991-1-4 / ISO 6897** (Tiện nghi dao động gió), **EN 1998-1:2004 / Eurocode 8** (Kháng chấn), **ASCE/SEI 7-16 / 7-22** (Tải trọng gió & Phân tích ổn định xoắn).  

---

## MỤC LỤC TỔNG QUAN

1. [Ma trận Quyết định Tổng thể: 9 Bài toán Kiểm tra Ổn định Nhà Cao tầng](#1-ma-trận-quyết-định-tổng-thể-9-bài-toán-kiểm-tra-ổn-định-nhà-cao-tầng)
2. [Cơ sở Cơ học: Phân định Mô hình Độ cứng 100% EI vs 50% EI](#2-cơ-sở-cơ-học-phân-định-mô-hình-độ-cứng-100-ei-vs-50-ei)
3. [Quy tắc Tổ hợp Tải trọng: Tổ hợp Tuyến tính (Linear) vs Tổ hợp Bao (Envelope)](#3-quy-tắc-tổ-hợp-tải-trọng-tổ-hợp-tuyến-tính-linear-vs-tổ-hợp-bao-envelope)
4. [Phương pháp Xác định Tải trọng Trọng lực Tích lũy $P_{tot}$ theo TCVN](#4-phương-pháp-xác-định-tải-trọng-trọng-lực-tích-lũy-p_tot-theo-tcvn)
5. [Bài toán 1: Kiểm tra Gia tốc Đỉnh do Gió (Wind Top Acceleration)](#5-bài-toán-1-kiểm-tra-gia-tốc-đỉnh-do-gió-wind-top-acceleration)
6. [Bài toán 2: Kiểm tra Chuyển vị Đỉnh do Gió (Wind Top Displacement — SLS)](#6-bài-toán-2-kiểm-tra-chuyển-vị-đỉnh-do-gió-wind-top-displacement--sls)
7. [Bài toán 3: Kiểm tra Chuyển vị Đỉnh do Động đất & Khe Phòng chấn (Seismic Top Displacement)](#7-bài-toán-3-kiểm-tra-chuyển-vị-đỉnh-do-động-đất--khe-phòng-chấn-seismic-top-displacement)
8. [Bài toán 4: Kiểm tra Chuyển vị Ngang Liên tầng do Gió (Wind Inter-story Drift — SLS)](#8-bài-toán-4-kiểm-tra-chuyển-vị-ngang-liên-tầng-do-gió-wind-inter-story-drift--sls)
9. [Bài toán 5: Kiểm tra Chuyển vị Ngang Liên tầng do Động đất (Seismic Inter-story Drift — DLS)](#9-bài-toán-5-kiểm-tra-chuyển-vị-ngang-liên-tầng-do-động-đất-seismic-inter-story-drift--dls)
10. [Bài toán 6: Kiểm tra Độ nhạy Bất đối xứng Xoắn Mặt bằng (Torsional Irregularity Ratio)](#10-bài-toán-6-kiểm-tra-độ-nhạy-bất-đối-xứng-xoắn-mặt-bằng-torsional-irregularity-ratio)
11. [Bài toán 7: Kiểm tra Khả năng Chống lật Móng Toàn thể (Global Overturning Stability — EQU)](#11-bài-toán-7-kiểm-tra-khả-năng-chống-lật-móng-toàn-thể-global-overturning-stability--equ)
12. [Bài toán 8: Kiểm soát Lực dọc Quy đổi Cột Khung & Vách Cứng ($\nu_d$ Capacity Design)](#12-bài-toán-8-kiểm-soát-lực-dọc-quy-đổi-cột-khung--vách-cứng-nu_d-capacity-design)
13. [Bài toán 9: Kiểm tra Hiệu ứng Bậc hai $P-\Delta$ Toàn tầng & $P-\delta$ Cột Cục bộ](#13-bài-toán-9-kiểm-tra-hiệu-ứng-bậc-hai-p-delta-toàn-tầng--p-delta-cột-cục-bộ)
14. [Bảng Tra cứu Đối chiếu Nhanh Toàn bộ Công thức, Ngưỡng & Hàm Code CES.Lib](#14-bảng-tra-cứu-đối-chiếu-nhanh-toàn-bộ-công-thức-ngưỡng--hàm-code-ceslib)

---

## 1. MA TRẬN QUYẾT ĐỊNH TỔNG THỂ: 9 BÀI TOÁN KIỂM TRA ỔN ĐỊNH NHÀ CAO TẦNG

Trong thuyết minh tính toán kết cấu nhà cao tầng theo hệ thống Tiêu chuẩn Xây dựng Việt Nam (TCVN), 9 bài toán kiểm tra ổn định tổng thể được chuẩn hóa ma trận kỹ thuật như sau:

| STT | Bài toán Kiểm tra | Căn cứ Tiêu chuẩn | Mục TMTT | Mô hình Độ cứng | Dạng Tổ hợp | Công thức Tổ hợp Tải trọng Điển hình | Tiêu chí Đánh giá & Hệ số An toàn ($FS$) |
| :---: | :--- | :--- | :---: | :---: | :---: | :--- | :--- |
| **1** | **Gia tốc đỉnh do gió**<br>*(Wind Top Acceleration)* | TCVN 2737:2023<br>TCXD 198:1997<br>ISO 6897 / EN | Gia tốc đỉnh | **100% Gross EI** | Modal / Dynamic | Trích xuất Mode 1 ($T_{1X}$) và Mode 2 ($T_{1Y}$) do gió động $W_{dyn}$ | $$a_{max} \le 150\text{ mm/s}^2$$<br>($FS = \dfrac{150}{a_{max}} \ge 1.0$) |
| **2** | **Chuyển vị đỉnh do gió**<br>*(Wind Top Displacement)* | TCVN 2737:2023<br>TCVN 5574:2018<br>TCXD 198:1997 | Chuyển vị đỉnh gió | **100% Gross EI** | **Linear**<br>*(SLS)* | `1.0 DEAD + 1.0 SDL + 0.7 LIVE ± 1.0 W_tc` | $$U = \sqrt{UX^2 + UY^2} \le \dfrac{H}{500}$$<br>($FS = \dfrac{[u]}{U} \ge 1.0$) |
| **3** | **Chuyển vị đỉnh Động đất & Khe lún/chấn**<br>*(Seismic Displacement)* | TCVN 9386-1:2025<br>(Mục 4.3.4 & 4.4.2) | Chuyển vị đỉnh ĐĐ | **50% Cracked EI** | **Envelope**<br>*(Seismic)* | `1.0 DEAD + 1.0 SDL + 0.3 LIVE ± 1.0 EX ± 0.3 EY` | $$d_s = q \cdot d_e$$<br>Khe phòng chấn: $\delta \ge \sqrt{d_{s1}^2 + d_{s2}^2}$ |
| **4** | **Chuyển vị liên tầng do gió**<br>*(Wind Inter-story Drift)* | TCVN 2737:2023<br>(Phụ lục G.2.5)<br>TCVN 5574:2018 | Chuyển vị liên tầng gió | **100% Gross EI** | **Linear**<br>*(SLS)* | `1.0 DEAD + 1.0 SDL + 0.7 LIVE ± 1.0 W_tc` | $$\dfrac{d_r}{h_s} = \dfrac{\Delta_i - \Delta_{i-1}}{h_s} \le \dfrac{1}{500}$$<br>($FS = \dfrac{[d_r/h_s]}{d_r/h_s} \ge 1.0$) |
| **5** | **Chuyển vị liên tầng Động đất**<br>*(Seismic Inter-story Drift)* | TCVN 9386-1:2025<br>(Mục 4.4.3.2) | Chuyển vị liên tầng ĐĐ | **50% Cracked EI** | **Envelope**<br>*(Seismic DLS)* | `1.0 DEAD + 1.0 SDL + 0.3 LIVE ± 1.0 EX ± 0.3 EY` | $$d_r \cdot \nu \le C_{limit} \cdot h_s$$<br>($0.005 h_s$ cho tường giòn; $0.0075 h_s$ cho tường dẻo) |
| **6** | **Khả năng Chống lật móng**<br>*(Overturning Stability)* | TCVN 2737:2023 (Đ.7.3)<br>TCVN 9386-5:2025<br>TCVN 5574:2018 | Chống lật móng | **100% Gross EI** | **Linear**<br>*(EQU / SLS / ULS)* | `0.9 DEAD + 0.9 SDL ± 1.0 W_tc` (SLS)<br>`0.9 DEAD + 0.9 SDL ± 1.3 W` (ULS)<br>*(Lực giữ $P_{tot,st} = 0.9\sum G$, Hoạt tải = 0)* | $$FS = \dfrac{M_{CL}}{M_L} \ge 1.50\text{ (Gió)}$$<br>hoặc $\ge 1.20 \div 1.30\text{ (ĐĐ)}$ |
| **7** | **Lực dọc quy đổi Cột khung**<br>*(Axial Ratio - Column)* | TCVN 9386-1:2025<br>(Mục 5.4.3.2.1) | $\nu_d$ Cột | **50% Cracked EI** | **Envelope**<br>*(Seismic ULS)* | Bao tổ hợp Động đất ULS:<br>`1.0 DEAD + 1.0 SDL + 0.3 LIVE ± 1.0 EX ± 0.3 EY` | $$\nu_d = \dfrac{N_{Ed}}{A_c \cdot f_{cd}} \le \nu_{d,max}$$<br>($\le 0.65\text{ DCL}; \le 0.55\text{ DCM}; \le 0.45\text{ DCH}$) |
| **8** | **Lực dọc quy đổi Vách cứng**<br>*(Axial Ratio - Pier)* | TCVN 9386-1:2025<br>(Mục 5.4.3.4.1) | $\nu_d$ Vách | **50% Cracked EI** | **Envelope**<br>*(Seismic ULS)* | Bao tổ hợp Động đất ULS:<br>`1.0 DEAD + 1.0 SDL + 0.3 LIVE ± 1.0 EX ± 0.3 EY` | $$\nu_d = \dfrac{N_{Ed}}{A_c \cdot f_{cd}} \le \nu_{d,max}$$<br>($\le 0.35$ vùng chân vách $h_{cr}$; $\le 0.40$ ngoài $h_{cr}$) |
| **9** | **Hiệu ứng Bậc hai P-Delta**<br>*(Second-order Stability $\theta$)* | TCVN 9386-1:2025<br>(Mục 4.4.2.2) | P-Delta | **50% Cracked EI** | **Envelope**<br>*(Seismic P-Δ)* | Tải đứng tích lũy $P_{tot}$:<br>`1.0 DEAD + 1.0 SDL + 0.3 LIVE`<br>kết hợp chuyển vị dạt tầng $d_r$ | $$\theta = \dfrac{P_{tot} \cdot d_r}{V_{tot} \cdot h_s} \le 0.10$$<br>($FS = \dfrac{0.10}{\theta} \ge 1.0$) |

---

## 2. CƠ SỞ CƠ HỌC: PHÂN ĐỊNH MÔ HÌNH ĐỘ CỨNG 100% EI VS 50% EI

Sự phân định rạch ròi giữa **Mô hình độ cứng tiết diện nguyên 100% EI (Gross Elastic Stiffness)** và **Mô hình độ cứng tiết diện nứt 50% EI (Cracked Stiffness)** là một trong những nguyên lý căn bản nhất của cơ học công trình hiện đại:

```
                  ┌──────────────────────────────────────────────────┐
                  │    PHÂN ĐỊNH MÔ HÌNH ĐỘ CỨNG KẾT CẤU BÊ TÔNG     │
                  └─────────────────────────┬────────────────────────┘
                                            │
                    ┌───────────────────────┴───────────────────────┐
                    ▼                                               ▼
     ┌─────────────────────────────┐                 ┌─────────────────────────────┐
     │      MODEL 100% GROSS EI    │                 │      MODEL 50% CRACKED EI   │
     │      (Tiết diện nguyên)     │                 │       (Tiết diện nứt)       │
     └──────────────┬──────────────┘                 └──────────────┬──────────────┘
                    │                                               │
     • Gia tốc đỉnh do gió                           • Chuyển vị đỉnh Động đất
     • Chuyển vị đỉnh do gió (SLS)                   • Chuyển vị liên tầng Động đất (dr)
     • Chuyển vị liên tầng do gió (SLS)              • Lực dọc quy đổi Cột (ν_d Frame)
     • Khả năng Chống lật móng (EQU)                 • Lực dọc quy đổi Vách (ν_d Pier)
     • Kiểm tra Chu kỳ gió T_1x, T_1y                • Hiệu ứng P-Delta địa chấn (θ)
```

### 2.1. Nhóm bài toán sử dụng Mô hình 100% EI (Gross Elastic Section)
1. **Cơ sở vật lý:**
   - Tải trọng gió tác động ngẫu nhiên theo xung động ngắn hạn. Dưới tác dụng của tải trọng gió tiêu chuẩn (SLS - chu kỳ lặp 20 đến 50 năm), ứng suất uốn kéo trong cấu kiện bê tông cốt thép nói chung thấp hơn hoặc xấp xỉ cường độ chịu kéo tính toán của bê tông ($R_{bt}$).
   - Vết nứt uốn chưa xuất hiện hoặc chỉ là các vi nứt bề mặt không làm suy giảm ma trận độ cứng tổng thể của tòa nhà. Kết cấu làm việc hoàn toàn trong miền đàn hồi tuyến tính.
2. **Quy định tiêu chuẩn viện dẫn:**
   - **TCXD 198:1997 (Mục 3.2)**, **TCVN 5574:2018 (Phụ lục M)** và **ASCE 7 (Mục C12.8)** đều chỉ định kiểm tra độ cứng chống uốn tổng thể của nhà cao tầng trên mô hình tiết diện nguyên chưa nứt ($I_{gross}$).

### 2.2. Nhóm bài toán bắt buộc sử dụng Mô hình 50% EI (Cracked Section)
1. **Quy định pháp lý bắt buộc theo TCVN 9386-1:2025 (Điều 4.3.1):**
   > *"Trừ khi có phân tích chính xác hơn về tiết diện bị nứt, các đặc trưng độ cứng uốn và độ cứng cắt của các cấu kiện BTCT phải lấy bằng **50% độ cứng của tiết diện không nứt** ($0.5 E_c I_g$ và $0.5 G A_c$)."*
2. **Cơ sở vật lý & Cơ chế phá hoại:**
   - Chấn động động đất truyền năng lượng sóng địa chấn theo chu kỳ biến dạng đổi chiều lớn, kích hoạt các khớp dẻo ở dầm và chân vách. Bê tông bị nứt đáng kể và mất ít nhất một nửa độ cứng đàn hồi ban đầu.
3. **Hậu quả nghiêm trọng nếu dùng sai Model 100% EI cho Động đất:**
   - **Chu kỳ dao động $T$ bị báo ngắn lại:** Độ cứng bị phóng đại gấp đôi làm $T$ giả tạo nhỏ đi, dẫn đến điểm rơi trên phổ phản ứng thiết kế $S_d(T)$ bị sai lệch hoàn toàn.
   - **Chuyển vị dạt tầng đàn hồi $d_e$ bị tính thiếu từ $40\% \div 50\%$:** Dẫn đến tính toán sai khe phòng chấn giữa 2 tòa tháp, gây nguy cơ va chạm sập đổ tòa tháp (Pounding Effect).
   - **Hệ số độ nhạy P-Delta $\theta$ bị báo ảo $< 0.10$:** Kỹ sư bỏ qua không chạy phân tích P-Delta bậc hai, trong khi thực tế trên mô hình nứt thì $\theta > 0.20$ bắt buộc phải tính toán khuếch đại!

---

## 3. QUY TẮC TỔ HỢP TẢI TRỌNG: TỔ HỢP TUYẾN TÍNH (LINEAR) VS TỔ HỢP BAO (ENVELOPE)

### 3.1. Tổ hợp Tuyến tính Đơn lẻ (Linear Add Combinations)
- **Bản chất toán học:** Là phương trình đại số cộng véc-tơ các trường hợp tải trọng riêng rẽ theo từng phương không gian xác định:
  $$Comb_k = 1.0\text{ DEAD} + 1.0\text{ SDL} + 0.7\text{ LIVE} + 1.0\text{ WX+}$$
- **Lý do bắt buộc dùng cho Gió & Chống lật:**
  - Tải trọng gió có hướng không gian xác định ($+X, -X, +Y, -Y$). Khi kiểm tra chuyển vị đỉnh hay dạt tầng do gió, ta cần xem xét chuyển vị tịnh tiến tương ứng dọc theo đúng phương gió thổi ($UX$ do $WX$, $UY$ do $WY$).
  - Đối với bài toán chống lật, mômen gây lật quanh một trục mép móng nhất định phải được đối trọng bởi đúng phần tĩnh tải trọng lực $0.9G$ phân bố tương ứng. Nếu gom vào tổ hợp bao Envelope sẽ làm mất phương hướng của véc-tơ mômen và gây sai lệch bản chất tĩnh học.

### 3.2. Tổ hợp Bao Cực đại (Envelope Combinations)
- **Bản chất toán học:** Không phải là phép cộng đại số, mà là phép quét tìm giá trị lớn nhất tuyệt đối (Max/Min Absolute) từ tập hợp nhiều tổ hợp con:
  $$ENV = \max(|Comb_1|, |Comb_2|, \dots, |Comb_m|)$$
- **Lý do bắt buộc dùng cho Động đất, Lực dọc $\nu_d$ và P-Delta:**
  - Sóng địa chấn kích động ngẫu nhiên 3 chiều không gian. Theo **TCVN 9386-1:2025 (Điều 4.3.3.5)**, tác động động đất được tổ hợp không gian:
    $$E = \pm 1.0 E_X \pm 0.3 E_Y \quad \text{hoặc} \quad E = \pm 0.3 E_X \pm 1.0 E_Y$$
    *(Sinh ra ít nhất 8 đến 16 tổ hợp động đất độc lập)*.
  - Khi kiểm tra an toàn cho một tầng (chuyển vị $d_r$, hệ số $\theta$) hoặc cho một cấu kiện (lực dọc $N_{Ed}$), kỹ sư bắt buộc phải lấy giá trị bao bất lợi nhất để đảm bảo kết cấu an toàn dưới mọi góc kích động của địa chấn.

---

## 4. PHƯƠNG PHÁP XÁC ĐỊNH TẢI TRỌNG TRỌNG LỰC TÍCH LŨY $P_{tot}$ THEO TCVN

Trong các công thức kiểm tra ổn định tổng thể — đặc biệt là **Hiệu ứng bậc hai P-Delta ($\theta$)** và **Kiểm tra chống lật móng** — thông số **$P_{tot}$** đóng vai trò quyết định mức độ an toàn.

### 4.1. Định nghĩa Cơ học & Bản chất Vật lý của $P_{tot}$
Tại cao trình sàn tầng $i$ đang xét, **$P_{tot,i}$ (Total Accumulated Gravity Load)** là tổng toàn bộ tải trọng trọng lực thẳng đứng tích lũy của tất cả các tầng nằm ở phía trên tầng đó dồn xuống (tính từ sàn tầng $i$ dồn lên đến đỉnh tầng mái $N_{story}$):

$$P_{tot,i} = \sum_{j=i}^{N_{story}} P_j \quad (\text{kN})$$

- **Ý nghĩa trong bài toán P-Delta ($\theta$):** Khi tầng $i$ bị uốn dạt ngang một đoạn lệch tầng tương đối $d_{r,i}$ dưới tác dụng của lực cắt địa chấn $V_{tot,i}$, toàn bộ tải trọng đứng $P_{tot,i}$ của các tầng trên sẽ bị lệch tâm một đoạn đúng bằng $d_{r,i}$ so với chân tầng, sinh ra mômen lật thứ cấp toàn tầng:
  $$\Delta M_i = P_{tot,i} \cdot d_{r,i} \quad (\text{kNm})$$
- **Ý nghĩa trong bài toán Chống lật móng:** Tại cao trình mặt cắt chân móng ($z = 0$), $P_{tot,base}$ chính là **Tổng trọng lượng toàn bộ tòa nhà** ($G_{tot}$).

### 4.2. Công thức Tổ hợp Xác định $P_{tot}$ theo Từng Tiêu chuẩn

| Bài toán Áp dụng | Căn cứ Tiêu chuẩn | Công thức Tổ hợp Xác định $P_{tot}$ | Hệ số Thành phần Chi tiết |
| :--- | :--- | :--- | :--- |
| **P-Delta Địa chấn ($\theta$) & Kháng chấn** | **TCVN 9386-1:2025**<br>(Điều 4.4.2.2 & 3.2.4) | $$P_{tot} = \sum G_{k,j} + \sum \psi_{Ei} \cdot Q_{k,j}$$ | • $\gamma_G = 1.0$ cho Tĩnh tải ($DEAD + SDL$)<br>• $\psi_{Ei} = 0.30$ cho Nhà ở, Chung cư, Văn phòng<br>• $\psi_{Ei} = 0.80$ cho Sàn Kho tàng, Thư viện, Server<br>• $\psi_{Ei} = 0$ cho Mái không sử dụng |
| **P-Delta do Gió (SLS)** | **TCVN 2737:2023**<br>(Điều 6.4) & TCVN 5574 | $$P_{tot} = \sum G_{k,j} + \sum \psi_0 \cdot Q_{k,j}$$ | • $\gamma_G = 1.0$ cho Tĩnh tải<br>• $\psi_0 = 0.70$ cho Hoạt tải dài hạn + ngắn hạn<br>• Tổ hợp: `1.0 DEAD + 1.0 SDL + 0.7 LIVE` |
| **P-Delta do Gió (ULS)** | **TCVN 2737:2023**<br>(Điều 6.3) & TCVN 5574 | $$P_{tot} = \sum \gamma_G G_k + \sum \gamma_Q \psi Q_k$$ | • $\gamma_G = 1.20 \div 1.30$ cho Tĩnh tải<br>• $\gamma_Q \cdot \psi = 1.20 \times 0.50 = 0.60$ cho Hoạt tải<br>• Tổ hợp: `1.2 DEAD + 1.2 SDL + 0.6 LIVE` |
| **Chống lật Móng (Overturning)** | **TCVN 9386-5:2025**<br>& TCVN 2737 (Điều 7.3) | $$P_{tot,st} = 0.90 \cdot \sum G_{k,j}$$ | • $\gamma_G = 0.90$ giảm tải giữ ổn định an toàn<br>• **Hoạt tải $LIVE = 0$** (Tuyệt đối không tính hoạt tải vào lực giữ chống lật) |

### 4.3. Quy trình Trích xuất $P_{tot}$ từ Phần mềm ETABS
1. **Bước 1:** Định nghĩa tổ hợp tải trọng đứng `COMB_PTOT_SEISMIC`: `Linear Add`, `DEAD = 1.0`, `SDL = 1.0`, `LIVE = 0.3`.
2. **Bước 2:** Nhấn **`Ctrl + T`** $\to$ `Analysis Results` $\to$ `Structure Results` $\to$ Chọn bảng **`Story Forces`**.
3. **Bước 3:** Lọc `Case/Combo = COMB_PTOT_SEISMIC` và lọc **`Location = Bottom`** *(Bắt buộc chọn Bottom vì mặt cắt chân tầng mới là nơi tiếp nhận toàn bộ tải trọng của tầng đó cộng dồn với tất cả các tầng phía trên)*.
4. **Bước 4:** Đọc giá trị tại cột **`P` (kN)** — đây chính là đại lượng $P_{tot}$ chuẩn xác của tầng đang xét!

---

## 5. BÀI TOÁN 1: KIỂM TRA GIA TỐC ĐỈNH DO GIÓ (WIND TOP ACCELERATION)

### 5.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Đảm bảo mức độ tiện nghi sinh hoạt và làm việc của con người (Human Comfort / Motion Sickness), chống say sóng, lo âu cho cư dân ở các tầng cao nhất dưới tác dụng của thành phần động của tải trọng gió.
- **Tiêu chuẩn viện dẫn:** **TCXD 198:1997 (Mục 3.2.4)**, **TCVN 2737:2023**, tham chiếu **ISO 6897:1984** và **EN 1991-1-4 (Phụ lục B & C)**.
- **Mô hình độ cứng:** **100% Gross EI**.
- **Dạng phân tích:** Phân tích dao động riêng (Modal Analysis) kết hợp tải trọng gió động $W_{dyn}$.

### 5.2. Công thức KaTeX Tính toán Gia tốc Đỉnh
Gia tốc dao động đỉnh công trình tại sàn tầng mái ứng với dạng dao động riêng thứ nhất theo mỗi phương được xác định theo phương trình cơ học kết cấu:

$$a_x = \Delta_x \cdot \left(\frac{2\pi}{T_{1x}}\right)^2 = \Delta_x \cdot \omega_{1x}^2 \quad (\text{mm/s}^2)$$

$$a_y = \Delta_y \cdot \left(\frac{2\pi}{T_{1y}}\right)^2 = \Delta_y \cdot \omega_{1y}^2 \quad (\text{mm/s}^2)$$

$$a_{max} = \max(a_x, a_y) \quad (\text{mm/s}^2)$$

*Trong đó:*
- $\Delta_x, \Delta_y$ ($\text{mm}$): Chuyển vị đỉnh tầng mái do thành phần động của tải trọng gió gây ra theo phương X và phương Y (trích xuất từ ETABS ứng với trường hợp tải gió động $WX_{dyn}, WY_{dyn}$).
- $T_{1x}, T_{1y}$ ($\text{giây}$): Chu kỳ dao động riêng thứ nhất tịnh tiến theo phương X và phương Y (trích xuất từ bảng `Modal Periods and Frequencies` trong ETABS).
- $\omega = \dfrac{2\pi}{T}$ ($\text{rad/s}$): Tần số góc dao động tự nhiên tương ứng.

### 5.3. Điều kiện Giới hạn & Hệ số An toàn ($FS$)
Theo quy định tại **TCXD 198:1997 (Mục 3.2.4)**:

$$a_{max} = \max(a_x, a_y) \le a_{lim} = 150 \text{ mm/s}^2 \quad (0.15 \text{ m/s}^2)$$

Hệ số an toàn:

$$FS_{accel} = \frac{a_{lim}}{a_{max}} = \frac{150}{a_{max}} \ge 1.00$$

*Bảng phân cấp mức độ phản ứng sinh lý của con người theo ISO 6897 / TCXD 198:*
- $a < 50\text{ mm/s}^2$: Con người hoàn toàn không cảm nhận được chuyển động uốn lắc.
- $50 \le a \le 150\text{ mm/s}^2$: Dao động nhẹ; sinh hoạt, làm việc bình thường (Ngưỡng thiết kế cho phép nhà ở và văn phòng cao cấp).
- $150 < a \le 500\text{ mm/s}^2$: Rung lắc rõ rệt; gây khó chịu, say tàu xe nếu kéo dài nhiều giờ trong bão lớn.
- $a > 500\text{ mm/s}^2$: Đi lại khó khăn, mất thăng bằng, hoảng sợ.

---

## 6. BÀI TOÁN 2: KIỂM TRA CHUYỂN VỊ ĐỈNH DO GIÓ (WIND TOP DISPLACEMENT — SLS)

### 6.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Kiểm soát độ cứng chống uốn tổng thể của tòa nhà; tránh biến dạng hình học quá mức gây rạn nứt tường xây bao che, kẹt nẹp kính mặt dựng Curtain Wall, hư hỏng hệ thống ống kỹ thuật và kẹt ray thang máy.
- **Tiêu chuẩn viện dẫn:** **TCXD 198:1997 (Mục 3.2.2)**, **TCVN 2737:2023 (Điều 6.4 & Phụ lục G)**, **TCVN 5574:2018 (Mục 8.2 & Phụ lục M)**.
- **Mô hình độ cứng:** **100% Gross EI**.
- **Dạng tổ hợp:** Tổ hợp tải trọng tiêu chuẩn **Linear Add (SLS)** với hệ số vượt tải $\gamma_f = 1.0$:
  $$Comb_{SLS,WX} = 1.0\text{ DEAD} + 1.0\text{ SDL} + 0.7\text{ LIVE} \pm 1.0\text{ WX\_tc}$$
  $$Comb_{SLS,WY} = 1.0\text{ DEAD} + 1.0\text{ SDL} + 0.7\text{ LIVE} \pm 1.0\text{ WY\_tc}$$

### 6.2. Công thức KaTeX Tính Véc-tơ Chuyển vị Không gian Tổng hợp
Tại cao trình sàn tầng mái (Story Roof / KTTM), chuyển vị ngang thực tế là hợp lực của 2 phương tịnh tiến vuông góc $UX$ và $UY$:

$$U = \sqrt{UX^2 + UY^2} \quad (\text{mm})$$

$$U_{max} = \max\left( U_{SLS,WX}, \; U_{SLS,WY} \right) \quad (\text{mm})$$

*Vị trí trích xuất:* Bắt buộc trích xuất tại **Tâm khối lượng sàn (Diaphragm Center of Mass - CM)** để đại diện chuẩn xác cho chuyển vị tịnh tiến cứng của toàn bộ khối sàn.

### 6.3. Điều kiện Giới hạn & Hệ số An toàn ($FS$)
Theo quy định tại **TCXD 198:1997 (Bảng 3.2)** & **TCVN 5574:2018**:

$$U_{max} \le [u] = \frac{H}{500}$$

Hệ số an toàn:

$$FS_{disp} = \frac{[u]}{U_{max}} \ge 1.00$$

*Quy chuẩn giới hạn $[u]$ theo giải pháp kết cấu:*
- Hệ Khung - Vách / Lõi BTCT toàn khối: $[u] = \dfrac{H}{500}$.
- Hệ Khung thuần BTCT không vách: $[u] = \dfrac{H}{400} \div \dfrac{H}{500}$.
- Hệ mặt dựng kính Curtain Wall cao cấp (yêu cầu hãng Facade): $[u] = \dfrac{H}{750} \div \dfrac{H}{1000}$.
- Kết cấu thép cao tầng: $[u] = \dfrac{H}{300} \div \dfrac{H}{400}$ (TCVN 5575:2024).

---

## 7. BÀI TOÁN 3: KIỂM TRA CHUYỂN VỊ ĐỈNH DO ĐỘNG ĐẤT & KHE PHÒNG CHẤN (SEISMIC TOP DISPLACEMENT)

### 7.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Xác định biên độ biến dạng ngang lớn nhất của tòa tháp khi kích hoạt biến dạng dẻo trong động đất thiết kế; xác định kích thước khoảng hở an toàn của **Khe phòng chấn (Seismic Joint)** để ngăn ngừa 2 tòa nhà va đập vào nhau gây sụp đổ lũy tiến (Pounding Effect).
- **Tiêu chuẩn viện dẫn:** **TCVN 9386-1:2025 (Mục 4.3.4 & 4.4.2)**, **Eurocode 8 (EN 1998-1:2004 Mục 4.4.2.7)**.
- **Mô hình độ cứng:** **50% Cracked EI** ($0.5 E_c I_g$ và $0.5 G A_c$).
- **Dạng tổ hợp:** Tổ hợp bao động đất **Envelope Seismic ULS/SLS**:
  $$Comb_{Seismic} = 1.0\text{ DEAD} + 1.0\text{ SDL} + \psi_{Ei}\text{ LIVE} \pm 1.0\text{ EX} \pm 0.3\text{ EY}$$

### 7.2. Công thức KaTeX Xác định Chuyển vị Thiết kế Phi tuyến ($d_s$)
Chuyển vị thu được từ phân tích phổ phản ứng đàn hồi trong ETABS chỉ là chuyển vị đàn hồi $d_e$. Chuyển vị thực tế của công trình khi các khớp dẻo hình thành là **Chuyển vị thiết kế phi tuyến ($d_s$)**:

$$d_s = q \cdot d_e \quad (\text{mm})$$

*Trong đó:*
- $d_e$ ($\text{mm}$): Chuyển vị đàn hồi trích xuất từ mô hình độ cứng nứt 50% EI trong ETABS.
- $q$: Hệ số ứng xử của kết cấu phụ thuộc vào giải pháp kết cấu và cấp độ dẻo (DCL: $q = 1.5 \div 2.0$; DCM: $q = 3.0 \div 3.9$; DCH: $q > 4.0$).

### 7.3. Điều kiện Giới hạn Bề rộng Khe Phòng chấn ($\delta$)
1. **Trường hợp hai khối tháp độc lập có cùng cao trình sàn và nằm chung trên một khối đế móng:**
   $$\delta \ge \sqrt{d_{s1}^2 + d_{s2}^2} \quad (\text{mm})$$
2. **Trường hợp hai khối tháp độc lập không chung móng, hoặc cao trình sàn các tầng bị lệch nhau (nguy cơ sàn tháp này đập vào giữa thân cột tháp kia):**
   $$\delta \ge d_{s1} + d_{s2} \quad (\text{mm})$$
3. **Khoảng cách từ mép công trình đến ranh giới khu đất lân cận:**
   $$s \ge d_s \quad (\text{mm})$$

---

## 8. BÀI TOÁN 4: KIỂM TRA CHUYỂN VỊ NGANG LIÊN TẦNG DO GIÓ (WIND INTER-STORY DRIFT — SLS)

### 8.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Giới hạn biến dạng trượt cắt tương đối giữa hai tầng liên tiếp kề nhau dưới tác dụng của gió bão; bảo vệ tường gạch xây chèn, vách ngăn phòng, hệ thống cửa và đường ống đứng không bị vặn nứt.
- **Tiêu chuẩn viện dẫn:** **TCVN 2737:2023 (Phụ lục G.2.5)**, **TCVN 5574:2018 (Mục 8.2)**.
- **Mô hình độ cứng:** **100% Gross EI**.
- **Dạng tổ hợp:** Linear Add (SLS) với hệ số $1.0$.

### 8.2. Công thức KaTeX Tính toán Chuyển vị Liên tầng do Gió
Tại mỗi cao trình tầng $i$ ($i = 1, 2, \dots, N_{story}$), hiệu số chuyển vị ngang liên tầng tương đối ($d_{r,i}$) và góc xoay lệch tầng ($\theta_{drift,i}$) được xác định:

$$d_{r,i} = \Delta_i - \Delta_{i-1} \quad (\text{mm})$$

$$\text{Drift Ratio} = \frac{d_{r,i}}{h_i} = \frac{\Delta_i - \Delta_{i-1}}{h_i}$$

*Trong đó:*
- $\Delta_i, \Delta_{i-1}$ ($\text{mm}$): Chuyển vị ngang tổng cộng tại tâm khối lượng sàn tầng $i$ và tầng $i-1$ do gió tiêu chuẩn gây ra.
- $h_i$ ($\text{mm}$): Chiều cao thông thủy của tầng $i$.

### 8.3. Điều kiện Giới hạn & Hệ số An toàn ($FS$)
Theo quy định tại **Phụ lục G.2.5 TCVN 2737:2023**:

$$\frac{d_{r,i}}{h_i} \le \left(\frac{d_r}{h}\right)_{limit}$$

$$FS_{drift} = \frac{\left(\dfrac{d_r}{h}\right)_{limit}}{\dfrac{d_{r,i}}{h_i}} \ge 1.00$$

*Ngưỡng giới hạn $\left(\dfrac{d_r}{h}\right)_{limit}$:*
- Khung nhà có tường xây gạch chèn vật liệu giòn liên kết trực tiếp: **$\dfrac{1}{500}$** ($0.0020$).
- Khung sử dụng vách ngăn dẻo hoặc thạch cao có chi tiết khe co giãn: **$\dfrac{1}{300}$** ($0.0033$).

---

## 9. BÀI TOÁN 5: KIỂM TRA CHUYỂN VỊ NGANG LIÊN TẦNG DO ĐỘNG ĐẤT (SEISMIC INTER-STORY DRIFT — DLS)

### 9.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Kiểm tra trạng thái giới hạn hạn chế hư hỏng (Damage Limitation State - DLS), đảm bảo các bộ phận phi kết cấu không bị hư hỏng quá mức dưới các trận động đất có xác suất xảy ra lớn hơn động đất thiết kế cực hạn.
- **Tiêu chuẩn viện dẫn:** **TCVN 9386-1:2025 (Mục 4.4.3.2)**, **Eurocode 8 (EN 1998-1:2004 Mục 4.4.3.2)**.
- **Mô hình độ cứng:** **50% Cracked EI**.
- **Dạng tổ hợp:** Envelope Seismic DLS.

### 9.2. Công thức KaTeX Tính toán Chuyển vị Liên tầng Kháng chấn
Chuyển vị lệch tầng dẻo thực tế giữa hai sàn tầng liên tiếp:

$$d_{r,i} = d_{s,i} - d_{s,i-1} = q \cdot (d_{e,i} - d_{e,i-1}) \quad (\text{mm})$$

Điều kiện kiểm tra an toàn theo **Điều 4.4.3.2 TCVN 9386-1:2025**:

$$d_{r,i} \cdot \nu \le C_{limit} \cdot h_i$$

$$FS_{seismic\_drift} = \frac{C_{limit} \cdot h_i}{d_{r,i} \cdot \nu} \ge 1.00$$

*Trong đó:*
- $\nu$: Hệ số giảm xét đến chu kỳ lặp tham chiếu nhỏ hơn của tác động động đất gây hư hại phi kết cấu (**Bảng 4.2 TCVN 9386**):
  - $\nu = 0.40$: Đối với công trình thuộc cấp hậu quả II (nhà ở, chung cư, văn phòng, khách sạn).
  - $\nu = 0.50$: Đối với công trình thuộc cấp hậu quả III và IV (bệnh viện, trung tâm cứu hỏa, công trình công cộng đông người).
- $C_{limit}$: Giới hạn độ dốc chuyển vị tầng tương đối:
  - **$C_{limit} = 0.005$ ($0.50\% h_i$):** Áp dụng cho các tòa nhà có bộ phận phi kết cấu bằng **vật liệu giòn liên kết trực tiếp vào khung** (ví dụ: tường chèn gạch đất sét nung, tường block xi măng).
  - **$C_{limit} = 0.0075$ ($0.75\% h_i$):** Áp dụng cho các tòa nhà có bộ phận phi kết cấu có độ dẻo cao hơn.
  - **$C_{limit} = 0.010$ ($1.00\% h_i$):** Áp dụng cho các tòa nhà mà bộ phận phi kết cấu được cố định sao cho không cản trở biến dạng của kết cấu, hoặc công trình không có bộ phận phi kết cấu.

---

## 10. BÀI TOÁN 6: KIỂM TRA ĐỘ NHẠY BẤT ĐỐI XỨNG XOẮN MẶT BẰNG (TORSIONAL IRREGULARITY RATIO)

### 10.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Phát hiện và định lượng mức độ xoắn không gian của các sàn tầng do sự lệch tâm giữa tâm cứng (Center of Rigidity - CR) và tâm khối lượng (Center of Mass - CM). Ngăn ngừa hiện tượng phá hoại giòn ở các cấu kiện cột/vách biên xa tâm cứng.
- **Tiêu chuẩn viện dẫn:** **TCVN 2737:2023**, **TCXD 198:1997**, tham chiếu chuẩn quốc tế **ASCE/SEI 7-16 / 7-22 (Mục 12.8.4.3)**.
- **Bảng trích xuất trong ETABS:** `Story Max Over Avg Displacements`.

### 10.2. Công thức KaTeX Tính toán Tỷ số Xoắn ($Ratio$)
Tại mỗi cao trình tầng, phần mềm quét qua toàn bộ các nút góc và nút biên của mặt bằng để tìm chuyển vị lớn nhất ($\Delta_{max}$) và nhỏ nhất ($\Delta_{min}$):

$$\Delta_{avg} = \frac{\Delta_{max} + \Delta_{min}}{2} \quad (\text{mm})$$

$$Ratio = \frac{\Delta_{max}}{\Delta_{avg}} = \frac{2 \cdot \Delta_{max}}{\Delta_{max} + \Delta_{min}}$$

```
                        MẶT BẰNG SÀN BỊ XOẮN
           ┌───────────────────────────────────────────┐
           │                                           │  Δ_max (Mép biên xa)
           │           CR (Tâm cứng)                   │ ◄──────────►
           │             +                             │
           │                     + CM (Tâm khối lượng) │
           │                                           │
           │                                           │  Δ_min (Mép gần)
           └───────────────────────────────────────────┘ ◄──►
```

### 10.3. Tiêu chí Phân cấp & Quy tắc Xử lý Kỹ thuật Bắt buộc

| Giá trị Tỷ số $Ratio$ | Phân loại Trạng thái Xoắn | Mức độ Nguy hiểm | Quy định Xử lý Thiết kế theo Tiêu chuẩn |
| :---: | :--- | :---: | :--- |
| **$Ratio \le 1.20$** | **Không bất đối xứng xoắn**<br>*(Regular Structure)* | An toàn | Kết cấu biến dạng tịnh tiến đồng đều. Không cần nhân hệ số khuếch đại ngẫu lực xoắn. |
| **$1.20 < Ratio \le 1.40$** | **Bất đối xứng xoắn Loại 1a**<br>*(Torsional Irregularity)* | Cảnh báo | Mặt bằng bị lệch tâm cứng đáng kể. Bắt buộc nhân ngẫu lực xoắn ngẫu nhiên với hệ số khuếch đại $A_x$:<br>$$A_x = \left(\dfrac{Ratio}{1.20}\right)^2 \le 3.0$$ |
| **$Ratio > 1.40$** | **Bất đối xứng xoắn Cực kỳ Nghiêm trọng**<br>*(Extreme Torsional Irregularity)* | **NGUY CƠ SỤP ĐỔ** | Tiêu chuẩn khuyến cáo không được sử dụng cấu hình này. Kỹ sư **bắt buộc phải bố trí lại hệ thống vách cứng/lõi thang** hoặc tăng tiết diện cột biên để cân bằng tâm cứng về gần tâm khối lượng! |

---

## 11. BÀI TOÁN 7: KIỂM TRA KHẢ NĂNG CHỐNG LẬT MÓNG TOÀN THỂ (GLOBAL OVERTURNING STABILITY — EQU)

### 11.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Kiểm tra trạng thái giới hạn cân bằng tĩnh học (EQU - Static Equilibrium) của toàn bộ khối công trình tại cao trình đáy đài móng ($z = 0$). Ngăn ngừa nguy cơ công trình bị xoay bật móng hoặc lật đổ dưới tác dụng của mômen lật do tải trọng ngang bão lớn hoặc động đất.
- **Tiêu chuẩn viện dẫn:** **TCVN 2737:2023 (Điều 7.3)**, **TCVN 9386-5:2025 (Mục 5)**, **TCVN 5574:2018 (Phụ lục E)**.
- **Mô hình độ cứng:** **100% Gross EI**.
- **Dạng tổ hợp:** Tổ hợp cân bằng tĩnh học Linear Add:
  - Tổ hợp Gió chống lật SLS: `0.9 DEAD + 0.9 SDL ± 1.0 W_tc`
  - Tổ hợp Gió chống lật ULS: `0.9 DEAD + 0.9 SDL ± 1.3 W`
  - Tổ hợp Động đất chống lật: `0.9 DEAD + 0.9 SDL ± 1.0 E`
  - **Lưu ý tối thượng:** **Hoạt tải $LIVE = 0$** (Tuyệt đối không tính hoạt tải vào lực giữ chống lật).

### 11.2. Công thức KaTeX Xác định Mômen Gây lật ($M_L$) và Mômen Chống lật ($M_{CL}$)
1. **Mômen gây lật tổng cộng tại đáy móng ($M_L$):**
   Tích lũy từ hợp lực ngang của từng tầng nhân với cánh tay đòn đến đáy đài móng:
   $$M_{L,X} = \sum_{i=1}^{N_{story}} (F_{Y,i} \cdot z_i) \quad (\text{kNm})$$
   $$M_{L,Y} = \sum_{i=1}^{N_{story}} (F_{X,i} \cdot z_i) \quad (\text{kNm})$$
   *(hoặc trích xuất trực tiếp giá trị Base Overturning Moment $M_x, M_y$ từ bảng Story Forces tại Base)*.
2. **Mômen chống lật giữ ổn định tin cậy ($M_{CL}$):**
   Tạo ra bởi tổng tĩnh tải đứng tin cậy của toàn bộ tòa nhà dồn xuống đáy móng, có xét đến hệ số giảm tải an toàn $\gamma_f = 0.90$:
   $$P_{tot,st} = 0.90 \cdot \sum G_k = 0.90 \cdot (G_{DEAD} + G_{SDL}) \quad (\text{kN})$$
   $$M_{CL,X} = P_{tot,st} \cdot \frac{B_x}{2} = (0.90 \cdot \sum G) \cdot \frac{B_x}{2} \quad (\text{kNm})$$
   $$M_{CL,Y} = P_{tot,st} \cdot \frac{B_y}{2} = (0.90 \cdot \sum G) \cdot \frac{B_y}{2} \quad (\text{kNm})$$
   *Trong đó: $B_x, B_y$ là kích thước hình học tổng thể của đáy móng theo phương X và Y.*

### 11.3. Điều kiện Giới hạn & Hệ số An toàn Chống lật ($FS$)
$$FS_{overturning} = \frac{M_{CL}}{M_L} \ge FS_{limit}$$

*Ngưỡng giới hạn cho phép:*
- **Dưới tác dụng của tải trọng Gió (Wind ULS / SLS):**
  $$FS = \frac{M_{CL}}{M_L} \ge 1.50$$
- **Dưới tác dụng của tải trọng Động đất (TCVN 9386-5:2025):**
  $$FS = \frac{M_{CL}}{M_L} \ge 1.20 \div 1.30$$
  *(Động đất cho phép hệ số an toàn thấp hơn do tính chất xuất hiện bất thường trong thời gian ngắn và đất nền có khả năng tiêu tán năng lượng)*.

---

## 12. BÀI TOÁN 8: KIỂM SOÁT LỰC DỌC QUY ĐỔI CỘT KHUNG & VÁCH CỨNG ($\nu_d$ CAPACITY DESIGN)

### 12.1. Mục đích & Căn cứ Tiêu chuẩn
- **Mục đích:** Là yêu cầu cốt lõi trong **Thiết kế Kháng chấn theo Năng lực (Capacity Design)**. Nếu lực nén dọc trục trong cột hoặc vách quá lớn, bê tông vùng nén sẽ đạt biến dạng co ngắn cực hạn $\varepsilon_{cu}$ và bị **nổ vỡ nát giòn (Brittle Crushing)** trước khi cốt thép kịp chảy dẻo hình thành khớp dẻo tiêu tán năng lượng, làm công trình sụp đổ tức thì.
- **Tiêu chuẩn viện dẫn:** **TCVN 9386-1:2025 (Mục 5.4.3.2.1, 5.4.3.4.1, 5.5.3.2.1)**, **Eurocode 8 (EN 1998-1:2004 Mục 5.4 & 5.5)**.
- **Mô hình độ cứng:** **50% Cracked EI**.
- **Dạng tổ hợp:** Tổ hợp bao Động đất Trạng thái Giới hạn Thứ nhất (**Seismic ULS Envelope Combos**).

### 12.2. Công thức KaTeX Tính toán Lực dọc Quy đổi ($\nu_d$)
Lực nén dọc trục tương đối quy đổi ($\nu_d$ - Normalized Axial Force) được xác định bằng tỷ số giữa ứng suất nén tính toán chia cho cường độ chịu nén tính toán của bê tông:

$$\nu_d = \frac{N_{Ed}}{A_c \cdot f_{cd}} = \frac{N_{Ed}}{A_c \cdot R_b}$$

*Trong đó:*
- $N_{Ed}$ ($\text{N}$ hoặc $\text{kN}$): Lực nén dọc trục lớn nhất trong cấu kiện trích xuất từ các tổ hợp bao động đất ULS.
- $A_c = b \cdot h$ ($\text{mm}^2$): Diện tích tiết diện ngang của cột hoặc vách BTCT.
- $f_{cd} = R_b$ ($\text{MPa}$): Cường độ chịu nén tính toán của bê tông theo TCVN 5574:2018 (hoặc $f_{cd} = f_{ck} / \gamma_c$ theo Eurocode 2).

### 12.3. Bảng Giới hạn Cho phép $\nu_{d,max}$ theo TCVN 9386-1:2025

| Loại Cấu kiện Kết cấu | Cấp Dẻo Kháng chấn | Vị trí Tiết diện Kiểm tra | Giới hạn Cho phép $\nu_{d,max}$ | Căn cứ Điều khoản TCVN 9386-1:2025 |
| :--- | :---: | :---: | :---: | :--- |
| **Cột Khung (Columns)** | **DCL** *(Cấp dẻo thấp)* | Toàn bộ chiều cao cột | **$\nu_d \le 0.65$** | Mục 5.4.3.2.1 |
| **Cột Khung (Columns)** | **DCM** *(Cấp dẻo trung bình)* | Toàn bộ chiều cao cột | **$\nu_d \le 0.55$** | Mục 5.4.3.2.1 |
| **Cột Khung (Columns)** | **DCH** *(Cấp dẻo cao)* | Toàn bộ chiều cao cột | **$\nu_d \le 0.45$** | Mục 5.5.3.2.1 |
| **Vách Cứng & Lõi (Walls/Piers)** | **DCM** *(Cấp dẻo trung bình)* | Vùng tới hạn chân vách ($h_{cr}$)<br>*(Critical region at base)* | **$\nu_d \le 0.35$** | Mục 5.4.3.4.1 |
| **Vách Cứng & Lõi (Walls/Piers)** | **DCM** *(Cấp dẻo trung bình)* | Các tầng phía trên ngoài vùng tới hạn | **$\nu_d \le 0.40$** | Mục 5.4.3.4.1 |

> [!WARNING]
> **HÀNH ĐỘNG KỸ THUẬT BẮT BUỘC KHI $\nu_d > \nu_{d,max}$:**  
> Khi $\nu_d$ vượt ngưỡng tiêu chuẩn, việc tăng thêm cốt thép dọc **HOÀN TOÀN KHÔNG CÓ TÁC DỤNG KHẮC PHỤC**!  
> Kỹ sư bắt buộc phải:  
> 1. Mở rộng kích thước hình học tiết diện bê tông ($b \times h$ của cột, chiều dày $t_w$ của vách).  
> 2. Hoặc nâng cấp cấp độ bền bê tông (ví dụ từ B30 lên B40, B50) để tăng cường độ $R_b$.

---

## 13. BÀI TOÁN 9: KIỂM TRA HIỆU ỨNG BẬC HAI $P-\Delta$ TOÀN TẦNG & $P-\delta$ CỘT CỤC BỘ

### 13.1. Phân biệt Rành mạch giữa $P-\Delta$ Toàn tầng & $P-\delta$ Cột
- **Hiệu ứng $P-\Delta$ lớn (Global Sway P-Delta):** Do chuyển vị ngang tương đối giữa các sàn tầng ($d_r$) làm lệch trục của tổng tải trọng đứng tích lũy ($P_{tot}$). Khống chế bởi **TCVN 9386-1:2025** thông qua **Hệ số độ nhạy $\theta$**.
- **Hiệu ứng $P-\delta$ nhỏ (Local Member P-delta):** Do độ uốn cong dọc trục $\delta(x)$ của bản thân thanh cột giữa hai đầu nút dưới lực nén dọc trục $N$. Khống chế bởi **TCVN 5574:2018** thông qua **Hệ số uốn dọc $\eta$**.

### 13.2. Công thức KaTeX Tính Hệ số Độ nhạy P-Delta Toàn tầng ($\theta$)
Theo quy định tại **Điều 4.4.2.2 TCVN 9386-1:2025**, tại mỗi cao trình tầng $i$:

$$\theta = \frac{P_{tot} \cdot d_r}{V_{tot} \cdot h_s}$$

*Trong đó:*
- $P_{tot}$ ($\text{kN}$): Tổng tải trọng trọng lực thẳng đứng tích lũy của toàn bộ các tầng nằm phía trên tầng đang xét:
  $$P_{tot} = \sum_{j \ge i} (G_{k,j} + \psi_{Ei} \cdot Q_{k,j})$$
- $d_r = q \cdot d_e$ ($\text{mm}$): Chuyển vị lệch tầng thiết kế dẻo phi tuyến tương đối giữa thềm trần và sàn tầng đang xét.
- $V_{tot}$ ($\text{kN}$): Tổng lực cắt tầng thiết kế do động đất tại tầng đang xét.
- $h_s$ ($\text{mm}$): Chiều cao của tầng đang xét.

### 13.3. Bốn Ngưỡng Kiểm soát & Quy tắc Xử lý Kỹ thuật Bắt buộc

```
    θ = 0.00                    θ = 0.10                    θ = 0.20                    θ = 0.30
       │                           │                           │                           │
       ├───────────────────────────┼───────────────────────────┼───────────────────────────┤
       │   🟢 BỎ QUA P-DELTA      │   🟡 KHUẾCH ĐẠI β        │   🟠 GIẢI TRỰC TIẾP      │   🔴 TIÊU CHUẨN CẤM
       │   (Độ cứng tầng đủ lớn)   │   (Nhân hệ số β = 1/(1-θ))│   (Chạy P-Delta phần mềm) │   (Thiết kế lại hệ vách)
```

1. **Khi $\theta \le 0.10$:**  
   - Kết cấu đủ cứng chống uốn dạt. Tiêu chuẩn **CHO PHÉP BỎ QUA** hiệu ứng P-Delta. Lấy trực tiếp nội lực phân tích bậc 1.
2. **Khi $0.10 < \theta \le 0.20$:**  
   - Bắt buộc phải kể đến hiệu ứng bậc hai. Tiêu chuẩn cho phép kể đến gần đúng bằng cách nhân toàn bộ nội lực động đất với hệ số khuếch đại $\beta$:
     $$\beta = \frac{1}{1 - \theta}$$
3. **Khi $0.20 < \theta \le 0.30$:**  
   - Phương pháp nhân hệ số $\beta$ không còn đủ tin cậy. **BẮT BUỘC phân tích phi tuyến hình học trực tiếp** (Khai báo P-Delta Iterative trong ETABS).
4. **Khi $\theta > 0.30$:**  
   - Công trình có nguy cơ sụp đổ lũy tiến do mất ổn định hình học tổng thể. **TIÊU CHUẨN NGHIÊM CẤM**. Bắt buộc phải **THIẾT KẾ LẠI KẾT CẤU** (tăng bề dày vách cứng, bổ sung lõi thang để giảm $d_r$).

> [!IMPORTANT]
> **Giới hạn Cực đại của $\theta$:** Trong mọi trường hợp, giá trị $\theta$ không được vượt quá:
> $$\theta \le \theta_{max} = \min\left(0.30, \; \frac{1}{q}\right)$$
> *(Ví dụ: Hệ khung vách có $q = 3.9 \implies \theta_{max} = \min(0.30, 0.256) = 0.256$).*

### 13.4. Thuật toán Kiểm tra Uốn dọc Cục bộ Cột $P-\delta$ theo TCVN 5574:2018 (Mục 8.1.2.2.4)
Khi cột chịu nén lệch tâm có độ mảnh $\lambda = l_0 / i > 14$ (hoặc $l_0 / h > 4$), mômen uốn thiết kế phải được khuếch đại bởi **Hệ số uốn dọc $\eta$**:

$$M_{calc} = \eta \cdot M_v$$

$$\eta = \frac{1}{1 - \dfrac{N}{N_{cr}}}$$

*Lực tới hạn Euler quy ước $N_{cr}$ và Độ cứng uốn phi tuyến $D$:*

$$N_{cr} = \frac{\pi^2 D}{l_0^2}$$

$$D = k_b E_b I + k_s E_s I_s$$

*Trong đó:*
- $k_s = 0.70$: Hệ số đóng góp độ cứng của cốt thép dọc.
- $k_b = \dfrac{0.15}{\varphi_l (0.3 + \delta_e)}$: Hệ số suy giảm độ cứng bê tông do nứt và biến dạng dẻo.
- $\varphi_l = 1 + \beta \dfrac{M_{l1}}{M_1} \in [1.0, 2.0]$: Hệ số xét tác dụng dài hạn của tải trọng (từ biến).
- $\delta_e = \dfrac{e_0}{h} \ge \delta_{e,min} = 0.5 - 0.01 \dfrac{l_0}{h} - 0.01 R_b$: Độ lệch tâm tương đối.
- $l_0 = \psi \cdot H$: Chiều dài tính toán của cột ($\psi = 0.70$ cho khung giằng có vách; $\psi = 1.20 \div 1.50$ cho khung không giằng).

---

## 14. BẢNG TRA CỨU ĐỐI CHIẾU NHANH TOÀN BỘ CÔNG THỨC, NGƯỠNG & HÀM CODE CES.LIB

Bảng tổng hợp tra cứu nhanh phục vụ lập trình, kiểm toán và lập thuyết minh kết cấu:

| Bài toán | Công thức KaTeX Cốt lõi | Ngưỡng Giới hạn Cho phép | Tiêu chuẩn Viện dẫn | Hàm/Module Code trong CES.Lib |
| :--- | :--- | :--- | :--- | :--- |
| **Gia tốc đỉnh** | $a = \Delta \cdot \left(\dfrac{2\pi}{T}\right)^2$ | $a \le 150\text{ mm/s}^2$ | TCXD 198:1997<br>TCVN 2737:2023 | `renderAccelerationTab()` trong [`app.js`](file:///d:/Kien%20thuc%20Xay%20Dung/app.js) |
| **Chuyển vị đỉnh gió** | $U = \sqrt{UX^2 + UY^2}$ | $U \le \dfrac{H}{500}$ | TCXD 198:1997<br>TCVN 5574:2018 | `renderDisplacementTab()` trong [`app.js`](file:///d:/Kien%20thuc%20Xay%20Dung/app.js) |
| **Chuyển vị đỉnh ĐĐ** | $d_s = q \cdot d_e$ | $\delta \ge \sqrt{d_{s1}^2 + d_{s2}^2}$ | TCVN 9386-1:2025 | Thẻ `on-dinh-to-hop-tai-trong-matrix` trong [`data.js`](file:///d:/Kien%20thuc%20Xay%20Dung/data.js) |
| **Chuyển vị tầng gió** | $\dfrac{d_r}{h_s} = \dfrac{\Delta_i - \Delta_{i-1}}{h_s}$ | $\dfrac{d_r}{h_s} \le \dfrac{1}{500}$ | TCVN 2737:2023<br>Phụ lục G.2.5 | `renderInterStoryDriftTab()` trong [`app.js`](file:///d:/Kien%20thuc%20Xay%20Dung/app.js) |
| **Chuyển vị tầng ĐĐ** | $d_{r,i} \cdot \nu$ | $d_{r,i} \cdot \nu \le C_{limit} \cdot h_s$ | TCVN 9386-1:2025<br>Mục 4.4.3.2 | `renderInterStoryDriftTab()` trong [`app.js`](file:///d:/Kien%20thuc%20Xay%20Dung/app.js) |
| **Bất đối xứng xoắn** | $Ratio = \dfrac{\Delta_{max}}{\Delta_{avg}}$ | $Ratio \le 1.20$<br>($A_x$ khi $1.20 < Ratio \le 1.40$) | TCVN 2737:2023<br>ASCE 7-16/22 | Thẻ `etabs-xem-chuyen-vi-ngang` trong [`data.js`](file:///d:/Kien%20thuc%20Xay%20Dung/data.js) |
| **Chống lật móng** | $FS = \dfrac{M_{CL}}{M_L}$ | $FS \ge 1.50\text{ (Gió)}$<br>$FS \ge 1.20\text{ (ĐĐ)}$ | TCVN 2737 (Đ.7.3)<br>TCVN 9386-5:2025 | `renderOverturningTab()` trong [`app.js`](file:///d:/Kien%20thuc%20Xay%20Dung/app.js) |
| **Lực dọc quy đổi $\nu_d$** | $\nu_d = \dfrac{N_{Ed}}{A_c \cdot R_b}$ | Cột: $\le 0.55\text{ (DCM)}$<br>Vách: $\le 0.35\text{ (chân)}$ | TCVN 9386-1:2025<br>Mục 5.4.3.2 & 5.4.3.4 | Thẻ `on-dinh-to-hop-tai-trong-matrix` trong [`data.js`](file:///d:/Kien%20thuc%20Xay%20Dung/data.js) |
| **P-Delta toàn tầng** | $\theta = \dfrac{P_{tot} \cdot d_r}{V_{tot} \cdot h_s}$ | $\theta \le 0.10$<br>($\theta_{max} \le 0.30$) | TCVN 9386-1:2025<br>Mục 4.4.2.2 | `renderPDeltaTab()` trong [`app.js`](file:///d:/Kien%20thuc%20Xay%20Dung/app.js) |
| **P-$\delta$ cục bộ cột** | $\eta = \dfrac{1}{1 - N/N_{cr}}$ | $M_{calc} = \eta \cdot M_v$ | TCVN 5574:2018<br>Mục 8.1.2.2.4 | [`column_interaction_engine.js`](file:///d:/Kien%20thuc%20Xay%20Dung/column_interaction_engine.js) |

---
*Tài liệu được biên soạn và chuẩn hóa toàn diện cho Hệ thống Thư viện Kiến thức Kết cấu CES.Lib.*
