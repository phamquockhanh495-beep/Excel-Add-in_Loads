#nullable enable
using System;
using System.ComponentModel;
using System.Globalization;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.ViewModels
{
    public sealed partial class VisualWorkspaceViewModel
    {
        public ObjectColorProfile ObjectColors { get; private set; }=ObjectColorProfile.Default;
        public void ApplyObjectColors(ObjectColorProfile profile)
        {
            ObjectColors=profile??throw new ArgumentNullException(nameof(profile));ColorByProperty=profile.ColorByProperty;
            Changed(nameof(ObjectColors));Changed(nameof(ColorByProperty));
        }
    }
    public sealed class ObjectColorRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ElementKind Kind { get; }
        public string Caption { get; }
        private string red,green,blue;
        public string Red { get=>red;set {red=value;Notify(nameof(Red));} }
        public string Green { get=>green;set {green=value;Notify(nameof(Green));} }
        public string Blue { get=>blue;set {blue=value;Notify(nameof(Blue));} }
        public string Hex { get { try{return ToColor().Hex;}catch(ArgumentException){return "RGB không hợp lệ";} } }
        public string PreviewColor { get {try{return ToColor().Hex;}catch(ArgumentException){return "#E2E8F0";} } }
        public ObjectColorRow(ObjectColor color,string caption)
        { Kind=color.Kind;Caption=caption;red=color.Red.ToString(CultureInfo.InvariantCulture);green=color.Green.ToString(CultureInfo.InvariantCulture);blue=color.Blue.ToString(CultureInfo.InvariantCulture); }
        public ObjectColor ToColor()
        {
            int Parse(string text){if(!int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out int value)||value<0||value>255)throw new ArgumentException(Caption+": RGB phải là số nguyên 0–255.");return value;}
            return new ObjectColor(Kind,Parse(Red),Parse(Green),Parse(Blue));
        }
        private void Notify(string name){foreach(var n in new[]{name,nameof(Hex),nameof(PreviewColor)})PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(n));}
    }
}
