#nullable enable
using System.Globalization;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class DraftOverlay
    {
        public string ElementId { get; }
        public ProjectedElement Geometry { get; }
        public string Text { get; }
        private DraftOverlay(string elementId, ProjectedElement geometry, string text) { ElementId = elementId; Geometry = geometry; Text = text; }
        public static DraftOverlay? Build(DraftLoad draft, ModelElement source, ViewPlane plane)
        {
            ModelElement element = source;
            if (draft.IsLine) {
                var a = source.Vertices[0]; var d = source.Vertices[1] - a;
                element = new ModelElement(source.Id, source.Name, source.Label, source.Story, source.Property, source.Kind,
                    new[] { a + d * draft.Start, a + d * draft.End });
            }
            var projected = Projection.Build(element, plane);
            if (projected == null) return null;
            string values = draft.Value1.ToString("0.###", CultureInfo.CurrentCulture);
            if (draft.IsLine) values += "→" + draft.Value2.ToString("0.###", CultureInfo.CurrentCulture) + " (I→J)";
            return new DraftOverlay(source.Id, projected, draft.Pattern + ": " + values + " " + draft.Unit + " · " + draft.DirectionName);
        }
    }
}
