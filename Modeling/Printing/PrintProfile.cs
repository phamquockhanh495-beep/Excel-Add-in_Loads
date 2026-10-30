#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Modeling.Printing
{
    public sealed class PrintProfile
    {
        public int Version { get; set; } = 1;
        public string Paper { get; set; } = "A3";
        public double Width { get; set; } = 420;
        public double Height { get; set; } = 297;
        public bool Landscape { get; set; } = true;
        public double Left { get; set; } = 12;
        public double Right { get; set; } = 12;
        public double Top { get; set; } = 12;
        public double Bottom { get; set; } = 12;
        public bool LockMargins { get; set; }
        public bool Header { get; set; } = true;
        public bool Footer { get; set; } = true;
        public double HeaderHeight { get; set; } = 18;
        public double FooterHeight { get; set; } = 14;
        public double Gap { get; set; } = 4;
        public string HeaderLeft { get; set; } = "{Project}";
        public string HeaderCenter { get; set; } = "LOADING CASE PLAN";
        public string HeaderRight { get; set; } = "{Revision}";
        public string FooterLeft { get; set; } = "{Story} · Z={Elevation} m";
        public string FooterCenter { get; set; } = "{Mode}: {Pattern}{LoadSet}\n{Scale}";
        public string FooterRight { get; set; } = "{Date}\nTrang {Page}/{Pages}";
        public double HeaderFont { get; set; } = 10;
        public double FooterFont { get; set; } = 8;
        public string HeaderColor { get; set; } = "#102A4C";
        public string FooterColor { get; set; } = "#102A4C";
        public bool HeaderBold { get; set; } = true;
        public bool HeaderItalic { get; set; }
        public bool FooterBold { get; set; }
        public bool FooterItalic { get; set; }
        public bool Rules { get; set; } = true;
        public string Project { get; set; } = "";
        public string Revision { get; set; } = "";
        public string Logo { get; set; } = "";
        public bool FullModelPath { get; set; }
        public bool Fit { get; set; } = true;
        public double Scale { get; set; } = 100;
        public bool Tile { get; set; }
        public double Overlap { get; set; } = 5;
        public bool Legend { get; set; } = true;
        public bool LegendRight { get; set; }
        public double LegendSize { get; set; } = 45;
        public double LegendFont { get; set; } = 8;
        public bool Labels { get; set; } = true;
        public bool MemberNames { get; set; } = true;
        public bool Extrude { get; set; } = true;
        public bool Frames { get; set; } = true;
        public bool Shells { get; set; } = true;
        public bool Joints { get; set; } = true;
        public bool Zero { get; set; }
        public bool Combined { get; set; }
        public bool SplitStories { get; set; }
        public void Validate()
        {
            if(Version!=1)throw new ArgumentException("Phiên bản cấu hình in không hỗ trợ.");
            foreach(var v in new[]{Width,Height,Left,Right,Top,Bottom,HeaderHeight,FooterHeight,Gap,Scale,Overlap,LegendSize,HeaderFont,FooterFont,LegendFont})if(!Number.Finite(v))throw new ArgumentException("Thông số in phải là số hữu hạn.");
            if(Width<50||Height<50||Width>3000||Height>3000)throw new ArgumentException("Khổ giấy tùy chỉnh: 50–3000 mm mỗi cạnh.");
            if(new[]{Left,Right,Top,Bottom,Gap,Overlap}.Any(v=>v<0)||HeaderHeight<0||FooterHeight<0)throw new ArgumentException("Margin và khoảng cách không được âm.");
            if(Scale<=0||Scale>1000000||new[]{HeaderFont,FooterFont,LegendFont}.Any(v=>v<6||v>36))throw new ArgumentException("Tỷ lệ phải dương; cỡ chữ in 6–36 pt.");
            if(Legend&&(LegendSize<15||LegendSize>1000))throw new ArgumentException("Vùng chú giải phải từ 15 đến 1000 mm.");
            foreach(var c in new[]{HeaderColor,FooterColor})if(!Regex.IsMatch(c??"","^#[0-9a-fA-F]{6}$"))throw new ArgumentException("Màu chữ phải theo RGB #RRGGBB.");
            foreach(var t in new[]{Paper,HeaderLeft,HeaderCenter,HeaderRight,FooterLeft,FooterCenter,FooterRight,Project,Revision,Logo})if(t==null||t.Length>2000)throw new ArgumentException("Nội dung in không hợp lệ hoặc quá dài.");
            var layout=new PrintLayout(this);
            if(layout.Plan.Width<Pt(20)||layout.Plan.Height<Pt(20))throw new ArgumentException("Margin/header/footer/chú giải chiếm hết vùng vẽ. Cần còn ít nhất 20 × 20 mm.");
            if(Tile&&!Fit&&Pt(Overlap)>=Math.Min(layout.Plan.Width,layout.Plan.Height))throw new ArgumentException("Chồng mép phải nhỏ hơn vùng bản vẽ.");
        }
        public static double Pt(double mm)=>mm*72/25.4;
        public static PrintProfile Read(string path){var p=JsonCodec.Deserialize<PrintProfile>(JsonCodec.Read(path));p.Validate();return p;}
        public void Save(string path){Validate();string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{JsonCodec.WriteAtomic(temp,JsonCodec.Serialize(this));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
    }
    public readonly struct PrintRect
    {
        public double X {get;} public double Y {get;} public double Width {get;} public double Height {get;}
        public PrintRect(double x,double y,double width,double height){X=x;Y=y;Width=width;Height=height;}
    }
    public sealed class PrintLayout
    {
        public double Width {get;} public double Height {get;}
        public PrintRect Body {get;} public PrintRect Plan {get;} public PrintRect Legend {get;} public PrintRect Header {get;} public PrintRect Footer {get;}
        public PrintLayout(PrintProfile p)
        {
            Width=PrintProfile.Pt(p.Landscape?Math.Max(p.Width,p.Height):Math.Min(p.Width,p.Height));Height=PrintProfile.Pt(p.Landscape?Math.Min(p.Width,p.Height):Math.Max(p.Width,p.Height));
            double x=PrintProfile.Pt(p.Left),w=Width-x-PrintProfile.Pt(p.Right),y=PrintProfile.Pt(p.Top),gap=PrintProfile.Pt(p.Gap);
            Header=new PrintRect(x,y,w,p.Header?PrintProfile.Pt(p.HeaderHeight):0);
            Footer=new PrintRect(x,Height-PrintProfile.Pt(p.Bottom)-(p.Footer?PrintProfile.Pt(p.FooterHeight):0),w,p.Footer?PrintProfile.Pt(p.FooterHeight):0);
            double top=y+(p.Header?Header.Height+gap:0),end=Footer.Y-(p.Footer?gap:0);
            Body=new PrintRect(x,top,w,end-top);
            double size=p.Legend?PrintProfile.Pt(p.LegendSize):0;
            Legend=p.LegendRight?new PrintRect(x+w-size,top,size,Body.Height):new PrintRect(x,end-size,w,size);
            Plan=new PrintRect(x,top,w-(p.Legend&&p.LegendRight?size+gap:0),Body.Height-(p.Legend&&!p.LegendRight?size+gap:0));
        }
    }
    public sealed class PrintRequest
    {
        public ModelSnapshot Model {get;}
        public PrintProfile Profile {get;}
        public string[] Stories {get;}
        public string[] Cases {get;}
        public bool Sets {get;}
        public string ModelPath {get;}
        public ViewerPreferences Display {get;}
        public ObjectColorProfile Colors {get;}
        public ViewerTextProfile Text {get;}
        public IReadOnlyDictionary<string,string> PropertyColors {get;}
        public Box2? Crop {get;}
        public string[]? VisibleIds {get;}
        public PrintRequest(ModelSnapshot model,PrintProfile profile,IEnumerable<string> stories,IEnumerable<string> cases,bool sets,string modelPath,ViewerPreferences display,ObjectColorProfile colors,ViewerTextProfile text,IReadOnlyDictionary<string,string>? propertyColors=null,Box2? crop=null,string[]? visibleIds=null)
        {
            Model=model;Profile=JsonCodec.Deserialize<PrintProfile>(JsonCodec.Serialize(profile));Profile.Validate();Stories=stories.Distinct(StringComparer.Ordinal).ToArray();Cases=cases.Distinct(StringComparer.Ordinal).ToArray();Sets=sets;ModelPath=modelPath;
            Display=JsonCodec.Deserialize<ViewerPreferences>(JsonCodec.Serialize(display));Display.Validate();Colors=colors;Text=text;PropertyColors=(propertyColors??new Dictionary<string,string>()).ToDictionary(k=>k.Key,v=>v.Value,StringComparer.Ordinal);Crop=crop;VisibleIds=visibleIds?.ToArray();
            if((long)Stories.Length*(Profile.Combined?1:Cases.Length)>300)throw new ArgumentException("Lô xuất vượt 300 mặt bằng; hãy chia nhỏ danh sách tầng / Pattern.");
            if(Stories.Length==0||Cases.Length==0)throw new ArgumentException("Chọn ít nhất một tầng và một Pattern / Load Set.");
            if(Stories.Any(s=>!Model.Levels.Any(l=>l.Name==s)))throw new ArgumentException("Tầng không tồn tại trong dữ liệu đã đọc.");
            var known=Sets?Model.Loads.Select(ReadLoadDisplay.LoadSetName):Model.LoadPatterns;
            if(Cases.Any(c=>!known.Contains(c,StringComparer.Ordinal)))throw new ArgumentException("Pattern / Load Set không tồn tại.");
            if(!Sets&&!Profile.Frames&&!Profile.Shells&&!Profile.Joints)throw new ArgumentException("Chọn nhóm tải cần xuất.");
            if(crop.HasValue&&(!Number.Finite(crop.Value.Left)||!Number.Finite(crop.Value.Right)||!Number.Finite(crop.Value.Top)||!Number.Finite(crop.Value.Bottom)||crop.Value.Right<=crop.Value.Left||crop.Value.Top<=crop.Value.Bottom))throw new ArgumentException("Vùng xuất hiện tại không hợp lệ.");
        }
    }
}
