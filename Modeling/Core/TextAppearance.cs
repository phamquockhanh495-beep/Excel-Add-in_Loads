#nullable enable
using System;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class TextAppearance
    {
        public double Size { get; }
        public int Red { get; }
        public int Green { get; }
        public int Blue { get; }
        public bool UseColor { get; }
        public string Hex=>$"#{Red:X2}{Green:X2}{Blue:X2}";
        public TextAppearance(double size,int red,int green,int blue,bool useColor=true)
        {
            if(!Number.Finite(size)||size<6||size>30)throw new ArgumentException("Cỡ chữ phải từ 6 đến 30 DIP.");
            if(red<0||red>255||green<0||green>255||blue<0||blue>255)throw new ArgumentException("RGB phải là số nguyên từ 0 đến 255.");
            Size=size;Red=red;Green=green;Blue=blue;UseColor=useColor;
        }
        public static TextAppearance StructureDefault=>new TextAppearance(10.5,15,34,64);
        public static TextAppearance LoadsDefault=>new TextAppearance(10,178,34,34,false);
    }
    public sealed class ViewerTextProfile
    {
        public TextAppearance Structure { get; }
        public TextAppearance Loads { get; }
        public ViewerTextProfile(TextAppearance structure,TextAppearance loads)
        { Structure=structure??throw new ArgumentNullException(nameof(structure));Loads=loads??throw new ArgumentNullException(nameof(loads)); }
    }
}
