using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CSiAPIv1;

namespace QksExcelAddIn.Services
{
    public partial class EtabsService
    {
        private sealed class Operation : IDisposable
        {
            internal string ModelId = AppConnectionManager.ModelIdentity;
            internal bool BackedUp;
            internal cSapModel? Model;
            internal eUnits? OriginalUnits;
            internal Operation? Previous;
            public void Dispose() {
                try {
                    if (Model != null && OriginalUnits.HasValue && Model.SetPresentUnits(OriginalUnits.Value) != 0)
                        CustomMessageBox.Show("Không khôi phục được đơn vị ETABS. Hãy kiểm tra trước khi tiếp tục.", "Đơn vị ETABS", CustomMessageBox.IconType.Warning);
                } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                finally { ActiveOperation.Value = Previous; }
            }
        }
        private static readonly AsyncLocal<Operation?> ActiveOperation = new AsyncLocal<Operation?>();
        public static IDisposable BeginOperation()
        {
            var operation = new Operation { Previous = ActiveOperation.Value };
            try { operation.Model = AppConnectionManager.ActiveEtabs?.SapModel; operation.OriginalUnits = operation.Model?.GetPresentUnits(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            ResetSessionCaches();
            ActiveOperation.Value = operation; return operation;
        }
        public IDisposable BeginWrite()
        {
            if (ActiveOperation.Value != null && ActiveOperation.Value.ModelId != AppConnectionManager.ModelIdentity)
                throw new InvalidOperationException("Kết nối ETABS đã đổi; hủy ghi dữ liệu từ phiên cũ.");
            var model = CurrentSapModel ?? throw new InvalidOperationException("Chưa kết nối ETABS.");
            if (model.GetModelIsLocked()) throw new InvalidOperationException("Mô hình ETABS đang khóa.");
            var operation = ActiveOperation.Value;
            if (operation == null || !operation.BackedUp)
            {
                string path = model.GetModelFilename();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    throw new InvalidOperationException("Lưu mô hình ETABS trước khi gán tải.");
                if (model.File.Save(path) != 0) throw new IOException("Không lưu được mô hình trước khi sao lưu. Chưa gán tải.");
                string directory = Path.Combine(Path.GetDirectoryName(path)!, "QKS_Backups");
                Directory.CreateDirectory(directory);
                File.Copy(path, Path.Combine(directory, Path.GetFileNameWithoutExtension(path) + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + Path.GetExtension(path)), false);
                if (operation != null) operation.BackedUp = true;
            }
            return new UnitsScope(model);
        }
        private sealed class UnitsScope : IDisposable
        {
            private readonly cSapModel _model;
            private readonly eUnits _original;
            internal UnitsScope(cSapModel model)
            {
                _model = model; _original = model.GetPresentUnits();
                if (model.SetPresentUnits(eUnits.kN_m_C) != 0) throw new InvalidOperationException("Không thiết lập được đơn vị kN-m.");
            }
            public void Dispose()
            {
                ResetSessionCaches();
                if (_model.SetPresentUnits(_original) != 0)
                    throw new InvalidOperationException("Đã xử lý lệnh nhưng không khôi phục được đơn vị ETABS. Kiểm tra model trước khi tiếp tục.");
            }
        }
    }
}
