using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QksExcelAddIn.Modeling.Printing;
namespace QksExcelAddIn.Views.Controls
{
    public sealed class PrintPreviewCanvas:FrameworkElement
    {
        public PrintPage? Page {get;private set;}
        private double zoom=.65;
        private static readonly FontFamily Font=new FontFamily(new Uri("pack://application:,,,/QksExcelAddIn;component/"),"./Resources/Fonts/#DejaVu Sans");
        public void Show(PrintPage? page,double value){Page=page;zoom=value;Width=page?.Width*zoom??600;Height=page?.Height*zoom??400;InvalidateVisual();}
        private static Brush Brush(string hex,double opacity=1)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)){Opacity=opacity};
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.White,null,new Rect(RenderSize));if(Page==null)return;dc.PushTransform(new ScaleTransform(zoom,zoom));
            foreach(var m in Page.Marks){
                if(m.Kind==PrintMarkKind.Clip){dc.PushClip(new RectangleGeometry(new Rect(m.Rect.X,m.Rect.Y,m.Rect.Width,m.Rect.Height)));continue;}
                if(m.Kind==PrintMarkKind.Restore){dc.Pop();continue;}
                var pen=new Pen(Brush(m.Color),m.Width);if(m.Dashed)pen.DashStyle=DashStyles.Dash;
                if(m.Kind==PrintMarkKind.Line)dc.DrawLine(pen,new Point(m.Points[0].X,m.Points[0].Y),new Point(m.Points[1].X,m.Points[1].Y));
                else if(m.Kind==PrintMarkKind.Polygon){Geometry Path(QksExcelAddIn.Modeling.Core.Vec2[] ps){var g=new StreamGeometry();using(var context=g.Open()){if(ps.Length>2){context.BeginFigure(new Point(ps[0].X,ps[0].Y),true,true);for(int i=1;i<ps.Length;i++)context.LineTo(new Point(ps[i].X,ps[i].Y),true,false);}}return g;}Geometry geometry=Path(m.Points);foreach(var hole in m.Holes)geometry=new CombinedGeometry(GeometryCombineMode.Exclude,geometry,Path(hole));dc.DrawGeometry(m.Fill==null?null:Brush(m.Fill,m.Opacity),m.Width>0?pen:null,geometry);}
                else if(m.Kind==PrintMarkKind.Text){dc.PushTransform(new TranslateTransform(m.Points[0].X,m.Points[0].Y));dc.PushTransform(new RotateTransform(m.Angle));var text=new FormattedText(m.Text,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(Font,m.Italic?FontStyles.Italic:FontStyles.Normal,m.Bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),m.Size,Brush(m.Color),VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(text,new Point());dc.Pop();dc.Pop();}
                else if(m.Kind==PrintMarkKind.Image&&m.Image!=null){var image=new BitmapImage();using(var stream=new MemoryStream(m.Image)){image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();}dc.DrawImage(image,new Rect(m.Rect.X,m.Rect.Y,m.Rect.Width,m.Rect.Height));}
            }
            dc.Pop();
        }
    }
}
