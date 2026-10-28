#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class SavedSection
    {
        public string Name { get; }
        public Vec2 Start { get; }
        public Vec2 End { get; }
        public double HalfDepth { get; }
        public bool Reverse { get; }
        public SavedSection(string name,Vec2 start,Vec2 end,double halfDepth,bool reverse)
        {
            name=(name??"").Trim();if(name.Length==0||name.Length>80)throw new ArgumentException("Tên mặt cắt phải có 1–80 ký tự.");
            ViewPlane.ElevationThrough(start,end,halfDepth,reverse);Name=name;Start=start;End=end;HalfDepth=halfDepth;Reverse=reverse;
        }
        public override string ToString()=>Name;
    }
    public sealed class SectionCatalog
    {
        public int Version { get; }
        public string ModelPath { get; }
        public IReadOnlyList<SavedSection> Sections { get; }
        public SectionCatalog(int version,string modelPath,IEnumerable<SavedSection> sections)
        {
            if(version!=1||string.IsNullOrWhiteSpace(modelPath))throw new ArgumentException("Thông tin danh mục mặt cắt không hợp lệ.");
            var values=sections.ToArray();if(values.Length>2000||values.Any(s=>s==null)||values.Select(s=>s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=values.Length)throw new ArgumentException("Tên mặt cắt trùng hoặc danh mục quá lớn.");
            Version=version;ModelPath=modelPath;Sections=Array.AsReadOnly(values);
        }
    }
}
