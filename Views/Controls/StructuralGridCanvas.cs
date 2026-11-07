using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Views.Controls
{
    public sealed partial class StructuralCanvas
    {
        public static readonly DependencyProperty GridItemsProperty=Register(nameof(GridItems),typeof(IReadOnlyList<GridDrawing>));
        public IReadOnlyList<GridDrawing>? GridItems {get=>(IReadOnlyList<GridDrawing>?)GetValue(GridItemsProperty);set=>SetValue(GridItemsProperty,value);}
        private void DrawGridLines(DrawingContext dc)
        {
            var pen=new Pen(Brushes.SlateGray,.65){DashStyle=DashStyles.Dash};
            foreach(var grid in GridItems??Array.Empty<GridDrawing>())foreach(var s in grid.Lines)dc.DrawLine(pen,Screen(s.A),Screen(s.B));
        }
        private void DrawGridBubbles(DrawingContext dc)
        {
            foreach(var grid in GridItems??Array.Empty<GridDrawing>())foreach(var p in grid.Bubbles){var at=Screen(p);var text=Text(grid.Label,Brushes.DarkSlateGray,10);double r=Math.Max(10,Math.Max(text.Width,text.Height)/2+3);
                if(at.X+r<0||at.X-r>ActualWidth||at.Y+r<0||at.Y-r>ActualHeight)continue;
                dc.DrawEllipse(Brushes.White,new Pen(Brushes.SlateGray,.8),at,r,r);dc.DrawText(text,new Point(at.X-text.Width/2,at.Y-text.Height/2));}
        }
    }
}
