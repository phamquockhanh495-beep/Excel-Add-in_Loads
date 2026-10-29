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
        string folder = Path.Combine(Path.GetTempPath(), "qks-slab-report-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument Part(ZipArchive zip, string path) { using (var stream = zip.GetEntry(path).Open()) return XDocument.Load(stream); }
        try {
            Check("xlsx roundtrip preserves numeric values, text IDs, source warnings and all story sheets", () => {
                string file = Path.Combine(folder, "all.xlsx"), before = JsonCodec.Serialize(model);
                var culture = CultureInfo.CurrentCulture;
                try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN"); Assert(SlabLoadWorkbook.Write(file, model, true, null, null) == 4); }
                finally { CultureInfo.CurrentCulture = culture; }
                using (var zip = ZipFile.OpenRead(file)) {
                    var names = Part(zip,"xl/workbook.xml").Descendants(ns+"sheet").Select(e=>e.Attribute("name").Value).ToArray();
                    Assert(names.SequenceEqual(new[]{"Thong tin","Tong hop","L1","L2"}));
                    Assert(Part(zip,"xl/worksheets/sheet1.xml").ToString().Contains("Test API warning"));
                    var l1 = Part(zip,"xl/worksheets/sheet3.xml"); Assert(l1.Descendants(ns+"row").Count()==4);
                    Assert(l1.Descendants(ns+"v").Any(e=>e.Value=="-2.125"));
                    var l2 = Part(zip,"xl/worksheets/sheet4.xml");
                    var cells = l2.Descendants(ns+"c").ToArray();
                    Assert(cells.Single(e=>e.Attribute("r").Value=="B2").Attribute("t").Value=="inlineStr");
                    Assert(cells.Single(e=>e.Attribute("r").Value=="B2").Value=="00123");
                    Assert(cells.Single(e=>e.Attribute("r").Value=="C2").Value=="=1+2" && !l2.Descendants(ns+"f").Any());
                    Assert(l2.Descendants(ns+"pane").Single().Attribute("state").Value=="frozen" && l2.Descendants(ns+"autoFilter").Any());
                }
                Assert(JsonCodec.Serialize(model)==before);
                // Retain a generated workbook for an independent reader smoke check.
                File.Copy(file, Path.Combine(Path.GetTempPath(),"qks-slab-report-smoke.xlsx"), true);
            });
            Check("xlsx current-story export and empty filter produce explicit zero-row reports", () => {
                string file = Path.Combine(folder,"current.xlsx"); Assert(SlabLoadWorkbook.Write(file,model,false,"SDL","L2")==1);
                using (var zip=ZipFile.OpenRead(file)) Assert(Part(zip,"xl/workbook.xml").Descendants(ns+"sheet").Count()==3);
                Assert(SlabLoadWorkbook.Write(file,model,false,"Missing","L1")==0);
                using (var zip=ZipFile.OpenRead(file)) Assert(Part(zip,"xl/worksheets/sheet3.xml").Descendants(ns+"row").Count()==1);
            });
            Check("xlsx normalizes duplicate, invalid and long story sheet names without losing rows", () => {
                var stories = new[]{"Tong hop", "L/1", "L?1", new string('x',40), new string('x',39)+"y", ""};
                var elements = stories.Select((name,i)=>new ModelElement("Area:X"+i,"X"+i,"",""+name,"S",ElementKind.Slab,slab.Vertices)).ToArray();
                var snapshot = new ModelSnapshot(1,"ETABS","","model","",DateTime.UtcNow,elements,stories.Select((n,i)=>new ModelLevel(n,i)),elements.Select(e=>new ExistingLoad(e.Id,"LL","Area uniform","Global",10,0,1,1,1,"kN/m²")),Array.Empty<string>());
                string file=Path.Combine(folder,"names.xlsx"); Assert(SlabLoadWorkbook.Write(file,snapshot,false,null,null)==6);
                using(var zip=ZipFile.OpenRead(file)) {
                    var names=Part(zip,"xl/workbook.xml").Descendants(ns+"sheet").Select(e=>e.Attribute("name").Value).ToArray();
                    Assert(names.Length==8&&names.Distinct(StringComparer.OrdinalIgnoreCase).Count()==8);
                    Assert(names.All(n=>n.Length<=31&&n.Length>0&&!n.Any(c=>":\\/?*[]".Contains(c))));
                }
            });
            Check("xlsx failed write preserves prior workbook and removes temporary package", () => {
                string file=Path.Combine(folder,"keep.xlsx"); File.WriteAllText(file,"original");
                var invalid=new ModelSnapshot(1,"ETABS","","bad\u0001identity","",DateTime.UtcNow,model.Elements,model.Levels,model.Loads,Array.Empty<string>());
                bool failed=false; try{SlabLoadWorkbook.Write(file,invalid,false,null,null);}catch(ArgumentException){failed=true;}
                Assert(failed&&File.ReadAllText(file)=="original"&&!Directory.GetFiles(folder,"*.tmp").Any());
            });
        } finally { Directory.Delete(folder,true); }
    }
}
