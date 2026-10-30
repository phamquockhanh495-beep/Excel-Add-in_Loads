#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using PdfSharp.Drawing;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Modeling.Printing
{
    public static partial class PlanPrintBuilder
    {
        private sealed class View
        {
            public ModelLevel Level=null!;public string Case="";public ProjectedElement[] Scene=Array.Empty<ProjectedElement>();
            public ReadLoadDisplay[] Loads=Array.Empty<ReadLoadDisplay>();public SlabLoadRegion[] Regions=Array.Empty<SlabLoadRegion>();
        }
        public static PrintDocument Build(PrintRequest request,CancellationToken token,Action<string>? progress=null)
        {
            var p=request.Profile;p.Validate();var layout=new PrintLayout(p);var pages=new List<PrintPage>();var warnings=new List<string>(request.Model.Diagnostics);
            var views=new List<View>();var chosen=new HashSet<string>(request.Cases,StringComparer.Ordinal);var ids=request.VisibleIds==null?null:new HashSet<string>(request.VisibleIds,StringComparer.Ordinal);
            double depth=double.Parse(request.Display.HalfDepth.Replace(',','.'),CultureInfo.InvariantCulture);
            foreach(var story in request.Stories) {
                token.ThrowIfCancellationRequested();progress?.Invoke("Dựng mặt bằng "+story);
                var level=request.Model.Levels.First(l=>l.Name==story);var plane=ViewPlane.Plan(level.Elevation,depth);var scene=new List<ProjectedElement>();
                foreach(var e in request.Model.Elements){token.ThrowIfCancellationRequested();if(ids!=null&&!ids.Contains(e.Id))continue;var setting=request.Display.Labels.FirstOrDefault(l=>l.Kind==e.Kind);var item=(e.Kind==ElementKind.Column||e.Kind==ElementKind.Wall||p.Extrude&&setting?.Extrude!=false)?SectionExtrusion.Build(e,plane):Projection.Build(e,plane);if(item!=null)scene.Add(item);}
                var holes=request.Model.Elements.Where(e=>e.Kind==ElementKind.Opening).Select(e=>Projection.Build(e,plane)).Where(e=>e!=null).Cast<ProjectedElement>().ToArray();
                var projected=scene.Select(e=>holes.Length>0?SectionExtrusion.WithOpenings(e,holes):e).ToArray();
                var rows=ReadLoadDisplay.Build(request.Model,projected,plane,null,!request.Sets&&p.Frames,request.Sets||p.Shells,!request.Sets&&p.Joints,false,Array.Empty<string>(),request.Sets||p.Zero).Where(r=>chosen.Contains(request.Sets?r.UniformLoadSet:r.Load.Pattern)).ToArray();
                foreach(var selection in p.Combined?new[]{string.Join(" + ",request.Cases)}:request.Cases){var filtered=p.Combined?rows:rows.Where(r=>(request.Sets?r.UniformLoadSet:r.Load.Pattern)==selection).ToArray();views.Add(new View{Level=level,Case=selection,Scene=projected,Loads=filtered,Regions=SlabLoadRegion.Build(filtered,request.Sets).ToArray()});}
            }
            if(views.Count>300)throw new ArgumentException("Lô xuất vượt 300 mặt bằng. Hãy giảm số tầng hoặc Pattern/Set.");
            var maxima=views.SelectMany(v=>v.Loads).GroupBy(LoadGlyphs.ScaleGroup).ToDictionary(g=>g.Key,g=>g.Max(r=>Math.Max(Math.Abs(r.Load.Value1),Math.Abs(r.Load.Value2))));
            byte[]? logo=null;double logoRatio=1;
            if(p.Header&&p.Logo.Length>0){if(!File.Exists(p.Logo)||new FileInfo(p.Logo).Length>10*1024*1024)throw new ArgumentException("Logo không tồn tại hoặc vượt 10 MB.");logo=File.ReadAllBytes(p.Logo);using(var s=new MemoryStream(logo))using(var im=XImage.FromStream(s))logoRatio=im.PointWidth/im.PointHeight;}
            foreach(var view in views) {
                token.ThrowIfCancellationRequested();var points=view.Scene.SelectMany(e=>e.Segments).SelectMany(s=>new[]{s.A,s.B}).ToArray();
                var bounds=request.Crop??(points.Length==0?new Box2(new Vec2(0,0),new Vec2(1,1)):new Box2(new Vec2(points.Min(t=>t.X),points.Min(t=>t.Y)),new Vec2(points.Max(t=>t.X),points.Max(t=>t.Y))));
                // Reserve room for glyphs and edge labels; geometry scale remains physical.
                double pad=Math.Min(65,Math.Min(layout.Plan.Width,layout.Plan.Height)/5),w=Math.Max(.1,bounds.Right-bounds.Left),h=Math.Max(.1,bounds.Top-bounds.Bottom);
                double usableW=layout.Plan.Width-2*pad,usableH=layout.Plan.Height-2*pad;
                double scale=p.Fit?Math.Min(usableW/w,usableH/h):PrintProfile.Pt(1000)/p.Scale;
                int cols=1,rows=1;double overlap=PrintProfile.Pt(p.Overlap);
                if(!p.Fit&&(w*scale>usableW+.01||h*scale>usableH+.01)){
                    if(!p.Tile)throw new ArgumentException(view.Level.Name+" / "+view.Case+": hình vượt giấy ở tỷ lệ 1:"+p.Scale+". Chọn Vừa trang hoặc Ghép trang.");
                    if(overlap>=Math.Min(usableW,usableH))throw new ArgumentException("Chồng mép quá lớn so với vùng vẽ khả dụng.");
                    cols=Math.Max(1,(int)Math.Ceiling((w*scale-overlap)/(usableW-overlap)));rows=Math.Max(1,(int)Math.Ceiling((h*scale-overlap)/(usableH-overlap)));
                }
                if((long)cols*rows+pages.Count>500)throw new ArgumentException("Vượt 500 trang; tăng tỷ lệ hoặc giảm phạm vi xuất.");
                var legend=LegendLines(view,request);
                for(int row=0;row<rows;row++)for(int col=0;col<cols;col++) {
                    var page=NewPage(view,layout);page.Scale="1:"+(PrintProfile.Pt(1000)/scale).ToString("0.##",CultureInfo.InvariantCulture);page.Title=view.Level.Name+" · "+view.Case+(cols*rows>1?" · ô "+(row+1)+"/"+(col+1):"")+(request.Crop.HasValue?" · vùng XY đang xem":"");
                    double ox=cols==1?layout.Plan.X+layout.Plan.Width/2-(bounds.Left+bounds.Right)/2*scale:layout.Plan.X+pad-bounds.Left*scale-col*(usableW-overlap);
                    double oy=rows==1?layout.Plan.Y+layout.Plan.Height/2+(bounds.Top+bounds.Bottom)/2*scale:layout.Plan.Y+pad+bounds.Top*scale-row*(usableH-overlap);
                    DrawPlan(page,view,request,layout.Plan,scale,ox,oy,maxima,token,warnings);
                    if(view.Loads.Length==0)page.TextAt("Không có tải đã đọc trong lựa chọn này",layout.Plan.X+8,layout.Plan.Y+8,9,"#9A3412");
                    if(cols*rows>1||request.Crop.HasValue)page.TextAt(page.Title,layout.Plan.X+8,layout.Plan.Y+layout.Plan.Height-14,7);
                    pages.Add(page);
                    if(p.Legend) {
                        var remaining=DrawLegend(page,legend,layout.Legend,p.LegendFont,request);
                        while(remaining.Count>0){token.ThrowIfCancellationRequested();var extra=NewPage(view,layout);extra.Scale=page.Scale;extra.Title=view.Level.Name+" · "+view.Case+" · Chú giải tiếp";remaining=DrawLegend(extra,remaining,layout.Body,p.LegendFont,request);pages.Add(extra);if(pages.Count>500)throw new ArgumentException("Chú giải vượt giới hạn 500 trang.");}
                    }
                }
            }
            string date=DateTime.Now.ToString("dd/MM/yyyy HH:mm",CultureInfo.InvariantCulture);
            for(int i=0;i<pages.Count;i++){token.ThrowIfCancellationRequested();AddBands(pages[i],request,layout,i+1,pages.Count,date,logo,logoRatio);}
            if(pages.Sum(x=>(long)x.Marks.Count)>2000000)throw new ArgumentException("Bản vẽ quá lớn; hãy xuất theo từng tầng.");
            return new PrintDocument(pages,warnings);
        }
        private static PrintPage NewPage(View v,PrintLayout l)=>new PrintPage{Story=v.Level.Name,Case=v.Case,Elevation=v.Level.Elevation,Width=l.Width,Height=l.Height};
        private sealed class RichLine { public string Text="";public string Prefix="";public string Color="#102A4C";public bool Bold,Italic;public string? Swatch; }
        private static List<RichLine> LegendLines(View view,PrintRequest request)
        {
            var lines=new List<RichLine>{new RichLine{Text=view.Level.Name+" · "+view.Case,Bold=true}};
            foreach(var region in view.Regions.GroupBy(r=>r.Key).Select(g=>g.First()).OrderBy(r=>r.Label)){
                lines.Add(new RichLine{Text=region.Label,Bold=true,Swatch=region.Color});
                foreach(var line in region.LegendLines){lines.Add(new RichLine{Text=line.PatternHeading+line.Value,Prefix=line.PatternHeading,Color=request.Text.Loads.UseColor?request.Text.Loads.Hex:"#4169E1"});lines.Add(new RichLine{Text=line.Axes+line.Source,Italic=true});if(line.LoadSet.Length>0)lines.Add(new RichLine{Text=line.LoadSet.Trim(' ','·'),Bold=true});}
            }
            foreach(var load in view.Loads.Where(r=>r.Source.Kind!=ElementKind.Slab).GroupBy(r=>r.Text).Select(g=>g.First()))lines.Add(new RichLine{Text=load.Text,Color=load.Category=="Frame"?"#800020":load.Category=="Joint"?"#9400D3":"#4169E1"});
            lines.Add(new RichLine{Text="kN · kN/m · kN/m² theo nhãn. ⊗ vào / ⊙ ra mặt phẳng; mũi tên kép/cung tròn: mômen. Kích thước mũi tên chuẩn hóa theo nhóm/đơn vị toàn lô.",Italic=true});
            return lines;
        }
        private static List<RichLine> DrawLegend(PrintPage page,List<RichLine> source,PrintRect rect,double size,PrintRequest request)
        {
            page.Rectangle(rect,"#FFFFFF");double y=rect.Y+6;page.TextAt("CHÚ GIẢI TẢI TRỌNG",rect.X+8,y,size,"#102A4C",true);y+=size*1.5;
            var remainder=new List<RichLine>();bool full=false;
            foreach(var item in source){var wrap=Wrap(item.Text,rect.Width-30,size,item.Bold||item.Prefix.Length>0,item.Italic);
                for(int j=0;j<wrap.Count;j++){
                    if(full||y+size*1.4>rect.Y+rect.Height-6){full=true;remainder.Add(new RichLine{Text=string.Join("\n",wrap.Skip(j)),Bold=item.Bold,Italic=item.Italic,Color=item.Color,Swatch=item.Swatch,Prefix=j==0?item.Prefix:""});break;}
                    if(j==0&&item.Swatch!=null)page.Rectangle(new PrintRect(rect.X+7,y+2,7,7),item.Swatch,item.Swatch);
                    if(j==0&&item.Prefix.Length>0&&wrap[j].StartsWith(item.Prefix,StringComparison.Ordinal)){page.TextAt(item.Prefix,rect.X+20,y,size,"#102A4C",true);page.TextAt(wrap[j].Substring(item.Prefix.Length),rect.X+20+PrintFonts.Measure(item.Prefix,size,true),y,size,item.Color);}
                    else page.TextAt(wrap[j],rect.X+20,y,size,item.Color,item.Bold,item.Italic);y+=size*1.4;
                }
            }
            if(remainder.Count==source.Count&&source.Count>0&&y<=rect.Y+6+size*1.5)throw new ArgumentException("Vùng chú giải quá nhỏ cho cỡ chữ đã chọn.");
            return remainder;
        }
        internal static List<string> Wrap(string text,double width,double size,bool bold=false,bool italic=false)
        {
            if(width<10)throw new ArgumentException("Vùng chữ quá hẹp.");var result=new List<string>();
            foreach(string paragraph in text.Replace("\r","").Split('\n')){
                string line="";foreach(string word in paragraph.Split(' ')){
                    string next=line.Length==0?word:line+" "+word;
                    if(PrintFonts.Measure(next,size,bold,italic)<=width){line=next;continue;}
                    if(line.Length>0){result.Add(line);line="";}
                    foreach(char ch in word){if(line.Length>0&&PrintFonts.Measure(line+ch,size,bold,italic)>width){result.Add(line);line="";}line+=ch;}
                }result.Add(line);
            }
            return result;
        }
        private static void AddBands(PrintPage page,PrintRequest r,PrintLayout l,int number,int count,string date,byte[]? logo,double logoRatio)
        {
            var p=r.Profile;string model=r.ModelPath.Length>0?r.ModelPath:r.Model.Identity;if(!p.FullModelPath)model=model.Replace('\\','/').Split('/').Last();
            string Expand(string s){var values=new Dictionary<string,string>{{"Project",p.Project},{"Model",model},{"Story",page.Story},{"Elevation",page.Elevation.ToString("0.###",CultureInfo.InvariantCulture)},{"Mode",r.Sets?"Load Sets":"Load Pattern"},{"Pattern",r.Sets?"":page.Case},{"LoadSet",r.Sets?page.Case:""},{"Scale",page.Scale},{"Date",date},{"Page",number.ToString()},{"Pages",count.ToString()},{"Revision",p.Revision}};return System.Text.RegularExpressions.Regex.Replace(s,@"\{(\w+)\}",m=>values.TryGetValue(m.Groups[1].Value,out var v)?v:m.Value);}
            void Band(PrintRect rect,string[] fields,double size,string color,bool bold,bool italic,bool header){
                double logoWidth=0;if(header&&logo!=null){double h=Math.Min(rect.Height-4,PrintProfile.Pt(16));if(h<=0)throw new ArgumentException("Header quá thấp cho logo.");logoWidth=Math.Min(PrintProfile.Pt(30),h*logoRatio);h=logoWidth/logoRatio;page.Marks.Add(new PrintMark{Kind=PrintMarkKind.Image,Image=logo,Rect=new PrintRect(rect.X,rect.Y,logoWidth,h)});logoWidth+=6;}
                double width=(rect.Width-logoWidth)/3;
                for(int i=0;i<3;i++){var lines=Wrap(Expand(fields[i]),width-10,size,bold,italic);if(lines.Count*size*1.4>rect.Height-3)throw new ArgumentException((header?"Header":"Footer")+" quá thấp cho nội dung trang "+number+". Tăng chiều cao hoặc giảm chữ.");double y=rect.Y;foreach(string line in lines){double x=rect.X+logoWidth+i*width+(i==0?0:i==1?(width-PrintFonts.Measure(line,size,bold,italic))/2:width-PrintFonts.Measure(line,size,bold,italic));page.TextAt(line,x,y,size,color,bold,italic);y+=size*1.4;}}
                if(p.Rules){double y=header?rect.Y+rect.Height:rect.Y-2;page.Line(new Vec2(rect.X,y),new Vec2(rect.X+rect.Width,y),color,.6);}
            }
            if(p.Header)Band(l.Header,new[]{p.HeaderLeft,p.HeaderCenter,p.HeaderRight},p.HeaderFont,p.HeaderColor,p.HeaderBold,p.HeaderItalic,true);
            if(p.Footer)Band(l.Footer,new[]{p.FooterLeft,p.FooterCenter,p.FooterRight},p.FooterFont,p.FooterColor,p.FooterBold,p.FooterItalic,false);
        }
    }
}
