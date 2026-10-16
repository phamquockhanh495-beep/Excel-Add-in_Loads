using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Reflection;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Helper tiện ích giao tiếp tốc độ cao và đáng tin cậy với AutoCAD (Thread-Safe Hybrid Mode).
    /// </summary>
    internal static class AutoCadHelper
    {
        public static readonly string DllPath = @"C:\QKS_JSON_Data\Loads\QKS_Loads_AcadPlugin.dll";
        public static readonly string LoadsDir = @"C:\QKS_JSON_Data\Loads";

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

        private static readonly string[] AcadProgIds = new string[]
        {
            "AutoCAD.Application",
            "AutoCAD.Application.25",
            "AutoCAD.Application.24.3",
            "AutoCAD.Application.24.2",
            "AutoCAD.Application.24.1",
            "AutoCAD.Application.24",
            "AutoCAD.Application.23.1",
            "AutoCAD.Application.23",
            "AutoCAD.Application.22",
            "AutoCAD.Application.21",
            "AutoCAD.Application.20"
        };

        /// <summary>
        /// Lấy đối tượng COM của một ProgID đang chạy trong Running Object Table (ROT).
        /// </summary>
        internal static object? GetActiveCOMObject(string progId)
        {
            try
            {
                Guid clsid;
                CLSIDFromProgID(progId, out clsid);
                GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
                return obj;
            }
            catch
            {
                try
                {
                    return Marshal.GetActiveObject(progId);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Lấy đối tượng AutoCAD.Application đang hoạt động trên hệ thống qua nhiều tầng truy vấn.
        /// </summary>
        internal static dynamic? GetActiveAutoCadApp()
        {
            // TẦNG 1: Duyệt qua danh sách ProgID phổ biến (AutoCAD 2016 -> 2026)
            foreach (var progId in AcadProgIds)
            {
                try
                {
                    object? obj = GetActiveCOMObject(progId);
                    if (obj != null)
                    {
                        return obj;
                    }
                }
                catch { }
            }

            // TẦNG 2: Quét Running Object Table (ROT) để tìm document hoặc application
            try
            {
                IRunningObjectTable? rot = null;
                IEnumMoniker? monikerEnumerator = null;
                if (GetRunningObjectTable(0, out rot) == 0 && rot != null)
                {
                    rot.EnumRunning(out monikerEnumerator);
                    if (monikerEnumerator != null)
                    {
                        monikerEnumerator.Reset();
                        IMoniker[] monikers = new IMoniker[1];
                        IntPtr fetched = IntPtr.Zero;

                        while (monikerEnumerator.Next(1, monikers, fetched) == 0)
                        {
                            IBindCtx? ctx = null;
                            if (CreateBindCtx(0, out ctx) == 0 && ctx != null)
                            {
                                string displayName = "";
                                monikers[0].GetDisplayName(ctx, null, out displayName);

                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    // A. Trường hợp file DWG / DXF đang mở
                                    if (displayName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) ||
                                        displayName.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
                                    {
                                        object? comObj = null;
                                        rot.GetObject(monikers[0], out comObj);
                                        if (comObj != null)
                                        {
                                            try
                                            {
                                                dynamic doc = comObj;
                                                dynamic app = doc.Application;
                                                if (app != null) return app;
                                            }
                                            catch { }
                                        }
                                    }
                                    // B. Trường hợp moniker của Application
                                    else if (displayName.IndexOf("AutoCAD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             displayName.IndexOf("acad", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        object? comObj = null;
                                        rot.GetObject(monikers[0], out comObj);
                                        if (comObj != null)
                                        {
                                            try
                                            {
                                                dynamic app = comObj;
                                                if (app.Documents != null) return app;
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi ROT enumeration AutoCAD Application: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Quét toàn bộ danh sách các bản vẽ AutoCAD đang mở trên hệ thống qua ROT và COM.
        /// </summary>
        internal static List<AcadDocInfo> GetRunningAutoCadDocuments()
        {
            var list = new List<AcadDocInfo>();
            var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // CÁCH 1: Duyệt qua AutoCAD.Application.Documents
            try
            {
                dynamic? acadApp = GetActiveAutoCadApp();
                if (acadApp != null)
                {
                    dynamic docs = acadApp.Documents;
                    foreach (dynamic doc in docs)
                    {
                        try
                        {
                            string docName = doc.Name;
                            string docPath = doc.FullName;

                            if (!string.IsNullOrEmpty(docName) && !addedNames.Contains(docName))
                            {
                                addedNames.Add(docName);
                                list.Add(new AcadDocInfo
                                {
                                    FileName = docName,
                                    FullPath = docPath,
                                    DocObj = doc
                                });
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi duyệt documents qua Application: {ex.Message}");
            }

            // CÁCH 2: Quét Running Object Table (ROT) bổ sung
            try
            {
                IRunningObjectTable? rot = null;
                IEnumMoniker? monikerEnumerator = null;
                if (GetRunningObjectTable(0, out rot) == 0 && rot != null)
                {
                    rot.EnumRunning(out monikerEnumerator);
                    if (monikerEnumerator != null)
                    {
                        monikerEnumerator.Reset();
                        IMoniker[] monikers = new IMoniker[1];
                        IntPtr fetched = IntPtr.Zero;

                        while (monikerEnumerator.Next(1, monikers, fetched) == 0)
                        {
                            IBindCtx? ctx = null;
                            if (CreateBindCtx(0, out ctx) == 0 && ctx != null)
                            {
                                string displayName = "";
                                monikers[0].GetDisplayName(ctx, null, out displayName);

                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    if (displayName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) ||
                                        displayName.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            object? comObj = null;
                                            rot.GetObject(monikers[0], out comObj);
                                            if (comObj != null)
                                            {
                                                dynamic doc = comObj;
                                                string docName = doc.Name;
                                                string docPath = doc.FullName;

                                                if (!string.IsNullOrEmpty(docName) && !addedNames.Contains(docName))
                                                {
                                                    addedNames.Add(docName);
                                                    list.Add(new AcadDocInfo
                                                    {
                                                        FileName = docName,
                                                        FullPath = docPath,
                                                        DocObj = doc
                                                    });
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi quét ROT cho AutoCAD documents: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lấy tài liệu AutoCAD (AcadDocument) đang được chọn hoặc đang active.
        /// Tự động xử lý an toàn đa luồng (Thread-safe STA Apartments) và tránh lỗi con trỏ COM stale.
        /// </summary>
        internal static dynamic? GetActiveAutoCadDoc()
        {
            try
            {
                string targetFileName = AppConnectionManager.ActiveAutoCad?.FileName ?? "";
                string targetFullPath = AppConnectionManager.ActiveAutoCad?.FullPath ?? "";

                // 1. Quét danh sách các tài liệu AutoCAD đang mở
                var docs = GetRunningAutoCadDocuments();
                if (docs.Count > 0)
                {
                    // Nếu có chỉ định file cụ thể trong AppConnectionManager
                    if (!string.IsNullOrEmpty(targetFileName) || !string.IsNullOrEmpty(targetFullPath))
                    {
                        foreach (var docInfo in docs)
                        {
                            if (!string.IsNullOrEmpty(targetFileName) && docInfo.FileName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                if (docInfo.DocObj != null) return docInfo.DocObj;
                            }
                            if (!string.IsNullOrEmpty(targetFullPath) && docInfo.FullPath.Equals(targetFullPath, StringComparison.OrdinalIgnoreCase))
                            {
                                if (docInfo.DocObj != null) return docInfo.DocObj;
                            }
                        }
                    }

                    // Nếu không chỉ định hoặc không khớp: Lấy document đầu tiên
                    if (docs[0].DocObj != null)
                    {
                        return docs[0].DocObj;
                    }
                }

                // 2. Fallback: Lấy ActiveDocument của AutoCAD Application
                dynamic? acadApp = GetActiveAutoCadApp();
                if (acadApp != null)
                {
                    dynamic? activeDoc = acadApp.ActiveDocument;
                    if (activeDoc != null)
                    {
                        return activeDoc;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi tổng thể GetActiveAutoCadDoc: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gọi hàm GetPoint an toàn từ AutoCAD Utility COM Object
        /// </summary>
        internal static double[]? AcadGetPoint(dynamic doc, string prompt, double[]? basePt = null)
        {
            if (doc == null) return null;

            try
            {
                dynamic utility = doc.Utility;
                object? pointObj = null;

                if (basePt != null && basePt.Length >= 2)
                {
                    double[] p3 = new double[] { basePt[0], basePt[1], basePt.Length > 2 ? basePt[2] : 0.0 };
                    try
                    {
                        object[] args = new object[] { p3, prompt };
                        pointObj = utility.GetType().InvokeMember(
                            "GetPoint",
                            BindingFlags.InvokeMethod,
                            null,
                            utility,
                            args
                        );
                    }
                    catch
                    {
                        pointObj = utility.GetPoint(p3, prompt);
                    }
                }
                else
                {
                    try
                    {
                        object[] args = new object[] { Type.Missing, prompt };
                        pointObj = utility.GetType().InvokeMember(
                            "GetPoint",
                            BindingFlags.InvokeMethod,
                            null,
                            utility,
                            args
                        );
                    }
                    catch
                    {
                        try
                        {
                            object[] args2 = new object[] { Missing.Value, prompt };
                            pointObj = utility.GetType().InvokeMember(
                                "GetPoint",
                                BindingFlags.InvokeMethod,
                                null,
                                utility,
                                args2
                            );
                        }
                        catch
                        {
                            pointObj = utility.GetPoint(Type.Missing, prompt);
                        }
                    }
                }

                if (pointObj is double[] dArr && dArr.Length >= 2)
                {
                    return dArr;
                }
                else if (pointObj is Array arr && arr.Length >= 2)
                {
                    return new double[] { Convert.ToDouble(arr.GetValue(0)), Convert.ToDouble(arr.GetValue(1)) };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"User canceled or error picking point: {ex.Message}");
            }
            return null;
        }

        internal static void DrawVisualFeedbackLine(dynamic doc, double x1, double y1, double x2, double y2)
        {
            if (doc == null) return;
            try
            {
                dynamic modelSpace = doc.ModelSpace;
                double[] p1 = new double[] { x1, y1, 0 };
                double[] p2 = new double[] { x2, y2, 0 };
                dynamic line = modelSpace.AddLine(p1, p2);
                line.Color = 1; // 1 = Red
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi vẽ visual feedback line: {ex.Message}");
            }
        }

        internal static (double[]? p1, double[]? p2) AcadGetRectangle(dynamic doc, string prompt1 = "\n[QKS Shell Load] Chọn điểm góc thứ nhất của vùng tải: ", string prompt2 = "\n[QKS Shell Load] Chọn điểm góc thứ hai đối diện: ")
        {
            if (doc == null) return (null, null);

            try
            {
                dynamic utility = doc.Utility;
                double[]? pt1 = AcadGetPoint(doc, prompt1);
                if (pt1 == null || pt1.Length < 2) return (null, null);

                double[] base3 = new double[] { pt1[0], pt1[1], 0.0 };
                object? pt2Obj = null;
                try
                {
                    object[] args = new object[] { base3, prompt2 };
                    pt2Obj = utility.GetType().InvokeMember(
                        "GetCorner",
                        BindingFlags.InvokeMethod,
                        null,
                        utility,
                        args
                    );
                }
                catch
                {
                    try
                    {
                        pt2Obj = utility.GetCorner(base3, prompt2);
                    }
                    catch
                    {
                        pt2Obj = AcadGetPoint(doc, prompt2, pt1);
                    }
                }

                double[]? pt2 = null;
                if (pt2Obj is double[] dArr && dArr.Length >= 2)
                {
                    pt2 = dArr;
                }
                else if (pt2Obj is Array arr && arr.Length >= 2)
                {
                    pt2 = new double[] { Convert.ToDouble(arr.GetValue(0)), Convert.ToDouble(arr.GetValue(1)) };
                }

                return (pt1, pt2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"User canceled or error picking rectangle: {ex.Message}");
            }
            return (null, null);
        }

        internal static void DrawZoneRectangle(dynamic doc, double minX, double minY, double maxX, double maxY)
        {
            if (doc == null) return;
            try
            {
                dynamic modelSpace = doc.ModelSpace;
                double[] points = new double[] {
                    minX, minY,
                    maxX, minY,
                    maxX, maxY,
                    minX, maxY
                };
                dynamic pline = modelSpace.AddLightWeightPolyline(points);
                pline.Closed = true;
                pline.Color = 1; // 1 = Red
                try { pline.Layer = "QKS_SHELL_ZONE_LOAD"; } catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi DrawZoneRectangle: {ex.Message}");
            }
        }

        internal static void SendCommand(dynamic doc, string cmd)
        {
            if (doc == null) return;
            try
            {
                doc.SendCommand(cmd);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi SendCommand: {ex.Message}");
            }
        }
    }
}
