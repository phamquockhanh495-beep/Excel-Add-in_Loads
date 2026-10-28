#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public IReadOnlyList<SavedSection> SavedSections { get; private set; }=Array.Empty<SavedSection>();
        private SavedSection? selectedSection;
        public SavedSection? SelectedSection { get=>selectedSection;set { selectedSection=value;Changed();if(value!=null)SectionName=value.Name; } }
        private string sectionName="MC-01";
        public string SectionName { get=>sectionName;set { sectionName=value;Changed(); } }
        public void LoadSections(IEnumerable<SavedSection> sections) { SavedSections=sections.ToArray();SelectedSection=SavedSections.FirstOrDefault();Changed(nameof(SavedSections)); }
        public void BeginNewSection()
        {
            SelectedSection=null;int index=1;while(SavedSections.Any(s=>s.Name.Equals("MC-"+index.ToString("00"),StringComparison.OrdinalIgnoreCase)))index++;
            SectionName="MC-"+index.ToString("00");PreparePlanPointPicking();
        }
        public void SaveCurrentSection()
        {
            if(Busy||Snapshot==null||!CutStart.HasValue||!CutEnd.HasValue)throw new InvalidOperationException("Chọn hai điểm để dựng mặt cắt trước khi lưu.");
            var section=new SavedSection(SectionName,CutStart.Value,CutEnd.Value,Parse(HalfDepth),Reverse);
            SavedSections=SavedSections.Where(s=>!s.Name.Equals(section.Name,StringComparison.OrdinalIgnoreCase)).Concat(new[]{section}).OrderBy(s=>s.Name,StringComparer.OrdinalIgnoreCase).ToArray();
            SelectedSection=section;Changed(nameof(SavedSections));
        }
        public void OpenSelectedSection()
        {
            if(Busy||SelectedSection==null||Snapshot==null)return;
            var section=SelectedSection;HalfDepth=section.HalfDepth.ToString("R",CultureInfo.InvariantCulture);Changed(nameof(HalfDepth));
            reverse=section.Reverse;Changed(nameof(Reverse));SetElevationThrough(section.Start,section.End);SectionName=section.Name;
        }
        public void RemoveSelectedSection()
        {
            if(Busy||SelectedSection==null)return;
            SavedSections=SavedSections.Where(s=>!ReferenceEquals(s,SelectedSection)).ToArray();SelectedSection=SavedSections.FirstOrDefault();Changed(nameof(SavedSections));
        }
    }
}
