#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Screen-space placement: fixed text size, readable member angle and a visible fallback.</summary>
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
        private static Vec2[] VisiblePolygon(IReadOnlyList<Vec2> polygon,Box2 view)
        {
            var points=polygon.ToArray();
            for(int edge=0;edge<4&&points.Length>0;edge++) {
                bool vertical=edge<2;double bound=edge==0?view.Left:edge==1?view.Right:edge==2?view.Bottom:view.Top;bool lower=edge==0||edge==2;
                double Coord(Vec2 p)=>vertical?p.X:p.Y;
                bool Inside(Vec2 p)=>lower?Coord(p)>=bound:Coord(p)<=bound;
                var result=new List<Vec2>();var a=points[points.Length-1];bool aInside=Inside(a);
                foreach(var b in points) {
                    bool bInside=Inside(b);
                    if(aInside!=bInside) { double t=(bound-Coord(a))/(Coord(b)-Coord(a));result.Add(new Vec2(a.X+t*(b.X-a.X),a.Y+t*(b.Y-a.Y))); }
                    if(bInside)result.Add(b);a=b;aInside=bInside;
                }
                points=result.ToArray();
            }
            return points;
        }
        public static MemberLabelPlacement? Place(IReadOnlyList<Segment2> lines,IReadOnlyList<Vec2> polygon,bool area,double width,double height,Box2 viewport,Func<MemberLabelPlacement,bool>? blocked=null)
        {
            var visible=lines.Select(l=>ViewerSelection.Clip(l,viewport)).Where(l=>l.HasValue).Select(l=>l!.Value).OrderByDescending(LengthSquared).ToArray();
            var visiblePolygon=polygon.Count>2?VisiblePolygon(polygon,viewport):Array.Empty<Vec2>();
            if(visible.Length==0&&visiblePolygon.Length==0)return null;
            Vec2 anchor;double angle;
            if(visiblePolygon.Length>0) { anchor=Centroid(visiblePolygon);angle=0; }
            else { var line=visible[0];anchor=new Vec2((line.A.X+line.B.X)/2,(line.A.Y+line.B.Y)/2);angle=area?0:ReadableAngle(line); }
            double a=angle*Math.PI/180,nx=Math.Sin(a),ny=-Math.Cos(a),tx=Math.Cos(a),ty=Math.Sin(a);
            var offsets=new List<Vec2>();if(area)offsets.Add(new Vec2(0,0));
            foreach(double gap in new[]{4.0,12,24,38})foreach(int sign in new[]{1,-1}) {
                double distance=height/2+gap;
                offsets.Add(new Vec2(sign*nx*distance,sign*ny*distance));
                offsets.Add(new Vec2(sign*nx*distance+tx*18,sign*ny*distance+ty*18));
                offsets.Add(new Vec2(sign*nx*distance-tx*18,sign*ny*distance-ty*18));
            }
            MemberLabelPlacement? fallback=null;
            foreach(var offset in offsets) {
                var placed=new MemberLabelPlacement(new Vec2(anchor.X+offset.X,anchor.Y+offset.Y),angle,width,height);
                double halfX=(placed.Bounds.Right-placed.Bounds.Left)/2,halfY=(placed.Bounds.Top-placed.Bounds.Bottom)/2;
                double Clamp(double value,double lo,double hi)=>lo>hi?(lo+hi)/2:Math.Max(lo,Math.Min(hi,value));
                placed=new MemberLabelPlacement(new Vec2(Clamp(placed.Center.X,viewport.Left+halfX,viewport.Right-halfX),Clamp(placed.Center.Y,viewport.Bottom+halfY,viewport.Top-halfY)),angle,width,height);
                if(fallback==null)fallback=placed;
                if(blocked?.Invoke(placed)!=true)return placed;
            }
            // Dense views still draw every enabled label; overlap never suppresses a member's text.
            return fallback;
        }
    }
}
