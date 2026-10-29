#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Qks.Shared;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class ViewerLabelPreference
    {
        public ElementKind Kind { get; set; }
        public bool Visible { get; set; } = true;
        public bool Extrude { get; set; } = true;
        public bool Label { get; set; } = true;
        public bool UniqueName { get; set; }
        public bool Section { get; set; }
    }
    public sealed class ViewerPreferences
    {
        public int Version { get; set; } = 1;
        public ViewerLabelPreference[] Labels { get; set; } = Array.Empty<ViewerLabelPreference>();
        public bool Extrude { get; set; } = true;
        public bool ColorByProperty { get; set; }
        public string Kind { get; set; } = "Tất cả";
        public string HalfDepth { get; set; } = "0.15";
        public bool Reverse { get; set; }
        public bool LoadSetsMode { get; set; }
        public bool FrameLoads { get; set; }
        public bool ShellLoads { get; set; }
        public bool JointLoads { get; set; }
        public bool LoadValues { get; set; } = true;
        public bool ZeroLoads { get; set; }
        public bool? LeftPanel { get; set; }
        public bool? RightPanel { get; set; }
        public string LastModelPath { get; set; } = "";
        public string LastStory { get; set; } = "";
        public string LastPattern { get; set; } = "";
        public string LastSet { get; set; } = "";
        public void Validate()
        {
            if (Version != 1) throw new ArgumentException("Phiên bản cấu hình 2D chưa được hỗ trợ.");
            if (Labels == null || Labels.Any(l => l == null || !Enum.IsDefined(typeof(ElementKind), l.Kind)) || Labels.Select(l => l.Kind).Distinct().Count() != Labels.Length)
                throw new ArgumentException("Cấu hình nhãn cấu kiện không hợp lệ.");
            if (Kind != "Tất cả" && !Enum.GetNames(typeof(ElementKind)).Contains(Kind)) throw new ArgumentException("Loại cấu kiện không hợp lệ.");
            if (HalfDepth == null || !double.TryParse(HalfDepth.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double depth) || !Number.Finite(depth) || depth < 0)
                throw new ArgumentException("Nửa dải xem phải là số không âm.");
            if (LastModelPath == null || LastStory == null || LastPattern == null || LastSet == null) throw new ArgumentException("Cấu hình mô hình không hợp lệ.");
        }
    }
    public static class ViewerPreferencesFile
    {
        public static ViewerPreferences Read(string path)
        {
            var result = JsonCodec.Deserialize<ViewerPreferences>(JsonCodec.Read(path)); result.Validate(); return result;
        }
        public static void Write(string path, ViewerPreferences profile)
        {
            profile.Validate(); string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                JsonCodec.WriteAtomic(temp, JsonCodec.Serialize(profile));
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            } finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
