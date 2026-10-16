using System;
using System.Collections.Generic;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Tiện ích sinh và quản lý màu sắc TrueColor RGB theo tên tiết diện dầm (Section).
    /// Áp dụng bảng màu Palette tương phản cao, tối ưu hiển thị trên nền CAD.
    /// </summary>
    public static class SectionColorHelper
    {
        // Bảng màu chuẩn được thiết kế đẹp mắt, nổi bật trên cả nền đen và trắng của CAD
        private static readonly (byte R, byte G, byte B)[] PresetPalette = new (byte, byte, byte)[]
        {
            (0, 206, 209),   // 1. Dark Turquoise (Cyan đậm)
            (255, 140, 0),   // 2. Dark Orange (Cam ấm)
            (50, 205, 50),   // 3. Lime Green (Xanh lá mạ sáng)
            (186, 85, 211),  // 4. Medium Orchid (Tím phong lan)
            (30, 144, 255),  // 5. Dodger Blue (Xanh dương hoàng gia)
            (255, 99, 71),   // 6. Tomato (Đỏ san hô)
            (255, 215, 0),   // 7. Gold (Vàng kim)
            (0, 250, 154),   // 8. Medium Spring Green (Xanh bạc hà)
            (255, 20, 147),  // 9. Deep Pink (Hồng cánh sen)
            (70, 130, 180),  // 10. Steel Blue (Xanh xám thép)
            (230, 126, 34),  // 11. Carrot Orange (Cam cà rốt)
            (155, 89, 182),  // 12. Amethyst Purple (Tím thạch anh)
            (26, 188, 156),  // 13. Turquoise (Ngọc lam)
            (241, 196, 15),  // 14. Sunflower Yellow (Vàng hướng dương)
            (231, 76, 60),   // 15. Alizarin Red (Đỏ tươi)
            (52, 152, 219)   // 16. Peter River Blue (Xanh lam nhạt)
        };

        private static readonly Dictionary<string, (byte R, byte G, byte B)> _cache = new Dictionary<string, (byte R, byte G, byte B)>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Lấy mã màu RGB (R, G, B) tương ứng với tên tiết diện dầm.
        /// Cùng một tên tiết diện sẽ luôn nhận được một mã màu cố định.
        /// </summary>
        public static (byte R, byte G, byte B) GetSectionRgb(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return (128, 128, 128); // Xám mặc định nếu không có tiết diện
            }

            string key = sectionName.Trim();
            lock (_cache)
            {
                if (_cache.TryGetValue(key, out var cachedColor))
                {
                    return cachedColor;
                }

                // Nếu số lượng tiết diện trong giới hạn Palette, gán tuần tự
                int index = _cache.Count;
                (byte R, byte G, byte B) color;

                if (index < PresetPalette.Length)
                {
                    color = PresetPalette[index];
                }
                else
                {
                    // Thuật toán Golden Ratio Hue Distribution cho số lượng tiết diện không giới hạn
                    int hash = Math.Abs(key.GetHashCode());
                    double goldenRatio = 0.618033988749895;
                    double hue = ((hash * goldenRatio) % 1.0) * 360.0;
                    double saturation = 0.80; // 80%
                    double lightness = 0.60;  // 60%

                    color = HslToRgb(hue, saturation, lightness);
                }

                _cache[key] = color;
                return color;
            }
        }

        /// <summary>
        /// Chuyển đổi mã màu HSL sang RGB
        /// </summary>
        private static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
        {
            double c = (1.0 - Math.Abs(2.0 * l - 1.0)) * s;
            double x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
            double m = l - c / 2.0;

            double r1 = 0, g1 = 0, b1 = 0;
            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)Math.Max(0, Math.Min(255, Math.Round((r1 + m) * 255.0)));
            byte g = (byte)Math.Max(0, Math.Min(255, Math.Round((g1 + m) * 255.0)));
            byte b = (byte)Math.Max(0, Math.Min(255, Math.Round((b1 + m) * 255.0)));

            return (r, g, b);
        }

        /// <summary>
        /// Xóa bộ nhớ đệm màu (dùng khi bắt đầu phiên xuất mới)
        /// </summary>
        public static void ResetCache()
        {
            lock (_cache)
            {
                _cache.Clear();
            }
        }
    }
}
