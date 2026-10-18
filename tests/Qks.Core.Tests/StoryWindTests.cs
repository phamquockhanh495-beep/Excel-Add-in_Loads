using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;

internal static partial class Program
{
    private static StoryWindInput BeamInput(string name="B1",string story="L1",string q="-2")=>new StoryWindInput{Story=story,Pattern="WX",UniqueName=name,Q1=q};
    private static StoryWindInput UserInput(string key="SYNTHETIC-KEY-1")=>new StoryWindInput{Target="UserLoads",Story="L1",Pattern="WX_USER",DiaphragmKey=key,Fx="10",Fy="0",Mz="2",X="3",Y="4"};
    private static StoryWindPlan Plan(params StoryWindInput[] rows)=>StoryWindPlan.Create(new DraftWorkspace(DraftFixture()),rows,Array.Empty<string>(),null,"test direct input");
    private static void WithWindWorkbook(Action<string> action)
    {
        string dir=Path.Combine(Path.GetTempPath(),"qks-wind-xlsx-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try { string file=Path.Combine(dir,"wind.xlsx");WindWorkbook.WriteTemplate(file,DraftFixture().Levels,"WX");action(file); }
        finally { Directory.Delete(dir,true); }
    }
    private static readonly XNamespace SheetNs="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static XElement WindCell(string address,string value,bool formula=false,string type="inlineStr")
    {
        var cell=new XElement(SheetNs+"c",new XAttribute("r",address));
        if(type!=null)cell.Add(new XAttribute("t",type));
        if(formula)cell.Add(new XElement(SheetNs+"f","-5/2"));
        if(value!=null)cell.Add(type=="inlineStr"?new XElement(SheetNs+"is",new XElement(SheetNs+"t",value)):new XElement(SheetNs+"v",value));return cell;
    }
    private static XDocument InputSheet(XElement valueCell)=>new XDocument(new XElement(SheetNs+"worksheet",new XElement(SheetNs+"sheetData",
        new XElement(SheetNs+"row",new XAttribute("r",1),WindCell("A1","Story"),WindCell("B1","Pattern"),WindCell("D1","UniqueName"),WindCell("F1","q1_kN_m")),
        new XElement(SheetNs+"row",new XAttribute("r",2),WindCell("A2","L1"),WindCell("B2","WX"),WindCell("D2","B1"),valueCell))));
    private static void ReplaceZipXml(string file,string part,XDocument xml)
    {
        using(var zip=ZipFile.Open(file,ZipArchiveMode.Update)){zip.GetEntry(part)?.Delete();using(var stream=zip.CreateEntry(part).Open())xml.Save(stream);}
    }
    private static void StoryWindTests()
    {
        Check("story wind: packaged beam CSV stages against synthetic source",()=>{var rows=WindWorkbook.Read(Path.Combine(ViewerTestRoot,"tests/fixtures/wind-input-beams.csv"));Assert(Plan(rows.ToArray()).DraftLoads.Count==2);});
        Check("story wind: packaged User Loads CSV stages against matching sample keys",()=>{string dir=Path.Combine(ViewerTestRoot,"tests/fixtures");var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(JsonCodec.Deserialize<ModelSnapshot>(JsonCodec.Read(Path.Combine(dir,"viewer-wind-source.json"))),true);vm.ImportUserWindDocument(JsonCodec.Deserialize<UserWindDocument>(JsonCodec.Read(Path.Combine(dir,"viewer-user-wind.qkswind.json"))));vm.ImportStoryWind(WindWorkbook.Read(Path.Combine(dir,"wind-input-user-loads.csv")),"Synthetic CSV");Assert(vm.WindUserRows.Single(r=>r.DiaphragmKey=="SYNTHETIC-KEY-ROOF").Fx==30&&!vm.CanApplyUserWind);});
        Check("story wind: input controls require source context and an idle viewer",()=>{var vm=new VisualWorkspaceViewModel();Assert(!vm.CanInputStoryWind);vm.SetSnapshot(DraftFixture(),true);Assert(vm.CanInputStoryWind);vm.Busy=true;Assert(!vm.CanInputStoryWind);vm.Busy=false;vm.SetSnapshot(null,false);Assert(!vm.CanInputStoryWind);});
        Check("story wind: direct q stays signed and is not multiplied by height",()=>{var p=Plan(BeamInput(q:"-2,5"));Near(p.DraftLoads.Single().Value1,-2.5);Near(p.DraftLoads[0].Value2,-2.5);Assert(p.DraftLoads[0].Kind==DraftKind.WindLine&&p.DraftLoads[0].Start==0&&p.DraftLoads[0].End==1);});
        Check("story wind: q2 can be a different explicit end value",()=>{var row=BeamInput();row.Q2="-5";var p=Plan(row);Near(p.DraftLoads[0].Value2,-5);});
        Check("story wind: only explicitly selected beams in requested story receive blank-name input",()=>{var p=StoryWindPlan.Create(new DraftWorkspace(DraftFixture()),new[]{BeamInput("","Roof")},new[]{"Frame:B1","Frame:B2","Frame:B3","Frame:C1"},null,"selected input");Assert(p.DraftLoads.Count==2&&p.DraftLoads.All(d=>d.ElementId=="Frame:B2"||d.ElementId=="Frame:B3"));});
        Check("story wind: blank name without matching selected beams rejects",()=>Throws<ArgumentException>(()=>Plan(BeamInput(""))));
        Check("story wind: raw UniqueName is required, not Frame prefix or Label guess",()=>Throws<ArgumentException>(()=>Plan(BeamInput("Frame:B1"))));
        Check("story wind: beam in different story, missing pattern, column rejected",()=>{Throws<ArgumentException>(()=>Plan(BeamInput("B1","Roof")));var r=BeamInput();r.Pattern="MISSING";Throws<ArgumentException>(()=>Plan(r));Throws<ArgumentException>(()=>Plan(BeamInput("C1")));});
        Check("story wind: missing numeric values cannot silently become zero",()=>{var r=BeamInput(q:"");Throws<ArgumentException>(()=>Plan(r));r.Q1="0";Near(Plan(r).DraftLoads[0].Value1,0);});
        Check("story wind: nonfinite, wrong direction and mixed dimensions reject",()=>{foreach(string value in new[]{"NaN","Infinity","1e999","1,234.56"})Throws<ArgumentException>(()=>Plan(BeamInput(q:value)));var r=BeamInput();r.Direction="Gravity";Throws<ArgumentException>(()=>Plan(r));r.Direction="Global-X";r.Fx="10";Throws<ArgumentException>(()=>Plan(r));});
        Check("story wind: duplicate beam scopes reject before draft mutation",()=>{var w=new DraftWorkspace(DraftFixture());Throws<ArgumentException>(()=>StoryWindPlan.Create(w,new[]{BeamInput(),BeamInput()},Array.Empty<string>(),null,"test"));Assert(w.Loads.Count==0);});
        Check("story wind: import reuses wind row ID and Undo restores whole batch",()=>{var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(DraftFixture(),true);vm.Drafts.Put(Slab());vm.ImportStoryWind(new[]{BeamInput("B2","Roof"),BeamInput("B3","Roof")},"first");Assert(vm.DraftRows.Count==3);string id=vm.DraftRows.First(d=>d.ElementId=="Frame:B2").Id;vm.ImportStoryWind(new[]{BeamInput("B2","Roof","-4")},"edit");Assert(vm.DraftRows.Count==3&&vm.DraftRows.First(d=>d.ElementId=="Frame:B2").Id==id);vm.UndoDraft();Near(vm.DraftRows.First(d=>d.ElementId=="Frame:B2").Value1,-2);vm.UndoDraft();Assert(vm.DraftRows.Count==1&&vm.DraftRows.Single().Kind==DraftKind.SlabUniform);});
        Check("story wind: mixed invalid batch leaves both editors unchanged",()=>{var h=new WindFake();var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(h.Inner.Source,true);vm.SetUserWindBaseline(h.State);var invalid=UserInput();invalid.Fx="bad";string before=JsonCodec.Serialize(vm.UserWindDocument());Throws<ArgumentException>(()=>vm.ImportStoryWind(new[]{BeamInput(),invalid},"invalid"));Assert(vm.DraftRows.Count==0&&JsonCodec.Serialize(vm.UserWindDocument())==before);});
        Check("story wind: User Loads keeps entered forces, moments and coordinates",()=>{var h=new WindFake();var p=StoryWindPlan.Create(new DraftWorkspace(h.Inner.Source),new[]{UserInput()},Array.Empty<string>(),h.State,"force input");var r=p.UserRows.Single();Near(r.Fx,10);Near(r.Fy,0);Near(r.Mz,2);Near(r.X,3);Near(r.Y,4);Assert(p.StoryAssignments[r.DiaphragmKey]=="L1");});
        Check("story wind: User Loads requires API key and one loaded pattern",()=>{var h=new WindFake();Throws<ArgumentException>(()=>StoryWindPlan.Create(new DraftWorkspace(h.Inner.Source),new[]{UserInput()},Array.Empty<string>(),null,"test"));Throws<ArgumentException>(()=>StoryWindPlan.Create(new DraftWorkspace(h.Inner.Source),new[]{UserInput("D1-guessed")},Array.Empty<string>(),h.State,"test"));});
        Check("story wind: User Loads requires all components, preserves explicit zero",()=>{var h=new WindFake();var row=UserInput();row.Mz="";Throws<ArgumentException>(()=>StoryWindPlan.Create(new DraftWorkspace(h.Inner.Source),new[]{row},Array.Empty<string>(),h.State,"test"));row.Mz="0";Near(StoryWindPlan.Create(new DraftWorkspace(h.Inner.Source),new[]{row},Array.Empty<string>(),h.State,"test").UserRows.Single().Mz,0);});
        Check("story wind: duplicate User Loads key across stories rejects",()=>{var h=new WindFake();var a=UserInput();var b=UserInput();b.Story="Roof";Throws<ArgumentException>(()=>StoryWindPlan.Create(new DraftWorkspace(h.Inner.Source),new[]{a,b},Array.Empty<string>(),h.State,"test"));});
        Check("story wind: story metadata survives saved User Loads JSON",()=>{var h=new WindFake();var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(h.Inner.Source,true);vm.SetUserWindBaseline(h.State);vm.ImportStoryWind(new[]{UserInput()},"Excel input");var doc=JsonCodec.Deserialize<UserWindDocument>(JsonCodec.Serialize(vm.UserWindDocument()));Assert(doc.StoryAssignments["SYNTHETIC-KEY-1"]=="L1"&&!doc.Basis.Contains("quy đổi"));var copy=new VisualWorkspaceViewModel();copy.SetSnapshot(h.Inner.Source,true);copy.ImportUserWindDocument(doc);Assert(copy.WindUserRows.Single(r=>r.DiaphragmKey=="SYNTHETIC-KEY-1").Story=="L1");});
        Check("story wind: saved nonexistent Story prevents live writer",()=>{var h=new WindFake();var d=WindDocument(h);var map=new System.Collections.Generic.Dictionary<string,string>{{d.Changes[0].DiaphragmKey,"UNKNOWN"}};var r=ApplyWind(h,new UserWindDocument(d.Identity,d.ModelFingerprint,d.Before,d.Changes,storyAssignments:map));Assert(!r.MayHaveChanges&&h.Inner.Backups==0);});
        Check("wind table: semicolon CSV accepts Vietnamese headings and decimal comma",()=>{var table=WindWorkbook.Csv("Tầng;Pattern;UniqueName;q1 (kN/m)\r\nL1;WX;B1;\"-2,5\"\r\n");var rows=WindInputTable.Parse(table);Near(Plan(rows.Single()).DraftLoads[0].Value1,-2.5);});
        Check("wind table: quoted CSV escapes and multiline cells preserved",()=>{var rows=WindWorkbook.Csv("Story,Pattern,q1,Note\nL1,WX,-2,\"A, \"\"B\"\"\nC\"\n");Assert(rows.Count==2&&rows[1][3]=="A, \"B\"\nC");});
        Check("wind table: unclosed CSV quotes reject",()=>Throws<InvalidDataException>(()=>WindWorkbook.Csv("Story,q1\n\"L1,2")));
        Check("wind table: duplicate/alias headings reject ambiguity",()=>{Throws<ArgumentException>(()=>WindInputTable.Parse(new[]{new[]{"Story","q1","q1"},new[]{"L1","2","3"}}));Throws<ArgumentException>(()=>WindInputTable.Parse(new[]{new[]{"Story","q1","q1_kN_m"},new[]{"L1","2","3"}}));});
        Check("wind table: does not accept pressure as line load",()=>Throws<ArgumentException>(()=>WindInputTable.Parse(new[]{new[]{"Story","p (kN/m²)"},new[]{"L1","2"}})));
        Check("wind table: rows cannot infer both line and story force target",()=>Throws<ArgumentException>(()=>WindInputTable.Parse(new[]{new[]{"Story","q1","Fx"},new[]{"L1","2","3"}})));
        Check("wind table: default pattern is explicit and comma CSV heading may follow notes",()=>{var rows=WindInputTable.Parse(WindWorkbook.Csv("Input wind values\nStory,UniqueName,q1\nL1,B1,-3\n"),"WX");Assert(rows[0].Pattern=="WX");Near(Plan(rows[0]).DraftLoads[0].Value1,-3);});
        Check("wind XLSX: template has source Story rows and no fabricated numeric loads",()=>WithWindWorkbook(file=>{var rows=WindWorkbook.Read(file);Assert(rows.Count==DraftFixture().Levels.Count*2&&rows.All(r=>!r.HasInput)&&rows.All(r=>r.Pattern=="WX"));using(var z=ZipFile.OpenRead(file))Assert(z.GetEntry("[Content_Types].xml")!=null&&z.GetEntry("xl/worksheets/sheet3.xml")!=null);}));
        Check("wind XLSX: sparse cells and cached numeric formula retain value",()=>WithWindWorkbook(file=>{ReplaceZipXml(file,"xl/worksheets/sheet1.xml",InputSheet(WindCell("F2","-2.5",true,null)));var row=WindWorkbook.Read(file).Single(r=>r.HasInput);Near(Plan(row).DraftLoads[0].Value1,-2.5);Assert(row.UniqueName=="B1");}));
        Check("wind XLSX: shared rich text resolves full Story name",()=>WithWindWorkbook(file=>{var sheet=InputSheet(WindCell("F2","-3",false,null));sheet.Descendants(SheetNs+"c").First(c=>c.Attribute("r").Value=="A2").ReplaceWith(WindCell("A2","0",false,"s"));ReplaceZipXml(file,"xl/worksheets/sheet1.xml",sheet);ReplaceZipXml(file,"xl/sharedStrings.xml",new XDocument(new XElement(SheetNs+"sst",new XElement(SheetNs+"si",new XElement(SheetNs+"r",new XElement(SheetNs+"t","L")),new XElement(SheetNs+"r",new XElement(SheetNs+"t","1"))))));Assert(WindWorkbook.Read(file).Single(r=>r.HasInput).Story=="L1");}));
        Check("wind XLSX: formula without cache is an error, never zero",()=>WithWindWorkbook(file=>{ReplaceZipXml(file,"xl/worksheets/sheet1.xml",InputSheet(WindCell("F2",null,true,null)));var r=WindWorkbook.Read(file).Single(r=>r.HasInput);Assert(r.Q1.Contains("WITHOUT_CACHE"));Throws<ArgumentException>(()=>Plan(r));}));
        Check("wind XLSX: Excel error cell does not become a load",()=>WithWindWorkbook(file=>{ReplaceZipXml(file,"xl/worksheets/sheet1.xml",InputSheet(WindCell("F2","#VALUE!",false,"e")));Throws<ArgumentException>(()=>Plan(WindWorkbook.Read(file).Single(r=>r.HasInput)));}));
        Check("wind XLSX: external worksheet targets are rejected",()=>WithWindWorkbook(file=>{using(var zip=ZipFile.OpenRead(file)){}var ns=(XNamespace)"http://schemas.openxmlformats.org/package/2006/relationships";ReplaceZipXml(file,"xl/_rels/workbook.xml.rels",new XDocument(new XElement(ns+"Relationships",new XElement(ns+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Target","https://example.invalid/evil.xml"),new XAttribute("TargetMode","External")))));Throws<InvalidDataException>(()=>WindWorkbook.Read(file));}));
        Check("wind XLSX: .xlsm data can be read without executing macros",()=>WithWindWorkbook(file=>{string macro=Path.ChangeExtension(file,".xlsm");File.Copy(file,macro);Assert(WindWorkbook.Read(macro).Count>0);}));
        Check("wind XLSX: binary .xls is directed to Excel range input",()=>WithWindWorkbook(file=>{string old=Path.ChangeExtension(file,".xls");File.Copy(file,old);Throws<InvalidDataException>(()=>WindWorkbook.Read(old));}));
        Check("wind XLSX: exporting again replaces the template atomically",()=>WithWindWorkbook(file=>{WindWorkbook.WriteTemplate(file,new[]{new ModelLevel("NEW STORY",12)},"WNEW");var r=WindWorkbook.Read(file);Assert(r.Count==2&&r.All(x=>x.Story=="NEW STORY"&&x.Pattern=="WNEW"));}));
    }
}
