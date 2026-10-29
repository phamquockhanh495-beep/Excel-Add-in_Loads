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

        // One row per Story / Load Set. Several values remain separate lines, never a sum.
        private sealed class LoadValue
        {
            public double Number { get; }
            public string Unit { get; }
            public LoadValue(double number, string unit) { Number = number; Unit = unit; }
        }

        public static int Write(string path, ModelSnapshot model, bool sets, string? filter, string? story)
        {
            var rows = Rows(model, sets, filter, story);
            var patterns = rows.Select(r => r.Load.Pattern).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            if (patterns.Length == 0) patterns = !sets && filter != null ? new[]{filter} : model.LoadPatterns.ToArray();
            if (patterns.Length == 0) patterns = new[]{"(Không có tải)"};
            if (patterns.Length > 16381) throw new InvalidDataException("Số Pattern vượt giới hạn cột Excel. Hãy xuất theo Pattern / Set.");
            var stories = story != null ? new[]{story} : model.Levels.OrderByDescending(l => l.Elevation).Select(l => l.Name)
                .Concat(rows.Select(r => r.Element.Story).OrderBy(n => n, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).ToArray();
            var sheets = new List<KeyValuePair<string, List<object?[]>>>();
            var byStory = rows.ToLookup(r => r.Element.Story, StringComparer.Ordinal);
            List<object?[]> Matrix(IEnumerable<string> levels)
            {
                var header = new object?[patterns.Length + 3]; header[0] = "STT"; header[1] = "Story"; header[2] = "Load Sets"; header[3] = "Load Pattern";
                var subheader = new object?[patterns.Length + 3]; Array.Copy(patterns, 0, subheader, 3, patterns.Length);
                var table = new List<object?[]> {header, subheader}; int index = 0;
                foreach (var level in levels)
                    foreach (var group in byStory[level].GroupBy(r => r.LoadSet).OrderBy(g => g.Key, StringComparer.Ordinal)) {
                        var row = new object?[patterns.Length + 3]; row[0] = ++index; row[1] = level;
                        row[2] = group.Key.Length == 0 ? "(Gán trực tiếp)" : group.Key;
                        var byPattern = group.ToLookup(r => r.Load.Pattern, StringComparer.Ordinal);
                        for (int c = 0; c < patterns.Length; c++) {
                            var loads = byPattern[patterns[c]].Select(r => r.Load)
                                .GroupBy(l => new {l.Kind, l.CoordinateSystem, l.Direction, l.Value1, l.Value2, l.Unit})
                                .Select(g => g.First()).OrderBy(l => l.Kind, StringComparer.Ordinal).ThenBy(l => l.CoordinateSystem, StringComparer.Ordinal)
                                .ThenBy(l => l.Direction).ThenBy(l => l.Value1).ThenBy(l => l.Value2).ThenBy(l => l.Unit, StringComparer.Ordinal).ToArray();
                            if (loads.Length == 0) continue;
                            bool Plain(ExistingLoad l) => l.Direction == 10 && l.CoordinateSystem == "Global" &&
                                (l.Kind == "Area uniform" || ReadLoadDisplay.LoadSetName(l).Length > 0);
                            if (loads.Length == 1 && loads[0].Value1 == loads[0].Value2 && Plain(loads[0]))
                                row[c + 3] = new LoadValue(loads[0].Value1, loads[0].Unit);
                            else row[c + 3] = string.Join("\n", loads.Select(l => l.Value1.ToString("R", CultureInfo.InvariantCulture) +
                                (l.Value1 == l.Value2 ? "" : " → " + l.Value2.ToString("R", CultureInfo.InvariantCulture)) + " " + l.Unit +
                                (Plain(l) ? "" : " · " + l.CoordinateSystem + " / " + Direction(l.Direction) + " · " + l.Kind)));
                        }
                        table.Add(row);
                    }
                return table;
            }
            sheets.Add(new KeyValuePair<string, List<object?[]>>("Tong hop", Matrix(stories)));
            foreach (string name in stories) sheets.Add(new KeyValuePair<string, List<object?[]>>(name, Matrix(new[]{name})));
            WritePackage(path, sheets, model.Loads.Select(ReadLoadDisplay.LoadSetName).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray());
            return rows.Count;
        }

        private static string Direction(int value)
        {
            var names = new[]{"", "Local 1", "Local 2", "Local 3", "X", "Y", "Z", "X projected", "Y projected", "Z projected", "Gravity", "Gravity projected"};
            return value > 0 && value < names.Length ? names[value] : "Dir " + value;
        }

        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static void WritePackage(string path, List<KeyValuePair<string, List<object?[]>>> sheets, string[] setNames)
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
                    var units = sheets.SelectMany(s => s.Value).SelectMany(r => r).OfType<LoadValue>().Select(v => v.Unit).Distinct(StringComparer.Ordinal).ToList();
                    var formatting = new WorkbookStyles(units, setNames);
                    var storyBands = sheets[0].Value.Skip(2).Select(r => (string)r[1]!).Distinct(StringComparer.Ordinal).Select((name, index) => new {name, band = index % 2}).ToDictionary(x => x.name, x => x.band, StringComparer.Ordinal);
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var list = new XElement(Main + "sheets"); var links = new XElement(package + "Relationships", Link("styles", "styles", "styles.xml"));
                    for (int i = 0; i < sheets.Count; i++) {
                        string id = "rId" + (i + 1), file = "worksheets/sheet" + (i + 1) + ".xml";
                        string name = SheetName(sheets[i].Key, names);
                        list.Add(new XElement(Main + "sheet", new XAttribute("name", name), new XAttribute("sheetId", i + 1), new XAttribute(rel + "id", id)));
                        links.Add(Link(id, "worksheet", file)); Override("/xl/" + file, "worksheet");
                        Entry("xl/" + file, Worksheet(sheets[i].Value, units, formatting, storyBands));
                    }
                    Entry("[Content_Types].xml", content);
                    Entry("_rels/.rels", new XElement(package + "Relationships", Link("rId1", "officeDocument", "xl/workbook.xml")));
                    Entry("xl/workbook.xml", new XElement(Main + "workbook", new XAttribute(XNamespace.Xmlns + "r", rel), list));
                    Entry("xl/_rels/workbook.xml.rels", links);
                    Entry("xl/styles.xml", formatting.Xml);

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

        private static string Column(int index)
        {
            string name = "";
            for (int n = index + 1; n > 0; n = (n - 1) / 26) name = (char)('A' + (n - 1) % 26) + name;
            return name;
        }

        private static XElement Worksheet(List<object?[]> rows, List<string> units, WorkbookStyles formatting, IReadOnlyDictionary<string, int> storyBands)
        {
            var data = new XElement(Main + "sheetData"); int width = rows[0].Length;
            for (int r = 0; r < rows.Count; r++) {
                int lines = rows[r].Max(v => v is string text ? text.Count(c => c == '\n') + 1 : 1);
                var row = new XElement(Main + "row", new XAttribute("r", r + 1), new XAttribute("ht", r == 0 ? 38 : r == 1 ? 24 : Math.Min(409, Math.Max(24, lines * 18))), new XAttribute("customHeight", 1));
                for (int c = 0; c < width; c++) {
                    object? value = rows[r][c]; var load = value as LoadValue;
                    bool numeric = value is int || load != null;
                    int style = r == 0 || r == 1 && c < 3 ? 1 : r == 1 ? 2 : load != null ? 4 + units.IndexOf(load.Unit) : c == 0 ? 3 : 0;
                    if (r >= 2) style = formatting.DataStyle(style, storyBands[(string)rows[r][1]!], c == 2 ? (string?)value : null);
                    var cell = new XElement(Main + "c", new XAttribute("r", Column(c) + (r + 1)), new XAttribute("s", style));
                    if (numeric) cell.Add(new XElement(Main + "v", load != null ? load.Number.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture)));
                    else if (value != null) {
                        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                        if (text.Length > 32767) throw new InvalidDataException("Nội dung ô vượt giới hạn Excel. Hãy thu hẹp phạm vi xuất.");
                        cell.Add(new XAttribute("t", "inlineStr"), new XElement(Main + "is", new XElement(Main + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text)));
                    }
                    row.Add(cell);
                }
                data.Add(row);
            }
            var merges = new XElement(Main + "mergeCells", new XAttribute("count", width > 4 ? 4 : 3),
                new[]{"A1:A2", "B1:B2", "C1:C2"}.Select(range => new XElement(Main + "mergeCell", new XAttribute("ref", range))));
            if (width > 4) merges.Add(new XElement(Main + "mergeCell", new XAttribute("ref", "D1:" + Column(width - 1) + "1")));
            return new XElement(Main + "worksheet", new XElement(Main + "sheetViews", new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0), new XElement(Main + "pane", new XAttribute("xSplit", 3), new XAttribute("ySplit", 2), new XAttribute("topLeftCell", "D3"), new XAttribute("activePane", "bottomRight"), new XAttribute("state", "frozen")))),
                new XElement(Main + "cols", Enumerable.Range(0, width).Select(c => new XElement(Main + "col", new XAttribute("min", c + 1), new XAttribute("max", c + 1), new XAttribute("width", c == 0 ? 7 : c == 1 ? 20 : c == 2 ? 30 : 22), new XAttribute("customWidth", 1)))), data,
                merges,
                new XElement(Main + "pageMargins", new XAttribute("left", .25), new XAttribute("right", .25), new XAttribute("top", .5), new XAttribute("bottom", .5), new XAttribute("header", .2), new XAttribute("footer", .2)),
                new XElement(Main + "pageSetup", new XAttribute("orientation", "landscape"), new XAttribute("paperSize", 9)));
        }

        // Cache combinations so a large export reuses cell formats instead of creating one per cell.
        private sealed class WorkbookStyles
        {
            public XElement Xml { get; } = XElement.Parse(Styles);
            private readonly Dictionary<string, int> setFonts = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> combinations = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly XElement xfs;
            public WorkbookStyles(List<string> units, string[] setNames)
            {
                xfs = Xml.Element(Main + "cellXfs")!;
                var formats = new XElement(Main + "numFmts", new XAttribute("count", units.Count));
                for (int i = 0; i < units.Count; i++) {
                    string literal = units[i].Replace("\\", "\\\\").Replace("\"", "\\\"");
                    formats.Add(new XElement(Main + "numFmt", new XAttribute("numFmtId", 164 + i), new XAttribute("formatCode", "0.###############\" " + literal + "\"")));
                    var xf = new XElement(xfs.Elements().First()); xf.SetAttributeValue("numFmtId", 164 + i); xf.SetAttributeValue("applyNumberFormat", 1); xfs.Add(xf);
                }
                Xml.AddFirst(formats);
                var fonts = Xml.Element(Main + "fonts")!;
                string[] palette = {"FF1D4ED8", "FF9D174D", "FF047857", "FF7C3AED", "FFB45309", "FF0E7490", "FFBE123C", "FF4338CA", "FF3F6212", "FF6B21A8", "FF9A3412", "FF0F766E"};
                var colors = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < setNames.Length; i++) {
                    string color;
                    if (i < palette.Length) color = palette[i];
                    else {
                        using (var sha = System.Security.Cryptography.SHA256.Create()) {
                            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(setNames[i]));
                            int rgb = (32 + hash[0] % 80) << 16 | (32 + hash[1] % 80) << 8 | (32 + hash[2] % 80);
                            color = "FF" + rgb.ToString("X6", CultureInfo.InvariantCulture);
                            while (colors.Contains(color)) { rgb = (rgb + 1) & 0x7F7F7F; color = "FF" + rgb.ToString("X6", CultureInfo.InvariantCulture); }
                        }
                    }
                    colors.Add(color); setFonts.Add(setNames[i], fonts.Elements().Count());
                    fonts.Add(new XElement(Main + "font", new XElement(Main + "b"), new XElement(Main + "sz", new XAttribute("val", 11)), new XElement(Main + "color", new XAttribute("rgb", color)), new XElement(Main + "name", new XAttribute("val", "Calibri"))));
                }
                fonts.SetAttributeValue("count", fonts.Elements().Count());
                xfs.SetAttributeValue("count", xfs.Elements().Count());
            }
            public int DataStyle(int basis, int band, string? set)
            {
                int font = set != null && setFonts.TryGetValue(set, out int id) ? id : 0;
                string key = basis + ":" + band + ":" + font;
                if (combinations.TryGetValue(key, out int existing)) return existing;
                var style = new XElement(xfs.Elements().ElementAt(basis));
                style.SetAttributeValue("fillId", 3 + band); style.SetAttributeValue("applyFill", 1);
                if (font != 0) { style.SetAttributeValue("fontId", font); style.SetAttributeValue("applyFont", 1); }
                int result = xfs.Elements().Count(); xfs.Add(style); xfs.SetAttributeValue("count", result + 1);
                combinations.Add(key, result); return result;
            }
        }

        private const string Styles = @"<styleSheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'>
