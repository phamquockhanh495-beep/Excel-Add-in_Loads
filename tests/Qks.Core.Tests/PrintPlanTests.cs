using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PdfSharp.Pdf.IO;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Modeling.Printing;
internal static partial class Program
{
    private static ModelSnapshot PdfFixture()
    {
        var elements=new List<ModelElement>();var loads=new List<ExistingLoad>();
        for(int i=1;i<=2;i++){
            double z=i*3;string story="Tầng "+i;
            elements.Add(new ModelElement("Area:"+i,"S"+i,"F"+i,story,"S175",ElementKind.Slab,new[]{new Vec3(0,0,z),new Vec3(12,0,z),new Vec3(12,8,z),new Vec3(0,8,z)},section:new SectionGeometry("Shell",.175,.175)));
            elements.Add(new ModelElement("Area:H"+i,"H"+i,"",story,"",ElementKind.Opening,new[]{new Vec3(5,3,z),new Vec3(7,3,z),new Vec3(7,5,z),new Vec3(5,5,z)}));
            elements.Add(new ModelElement("Frame:"+i,"B"+i,"B"+i,story,"B300x600",ElementKind.Beam,new[]{new Vec3(0,0,z),new Vec3(12,0,z)},localAxisDegrees:0,section:new SectionGeometry("Rectangle",.3,.6)));
            elements.Add(new ModelElement("Frame:C"+i,"C"+i,"C"+i,story,"C400",ElementKind.Column,new[]{new Vec3(12,8,z-3),new Vec3(12,8,z)},localAxisDegrees:0,section:new SectionGeometry("Rectangle",.4,.4)));
            elements.Add(new ModelElement("Joint:"+i,"J"+i,"",story,"",ElementKind.Joint,new[]{new Vec3(9,6,z)}));
            loads.Add(new ExistingLoad("Area:"+i,"FL","Area uniform set: Residential","Global",10,0,1,1.5,1.5,"kN/m²"));
            loads.Add(new ExistingLoad("Area:"+i,"LLAB","Area uniform set: Residential","Global",10,0,1,2,2,"kN/m²"));
            loads.Add(new ExistingLoad("Frame:"+i,"WL","Frame force/length","Global",10,.2,.8,3,5,"kN/m"));
            loads.Add(new ExistingLoad("Joint:"+i,"WX","Joint force","Global",1,0,0,i*100,i*100,"kN"));
        }
        return new ModelSnapshot(1,"ETABS","22","PDF DEMO — DỮ LIỆU MINH HỌA","kN_m_C",DateTime.UtcNow,elements,new[]{new ModelLevel("Tầng 1",3),new ModelLevel("Tầng 2",6)},loads,Array.Empty<string>());
    }
    private static PrintRequest PdfRequest(PrintProfile p=null,string[] stories=null,string[] cases=null,bool sets=false,ModelSnapshot model=null)
    {
        model=model??PdfFixture();return new PrintRequest(model,p??new PrintProfile(),stories??model.Levels.Select(l=>l.Name).ToArray(),cases??new[]{"FL","WL","WX"},sets,"C:\\DEMO\\Thử nghiệm.EDB",new ViewerPreferences(),ObjectColorProfile.Default,new ViewerTextProfile(TextAppearance.StructureDefault,TextAppearance.LoadsDefault));
    }
    private static void PrintPlanTests()
    {
        Check("print profile validates margins, paper, fonts and remaining plan area",()=>{
            new PrintProfile().Validate();foreach(double n in new[]{double.NaN,double.PositiveInfinity,-1,40,3001})Throws<ArgumentException>(()=>new PrintProfile{Width=n}.Validate());
            Throws<ArgumentException>(()=>new PrintProfile{Left=400}.Validate());Throws<ArgumentException>(()=>new PrintProfile{HeaderFont=2}.Validate());Throws<ArgumentException>(()=>new PrintProfile{HeaderColor="red"}.Validate());
            var l=new PrintLayout(new PrintProfile{Width=210,Height=297,Landscape=false});Near(l.Width,210*72/25.4);Near(l.Height,297*72/25.4);
        });
        Check("PDF request snapshots profile and rejects missing floor or pattern",()=>{
            var p=new PrintProfile();var r=PdfRequest(p);p.Width=500;Assert(r.Profile.Width==420);Throws<ArgumentException>(()=>PdfRequest(stories:new[]{"Missing"}));Throws<ArgumentException>(()=>PdfRequest(cases:new[]{"Missing"}));
        });
        var profile=new PrintProfile{Combined=true,Project="DỮ LIỆU MINH HỌA · CÔNG TRÌNH THỬ",HeaderRight="Rev A",HeaderHeight=20,FooterHeight=18};
        PrintDocument demo=null;
        Check("PDF scene preserves signed frame labels, slab openings and joint vectors without model mutation",()=>{
            var request=PdfRequest(profile);string before=JsonCodec.Serialize(request.Model);demo=PlanPrintBuilder.Build(request,CancellationToken.None);
            Assert(demo.Pages.Count>=2&&demo.Pages.Any(p=>p.Marks.Any(m=>m.Text.Contains("WL - 3 → 5 kN/m"))));
            Assert(demo.Pages.Any(p=>p.Marks.Any(m=>m.Kind==PrintMarkKind.Polygon&&m.Holes.Length>0)));
            Assert(demo.Pages.Any(p=>p.Marks.Any(m=>m.Kind==PrintMarkKind.Line&&m.Color=="#9400D3")));
            Assert(JsonCodec.Serialize(request.Model)==before);
        });
        Check("load-set PDF independently includes every component and labels the set",()=>{
            var doc=PlanPrintBuilder.Build(PdfRequest(new PrintProfile(),cases:new[]{"Residential"},sets:true),CancellationToken.None);
            var words=doc.Pages.SelectMany(p=>p.Marks).Select(m=>m.Text).ToArray();Assert(words.Contains("Residential")&&words.Contains("FL: ")&&words.Contains("1.5 kN/m²")&&words.Contains("LLAB: ")&&words.Contains("2 kN/m²")&&!words.Any(t=>t.Contains("WL -")));
        });
        Check("fixed PDF scale preserves physical length of a partial frame assignment",()=>{
            var p=new PrintProfile{Fit=false,Scale=100,Legend=false};var doc=PlanPrintBuilder.Build(PdfRequest(p,new[]{"Tầng 1"},new[]{"WL"}),CancellationToken.None);
            var line=doc.Pages[0].Marks.Single(m=>m.Kind==PrintMarkKind.Line&&m.Color=="#800020"&&m.Width==2);
            Near(Math.Abs(line.Points[1].X-line.Points[0].X),7.2*1000*72/25.4/100);
        });
        Check("unknown load axes remain explicit warnings rather than fabricated directions",()=>{
            var f=PdfFixture();var loads=f.Loads.Concat(new[]{new ExistingLoad("Joint:1","NAMED","Joint force","Unresolved",1,0,0,30,30,"kN")});
            var model=new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,f.Levels,loads,f.Diagnostics);
            var doc=PlanPrintBuilder.Build(PdfRequest(stories:new[]{"Tầng 1"},cases:new[]{"NAMED"},model:model),CancellationToken.None);
            Assert(doc.Warnings.Any(w=>w.Contains("Joint:1")&&w.Contains("Chưa xác định")));
        });
        Check("fixed print scale rejects oversized plans unless tiling is enabled",()=>{
            var p=new PrintProfile{Fit=false,Scale=10,Legend=false};Throws<ArgumentException>(()=>PlanPrintBuilder.Build(PdfRequest(p,stories:new[]{"Tầng 1"},cases:new[]{"WL"}),CancellationToken.None));
            p.Tile=true;var doc=PlanPrintBuilder.Build(PdfRequest(p,stories:new[]{"Tầng 1"},cases:new[]{"WL"}),CancellationToken.None);Assert(doc.Pages.Count>1&&doc.Pages.All(page=>page.Scale=="1:10"));
        });
        Check("PDF overflow legends create continuation pages with final page totals",()=>{
            var f=PdfFixture();var extra=Enumerable.Range(0,25).Select(i=>new ExistingLoad("Area:1","P"+i,"Area uniform set: Set "+i,"Global",10,0,1,i,i,"kN/m²"));
            var model=new ModelSnapshot(1,f.Source,f.Version,f.Identity,f.SourceUnits,f.CapturedUtc,f.Elements,f.Levels,f.Loads.Concat(extra),f.Diagnostics);
            var p=new PrintProfile{Combined=true,LegendSize=20,FooterHeight=22};var doc=PlanPrintBuilder.Build(PdfRequest(p,new[]{"Tầng 1"},model.LoadPatterns.ToArray(),model:model),CancellationToken.None);
            Assert(doc.Pages.Count>1&&doc.Pages.Any(page=>page.Title.Contains("Chú giải tiếp")));
            Assert(doc.Pages.Last().Marks.Any(m=>m.Text=="Trang "+doc.Pages.Count+"/"+doc.Pages.Count));
        });
        Check("header overflow is explicit and cancellation stops layout",()=>{
            Throws<ArgumentException>(()=>PlanPrintBuilder.Build(PdfRequest(new PrintProfile{HeaderHeight=2}),CancellationToken.None));
            var cancel=new CancellationTokenSource();cancel.Cancel();Throws<OperationCanceledException>(()=>PlanPrintBuilder.Build(PdfRequest(),cancel.Token));
        });
        string dir=Path.Combine(Path.GetTempPath(),"qks-pdf-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{
            Check("vector PDF writes readable pages with physical paper dimensions",()=>{
                string file=Path.Combine(dir,"plan.pdf");PlanPdfWriter.Write(file,demo.Pages,CancellationToken.None);
                using(var pdf=PdfReader.Open(file,PdfDocumentOpenMode.Import)){Assert(pdf.PageCount==demo.Pages.Count);Assert(Math.Abs(pdf.Pages[0].Width.Point-420*72/25.4)<.01);Assert(Math.Abs(pdf.Pages[0].Height.Point-297*72/25.4)<.01);}
                File.Copy(file,Path.Combine(Path.GetTempPath(),"QKS-Loading-Case-Plan-Demo.pdf"),true);
            });
            Check("PDF renderer accepts intersecting and overlapping holes without painting outside shell",()=>{
                Vec2[] Quad(double a,double b,double c,double d)=>new[]{new Vec2(a,b),new Vec2(c,b),new Vec2(c,d),new Vec2(a,d)};
                var page=new PrintPage{Width=300,Height=300};page.Marks.Add(new PrintMark{Kind=PrintMarkKind.Polygon,Points=Quad(50,50,200,200),Holes=new[]{Quad(120,120,250,250),Quad(80,80,160,160)},Fill="#0000FF",Width=0});
                PlanPdfWriter.Write(Path.Combine(Path.GetTempPath(),"QKS-PDF-Holes-Test.pdf"),new[]{page},CancellationToken.None);
            });
            Check("cancelled PDF export preserves previous output and removes staging",()=>{
                string file=Path.Combine(dir,"keep.pdf");File.WriteAllText(file,"old");var cancel=new CancellationTokenSource();
                Throws<OperationCanceledException>(()=>PlanPdfWriter.Write(file,demo.Pages,cancel.Token,(a,b)=>cancel.Cancel()));Assert(File.ReadAllText(file)=="old"&&!Directory.GetFiles(dir,"*.tmp").Any());
            });
            Check("split-story export stages all files and rolls back on publish failure",()=>{
                string folder=Path.Combine(dir,"split");Directory.CreateDirectory(folder);string first=Path.Combine(folder,"QKS_Tầng 1.pdf");File.WriteAllText(first,"old");Directory.CreateDirectory(Path.Combine(folder,"QKS_Tầng 2.pdf"));
                bool failed=false;try{PlanPdfWriter.WriteByStory(folder,demo,CancellationToken.None);}catch(IOException){failed=true;}Assert(failed&&File.ReadAllText(first)=="old");Assert(!Directory.GetDirectories(folder,".qks-pdf-*").Any());
            });
            Check("print profile persistence roundtrips custom layout and rejects invalid replacement",()=>{
                string path=Path.Combine(dir,"print.json");var p=new PrintProfile{Paper="Custom",Width=500,Height=350,Left=0,HeaderColor="#123456",Project="Tiếng Việt",SplitStories=true};p.Save(path);var read=PrintProfile.Read(path);Assert(read.Width==500&&read.Project=="Tiếng Việt"&&read.SplitStories);
                string old=File.ReadAllText(path);p.Top=double.NaN;Throws<ArgumentException>(()=>p.Save(path));Assert(old==File.ReadAllText(path));
            });
        }finally{Directory.Delete(dir,true);}
    }
}
