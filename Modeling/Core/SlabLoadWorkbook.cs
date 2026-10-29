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
    // Export original slab assignments, independent of viewport clipping and display deduplication.
    public sealed class SlabLoadReportRow
    {
        public ModelElement Element { get; }
        public ExistingLoad Load { get; }
        public string LoadSet => ReadLoadDisplay.LoadSetName(Load);
        public SlabLoadReportRow(ModelElement element, ExistingLoad load) { Element = element; Load = load; }
    }

    public static class SlabLoadWorkbook
    {
        public static IReadOnlyList<SlabLoadReportRow> Rows(ModelSnapshot model, bool sets, string? filter, string? story)
        {
            var slabs = model.Elements.Where(e => e.Kind == ElementKind.Slab && (story == null || e.Story == story))
                .ToDictionary(e => e.Id, StringComparer.Ordinal);
            return model.Loads.Where(l => slabs.ContainsKey(l.ElementId) &&
                (sets ? ReadLoadDisplay.LoadSetName(l).Length > 0 && (filter == null || ReadLoadDisplay.LoadSetName(l) == filter)
                      : filter == null || l.Pattern == filter))
                .Select(l => new SlabLoadReportRow(slabs[l.ElementId], l))
                .OrderBy(r => r.Element.Story, StringComparer.Ordinal).ThenBy(r => r.Element.Name, StringComparer.Ordinal)
                .ThenBy(r => r.LoadSet, StringComparer.Ordinal).ThenBy(r => r.Load.Pattern, StringComparer.Ordinal)
                .ThenBy(r => r.Load.Direction).ThenBy(r => r.Load.Step).ToArray();
        }

        public static int Write(string path, ModelSnapshot model, bool sets, string? filter, string? story)
        {
            var rows = Rows(model, sets, filter, story);
            var sheets = new List<KeyValuePair<string, List<object?[]>>>();
            void Add(string name, IEnumerable<object?[]> values) => sheets.Add(new KeyValuePair<string, List<object?[]>>(name, values.ToList()));
            Add("Thong tin", new[] {
                new object?[]{"QKS · TẢI SÀN ĐÃ ĐỌC", "Thông tin báo cáo"},
                new object?[]{"Mô hình", model.Identity}, new object?[]{"Nguồn / phiên bản", model.Source + " " + model.Version},
                new object?[]{"Dữ liệu đọc lúc (UTC)", model.CapturedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)},
                new object?[]{"Xuất lúc (UTC)", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)},
                new object?[]{"Chế độ", sets ? "Load Sets" : "Load Pattern"}, new object?[]{"Bộ lọc", filter ?? "Tất cả"},
                new object?[]{"Phạm vi", story ?? "Toàn bộ tầng"}, new object?[]{"Số bản ghi", rows.Count},
                new object?[]{"Quy tắc", "Toàn bộ tải sàn đã đọc trong phạm vi; gồm tải bằng 0, không theo chọn/ẩn/zoom. Mỗi dòng chi tiết là một thành phần tải gốc; không cộng tải trực tiếp với tải từ set."},
                new object?[]{"Direction API", "1/2/3: Local 1/2/3; 4/5/6: X/Y/Z; 7/8/9: X/Y/Z projected; 10: Gravity; 11: Gravity projected. Giữ nguyên dấu tải API."},
                new object?[]{"Load Sets", "Xuất mọi Load Pattern thành phần của set được chọn, độc lập với bộ lọc Load Pattern."},
                new object?[]{"Giới hạn", "Chỉ dữ liệu API đã đọc, không bổ sung tải tự sinh hay suy diễn tải thiếu. Tầng theo Story của đối tượng; đơn vị ghi riêng trên từng dòng."}
            }.Concat(model.Diagnostics.Select(d => new object?[]{"Chẩn đoán đọc API", d})));
            var summary = new List<object?[]> { new object?[]{"Story", "Load Set", "Load Pattern", "Loại tải", "Hệ tọa độ", "Direction API", "Giá trị 1", "Giá trị 2", "Đơn vị", "Step", "Số ô sàn", "Số bản ghi"} };
            foreach (var g in rows.GroupBy(r => new {r.Element.Story, r.LoadSet, r.Load.Pattern, r.Load.Kind, r.Load.CoordinateSystem, r.Load.Direction, r.Load.Value1, r.Load.Value2, r.Load.Unit, r.Load.Step}))
                summary.Add(new object?[]{g.Key.Story, g.Key.LoadSet, g.Key.Pattern, g.Key.Kind, g.Key.CoordinateSystem, g.Key.Direction, g.Key.Value1, g.Key.Value2, g.Key.Unit, g.Key.Step, g.Select(r => r.Element.Id).Distinct().Count(), g.Count()});
            Add("Tong hop", summary);
            var stories = story != null ? new[]{story} : model.Levels.OrderBy(l => l.Elevation).Select(l => l.Name)
                .Concat(rows.Select(r => r.Element.Story).OrderBy(n => n, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).ToArray();
            var byStory = rows.ToLookup(r => r.Element.Story, StringComparer.Ordinal);
            foreach (string name in stories) {
                var detail = new List<object?[]> { new object?[]{"Story", "UniqueName", "Label", "Section Property", "Load Set", "Load Pattern", "Loại tải", "Hệ tọa độ", "Direction API", "Giá trị 1", "Giá trị 2", "Đơn vị", "Step"} };
                detail.AddRange(byStory[name].Select(r => new object?[]{r.Element.Story, r.Element.Name, r.Element.Label, r.Element.Property, r.LoadSet, r.Load.Pattern, r.Load.Kind, r.Load.CoordinateSystem, r.Load.Direction, r.Load.Value1, r.Load.Value2, r.Load.Unit, r.Load.Step}));
                Add(name, detail);
            }
            WritePackage(path, sheets);
            return rows.Count;
        }

        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static void WritePackage(string path, List<KeyValuePair<string, List<object?[]>>> sheets)
        {
            if (sheets.Any(s => s.Value.Count > 1048576)) throw new InvalidDataException("Số dòng vượt giới hạn Excel. Hãy xuất theo tầng hoặc Pattern / Set.");
            string full = Path.GetFullPath(path), temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships", package = "http://schemas.openxmlformats.org/package/2006/relationships", types = "http://schemas.openxmlformats.org/package/2006/content-types";
            try {
                using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create)) {
                    void Entry(string name, XElement xml) { using (var stream = zip.CreateEntry(name).Open()) using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) })) new XDocument(xml).Save(writer); }
                    XElement Link(string id, string kind, string target) => new XElement(package + "Relationship", new XAttribute("Id", id), new XAttribute("Type", rel.NamespaceName + "/" + kind), new XAttribute("Target", target));
                    var content = new XElement(types + "Types", new XElement(types + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")), new XElement(types + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")));
                    void Override(string part, string kind) => content.Add(new XElement(types + "Override", new XAttribute("PartName", part), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml." + kind + "+xml")));
                    Override("/xl/workbook.xml", "sheet.main"); Override("/xl/styles.xml", "styles");
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var list = new XElement(Main + "sheets"); var links = new XElement(package + "Relationships", Link("styles", "styles", "styles.xml"));
                    for (int i = 0; i < sheets.Count; i++) {
                        string id = "rId" + (i + 1), file = "worksheets/sheet" + (i + 1) + ".xml";
                        string name = SheetName(sheets[i].Key, names);
                        list.Add(new XElement(Main + "sheet", new XAttribute("name", name), new XAttribute("sheetId", i + 1), new XAttribute(rel + "id", id)));
                        links.Add(Link(id, "worksheet", file)); Override("/xl/" + file, "worksheet");
                        Entry("xl/" + file, Worksheet(sheets[i].Value));
                    }
                    Entry("[Content_Types].xml", content);
                    Entry("_rels/.rels", new XElement(package + "Relationships", Link("rId1", "officeDocument", "xl/workbook.xml")));
                    Entry("xl/workbook.xml", new XElement(Main + "workbook", new XAttribute(XNamespace.Xmlns + "r", rel), list));
                    Entry("xl/_rels/workbook.xml.rels", links);
                    Entry("xl/styles.xml", XElement.Parse(Styles));
                }
                if (File.Exists(full)) File.Replace(temp, full, null); else File.Move(temp, full);
            } finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        private static string SheetName(string input, HashSet<string> used)
        {
            string basis = new string(input.Select(c => ":\\/?*[]".Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim('\'');
            if (string.IsNullOrWhiteSpace(basis)) basis = "Khong co Story";
            if (basis.Length > 31) basis = basis.Substring(0, 31).TrimEnd('\'');
            if (string.Equals(basis, "History", StringComparison.OrdinalIgnoreCase)) basis += "_";
            string name = basis; int suffix = 1;
            while (!used.Add(name)) { string tail = " (" + (++suffix) + ")"; name = basis.Substring(0, Math.Min(basis.Length, 31 - tail.Length)) + tail; }
            return name;
        }

        private static XElement Worksheet(List<object?[]> rows)
        {
            var data = new XElement(Main + "sheetData");
            for (int r = 0; r < rows.Count; r++) {
                var row = new XElement(Main + "row", new XAttribute("r", r + 1));
                for (int c = 0; c < rows[r].Length; c++) {
                    object? value = rows[r][c]; bool numeric = value is double || value is int;
                    var cell = new XElement(Main + "c", new XAttribute("r", ((char)('A' + c)).ToString() + (r + 1)), new XAttribute("s", r == 0 ? 1 : numeric ? 2 : 0));
                    if (numeric) cell.Add(new XElement(Main + "v", value is double number ? number.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture)));
                    else cell.Add(new XAttribute("t", "inlineStr"), new XElement(Main + "is", new XElement(Main + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), Convert.ToString(value, CultureInfo.InvariantCulture) ?? "")));
                    row.Add(cell);
                }
                data.Add(row);
            }
            int width = rows.Max(r => r.Length);
            return new XElement(Main + "worksheet", new XElement(Main + "sheetViews", new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0), new XElement(Main + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
                new XElement(Main + "cols", Enumerable.Range(1, width).Select(c => new XElement(Main + "col", new XAttribute("min", c), new XAttribute("max", c), new XAttribute("width", c == 7 ? 34 : 22), new XAttribute("customWidth", 1)))), data,
                new XElement(Main + "autoFilter", new XAttribute("ref", "A1:" + ((char)('A' + width - 1)) + rows.Count)));
        }

        private const string Styles = @"<styleSheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'>
<fonts count='2'><font><sz val='11'/><name val='Calibri'/></font><font><b/><sz val='11'/><color rgb='FFFFFFFF'/><name val='Calibri'/></font></fonts>
<fills count='3'><fill><patternFill patternType='none'/></fill><fill><patternFill patternType='gray125'/></fill><fill><patternFill patternType='solid'><fgColor rgb='FF102A4C'/><bgColor indexed='64'/></patternFill></fill></fills>
<borders count='1'><border><left/><right/><top/><bottom/><diagonal/></border></borders>
<cellStyleXfs count='1'><xf numFmtId='0' fontId='0' fillId='0' borderId='0'/></cellStyleXfs>
<cellXfs count='3'><xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0'><alignment vertical='top' wrapText='1'/></xf><xf numFmtId='0' fontId='1' fillId='2' borderId='0' xfId='0' applyFont='1' applyFill='1'><alignment vertical='center' wrapText='1'/></xf><xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0'><alignment vertical='top'/></xf></cellXfs>
<cellStyles count='1'><cellStyle name='Normal' xfId='0' builtinId='0'/></cellStyles></styleSheet>";
    }
}
