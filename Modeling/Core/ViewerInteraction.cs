#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    public enum SelectionChange { Replace, Add, Toggle }
    public readonly struct Box2
    {
        public double Left { get; }
        public double Right { get; }
        public double Bottom { get; }
        public double Top { get; }
        public Box2(Vec2 a, Vec2 b) { Left = Math.Min(a.X,b.X); Right = Math.Max(a.X,b.X); Bottom = Math.Min(a.Y,b.Y); Top = Math.Max(a.Y,b.Y); }
        public bool Contains(Vec2 p) => p.X >= Left-1e-9 && p.X <= Right+1e-9 && p.Y >= Bottom-1e-9 && p.Y <= Top+1e-9;
        public bool Intersects(Box2 b) => Left <= b.Right && Right >= b.Left && Bottom <= b.Top && Top >= b.Bottom;
        public Box2 Inflate(double amount) => new Box2(new Vec2(Left-amount,Bottom-amount),new Vec2(Right+amount,Top+amount));
    }
    public static class ViewerSelection
    {
        public static string[] Change(IEnumerable<string> current, IEnumerable<string> incoming, SelectionChange change)
        {
            var values = change == SelectionChange.Replace ? new List<string>() : current.Distinct(StringComparer.Ordinal).ToList();
            var members=new HashSet<string>(values,StringComparer.Ordinal);
            foreach (var id in incoming.Distinct(StringComparer.Ordinal)) {
                if(change==SelectionChange.Toggle&&members.Remove(id))continue;
                if(members.Add(id))values.Add(id);
            }
            return values.Where(members.Contains).ToArray();
        }
        public static bool InBox(ProjectedElement element, Box2 box, bool crossing)
        {
            if (!crossing) return element.Segments.Count > 0 && element.Segments.All(s => box.Contains(s.A) && box.Contains(s.B));
            return element.Segments.Any(s => Intersects(s,box)) || element.Polygon.Count > 2 && Contains(element.Polygon,new Vec2(box.Left,box.Bottom));
        }
        public static bool Hit(ProjectedElement element, Vec2 point, double tolerance) =>
            element.Segments.Any(s => Projection.Distance(point,s)<=tolerance) || element.Polygon.Count>2 && Contains(element.Polygon,point);
        public static bool Intersects(Segment2 segment, Box2 box)=>Clip(segment,box).HasValue;
        public static Segment2? Clip(Segment2 segment, Box2 box)
        {
            double lo=0,hi=1,dx=segment.B.X-segment.A.X,dy=segment.B.Y-segment.A.Y;
            bool Clip(double p,double q) { if(Math.Abs(p)<1e-14)return q>=-1e-9; double r=q/p;if(p<0)lo=Math.Max(lo,r);else hi=Math.Min(hi,r);return lo<=hi+1e-9; }
            if(!Clip(-dx,segment.A.X-box.Left)||!Clip(dx,box.Right-segment.A.X)||!Clip(-dy,segment.A.Y-box.Bottom)||!Clip(dy,box.Top-segment.A.Y))return null;
            return new Segment2(new Vec2(segment.A.X+lo*dx,segment.A.Y+lo*dy),new Vec2(segment.A.X+hi*dx,segment.A.Y+hi*dy));
        }
        public static bool Contains(IReadOnlyList<Vec2> polygon,Vec2 point)
        {
            bool inside=false;
            for(int i=0,j=polygon.Count-1;i<polygon.Count;j=i++) {
                var a=polygon[j];var b=polygon[i];
                if(Projection.Distance(point,new Segment2(a,b))<1e-9)return true;
                if((a.Y>point.Y)!=(b.Y>point.Y)&&point.X<(b.X-a.X)*(point.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
            }
            return inside;
        }
    }
    public enum SnapKind { Endpoint, Midpoint, Edge }
    public sealed class ModelSnap
    {
        public Vec2 Point { get; }
        public string ElementId { get; }
        public SnapKind Kind { get; }
        private ModelSnap(Vec2 point,string elementId,SnapKind kind) { Point=point;ElementId=elementId;Kind=kind; }
        public static ModelSnap? Find(IEnumerable<ProjectedElement> elements, Vec2 point, double tolerance)
        {
            var items=elements.ToArray();
            for(int pass=0;pass<3;pass++) {
                ModelSnap? best=null;double distance=tolerance;
                foreach(var item in items) foreach(var segment in item.Segments) {
                    Vec2[] candidates;
                    if(pass==0)candidates=new[]{segment.A,segment.B};
                    else if(pass==1)candidates=new[]{new Vec2((segment.A.X+segment.B.X)/2,(segment.A.Y+segment.B.Y)/2)};
                    else {
                        double dx=segment.B.X-segment.A.X,dy=segment.B.Y-segment.A.Y,d2=dx*dx+dy*dy;
                        double t=d2<1e-20?0:Math.Max(0,Math.Min(1,((point.X-segment.A.X)*dx+(point.Y-segment.A.Y)*dy)/d2));
                        candidates=new[]{new Vec2(segment.A.X+t*dx,segment.A.Y+t*dy)};
                    }
                    foreach(var candidate in candidates) {
                        double d=Math.Sqrt(Math.Pow(candidate.X-point.X,2)+Math.Pow(candidate.Y-point.Y,2));
                        if(d<=distance) { distance=d;best=new ModelSnap(candidate,item.Source.Id,(SnapKind)pass); }
                    }
                }
                if(best!=null)return best;
            }
            return null;
        }
    }
    public static class ElementLabels
    {
        public static string Text(ModelElement element,bool label,bool section,bool uniqueName) => string.Join("\n",new[]{label?element.Label:"",section?element.Property:"",uniqueName?element.Name:""}.Where(s=>!string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal));
        public static Vec2 Anchor(ProjectedElement element)
        {
            var points=element.Polygon;
            if(points.Count>2) {
                double area=0,x=0,y=0;
                for(int i=0;i<points.Count;i++) { var a=points[i];var b=points[(i+1)%points.Count];double cross=a.X*b.Y-b.X*a.Y;area+=cross;x+=(a.X+b.X)*cross;y+=(a.Y+b.Y)*cross; }
                if(Math.Abs(area)>1e-12)return new Vec2(x/(3*area),y/(3*area));
            }
            var line=element.Segments.OrderByDescending(s=>Math.Pow(s.B.X-s.A.X,2)+Math.Pow(s.B.Y-s.A.Y,2)).First();
            return new Vec2((line.A.X+line.B.X)/2,(line.A.Y+line.B.Y)/2);
        }
    }
}
