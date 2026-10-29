#nullable enable
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class LabelDisplaySetting : System.ComponentModel.INotifyPropertyChanged
    {
        public ElementKind Kind { get; }
        public string Caption { get; }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private bool visible=true;
        public bool Visible { get=>visible;set { if(visible==value)return;visible=value;PropertyChanged?.Invoke(this,new System.ComponentModel.PropertyChangedEventArgs(nameof(Visible))); } }
        private bool extrude=true, label=true, uniqueName, section;
        public bool Extrude { get=>extrude;set=>Set(ref extrude,value,nameof(Extrude)); }
        public bool Label { get=>label;set=>Set(ref label,value,nameof(Label)); }
        public bool UniqueName { get=>uniqueName;set=>Set(ref uniqueName,value,nameof(UniqueName)); }
        public bool Section { get=>section;set=>Set(ref section,value,nameof(Section)); }
        private void Set(ref bool field,bool value,string name) { if(field==value)return;field=value;PropertyChanged?.Invoke(this,new System.ComponentModel.PropertyChangedEventArgs(name)); }
        public LabelDisplaySetting(ElementKind kind, string caption) { Kind = kind; Caption = caption; }
        public string Text(ModelElement element) => ElementLabels.Text(element, Label, Section, UniqueName);
    }
}