<fonts count='2'><font><sz val='11'/><name val='Calibri'/></font><font><b/><sz val='11'/><color rgb='FFFFFFFF'/><name val='Calibri'/></font></fonts>
<fills count='5'><fill><patternFill patternType='none'/></fill><fill><patternFill patternType='gray125'/></fill><fill><patternFill patternType='solid'><fgColor rgb='FF002060'/><bgColor indexed='64'/></patternFill></fill><fill><patternFill patternType='solid'><fgColor rgb='FFF0F6FC'/><bgColor indexed='64'/></patternFill></fill><fill><patternFill patternType='solid'><fgColor rgb='FFFFF8E9'/><bgColor indexed='64'/></patternFill></fill></fills>
<borders count='1'><border><left style='thin'><color rgb='FF8297B0'/></left><right style='thin'><color rgb='FF8297B0'/></right><top style='thin'><color rgb='FFD4D4D4'/></top><bottom style='thin'><color rgb='FFD4D4D4'/></bottom><diagonal/></border></borders>
<cellStyleXfs count='1'><xf numFmtId='0' fontId='0' fillId='0' borderId='0'/></cellStyleXfs>
<cellXfs count='4'>
<xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0' applyAlignment='1' applyBorder='1'><alignment horizontal='left' vertical='center' wrapText='1'/></xf>
<xf numFmtId='0' fontId='1' fillId='2' borderId='0' xfId='0' applyFont='1' applyFill='1' applyAlignment='1' applyBorder='1'><alignment horizontal='center' vertical='center' wrapText='1'/></xf>
<xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0' applyAlignment='1' applyBorder='1'><alignment horizontal='center' vertical='center' wrapText='1'/></xf>
<xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0' applyAlignment='1' applyBorder='1'><alignment horizontal='right' vertical='center'/></xf>
</cellXfs><cellStyles count='1'><cellStyle name='Normal' xfId='0' builtinId='0'/></cellStyles></styleSheet>";
    }
}
