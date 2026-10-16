using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Qks.Shared;

namespace QKS_Loads_AcadPlugin
{
    internal static class CadRuntime
    {
        internal static CadRequest Request;
        internal static Queue<string> Arguments;
        internal static bool Completed;
        internal static string Error, Output;
        internal static bool RequiresScope => Request != null && (Request.Action.Contains("SCAN") || Request.Action.Contains("UPDATE") || Request.Action.Contains("SELECT_SECTION"));
        internal sealed class StringInput { public PromptStatus Status; public string StringResult; }
        internal sealed class NumberInput { public PromptStatus Status; public double Value; }
        internal sealed class IntegerInput { public PromptStatus Status; public int Value; }
        internal static StringInput String(Editor ed, string prompt)
        {
            if (Request != null) return new StringInput { Status = PromptStatus.OK, StringResult = Arguments.Dequeue() };
            var result = ed.GetString(new PromptStringOptions(prompt) { AllowSpaces = true });
            return new StringInput { Status = result.Status, StringResult = result.StringResult };
        }
        internal static NumberInput Number(Editor ed, string prompt)
        {
            if (Request != null) {
                double value = double.Parse(Arguments.Dequeue(), CultureInfo.InvariantCulture);
                if (!LoadTextParser.Finite(value)) throw new InvalidDataException("Non-finite argument.");
                return new NumberInput { Status = PromptStatus.OK, Value = value };
            }
            var result = ed.GetDouble(prompt); return new NumberInput { Status = result.Status, Value = result.Value };
        }
        internal static IntegerInput Integer(Editor ed, string prompt)
        {
            if (Request != null) return new IntegerInput { Status = PromptStatus.OK, Value = int.Parse(Arguments.Dequeue(), CultureInfo.InvariantCulture) };
            var result = ed.GetInteger(prompt); return new IntegerInput { Status = result.Status, Value = result.Value };
        }
        internal static void Cancel() { if (Request != null) throw new OperationCanceledException("AutoCAD command canceled."); }
        internal static void Message(Editor ed, string text)
        {
            if (Request != null && (text.Contains("[QKS Error]") || text.Contains("Lỗi"))) Error = text;
            ed.WriteMessage(text);
        }
        internal static void WriteResult(string path, string json)
        {
            if (Request != null) { Output = json; Completed = true; }
            else File.WriteAllText(path, json);
        }
        internal static void Tag(DBObject obj, Transaction tr, Database db)
        {
            if (Request == null || !(obj is Entity)) return;
            var table = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!table.Has("QKS_SCOPE")) {
                table.UpgradeOpen(); var record = new RegAppTableRecord { Name = "QKS_SCOPE" };
                table.Add(record); tr.AddNewlyCreatedDBObject(record, true);
            }
            using (var data = new ResultBuffer(new TypedValue(1001, "QKS_SCOPE"), new TypedValue(1000, Request.ExportId))) obj.XData = data;
        }
        internal static bool InScope(DBObject obj)
        {
            if (!RequiresScope) return true;
            using (var data = obj.GetXDataForApplication("QKS_SCOPE")) {
                if (data == null) return false;
                foreach (var value in data) if (value.TypeCode == 1000 && string.Equals(value.Value as string, Request.ExportId, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
    public static partial class QksLoadsCommands
    {
        [CommandMethod("QKS_EXECUTE", CommandFlags.Modal)]
        public static void ExecuteRequest()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var input = doc.Editor.GetString(new PromptStringOptions("\nQKS request: ") { AllowSpaces = true });
            if (input.Status != PromptStatus.OK) return;
            CadRequest request = null;
            try
            {
                string path = input.StringResult.Trim('"');
                if (!Protocol.IsInside(path, Protocol.Root)) throw new InvalidDataException("Request outside exchange directory.");
                request = JsonCodec.Deserialize<CadRequest>(JsonCodec.Read(path)); Protocol.Validate(request);
                if (File.Exists(request.ResponsePath)) { doc.Editor.WriteMessage("\n[QKS] Yêu cầu này đã có kết quả; không thực thi lại."); return; }
                using (var receipt = new FileStream(path + ".started", FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }

                if (!Protocol.SamePath(doc.Name, request.DocumentPath)) throw new InvalidOperationException("The target drawing changed. Reconnect API.");
                var actions = new Dictionary<string, Action> {
                    { "QKS_WALL_DRAW_BEAMS", DrawWallLoadsCommand }, { "QKS_WALL_DRAW_LEGEND", DrawSectionLegendCommand },
                    { "QKS_WALL_SELECT_SECTION", SelectSectionCommand }, { "QKS_WALL_CLEAR_SELECTION", ClearSelectionCommand },
                    { "QKS_WALL_GET_LINES", GetWallLinesCommand }, { "QKS_WALL_PICK_LOAD", PickWallLoadLineCommand },
                    { "QKS_WALL_GET_SELECTED_BEAMS", GetSelectedBeamsCommand }, { "QKS_WALL_SCAN_LOADS", ScanLoadsCommand },
                    { "QKS_WALL_UPDATE_TEXT", UpdateWallTextCommand }, { "QKS_SHELL_DRAW_SLABS", DrawShellLoadsCommand },
                    { "QKS_SHELL_SELECT_SECTION", SelectShellSectionCommand }, { "QKS_SHELL_CLEAR_SELECTION", ClearShellSelectionCommand },
                    { "QKS_SHELL_UPDATE_TEXT", UpdateShellTextCommand }, { "QKS_SHELL_SCAN_LOADS", ScanShellLoadsCommand },
                    { "QKS_SHELL_DRAW_TABLE", DrawShellTableCommand }
                };
                if (doc.Database.Insunits != UnitsValue.Millimeters)
                    throw new InvalidOperationException("Bản vẽ phải dùng millimeter (INSUNITS=4). Kiểm tra đơn vị thực tế trước khi chuyển đổi, không chỉ đổi nhãn đơn vị.");
                if (!doc.Editor.CurrentUserCoordinateSystem.IsEqualTo(Matrix3d.Identity))
                    throw new InvalidOperationException("Chuyển UCS về World trước khi trao đổi tải với ETABS.");
                Action action;
                if (!actions.TryGetValue(request.Action, out action)) throw new InvalidDataException("Unsupported QKS command.");
                CadRuntime.Request = request; CadRuntime.Arguments = new Queue<string>(request.Arguments);
                CadRuntime.Completed = false; CadRuntime.Error = null; CadRuntime.Output = null;
                if (CadRuntime.RequiresScope) ValidateExport(doc);
                action();
                if (CadRuntime.Arguments.Count != 0) throw new InvalidDataException("Unexpected extra arguments.");
                if (request.Action.Contains("CLEAR_SELECTION")) CadRuntime.Completed = true;
                if (!CadRuntime.Completed || CadRuntime.Error != null) throw new InvalidOperationException(CadRuntime.Error ?? "Command produced no result; nothing was applied.");
                Reply(request, doc.Name, "succeeded", null, CadRuntime.Output);
            }
            catch (System.Exception ex)
            {
                if (request != null && Protocol.IsInside(request.ResponsePath, Protocol.Root))
                    Reply(request, doc.Name, ex is OperationCanceledException ? "canceled" : "failed", ex.Message, null);
                doc.Editor.WriteMessage("\n[QKS] " + ex.Message);
            }
            finally { CadRuntime.Request = null; CadRuntime.Arguments = null; }
        }
        private static void ValidateExport(Document document)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            using (var tr = document.Database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)tr.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var model = (BlockTableRecord)tr.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in model)
                {
                    var obj = tr.GetObject(id, OpenMode.ForRead);
                    if (!(obj is MText) && !(obj is DBText)) continue;
                    if (!CadRuntime.InScope(obj)) continue;
                    string data = GetXData(obj, "QKS_LOAD") + "|" + GetXData(obj, "QKS_SEGMENT") + "|" + GetXData(obj, "QKS_SHELL_LOAD");
                    if (!keys.Add(data)) throw new InvalidOperationException("Phát hiện text trùng định danh (có thể do COPY mặt bằng). Hãy xuất lại một bộ riêng trước khi đồng bộ.");
                }
            }
        }
        private static void Reply(CadRequest request, string document, string status, string error, string payload)
        {
            JsonCodec.WriteAtomic(request.ResponsePath, JsonCodec.Serialize(new CadResponse {
                RequestId = request.RequestId, ExportId = request.ExportId, DocumentPath = document,
                Status = status, Error = error, PayloadJson = payload }));
        }
    }
}
