namespace QksExcelAddIn.Modeling.Core
{
    public sealed class LabelDisplaySetting
    {
        public ElementKind Kind { get; }
        public string Caption { get; }
        public bool Visible { get; set; } = true;
        public bool Extrude { get; set; } = true;
        public bool Label { get; set; } = true;
        public bool UniqueName { get; set; }
        public bool Section { get; set; }
        public LabelDisplaySetting(ElementKind kind, string caption) { Kind = kind; Caption = caption; }
        public string Text(ModelElement element) => ElementLabels.Text(element, Label, Section, UniqueName);
    }
}
