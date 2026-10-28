using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Views.Controls
{
    public sealed partial class StructuralCanvas
    {
        private void DrawReadLoads(DrawingContext dc,Box2 viewport)
        {
            var rows=ReadLoadItems??Array.Empty<ReadLoadDisplay>();
            var maxima=rows.GroupBy(LoadGlyphs.ScaleGroup).ToDictionary(g=>g.Key,g=>g.Max(r=>Math.Max(Math.Abs(r.Load.Value1),Math.Abs(r.Load.Value2))));
            foreach(var row in rows) {
                Brush brush=row.Category=="Frame"?Brushes.Firebrick:row.Category=="Shell"?Brushes.RoyalBlue:Brushes.DarkViolet;
                var pen=new Pen(brush,1.4);var vector=LoadGlyphs.Direction(row);
                // Plan wall loads are assignment-length strokes. Do not change point loads,
                // horizontal forces, moments or Joint/Shell direction symbols.
                bool planLine=!Elevation&&row.Category=="Frame"&&row.Load.Start!=row.Load.End&&
                    !LoadGlyphs.IsMoment(row)&&vector.HasValue&&Math.Abs(vector.Value.Dot(row.Plane.Normal))>1-1e-6;
                if(planLine) {
                    var lines=row.Geometry.AxisSegments.Select(s=>new Segment2(V(Screen(s.A)),V(Screen(s.B)))).ToArray();
                    var loadPen=new Pen(SelectionBrush,3){StartLineCap=PenLineCap.Flat,EndLineCap=PenLineCap.Flat};
                    foreach(var line in lines)dc.DrawLine(loadPen,new Point(line.A.X,line.A.Y),new Point(line.B.X,line.B.Y));
                    if(ShowLoadValues)DrawLabel(dc,lines,Array.Empty<Vec2>(),false,row.Text,SelectionBrush,viewport,true);
                    continue;
                }
                var samples=LoadGlyphs.Samples(row,scale);
                Point? previousTail=null;Point? labelPoint=null;
                foreach(var sample in samples) {
                    var head=Screen(sample);double value=LoadGlyphs.ValueAt(row,sample);
                    double size=LoadGlyphs.ArrowLength(value,maxima[LoadGlyphs.ScaleGroup(row)]);
                    if(head.X< -160||head.X>ActualWidth+160||head.Y< -160||head.Y>ActualHeight+160){previousTail=null;continue;}
                    if(row.Category=="Joint")DrawCross(dc,pen,head,3);
                    if(!vector.HasValue||size==0){labelPoint=labelPoint??head;continue;}
                    var direction=vector.Value*Math.Sign(value);
                    double x=direction.Dot(row.Plane.U),y=-direction.Dot(row.Plane.V),normal=direction.Dot(row.Plane.Normal),length=Math.Sqrt(x*x+y*y);
                    bool moment=LoadGlyphs.IsMoment(row);
                    if(length>.00001) {
                        // Preserve the projected component magnitude. Arrow head is at the assignment location.
                        var tail=new Point(head.X-x*size,head.Y-y*size);DrawArrow(dc,pen,tail,head,moment);
                        if(!moment&&previousTail.HasValue&&row.Load.Start!=row.Load.End&&row.Category=="Frame")dc.DrawLine(pen,previousTail.Value,tail);
                        previousTail=tail;labelPoint=labelPoint??tail;
                    }
                    if(Math.Abs(normal)>.00001) {
                        if(moment)DrawMoment(dc,pen,head,Math.Max(8,Math.Min(22,size*Math.Abs(normal)/3)),normal>0);
                        else {
                            double radius=Math.Max(3,Math.Min(8,size*Math.Abs(normal)/10));dc.DrawEllipse(null,pen,head,radius,radius);
                            if(normal>0)dc.DrawEllipse(brush,null,head,1.3,1.3);else DrawCross(dc,pen,head,radius*.6);
                        }
                        labelPoint=labelPoint??head;
                    }
                }
                if(ShowLoadValues&&labelPoint.HasValue) {
                    var at=labelPoint.Value;string label=row.Text+(vector.HasValue?"":" · chưa xác định hướng");
                    dc.DrawText(ModelText(label,brush,true),new Point(at.X+7,at.Y-17));
                }
            }
        }
        private static void DrawCross(DrawingContext dc,Pen pen,Point p,double radius)
        { dc.DrawLine(pen,new Point(p.X-radius,p.Y-radius),new Point(p.X+radius,p.Y+radius));dc.DrawLine(pen,new Point(p.X-radius,p.Y+radius),new Point(p.X+radius,p.Y-radius)); }
        private static void DrawArrow(DrawingContext dc,Pen pen,Point tail,Point head,bool doubleHead=false)
        {
            var v=head-tail;if(v.Length<.001)return;double size=Math.Min(6,v.Length*.4);v.Normalize();var side=new Vector(-v.Y,v.X);
            dc.DrawLine(pen,tail,head);dc.DrawLine(pen,head,head-v*size+side*size*.45);dc.DrawLine(pen,head,head-v*size-side*size*.45);
            if(doubleHead){var p=head-v*4;dc.DrawLine(pen,p,p-v*size+side*size*.45);dc.DrawLine(pen,p,p-v*size-side*size*.45);}
        }
        private static void DrawMoment(DrawingContext dc,Pen pen,Point center,double radius,bool positive)
        {
            Point? previous=null;double sign=positive?-1:1;
            for(int i=0;i<=24;i++){double angle=sign*(.25+i*1.5*Math.PI/24);var p=new Point(center.X+radius*Math.Cos(angle),center.Y+radius*Math.Sin(angle));if(previous.HasValue){if(i==24)DrawArrow(dc,pen,previous.Value,p);else dc.DrawLine(pen,previous.Value,p);}previous=p;}
        }
    }
}
