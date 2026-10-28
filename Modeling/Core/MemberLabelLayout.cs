#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Model-anchored placement: fixed font size and a deterministic offset; never relocate on zoom or overlap.</summary>
    public sealed class MemberLabelPlacement
    {
        public Vec2 Center { get; }
        public double AngleDegrees { get; }
        public double Width { get; }
        public double Height { get; }
        public Box2 Bounds { get; }
        public MemberLabelPlacement(Vec2 center,double angle,double width,double height)
        {
            Center=center;AngleDegrees=angle;Width=width;Height=height;
            double radians=angle*Math.PI/180,c=Math.Abs(Math.Cos(radians)),s=Math.Abs(Math.Sin(radians));
            double x=(c*width+s*height)/2,y=(s*width+c*height)/2;
            Bounds=new Box2(new Vec2(center.X-x,center.Y-y),new Vec2(center.X+x,center.Y+y));
        }
        public bool Intersects(Segment2 segment,double margin=2)
        {
            double a=-AngleDegrees*Math.PI/180,c=Math.Cos(a),s=Math.Sin(a);
            Vec2 Local(Vec2 p) { double x=p.X-Center.X,y=p.Y-Center.Y;return new Vec2(c*x-s*y,s*x+c*y); }
            return ViewerSelection.Intersects(new Segment2(Local(segment.A),Local(segment.B)),new Box2(new Vec2(-Width/2-margin,-Height/2-margin),new Vec2(Width/2+margin,Height/2+margin)));
        }
    }
    public static class MemberLabelLayout
    {
        public static double ReadableAngle(Segment2 segment)
        {
            double angle=Math.Atan2(segment.B.Y-segment.A.Y,segment.B.X-segment.A.X)*180/Math.PI;
            if(angle>=90)angle-=180;if(angle< -90)angle+=180;return angle;
        }
        private static double LengthSquared(Segment2 line)=>Math.Pow(line.B.X-line.A.X,2)+Math.Pow(line.B.Y-line.A.Y,2);
        public static Vec2 Centroid(IReadOnlyList<Vec2> points)
        {
            double area=0,x=0,y=0;
            for(int i=0;i<points.Count;i++) { var a=points[i];var b=points[(i+1)%points.Count];double cross=a.X*b.Y-b.X*a.Y;area+=cross;x+=(a.X+b.X)*cross;y+=(a.Y+b.Y)*cross; }
            return Math.Abs(area)>1e-12?new Vec2(x/(3*area),y/(3*area)):new Vec2(points.Average(p=>p.X),points.Average(p=>p.Y));
        }
        public static MemberLabelPlacement? Place(IReadOnlyList<Segment2> lines,IReadOnlyList<Vec2> polygon,bool area,double width,double height,Box2 viewport)
        {
            Vec2 anchor;double angle;
            // Use the complete projected member, never the viewport-clipped fragment.
            // In particular, an extruded beam polygon must not override its axis direction.
            if(area && polygon.Count>2) { anchor=Centroid(polygon);angle=0; }
            else {
                if(lines.Count==0)return null;
                var line=lines.OrderByDescending(LengthSquared).First();
                anchor=new Vec2((line.A.X+line.B.X)/2,(line.A.Y+line.B.Y)/2);
                angle=area?0:ReadableAngle(line);
            }
            if(!area) {
                double a=angle*Math.PI/180,distance=height/2+4;
                anchor=new Vec2(anchor.X+Math.Sin(a)*distance,anchor.Y-Math.Cos(a)*distance);
            }
            var placed=new MemberLabelPlacement(anchor,angle,width,height);
            // Let the canvas clip text at its real position rather than moving it to an edge.
            return placed.Bounds.Intersects(viewport)?placed:null;
        }
    }
}
