using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static void SlabReportTests()
    {
        var f = SelectFixture();
        var slab = f.Elements.Single(e => e.Id == "Area:1");
        var upper = new ModelElement("Area:3", "00123", "=1+2", "L2", "S200", ElementKind.Slab, slab.Vertices.Select(p => p + new Vec3(0, 0, 3)));
        var loads = new[]{
            new ExistingLoad(slab.Id,"SDL","Area uniform set: Home","Global",10,0,1,-2.125,-2.125,"kN/m²"),
            new ExistingLoad(slab.Id,"LL","Area uniform set: Home","Global",10,0,1,3,3,"kN/m²"),
            new ExistingLoad(slab.Id,"ZERO","Area uniform set: Home","Global",10,0,1,0,0,"kN/m²"),
            new ExistingLoad(slab.Id,"SDL","Area uniform","Global",10,0,1,4,4,"kN/m²"),
            new ExistingLoad(upper.Id,"SDL","Area uniform set: Home","Global",10,0,1,-2.125,-2.125,"kN/m²"),
            new ExistingLoad("Area:2","SDL","Area uniform set: Home","Global",10,0,1,99,99,"kN/m²")
        };
        var model = new ModelSnapshot(1, f.Source, f.Version, f.Identity, f.SourceUnits, f.CapturedUtc, f.Elements.Concat(new[]{upper}), f.Levels, loads, new[]{"Test API warning"});
        Check("slab labels contain only distinct patterns; legend retains signed load values", () => {
            var vm = new VisualWorkspaceViewModel(); vm.SetSnapshot(model, true); vm.Level = vm.Levels.First(); vm.ShowShellLoads = true;
            Assert(vm.SlabRegions.Single().Label == "LL / SDL"); Assert(vm.SlabRegions.Single().LegendLabel.Contains("-2.125"));
            vm.LoadSetsMode = true;
            var region = vm.SlabRegions.Single(); Assert(region.Label == "Home");
            foreach (var text in new[]{"SDL: -2.125", "LL: 3", "ZERO: 0", "kN/m²", "Gravity"}) Assert(region.LegendLabel.Contains(text));
            Assert(!region.LegendLabel.Contains("SDL: 4") && vm.ReadLoadItems.Count == 2); // second assignment belongs to wall
        });
        Check("slab report scope preserves every set component, excludes walls and direct loads", () => {
            var rows = SlabLoadWorkbook.Rows(model, true, "Home", "L1");
            Assert(rows.Count == 3 && rows.All(r => r.Element.Kind == ElementKind.Slab));
            Assert(rows.Any(r => r.Load.Value1 == 0) && rows.Any(r => r.Load.Value1 < 0));
            Assert(SlabLoadWorkbook.Rows(model, true, "Home", null).Count == 4);
            Assert(SlabLoadWorkbook.Rows(model, true, "missing", null).Count == 0);
        });
        Check("pattern export includes direct and set sources without summing or double expansion", () => {
            var rows = SlabLoadWorkbook.Rows(model, false, "SDL", null);
            Assert(rows.Count == 3 && rows.Count(r => r.LoadSet == "Home") == 2 && rows.Count(r => r.LoadSet == "") == 1);
            Assert(SlabLoadWorkbook.Rows(model, false, "SDL", "L1").Count == 2);
        });
        Check("legend splits formatting fields and follows text RGB changes", () => {
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(model,true);vm.Level=vm.Levels.First();vm.LoadSetsMode=true;
            var line=vm.SlabRegions.Single().LegendLines.Single(l=>l.PatternHeading=="SDL: ");
            Assert(line.Value=="-2.125 kN/m²"&&line.Axes==" · Global / Gravity"&&line.LoadSet==" · Home"&&line.Source=="");
            bool notified=false;vm.PropertyChanged+=(_,e)=>notified|=e.PropertyName==nameof(vm.SlabLegendValueColor);
            vm.ApplyTextProfile(new ViewerTextProfile(TextAppearance.StructureDefault,new TextAppearance(12,12,34,56)));
            Assert(notified&&vm.SlabLegendValueColor=="#0C2238");
            vm.ApplyTextProfile(new ViewerTextProfile(TextAppearance.StructureDefault,TextAppearance.LoadsDefault));
            Assert(vm.SlabLegendValueColor=="#4169E1");
        });
        string folder = Path.Combine(Path.GetTempPath(), "qks-slab-report-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument Part(ZipArchive zip, string path) { using (var stream = zip.GetEntry(path).Open()) return XDocument.Load(stream); }
        try {
            Check("xlsx matrix uses merged navy headers, numeric values with units and descending stories", () => {
                string file = Path.Combine(folder, "all.xlsx"), before = JsonCodec.Serialize(model);
                var culture = CultureInfo.CurrentCulture;
                try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN"); Assert(SlabLoadWorkbook.Write(file, model, true, null, null) == 4); }
                finally { CultureInfo.CurrentCulture = culture; }
                using (var zip = ZipFile.OpenRead(file)) {
                    var names = Part(zip,"xl/workbook.xml").Descendants(ns+"sheet").Select(e=>e.Attribute("name").Value).ToArray();
                    Assert(names.SequenceEqual(new[]{"Tong hop","L2","L1"}));
                    var summary = Part(zip,"xl/worksheets/sheet1.xml");
                    Assert(summary.Descendants(ns+"row").Count()==4);
                    Assert(summary.Descendants(ns+"c").Single(e=>e.Attribute("r").Value=="B3").Value=="L2");
                    Assert(summary.Descendants(ns+"mergeCell").Select(e=>e.Attribute("ref").Value).SequenceEqual(new[]{"A1:A2","B1:B2","C1:C2","D1:F1"}));
                    var l2=Part(zip,"xl/worksheets/sheet2.xml");
                    Assert(l2.Descendants(ns+"c").Single(e=>e.Attribute("r").Value=="E3").Value=="-2.125");
                    Assert(l2.Descendants(ns+"pane").Single().Attribute("topLeftCell").Value=="D3");
                    var styles=Part(zip,"xl/styles.xml");
                    Assert(styles.Descendants(ns+"numFmt").Single().Attribute("formatCode").Value.Contains("kN/m²"));
                    Assert(styles.Descendants(ns+"fgColor").Single().Attribute("rgb").Value=="FF002060");
                    Assert(!summary.Descendants(ns+"t").Any(e=>new[]{"Step","Số bản ghi","Số ô sàn"}.Contains(e.Value)));
                }
                Assert(JsonCodec.Serialize(model)==before);
                File.Copy(file, Path.Combine(Path.GetTempPath(),"qks-slab-matrix-smoke.xlsx"), true);
            });
            Check("xlsx current-story export and empty filter produce explicit zero-row reports", () => {
                string file = Path.Combine(folder,"current.xlsx"); Assert(SlabLoadWorkbook.Write(file,model,false,"SDL","L2")==1);
                using (var zip=ZipFile.OpenRead(file)) Assert(Part(zip,"xl/workbook.xml").Descendants(ns+"sheet").Count()==2);
                Assert(SlabLoadWorkbook.Write(file,model,false,"Missing","L1")==0);
                using (var zip=ZipFile.OpenRead(file)) Assert(Part(zip,"xl/worksheets/sheet2.xml").Descendants(ns+"row").Count()==2);
            });
            Check("xlsx normalizes duplicate, invalid and long story sheet names without losing rows", () => {
                var stories = new[]{"Tong hop", "L/1", "L?1", new string('x',40), new string('x',39)+"y", ""};
                var elements = stories.Select((name,i)=>new ModelElement("Area:X"+i,"X"+i,"",""+name,"S",ElementKind.Slab,slab.Vertices)).ToArray();
                var snapshot = new ModelSnapshot(1,"ETABS","","model","",DateTime.UtcNow,elements,stories.Select((n,i)=>new ModelLevel(n,i)),elements.Select(e=>new ExistingLoad(e.Id,"LL","Area uniform","Global",10,0,1,1,1,"kN/m²")),Array.Empty<string>());
                string file=Path.Combine(folder,"names.xlsx"); Assert(SlabLoadWorkbook.Write(file,snapshot,false,null,null)==6);
                using(var zip=ZipFile.OpenRead(file)) {
                    var names=Part(zip,"xl/workbook.xml").Descendants(ns+"sheet").Select(e=>e.Attribute("name").Value).ToArray();
                    Assert(names.Length==7&&names.Distinct(StringComparer.OrdinalIgnoreCase).Count()==7);
                    Assert(names.All(n=>n.Length<=31&&n.Length>0&&!n.Any(c=>":\\/?*[]".Contains(c))));
                }
            });
            Check("matrix keeps distinct magnitudes and axes without summing assignments", () => {
                var extra=new[]{new ExistingLoad(slab.Id,"SDL","Area uniform","Global",10,0,1,5,5,"kN/m²"),new ExistingLoad(slab.Id,"SDL","Area uniform","Global",4,0,1,5,5,"kN/m²")};
                var snapshot=new ModelSnapshot(1,"ETABS","","model","",DateTime.UtcNow,model.Elements,model.Levels,model.Loads.Concat(extra),Array.Empty<string>());
                string file=Path.Combine(folder,"multiple.xlsx");SlabLoadWorkbook.Write(file,snapshot,false,"SDL","L1");
                using(var zip=ZipFile.OpenRead(file)) {
                    var sheet=Part(zip,"xl/worksheets/sheet1.xml");
                    var value=sheet.Descendants(ns+"c").Single(e=>e.Attribute("r").Value=="D3").Value;
                    Assert(value.Contains("4 kN/m²")&&value.Contains("5 kN/m²")&&value.Contains("Global / X"));
                    Assert(value.Split('\n').Length==3&&!value.Contains("14 kN"));
                }
            });
            Check("matrix supports columns beyond Z and keeps names as text", () => {
                var element=new ModelElement("Area:wide","a","","=1+2","S",ElementKind.Slab,slab.Vertices);
                var wide=new ModelSnapshot(1,"ETABS","","model","",DateTime.UtcNow,new[]{element},new[]{new ModelLevel("=1+2",3)},Enumerable.Range(0,30).Select(i=>new ExistingLoad(element.Id,"P"+i.ToString("D2"),"Area uniform set: 00123","Global",10,0,1,i,i,"kN/m²")),Array.Empty<string>());
                string file=Path.Combine(folder,"wide.xlsx");SlabLoadWorkbook.Write(file,wide,true,null,null);
                using(var zip=ZipFile.OpenRead(file)) {
                    var sheet=Part(zip,"xl/worksheets/sheet1.xml");var cells=sheet.Descendants(ns+"c").ToArray();
                    Assert(cells.Single(e=>e.Attribute("r").Value=="AG3").Value=="29");
                    Assert(cells.Single(e=>e.Attribute("r").Value=="B3").Attribute("t").Value=="inlineStr");
                    Assert(cells.Single(e=>e.Attribute("r").Value=="B3").Value=="=1+2");
                    Assert(cells.Single(e=>e.Attribute("r").Value=="C3").Value=="00123"&&!sheet.Descendants(ns+"f").Any());
                    Assert(sheet.Descendants(ns+"mergeCell").Any(e=>e.Attribute("ref").Value=="D1:AG1"));
                }
            });
            Check("xlsx failed write preserves prior workbook and removes temporary package", () => {
                string file=Path.Combine(folder,"keep.xlsx"); File.WriteAllText(file,"original");
                var invalid=new ModelSnapshot(1,"ETABS","","model","",DateTime.UtcNow,model.Elements,model.Levels,new[]{new ExistingLoad(slab.Id,"bad\u0001pattern","Area uniform","Global",10,0,1,1,1,"kN/m²")},Array.Empty<string>());
                bool failed=false; try{SlabLoadWorkbook.Write(file,invalid,false,null,null);}catch(ArgumentException){failed=true;}
                Assert(failed&&File.ReadAllText(file)=="original"&&!Directory.GetFiles(folder,"*.tmp").Any());
            });
        } finally { Directory.Delete(folder,true); }
    }
}
