using System;
using System.Linq;
using CSiAPIv1;
namespace QksExcelAddIn.Services
{
    public partial class EtabsService
    {
        private void VerifyUniform(string area, string pattern, double expected, int direction)
        {
            var model = CurrentSapModel ?? throw new InvalidOperationException("ETABS mất kết nối khi đọc lại tải.");
            int count = 0; string[] names = new string[0], patterns = new string[0], systems = new string[0];
            int[] directions = new int[0]; double[] values = new double[0];
            if (model.AreaObj.GetLoadUniform(area, ref count, ref names, ref patterns, ref systems, ref directions, ref values, eItemType.Objects) != 0 ||
                !Enumerable.Range(0, count).Any(i => patterns[i].Equals(pattern, StringComparison.OrdinalIgnoreCase) && directions[i] == direction && Math.Abs(values[i] - expected) <= Math.Max(1e-6, Math.Abs(expected) * 1e-6)))
                throw new InvalidOperationException("Đọc lại tải không khớp tại sàn " + area + "/" + pattern + ". Kiểm tra bản sao lưu trước khi tiếp tục.");
        }
        private void VerifyFrame(string frame, string pattern, int direction, double start, double end, double v1, double v2, bool relative)
        {
            var loads = GetBeamDistributedLoads(frame, pattern, direction);
            bool found = loads.Any(x => Math.Abs((relative ? x.RelDist1 : x.Dist1) - start) < 1e-6 &&
                Math.Abs((relative ? x.RelDist2 : x.Dist2) - end) < 1e-6 && Math.Abs(x.Val1 - v1) <= Math.Max(1e-6, Math.Abs(v1) * 1e-6) &&
                Math.Abs(x.Val2 - v2) <= Math.Max(1e-6, Math.Abs(v2) * 1e-6));
            if (!found) throw new InvalidOperationException("Đọc lại tải không khớp tại dầm " + frame + "/" + pattern + ". Kiểm tra model và bản sao lưu.");
        }
    }
}
