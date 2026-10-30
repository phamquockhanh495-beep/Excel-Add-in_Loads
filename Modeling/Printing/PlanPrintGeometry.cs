#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Modeling.Printing
{
    public static partial class PlanPrintBuilder
    {
        private static void DrawPlan(PrintPage page,View view,PrintRequest request,PrintRect rect,double scale,double ox,double oy,Dictionary<string,double> maxima,CancellationToken token,List<string> warnings)
        {
            Vec2 Map(Vec2 p)=>new Vec2(ox+p.X*scale,oy-p.Y*scale);
            var viewport=new Box2(new Vec2(rect.X,rect.Y),new Vec2(rect.X+rect.Width,rect.Y+rect.Height));
            page.Rectangle(rect,"#FFFFFF");page.Marks.Add(new PrintMark{Kind=PrintMarkKind.Clip,Rect=rect});
            var slabs=view.Regions.ToDictionary(r=>r.ElementId);var loadedIds=new HashSet<string>(view.Loads.Select(r=>r.ElementId),StringComparer.Ordinal);var labels=new List<Action>();
            var settings=request.Display.Labels.ToDictionary(l=>l.Kind);
            foreach(var item in view.Scene.OrderBy(i=>i.Source.Kind==ElementKind.Slab?0:i.Source.Kind==ElementKind.Wall?1:2)){
                token.ThrowIfCancellationRequested();var source=item.Source;settings.TryGetValue(source.Kind,out var setting);
                var lines=item.Segments.Select(s=>new Segment2(Map(s.A),Map(s.B))).ToArray();
                var polygon=item.Polygon.Select(Map).ToArray();
                if(!lines.Any(s=>ViewerSelection.Intersects(s,viewport))&&!(polygon.Length>2&&ViewerSelection.Contains(polygon,new Vec2(rect.X+rect.Width/2,rect.Y+rect.Height/2))))continue;
                string key=source.Kind+"|"+source.Property;
                string color=request.Display.ColorByProperty?request.PropertyColors.TryGetValue(key,out var custom)?custom:PropertyColors.ForProperty(key):request.Colors.ForKind(source.Kind);
                bool loaded=slabs.TryGetValue(source.Id,out var region);if(loaded)color=region!.Color;
                bool outline=source.Kind==ElementKind.Opening||(source.Kind==ElementKind.Column||source.Kind==ElementKind.Wall)&&(!request.Profile.Extrude||setting?.Extrude==false);
                if(!outline){var faces=item.Faces.Count>0?item.Faces:polygon.Length>2?new[]{item.Polygon.ToArray()}:Array.Empty<Vec2[]>();foreach(var face in faces)page.Marks.Add(new PrintMark{Kind=PrintMarkKind.Polygon,Points=face.Select(Map).ToArray(),Holes=item.Voids.Select(h=>h.Select(Map).ToArray()).ToArray(),Fill=color,Color=color,Opacity=source.Kind==ElementKind.Slab?(loaded?.43:.19):.43,Width=0});}
                if(source.Kind!=ElementKind.Slab)foreach(var line in lines)page.Line(line.A,line.B,color,.65,item.IsCut&&item.Faces.Count==0||source.Kind==ElementKind.Opening);
                if(source.Kind==ElementKind.Joint&&lines.Length>0)Cross(page,lines[0].A,color,2);
                string text=loaded?(request.Profile.Labels?region!.Label:""):request.Profile.MemberNames?ElementLabels.Text(source,setting?.Label??true,setting?.Section??false,setting?.UniqueName??false):"";
                if(text.Length>0){var axis=item.AxisSegments.Select(s=>new Segment2(Map(s.A),Map(s.B))).ToArray();bool area=item.Polygon.Count>2&&(source.Kind==ElementKind.Slab||source.Kind==ElementKind.Column||source.Kind==ElementKind.Opening);var style=loaded?request.Text.Loads:request.Text.Structure;var labelPolygon=polygon;
                    if(area&&polygon.Length>2&&item.Voids.Count>0){var anchor=MemberLabelLayout.Centroid(polygon);var holes=item.Voids.Select(h=>h.Select(Map).ToArray()).ToArray();
                        if(holes.Any(h=>ViewerSelection.Contains(h,anchor))){var candidates=Enumerable.Range(1,19).SelectMany(a=>Enumerable.Range(1,19).Select(b=>new Vec2(polygon.Min(v=>v.X)+(polygon.Max(v=>v.X)-polygon.Min(v=>v.X))*a/20,polygon.Min(v=>v.Y)+(polygon.Max(v=>v.Y)-polygon.Min(v=>v.Y))*b/20))).Where(v=>ViewerSelection.Contains(polygon,v)&&!holes.Any(h=>ViewerSelection.Contains(h,v))).OrderBy(v=>Math.Pow(v.X-anchor.X,2)+Math.Pow(v.Y-anchor.Y,2)).ToArray();
                            if(candidates.Length>0){var v=candidates[0];labelPolygon=new[]{new Vec2(v.X-1,v.Y-1),new Vec2(v.X+1,v.Y-1),new Vec2(v.X+1,v.Y+1),new Vec2(v.X-1,v.Y+1)};}}
                    }
                    labels.Add(()=>Label(page,axis,labelPolygon,area,text,style.Size*.75,style.UseColor?style.Hex:"#102240",viewport,warnings,!loaded&&request.Profile.Labels&&loadedIds.Contains(source.Id)));}
            }
            foreach(var action in labels)action();
            foreach(var row in view.Loads){token.ThrowIfCancellationRequested();if(row.Source.Kind==ElementKind.Slab)continue;
                var direction=LoadGlyphs.Direction(row);string color=row.Category=="Frame"?"#800020":row.Category=="Joint"?"#9400D3":"#4169E1";
                string textColor=request.Text.Loads.UseColor?request.Text.Loads.Hex:color;
                bool lineLoad=row.Category=="Frame"&&row.Load.Start!=row.Load.End&&!LoadGlyphs.IsMoment(row)&&direction.HasValue&&Math.Abs(direction.Value.Dot(row.Plane.Normal))>1-1e-6;
                if(lineLoad){var lines=row.Geometry.AxisSegments.Select(s=>new Segment2(Map(s.A),Map(s.B))).ToArray();foreach(var line in lines)page.Line(line.A,line.B,color,2);if(request.Profile.Labels)Label(page,lines,Array.Empty<Vec2>(),false,row.PlanLineLabel,request.Text.Loads.Size*.75,textColor,viewport,warnings);continue;}
                Vec2? label=null,previous=null;
                foreach(var at in LoadGlyphs.Samples(row,scale/.75)){
                    var head=Map(at);if(!viewport.Inflate(100).Contains(head)){previous=null;continue;}double value=LoadGlyphs.ValueAt(row,at);double size=LoadGlyphs.ArrowLength(value,maxima[LoadGlyphs.ScaleGroup(row)])*.75;
                    if(row.Category=="Joint")Cross(page,head,color,2);
                    if(!direction.HasValue||size==0){label=label??head;continue;}
                    var d=direction.Value*Math.Sign(value);double x=d.Dot(row.Plane.U),y=-d.Dot(row.Plane.V),z=d.Dot(row.Plane.Normal);bool moment=LoadGlyphs.IsMoment(row);
                    if(Math.Sqrt(x*x+y*y)>1e-6){var tail=new Vec2(head.X-x*size,head.Y-y*size);Arrow(page,tail,head,color,moment);if(!moment&&previous.HasValue&&row.Category=="Frame"&&row.Load.Start!=row.Load.End)page.Line(previous.Value,tail,color);previous=tail;label=label??tail;}
                    if(Math.Abs(z)>1e-6){double radius=Math.Max(3,Math.Min(moment?16:6,size*Math.Abs(z)/(moment?3:10)));if(moment){Vec2? prev=null;for(int k=0;k<=24;k++){double a=(z>0?-1:1)*(.25+k*1.5*Math.PI/24);var p=new Vec2(head.X+radius*Math.Cos(a),head.Y+radius*Math.Sin(a));if(prev.HasValue){if(k==24)Arrow(page,prev.Value,p,color,false);else page.Line(prev.Value,p,color);}prev=p;}}else{Circle(page,head,radius,color);if(z>0)Circle(page,head,1,color);else Cross(page,head,color,radius*.6);}label=label??head;}
                }
                if(!direction.HasValue)warnings.Add("Chưa xác định hướng tải: "+row.ElementId+" / "+row.Load.Pattern+". PDF chỉ ghi giá trị, không suy diễn mũi tên.");
                if(request.Profile.Labels&&label.HasValue)page.TextAt(row.Text+(direction.HasValue?"":" · chưa xác định hướng"),label.Value.X+5,label.Value.Y-12,request.Text.Loads.Size*.75,textColor);
            }
            page.Marks.Add(new PrintMark{Kind=PrintMarkKind.Restore});
        }
        private static void Label(PrintPage page,IReadOnlyList<Segment2> lines,IReadOnlyList<Vec2> polygon,bool area,string text,double size,string color,Box2 viewport,List<string> warnings,bool below=false)
        {
            double width=PrintFonts.Measure(text,size),height=size*1.4;var placed=MemberLabelLayout.Place(lines,polygon,area,width,height,viewport);if(placed==null)return;
            if(below&&!area){double rad=placed.AngleDegrees*Math.PI/180;placed=new MemberLabelPlacement(new Vec2(placed.Center.X-Math.Sin(rad)*(height+8),placed.Center.Y+Math.Cos(rad)*(height+8)),placed.AngleDegrees,width,height);}
            if(!viewport.Contains(new Vec2(placed.Bounds.Left,placed.Bounds.Bottom))||!viewport.Contains(new Vec2(placed.Bounds.Right,placed.Bounds.Top)))warnings.Add("Một số nhãn chạm biên vùng in. Kiểm tra xem trước, tăng khổ giấy hoặc giảm cỡ chữ; giá trị vẫn có trong chú giải.");
            double a=placed.AngleDegrees*Math.PI/180;page.TextAt(text,placed.Center.X-width/2*Math.Cos(a)+height/2*Math.Sin(a),placed.Center.Y-width/2*Math.Sin(a)-height/2*Math.Cos(a),size,color,false,false,placed.AngleDegrees);
        }
        private static void Cross(PrintPage p,Vec2 c,string color,double r){p.Line(new Vec2(c.X-r,c.Y-r),new Vec2(c.X+r,c.Y+r),color);p.Line(new Vec2(c.X-r,c.Y+r),new Vec2(c.X+r,c.Y-r),color);}
        private static void Circle(PrintPage p,Vec2 c,double r,string color){for(int i=0;i<20;i++)p.Line(new Vec2(c.X+r*Math.Cos(i*Math.PI/10),c.Y+r*Math.Sin(i*Math.PI/10)),new Vec2(c.X+r*Math.Cos((i+1)*Math.PI/10),c.Y+r*Math.Sin((i+1)*Math.PI/10)),color);}
        private static void Arrow(PrintPage p,Vec2 tail,Vec2 head,string color,bool twice)
        {
            double x=head.X-tail.X,y=head.Y-tail.Y,len=Math.Sqrt(x*x+y*y);if(len<.001)return;double s=Math.Min(4.5,len*.4);x/=len;y/=len;p.Line(tail,head,color);
            for(int i=0;i<(twice?2:1);i++){var at=new Vec2(head.X-i*x*3,head.Y-i*y*3);p.Line(at,new Vec2(at.X-x*s-y*s*.45,at.Y-y*s+x*s*.45),color);p.Line(at,new Vec2(at.X-x*s+y*s*.45,at.Y-y*s-x*s*.45),color);}
        }
    }
}
