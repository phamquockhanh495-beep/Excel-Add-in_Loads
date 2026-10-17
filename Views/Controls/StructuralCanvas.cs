using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Views.Controls
{
    // DrawingContext avoids a WPF FrameworkElement per member/label.
    public sealed class StructuralCanvas : FrameworkElement
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(IReadOnlyList<ProjectedElement>), typeof(StructuralCanvas), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SelectedIdProperty = DependencyProperty.Register(nameof(SelectedId), typeof(string), typeof(StructuralCanvas), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(nameof(ShowLabels), typeof(bool), typeof(StructuralCanvas), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty HasOpeningsProperty = DependencyProperty.Register(nameof(HasOpenings), typeof(bool), typeof(StructuralCanvas), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ColorByPropertyProperty = DependencyProperty.Register(nameof(ColorByProperty), typeof(bool), typeof(StructuralCanvas), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ColorOverridesProperty = DependencyProperty.Register(nameof(ColorOverrides), typeof(Dictionary<string, string>), typeof(StructuralCanvas), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DraftOverlaysProperty = DependencyProperty.Register(nameof(DraftOverlays), typeof(IReadOnlyList<DraftOverlay>), typeof(StructuralCanvas), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ShowDraftValuesProperty = DependencyProperty.Register(nameof(ShowDraftValues), typeof(bool), typeof(StructuralCanvas), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        public IReadOnlyList<ProjectedElement>? Items { get => (IReadOnlyList<ProjectedElement>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
        public string SelectedId { get => (string)GetValue(SelectedIdProperty); set => SetValue(SelectedIdProperty, value); }
        public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }
        public bool HasOpenings { get => (bool)GetValue(HasOpeningsProperty); set => SetValue(HasOpeningsProperty, value); }
        public bool ColorByProperty { get => (bool)GetValue(ColorByPropertyProperty); set => SetValue(ColorByPropertyProperty, value); }
        public Dictionary<string, string>? ColorOverrides { get => (Dictionary<string, string>?)GetValue(ColorOverridesProperty); set => SetValue(ColorOverridesProperty, value); }
        public IReadOnlyList<DraftOverlay>? DraftOverlays { get => (IReadOnlyList<DraftOverlay>?)GetValue(DraftOverlaysProperty); set => SetValue(DraftOverlaysProperty, value); }
        public bool ShowDraftValues { get => (bool)GetValue(ShowDraftValuesProperty); set => SetValue(ShowDraftValuesProperty, value); }
        public event Action<ModelElement>? Picked;
        public event Action<Vec2>? PlanePointPicked;
        public bool PickingPlanePoints { get; set; }
        public Vec2? FirstPlanePoint { get; set; }
        private double scale = 50, ox = 100, oy = 300;
        private Point? pan;
        private bool fitted;
        public StructuralCanvas() { ClipToBounds = true; Focusable = true; }
        private Point Screen(Vec2 p) => new Point(ox + p.X * scale, oy - p.Y * scale);
        public void Fit()
        {
            var points = Items?.SelectMany(i => i.Segments).SelectMany(s => new[] { s.A, s.B }).ToArray();
            if (points == null || points.Length == 0 || ActualWidth < 10 || ActualHeight < 10) return;
            double minX = points.Min(p => p.X), maxX = points.Max(p => p.X), minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
            scale = Math.Max(.0001, Math.Min((ActualWidth - 90) / Math.Max(.5, maxX - minX), (ActualHeight - 90) / Math.Max(.5, maxY - minY)));
            ox = ActualWidth / 2 - (minX + maxX) / 2 * scale; oy = ActualHeight / 2 + (minY + maxY) / 2 * scale;
            fitted = true; InvalidateVisual();
        }
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
            if (!fitted && Items?.Count > 0) { Fit(); }
            var items = Items ?? Array.Empty<ProjectedElement>();
            bool openings = HasOpenings || items.Any(i => i.Source.Kind == ElementKind.Opening);
            int labels = 0;
            foreach (var item in items.OrderBy(i => i.Source.Vertices.Count == 2 ? 1 : 0).ThenBy(i => i.Source.Id == SelectedId ? 1 : 0)) {
                string key = item.Source.Kind + "|" + item.Source.Property;
                string hex = ColorByProperty ? ColorOverrides != null && ColorOverrides.TryGetValue(key, out var custom) ? custom : PropertyColors.ForProperty(key) : PropertyColors.ForKind(item.Source.Kind);
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                bool selected = item.Source.Id == SelectedId;
                var pen = new Pen(selected ? Brushes.DarkOrange : brush, selected ? 3.5 : item.Source.Vertices.Count == 2 ? 2 : 1.2);
                if (item.IsCut || item.Source.Kind == ElementKind.Opening) pen.DashStyle = DashStyles.Dash;
                if (!openings && item.Polygon.Count > 2) {
                    var path = new StreamGeometry();
                    using (var g = path.Open()) { g.BeginFigure(Screen(item.Polygon[0]), true, true); g.PolyLineTo(item.Polygon.Skip(1).Select(Screen).ToArray(), true, false); }
                    dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(24, color.R, color.G, color.B)), null, path);
                }
                foreach (var segment in item.Segments) {
                    var a = Screen(segment.A); var b = Screen(segment.B);
                    dc.DrawLine(pen, a, b);
                    if ((a - b).Length < 4) dc.DrawEllipse(null, pen, a, 4, 4);
                }
                if (ShowLabels && labels < 500 && item.Segments.Count > 0) {
                    var s = item.Segments[0]; var p = Screen(new Vec2((s.A.X + s.B.X) / 2, (s.A.Y + s.B.Y) / 2));
                    if (p.X >= 0 && p.Y >= 0 && p.X < ActualWidth && p.Y < ActualHeight) {
                        var text = new FormattedText(string.IsNullOrEmpty(item.Source.Label) ? item.Source.Name : item.Source.Label,
                            System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11,
                            selected ? Brushes.DarkOrange : brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        dc.DrawText(text, new Point(p.X + 5, p.Y - 16)); labels++;
                    }
                }
            }
            int draftLabels = 0;
            foreach (var overlay in DraftOverlays ?? Array.Empty<DraftOverlay>()) {
                var pen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6")), 3) { DashStyle = DashStyles.Dash };
                foreach (var s in overlay.Geometry.Segments) {
                    if (overlay.ElementId == SelectedId) dc.DrawLine(new Pen(Brushes.DarkOrange, 6), Screen(s.A), Screen(s.B));
                    dc.DrawLine(pen, Screen(s.A), Screen(s.B));
                }
                if (ShowDraftValues && draftLabels < 200 && overlay.Geometry.Segments.Count > 0) {
                    var s = overlay.Geometry.Segments[0]; var p = Screen(new Vec2((s.A.X+s.B.X)/2,(s.A.Y+s.B.Y)/2));
                    if (p.X >= 0 && p.Y >= 0 && p.X < ActualWidth && p.Y < ActualHeight) {
                        var text = new FormattedText(overlay.Text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, pen.Brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        dc.DrawText(text, new Point(p.X+5,p.Y+8)); draftLabels++;
                    }
                }
            }
            if (FirstPlanePoint.HasValue) dc.DrawEllipse(Brushes.White, new Pen(Brushes.Teal, 2), Screen(FirstPlanePoint.Value), 6, 6);
            var caption = new FormattedText(items.Count == 0 ? "Chưa có hình học trong vùng xem" : "U →   V ↑     " + (openings ? "Có opening: chỉ vẽ biên area · " : "") + ((DraftOverlays?.Count ?? 0)>0 ? "Tím: NHÁP, chưa ghi" : ""),
                System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, Brushes.SlateGray, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(caption, new Point(14, Math.Max(5, ActualHeight - 28)));
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var p = e.GetPosition(this); double next = Math.Max(.0001, Math.Min(100000, scale * (e.Delta > 0 ? 1.2 : 1 / 1.2))), ratio = next / scale;
            ox = p.X - (p.X - ox) * ratio; oy = p.Y - (p.Y - oy) * ratio; scale = next; InvalidateVisual(); e.Handled = true;
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus(); var p = e.GetPosition(this);
            if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right) { pan = p; CaptureMouse(); Cursor = Cursors.Hand; }
            else if (e.ChangedButton == MouseButton.Left) {
                var world = new Vec2((p.X - ox) / scale, (oy - p.Y) / scale);
                if (PickingPlanePoints) {
                    var vertices = (Items ?? Array.Empty<ProjectedElement>()).SelectMany(i => i.Source.Vertices).Select(v => new Vec2(v.X,v.Y));
                    var nearest = vertices.Select(v => new { Point=v, Distance=(Screen(v)-p).Length }).Where(v => v.Distance<=8).OrderBy(v=>v.Distance).FirstOrDefault();
                    PlanePointPicked?.Invoke(nearest?.Point ?? world); e.Handled = true; return;
                }
                var hits = (Items ?? Array.Empty<ProjectedElement>()).Where(i => i.Segments.Any(s => Projection.Distance(world, s) * scale <= 7)).OrderBy(i => i.Source.Id, StringComparer.Ordinal).ToArray();
                if (hits.Length > 0) {
                    int index = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? Array.FindIndex(hits, i => i.Source.Id == SelectedId) + 1 : 0;
                    Picked?.Invoke(hits[index % hits.Length].Source);
                }
            }
            e.Handled = true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (pan.HasValue) { var p = e.GetPosition(this); ox += p.X - pan.Value.X; oy += p.Y - pan.Value.Y; pan = p; InvalidateVisual(); }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e) { pan = null; ReleaseMouseCapture(); Cursor = PickingPlanePoints ? Cursors.Cross : Cursors.Arrow; }
        protected override void OnLostMouseCapture(MouseEventArgs e) { pan = null; Cursor = PickingPlanePoints ? Cursors.Cross : Cursors.Arrow; }
    }
}
