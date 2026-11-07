#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class ModelGridLine
    {
        public string System {get;} public string Label {get;} public IReadOnlyList<Vec2> Points {get;}
        public bool Infinite {get;} public bool Visible {get;} public bool BubbleStart {get;} public bool BubbleEnd {get;}
        public double? MinZ {get;} public double? MaxZ {get;}
        public ModelGridLine(string system,string label,IEnumerable<Vec2> points,bool infinite=true,bool visible=true,bool bubbleStart=true,bool bubbleEnd=true,double? minZ=null,double? maxZ=null)
        {
            var p=points.ToArray();if(p.Length<2||p.Any(v=>!Number.Finite(v.X)||!Number.Finite(v.Y)))throw new ArgumentException("Tọa độ lưới trục không hợp lệ.");
            System=system;Label=label;Points=Array.AsReadOnly(p);Infinite=infinite;Visible=visible;BubbleStart=bubbleStart;BubbleEnd=bubbleEnd;MinZ=minZ;MaxZ=maxZ;
        }
        public bool At(double z)=>Visible&&(!MinZ.HasValue||z>=MinZ.Value-1e-6)&&(!MaxZ.HasValue||z<=MaxZ.Value+1e-6);
    }
    public sealed class GridDrawing
    {
        public string Label {get;} public Segment2[] Lines {get;} public Vec2[] Bubbles {get;}
        public GridDrawing(string label,IEnumerable<Segment2> lines,IEnumerable<Vec2> bubbles){Label=label;Lines=lines.ToArray();Bubbles=bubbles.ToArray();}
    }
    public static class GridGeometry
    {
        public static Vec2 Transform(Vec2 p,double x,double y,double angle,double metres)
        {double a=angle*Math.PI/180;return new Vec2((x+p.X*Math.Cos(a)-p.Y*Math.Sin(a))*metres,(y+p.X*Math.Sin(a)+p.Y*Math.Cos(a))*metres);}
        public static bool[] Bubbles(string location)
        {
            switch((location??"").Trim().ToUpperInvariant()){
                case "START":case "BOTTOM":case "LEFT":return new[]{true,false};
                case "END":case "TOP":case "RIGHT":return new[]{false,true};
                case "NONE":return new[]{false,false};
                default:return new[]{true,true};
            }
        }
        public static Segment2? Clip(Vec2 a,Vec2 b,Box2 box,bool infinite=false)
        {
            double lo=infinite?double.NegativeInfinity:0,hi=infinite?double.PositiveInfinity:1;
            bool Bound(double start,double delta,double min,double max){if(Math.Abs(delta)<1e-12)return start>=min-1e-9&&start<=max+1e-9;double t1=(min-start)/delta,t2=(max-start)/delta;lo=Math.Max(lo,Math.Min(t1,t2));hi=Math.Min(hi,Math.Max(t1,t2));return lo<=hi;}
            double dx=b.X-a.X,dy=b.Y-a.Y;if(dx*dx+dy*dy<1e-20)return null;
            if(!Bound(a.X,dx,box.Left,box.Right)||!Bound(a.Y,dy,box.Bottom,box.Top)||!Number.Finite(lo)||!Number.Finite(hi))return null;
            return new Segment2(new Vec2(a.X+lo*dx,a.Y+lo*dy),new Vec2(a.X+hi*dx,a.Y+hi*dy));
        }
        public static IReadOnlyList<GridDrawing> Plan(IEnumerable<ModelGridLine> source,Box2 box,double z)
        {
            var grids=source.Where(g=>g.At(z)).ToArray();bool prefix=grids.Select(g=>g.System).Distinct().Count()>1;var result=new List<GridDrawing>();
            foreach(var g in grids){var lines=Enumerable.Range(1,g.Points.Count-1).Select(i=>Clip(g.Points[i-1],g.Points[i],box,g.Infinite)).Where(s=>s.HasValue).Select(s=>s!.Value).ToArray();if(lines.Length==0)continue;
                var bubbles=new List<Vec2>();if(g.BubbleStart)bubbles.Add(lines[0].A);if(g.BubbleEnd&&(!g.BubbleStart||Math.Abs(lines[0].A.X-lines.Last().B.X)+Math.Abs(lines[0].A.Y-lines.Last().B.Y)>1e-6))bubbles.Add(lines.Last().B);
                result.Add(new GridDrawing(prefix?g.System+":"+g.Label:g.Label,lines,bubbles));}
            return result;
        }
        public static IReadOnlyList<GridDrawing> Elevation(IEnumerable<ModelGridLine> source,ViewPlane plane,double minZ,double maxZ)
        {
            var result=new List<GridDrawing>();var grids=source.Where(g=>g.Visible).ToArray();bool prefix=grids.Select(g=>g.System).Distinct().Count()>1;
            foreach(var g in grids){double low=Math.Max(minZ,g.MinZ??minZ),high=Math.Min(maxZ,g.MaxZ??maxZ);if(low>high)continue;var hits=new List<double>();
                for(int i=1;i<g.Points.Count;i++){var a=new Vec3(g.Points[i-1].X,g.Points[i-1].Y,0);var b=new Vec3(g.Points[i].X,g.Points[i].Y,0);double da=plane.Depth(a),db=plane.Depth(b);if(Math.Abs(db-da)<1e-10)continue;double t=-da/(db-da);if(!g.Infinite&&(t<0||t>1))continue;double u=plane.Project(a+(b-a)*t).X;
                    if(plane.MinU.HasValue&&(u<plane.MinU.Value-1e-7||u>plane.MaxU!.Value+1e-7)||hits.Any(v=>Math.Abs(v-u)<1e-6))continue;hits.Add(u);}
                foreach(double u in hits){var top=new Vec2(u,high);result.Add(new GridDrawing(prefix?g.System+":"+g.Label:g.Label,new[]{new Segment2(new Vec2(u,low),top)},new[]{top}));}}
            return result;
        }
    }
}
