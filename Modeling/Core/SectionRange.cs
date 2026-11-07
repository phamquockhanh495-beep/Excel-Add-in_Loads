#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    public static class SectionRange
    {
        public static ProjectedElement? Clip(ProjectedElement? item, ViewPlane plane)
        {
            if(plane.MinU.HasValue)item=ClipAxis(item,plane.MinU.Value,plane.MaxU!.Value,false);
            if(plane.MinV.HasValue)item=ClipAxis(item,plane.MinV.Value,plane.MaxV!.Value,true);
            return item;
        }
        private static ProjectedElement? ClipAxis(ProjectedElement? source,double min,double max,bool vertical)
        {
            if(source==null)return null;
            Vec2 Swap(Vec2 p)=>vertical?new Vec2(p.Y,p.X):p;
            ProjectedElement Map(ProjectedElement e)=>new ProjectedElement(e.Source,e.Segments.Select(s=>new Segment2(Swap(s.A),Swap(s.B))).ToList(),e.Polygon.Select(Swap).ToArray(),e.IsCut,e.Faces.Select(f=>f.Select(Swap).ToArray()).ToArray(),e.AxisSegments.Select(s=>new Segment2(Swap(s.A),Swap(s.B))).ToArray(),e.Voids.Select(f=>f.Select(Swap).ToArray()).ToArray());
            var item=Map(source);
            Segment2? Line(Segment2 s) {
                double dx=s.B.X-s.A.X,lo=0,hi=1;
                if(Math.Abs(dx)<1e-12)return s.A.X>=min-1e-9&&s.A.X<=max+1e-9?(Segment2?)s:null;
                double a=(min-s.A.X)/dx,b=(max-s.A.X)/dx;lo=Math.Max(lo,Math.Min(a,b));hi=Math.Min(hi,Math.Max(a,b));
                return lo>hi?null:(Segment2?)new Segment2(new Vec2(s.A.X+lo*dx,s.A.Y+lo*(s.B.Y-s.A.Y)),new Vec2(s.A.X+hi*dx,s.A.Y+hi*(s.B.Y-s.A.Y)));
            }
            Segment2[] Lines(IEnumerable<Segment2> lines)=>lines.Select(Line).Where(s=>s.HasValue).Select(s=>s!.Value).ToArray();
            Vec2[] Polygon(IReadOnlyList<Vec2> points) {
                var result=points.ToArray();
                foreach(bool lower in new[]{true,false}) {
                    if(result.Length==0)break;double bound=lower?min:max;var next=new List<Vec2>();var a=result[result.Length-1];
                    bool Inside(Vec2 p)=>lower?p.X>=bound:p.X<=bound;
                    foreach(var b in result) { if(Inside(a)!=Inside(b)) { double t=(bound-a.X)/(b.X-a.X);next.Add(new Vec2(bound,a.Y+t*(b.Y-a.Y))); }if(Inside(b))next.Add(b);a=b; }result=next.ToArray();
                }
                return result.Length<3?Array.Empty<Vec2>():result;
            }
            IEnumerable<Segment2> Edges(Vec2[] p)=>p.Select((a,i)=>new Segment2(a,p[(i+1)%p.Length]));
            var polygon=Polygon(item.Polygon);var faces=item.Faces.Select(Polygon).Where(f=>f.Length>2).ToArray();
            var segments=faces.Length>0?faces.SelectMany(Edges).ToList():polygon.Length>2?Edges(polygon).ToList():Lines(item.Segments).ToList();
            if(segments.Count==0)return null;
            bool cut=item.IsCut||item.Segments.Any(s=>s.A.X<min||s.A.X>max||s.B.X<min||s.B.X>max);
            return Map(new ProjectedElement(item.Source,segments,polygon,cut,faces,Lines(item.AxisSegments),item.Voids.Select(Polygon).Where(p=>p.Length>2).ToArray()));
        }
    }
}
