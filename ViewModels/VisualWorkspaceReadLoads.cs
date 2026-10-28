#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public const string AllReadPatterns="(Tất cả pattern)";
        public IEnumerable<string> ReadPatterns => new[]{AllReadPatterns}.Concat(Snapshot?.LoadPatterns??Array.Empty<string>());
        private string readPattern=AllReadPatterns;
        public string ReadPattern { get=>readPattern;set { readPattern=value??AllReadPatterns;Changed();RefreshReadLoads(); } }
        private bool showFrameLoads,showShellLoads,showJointLoads,onlySelectedLoads,showZeroLoads;
        private bool showLoadValues=true;
        public bool ShowFrameLoads { get=>showFrameLoads;set { showFrameLoads=value;Changed();RefreshReadLoads(); } }
        public bool ShowShellLoads { get=>showShellLoads;set { showShellLoads=value;Changed();RefreshReadLoads(); } }
        public bool ShowJointLoads { get=>showJointLoads;set { showJointLoads=value;Changed();RefreshReadLoads(); } }
        public bool OnlySelectedLoads { get=>onlySelectedLoads;set { onlySelectedLoads=value;Changed();RefreshReadLoads(); } }
        public bool ShowZeroLoads { get=>showZeroLoads;set { showZeroLoads=value;Changed();RefreshReadLoads(); } }
        public bool ShowLoadValues { get=>showLoadValues;set { showLoadValues=value;Changed(); } }
        public IReadOnlyList<ReadLoadDisplay> ReadLoadItems { get; private set; }=Array.Empty<ReadLoadDisplay>();
        public string ReadLoadSummary => !(ShowFrameLoads||ShowShellLoads||ShowJointLoads)?"Tick nhóm tải để hiển thị trên hình.":
            ReadLoadItems.Count+" thành phần tải · Frame "+ReadLoadItems.Count(l=>l.Category=="Frame")+" / Shell "+ReadLoadItems.Count(l=>l.Category=="Shell")+" / Joint "+ReadLoadItems.Count(l=>l.Category=="Joint")+(Snapshot?.Diagnostics.Any(d=>d.Contains("(API "))==true?" · Có cảnh báo đọc API":"");
        public string ReadSourceSummary { get {
            var rows=(Snapshot?.Loads??Array.Empty<ExistingLoad>()).Where(l=>(ReadPattern==AllReadPatterns||l.Pattern==ReadPattern)&&(ShowZeroLoads||l.Value1!=0||l.Value2!=0)).ToArray();
            return "Nguồn / pattern: Frame "+rows.Count(l=>l.ElementId.StartsWith("Frame:"))+" · Shell "+rows.Count(l=>l.ElementId.StartsWith("Area:"))+" · Joint "+rows.Count(l=>l.ElementId.StartsWith("Joint:"))+". Bảng bên dưới chỉ gồm vùng đang xem.";
        } }
        public string LoadReadNote => "Hỗ trợ đọc: frame distributed/point, shell uniform/uniform-to-frame và Uniform Load Sets, joint lực/mômen. Chưa gồm tải tự sinh, nhiệt độ, surface pressure hoặc shell nonuniform. Tải nút liệt kê theo từng thành phần và step.";
        private void RefreshReadLoads()
        {
            try { ReadLoadItems=ReadLoadDisplay.Build(Snapshot,Scene,CurrentPlane(),ReadPattern==AllReadPatterns?null:ReadPattern,ShowFrameLoads,ShowShellLoads,ShowJointLoads,OnlySelectedLoads,SelectedIds,ShowZeroLoads); }
            catch(ArgumentException) { ReadLoadItems=Array.Empty<ReadLoadDisplay>(); }
            Changed(nameof(ReadLoadItems));Changed(nameof(ReadLoadSummary));Changed(nameof(ReadSourceSummary));
        }
        private void ResetReadLoads()
        {
            if(!ReadPatterns.Contains(readPattern))readPattern=AllReadPatterns;
            Changed(nameof(ReadPatterns));Changed(nameof(ReadPattern));RefreshReadLoads();
        }
    }
}
