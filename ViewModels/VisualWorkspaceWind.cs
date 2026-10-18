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
        private string story="";
        public string Story { get=>story;set { story=value;PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(Story))); } }
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
        public string WindReference { get; set; } = "Nhập tay theo Story";
        public bool CanInputStoryWind=>!Busy&&DraftContextActive;
        public UserWindState? WindUserBaseline { get; private set; }
        private string windModelFingerprint = "";
        private string windIdentity = "";
        private bool windRequiresReload;
        private string savedWindState = "";
        public IReadOnlyList<UserWindEditRow> WindUserRows { get; private set; } = Array.Empty<UserWindEditRow>();
        public UserWindEditRow? SelectedWindUserRow { get; set; }
        public bool CanApplyUserWind => !Busy && !Offline && !applyNeedsRead && !windRequiresReload && Snapshot?.Source == "ETABS" && WindUserRows.Count > 0 && windModelFingerprint == snapshotFingerprint;
        private string WindState => JsonCodec.Serialize(WindUserRows.Select(r => new { Load=r.Row(),r.Story }));
        public bool WindUserDirty { get { try { return WindUserRows.Count > 0 && WindState != savedWindState; } catch { return true; } } }
        public string WindUserStatus => WindUserBaseline == null ? "Chưa đọc User Loads." : WindUserBaseline.Pattern + " · " + WindUserRows.Count + " khóa diaphragm · " + (WindUserDirty ? "nháp chưa lưu" : "đã lưu/không đổi") + (windModelFingerprint != snapshotFingerprint || windRequiresReload ? " · cần đọc lại User Loads" : "");
        public void NotifyWind()
        {
            Changed(nameof(CanApplyUserWind)); Changed(nameof(CanInputStoryWind)); Changed(nameof(WindUserRows)); Changed(nameof(WindUserStatus));
        }
        public void ImportStoryWind(IEnumerable<StoryWindInput> rows,string basis)
        {
            if(Busy||!DraftContextActive)throw new InvalidOperationException("Đọc đúng snapshot và tạo bộ nháp trước khi nhập gió.");
            var inputs=rows.ToArray();
            if(inputs.Any(r=>r.HasInput&&string.Equals(r.Target,"UserLoads",StringComparison.OrdinalIgnoreCase))&&windModelFingerprint!=snapshotFingerprint)throw new InvalidOperationException("Đọc User Loads đúng snapshot trước khi nhập lực tầng.");
            var plan=StoryWindPlan.Create(Drafts!,inputs,SelectedIds,WindUserBaseline,basis);
            Drafts!.ReplaceLoads(plan.DraftLoads);
            foreach(var update in plan.UserRows) {
                var target=WindUserRows.Single(r=>r.DiaphragmKey==update.DiaphragmKey);
                target.Fx=update.Fx;target.Fy=update.Fy;target.Mz=update.Mz;target.X=update.X;target.Y=update.Y;target.Story=plan.StoryAssignments[update.DiaphragmKey];
            }
            WindReference=basis;NotifyDraftState();NotifyWind();Status="Đã nhập "+plan.InputCount+" dòng tải theo Story vào NHÁP; giữ nguyên q/F/M và đơn vị, không quy đổi, chưa ghi ETABS.";
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
                "Nhập giá trị gió theo Story · " + WindReference,storyAssignments:WindUserRows.Where(r=>!string.IsNullOrWhiteSpace(r.Story)).ToDictionary(r=>r.DiaphragmKey,r=>r.Story,StringComparer.Ordinal));
        }
        public void ImportUserWindDocument(UserWindDocument document)
        {
            if (Snapshot == null || document.Identity != Snapshot.Identity || document.ModelFingerprint != snapshotFingerprint) throw new ArgumentException("Nháp User Loads thuộc nguồn khác.");
            var expected = document.Expected(); SetUserWindBaseline(document.Before);
            foreach (var source in expected.Rows) {
                var row = WindUserRows.Single(r => r.DiaphragmKey == source.DiaphragmKey);
                row.Fx = source.Fx; row.Fy = source.Fy; row.Mz = source.Mz; row.X = source.X; row.Y = source.Y;
                if(document.StoryAssignments.TryGetValue(source.DiaphragmKey,out var story))row.Story=story;
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
    }
}
