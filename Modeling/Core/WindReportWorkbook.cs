#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Portable, macro-free export of the editable wind input, not proof of host assignment.</summary>
    public static class WindReportWorkbook
    {
        public static void Write(string path,ModelSnapshot model,IEnumerable<StoryWindInput> source,bool users)
        {
            var rows=source.ToArray();
            if(rows.Length==0||rows.Length>10000)throw new ArgumentException("Cần 1–10.000 dòng để xuất Excel.");
            if(rows.Any(r=>r.Target!=(users?"UserLoads":"Beam")))throw new ArgumentException("Xuất riêng từng phương thức.");
            string[] headers=users?new[]{"Target","Story","Pattern","DiaphragmKey","Fx_kN","Fy_kN","Mz_kNm","X_m","Y_m"}:new[]{"Target","Story","Pattern","UniqueName","Direction","q1_kN_m","q2_kN_m","Action","r1","r2"};
            bool sets=users&&rows.Any(x=>x.UserLoadSet.Length>0);
            if(sets)headers=headers.Concat(new[]{"LoadSet","NumberOfLoadSets","Reversible"}).ToArray();
            var order=model.Levels.ToDictionary(l=>l.Name,l=>l.Elevation,StringComparer.Ordinal);
            rows=rows.OrderByDescending(r=>order.TryGetValue(r.Story,out double z)?z:double.MinValue).ThenBy(r=>r.Pattern,StringComparer.Ordinal).ThenBy(r=>users?r.DiaphragmKey:r.UniqueName,StringComparer.Ordinal).ToArray();
            XNamespace s="http://schemas.openxmlformats.org/spreadsheetml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships",p="http://schemas.openxmlformats.org/package/2006/relationships",t="http://schemas.openxmlformats.org/package/2006/content-types";
            var data=new XElement(s+"sheetData");
            void Row(int index,string[] cells,int style,bool numbers=false){var row=new XElement(s+"row",new XAttribute("r",index),new XAttribute("ht",index==3?30:23),new XAttribute("customHeight",1));for(int c=0;c<cells.Length;c++){
                bool numeric=numbers&&(users?c>=4&&c!=11:c==5||c==6||c==8||c==9)&&double.TryParse(cells[c].Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out _);
                var cell=new XElement(s+"c",new XAttribute("r",((char)('A'+c)).ToString()+index),new XAttribute("s",style));
                if(numeric){double number=StoryWindPlan.NumberValue(cells[c],headers[c]);cell.Add(new XElement(s+"v",number.ToString("R",CultureInfo.InvariantCulture)));}
                else {cell.Add(new XAttribute("t","inlineStr"),new XElement(s+"is",new XElement(s+"t",new XAttribute(XNamespace.Xml+"space","preserve"),cells[c])));}row.Add(cell);
            }data.Add(row);}
            Row(1,new[]{"QKS WIND · BỘ TẢI SOẠN — KHÔNG PHẢI XÁC NHẬN ĐÃ GÁN ETABS"},1);
            Row(2,new[]{model.Identity+" · kN, m · "+(users?"Story do người dùng đối chiếu khóa API":"q theo chiều dài thực I→J; r1/r2 theo tỷ lệ")},0);Row(3,headers,1);
            string previous="";int band=0,index=4;
            foreach(var row in rows){if(row.Story!=previous){band=1-band;previous=row.Story;}var cells=users?new[]{row.Target,row.Story,row.Pattern,row.DiaphragmKey,row.Fx,row.Fy,row.Mz,row.X,row.Y}:new[]{row.Target,row.Story,row.Pattern,row.UniqueName,row.Direction,row.Q1,row.Q2,row.Action,row.R1,row.R2};if(sets)cells=cells.Concat(new[]{row.UserLoadSet,row.UserSetCount,row.UserReversible}).ToArray();Row(index++,cells,band==0?2:3,true);}
            string last=((char)('A'+headers.Length-1)).ToString();
            var sheet=new XElement(s+"worksheet",new XElement(s+"sheetViews",new XElement(s+"sheetView",new XAttribute("workbookViewId",0),new XElement(s+"pane",new XAttribute("ySplit",3),new XAttribute("topLeftCell","A4"),new XAttribute("activePane","bottomLeft"),new XAttribute("state","frozen")))),new XElement(s+"cols",Enumerable.Range(1,headers.Length).Select(c=>new XElement(s+"col",new XAttribute("min",c),new XAttribute("max",c),new XAttribute("width",c<=4?24:18),new XAttribute("customWidth",1)))),data,new XElement(s+"autoFilter",new XAttribute("ref","A3:"+last+(index-1))),new XElement(s+"mergeCells",new XElement(s+"mergeCell",new XAttribute("ref","A1:"+last+"1")),new XElement(s+"mergeCell",new XAttribute("ref","A2:"+last+"2"))));
            string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{
                using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create)){
                    void Entry(string name,XElement xml){using(var stream=zip.CreateEntry(name).Open())using(var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)}))new XDocument(xml).Save(writer);}
                    Entry("[Content_Types].xml",new XElement(t+"Types",new XElement(t+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(t+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new[]{("/xl/workbook.xml","sheet.main"),("/xl/worksheets/sheet1.xml","worksheet"),("/xl/styles.xml","styles")}.Select(x=>new XElement(t+"Override",new XAttribute("PartName",x.Item1),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml."+x.Item2+"+xml")))));
                    Entry("_rels/.rels",new XElement(p+"Relationships",new XElement(p+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",r.NamespaceName+"/officeDocument"),new XAttribute("Target","xl/workbook.xml"))));
                    Entry("xl/workbook.xml",new XElement(s+"workbook",new XElement(s+"sheets",new XElement(s+"sheet",new XAttribute("name",users?"WindStories":"WindBeams"),new XAttribute("sheetId",1),new XAttribute(r+"id","rId1")))));
                    Entry("xl/_rels/workbook.xml.rels",new XElement(p+"Relationships",new XElement(p+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",r.NamespaceName+"/worksheet"),new XAttribute("Target","worksheets/sheet1.xml")),new XElement(p+"Relationship",new XAttribute("Id","rId2"),new XAttribute("Type",r.NamespaceName+"/styles"),new XAttribute("Target","styles.xml"))));
                    Entry("xl/worksheets/sheet1.xml",sheet);
                    Entry("xl/styles.xml",XElement.Parse(Styles));
                }
                if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
        private const string Styles=@"<styleSheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'>
<fonts count='2'><font><sz val='11'/><name val='Calibri'/><color rgb='FF0F2240'/></font><font><b/><sz val='11'/><name val='Calibri'/><color rgb='FFFFFFFF'/></font></fonts>
<fills count='4'><fill><patternFill patternType='none'/></fill><fill><patternFill patternType='gray125'/></fill><fill><patternFill patternType='solid'><fgColor rgb='FF0F2240'/><bgColor indexed='64'/></patternFill></fill><fill><patternFill patternType='solid'><fgColor rgb='FFEFF5FB'/><bgColor indexed='64'/></patternFill></fill></fills>
<borders count='2'><border/><border><left style='thin'><color rgb='FFD0DAE5'/></left><right style='thin'><color rgb='FFD0DAE5'/></right><top style='thin'><color rgb='FFD0DAE5'/></top><bottom style='thin'><color rgb='FFD0DAE5'/></bottom></border></borders>
<cellStyleXfs count='1'><xf numFmtId='0' fontId='0' fillId='0' borderId='0'/></cellStyleXfs>
<cellXfs count='4'><xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0'/><xf numFmtId='0' fontId='1' fillId='2' borderId='1' xfId='0' applyAlignment='1'><alignment horizontal='center' vertical='center' wrapText='1'/></xf><xf numFmtId='0' fontId='0' fillId='0' borderId='1' xfId='0' applyAlignment='1'><alignment horizontal='center' vertical='center'/></xf><xf numFmtId='0' fontId='0' fillId='3' borderId='1' xfId='0' applyAlignment='1'><alignment horizontal='center' vertical='center'/></xf></cellXfs>
<cellStyles count='1'><cellStyle name='Normal' xfId='0' builtinId='0'/></cellStyles></styleSheet>";
    }
}
