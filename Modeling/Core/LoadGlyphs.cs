#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    // API transformation rows are the local basis vectors expressed in Global coordinates.
    public static class LoadGlyphs
    {
        public static bool ValidAxes(IReadOnlyList<Vec3>? axes) => axes!=null&&axes.Count==3&&axes.All(a=>a.IsFinite&&Math.Abs(a.Length-1)<1e-5)&&
            Math.Abs(axes[0].Dot(axes[1]))<1e-5&&Math.Abs(axes[0].Dot(axes[2]))<1e-5&&Math.Abs(axes[1].Dot(axes[2]))<1e-5&&axes[0].Cross(axes[1]).Dot(axes[2])>.99999;
        public static Vec3? Direction(ReadLoadDisplay row)
        {
            var load=row.Load;int index;
            if(load.Direction<1||load.Direction>(row.Category=="Joint"?6:11))return null;
            var global=new[]{new Vec3(1,0,0),new Vec3(0,1,0),new Vec3(0,0,1)};
            if(row.Category=="Joint")index=(load.Direction-1)%3;
            else {
                if(load.Direction==10||load.Direction==11)return new Vec3(0,0,-1);
                index=(load.Direction-1)%3;
                if(load.Direction>=4&&load.Direction<=9) {
                    // X/Y/Z refer to the specified coordinate system; named systems require a transform.
                    return string.Equals(load.CoordinateSystem,"Global",StringComparison.OrdinalIgnoreCase)?global[index]:(Vec3?)null;
                }
            }
            if(index<0||index>2)return null;
            if(string.Equals(load.CoordinateSystem,"Global",StringComparison.OrdinalIgnoreCase)&&row.Category=="Joint")return global[index];
            if((row.Category!="Joint"||string.Equals(load.CoordinateSystem,"Local",StringComparison.OrdinalIgnoreCase))&&ValidAxes(row.Source.LocalAxes))return row.Source.LocalAxes![index];
            return null;
        }
        public static bool IsMoment(ReadLoadDisplay row)=>row.Load.Kind.IndexOf("moment",StringComparison.OrdinalIgnoreCase)>=0;
        public static string ScaleGroup(ReadLoadDisplay row)=>row.Category+"|"+row.Load.Unit+"|"+IsMoment(row);
        public static double ArrowLength(double value,double maximum,double scale=1)
        {
            if(!Number.Finite(value)||!Number.Finite(maximum)||!Number.Finite(scale)||maximum<=0||value==0)return 0;
            return Math.Min(160,Math.Max(6,84*Math.Abs(value)/maximum*Math.Max(.25,Math.Min(2.5,scale))));
        }
        // Interpolate the actual assignment at a clipped point; never stretch the original q1/q2 to a cut segment.
        public static double ValueAt(ReadLoadDisplay row,Vec2 point)
        {
            if(row.Category!="Frame"||row.Load.Start==row.Load.End)return row.Load.Value1;
            var a=row.Plane.Project(row.Source.Vertices[0]);var b=row.Plane.Project(row.Source.Vertices[1]);
            double dx=b.X-a.X,dy=b.Y-a.Y,length=dx*dx+dy*dy;
            if(length<1e-16)return row.Load.Value1;
            double r=((point.X-a.X)*dx+(point.Y-a.Y)*dy)/length;
            double t=Math.Max(0,Math.Min(1,(r-row.Load.Start)/(row.Load.End-row.Load.Start)));
            return row.Load.Value1+(row.Load.Value2-row.Load.Value1)*t;
        }
        public static Vec2[] Samples(ReadLoadDisplay row,double pixelsPerMetre)
        {
            if(row.Category=="Shell"&&row.Geometry.Polygon.Count>2) {
                var polygon=row.Geometry.Polygon;
                // Area centroid, with a fallback to a visible segment for degenerate edge-on shells.
                double area=0,x=0,y=0;
                for(int i=0;i<polygon.Count;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];double cross=a.X*b.Y-b.X*a.Y;area+=cross;x+=(a.X+b.X)*cross;y+=(a.Y+b.Y)*cross;}
                if(Math.Abs(area)>1e-10)return new[]{new Vec2(x/(3*area),y/(3*area))};
            }
            var lines=row.Geometry.AxisSegments;
            if(lines.Count==0)return Array.Empty<Vec2>();
            // For a shell section select its longest analytical intersection, not extruded bottom edges.
            if(row.Category=="Shell")lines=new[]{lines.OrderByDescending(s=>Math.Pow(s.B.X-s.A.X,2)+Math.Pow(s.B.Y-s.A.Y,2)).First()};
            var result=new List<Vec2>();
            foreach(var s in lines) {
                double length=Math.Sqrt(Math.Pow(s.B.X-s.A.X,2)+Math.Pow(s.B.Y-s.A.Y,2));
                int count=length<1e-10?0:Math.Max(1,Math.Min(40,(int)Math.Ceiling(length*pixelsPerMetre/34)));
                for(int i=0;i<=count;i++){double t=count==0?0:(double)i/count;result.Add(new Vec2(s.A.X+(s.B.X-s.A.X)*t,s.A.Y+(s.B.Y-s.A.Y)*t));}
            }
            return result.ToArray();
        }
    }
}
