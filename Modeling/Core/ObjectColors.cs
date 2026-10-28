#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class ObjectColor
    {
        public ElementKind Kind { get; }
        public int Red { get; }
        public int Green { get; }
        public int Blue { get; }
        public string Hex=>$"#{Red:X2}{Green:X2}{Blue:X2}";
        public ObjectColor(ElementKind kind,int red,int green,int blue)
        {
            if(!Enum.IsDefined(typeof(ElementKind),kind))throw new ArgumentException("Loại cấu kiện không hợp lệ.");
            if(red<0||red>255||green<0||green>255||blue<0||blue>255)throw new ArgumentException("RGB phải từ 0 đến 255.");
            Kind=kind;Red=red;Green=green;Blue=blue;
        }
        public static ObjectColor Default(ElementKind kind)
        {
            var hex=PropertyColors.ForKind(kind);
            return new ObjectColor(kind,Convert.ToInt32(hex.Substring(1,2),16),Convert.ToInt32(hex.Substring(3,2),16),Convert.ToInt32(hex.Substring(5,2),16));
        }
    }
    public sealed class ObjectColorProfile
    {
        public int Version { get; }
        public bool ColorByProperty { get; }
        public IReadOnlyList<ObjectColor> Colors { get; }
        public ObjectColorProfile(int version,bool colorByProperty,IEnumerable<ObjectColor> colors)
        {
            if(version!=1||colors==null)throw new ArgumentException("Cấu hình Color Objects không hợp lệ.");
            var items=colors.ToArray();if(items.Any(c=>c==null)||items.Select(c=>c.Kind).Distinct().Count()!=items.Length)throw new ArgumentException("Loại cấu kiện bị trùng hoặc thiếu dữ liệu màu.");
            Version=version;ColorByProperty=colorByProperty;Colors=Array.AsReadOnly(items);
        }
        public string ForKind(ElementKind kind)=>Colors.FirstOrDefault(c=>c.Kind==kind)?.Hex??PropertyColors.ForKind(kind);
        public static ObjectColorProfile Default=>new ObjectColorProfile(1,false,Enum.GetValues(typeof(ElementKind)).Cast<ElementKind>().Select(ObjectColor.Default));
    }
}
