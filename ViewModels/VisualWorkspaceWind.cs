#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed class UserWindEditRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string DiaphragmKey { get; }
        private double fx, fy, mz, x, y;
        public double Fx { get => fx; set { fx = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fx))); } }
        public double Fy { get => fy; set { fy = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fy))); } }
        public double Mz { get => mz; set { mz = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mz))); } }
        public double X { get => x; set { x = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X))); } }
        public double Y { get => y; set { y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y))); } }
        public UserWindEditRow(UserWindRow row) { DiaphragmKey = row.DiaphragmKey; fx = row.Fx; fy = row.Fy; mz = row.Mz; x = row.X; y = row.Y; }
        public UserWindRow Row() => new UserWindRow(DiaphragmKey, Fx, Fy, Mz, X, Y);
    }
    public sealed partial class VisualWorkspaceViewModel
    {
        public string[] WindDirections { get; } = { "Global-X", "Global-Y" };
        public string WindDirection { get; set; } = "Global-X";
        public string WindBottom { get; set; } = "";
        public string WindTop { get; set; } = "";
        public string WindPressureBottom { get; set; } = "";
        public string WindPressureTop { get; set; } = "";
        public string WindTributaryHeight { get; set; } = "";
        public string WindReference { get; set; } = "";
        public string WindPeriod { get; set; } = "";
        public string WindStoryBottom { get; set; } = "";
        public string WindStoryTop { get; set; } = "";
        public string WindFacadeWidth { get; set; } = "";
        public bool CanBuildWind => !Busy && DraftContextActive && SelectedElement?.Kind == ElementKind.Beam;
        public UserWindState? WindUserBaseline { get; private set; }
        private string windModelFingerprint = "";
        private string windIdentity = "";
        private bool windRequiresReload;
        private string savedWindState = "";
        public IReadOnlyList<UserWindEditRow> WindUserRows { get; private set; } = Array.Empty<UserWindEditRow>();
        public UserWindEditRow? SelectedWindUserRow { get; set; }
        public bool CanApplyUserWind => !Busy && !Offline && !applyNeedsRead && !windRequiresReload && Snapshot?.Source == "ETABS" && WindUserRows.Count > 0 && windModelFingerprint == snapshotFingerprint;
        private string WindState => JsonCodec.Serialize(WindUserRows.Select(r => r.Row()));
        public bool WindUserDirty { get { try { return WindUserRows.Count > 0 && WindState != savedWindState; } catch { return true; } } }
        public string WindUserStatus => WindUserBaseline == null ? "Chưa đọc User Loads." : WindUserBaseline.Pattern + " · " + WindUserRows.Count + " khóa diaphragm · " + (WindUserDirty ? "nháp chưa lưu" : "đã lưu/không đổi") + (windModelFingerprint != snapshotFingerprint || windRequiresReload ? " · cần đọc lại User Loads" : "");
        public void NotifyWind()
        {
            Changed(nameof(CanBuildWind)); Changed(nameof(CanApplyUserWind)); Changed(nameof(WindUserRows)); Changed(nameof(WindUserStatus));
        }
        private string WindBasis()
        {
            if (string.IsNullOrWhiteSpace(WindReference)) throw new ArgumentException("Ghi nguồn/bảng tính áp lực đã kiểm chứng; module chưa tự tính hệ số TCVN.");
            string period = "";
            if (!string.IsNullOrWhiteSpace(WindPeriod)) { double t = Parse(WindPeriod); if (t <= 0) throw new ArgumentException("Chu kỳ T phải > 0 s."); period = " · T=" + t.ToString("R", CultureInfo.InvariantCulture) + " s (tham chiếu)"; }
            return WindReference + period + " · p(z) tuyến tính, quy đổi hình học; không tự tính Gf.";
        }
        public void BuildWindOnBeam()
        {
            if (!CanBuildWind) throw new InvalidOperationException("Chọn dầm biên và bộ nháp đúng nguồn.");
            var band = new WindPressureBand(Parse(WindBottom), Parse(WindTop), Parse(WindPressureBottom), Parse(WindPressureTop));
            int direction = LoadDirections.Code(WindDirection);
            var action = DraftActionName == DraftActions[0] ? DraftAction.Add : DraftActionName == DraftActions[1] ? DraftAction.ReplaceMatching : throw new ArgumentException("Thao tác tải không hợp lệ.");
            var mapped = band.OnBeam(SelectedElement!, DraftPattern, direction, Parse(WindTributaryHeight), action);
            var load = new DraftLoad(editingId ?? mapped.Id, mapped.ElementId, mapped.Kind, mapped.Pattern, mapped.Direction, mapped.Start, mapped.End, mapped.Value1, mapped.Value2, mapped.Action, WindBasis());
            Drafts!.Put(load); editingId = load.Id; NotifyDraftState(); Status = "Đã tạo NHÁP gió dầm biên: p × chiều cao đón gió = q; chưa ghi model.";
        }
        public void SetUserWindBaseline(UserWindState baseline)
        {
            if (Snapshot == null) throw new InvalidOperationException("Đọc/mở snapshot trước.");
            WindUserBaseline = baseline; windModelFingerprint = snapshotFingerprint; windIdentity = Snapshot.Identity; windRequiresReload = false;
            WindUserRows = baseline.Rows.Select(r => new UserWindEditRow(r)).ToArray();
            foreach (var row in WindUserRows) row.PropertyChanged += (_, __) => { Changed(nameof(WindUserStatus)); };
            savedWindState = WindState; SelectedWindUserRow = null; NotifyWind();
        }
        public UserWindDocument UserWindDocument()
        {
            if (WindUserBaseline == null) throw new InvalidOperationException("Chưa có dữ liệu User Loads.");
            var before = WindUserBaseline.Rows.ToDictionary(r => r.DiaphragmKey, StringComparer.Ordinal);
            var changes = WindUserRows.Select(r => r.Row()).Where(r => JsonCodec.Serialize(r) != JsonCodec.Serialize(before[r.DiaphragmKey]));
            return new UserWindDocument(windIdentity, windModelFingerprint, WindUserBaseline, changes,
                "Nháp User Loads · " + WindReference + " · T=" + WindPeriod + " s (tham chiếu, chưa tự tính hệ số TCVN)");
        }
        public void ImportUserWindDocument(UserWindDocument document)
        {
            if (Snapshot == null || document.Identity != Snapshot.Identity || document.ModelFingerprint != snapshotFingerprint) throw new ArgumentException("Nháp User Loads thuộc nguồn khác.");
            var expected = document.Expected(); SetUserWindBaseline(document.Before);
            foreach (var source in expected.Rows) {
                var row = WindUserRows.Single(r => r.DiaphragmKey == source.DiaphragmKey);
                row.Fx = source.Fx; row.Fy = source.Fy; row.Mz = source.Mz; row.X = source.X; row.Y = source.Y;
            }
            MarkUserWindSaved();
        }
        public void MarkUserWindSaved() { savedWindState = WindState; Changed(nameof(WindUserStatus)); }
        public void SetUserWindReport(UserWindReport report)
        {
            LastWriteReport = report; applyNeedsRead = report.MayHaveChanges || !report.UnitsRestored;
            windRequiresReload = applyNeedsRead; lastWriteSummary = report.Outcome + " · User Loads · " + report.Error;
            NotifyApply(); NotifyWind();
        }
        public void BuildWindOnUserRow()
        {
            if (Busy || SelectedWindUserRow == null || windModelFingerprint != snapshotFingerprint) throw new InvalidOperationException("Chọn hàng trong bảng User Loads gió trước.");
            WindBasis(); var band = new WindPressureBand(Parse(WindBottom), Parse(WindTop), Parse(WindPressureBottom), Parse(WindPressureTop));
            double force = band.StoryForce(Parse(WindStoryBottom), Parse(WindStoryTop), Parse(WindFacadeWidth));
            int direction = LoadDirections.Code(WindDirection);
            if (direction == 4) SelectedWindUserRow.Fx = force; else if (direction == 5) SelectedWindUserRow.Fy = force; else throw new ArgumentException("Chỉ dùng Global X/Y cho gió.");
            Status = "Đã quy đổi NHÁP p(z) × diện tích đón gió → F. Giữ nguyên hướng còn lại, Mz và X/Y; kiểm tra đúng khóa/tầng trước khi Apply.";
        }
    }
}
