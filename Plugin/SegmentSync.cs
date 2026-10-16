using System;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace QKS_Loads_AcadPlugin
{
    public static partial class QksLoadsCommands
    {
        private static string SegmentText(DBObject entity, CadWallLiveBeamItem beam)
        {
            string[] span = GetXData(entity, "QKS_SEGMENT").Split('|');
            double start, end;
            if (span.Length != 2 || !double.TryParse(span[0], NumberStyles.Float, CultureInfo.InvariantCulture, out start) ||
                !double.TryParse(span[1], NumberStyles.Float, CultureInfo.InvariantCulture, out end))
                throw new InvalidOperationException("Thiếu thông tin phân đoạn. Hãy xuất lại mặt bằng bằng plugin mới.");
            var segments = beam.Segments ?? new System.Collections.Generic.List<CadLoadSegment>();
            if (segments.Count == 0) return "0";
            var matches = segments.Where(s => Math.Abs(s.StartDist - start) < 0.0001 && Math.Abs(s.EndDist - end) < 0.0001).ToList();
            if (matches.Count != 1) throw new InvalidOperationException("Phân đoạn tải đã thay đổi. Hãy xuất lại mặt bằng để cập nhật hình học; chưa sửa text CAD.");
            return string.IsNullOrWhiteSpace(matches[0].Text) ? "0" : matches[0].Text;
        }
    }
}
