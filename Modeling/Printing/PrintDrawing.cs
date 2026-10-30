#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Modeling.Printing
{
    public static class PrintFonts
    {
        private static readonly object Gate=new object();
        private static readonly Dictionary<string,XFont> Cache=new Dictionary<string,XFont>(StringComparer.Ordinal);
        private sealed class Resolver:IFontResolver
        {
            public byte[] GetFont(string name){using(var s=typeof(PrintFonts).Assembly.GetManifestResourceStream("Qks.Fonts."+name+".ttf")??throw new InvalidOperationException("Thiếu font PDF."))using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}}
            public FontResolverInfo? ResolveTypeface(string family,bool bold,bool italic)=>family=="QKS Print"?new FontResolverInfo("DejaVuSans"+(bold?(italic?"-BoldOblique":"-Bold"):(italic?"-Oblique":""))):null;
        }
        public static XFont Font(double size,bool bold=false,bool italic=false){lock(Gate){if(GlobalFontSettings.FontResolver==null)GlobalFontSettings.FontResolver=new Resolver();string key=size.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+bold+":"+italic;if(!Cache.TryGetValue(key,out var font)){font=new XFont("QKS Print",size,(bold?XFontStyleEx.Bold:XFontStyleEx.Regular)|(italic?XFontStyleEx.Italic:XFontStyleEx.Regular),new XPdfFontOptions(PdfFontEncoding.Unicode));Cache.Add(key,font);}return font;}}
        public static double Measure(string text,double size,bool bold=false,bool italic=false){using(var g=XGraphics.CreateMeasureContext(new XSize(1000,1000),XGraphicsUnit.Point,XPageDirection.Downwards))return g.MeasureString(text,Font(size,bold,italic)).Width;}
    }
    public enum PrintMarkKind { Line, Polygon, Text, Clip, Restore, Image }
    public sealed class PrintMark
    {
        public PrintMarkKind Kind {get;set;}
        public Vec2[] Points {get;set;}=Array.Empty<Vec2>();
        public Vec2[][] Holes {get;set;}=Array.Empty<Vec2[]>();
        public string Color {get;set;}="#102A4C";
        public string? Fill {get;set;}
        public double Opacity {get;set;}=1;
        public double Width {get;set;}=.6;
        public bool Dashed {get;set;}
        public string Text {get;set;}="";
        public double Size {get;set;}=8;
        public bool Bold {get;set;}
        public bool Italic {get;set;}
        public double Angle {get;set;}
        public PrintRect Rect {get;set;}
        public byte[]? Image {get;set;}
    }
    public sealed class PrintPage
    {
        public string Story {get;set;}="";
        public string Case {get;set;}="";
        public string Title {get;set;}="";
        public string Scale {get;set;}="";
        public double Elevation {get;set;}
        public double Width {get;set;}
        public double Height {get;set;}
        public List<PrintMark> Marks {get;}=new List<PrintMark>();
        public void Line(Vec2 a,Vec2 b,string color="#102A4C",double width=.6,bool dash=false)=>Marks.Add(new PrintMark{Kind=PrintMarkKind.Line,Points=new[]{a,b},Color=color,Width=width,Dashed=dash});
        public void TextAt(string text,double x,double y,double size=8,string color="#102A4C",bool bold=false,bool italic=false,double angle=0)=>Marks.Add(new PrintMark{Kind=PrintMarkKind.Text,Text=text,Points=new[]{new Vec2(x,y)},Size=size,Color=color,Bold=bold,Italic=italic,Angle=angle});
        public void Rectangle(PrintRect r,string? fill,string stroke="#B9C5D3",double width=.5)=>Marks.Add(new PrintMark{Kind=PrintMarkKind.Polygon,Rect=r,Points=new[]{new Vec2(r.X,r.Y),new Vec2(r.X+r.Width,r.Y),new Vec2(r.X+r.Width,r.Y+r.Height),new Vec2(r.X,r.Y+r.Height)},Fill=fill,Color=stroke,Width=width});
    }
    public sealed class PrintDocument
    {
        public IReadOnlyList<PrintPage> Pages {get;}
        public IReadOnlyList<string> Warnings {get;}
        public PrintDocument(List<PrintPage> pages,IEnumerable<string> warnings){Pages=pages.AsReadOnly();Warnings=warnings.Distinct().ToArray();}
    }
    public static class PlanPdfWriter
    {
        private static XColor Color(string rgb,double opacity=1){int n=Convert.ToInt32(rgb.TrimStart('#'),16);return XColor.FromArgb((int)(Math.Max(0,Math.Min(1,opacity))*255),(n>>16)&255,(n>>8)&255,n&255);}
        public static void Write(string path,IEnumerable<PrintPage> source,CancellationToken token,Action<int,int>? progress=null)
        {
            var pages=source.ToArray();if(pages.Length==0)throw new ArgumentException("Chưa có trang để xuất.");
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                using(var pdf=new PdfDocument()) {
                    pdf.Info.Title="QKS — Loading Case Plan";pdf.Info.Creator="QKS 2D / PDFsharp";
                    for(int i=0;i<pages.Length;i++) {
                        token.ThrowIfCancellationRequested();var page=pdf.AddPage();page.Width=XUnit.FromPoint(pages[i].Width);page.Height=XUnit.FromPoint(pages[i].Height);
                        using(var g=XGraphics.FromPdfPage(page)) {
                            var states=new Stack<XGraphicsState>();
                            foreach(var m in pages[i].Marks){token.ThrowIfCancellationRequested();
                                if(m.Kind==PrintMarkKind.Clip){states.Push(g.Save());g.IntersectClip(new XRect(m.Rect.X,m.Rect.Y,m.Rect.Width,m.Rect.Height));continue;}
                                if(m.Kind==PrintMarkKind.Restore){g.Restore(states.Pop());continue;}
                                var pen=new XPen(Color(m.Color),m.Width);if(m.Dashed)pen.DashStyle=XDashStyle.Dash;
                                if(m.Kind==PrintMarkKind.Line)g.DrawLine(pen,m.Points[0].X,m.Points[0].Y,m.Points[1].X,m.Points[1].Y);
                                else if(m.Kind==PrintMarkKind.Polygon){var p=new XGraphicsPath();p.AddPolygon(m.Points.Select(v=>new XPoint(v.X,v.Y)).ToArray());var state=g.Save();
                                    if(m.Holes.Length>0){g.IntersectClip(p);double x=m.Points.Min(v=>v.X)-1,y=m.Points.Min(v=>v.Y)-1,w=m.Points.Max(v=>v.X)-x+1,h=m.Points.Max(v=>v.Y)-y+1;
                                        foreach(var hole in m.Holes.Where(hole=>hole.Length>2)){var mask=new XGraphicsPath{FillMode=XFillMode.Alternate};mask.AddRectangle(new XRect(x,y,w,h));mask.AddPolygon(hole.Select(v=>new XPoint(v.X,v.Y)).ToArray());g.IntersectClip(mask);}}
                                    g.DrawPath(m.Width>0?pen:null,m.Fill==null?null:new XSolidBrush(Color(m.Fill,m.Opacity)),p);g.Restore(state);}
                                else if(m.Kind==PrintMarkKind.Text){var st=g.Save();g.TranslateTransform(m.Points[0].X,m.Points[0].Y);g.RotateTransform(m.Angle);g.DrawString(m.Text,PrintFonts.Font(m.Size,m.Bold,m.Italic),new XSolidBrush(Color(m.Color)),new XPoint(0,0),XStringFormats.TopLeft);g.Restore(st);}
                                else if(m.Kind==PrintMarkKind.Image&&m.Image!=null){using(var stream=new MemoryStream(m.Image))using(var image=XImage.FromStream(stream))g.DrawImage(image,m.Rect.X,m.Rect.Y,m.Rect.Width,m.Rect.Height);}
                            }
                        }
                        progress?.Invoke(i+1,pages.Length);
                    }
                    token.ThrowIfCancellationRequested();pdf.Save(temp);
                }
                token.ThrowIfCancellationRequested();if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
            } finally {if(File.Exists(temp))File.Delete(temp);}
        }
        // Stage the entire batch first; rollback on failure so cancellation never publishes a partial batch.
        public static void WriteByStory(string folder,PrintDocument document,CancellationToken token,Action<int,int>? progress=null)
        {
            string stage=Path.Combine(folder,".qks-pdf-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
            var moved=new List<string>();var backups=new Dictionary<string,string>();bool keepRecovery=false;
            try {
                var groups=document.Pages.GroupBy(p=>p.Story).ToArray();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var files=new List<string>();int count=0;
                foreach(var group in groups){string name=new string(group.Key.Select(c=>Path.GetInvalidFileNameChars().Contains(c)||"<>:\"/\\|?*".Contains(c)?'_':c).ToArray()).Trim().TrimEnd('.');if(name.Length>80)name=name.Substring(0,80);if(name.Length==0)name="Story";string stem="QKS_"+name;int n=1;name=stem+".pdf";while(!names.Add(name))name=stem+"_"+(++n)+".pdf";files.Add(name);Write(Path.Combine(stage,name),group,token,(a,b)=>progress?.Invoke(count+a,document.Pages.Count));count+=group.Count();}
                foreach(string file in files){token.ThrowIfCancellationRequested();string dest=Path.Combine(folder,file);if(File.Exists(dest)){string backup=Path.Combine(stage,file+".old");File.Move(dest,backup);backups.Add(dest,backup);}File.Move(Path.Combine(stage,file),dest);moved.Add(dest);}
            } catch(Exception failure) {
                var errors=new List<Exception>();
                foreach(var file in moved)try{if(File.Exists(file))File.Delete(file);}catch(Exception ex){errors.Add(ex);}
                foreach(var pair in backups)try{if(File.Exists(pair.Value))File.Move(pair.Value,pair.Key);}catch(Exception ex){errors.Add(ex);}
                if(errors.Count>0){keepRecovery=true;throw new IOException("Chưa phục hồi được mọi file do file bị khóa. Giữ bản sao để phục hồi tại: "+stage,new AggregateException(new[]{failure}.Concat(errors)));}
                throw;
            }
            finally {if(!keepRecovery&&Directory.Exists(stage))Directory.Delete(stage,true);}
        }
    }
}
