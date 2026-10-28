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
        public bool Extrude { get; set; } = true;
        public bool Label { get; set; } = true;
        public bool UniqueName { get; set; }
        public bool Section { get; set; }
        public LabelDisplaySetting(ElementKind kind, string caption) { Kind = kind; Caption = caption; }
        public string Text(ModelElement element) => ElementLabels.Text(element, Label, Section, UniqueName);
    }
}
