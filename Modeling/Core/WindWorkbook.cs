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
    public static class WindWorkbook
    {
        public static void WriteTemplate(string path,IEnumerable<ModelLevel> source,string defaultPattern)
        {
            var levels=source.ToArray();string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
            XNamespace main="http://schemas.openxmlformats.org/spreadsheetml/2006/main",rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships",package="http://schemas.openxmlformats.org/package/2006/relationships",types="http://schemas.openxmlformats.org/package/2006/content-types";
            try {
                using(var archive=ZipFile.Open(temp,ZipArchiveMode.Create)) {
                    void Entry(string name,XDocument xml) { using(var stream=archive.CreateEntry(name).Open())using(var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)}))xml.Save(writer); }
                    var content=new XElement(types+"Types",new XElement(types+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(types+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),new XElement(types+"Override",new XAttribute("PartName","/xl/workbook.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")));
                    var sheetNames=new[]{"WindBeams","WindStories","ReadMe"};var sheetList=new XElement(main+"sheets");var relationships=new XElement(package+"Relationships");
                    for(int i=1;i<=3;i++) { content.Add(new XElement(types+"Override",new XAttribute("PartName","/xl/worksheets/sheet"+i+".xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));sheetList.Add(new XElement(main+"sheet",new XAttribute("name",sheetNames[i-1]),new XAttribute("sheetId",i),new XAttribute(rel+"id","rId"+i)));relationships.Add(new XElement(package+"Relationship",new XAttribute("Id","rId"+i),new XAttribute("Type",rel.NamespaceName+"/worksheet"),new XAttribute("Target","worksheets/sheet"+i+".xml"))); }
                    Entry("[Content_Types].xml",new XDocument(content));Entry("_rels/.rels",new XDocument(new XElement(package+"Relationships",new XElement(package+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",rel.NamespaceName+"/officeDocument"),new XAttribute("Target","xl/workbook.xml")))));Entry("xl/workbook.xml",new XDocument(new XElement(main+"workbook",new XAttribute(XNamespace.Xmlns+"r",rel),sheetList)));Entry("xl/_rels/workbook.xml.rels",new XDocument(relationships));
                    void Sheet(int index,IEnumerable<string[]> values) { var data=new XElement(main+"sheetData");int r=0;foreach(var row in values) { r++;var element=new XElement(main+"row",new XAttribute("r",r));for(int c=0;c<row.Length;c++)element.Add(new XElement(main+"c",new XAttribute("r",((char)('A'+c)).ToString()+r),new XAttribute("t","inlineStr"),new XElement(main+"is",new XElement(main+"t",row[c]))));data.Add(element); }Entry("xl/worksheets/sheet"+index+".xml",new XDocument(new XElement(main+"worksheet",data))); }
                    Sheet(1,new[]{new[]{"Target","Story","Pattern","UniqueName","Direction","q1_kN_m","q2_kN_m","Action"}}.Concat(levels.Select(l=>new[]{"Beam",l.Name,defaultPattern,"","Global-X","","","ReplaceMatching"})));
                    Sheet(2,new[]{new[]{"Target","Story","Pattern","DiaphragmKey","Fx_kN","Fy_kN","Mz_kNm","X_m","Y_m"}}.Concat(levels.Select(l=>new[]{"UserLoads",l.Name,defaultPattern,"","","","","",""})));
                    Sheet(3,new[]{new[]{"QKS - Nhap truc tiep gia tri tai gio theo Story; khong quy doi"},new[]{"Dien q1/q2 [kN/m] hoac Fx/Fy [kN], Mz [kN.m], X/Y [m]. O rong khong tu thanh 0."},new[]{"UniqueName de trong: chi gan NHAP cho dam dang chon thuoc Story. Nhap UniqueName o dinh dang Text."},new[]{"User Loads: doc khoa diaphragm tu ETABS truoc va tu doi chieu dung Story. Moi lan sua mot User Loads pattern."},new[]{"Gia tri formula dung cache da luu: mo Excel, tinh va luu workbook truoc khi nhap. Khong co tai mau tu dong."}});
                }
                if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);
            } finally { if(File.Exists(temp))File.Delete(temp); }
        }
        public static IReadOnlyList<StoryWindInput> Read(string path,string defaultPattern="")
        {
            if(new FileInfo(path).Length>64L*1024*1024)throw new InvalidDataException("File vượt 64 MiB.");
            string extension=Path.GetExtension(path).ToLowerInvariant();
            if(extension==".csv")return WindInputTable.Parse(Csv(File.ReadAllText(path)),defaultPattern);
            if(extension!=".xlsx"&&extension!=".xlsm")throw new InvalidDataException("Dùng .xlsx/.xlsm/CSV; file .xls có thể lấy qua vùng chọn Excel đang mở.");
            using(var archive=ZipFile.OpenRead(path)) {
                long totalXmlBytes=0;
                XDocument Xml(string entry) {
                    var file=archive.GetEntry(entry)??throw new InvalidDataException("Thiếu phần workbook: "+entry);
                    if(file.Length>32L*1024*1024)throw new InvalidDataException("Worksheet/sharedStrings vượt 32 MiB.");
                    totalXmlBytes+=file.Length;if(totalXmlBytes>64L*1024*1024)throw new InvalidDataException("Tổng XML đọc trong workbook vượt 64 MiB.");
                    using(var stream=file.Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=32L*1024*1024}))return XDocument.Load(reader);
                }
                var workbook=Xml("xl/workbook.xml");var relations=Xml("xl/_rels/workbook.xml.rels").Descendants().Where(e=>e.Name.LocalName=="Relationship"&&e.Attribute("TargetMode")?.Value!="External").ToDictionary(e=>e.Attribute("Id")!.Value,e=>e.Attribute("Target")!.Value,StringComparer.Ordinal);
                var shared=archive.GetEntry("xl/sharedStrings.xml")==null?Array.Empty<string>():Xml("xl/sharedStrings.xml").Descendants().Where(e=>e.Name.LocalName=="si").Select(e=>string.Concat(e.Descendants().Where(t=>t.Name.LocalName=="t").Select(t=>t.Value))).ToArray();
                var sheets=workbook.Descendants().Where(e=>e.Name.LocalName=="sheet").ToArray();
                var named=sheets.Where(e=>new[]{"WindBeams","WindStories"}.Contains(e.Attribute("name")?.Value,StringComparer.OrdinalIgnoreCase)).ToArray();
                if(named.Length>0)sheets=named;
                var result=new List<StoryWindInput>();var errors=new List<string>();
                foreach(var sheet in sheets.Take(64)) {
                    string id=sheet.Attributes().First(a=>a.Name.LocalName=="id").Value;
                    if(!relations.TryGetValue(id,out var target))throw new InvalidDataException("Worksheet relationship thiếu hoặc trỏ ra ngoài workbook.");
                    var uri=new Uri(new Uri("https://qks.invalid/xl/workbook.xml"),target);string entry=uri.AbsolutePath.TrimStart('/');
                    if(uri.Host!="qks.invalid"||!entry.StartsWith("xl/",StringComparison.Ordinal))throw new InvalidDataException("Worksheet target không hợp lệ.");
                    var data=new List<string[]>();
                    foreach(var row in Xml(entry).Descendants().Where(e=>e.Name.LocalName=="row")) {
                        var cells=new Dictionary<int,string>();int sequential=0;
                        foreach(var cell in row.Elements().Where(e=>e.Name.LocalName=="c")) {
                            string address=cell.Attribute("r")?.Value??"";int col=0;foreach(char c in address.TakeWhile(char.IsLetter))col=col*26+(char.ToUpperInvariant(c)-'A'+1);col=col==0?sequential:col-1;sequential=col+1;
                            if(col>=64)continue;string type=cell.Attribute("t")?.Value??"";string value=cell.Elements().FirstOrDefault(e=>e.Name.LocalName=="v")?.Value??"";
                            if(type=="s") { if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out int index)||index<0||index>=shared.Length)throw new InvalidDataException("Shared string không hợp lệ.");value=shared[index]; }
                            else if(type=="inlineStr")value=string.Concat(cell.Descendants().Where(e=>e.Name.LocalName=="t").Select(e=>e.Value));
                            else if(type=="e")value="#EXCEL_ERROR:"+value;
                            if(cell.Elements().Any(e=>e.Name.LocalName=="f")&&value.Length==0)value="#FORMULA_WITHOUT_CACHE";
                            if(cells.ContainsKey(col))throw new InvalidDataException("Worksheet có ô dữ liệu trùng địa chỉ.");cells[col]=value;
                        }
                        int width=cells.Count==0?0:cells.Keys.Max()+1;var values=new string[width];for(int c=0;c<width;c++)values[c]=cells.TryGetValue(c,out var value)?value:"";data.Add(values);
                        if(data.Count>10020)throw new InvalidDataException("Worksheet vượt 10.000 dòng dữ liệu.");
                    }
                    try { result.AddRange(WindInputTable.Parse(data,defaultPattern)); }
                    catch(ArgumentException ex) when(named.Length==0&&ex.Message.StartsWith("Không tìm thấy heading",StringComparison.Ordinal)) { errors.Add(sheet.Attribute("name")?.Value+": "+ex.Message); }
                    if(result.Count>10000)throw new InvalidDataException("Workbook vượt 10.000 dòng tải.");
                }
                if(result.Count==0)throw new InvalidDataException("Không có bảng gió nhận diện được. "+string.Join("; ",errors.Take(3)));
                return result.AsReadOnly();
            }
        }
        public static IReadOnlyList<string[]> Csv(string text)
        {
            var lines=text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            string line=lines.Take(20).FirstOrDefault(value=>
                (value.IndexOf("story",StringComparison.OrdinalIgnoreCase)>=0||value.IndexOf("tầng",StringComparison.OrdinalIgnoreCase)>=0||value.IndexOf("tang",StringComparison.OrdinalIgnoreCase)>=0)&&
                (value.IndexOf("q1",StringComparison.OrdinalIgnoreCase)>=0||value.IndexOf("fx",StringComparison.OrdinalIgnoreCase)>=0||value.IndexOf("fy",StringComparison.OrdinalIgnoreCase)>=0))??lines.FirstOrDefault()??"";
            char separator=new[]{';',',','\t'}.OrderByDescending(c=>line.Count(ch=>ch==c)).First();
            var rows=new List<string[]>();var fields=new List<string>();var field=new StringBuilder();bool quoted=false;
            for(int i=0;i<text.Length;i++) {
                char c=text[i];
                if(c=='"') { if(quoted&&i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else quoted=!quoted; }
                else if(!quoted&&(c==separator||c=='\r'||c=='\n')) {
                    fields.Add(field.ToString());field.Clear();
                    if(c!=separator) { rows.Add(fields.ToArray());fields.Clear();if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++; }
                    if(fields.Count>64||rows.Count>10020)throw new InvalidDataException("CSV vượt giới hạn dòng/cột.");
                } else field.Append(c);
            }
            if(quoted)throw new InvalidDataException("CSV có dấu ngoặc kép chưa đóng.");
            if(field.Length>0||fields.Count>0) { fields.Add(field.ToString());rows.Add(fields.ToArray()); }
            return rows.AsReadOnly();
        }
    }
}
