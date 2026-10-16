using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Web.Script.Serialization;

namespace QKS_Loads_AcadPlugin
{
    #region Data Models for Loads
    public class CadExportVisualSettings
    {
        public double TextHeight { get; set; } = 200.0;
        public int TextColorIndex { get; set; } = 1;
        public int BeamColorIndex { get; set; } = 8;
        public int DotColorIndex { get; set; } = 2;
        public double DotRadius { get; set; } = 40.0;
        public string TextLayerName { get; set; } = "QKS_WALL_LOAD_TEXT";
        public string BeamLayerName { get; set; } = "QKS_WALL_LOAD_BEAM";
        public string DotLayerName { get; set; } = "QKS_WALL_LOAD_DOT";
        public bool UseSectionRgbColor { get; set; } = true;
    }

    public class CadLoadSegment
    {
        public double StartDist { get; set; }
        public double EndDist { get; set; }
        public double Val1 { get; set; }
        public double Val2 { get; set; }
        public double MidX { get; set; }
        public double MidY { get; set; }
        public double AngleDeg { get; set; }
        public string Text { get; set; }
    }

    public class CadBreakDot
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double DistAlongBeam { get; set; }
    }

    public class CadBeamLoadExportItem
    {
        public string BeamName { get; set; }
        public string Section { get; set; }
        public int ColorR { get; set; } = -1;
        public int ColorG { get; set; } = -1;
        public int ColorB { get; set; } = -1;
        public string ColorHex { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Length { get; set; }
        public double AngleDeg { get; set; }
        public List<CadLoadSegment> Segments { get; set; }
        public List<CadBreakDot> BreakDots { get; set; }
    }

    public class CadExportPayload
    {
        public string Action { get; set; }
        public string Story { get; set; }
        public string LoadPattern { get; set; }
        public string DirectionName { get; set; }
        public int DirectionCode { get; set; }
        public string UnitLabel { get; set; } = "kN/m";
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public bool DrawLegend { get; set; }
        public double LegendPointX { get; set; }
        public double LegendPointY { get; set; }
        public CadExportVisualSettings Settings { get; set; }
        public List<CadBeamLoadExportItem> Beams { get; set; }
    }

    public class CadImportedTextItem
    {
        public string TextValue { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string XDataBeamName { get; set; }
        public string Section { get; set; }
    }

    public class CadImportedDotItem
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class CadImportedTickItem
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double MidX { get; set; }
        public double MidY { get; set; }
        public string XDataBeamName { get; set; }
        public string Section { get; set; }
    }

    public class CadImportedBeamLineItem
    {
        public string BeamName { get; set; }
        public string Section { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }

    public class CadImportPayload
    {
        public string Action { get; set; }
        public List<CadImportedTextItem> Texts { get; set; }
        public List<CadImportedDotItem> Dots { get; set; }
        public List<CadImportedTickItem> Ticks { get; set; }
        public List<CadImportedBeamLineItem> BeamLines { get; set; }
    }

    public class AutoCadWallLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double Z1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Z2 { get; set; }
        public string Thickness { get; set; } = "200";
        public string BeamName { get; set; } = string.Empty;
    }

    public class AutoCadWallQuery
    {
        public string Action { get; set; }
        public List<AutoCadWallLine> Lines { get; set; }
    }

    public class CadShellPolygonItem
    {
        public string AreaName { get; set; }
        public string Section { get; set; }
        public List<double[]> Points { get; set; }
        public double CentroidX { get; set; }
        public double CentroidY { get; set; }
        public double AreaM2 { get; set; }
        public int ColorR { get; set; } = 200;
        public int ColorG { get; set; } = 200;
        public int ColorB { get; set; } = 200;
        public string SectionText { get; set; }
        public string LoadText { get; set; }
        public bool HasLoad { get; set; }
    }

    public class CadShellExportPayload
    {
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public double TextHeight { get; set; } = 250.0;
        public int TextColorIndex { get; set; } = 4;
        public double MinAreaM2 { get; set; } = 0.5;
        public bool DrawBoundary { get; set; } = true;
        public bool DrawHatch { get; set; } = true;
        public int HatchTransparency { get; set; } = 70;
        public bool DrawRectangle { get; set; } = true;
        public bool DrawText { get; set; } = true;
        public string StoryName { get; set; }
        public string ModeTitle { get; set; }
        public List<CadShellPolygonItem> Slabs { get; set; }
    }

    public class CadShellLiveUpdatePayload
    {
        public string TargetSection { get; set; }
        public string NewLoadText { get; set; }
        public Dictionary<string, string> SectionLoadMap { get; set; }
        public int TextColorIndex { get; set; } = 4;
    }

    public class CadWallLiveUpdatePayload
    {
        public string Action { get; set; } = "UPDATE_WALL_LOAD_LIVE";
        public string Story { get; set; }
        public string LoadPattern { get; set; }
        public Dictionary<string, CadWallLiveBeamItem> BeamLoads { get; set; } = new Dictionary<string, CadWallLiveBeamItem>(StringComparer.OrdinalIgnoreCase);
    }

    public class CadWallLiveBeamItem
    {
        public string BeamName { get; set; }
        public string Section { get; set; }
        public string LoadText { get; set; } = "0";
        public double MaxLoadValue { get; set; } = 0.0;
        public List<CadLoadSegment> Segments { get; set; } = new List<CadLoadSegment>();
    }

    public class CadShellZoneRectPayload
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }

    public class CadShellImportItem
    {
        public string AreaName { get; set; }
        public string Section { get; set; }
        public string RawText { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class CadSlabTableExportPayload
    {
        public string StoryName { get; set; }
        public double InsertX { get; set; }
        public double InsertY { get; set; }
        public double TextHeight { get; set; } = 250.0;
        public List<CadSlabTableItem> Items { get; set; }
    }

    public class CadSlabTableItem
    {
        public int Stt { get; set; }
        public string SectionName { get; set; }
        public int ColorR { get; set; } = 200;
        public int ColorG { get; set; } = 200;
        public int ColorB { get; set; } = 200;
        public int Count { get; set; }
        public double TotalAreaM2 { get; set; }
        public string LoadSummary { get; set; }
    }
    #endregion

    /// <summary>
    /// AutoCAD Plugin cho Extension QKS - Module Loads
    /// </summary>
    public class QksLoadsExtension : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                string loadsDir = @"C:\QKS_JSON_Data\Loads";
                if (!Directory.Exists(loadsDir))
                {
                    Directory.CreateDirectory(loadsDir);
                }

                Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;
                if (ed != null)
                {
                    ed.WriteMessage("\n=======================================================");
                    ed.WriteMessage("\n [Extension QKS - Loads] Plugin AutoCAD đã nạp thành công!");
                    ed.WriteMessage("\n Các lệnh khả dụng:");
                    ed.WriteMessage("\n  - QKS_WALL_DRAW_BEAMS     : Vẽ mặt bằng dầm + Dot ngưng tải + Text tải trọng");
                    ed.WriteMessage("\n  - QKS_WALL_SCAN_LOADS     : Quét các Text tải trọng đã sửa để nạp vào ETABS");
                    ed.WriteMessage("\n  - QKS_WALL_UPDATE_TEXT    : Cập nhật Text tải tường thời gian thực (Live CAD Sync)");
                    ed.WriteMessage("\n  - QKS_WALL_PICK_BASEPOINT : Pick điểm gốc BasePoint");
                    ed.WriteMessage("\n  - QKS_WALL_GET_LINES      : Chọn các line tường kiến trúc trên CAD");
                    ed.WriteMessage("\n=======================================================\n");
                }
            }
            catch { }
        }

        public void Terminate() { }
    }

    /// <summary>
    /// Các hàm lệnh CommandMethod thực thi trực tiếp In-Process trong AutoCAD
    /// </summary>
    public static class QksLoadsCommands
    {
        private static JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        #region Layer & XData Helpers
        private static string EnsureLayerExists(Transaction tr, Database db, string layerName, int colorIndex)
        {
            short targetColor = (short)(colorIndex <= 0 ? 7 : colorIndex);
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, targetColor)
                };
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
            else
            {
                ObjectId layerId = lt[layerName];
                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, targetColor);
            }
            return layerName;
        }

        private static void EnsureRegApp(Transaction tr, Database db, string appName)
        {
            RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!rat.Has(appName))
            {
                rat.UpgradeOpen();
                RegAppTableRecord ratr = new RegAppTableRecord { Name = appName };
                rat.Add(ratr);
                tr.AddNewlyCreatedDBObject(ratr, true);
            }
        }

        private static void SetXData(DBObject obj, string appName, string data)
        {
            var rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, data)
            );
            obj.XData = rb;
        }

        private static string GetXData(DBObject obj, string appName)
        {
            ResultBuffer rb = obj.GetXDataForApplication(appName);
            if (rb != null)
            {
                foreach (TypedValue tv in rb)
                {
                    if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                    {
                        return tv.Value?.ToString();
                    }
                }
            }
            return string.Empty;
        }

        private static Autodesk.AutoCAD.Colors.Color GetColorByLoadValue(double val)
        {
            double absV = Math.Abs(val);
            if (absV <= 5.0)
            {
                // Tải nhẹ (<= 5 kN/m): Màu Cyan (ACI 4)
                return Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 4);
            }
            else if (absV <= 12.0)
            {
                // Tải trung bình (5 - 12 kN/m): Màu Vàng (ACI 2)
                return Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 2);
            }
            else
            {
                // Tải nặng (> 12 kN/m): Màu Đỏ (ACI 1)
                return Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
            }
        }
        #endregion

        #region CommandMethods (In-Process Hybrid Mode)
        /// <summary>
        /// LỆNH 1: VẼ MẶT BẰNG DẦM + DOT NGƯNG TẢI + TEXT TẢI TRỌNG
        /// Cú pháp: QKS_WALL_DRAW_BEAMS "<jsonPath>" <pasteX> <pasteY> <textHeight> <textColor> <beamColor>
        /// </summary>
        [CommandMethod("QKS_WALL_DRAW_BEAMS", CommandFlags.Modal)]
        public static void DrawWallLoadsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. Đọc các tham số truyền vào
                PromptResult resJson = ed.GetString("\nĐường dẫn JSON: ");
                if (resJson.Status != PromptStatus.OK) return;
                string jsonPath = resJson.StringResult.Trim('\"');

                PromptDoubleResult resX = ed.GetDouble("\nTọa độ X điểm dán: ");
                if (resX.Status != PromptStatus.OK) return;
                double pasteX = resX.Value;

                PromptDoubleResult resY = ed.GetDouble("\nTọa độ Y điểm dán: ");
                if (resY.Status != PromptStatus.OK) return;
                double pasteY = resY.Value;

                PromptDoubleResult resH = ed.GetDouble("\nChiều cao chữ: ");
                if (resH.Status != PromptStatus.OK) return;
                double textHeight = resH.Value;

                PromptIntegerResult resTextColor = ed.GetInteger("\nMã màu text tải: ");
                if (resTextColor.Status != PromptStatus.OK) return;
                int textColorIndex = resTextColor.Value;

                PromptIntegerResult resBeamColor = ed.GetInteger("\nMã màu frame dầm: ");
                if (resBeamColor.Status != PromptStatus.OK) return;
                int beamColorIndex = resBeamColor.Value;

                // 2. Đọc file JSON
                if (!File.Exists(jsonPath))
                {
                    ed.WriteMessage($"\n[QKS Error] Không tìm thấy file dữ liệu tại: {jsonPath}");
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var payload = _json.Deserialize<CadExportPayload>(jsonContent);
                if (payload == null || payload.Beams == null || payload.Beams.Count == 0)
                {
                    ed.WriteMessage("\n[QKS Error] Dữ liệu dầm xuất rỗng.");
                    return;
                }

                // 3. Khởi tạo Transaction vẽ hàng loạt đối tượng trong AutoCAD
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string beamLayer = EnsureLayerExists(tr, db, "QKS_WALL_LOAD_BEAM", beamColorIndex);
                    string segLayer = EnsureLayerExists(tr, db, "QKS_WALL_LOAD_SEGMENT", 2); // Yellow default
                    string textLayer = EnsureLayerExists(tr, db, "QKS_WALL_LOAD_TEXT", textColorIndex);
                    EnsureRegApp(tr, db, "QKS_LOAD");

                    int beamCount = 0;
                    int segmentCount = 0;
                    int segPolyCount = 0;

                    // Độ dịch chuyển tọa độ: dX = pasteX - baseX, dY = pasteY - baseY
                    double dX = pasteX - payload.BasePointX;
                    double dY = pasteY - payload.BasePointY;

                    double th = textHeight > 0 ? textHeight : 200.0;

                    foreach (var beam in payload.Beams)
                    {
                        double p1X = beam.X1 + dX;
                        double p1Y = beam.Y1 + dY;
                        double p2X = beam.X2 + dX;
                        double p2Y = beam.Y2 + dY;

                        double bLen = Math.Sqrt((p2X - p1X) * (p2X - p1X) + (p2Y - p1Y) * (p2Y - p1Y));
                        double ux = bLen > 0.001 ? (p2X - p1X) / bLen : 1.0;
                        double uy = bLen > 0.001 ? (p2Y - p1Y) / bLen : 0.0;

                        // 1. Vẽ trục dầm
                        Line beamLine = new Line(new Point3d(p1X, p1Y, 0), new Point3d(p2X, p2Y, 0));
                        beamLine.Layer = beamLayer;

                        // Nếu bật chế độ màu RGB theo tiết diện dầm
                        bool useRgb = payload.Settings != null && payload.Settings.UseSectionRgbColor;
                        if (useRgb && beam.ColorR >= 0 && beam.ColorG >= 0 && beam.ColorB >= 0)
                        {
                            beamLine.Color = Autodesk.AutoCAD.Colors.Color.FromRgb((byte)beam.ColorR, (byte)beam.ColorG, (byte)beam.ColorB);
                        }
                        else
                        {
                            beamLine.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, (short)beamColorIndex);
                        }

                        ms.AppendEntity(beamLine);
                        tr.AddNewlyCreatedDBObject(beamLine, true);
                        SetXData(beamLine, "QKS_LOAD", $"{beam.BeamName}|{beam.Section}");
                        beamCount++;

                        // 2. Vẽ Polyline tải trọng nét đậm (Global Width = 5.0mm) cho phân đoạn tải lẻ
                        // Nếu phân đoạn gán Full tải toàn bộ nhịp dầm -> KHÔNG vẽ Pline để bản vẽ thông thoáng tuyệt đối!
                        if (beam.Segments != null && beam.Segments.Count > 0)
                        {
                            foreach (var seg in beam.Segments)
                            {
                                double sDistMm = seg.StartDist;
                                double eDistMm = seg.EndDist;
                                bool hasLoad = (Math.Abs(seg.Val1) >= 0.001 || Math.Abs(seg.Val2) >= 0.001) && seg.Text != "0" && !string.IsNullOrWhiteSpace(seg.Text);

                                bool isFullBeam = (sDistMm <= 0.05 * bLen && eDistMm >= 0.95 * bLen) || ((eDistMm - sDistMm) >= 0.95 * bLen);

                                if (hasLoad && eDistMm > sDistMm && !isFullBeam)
                                {
                                    double segP1X = p1X + sDistMm * ux;
                                    double segP1Y = p1Y + sDistMm * uy;
                                    double segP2X = p1X + eDistMm * ux;
                                    double segP2Y = p1Y + eDistMm * uy;

                                    Polyline segPline = new Polyline();
                                    segPline.AddVertexAt(0, new Point2d(segP1X, segP1Y), 0, 5.0, 5.0);
                                    segPline.AddVertexAt(1, new Point2d(segP2X, segP2Y), 0, 5.0, 5.0);
                                    segPline.ConstantWidth = 5.0;
                                    segPline.Layer = segLayer;
                                    segPline.Color = GetColorByLoadValue(seg.Val1);

                                    ms.AppendEntity(segPline);
                                    tr.AddNewlyCreatedDBObject(segPline, true);
                                    SetXData(segPline, "QKS_LOAD", $"{beam.BeamName}|{beam.Section}");
                                    segPolyCount++;
                                }

                                // Vẽ Text tải trọng tại trung điểm phân đoạn
                                double tx = seg.MidX + dX;
                                double ty = seg.MidY + dY;

                                MText mText = new MText();
                                mText.SetDatabaseDefaults();
                                mText.Layer = textLayer;
                                mText.TextHeight = th;
                                mText.Attachment = AttachmentPoint.MiddleCenter;
                                mText.Contents = !string.IsNullOrEmpty(seg.Text) ? seg.Text : "0";
                                mText.Location = new Point3d(tx, ty, 0);
                                mText.Rotation = seg.AngleDeg * (Math.PI / 180.0);

                                ms.AppendEntity(mText);
                                tr.AddNewlyCreatedDBObject(mText, true);
                                SetXData(mText, "QKS_LOAD", $"{beam.BeamName}|{beam.Section}");
                                segmentCount++;
                            }
                        }
                        else
                        {
                            // Fallback nếu dầm chưa có segment nào: vẽ text 0 tại trung điểm dầm
                            double midX = (p1X + p2X) / 2.0;
                            double midY = (p1Y + p2Y) / 2.0;

                            MText mText = new MText();
                            mText.SetDatabaseDefaults();
                            mText.Layer = textLayer;
                            mText.TextHeight = th;
                            mText.Attachment = AttachmentPoint.MiddleCenter;
                            mText.Contents = "0";
                            mText.Location = new Point3d(midX, midY, 0);
                            mText.Rotation = beam.AngleDeg * (Math.PI / 180.0);

                            ms.AppendEntity(mText);
                            tr.AddNewlyCreatedDBObject(mText, true);
                            SetXData(mText, "QKS_LOAD", $"{beam.BeamName}|{beam.Section}");
                            segmentCount++;
                        }
                    }

                    // 4. Vẽ Bảng Chú Thích Tiết Diện Dầm (Section Legend) nếu được bật
                    if (payload.DrawLegend)
                    {
                        var uniqueSections = new Dictionary<string, Tuple<int, int, int>>();
                        foreach (var b in payload.Beams)
                        {
                            if (!string.IsNullOrEmpty(b.Section) && !uniqueSections.ContainsKey(b.Section))
                            {
                                uniqueSections[b.Section] = new Tuple<int, int, int>(b.ColorR, b.ColorG, b.ColorB);
                            }
                        }

                        if (uniqueSections.Count > 0)
                        {
                            double lx = payload.LegendPointX;
                            double ly = payload.LegendPointY;
                            double rowSpacing = th * 2.5;
                            double lineSampleLen = th * 7.0;
                            double boxPadding = th * 1.5;
                            double maxSecTextWidth = th * 8.0;

                            // Title
                            MText titleText = new MText();
                            titleText.SetDatabaseDefaults();
                            titleText.Layer = textLayer;
                            titleText.TextHeight = th * 1.2;
                            titleText.Contents = "BẢNG CHÚ THÍCH TIẾT DIỆN DẦM";
                            titleText.Location = new Point3d(lx, ly, 0);
                            titleText.Attachment = AttachmentPoint.TopLeft;
                            ms.AppendEntity(titleText);
                            tr.AddNewlyCreatedDBObject(titleText, true);

                            double currentY = ly - th * 2.5;
                            int secIdx = 0;
                            foreach (var kvp in uniqueSections)
                            {
                                string secName = kvp.Key;
                                int r = kvp.Value.Item1;
                                int g = kvp.Value.Item2;
                                int b = kvp.Value.Item3;

                                double sampleY = currentY - (secIdx * rowSpacing);

                                // Line mẫu màu sắc
                                Line sampleLine = new Line(new Point3d(lx, sampleY, 0), new Point3d(lx + lineSampleLen, sampleY, 0));
                                sampleLine.Layer = beamLayer;
                                if (r >= 0 && g >= 0 && b >= 0)
                                {
                                    sampleLine.Color = Autodesk.AutoCAD.Colors.Color.FromRgb((byte)r, (byte)g, (byte)b);
                                }
                                else
                                {
                                    sampleLine.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, (short)beamColorIndex);
                                }
                                ms.AppendEntity(sampleLine);
                                tr.AddNewlyCreatedDBObject(sampleLine, true);

                                // Text tên tiết diện
                                MText secText = new MText();
                                secText.SetDatabaseDefaults();
                                secText.Layer = textLayer;
                                secText.TextHeight = th;
                                secText.Contents = secName;
                                secText.Location = new Point3d(lx + lineSampleLen + (th * 0.8), sampleY, 0);
                                secText.Attachment = AttachmentPoint.MiddleLeft;
                                ms.AppendEntity(secText);
                                tr.AddNewlyCreatedDBObject(secText, true);

                                secIdx++;
                            }

                            // Khung chữ nhật bao quanh Legend
                            double boxWidth = lineSampleLen + maxSecTextWidth + (boxPadding * 2);
                            double boxHeight = (secIdx * rowSpacing) + (th * 3.5) + boxPadding;

                            Polyline legendBox = new Polyline();
                            legendBox.SetDatabaseDefaults();
                            legendBox.Layer = beamLayer;
                            legendBox.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 4); // Cyan
                            legendBox.AddVertexAt(0, new Point2d(lx - boxPadding, ly + boxPadding), 0, 0, 0);
                            legendBox.AddVertexAt(1, new Point2d(lx - boxPadding + boxWidth, ly + boxPadding), 0, 0, 0);
                            legendBox.AddVertexAt(2, new Point2d(lx - boxPadding + boxWidth, ly + boxPadding - boxHeight), 0, 0, 0);
                            legendBox.AddVertexAt(3, new Point2d(lx - boxPadding, ly + boxPadding - boxHeight), 0, 0, 0);
                            legendBox.Closed = true;
                            ms.AppendEntity(legendBox);
                            tr.AddNewlyCreatedDBObject(legendBox, true);
                        }
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n[QKS Loads] Hoàn tất vẽ {beamCount} dầm, {segmentCount} text tải trọng và {segPolyCount} đoạn Polyline tải (Width=5mm).");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi vẽ dầm tải: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH 1B: VẼ RIÊNG BẢNG CHÚ THÍCH TIẾT DIỆN DẦM (SECTION LEGEND) TRÊN CAD
        /// </summary>
        [CommandMethod("QKS_WALL_DRAW_LEGEND", CommandFlags.Modal)]
        public static void DrawSectionLegendCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. Đọc các tham số truyền vào từ dòng lệnh
                PromptResult resJson = ed.GetString("\nĐường dẫn JSON: ");
                if (resJson.Status != PromptStatus.OK) return;
                string jsonPath = resJson.StringResult.Trim('\"');

                PromptDoubleResult resX = ed.GetDouble("\nTọa độ X điểm đặt legend: ");
                if (resX.Status != PromptStatus.OK) return;
                double lx = resX.Value;

                PromptDoubleResult resY = ed.GetDouble("\nTọa độ Y điểm đặt legend: ");
                if (resY.Status != PromptStatus.OK) return;
                double ly = resY.Value;

                PromptDoubleResult resTh = ed.GetDouble("\nChiều cao chữ: ");
                if (resTh.Status != PromptStatus.OK) return;
                double textHeight = resTh.Value > 0 ? resTh.Value : 200.0;

                if (!File.Exists(jsonPath))
                {
                    ed.WriteMessage($"\n[QKS Error] Không tìm thấy file dữ liệu tại: {jsonPath}");
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var payload = _json.Deserialize<CadExportPayload>(jsonContent);
                if (payload == null || payload.Beams == null || payload.Beams.Count == 0)
                {
                    ed.WriteMessage("\n[QKS Error] Dữ liệu dầm xuất rỗng.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string beamLayer = EnsureLayerExists(tr, db, "QKS_WALL_LOAD_BEAM", 8);
                    string textLayer = EnsureLayerExists(tr, db, "QKS_WALL_LOAD_TEXT", 1);

                    var uniqueSections = new Dictionary<string, Tuple<int, int, int>>();
                    foreach (var b in payload.Beams)
                    {
                        if (!string.IsNullOrEmpty(b.Section) && !uniqueSections.ContainsKey(b.Section))
                        {
                            uniqueSections[b.Section] = new Tuple<int, int, int>(b.ColorR, b.ColorG, b.ColorB);
                        }
                    }

                    if (uniqueSections.Count > 0)
                    {
                        double th = textHeight > 0 ? textHeight : 200.0;
                        double rowSpacing = th * 2.5;
                        double lineSampleLen = th * 7.0;
                        double boxPadding = th * 1.5;
                        double maxSecTextWidth = th * 8.0;

                        // Title
                        MText titleText = new MText();
                        titleText.SetDatabaseDefaults();
                        titleText.Layer = textLayer;
                        titleText.TextHeight = th * 1.2;
                        titleText.Contents = "BẢNG CHÚ THÍCH TIẾT DIỆN DẦM";
                        titleText.Location = new Point3d(lx, ly, 0);
                        titleText.Attachment = AttachmentPoint.TopLeft;
                        ms.AppendEntity(titleText);
                        tr.AddNewlyCreatedDBObject(titleText, true);

                        double currentY = ly - th * 2.5;
                        int secIdx = 0;
                        foreach (var kvp in uniqueSections)
                        {
                            string secName = kvp.Key;
                            int r = kvp.Value.Item1;
                            int g = kvp.Value.Item2;
                            int b = kvp.Value.Item3;

                            double sampleY = currentY - (secIdx * rowSpacing);

                            // Line mẫu màu sắc
                            Line sampleLine = new Line(new Point3d(lx, sampleY, 0), new Point3d(lx + lineSampleLen, sampleY, 0));
                            sampleLine.Layer = beamLayer;
                            if (r >= 0 && g >= 0 && b >= 0)
                            {
                                sampleLine.Color = Autodesk.AutoCAD.Colors.Color.FromRgb((byte)r, (byte)g, (byte)b);
                            }
                            else
                            {
                                sampleLine.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 8);
                            }
                            ms.AppendEntity(sampleLine);
                            tr.AddNewlyCreatedDBObject(sampleLine, true);

                            // Text tên tiết diện
                            MText secText = new MText();
                            secText.SetDatabaseDefaults();
                            secText.Layer = textLayer;
                            secText.TextHeight = th;
                            secText.Contents = secName;
                            secText.Location = new Point3d(lx + lineSampleLen + (th * 0.8), sampleY, 0);
                            secText.Attachment = AttachmentPoint.MiddleLeft;
                            ms.AppendEntity(secText);
                            tr.AddNewlyCreatedDBObject(secText, true);

                            secIdx++;
                        }

                        // Khung chữ nhật bao quanh Legend
                        double boxWidth = lineSampleLen + maxSecTextWidth + (boxPadding * 2);
                        double boxHeight = (secIdx * rowSpacing) + (th * 3.5) + boxPadding;

                        Polyline legendBox = new Polyline();
                        legendBox.SetDatabaseDefaults();
                        legendBox.Layer = beamLayer;
                        legendBox.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 4); // Cyan
                        legendBox.AddVertexAt(0, new Point2d(lx - boxPadding, ly + boxPadding), 0, 0, 0);
                        legendBox.AddVertexAt(1, new Point2d(lx - boxPadding + boxWidth, ly + boxPadding), 0, 0, 0);
                        legendBox.AddVertexAt(2, new Point2d(lx - boxPadding + boxWidth, ly + boxPadding - boxHeight), 0, 0, 0);
                        legendBox.AddVertexAt(3, new Point2d(lx - boxPadding, ly + boxPadding - boxHeight), 0, 0, 0);
                        legendBox.Closed = true;
                        ms.AppendEntity(legendBox);
                        tr.AddNewlyCreatedDBObject(legendBox, true);
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n[QKS Loads] Hoàn tất vẽ bảng chú thích với {uniqueSections.Count} loại tiết diện.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi vẽ bảng chú thích: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH 1C: QUÉT CHỌN TOÀN BỘ CÁC DẦM THEO TIẾT DIỆN TRÊN AUTOCAD
        /// Cú pháp: QKS_WALL_SELECT_SECTION "<SectionName>"
        /// </summary>
        [CommandMethod("QKS_WALL_SELECT_SECTION", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void SelectSectionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                PromptResult res = ed.GetString("\nNhập tên tiết diện cần chọn: ");
                if (res.Status != PromptStatus.OK) return;
                string targetSection = res.StringResult.Trim('\"', ' ');

                if (string.IsNullOrEmpty(targetSection)) return;

                List<ObjectId> matchingIds = new List<ObjectId>();
                var result = new AutoCadWallQuery { Action = "GetSelectedBeams", Lines = new List<AutoCadWallLine>() };

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    foreach (ObjectId id in ms)
                    {
                        if (id.ObjectClass.DxfName == "LINE")
                        {
                            Line line = tr.GetObject(id, OpenMode.ForRead) as Line;
                            if (line != null && line.Layer.Equals("QKS_WALL_LOAD_BEAM", StringComparison.OrdinalIgnoreCase))
                            {
                                string xdata = GetXData(line, "QKS_LOAD");
                                if (!string.IsNullOrEmpty(xdata))
                                {
                                    string[] parts = xdata.Split('|');
                                    if (parts.Length >= 2 && parts[1].Trim().Equals(targetSection, StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchingIds.Add(id);

                                        string bName = parts[0].Trim();
                                        result.Lines.Add(new AutoCadWallLine
                                        {
                                            X1 = line.StartPoint.X,
                                            Y1 = line.StartPoint.Y,
                                            Z1 = line.StartPoint.Z,
                                            X2 = line.EndPoint.X,
                                            Y2 = line.EndPoint.Y,
                                            Z2 = line.EndPoint.Z,
                                            Thickness = "200",
                                            BeamName = bName
                                        });
                                    }
                                }
                            }
                        }
                    }
                    tr.Commit();
                }

                if (matchingIds.Count > 0)
                {
                    ed.SetImpliedSelection(matchingIds.ToArray());
                    ed.WriteMessage($"\n[QKS Loads] Đã quét chọn {matchingIds.Count} dầm mang tiết diện '{targetSection}' trên bản vẽ AutoCAD.\n");

                    try
                    {
                        string filePath = @"C:\QKS_JSON_Data\Loads\cad_wall_lines.json";
                        File.WriteAllText(filePath, _json.Serialize(result));
                    }
                    catch { }
                }
                else
                {
                    ed.WriteMessage($"\n[QKS Loads] Không tìm thấy dầm nào mang tiết diện '{targetSection}' trên layer QKS_WALL_LOAD_BEAM.\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi chọn dầm theo tiết diện: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH 1D: BỎ CHỌN TOÀN BỘ CÁC ĐỐI TƯỢNG TRÊN AUTOCAD (CLEAR SELECTION)
        /// </summary>
        [CommandMethod("QKS_WALL_CLEAR_SELECTION", CommandFlags.Modal | CommandFlags.Redraw)]
        public static void ClearSelectionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            try
            {
                ed.SetImpliedSelection(new ObjectId[0]);
                ed.WriteMessage("\n[QKS Loads] Đã hủy chọn đối tượng trên AutoCAD.\n");

                string filePath = @"C:\QKS_JSON_Data\Loads\cad_wall_lines.json";
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// LỆNH 2: PICK ĐIỂM GỐC BASEPOINT
        /// </summary>
        [CommandMethod("QKS_WALL_PICK_BASEPOINT", CommandFlags.Modal)]
        public static void PickBasePointCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptPointOptions ppo = new PromptPointOptions("\n[QKS Loads] Vui lòng pick điểm gốc BasePoint: ");
            PromptPointResult ppr = ed.GetPoint(ppo);

            if (ppr.Status == PromptStatus.OK)
            {
                double x = Math.Round(ppr.Value.X, 4);
                double y = Math.Round(ppr.Value.Y, 4);

                string filePath = @"C:\QKS_JSON_Data\Loads\base_point.json";
                var data = new { X = x, Y = y, Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                File.WriteAllText(filePath, _json.Serialize(data));

                ed.WriteMessage($"\n[QKS Loads] Đã lưu BasePoint: X = {x}, Y = {y}");
            }
        }

        /// <summary>
        /// LỆNH 3: QUÉT CHỌN LINE TƯỜNG KIẾN TRÚC TRÊN CAD
        /// </summary>
        [CommandMethod("QKS_WALL_GET_LINES", CommandFlags.Modal)]
        public static void GetWallLinesCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            var result = new AutoCadWallQuery { Action = "GetWallLines", Lines = new List<AutoCadWallLine>() };

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\n[QKS Loads] Quét chọn các nét tường/dầm trên bản vẽ CAD: ";

            TypedValue[] filter = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };

            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(pso, sf);
            if (psr.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                    if (obj is Line line)
                    {
                        // Bỏ qua nếu đối tượng thuộc các layer text, tick cũ, dot hoặc segment tải trọng
                        if (line.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_DOT", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_SEGMENT", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string bName = GetXData(line, "QKS_LOAD");
                        if (!string.IsNullOrEmpty(bName) && bName.Contains("|"))
                        {
                            bName = bName.Split('|')[0].Trim();
                        }
                        result.Lines.Add(new AutoCadWallLine
                        {
                            X1 = line.StartPoint.X,
                            Y1 = line.StartPoint.Y,
                            Z1 = line.StartPoint.Z,
                            X2 = line.EndPoint.X,
                            Y2 = line.EndPoint.Y,
                            Z2 = line.EndPoint.Z,
                            Thickness = "200",
                            BeamName = bName
                        });
                    }
                    else if (obj is Polyline poly)
                    {
                        if (poly.Layer.Equals("QKS_WALL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase) ||
                            poly.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase) ||
                            poly.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase) ||
                            poly.Layer.Equals("QKS_WALL_LOAD_SEGMENT", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string bName = GetXData(poly, "QKS_LOAD");
                        if (!string.IsNullOrEmpty(bName) && bName.Contains("|"))
                        {
                            bName = bName.Split('|')[0].Trim();
                        }
                        for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                        {
                            Point2d pt1 = poly.GetPoint2dAt(i);
                            Point2d pt2 = poly.GetPoint2dAt(i + 1);
                            result.Lines.Add(new AutoCadWallLine
                            {
                                X1 = pt1.X,
                                Y1 = pt1.Y,
                                Z1 = poly.Elevation,
                                X2 = pt2.X,
                                Y2 = pt2.Y,
                                Z2 = poly.Elevation,
                                Thickness = "200",
                                BeamName = bName
                            });
                        }
                    }
                }
                tr.Commit();
            }

            string filePath = @"C:\QKS_JSON_Data\Loads\cad_wall_lines.json";
            File.WriteAllText(filePath, _json.Serialize(result));

            ed.WriteMessage($"\n[QKS Loads] Đã lưu {result.Lines.Count} đoạn tường vào file: {filePath}");
        }

        /// <summary>
        /// LỆNH 3B: PICK 2 ĐIỂM THEO DẦM (VẼ LIÊN TỤC CHO ĐẾN KHI NHẤN ENTER / SPACE ĐỂ KẾT THÚC)
        /// </summary>
        [CommandMethod("QKS_WALL_PICK_LOAD", CommandFlags.Modal)]
        public static void PickWallLoadLineCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pickedLines = new List<AutoCadWallLine>();

            while (true)
            {
                PromptPointOptions ppo1 = new PromptPointOptions($"\n[QKS Loads] Pick điểm ĐẦU đoạn tường #{pickedLines.Count + 1} (Enter/Space để HOÀN THÀNH): ");
                ppo1.AllowNone = true; // Cho phép ấn Enter hoặc Space để kết thúc!
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                if (ppr1.Status != PromptStatus.OK)
                {
                    // Người dùng ấn Enter / Space / Escape -> Kết thúc việc pick
                    break;
                }

                PromptPointOptions ppo2 = new PromptPointOptions($"\n[QKS Loads] Pick điểm CUỐI đoạn tường #{pickedLines.Count + 1}: ");
                ppo2.UseBasePoint = true;
                ppo2.BasePoint = ppr1.Value;
                ppo2.UseDashedLine = false; // HIỂN THỊ NÉT LIỀN (SOLID LINE) THAY VÌ NÉT ĐỨT
                ppo2.AllowNone = false;

                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK)
                {
                    break;
                }

                Point3d pt1 = ppr1.Value;
                Point3d pt2 = ppr2.Value;

                if (pt1.DistanceTo(pt2) < 1.0)
                {
                    ed.WriteMessage("\n[QKS Loads] Đoạn pick quá ngắn (< 1mm), vui lòng pick lại.");
                    continue;
                }

                string matchedBeamName = "";

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Tạo layer hiển thị nét tường vừa gán tải: QKS_WALL_LOAD_DRAWN (Màu Đỏ)
                    string drawnLayer = EnsureLayerExists(tr, db, "QKS_WALL_LOAD_DRAWN", 1); 

                    // Tạo đối tượng Polyline với Global Width cố định 35mm
                    Polyline wallPoly = new Polyline();
                    wallPoly.SetDatabaseDefaults();
                    wallPoly.Layer = drawnLayer;
                    wallPoly.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // Red
                    wallPoly.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0, 35.0, 35.0);
                    wallPoly.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0, 35.0, 35.0);
                    wallPoly.ConstantWidth = 35.0;
                    wallPoly.LineWeight = LineWeight.LineWeight035;

                    ms.AppendEntity(wallPoly);
                    tr.AddNewlyCreatedDBObject(wallPoly, true);

                    // Dò tìm dầm trên layer QKS_WALL_LOAD_BEAM khớp chính xác nhất
                    TypedValue[] filter = new TypedValue[]
                    {
                        new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.LayerName, "QKS_WALL_LOAD_BEAM")
                    };
                    PromptSelectionResult psr = ed.SelectAll(new SelectionFilter(filter));
                    if (psr.Status == PromptStatus.OK)
                    {
                        double bestScore = double.MaxValue;
                        foreach (SelectedObject so in psr.Value)
                        {
                            if (so == null) continue;
                            if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is Line bLine)
                            {
                                double score = CalculateBeamMatchingScore(
                                    pt1.X, pt1.Y, pt2.X, pt2.Y,
                                    bLine.StartPoint.X, bLine.StartPoint.Y, bLine.EndPoint.X, bLine.EndPoint.Y,
                                    maxPerpDist: 500.0
                                );

                                if (score < bestScore)
                                {
                                    bestScore = score;
                                    string xd = GetXData(bLine, "QKS_LOAD");
                                    if (!string.IsNullOrEmpty(xd))
                                    {
                                        string[] parts = xd.Split('|');
                                        matchedBeamName = parts[0];
                                    }
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                pickedLines.Add(new AutoCadWallLine
                {
                    X1 = pt1.X,
                    Y1 = pt1.Y,
                    Z1 = pt1.Z,
                    X2 = pt2.X,
                    Y2 = pt2.Y,
                    Z2 = pt2.Z,
                    Thickness = "200",
                    BeamName = matchedBeamName
                });

                ed.WriteMessage($"\n[QKS Loads] -> Đoạn #{pickedLines.Count} đã vẽ (Dầm khớp: {(string.IsNullOrEmpty(matchedBeamName) ? "Tự động so sánh ETABS" : matchedBeamName)}).");
            }

            if (pickedLines.Count == 0)
            {
                ed.WriteMessage("\n[QKS Loads] Đã hoàn tất hoặc hủy thao tác vẽ dầm/tường.");
                return;
            }

            var result = new AutoCadWallQuery
            {
                Action = "PickWallLoadLine",
                Lines = pickedLines
            };

            string filePath = @"C:\QKS_JSON_Data\Loads\cad_wall_lines.json";
            File.WriteAllText(filePath, _json.Serialize(result));

            ed.WriteMessage($"\n[QKS Loads] Đã lưu tổng cộng {pickedLines.Count} đoạn tường vào file: {filePath}\n");
        }

        /// <summary>
        /// LỆNH 1E: LẤY CÁC DẦM ĐANG CHỌN HOẶC QUÉT CHỌN TRÊN CAD ĐỂ GÁN TẢI ĐỊNH MỨC (TAB 2)
        /// </summary>
        [CommandMethod("QKS_WALL_GET_SELECTED_BEAMS", CommandFlags.Modal | CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void GetSelectedBeamsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var result = new AutoCadWallQuery { Action = "GetSelectedBeams", Lines = new List<AutoCadWallLine>() };

            TypedValue[] filter = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };
            SelectionFilter sf = new SelectionFilter(filter);

            // 1. Kiểm tra đối tượng đang được chọn trước (Implied Selection từ nút 'Chọn CAD' của Tab 2)
            PromptSelectionResult psr = ed.SelectImplied();
            if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
            {
                // 2. Nếu chưa có chọn trước, yêu cầu người dùng quét chọn trên màn hình CAD
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\n[QKS Loads] Quét chọn các dầm trên bản vẽ CAD để gán tải định mức: ";
                psr = ed.GetSelection(pso, sf);
            }

            if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
            {
                ed.WriteMessage("\n[QKS Loads] Không có dầm nào được chọn.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                    if (obj is Line line)
                    {
                        if (line.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_DOT", StringComparison.OrdinalIgnoreCase) ||
                            line.Layer.Equals("QKS_WALL_LOAD_SEGMENT", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string bName = GetXData(line, "QKS_LOAD");
                        if (!string.IsNullOrEmpty(bName) && bName.Contains("|"))
                        {
                            bName = bName.Split('|')[0].Trim();
                        }

                        result.Lines.Add(new AutoCadWallLine
                        {
                            X1 = line.StartPoint.X,
                            Y1 = line.StartPoint.Y,
                            Z1 = line.StartPoint.Z,
                            X2 = line.EndPoint.X,
                            Y2 = line.EndPoint.Y,
                            Z2 = line.EndPoint.Z,
                            Thickness = "200",
                            BeamName = bName
                        });
                    }
                    else if (obj is Polyline poly)
                    {
                        if (poly.Layer.Equals("QKS_WALL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase) ||
                            poly.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase) ||
                            poly.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase) ||
                            poly.Layer.Equals("QKS_WALL_LOAD_SEGMENT", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string bName = GetXData(poly, "QKS_LOAD");
                        if (!string.IsNullOrEmpty(bName) && bName.Contains("|"))
                        {
                            bName = bName.Split('|')[0].Trim();
                        }

                        for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                        {
                            Point2d pt1 = poly.GetPoint2dAt(i);
                            Point2d pt2 = poly.GetPoint2dAt(i + 1);
                            result.Lines.Add(new AutoCadWallLine
                            {
                                X1 = pt1.X,
                                Y1 = pt1.Y,
                                Z1 = poly.Elevation,
                                X2 = pt2.X,
                                Y2 = pt2.Y,
                                Z2 = poly.Elevation,
                                Thickness = "200",
                                BeamName = bName
                            });
                        }
                    }
                }
                tr.Commit();
            }

            string filePath = @"C:\QKS_JSON_Data\Loads\cad_wall_lines.json";
            File.WriteAllText(filePath, _json.Serialize(result));

            ed.WriteMessage($"\n[QKS Loads] Đã lấy {result.Lines.Count} dầm được chọn và lưu vào file: {filePath}\n");
        }

        /// <summary>
        /// LỆNH 4: QUÉT TOÀN BỘ TEXT TẢI TRỌNG VÀ DOT ĐÃ SỬA ĐỂ IMPORT VÀO ETABS
        /// </summary>
        [CommandMethod("QKS_WALL_SCAN_LOADS", CommandFlags.Modal)]
        public static void ScanLoadsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var result = new CadImportPayload
            {
                Action = "GET_EDITED_WALL_LOADS_FROM_CAD",
                Texts = new List<CadImportedTextItem>(),
                Dots = new List<CadImportedDotItem>(),
                Ticks = new List<CadImportedTickItem>(),
                BeamLines = new List<CadImportedBeamLineItem>()
            };

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                EnsureRegApp(tr, db, "QKS_LOAD");
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                // Pass 1: Quét toàn bộ Line dầm trên layer QKS_WALL_LOAD_BEAM
                foreach (ObjectId objId in ms)
                {
                    DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                    if (obj is Line bLine && (bLine.Layer == "QKS_WALL_LOAD_BEAM" || bLine.Layer.StartsWith("QKS_")))
                    {
                        // Bỏ qua line chia tải nếu nằm trên layer TICK
                        if (bLine.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase) ||
                            bLine.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string rawXData = GetXData(bLine, "QKS_LOAD");
                        string bName = rawXData;
                        string sec = "";
                        if (rawXData.Contains("|"))
                        {
                            var parts = rawXData.Split('|');
                            bName = parts[0];
                            sec = parts.Length > 1 ? parts[1] : "";
                        }

                        if (!string.IsNullOrEmpty(bName))
                        {
                            result.BeamLines.Add(new CadImportedBeamLineItem
                            {
                                BeamName = bName,
                                Section = sec,
                                X1 = Math.Round(bLine.StartPoint.X, 3),
                                Y1 = Math.Round(bLine.StartPoint.Y, 3),
                                X2 = Math.Round(bLine.EndPoint.X, 3),
                                Y2 = Math.Round(bLine.EndPoint.Y, 3)
                            });
                        }
                    }
                }

                // Pass 2: Quét Text tải trọng, Line chia tải (Tick) và Dot
                foreach (ObjectId objId in ms)
                {
                    DBObject obj = tr.GetObject(objId, OpenMode.ForRead);

                    if (obj is DBText dbText && (dbText.Layer == "QKS_WALL_LOAD_TEXT" || dbText.Layer.StartsWith("QKS_")))
                    {
                        Point3d pos = dbText.Justify == AttachmentPoint.BaseLeft ? dbText.Position : dbText.AlignmentPoint;
                        string rawXData = GetXData(dbText, "QKS_LOAD");
                        string bName = rawXData;
                        string sec = "";
                        if (rawXData.Contains("|"))
                        {
                            var parts = rawXData.Split('|');
                            bName = parts[0];
                            sec = parts.Length > 1 ? parts[1] : "";
                        }

                        // Nếu XData rỗng, tìm dầm gần nhất trong BeamLines
                        if (string.IsNullOrEmpty(bName) && result.BeamLines.Count > 0)
                        {
                            double minD = double.MaxValue;
                            foreach (var bl in result.BeamLines)
                            {
                                double d = DistancePointToSegment(pos.X, pos.Y, bl.X1, bl.Y1, bl.X2, bl.Y2);
                                if (d < minD)
                                {
                                    minD = d;
                                    bName = bl.BeamName;
                                    sec = bl.Section;
                                }
                            }
                            if (minD > 1000.0) { bName = ""; sec = ""; }
                        }

                        result.Texts.Add(new CadImportedTextItem
                        {
                            TextValue = dbText.TextString,
                            X = Math.Round(pos.X, 3),
                            Y = Math.Round(pos.Y, 3),
                            XDataBeamName = bName,
                            Section = sec
                        });
                    }
                    else if (obj is MText mText && (mText.Layer == "QKS_WALL_LOAD_TEXT" || mText.Layer.StartsWith("QKS_")))
                    {
                        string rawXData = GetXData(mText, "QKS_LOAD");
                        string bName = rawXData;
                        string sec = "";
                        if (rawXData.Contains("|"))
                        {
                            var parts = rawXData.Split('|');
                            bName = parts[0];
                            sec = parts.Length > 1 ? parts[1] : "";
                        }

                        // Nếu XData rỗng, tìm dầm gần nhất trong BeamLines
                        if (string.IsNullOrEmpty(bName) && result.BeamLines.Count > 0)
                        {
                            double minD = double.MaxValue;
                            foreach (var bl in result.BeamLines)
                            {
                                double d = DistancePointToSegment(mText.Location.X, mText.Location.Y, bl.X1, bl.Y1, bl.X2, bl.Y2);
                                if (d < minD)
                                {
                                    minD = d;
                                    bName = bl.BeamName;
                                    sec = bl.Section;
                                }
                            }
                            if (minD > 1000.0) { bName = ""; sec = ""; }
                        }

                        result.Texts.Add(new CadImportedTextItem
                        {
                            TextValue = mText.Text,
                            X = Math.Round(mText.Location.X, 3),
                            Y = Math.Round(mText.Location.Y, 3),
                            XDataBeamName = bName,
                            Section = sec
                        });
                    }
                    else if (obj is Line tickLine && (tickLine.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase) 
                                                   || tickLine.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase)
                                                   || tickLine.Layer.Equals("QKS_WALL_LOAD_DOT", StringComparison.OrdinalIgnoreCase)))
                    {
                        double mX = (tickLine.StartPoint.X + tickLine.EndPoint.X) / 2.0;
                        double mY = (tickLine.StartPoint.Y + tickLine.EndPoint.Y) / 2.0;

                        string rawXData = GetXData(tickLine, "QKS_LOAD");
                        string bName = rawXData;
                        string sec = "";
                        if (rawXData.Contains("|"))
                        {
                            var parts = rawXData.Split('|');
                            bName = parts[0];
                            sec = parts.Length > 1 ? parts[1] : "";
                        }

                        if (string.IsNullOrEmpty(bName) && result.BeamLines.Count > 0)
                        {
                            double minD = double.MaxValue;
                            foreach (var bl in result.BeamLines)
                            {
                                double d = DistancePointToSegment(mX, mY, bl.X1, bl.Y1, bl.X2, bl.Y2);
                                if (d < minD)
                                {
                                    minD = d;
                                    bName = bl.BeamName;
                                    sec = bl.Section;
                                }
                            }
                            if (minD > 600.0) { bName = ""; sec = ""; }
                        }

                        result.Ticks.Add(new CadImportedTickItem
                        {
                            X1 = Math.Round(tickLine.StartPoint.X, 3),
                            Y1 = Math.Round(tickLine.StartPoint.Y, 3),
                            X2 = Math.Round(tickLine.EndPoint.X, 3),
                            Y2 = Math.Round(tickLine.EndPoint.Y, 3),
                            MidX = Math.Round(mX, 3),
                            MidY = Math.Round(mY, 3),
                            XDataBeamName = bName,
                            Section = sec
                        });
                    }
                    else if (obj is Polyline segPoly && (segPoly.Layer.Equals("QKS_WALL_LOAD_SEGMENT", StringComparison.OrdinalIgnoreCase) 
                                                      || segPoly.Layer.Equals("QKS_WALL_LOAD_TICK", StringComparison.OrdinalIgnoreCase)
                                                      || segPoly.Layer.Equals("QKS_WALL_LOAD_TICKS", StringComparison.OrdinalIgnoreCase)))
                    {
                        string rawXData = GetXData(segPoly, "QKS_LOAD");
                        string bName = rawXData;
                        string sec = "";
                        if (rawXData.Contains("|"))
                        {
                            var parts = rawXData.Split('|');
                            bName = parts[0];
                            sec = parts.Length > 1 ? parts[1] : "";
                        }

                        int numVerts = segPoly.NumberOfVertices;
                        if (numVerts >= 2)
                        {
                            Point2d pStart = segPoly.GetPoint2dAt(0);
                            Point2d pEnd = segPoly.GetPoint2dAt(numVerts - 1);
                            double mX = (pStart.X + pEnd.X) / 2.0;
                            double mY = (pStart.Y + pEnd.Y) / 2.0;

                            if (string.IsNullOrEmpty(bName) && result.BeamLines.Count > 0)
                            {
                                double minD = double.MaxValue;
                                foreach (var bl in result.BeamLines)
                                {
                                    double d = DistancePointToSegment(mX, mY, bl.X1, bl.Y1, bl.X2, bl.Y2);
                                    if (d < minD)
                                    {
                                        minD = d;
                                        bName = bl.BeamName;
                                        sec = bl.Section;
                                    }
                                }
                                if (minD > 600.0) { bName = ""; sec = ""; }
                            }

                            // Thêm mút đầu
                            result.Ticks.Add(new CadImportedTickItem
                            {
                                X1 = Math.Round(pStart.X, 3),
                                Y1 = Math.Round(pStart.Y, 3),
                                X2 = Math.Round(pStart.X, 3),
                                Y2 = Math.Round(pStart.Y, 3),
                                MidX = Math.Round(pStart.X, 3),
                                MidY = Math.Round(pStart.Y, 3),
                                XDataBeamName = bName,
                                Section = sec
                            });

                            // Thêm mút cuối
                            result.Ticks.Add(new CadImportedTickItem
                            {
                                X1 = Math.Round(pEnd.X, 3),
                                Y1 = Math.Round(pEnd.Y, 3),
                                X2 = Math.Round(pEnd.X, 3),
                                Y2 = Math.Round(pEnd.Y, 3),
                                MidX = Math.Round(pEnd.X, 3),
                                MidY = Math.Round(pEnd.Y, 3),
                                XDataBeamName = bName,
                                Section = sec
                            });
                        }
                    }
                    else if (obj is Circle circle && circle.Layer == "QKS_WALL_LOAD_DOT")
                    {
                        result.Dots.Add(new CadImportedDotItem
                        {
                            X = Math.Round(circle.Center.X, 3),
                            Y = Math.Round(circle.Center.Y, 3)
                        });
                    }
                }

                tr.Commit();
            }

            string filePath = @"C:\QKS_JSON_Data\Loads\cad_edited_loads.json";
            File.WriteAllText(filePath, _json.Serialize(result));

            ed.WriteMessage($"\n[QKS Loads] Đã quét {result.Texts.Count} Text tải, {result.Ticks.Count} Line chia tải và {result.BeamLines.Count} dầm.");
        }

        #region QKS SHELL LOAD COMMANDS

        /// <summary>
        /// LỆNH: VẼ MẶT BẰNG SÀN (SHELL LOAD) + RECTANGLE + TEXT TIẾT DIỆN & TẢI TRỌNG
        /// Cú pháp: QKS_SHELL_DRAW_SLABS "<jsonPath>" <pasteX> <pasteY> <textHeight> <textColor> <minArea>
        /// </summary>
        [CommandMethod("QKS_SHELL_DRAW_SLABS", CommandFlags.Modal)]
        public static void DrawShellLoadsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. Nhận tham số
                PromptResult resPath = ed.GetString("\nĐường dẫn file JSON sàn: ");
                if (resPath.Status != PromptStatus.OK) return;
                string jsonPath = resPath.StringResult.Trim('"');

                PromptDoubleResult resX = ed.GetDouble("\nTọa độ X điểm dán: ");
                if (resX.Status != PromptStatus.OK) return;
                double pasteX = resX.Value;

                PromptDoubleResult resY = ed.GetDouble("\nTọa độ Y điểm dán: ");
                if (resY.Status != PromptStatus.OK) return;
                double pasteY = resY.Value;

                PromptDoubleResult resH = ed.GetDouble("\nChiều cao chữ: ");
                if (resH.Status != PromptStatus.OK) return;
                double textHeight = resH.Value;

                PromptIntegerResult resTextColor = ed.GetInteger("\nMã màu text tải: ");
                if (resTextColor.Status != PromptStatus.OK) return;
                int textColorIndex = resTextColor.Value;

                PromptDoubleResult resMinArea = ed.GetDouble("\nDiện tích sàn tối thiểu (m2): ");
                double minAreaM2 = resMinArea.Status == PromptStatus.OK ? resMinArea.Value : 0.5;

                // 2. Đọc JSON
                if (!File.Exists(jsonPath))
                {
                    ed.WriteMessage($"\n[QKS Error] Không tìm thấy file dữ liệu sàn tại: {jsonPath}");
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var payload = _json.Deserialize<CadShellExportPayload>(jsonContent);
                if (payload == null || payload.Slabs == null || payload.Slabs.Count == 0)
                {
                    ed.WriteMessage("\n[QKS Error] Dữ liệu ô sàn rỗng.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string boundaryLayer = EnsureLayerExists(tr, db, "QKS_SHELL_LOAD_BOUNDARY", 7);
                    string hatchLayer = EnsureLayerExists(tr, db, "QKS_SHELL_LOAD_HATCH", 7);
                    string rectLayer = EnsureLayerExists(tr, db, "QKS_SHELL_LOAD_RECT", 7);
                    string textLayer = EnsureLayerExists(tr, db, "QKS_SHELL_LOAD_TEXT", textColorIndex);
                    EnsureRegApp(tr, db, "QKS_SHELL_LOAD");

                    ObjectId textStyleId = EnsureTextStyleExists(tr, db, "Arial", "arial.ttf");
                    double dX = pasteX - payload.BasePointX;
                    double dY = pasteY - payload.BasePointY;
                    double th = textHeight > 0 ? textHeight : 200.0;

                    int slabCount = 0;
                    int textCount = 0;
                    ObjectIdCollection hatchIds = new ObjectIdCollection();

                    foreach (var slab in payload.Slabs)
                    {
                        if (slab.Points == null || slab.Points.Count < 3) continue;

                        Autodesk.AutoCAD.Colors.Color slabColor;
                        if (slab.ColorR >= 0 && slab.ColorG >= 0 && slab.ColorB >= 0)
                        {
                            slabColor = Autodesk.AutoCAD.Colors.Color.FromRgb((byte)slab.ColorR, (byte)slab.ColorG, (byte)slab.ColorB);
                        }
                        else
                        {
                            slabColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7);
                        }

                        // 1. Vẽ đường bao ô sàn (Closed LWPolyline)
                        Polyline poly = new Polyline();
                        poly.Layer = boundaryLayer;
                        poly.Color = slabColor;

                        for (int i = 0; i < slab.Points.Count; i++)
                        {
                            double px = slab.Points[i][0] + dX;
                            double py = slab.Points[i][1] + dY;
                            poly.AddVertexAt(i, new Point2d(px, py), 0, 0, 0);
                        }
                        poly.Closed = true;

                        ObjectId polyId = ms.AppendEntity(poly);
                        tr.AddNewlyCreatedDBObject(poly, true);
                        SetXData(poly, "QKS_SHELL_LOAD", $"{slab.AreaName}|{slab.Section}|{slab.LoadText}");
                        slabCount++;

                        // 1.5. Tô Hatch Solid theo màu TrueColor của tiết diện sàn (nếu bật DrawHatch)
                        if (payload.DrawHatch)
                        {
                            try
                            {
                                Hatch hatch = new Hatch();
                                hatch.SetDatabaseDefaults();
                                hatch.Layer = hatchLayer;
                                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                hatch.Color = slabColor;

                                int transPercent = payload.HatchTransparency > 0 ? payload.HatchTransparency : 70;
                                byte alpha = (byte)(255 * (100 - Math.Min(90, Math.Max(0, transPercent))) / 100);
                                hatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency(alpha);

                                ObjectId hatchId = ms.AppendEntity(hatch);
                                tr.AddNewlyCreatedDBObject(hatch, true);

                                ObjectIdCollection loopIds = new ObjectIdCollection { polyId };
                                hatch.AppendLoop(HatchLoopTypes.Default, loopIds);
                                hatch.EvaluateHatch(true);

                                SetXData(hatch, "QKS_SHELL_LOAD", $"{slab.AreaName}|{slab.Section}|HATCH");
                                hatchIds.Add(hatchId);
                            }
                            catch (System.Exception)
                            {
                                // Bỏ qua nếu ô sàn có hình học phức tạp/tự cắt
                            }
                        }

                        // 2. Vẽ Khung Rectangle & Text chú thích nếu diện tích >= minAreaM2
                        if (slab.AreaM2 >= minAreaM2)
                        {
                            double cx = slab.CentroidX + dX;
                            double cy = slab.CentroidY + dY;

                            string line1 = slab.SectionText ?? "";
                            string line2 = FormatLoadUnits(slab.LoadText);
                            bool hasLoad = !string.IsNullOrEmpty(line2) && line2 != "0";

                            // Tách các dòng tải trọng (hỗ trợ phân tách theo \r\n, \n, \P hoặc ;)
                            string[] loadLines = Array.Empty<string>();
                            if (hasLoad)
                            {
                                loadLines = line2.Split(new string[] { "\r\n", "\n", "\\P", "; " }, StringSplitOptions.RemoveEmptyEntries);
                            }

                            int maxLen = line1.Length;
                            if (loadLines.Length > 0)
                            {
                                foreach (var l in loadLines)
                                {
                                    if (l.Length > maxLen) maxLen = l.Length;
                                }
                            }
                            if (maxLen == 0) maxLen = 4;

                            int totalLines = hasLoad && loadLines.Length > 0 ? (1 + loadLines.Length) : 1;
                            double boxW = (maxLen * th * 0.62) + (th * 1.0);
                            double boxH = hasLoad && loadLines.Length > 0 ? ((totalLines * th * 1.35) + (th * 0.8)) : (th * 1.8);

                            double halfW = boxW / 2.0;
                            double halfH = boxH / 2.0;

                            if (payload.DrawRectangle)
                            {
                                // Vẽ Khung chữ nhật bao quanh Text
                                Polyline box = new Polyline();
                                box.Layer = rectLayer;
                                box.Color = slabColor;
                                box.AddVertexAt(0, new Point2d(cx - halfW, cy - halfH), 0, 0, 0);
                                box.AddVertexAt(1, new Point2d(cx + halfW, cy - halfH), 0, 0, 0);
                                box.AddVertexAt(2, new Point2d(cx + halfW, cy + halfH), 0, 0, 0);
                                box.AddVertexAt(3, new Point2d(cx - halfW, cy + halfH), 0, 0, 0);
                                box.Closed = true;

                                ms.AppendEntity(box);
                                tr.AddNewlyCreatedDBObject(box, true);
                                SetXData(box, "QKS_SHELL_LOAD", $"{slab.AreaName}|{slab.Section}|BOX");
                            }

                            if (payload.DrawText)
                            {
                                // 1. Vẽ MText Tiết Diện Sàn (Section) ở nửa trên của box
                                double secY = hasLoad && loadLines.Length > 0 ? (cy + halfH - (th * 0.90)) : cy;

                                MText mTextSec = new MText();
                                mTextSec.SetDatabaseDefaults();
                                mTextSec.Layer = textLayer;
                                mTextSec.TextStyleId = textStyleId;
                                mTextSec.TextHeight = th;
                                mTextSec.Attachment = AttachmentPoint.MiddleCenter;
                                mTextSec.Location = new Point3d(cx, secY, 0);
                                mTextSec.Contents = line1;

                                ms.AppendEntity(mTextSec);
                                tr.AddNewlyCreatedDBObject(mTextSec, true);
                                SetXData(mTextSec, "QKS_SHELL_LOAD", $"{slab.AreaName}|{slab.Section}|SEC");
                                textCount++;

                                // 2. Vẽ MText Tải Trọng Sàn (Load) riêng biệt nhiều dòng (\P)
                                if (hasLoad && loadLines.Length > 0)
                                {
                                    double loadCenterY = cy - (th * 0.55);

                                    MText mTextLoad = new MText();
                                    mTextLoad.SetDatabaseDefaults();
                                    mTextLoad.Layer = textLayer;
                                    mTextLoad.TextStyleId = textStyleId;
                                    mTextLoad.TextHeight = th * 0.90;
                                    mTextLoad.Attachment = AttachmentPoint.MiddleCenter;
                                    mTextLoad.Location = new Point3d(cx, loadCenterY, 0);
                                    mTextLoad.Contents = string.Join("\\P", loadLines);

                                    ms.AppendEntity(mTextLoad);
                                    tr.AddNewlyCreatedDBObject(mTextLoad, true);
                                    SetXData(mTextLoad, "QKS_SHELL_LOAD", $"{slab.AreaName}|{slab.Section}|LOAD");
                                    textCount++;
                                }
                            }
                        }
                    }

                    // Đưa toàn bộ Hatch xuống lớp dưới cùng (Send to Back) để không che khuất Text/Khung
                    if (hatchIds.Count > 0)
                    {
                        try
                        {
                            DrawOrderTable dot = (DrawOrderTable)tr.GetObject(ms.DrawOrderTableId, OpenMode.ForWrite);
                            dot.MoveToBottom(hatchIds);
                        }
                        catch { }
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n[QKS Shell Load] Đã xuất thành công {slabCount} ô sàn (kèm {hatchIds.Count} mảng Hatch màu) và {textCount} ghi chú tải.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi vẽ mặt bằng sàn: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH: CHỌN VÀ HIGHLIGHT TOÀN BỘ Ô SÀN THEO TIẾT DIỆN
        /// Cú pháp: QKS_SHELL_SELECT_SECTION "<sectionName>"
        /// </summary>
        [CommandMethod("QKS_SHELL_SELECT_SECTION", CommandFlags.Modal)]
        public static void SelectShellSectionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptResult res = ed.GetString("\nTên tiết diện sàn cần chọn: ");
                if (res.Status != PromptStatus.OK) return;
                string secName = res.StringResult.Trim().Trim('"');

                var matchedIds = new ObjectIdCollection();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    foreach (ObjectId objId in ms)
                    {
                        DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                        string xdata = GetXData(obj, "QKS_SHELL_LOAD");
                        if (!string.IsNullOrEmpty(xdata))
                        {
                            var parts = xdata.Split('|');
                            string section = parts.Length > 1 ? parts[1] : "";
                            if (section.Equals(secName, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedIds.Add(objId);
                            }
                        }
                    }
                    tr.Commit();
                }

                if (matchedIds.Count > 0)
                {
                    ObjectId[] idsArray = new ObjectId[matchedIds.Count];
                    matchedIds.CopyTo(idsArray, 0);
                    ed.SetImpliedSelection(idsArray);
                    ed.WriteMessage($"\n[QKS Shell Load] Đã chọn {matchedIds.Count} đối tượng sàn thuộc tiết diện: {secName}");
                }
                else
                {
                    ed.WriteMessage($"\n[QKS Shell Load] Không tìm thấy ô sàn nào có tiết diện: {secName}");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi chọn tiết diện sàn: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH: PICK 2 ĐIỂM VẼ VÙNG TẢI RECTANGLE CỤC BỘ TRÊN CAD
        /// Cú pháp: QKS_SHELL_PICK_ZONE_RECT
        /// </summary>
        [CommandMethod("QKS_SHELL_PICK_ZONE_RECT", CommandFlags.Modal)]
        public static void PickZoneRectCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptPointOptions ppo1 = new PromptPointOptions("\n[QKS Shell Load] Chọn điểm góc thứ nhất của vùng tải: ");
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                if (ppr1.Status != PromptStatus.OK) return;

                PromptCornerOptions pco = new PromptCornerOptions("\n[QKS Shell Load] Chọn điểm góc thứ hai đối diện: ", ppr1.Value);
                PromptPointResult ppr2 = ed.GetCorner(pco);
                if (ppr2.Status != PromptStatus.OK) return;

                Point3d pt1 = ppr1.Value;
                Point3d pt2 = ppr2.Value;

                double minX = Math.Min(pt1.X, pt2.X);
                double maxX = Math.Max(pt1.X, pt2.X);
                double minY = Math.Min(pt1.Y, pt2.Y);
                double maxY = Math.Max(pt1.Y, pt2.Y);

                // Vẽ khung vùng tải tạm thời trên CAD
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string zoneLayer = EnsureLayerExists(tr, db, "QKS_SHELL_ZONE_LOAD", 1); // Màu Đỏ ACI 1
                    Polyline rect = new Polyline();
                    rect.Layer = zoneLayer;
                    rect.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);
                    rect.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
                    rect.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
                    rect.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
                    rect.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
                    rect.Closed = true;

                    ms.AppendEntity(rect);
                    tr.AddNewlyCreatedDBObject(rect, true);
                    tr.Commit();
                }

                var zoneData = new CadShellZoneRectPayload
                {
                    X1 = Math.Round(minX, 3),
                    Y1 = Math.Round(minY, 3),
                    X2 = Math.Round(maxX, 3),
                    Y2 = Math.Round(maxY, 3)
                };

                string outDir = @"C:\QKS_JSON_Data\Loads";
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                string outFile = Path.Combine(outDir, "cad_zone_rect.json");
                File.WriteAllText(outFile, _json.Serialize(zoneData));

                ed.WriteMessage($"\n[QKS Shell Load] Đã chọn vùng tải: ({minX:0.##}, {minY:0.##}) -> ({maxX:0.##}, {maxY:0.##})");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi pick vùng tải: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH: CẬP NHẬT TRỰC TIẾP TEXT VÀ NÉT TẢI TƯỜNG TRÊN AUTOCAD (LIVE IN-PLACE SYNC)
        /// Cú pháp: QKS_WALL_UPDATE_TEXT "<jsonPath>"
        /// </summary>
        [CommandMethod("QKS_WALL_UPDATE_TEXT", CommandFlags.Modal)]
        public static void UpdateWallTextCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptResult resPath = ed.GetString("\nĐường dẫn file JSON cập nhật text tải tường: ");
                if (resPath.Status != PromptStatus.OK) return;
                string jsonPath = resPath.StringResult.Trim('"');

                if (!File.Exists(jsonPath)) return;

                var payload = _json.Deserialize<CadWallLiveUpdatePayload>(File.ReadAllText(jsonPath));
                if (payload == null || payload.BeamLoads == null || payload.BeamLoads.Count == 0) return;

                int updatedCount = 0;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (ObjectId objId in ms)
                    {
                        DBObject obj = tr.GetObject(objId, OpenMode.ForRead);

                        if (obj is MText mText && (mText.Layer.Equals("QKS_WALL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase) || mText.Layer.StartsWith("QKS_")))
                        {
                            string rawXData = GetXData(mText, "QKS_LOAD");
                            string bName = rawXData;
                            if (rawXData.Contains("|"))
                            {
                                bName = rawXData.Split('|')[0];
                            }

                            if (!string.IsNullOrEmpty(bName) && payload.BeamLoads.TryGetValue(bName, out var beamItem))
                            {
                                mText.UpgradeOpen();
                                mText.Contents = !string.IsNullOrEmpty(beamItem.LoadText) ? beamItem.LoadText : "0";
                                mText.Color = GetColorByLoadValue(beamItem.MaxLoadValue);
                                updatedCount++;
                            }
                        }
                        else if (obj is DBText dbText && (dbText.Layer.Equals("QKS_WALL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase) || dbText.Layer.StartsWith("QKS_")))
                        {
                            string rawXData = GetXData(dbText, "QKS_LOAD");
                            string bName = rawXData;
                            if (rawXData.Contains("|"))
                            {
                                bName = rawXData.Split('|')[0];
                            }

                            if (!string.IsNullOrEmpty(bName) && payload.BeamLoads.TryGetValue(bName, out var beamItem))
                            {
                                dbText.UpgradeOpen();
                                dbText.TextString = !string.IsNullOrEmpty(beamItem.LoadText) ? beamItem.LoadText : "0";
                                dbText.Color = GetColorByLoadValue(beamItem.MaxLoadValue);
                                updatedCount++;
                            }
                        }
                        else if (obj is Polyline segPoly && segPoly.Layer.Equals("QKS_WALL_LOAD_SEGMENT", StringComparison.OrdinalIgnoreCase))
                        {
                            string rawXData = GetXData(segPoly, "QKS_LOAD");
                            string bName = rawXData;
                            if (rawXData.Contains("|"))
                            {
                                bName = rawXData.Split('|')[0];
                            }

                            if (!string.IsNullOrEmpty(bName) && payload.BeamLoads.TryGetValue(bName, out var beamItem))
                            {
                                segPoly.UpgradeOpen();
                                if (beamItem.MaxLoadValue > 0.001)
                                {
                                    segPoly.Color = GetColorByLoadValue(beamItem.MaxLoadValue);
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n[QKS Wall Load] Đã đồng bộ trực tiếp tải tường Live cho {updatedCount} dầm tầng {payload.Story}.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi cập nhật text tải tường: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH: CẬP NHẬT TEXT TẢI TRỌNG TRÊN CAD THỜI GIAN THỰC (IN-PLACE LIVE SYNC)
        /// Cú pháp: QKS_SHELL_UPDATE_TEXT "<jsonPath>"
        /// </summary>
        [CommandMethod("QKS_SHELL_UPDATE_TEXT", CommandFlags.Modal)]
        public static void UpdateShellTextCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptResult resPath = ed.GetString("\nĐường dẫn file JSON cập nhật text: ");
                if (resPath.Status != PromptStatus.OK) return;
                string jsonPath = resPath.StringResult.Trim('"');

                if (!File.Exists(jsonPath)) return;

                var payload = _json.Deserialize<CadShellLiveUpdatePayload>(File.ReadAllText(jsonPath));
                if (payload == null) return;

                int updatedCount = 0;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    bool hasSingleTarget = !string.IsNullOrEmpty(payload.TargetSection);
                    bool hasMap = payload.SectionLoadMap != null && payload.SectionLoadMap.Count > 0;
                    if (!hasSingleTarget && !hasMap) return;

                    foreach (ObjectId objId in ms)
                    {
                        DBObject obj = tr.GetObject(objId, OpenMode.ForRead);
                        if (obj is MText mText && mText.Layer.Equals("QKS_SHELL_LOAD_TEXT", StringComparison.OrdinalIgnoreCase))
                        {
                            string xdata = GetXData(mText, "QKS_SHELL_LOAD");
                            if (!string.IsNullOrEmpty(xdata))
                            {
                                var parts = xdata.Split('|');
                                string sec = parts.Length > 1 ? parts[1] : "";
                                string type = parts.Length > 2 ? parts[2] : "";

                                if (hasSingleTarget && sec.Equals(payload.TargetSection, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (type.Equals("LOAD", StringComparison.OrdinalIgnoreCase) || (!type.Equals("SEC", StringComparison.OrdinalIgnoreCase) && !mText.Contents.Equals(sec, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        mText.UpgradeOpen();
                                        mText.TextStyleId = EnsureTextStyleExists(tr, db, "Arial", "arial.ttf");
                                        string newText = (string.IsNullOrEmpty(payload.NewLoadText) || payload.NewLoadText == "0") ? "" : FormatLoadUnits(payload.NewLoadText);
                                        mText.Contents = newText;
                                        SetXData(mText, "QKS_SHELL_LOAD", $"{parts[0]}|{sec}|LOAD");
                                        updatedCount++;
                                    }
                                }
                                else if (hasMap && payload.SectionLoadMap.TryGetValue(sec, out string mappedText))
                                {
                                    if (type.Equals("LOAD", StringComparison.OrdinalIgnoreCase) || (!type.Equals("SEC", StringComparison.OrdinalIgnoreCase) && !mText.Contents.Equals(sec, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        mText.UpgradeOpen();
                                        mText.TextStyleId = EnsureTextStyleExists(tr, db, "Arial", "arial.ttf");
                                        string newText = (string.IsNullOrEmpty(mappedText) || mappedText == "0") ? "" : FormatLoadUnits(mappedText);
                                        mText.Contents = newText;
                                        SetXData(mText, "QKS_SHELL_LOAD", $"{parts[0]}|{sec}|LOAD");
                                        updatedCount++;
                                    }
                                }
                            }
                        }
                    }
                    tr.Commit();
                }
                string targetDesc = !string.IsNullOrEmpty(payload.TargetSection) ? $"tiết diện {payload.TargetSection}" : $"toàn bộ {payload.SectionLoadMap?.Count} tiết diện";
                ed.WriteMessage($"\n[QKS Shell Load] Đã cập nhật Text tải thời gian thực cho {updatedCount} ô sàn ({targetDesc}).");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi cập nhật text tải: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH: QUÉT CHỌN CÁC TEXT TẢI TRỌNG SÀN TRÊN CAD ĐỂ IMPORT VÀO ETABS
        /// Cú pháp: QKS_SHELL_SCAN_LOADS
        /// </summary>
        [CommandMethod("QKS_SHELL_SCAN_LOADS", CommandFlags.Modal)]
        public static void ScanShellLoadsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. Quét chọn các đối tượng Text/MText tải sàn trên AutoCAD
                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                selOpts.MessageForAdding = "\n[QKS Shell Load] Quét chọn các Text tải trọng trên AutoCAD (hoặc Enter để chọn tất cả): ";
                selOpts.AllowDuplicates = false;

                TypedValue[] filter = new TypedValue[] {
                    new TypedValue((int)DxfCode.Start, "MTEXT,TEXT")
                };
                SelectionFilter selFilter = new SelectionFilter(filter);

                PromptSelectionResult selRes = ed.GetSelection(selOpts, selFilter);
                SelectionSet ss = null;

                if (selRes.Status == PromptStatus.OK)
                {
                    ss = selRes.Value;
                }
                else
                {
                    // Nếu ấn Enter, chọn tất cả MText trên model space
                    PromptSelectionResult allRes = ed.SelectAll(selFilter);
                    if (allRes.Status == PromptStatus.OK)
                    {
                        ss = allRes.Value;
                    }
                }

                if (ss == null || ss.Count == 0)
                {
                    ed.WriteMessage("\n[QKS Shell Load] Không có Text tải nào được chọn.");
                    return;
                }

                var resultList = new List<CadShellImportItem>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in ss)
                    {
                        if (so == null) continue;
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);
                        if (obj is MText mText)
                        {
                            string xdata = GetXData(mText, "QKS_SHELL_LOAD");
                            if (!string.IsNullOrEmpty(xdata))
                            {
                                var parts = xdata.Split('|');
                                string areaName = parts.Length > 0 ? parts[0] : "";
                                string section = parts.Length > 1 ? parts[1] : "";
                                string tag = parts.Length > 2 ? parts[2] : "";

                                if (!tag.Equals("SEC", StringComparison.OrdinalIgnoreCase))
                                {
                                    resultList.Add(new CadShellImportItem
                                    {
                                        AreaName = areaName,
                                        Section = section,
                                        RawText = mText.Contents,
                                        X = Math.Round(mText.Location.X, 3),
                                        Y = Math.Round(mText.Location.Y, 3)
                                    });
                                }
                            }
                        }
                    }
                    tr.Commit();
                }

                string outDir = @"C:\QKS_JSON_Data\Loads";
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                string filePath = Path.Combine(outDir, "cad_shell_import.json");
                File.WriteAllText(filePath, _json.Serialize(resultList));

                ed.WriteMessage($"\n[QKS Shell Load] Đã quét thành công {resultList.Count} Text tải trọng sàn từ AutoCAD.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi quét Text tải sàn: {ex.Message}");
            }
        }

        /// <summary>
        /// LỆNH: HỦY CHỌN TRÊN AUTOCAD
        /// Cú pháp: QKS_SHELL_CLEAR_SELECTION
        /// </summary>
        [CommandMethod("QKS_SHELL_CLEAR_SELECTION", CommandFlags.Modal)]
        public static void ClearShellSelectionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            try
            {
                ed.SetImpliedSelection(new ObjectId[0]);
            }
            catch { }
        }

        /// <summary>
        /// LỆNH: VẼ BẢNG THỐNG KÊ TIẾT DIỆN SÀN VÀ CHÚ GIẢI MÀU TRUECOLOR LÊN AUTOCAD
        /// Cú pháp: QKS_SHELL_DRAW_TABLE "<jsonPath>"
        /// </summary>
        [CommandMethod("QKS_SHELL_DRAW_TABLE", CommandFlags.Modal)]
        public static void DrawShellTableCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptResult resPath = ed.GetString("\nĐường dẫn file JSON bảng thống kê sàn: ");
                if (resPath.Status != PromptStatus.OK) return;
                string jsonPath = resPath.StringResult.Trim('"');

                PromptPointResult ptRes = ed.GetPoint("\nChọn điểm chèn góc trên bên trái của Bảng Thống Kê Sàn: ");
                if (ptRes.Status != PromptStatus.OK) return;
                Point3d insPt = ptRes.Value;

                if (!File.Exists(jsonPath))
                {
                    ed.WriteMessage($"\n[QKS Error] Không tìm thấy file: {jsonPath}");
                    return;
                }

                var payload = _json.Deserialize<CadSlabTableExportPayload>(File.ReadAllText(jsonPath));
                if (payload == null || payload.Items == null || payload.Items.Count == 0)
                {
                    ed.WriteMessage("\n[QKS Error] Dữ liệu bảng thống kê rỗng.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string tableLayer = EnsureLayerExists(tr, db, "QKS_SHELL_TABLE", 7);
                    ObjectId textStyleId = EnsureTextStyleExists(tr, db, "Arial", "arial.ttf");

                    double th = payload.TextHeight > 0 ? payload.TextHeight : 250.0;
                    int numItems = payload.Items.Count;
                    int numRows = numItems + 3; // 1 Title + 1 Header + N Items + 1 Summary
                    int numCols = 6;

                    // Sử dụng đối tượng Table chuẩn của AutoCAD
                    Table table = new Table();
                    table.SetDatabaseDefaults();
                    table.Layer = tableLayer;
                    table.Position = insPt;

                    table.SetSize(numRows, numCols);
                    table.SetRowHeight(th * 1.9);

                    // Thiết lập khoảng căn lề (Margins: Trên, Dưới, Trái, Phải) cho toàn bộ ô trong bảng
                    try
                    {
#pragma warning disable CS0618
                        for (int r = 0; r < numRows; r++)
                        {
                            for (int c = 0; c < numCols; c++)
                            {
                                table.SetMargin(r, c, CellMargins.Top, th * 0.35);
                                table.SetMargin(r, c, CellMargins.Bottom, th * 0.35);
                                table.SetMargin(r, c, CellMargins.Left, th * 0.50);
                                table.SetMargin(r, c, CellMargins.Right, th * 0.50);
                            }
                        }
#pragma warning restore CS0618
                    }
                    catch { }

                    // Thiết lập chiều cao dòng hợp lý, thông thoáng, thanh thoát
                    table.Rows[0].Height = th * 2.2;
                    table.Rows[1].Height = th * 1.8;
                    table.Rows[numRows - 1].Height = th * 1.8;

                    // Thiết lập chiều rộng từng cột rộng rãi, không bao giờ bị đè chữ hay dính line
                    table.Columns[0].Width = th * 4.5;  // Cột 0: STT
                    table.Columns[1].Width = th * 6.0;  // Cột 1: MÀU SẮC
                    table.Columns[2].Width = th * 11.0; // Cột 2: TIẾT DIỆN
                    table.Columns[3].Width = th * 7.5;  // Cột 3: SỐ LƯỢNG
                    table.Columns[4].Width = th * 11.0; // Cột 4: DIỆN TÍCH (m²) (Căn giữa)
                    table.Columns[5].Width = th * 28.0; // Cột 5: THIẾT KẾ TẢI

                    // 1. DÒNG TIÊU ĐỀ BẢNG (Row 0)
                    table.Cells[0, 0].TextStyleId = textStyleId;
                    table.Cells[0, 0].TextHeight = th * 1.15;
                    table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[0, 0].TextString = $"BẢNG THỐNG KÊ TIẾT DIỆN SÀN - TẦNG {payload.StoryName.ToUpper()}";

                    // 2. DÒNG HEADER (Row 1)
                    string[] headers = new string[] { "STT", "MÀU SẮC", "TIẾT DIỆN", "SỐ LƯỢNG", "DIỆN TÍCH (m²)", "THIẾT KẾ TẢI" };
                    for (int c = 0; c < headers.Length; c++)
                    {
                        table.Cells[1, c].TextStyleId = textStyleId;
                        table.Cells[1, c].TextHeight = th * 0.90;
                        table.Cells[1, c].Alignment = CellAlignment.MiddleCenter;
                        table.Cells[1, c].TextString = headers[c];
                    }

                    // 3. CÁC DÒNG DỮ LIỆU TIẾT DIỆN SÀN
                    int totalSlabs = 0;
                    double totalArea = 0.0;

                    for (int i = 0; i < numItems; i++)
                    {
                        int r = i + 2;
                        var item = payload.Items[i];
                        totalSlabs += item.Count;
                        totalArea += item.TotalAreaM2;

                        Autodesk.AutoCAD.Colors.Color secColor;
                        if (item.ColorR >= 0 && item.ColorG >= 0 && item.ColorB >= 0)
                            secColor = Autodesk.AutoCAD.Colors.Color.FromRgb((byte)item.ColorR, (byte)item.ColorG, (byte)item.ColorB);
                        else
                            secColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7);

                        for (int c = 0; c < numCols; c++)
                        {
                            table.Cells[r, c].TextStyleId = textStyleId;
                            table.Cells[r, c].TextHeight = th * 0.85;
                        }

                        // Col 0: STT
                        table.Cells[r, 0].Alignment = CellAlignment.MiddleCenter;
                        table.Cells[r, 0].TextString = (i + 1).ToString();

                        // Col 1: MÀU SẮC (Tô màu TrueColor trực tiếp vào nền ô)
                        table.Cells[r, 1].Alignment = CellAlignment.MiddleCenter;
                        table.Cells[r, 1].BackgroundColor = secColor;
                        table.Cells[r, 1].TextString = "";

                        // Col 2: TIẾT DIỆN
                        table.Cells[r, 2].Alignment = CellAlignment.MiddleCenter;
                        table.Cells[r, 2].TextString = item.SectionName;

                        // Col 3: SỐ LƯỢNG
                        table.Cells[r, 3].Alignment = CellAlignment.MiddleCenter;
                        table.Cells[r, 3].TextString = item.Count.ToString();

                        // Col 4: DIỆN TÍCH (m²) - Căn giữa theo yêu cầu
                        table.Cells[r, 4].Alignment = CellAlignment.MiddleCenter;
                        table.Cells[r, 4].TextString = item.TotalAreaM2.ToString("N2");

                        // Col 5: THIẾT KẾ TẢI
                        table.Cells[r, 5].Alignment = CellAlignment.MiddleLeft;
                        table.Cells[r, 5].TextString = FormatLoadUnits(item.LoadSummary);
                    }

                    // 4. DÒNG TỔNG CỘNG
                    int sumRow = numRows - 1;
                    for (int c = 0; c < numCols; c++)
                    {
                        table.Cells[sumRow, c].TextStyleId = textStyleId;
                        table.Cells[sumRow, c].TextHeight = th * 0.90;
                    }

                    table.Cells[sumRow, 2].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[sumRow, 2].TextString = "TỔNG CỘNG";

                    table.Cells[sumRow, 3].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[sumRow, 3].TextString = totalSlabs.ToString();

                    // Cột 4 Tổng diện tích - Căn giữa
                    table.Cells[sumRow, 4].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[sumRow, 4].TextString = totalArea.ToString("N2");

                    table.Cells[sumRow, 5].Alignment = CellAlignment.MiddleLeft;
                    table.Cells[sumRow, 5].TextString = "";

                    table.GenerateLayout();
                    ms.AppendEntity(table);
                    tr.AddNewlyCreatedDBObject(table, true);

                    tr.Commit();
                    ed.WriteMessage($"\n[QKS Shell Load] Đã vẽ thành công Bảng Thống Kê Sàn ({payload.Items.Count} loại tiết diện, {totalSlabs} ô sàn, tổng {totalArea:N2} m²).");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[QKS Error] Lỗi vẽ bảng thống kê sàn: {ex.Message}");
            }
        }

        private static ObjectId EnsureTextStyleExists(Transaction tr, Database db, string styleName = "Arial", string fontFile = "arial.ttf")
        {
            TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (!tst.Has(styleName))
            {
                tst.UpgradeOpen();
                TextStyleTableRecord tstr = new TextStyleTableRecord
                {
                    Name = styleName,
                    FileName = fontFile,
                    TextSize = 0.0
                };
                ObjectId id = tst.Add(tstr);
                tr.AddNewlyCreatedDBObject(tstr, true);
                return id;
            }
            else
            {
                ObjectId textStyleId = tst[styleName];
                TextStyleTableRecord tstr = (TextStyleTableRecord)tr.GetObject(textStyleId, OpenMode.ForWrite);
                tstr.FileName = fontFile;
                tstr.TextSize = 0.0;
                return textStyleId;
            }
        }

        private static string FormatLoadUnits(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("kN/m2", "kN/m²").Replace("m2", "m²");
        }

        private static Polyline CreateTableRectPoly(double x1, double y1, double x2, double y2, string layer, int colorIndex)
        {
            Polyline p = new Polyline();
            p.Layer = layer;
            p.ColorIndex = colorIndex;
            p.AddVertexAt(0, new Point2d(x1, y1), 0, 0, 0);
            p.AddVertexAt(1, new Point2d(x2, y1), 0, 0, 0);
            p.AddVertexAt(2, new Point2d(x2, y2), 0, 0, 0);
            p.AddVertexAt(3, new Point2d(x1, y2), 0, 0, 0);
            p.Closed = true;
            return p;
        }

        private static MText CreateTableCellText(double x, double y, string text, double height, string layer, AttachmentPoint attach)
        {
            MText m = new MText();
            m.SetDatabaseDefaults();
            m.Layer = layer;
            m.TextHeight = height;
            m.Attachment = attach;
            m.Location = new Point3d(x, y, 0);
            m.Contents = text;
            return m;
        }

        #endregion

        /// <summary>
        /// Thuật toán đánh giá mức độ khớp hình học giữa đoạn pick và dầm ứng viên:
        /// 1. Kiểm tra góc phương vị (phải cùng phương, loại bỏ dầm vuông góc/xiên khác hướng).
        /// 2. Kiểm tra khoảng cách vuông góc từ đoạn pick tới đường thẳng chứa trục dầm.
        /// 3. Kiểm tra độ dài chồng lấn (overlap) dọc theo trục dầm (loại bỏ trường hợp chỉ chạm tại nút).
        /// </summary>
        public static double CalculateBeamMatchingScore(
            double px1, double py1, double px2, double py2,
            double bx1, double by1, double bx2, double by2,
            double maxPerpDist = 500.0)
        {
            double pux = px2 - px1;
            double puy = py2 - py1;
            double pLen = Math.Sqrt(pux * pux + puy * puy);

            double bvx = bx2 - bx1;
            double bvy = by2 - by1;
            double bLen = Math.Sqrt(bvx * bvx + bvy * bvy);

            if (pLen < 0.001 || bLen < 0.001) return double.MaxValue;

            // 1. Kiểm tra góc hợp giữa 2 vector (Cosine)
            double dot = pux * bvx + puy * bvy;
            double cosTheta = Math.Abs(dot) / (pLen * bLen);
            if (cosTheta > 1.0) cosTheta = 1.0;

            // BẮT BUỘC: cos(theta) >= 0.85 (góc lệch tối đa ~30 độ). Bỏ qua dầm vuông góc/xiên khác phương
            if (cosTheta < 0.85) return double.MaxValue;

            // 2. Khoảng cách vuông góc từ 2 điểm pick tới đường thẳng chứa dầm
            double A = bvy;
            double B = -bvx;
            double C = bx2 * by1 - by2 * bx1;

            double distPerp1 = Math.Abs(A * px1 + B * py1 + C) / bLen;
            double distPerp2 = Math.Abs(A * px2 + B * py2 + C) / bLen;
            double maxDistPerp = Math.Max(distPerp1, distPerp2);

            if (maxDistPerp > maxPerpDist) return double.MaxValue;

            // 3. Chiếu 2 điểm pick lên trục dầm để tính độ dài chồng lấn (overlap)
            double t1 = ((px1 - bx1) * bvx + (py1 - by1) * bvy) / bLen;
            double t2 = ((px2 - bx1) * bvx + (py2 - by1) * bvy) / bLen;

            double s = Math.Min(t1, t2);
            double e = Math.Max(t1, t2);

            double overlapStart = Math.Max(0.0, s);
            double overlapEnd = Math.Min(bLen, e);
            double overlapLen = Math.Max(0.0, overlapEnd - overlapStart);

            double minLen = Math.Min(pLen, bLen);
            double overlapRatio = minLen > 0.001 ? overlapLen / minLen : 0.0;

            // BẮT BUỘC: Có độ dài chồng lấn dọc trục dầm >= 15% (loại bỏ trường hợp chỉ chạm tại nút giao)
            if (overlapRatio < 0.15) return double.MaxValue;

            // Điểm phạt càng nhỏ thì mức độ khớp càng cao và chính xác
            double score = maxDistPerp + (1.0 - cosTheta) * 500.0 + (1.0 - overlapRatio) * 300.0;
            return score;
        }

        private static double DistancePointToSegment(double px, double py, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            if (dx == 0 && dy == 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            double t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
            if (t < 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            if (t > 1) return Math.Sqrt((px - x2) * (px - x2) + (py - y2) * (py - y2));
            double projX = x1 + t * dx;
            double projY = y1 + t * dy;
            return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
        }

        private static double DistanceSegmentToSegment(double ax1, double ay1, double ax2, double ay2, double bx1, double by1, double bx2, double by2)
        {
            double d1 = DistancePointToSegment(ax1, ay1, bx1, by1, bx2, by2);
            double d2 = DistancePointToSegment(ax2, ay2, bx1, by1, bx2, by2);
            double d3 = DistancePointToSegment(bx1, by1, ax1, ay1, ax2, ay2);
            double d4 = DistancePointToSegment(bx2, by2, ax1, ay1, ax2, ay2);
            return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
        }
        #endregion
    }
}
