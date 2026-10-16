using System;
using Qks.Shared;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CSiAPIv1;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Service kết nối và thao tác với mô hình ETABS thông qua CSiAPIv1.
    /// Áp dụng thuật toán kết nối đa tầng (ROT + Helper PID + Fallback) từ QKS Beam.
    /// </summary>
    public partial class EtabsService
    {
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        private static readonly object _etabsLock = new object();
        public static object EtabsLock => _etabsLock;
        public static void ResetSessionCaches()
        {
            lock (_etabsLock) { ClearAllStoryBeamsCache(); _storySlabsDictCache.Clear(); _sectionAssignedLoadSets.Clear(); }
        }

        public cSapModel? CurrentSapModel => AppConnectionManager.GetSapModel();
        public bool IsConnected => AppConnectionManager.IsEtabsConnected;

        /// <summary>
        /// Quét toàn bộ các instance ETABS đang mở trên hệ thống Windows.
        /// Sử dụng thuật toán đa tầng: ROT Enumeration -> Helper.GetObjectProcess -> Helper.GetObject.
        /// </summary>
        public static List<EtabsInstanceInfo> GetRunningEtabsInstances()
        {
            var instances = new List<EtabsInstanceInfo>();

            var etabsProcesses = Process.GetProcessesByName("ETABS");
            if (etabsProcesses.Length == 0) return instances;

            bool hasActive = false;
            foreach (var p in etabsProcesses)
            {
                try
                {
                    if (!p.HasExited && p.Responding)
                    {
                        hasActive = true;
                        break;
                    }
                }
                catch { }
            }

            if (!hasActive) return instances;

            // =========================================================================
            // TẦNG 1: Duyệt bảng ROT (Running Object Table) chuẩn mực như QKS Beam
            // =========================================================================
            IRunningObjectTable? rot = null;
            IEnumMoniker? monikerEnumerator = null;

            try
            {
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

                                if (displayName.StartsWith("!CSI.ETABS.API.ETABSObject:", StringComparison.OrdinalIgnoreCase))
                                {
                                    string[] parts = displayName.Split(':');
                                    if (parts.Length > 1 && int.TryParse(parts[1], out int procId))
                                    {
                                        bool procRunning = false;
                                        try
                                        {
                                            var p = Process.GetProcessById(procId);
                                            if (p != null && !p.HasExited && p.Responding)
                                            {
                                                procRunning = true;
                                            }
                                        }
                                        catch { }

                                        if (!procRunning) continue;

                                        try
                                        {
                                            object? comObj = null;
                                            rot.GetObject(monikers[0], out comObj);
                                            if (comObj is cOAPI etabsObj)
                                            {
                                                cSapModel sapModel = etabsObj.SapModel;

                                                string modelPath = "";
                                                try
                                                {
                                                    modelPath = sapModel.GetModelFilename(true);
                                                }
                                                catch { }

                                                string modelName = "Untitled";
                                                if (!string.IsNullOrEmpty(modelPath))
                                                {
                                                    modelName = Path.GetFileNameWithoutExtension(modelPath);
                                                }

                                                string versionStr = "";
                                                double verNumber = 0;
                                                try
                                                {
                                                    sapModel.GetVersion(ref versionStr, ref verNumber);
                                                }
                                                catch { }

                                                string display = $"{procId}_ETABS Ultimate {versionStr} - {modelName}";

                                                instances.Add(new EtabsInstanceInfo
                                                {
                                                    ProcessId = procId,
                                                    DisplayName = display,
                                                    ModelPath = modelPath,
                                                    ModelFileName = modelName,
                                                    Version = versionStr,
                                                    EtabsObject = etabsObj,
                                                    SapModel = sapModel
                                                });
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Lỗi trích xuất ROT ETABS {procId}: {ex.Message}");
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
                Debug.WriteLine($"Lỗi quét ROT: {ex.Message}");
            }

            // =========================================================================
            // TẦNG 2: Nếu ROT không thấy, Fallback dùng CSiAPIv1.Helper kết nối theo PID
            // =========================================================================
            if (instances.Count == 0 && etabsProcesses.Length > 0)
            {
                try
                {
                    cHelper myHelper = new Helper();

                    foreach (var p in etabsProcesses)
                    {
                        try
                        {
                            if (!p.HasExited && p.Responding)
                            {
                                cOAPI? etabsObj = myHelper.GetObjectProcess("CSI.ETABS.API.ETABSObject", p.Id);
                                if (etabsObj != null)
                                {
                                    cSapModel sapModel = etabsObj.SapModel;
                                    string modelPath = "";
                                    try { modelPath = sapModel.GetModelFilename(true); } catch { }
                                    string modelName = !string.IsNullOrEmpty(modelPath) ? Path.GetFileNameWithoutExtension(modelPath) : "Untitled";
                                    string versionStr = "";
                                    double verNumber = 0;
                                    try { sapModel.GetVersion(ref versionStr, ref verNumber); } catch { }

                                    string display = $"{p.Id}_ETABS Ultimate {versionStr} - {modelName}";
                                    instances.Add(new EtabsInstanceInfo
                                    {
                                        ProcessId = p.Id,
                                        DisplayName = display,
                                        ModelPath = modelPath,
                                        ModelFileName = modelName,
                                        Version = versionStr,
                                        EtabsObject = etabsObj,
                                        SapModel = sapModel
                                    });
                                }
                            }
                        }
                        catch { }
                    }

                    // TẦNG 3: Thử GetObject chung nếu theo PID vẫn chưa ra
                    if (instances.Count == 0)
                    {
                        try
                        {
                            cOAPI? etabsObj = myHelper.GetObject("CSI.ETABS.API.ETABSObject");
                            if (etabsObj != null)
                            {
                                cSapModel sapModel = etabsObj.SapModel;
                                string modelPath = "";
                                try { modelPath = sapModel.GetModelFilename(true); } catch { }
                                string modelName = !string.IsNullOrEmpty(modelPath) ? Path.GetFileNameWithoutExtension(modelPath) : "Untitled";
                                string versionStr = "";
                                double verNumber = 0;
                                try { sapModel.GetVersion(ref versionStr, ref verNumber); } catch { }

                                int pid = etabsProcesses.Length > 0 ? etabsProcesses[0].Id : 0;
                                string display = $"{pid}_ETABS Ultimate {versionStr} - {modelName}";
                                instances.Add(new EtabsInstanceInfo
                                {
                                    ProcessId = pid,
                                    DisplayName = display,
                                    ModelPath = modelPath,
                                    ModelFileName = modelName,
                                    Version = versionStr,
                                    EtabsObject = etabsObj,
                                    SapModel = sapModel
                                });
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi CSiAPIv1 Helper fallback: {ex.Message}");
                }
            }

            return instances;
        }

        /// <summary>
        /// Khởi chạy ETABS và mở tệp tin mô hình .edb (tương tự Get_API.xaml.vb dòng 404-407)
        /// </summary>
        public static EtabsInstanceInfo? LaunchAndOpenModel(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                cHelper myHelper = new Helper();
                cOAPI etabsObj = myHelper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
                etabsObj.ApplicationStart();
                cSapModel sapModel = etabsObj.SapModel;
                sapModel.File.OpenFile(filePath);

                string modelName = Path.GetFileNameWithoutExtension(filePath);
                string versionStr = "";
                double verNumber = 0;
                try { sapModel.GetVersion(ref versionStr, ref verNumber); } catch { }

                return new EtabsInstanceInfo
                {
                    ProcessId = 0,
                    DisplayName = $"ETABS Ultimate {versionStr} - {modelName}",
                    ModelPath = filePath,
                    ModelFileName = modelName,
                    Version = versionStr,
                    EtabsObject = etabsObj,
                    SapModel = sapModel
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khởi chạy ETABS: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy tên file mô hình đang kết nối
        /// </summary>
        public string GetModelFilename()
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return string.Empty;

            try
            {
                return sapModel.GetModelFilename(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy tên file mô hình: {ex.Message}");
            }
            return AppConnectionManager.ActiveEtabs?.ModelFileName ?? string.Empty;
        }

        /// <summary>
        /// Chuẩn hóa đơn vị đo lường trong ETABS về kN_m_C
        /// </summary>
        public bool EnsureStandardUnits()
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return false;

            try
            {
                int ret = sapModel.SetPresentUnits(eUnits.kN_m_C);
                return ret == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi set đơn vị kN_m_C: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem mô hình ETABS có đang bị khóa (Locked) không
        /// </summary>
        public bool IsModelLocked()
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return true;

            try
            {
                return sapModel.GetModelIsLocked();
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Làm mới khung nhìn cửa sổ ETABS và xóa vùng chọn
        /// </summary>
        public void RefreshView()
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return;

            try
            {
                sapModel.View.RefreshWindow();
                sapModel.SelectObj.ClearSelection();
            }
            catch { }
        }

        /// <summary>
        /// Lấy danh sách tầng từ ETABS
        /// </summary>
        public int GetStoryList(out string[] names, out double[] heights, out double[] elevations)
        {
            names = Array.Empty<string>();
            heights = Array.Empty<double>();
            elevations = Array.Empty<double>();

            var sapModel = CurrentSapModel;
            if (sapModel == null) return -1;

            try
            {
                lock (_etabsLock)
                {
                    if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

                    int numberNames = 0;
                    string[] myNames = Array.Empty<string>();

                    int ret = sapModel.Story.GetNameList(ref numberNames, ref myNames);
                    if (ret == 0 && myNames != null && myNames.Length > 0)
                    {
                        names = myNames;
                        int count = names.Length;
                        heights = new double[count];
                        elevations = new double[count];

                        for (int i = 0; i < count; i++)
                        {
                            double h = 0;
                            sapModel.Story.GetHeight(names[i], ref h);
                            heights[i] = h;

                            double el = 0;
                            sapModel.Story.GetElevation(names[i], ref el);
                            elevations[i] = el;
                        }
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc tầng từ ETABS: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Lấy danh sách các trường hợp tải (Load Patterns) từ mô hình ETABS
        /// </summary>
        public int GetLoadPatternList(out string[] patternNames)
        {
            patternNames = Array.Empty<string>();
            var sapModel = CurrentSapModel;
            if (sapModel == null) return -1;

            try
            {
                lock (_etabsLock)
                {
                    int numberNames = 0;
                    string[] myNames = Array.Empty<string>();
                    int ret = sapModel.LoadPatterns.GetNameList(ref numberNames, ref myNames);
                    if (ret == 0 && myNames != null && myNames.Length > 0)
                    {
                        patternNames = myNames;
                        return patternNames.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc Load Pattern từ ETABS: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ dầm ngang và tọa độ từ mô hình ETABS
        /// </summary>
        private static string _cachedStoryName = "";
        private static List<EtabsBeam>? _cachedStoryBeams = null;

        public static void InvalidateStoryBeamsCache()
        {
            _cachedStoryName = "";
            _cachedStoryBeams = null;
        }

        /// <summary>
        /// Lấy thông tin 1 dầm duy nhất theo tên với tốc độ tức thời (< 1 millisecond, chỉ 4 COM calls)
        /// </summary>
        public EtabsBeam? GetBeamByName(string beamName)
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null || string.IsNullOrWhiteSpace(beamName)) return null;

            try
            {
                string pt1 = "";
                string pt2 = "";
                int retPoints = sapModel.FrameObj.GetPoints(beamName, ref pt1, ref pt2);
                if (retPoints == 0)
                {
                    double x1 = 0, y1 = 0, z1 = 0;
                    double x2 = 0, y2 = 0, z2 = 0;

                    int retCoord1 = sapModel.PointObj.GetCoordCartesian(pt1, ref x1, ref y1, ref z1);
                    int retCoord2 = sapModel.PointObj.GetCoordCartesian(pt2, ref x2, ref y2, ref z2);

                    if (retCoord1 == 0 && retCoord2 == 0)
                    {
                        string propName = "";
                        string sAuto = "";
                        sapModel.FrameObj.GetSection(beamName, ref propName, ref sAuto);

                        return new EtabsBeam
                        {
                            Name = beamName,
                            Point1 = pt1,
                            Point2 = pt2,
                            X1 = x1,
                            Y1 = y1,
                            Z1 = z1,
                            X2 = x2,
                            Y2 = y2,
                            Z2 = z2,
                            Section = propName
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy dầm {beamName}: {ex.Message}");
            }
            return null;
        }

        public List<EtabsBeam> GetCachedBeamsForStory(string storyName)
        {
            if (_cachedStoryBeams != null && _cachedStoryName.Equals(storyName, StringComparison.OrdinalIgnoreCase))
            {
                return _cachedStoryBeams;
            }
            _cachedStoryName = storyName;
            _cachedStoryBeams = GetBeamsForStory(storyName);
            return _cachedStoryBeams;
        }

        public List<EtabsBeam> GetBeamsList()
        {
            var list = new List<EtabsBeam>();
            var sapModel = CurrentSapModel;
            if (sapModel == null) return list;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

            try
            {
                int numberNames = 0;
                string[] frameNames = Array.Empty<string>();

                int retNames = sapModel.FrameObj.GetNameList(ref numberNames, ref frameNames);
                if (retNames == 0 && frameNames != null)
                {
                    foreach (var name in frameNames)
                    {
                        string pt1 = "";
                        string pt2 = "";
                        int retPoints = sapModel.FrameObj.GetPoints(name, ref pt1, ref pt2);

                        if (retPoints == 0)
                        {
                            double x1 = 0, y1 = 0, z1 = 0;
                            double x2 = 0, y2 = 0, z2 = 0;

                            int retCoord1 = sapModel.PointObj.GetCoordCartesian(pt1, ref x1, ref y1, ref z1);
                            int retCoord2 = sapModel.PointObj.GetCoordCartesian(pt2, ref x2, ref y2, ref z2);

                            if (retCoord1 == 0 && retCoord2 == 0)
                            {
                                // Lọc các thanh dầm ngang (Z1 xấp xỉ Z2 trong sai số 1mm)
                                if (Math.Abs(z1 - z2) < 0.001)
                                {
                                    string propName = "";
                                    string sAuto = "";
                                    sapModel.FrameObj.GetSection(name, ref propName, ref sAuto);

                                    list.Add(new EtabsBeam
                                    {
                                        Name = name,
                                        Point1 = pt1,
                                        Point2 = pt2,
                                        X1 = x1,
                                        Y1 = y1,
                                        Z1 = z1,
                                        X2 = x2,
                                        Y2 = y2,
                                        Z2 = z2,
                                        Section = propName
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy danh sách dầm ETABS: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Đảm bảo Load Pattern tồn tại trong ETABS trước khi gán tải (tự động tạo mới nếu chưa có)
        /// </summary>
        public bool EnsureLoadPatternExists(string loadPat)
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null || string.IsNullOrWhiteSpace(loadPat)) return false;

            try
            {
                string[] patNames;
                int count = GetLoadPatternList(out patNames);
                for (int i = 0; i < count; i++)
                {
                    if (patNames[i].Equals(loadPat.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false; // Pattern type must be configured explicitly in ETABS.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi kiểm tra/tạo load pattern {loadPat}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gán tải phân bố đều trên toàn bộ chiều dài dầm trong ETABS
        /// </summary>
        public bool AssignDistributedLoad(string frameName, string loadPat, int direction, double value, bool replace = true)
        {
            using (BeginWrite())
            {
            if (!LoadTextParser.Finite(value)) throw new ArgumentException("Giá trị hoặc miền tải không hợp lệ.");
            var sapModel = CurrentSapModel;
            if (sapModel == null) return false;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");
            if (!EnsureLoadPatternExists(loadPat)) throw new InvalidOperationException("Load pattern chưa tồn tại; hãy tạo đúng loại tải trong ETABS.");

            try
            {
                // CSiAPIv1.cFrameObj.SetLoadDistributed
                // (string Name, string LoadPat, int MyType, int Dir, double Dist1, double Dist2, double Val1, double Val2, string CSys, bool RelDist, bool Replace, eItemType ItemType)
                int ret = sapModel.FrameObj.SetLoadDistributed(frameName, loadPat, 1, direction, 0.0, 1.0, value, value, LoadDirections.CoordinateSystem(direction), true, replace);

                if (ret == 0) VerifyFrame(frameName, loadPat, direction, 0, 1, value, value, true);
                return ret == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gán tải dầm {frameName}: {ex.Message}");
                return false;
            }

            }
        }

        /// <summary>
        /// Gán tải phân bố trên một phân đoạn xác định (dist1 đến dist2 tính bằng mét) của dầm trong ETABS
        /// </summary>
        public bool AssignSubSegmentDistributedLoad(string frameName, string loadPat, int direction, double dist1, double dist2, double value, bool replace = true)
        {
            using (BeginWrite())
            {
            if (!LoadTextParser.Finite(value) || !LoadTextParser.Finite(dist1) || !LoadTextParser.Finite(dist2) || dist1 < 0 || dist2 <= dist1) throw new ArgumentException("Giá trị hoặc miền tải không hợp lệ.");
            var sapModel = CurrentSapModel;
            if (sapModel == null) return false;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");
            if (!EnsureLoadPatternExists(loadPat)) throw new InvalidOperationException("Load pattern chưa tồn tại; hãy tạo đúng loại tải trong ETABS.");

            try
            {
                double s = Math.Min(dist1, dist2);
                double e = Math.Max(dist1, dist2);

                if (Math.Abs(e - s) < 0.01)
                {
                    return false; // Invalid/zero-length segment, never widen to the whole beam.
                }

                // Dist1 = s, Dist2 = e (mét), RelDist = false, Replace = replace
                int ret = sapModel.FrameObj.SetLoadDistributed(frameName, loadPat, 1, direction, s, e, value, value, LoadDirections.CoordinateSystem(direction), false, replace);

                if (ret == 0) VerifyFrame(frameName, loadPat, direction, s, e, value, value, false);
                return ret == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gán tải phân đoạn dầm {frameName}: {ex.Message}");
                return false;
            }

            }
        }

        /// <summary>
        /// Lấy chiều cao (depth) của tiết diện dầm chữ nhật trong ETABS
        /// Trả về 0.0 nếu không có dầm hoặc không đọc được tiết diện (không áp đặt fallback 0.4m cứng)
        /// </summary>
        public double GetBeamDepth(string beamName)
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null || string.IsNullOrWhiteSpace(beamName)) return 0.0;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

            try
            {
                string secName = "";
                string sAuto = "";
                sapModel.FrameObj.GetSection(beamName, ref secName, ref sAuto);

                if (!string.IsNullOrEmpty(secName))
                {
                    string fileName = "";
                    string matProp = "";
                    double t3 = 0; // Height (Depth)
                    double t2 = 0; // Width
                    int color = 0;
                    string notes = "";
                    string guid = "";

                    int ret = sapModel.PropFrame.GetRectangle(secName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid);
                    if (ret == 0 && t3 > 0)
                    {
                        return t3;
                    }

                    // Fallback thông minh: Parse kích thước từ tên tiết diện (ví dụ: B20x40, D20x45, 200x400)
                    var match = System.Text.RegularExpressions.Regex.Match(secName, @"(\d+)[xX*](\d+)");
                    if (match.Success)
                    {
                        if (double.TryParse(match.Groups[2].Value, out double val2))
                        {
                            if (val2 >= 100) return val2 / 1000.0; // ví dụ 400 -> 0.4m, 450 -> 0.45m
                            if (val2 >= 10) return val2 / 100.0;   // ví dụ 40 -> 0.4m, 45 -> 0.45m
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc chiều cao tiết diện dầm {beamName}: {ex.Message}");
            }

            return 0.0;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ tầng được sắp xếp theo cao độ Z tăng dần (từ dưới lên trên)
        /// </summary>
        public List<EtabsStoryItem> GetSortedStories()
        {
            var result = new List<EtabsStoryItem>();
            string[] names;
            double[] heights;
            double[] elevations;
            int count = GetStoryList(out names, out heights, out elevations);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add(new EtabsStoryItem
                    {
                        Name = names[i],
                        Height = heights[i],
                        Elevation = elevations[i]
                    });
                }
                result = result.OrderBy(s => s.Elevation).ToList();
            }
            return result;
        }

        /// <summary>
        /// Xác định tầng ngay phía trên tầng hiện tại (Z_upper > Z_current) và khoảng cách tầng giữa 2 sàn
        /// Trả về false nếu đang ở tầng cao nhất (không có tầng trên) hoặc lỗi.
        /// </summary>
        public bool GetUpperStoryInfo(string currentStoryName, out string upperStoryName, out double storyHeight)
        {
            upperStoryName = string.Empty;
            storyHeight = 3.4;

            var sorted = GetSortedStories();
            if (sorted.Count == 0)
            {
                storyHeight = GetStoryHeight(currentStoryName);
                return false;
            }

            int curIdx = sorted.FindIndex(s => s.Name.Equals(currentStoryName, StringComparison.OrdinalIgnoreCase));
            if (curIdx < 0)
            {
                storyHeight = GetStoryHeight(currentStoryName);
                return false;
            }

            // Nếu đang ở tầng cao nhất trong danh sách (không còn story nào có cao độ Z > Z_current)
            if (curIdx >= sorted.Count - 1)
            {
                // Dầm ở tầng cao nhất (không còn tầng trên): Quy ước H = 1.50m (chuẩn hóa tải tường thu hồi / lan can mái / parapet)
                storyHeight = 1.50;
                return false;
            }

            var nextStory = sorted[curIdx + 1];
            upperStoryName = nextStory.Name;
            double deltaZ = nextStory.Elevation - sorted[curIdx].Elevation;
            storyHeight = deltaZ > 0.1 ? deltaZ : (nextStory.Height > 0.1 ? nextStory.Height : 3.4);
            return true;
        }

        /// <summary>
        /// Tìm phần tử dầm tầng trên U(X1, Y1, Z1) trực tiếp thẳng hàng và đè lên trục dầm tầng dưới B(X, Y, Z)
        /// Trả về null nếu tầng trên không có dầm (chỉ có sàn Slab, khoảng thông tầng, hoặc dầm biên mái).
        /// </summary>
        public EtabsBeam? FindUpperBeam(EtabsBeam currentBeam, string upperStoryName, double maxOffsetTol = 0.35)
        {
            if (string.IsNullOrWhiteSpace(upperStoryName)) return null;

            var upperBeams = GetBeamsForStory(upperStoryName);
            if (upperBeams == null || upperBeams.Count == 0) return null;

            double curDx = currentBeam.X2 - currentBeam.X1;
            double curDy = currentBeam.Y2 - currentBeam.Y1;
            double curLen = Math.Sqrt(curDx * curDx + curDy * curDy);
            if (curLen < 0.01) return null;

            double curUx = curDx / curLen;
            double curUy = curDy / curLen;

            EtabsBeam? bestMatch = null;
            double bestScore = double.MaxValue;

            foreach (var uBeam in upperBeams)
            {
                double uDx = uBeam.X2 - uBeam.X1;
                double uDy = uBeam.Y2 - uBeam.Y1;
                double uLen = Math.Sqrt(uDx * uDx + uDy * uDy);
                if (uLen < 0.01) continue;

                double uUx = uDx / uLen;
                double uUy = uDy / uLen;

                // 1. Kiểm tra góc định hướng: 2 dầm phải song song hoặc cùng phương (dung sai góc ~18 độ)
                double dot = curUx * uUx + curUy * uUy;
                if (Math.Abs(dot) < 0.95) continue;

                // 2. Kiểm tra khoảng cách vuông góc trên mặt bằng XY (lệch trục tim dầm)
                double midUx = (uBeam.X1 + uBeam.X2) / 2.0;
                double midUy = (uBeam.Y1 + uBeam.Y2) / 2.0;
                double wx = midUx - currentBeam.X1;
                double wy = midUy - currentBeam.Y1;
                double perpDist = Math.Abs(wx * curUy - wy * curUx);
                if (perpDist > maxOffsetTol) continue;

                // 3. Kiểm tra độ chồng lấn dọc trục (longitudinal overlap)
                double t1 = (uBeam.X1 - currentBeam.X1) * curUx + (uBeam.Y1 - currentBeam.Y1) * curUy;
                double t2 = (uBeam.X2 - currentBeam.X1) * curUx + (uBeam.Y2 - currentBeam.Y1) * curUy;
                double uMin = Math.Min(t1, t2);
                double uMax = Math.Max(t1, t2);

                double overlap = Math.Max(0.0, Math.Min(curLen, uMax) - Math.Max(0.0, uMin));
                if (overlap >= 0.2 * Math.Min(curLen, uLen) || overlap >= 0.5)
                {
                    // Điểm đánh giá: ưu tiên khoảng cách tim nhỏ nhất và độ chồng lấn lớn nhất
                    double score = perpDist - (overlap / Math.Max(curLen, 1.0));
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMatch = uBeam;
                    }
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Lấy chiều cao tầng chỉ định từ ETABS
        /// </summary>
        public double GetStoryHeight(string storyName)
        {
            string[] names;
            double[] heights;
            double[] elevations;
            int count = GetStoryList(out names, out heights, out elevations);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (names[i].Equals(storyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return heights[i];
                    }
                }
            }
            return 3.4; // Default 3.4m
        }

        /// <summary>
        /// Lấy nhãn đơn vị tải trọng lực/chiều dài hiện hành từ ETABS (ví dụ: kN/m, T/m, kgf/m)
        /// </summary>
        public string GetUnitLabel()
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return "kN/m";

            try
            {
                eUnits units = sapModel.GetPresentUnits();
                switch (units)
                {
                    case eUnits.Ton_m_C:
                    case eUnits.Ton_cm_C:
                    case eUnits.Ton_mm_C:
                        return "T/m";
                    case eUnits.kN_m_C:
                    case eUnits.kN_cm_C:
                    case eUnits.kN_mm_C:
                        return "kN/m";
                    case eUnits.kgf_m_C:
                    case eUnits.kgf_cm_C:
                    case eUnits.kgf_mm_C:
                        return "kgf/m";
                    case eUnits.N_m_C:
                    case eUnits.N_cm_C:
                    case eUnits.N_mm_C:
                        return "N/m";
                    case eUnits.kip_ft_F:
                    case eUnits.kip_in_F:
                        return "kip/ft";
                    case eUnits.lb_ft_F:
                    case eUnits.lb_in_F:
                        return "lb/ft";
                    default:
                        return "kN/m";
                }
            }
            catch
            {
                return "kN/m";
            }
        }

        private static readonly Dictionary<string, List<EtabsBeam>> _storyBeamsDictCache = new Dictionary<string, List<EtabsBeam>>(StringComparer.OrdinalIgnoreCase);

        public static void ClearAllStoryBeamsCache()
        {
            _storyBeamsDictCache.Clear();
            _cachedStoryName = "";
            _cachedStoryBeams = null;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ dầm thuộc một tầng cụ thể (sử dụng GetNameListOnStory siêu tốc và Cache đa tầng)
        /// </summary>
        public List<EtabsBeam> GetBeamsForStory(string storyName)
        {
            if (string.IsNullOrWhiteSpace(storyName)) return new List<EtabsBeam>();

            // 1. Kiểm tra cache trong bộ nhớ RAM
            if (_storyBeamsDictCache.TryGetValue(storyName.Trim(), out var cached) && cached != null && cached.Count > 0)
            {
                return cached;
            }

            var list = new List<EtabsBeam>();
            var sapModel = CurrentSapModel;
            if (sapModel == null) return list;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

            try
            {
                // 2. Truy vấn siêu tốc qua GetNameListOnStory (chỉ ~100 COM calls thay vì 25.000 calls)
                int numNamesOnStory = 0;
                string[] frameNames = Array.Empty<string>();

                int retStory = sapModel.FrameObj.GetNameListOnStory(storyName.Trim(), ref numNamesOnStory, ref frameNames);
                if (retStory == 0 && frameNames != null && frameNames.Length > 0)
                {
                    foreach (var name in frameNames)
                    {
                        string pt1 = "";
                        string pt2 = "";
                        int retPoints = sapModel.FrameObj.GetPoints(name, ref pt1, ref pt2);
                        if (retPoints == 0)
                        {
                            double x1 = 0, y1 = 0, z1 = 0;
                            double x2 = 0, y2 = 0, z2 = 0;
                            int retC1 = sapModel.PointObj.GetCoordCartesian(pt1, ref x1, ref y1, ref z1);
                            int retC2 = sapModel.PointObj.GetCoordCartesian(pt2, ref x2, ref y2, ref z2);

                            if (retC1 == 0 && retC2 == 0)
                            {
                                // Lọc dầm: phần tử nằm ngang (|Z1 - Z2| < 0.001)
                                if (Math.Abs(z1 - z2) < 0.001)
                                {
                                    string propName = "";
                                    string sAuto = "";
                                    sapModel.FrameObj.GetSection(name, ref propName, ref sAuto);

                                    list.Add(new EtabsBeam
                                    {
                                        Name = name,
                                        Point1 = pt1,
                                        Point2 = pt2,
                                        X1 = x1,
                                        Y1 = y1,
                                        Z1 = z1,
                                        X2 = x2,
                                        Y2 = y2,
                                        Z2 = z2,
                                        Section = propName
                                    });
                                }
                            }
                        }
                    }

                    if (list.Count > 0)
                    {
                        _storyBeamsDictCache[storyName.Trim()] = list;
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi truy vấn GetNameListOnStory {storyName}: {ex.Message}");
            }

            // 3. Fallback: Nếu GetNameListOnStory không ra dầm, dùng thuật toán lọc cao độ
            var allBeams = GetBeamsList();
            if (allBeams.Count == 0) return list;

            string[] storyNames;
            double[] storyHeights;
            double[] storyElevations;
            int count = GetStoryList(out storyNames, out storyHeights, out storyElevations);

            double targetElevation = 0.0;
            double storyHeight = 3.5;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (storyNames[i].Equals(storyName, StringComparison.OrdinalIgnoreCase))
                {
                    targetElevation = storyElevations[i];
                    storyHeight = storyHeights[i] > 0 ? storyHeights[i] : 3.5;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return new List<EtabsBeam>();
            }

            // Lọc các dầm có cao độ nằm trong dải tầng
            double maxDelta = 0.001;
            foreach (var b in allBeams)
            {
                if (Math.Abs(b.Z1 - targetElevation) <= maxDelta)
                {
                    list.Add(b);
                }
            }

            var result = list;
            _storyBeamsDictCache[storyName.Trim()] = result;
            return result;
        }

        /// <summary>
        /// Chuyển đổi tên hướng tải thành mã direction trong ETABS API
        /// Lưu ý: Theo chuẩn CSiAPIv1, Dir = 10 là Gravity (hướng trọng lực xuống dưới, -Z), Dir = 11 là Gravity Proj.
        /// </summary>
        public static int GetDirectionCode(string directionName) => LoadDirections.Code(directionName);

        /// <summary>
        /// Đọc toàn bộ các phân đoạn tải phân bố (Distributed Loads) đang gán trên dầm
        /// </summary>
        public List<EtabsFrameDistributedLoad> GetBeamDistributedLoads(string frameName, string loadPattern, int direction)
        {
            var list = new List<EtabsFrameDistributedLoad>();
            var sapModel = CurrentSapModel;
            if (sapModel == null) return list;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

            try
            {
                int numberLoads = 0;
                string[] frameNames = Array.Empty<string>();
                string[] loadPats = Array.Empty<string>();
                int[] myTypes = Array.Empty<int>();
                string[] cSyss = Array.Empty<string>();
                int[] dirs = Array.Empty<int>();
                double[] rd1 = Array.Empty<double>();
                double[] rd2 = Array.Empty<double>();
                double[] d1 = Array.Empty<double>();
                double[] d2 = Array.Empty<double>();
                double[] val1 = Array.Empty<double>();
                double[] val2 = Array.Empty<double>();

                // CSiAPIv1.cFrameObj.GetLoadDistributed:
                // 0: Name (string)
                // 1: ref NumberItems (int)
                // 2: ref FrameName (string[])
                // 3: ref LoadPat (string[])
                // 4: ref MyType (int[])
                // 5: ref CSys (string[])
                // 6: ref Dir (int[])
                // 7: ref RD1 (double[])
                // 8: ref RD2 (double[])
                // 9: ref Dist1 (double[])
                // 10: ref Dist2 (double[])
                // 11: ref Val1 (double[])
                // 12: ref Val2 (double[])
                // 13: ItemType (eItemType)
                int ret = sapModel.FrameObj.GetLoadDistributed(
                    frameName,
                    ref numberLoads,
                    ref frameNames,
                    ref loadPats,
                    ref myTypes,
                    ref cSyss,
                    ref dirs,
                    ref rd1,
                    ref rd2,
                    ref d1,
                    ref d2,
                    ref val1,
                    ref val2,
                    CSiAPIv1.eItemType.Objects
                );

                if (ret == 0 && numberLoads > 0 && loadPats != null)
                {
                    for (int i = 0; i < numberLoads; i++)
                    {
                        string currentPat = (loadPats != null && loadPats.Length > i) ? loadPats[i]?.Trim() ?? "" : "";

                        // Lọc theo Load Pattern (không phân biệt hoa thường)
                        if (!string.IsNullOrEmpty(loadPattern) && !currentPat.Equals(loadPattern.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Lọc theo Direction (nếu direction > 0)
                        if (direction > 0)
                        {
                            int d = (dirs != null && dirs.Length > i) ? dirs[i] : 10;

                            bool dirMatch = (d == direction);
                            if (!dirMatch)
                            {
                                continue;
                            }
                        }

                        list.Add(new EtabsFrameDistributedLoad
                        {
                            FrameName = (frameNames != null && frameNames.Length > i) ? frameNames[i] : frameName,
                            LoadPat = (loadPats != null && loadPats.Length > i) ? loadPats[i] : loadPattern,
                            CSys = (cSyss != null && cSyss.Length > i) ? cSyss[i] : "Global",
                            Dir = (dirs != null && dirs.Length > i) ? dirs[i] : 6,
                            RelDist1 = (rd1 != null && rd1.Length > i) ? rd1[i] : 0,
                            RelDist2 = (rd2 != null && rd2.Length > i) ? rd2[i] : 1,
                            Dist1 = (d1 != null && d1.Length > i) ? d1[i] : 0,
                            Dist2 = (d2 != null && d2.Length > i) ? d2[i] : 0,
                            Val1 = (val1 != null && val1.Length > i) ? Math.Round(val1[i], 2) : 0,
                            Val2 = (val2 != null && val2.Length > i) ? Math.Round(val2[i], 2) : 0
                        });
                    }

                    // Fallback: Nếu không tìm thấy tải khớp hướng, nhưng có tải thuộc loadPattern đó thì lấy tất cả
                    if (list.Count == 0 && !string.IsNullOrEmpty(loadPattern))
                    {
                        for (int i = 0; i < numberLoads; i++)
                        {
                            string currentPat = (loadPats != null && loadPats.Length > i) ? loadPats[i]?.Trim() ?? "" : "";
                            if (currentPat.Equals(loadPattern.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                list.Add(new EtabsFrameDistributedLoad
                                {
                                    FrameName = (frameNames != null && frameNames.Length > i) ? frameNames[i] : frameName,
                                    LoadPat = (loadPats != null && loadPats.Length > i) ? loadPats[i] : loadPattern,
                                    CSys = (cSyss != null && cSyss.Length > i) ? cSyss[i] : "Global",
                                    Dir = (dirs != null && dirs.Length > i) ? dirs[i] : 6,
                                    RelDist1 = (rd1 != null && rd1.Length > i) ? rd1[i] : 0,
                                    RelDist2 = (rd2 != null && rd2.Length > i) ? rd2[i] : 1,
                                    Dist1 = (d1 != null && d1.Length > i) ? d1[i] : 0,
                                    Dist2 = (d2 != null && d2.Length > i) ? d2[i] : 0,
                                    Val1 = (val1 != null && val1.Length > i) ? Math.Round(val1[i], 2) : 0,
                                    Val2 = (val2 != null && val2.Length > i) ? Math.Round(val2[i], 2) : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc tải phân bố dầm {frameName}: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Lấy danh sách các dầm đang được quét chọn (Selected Frames) trong ETABS
        /// </summary>
        public List<EtabsBeam> GetSelectedBeamsList()
        {
            var list = new List<EtabsBeam>();
            var sapModel = CurrentSapModel;
            if (sapModel == null) return list;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

            try
            {
                int numberItems = 0;
                int[] objectTypes = Array.Empty<int>();
                string[] objectNames = Array.Empty<string>();

                int ret = sapModel.SelectObj.GetSelected(ref numberItems, ref objectTypes, ref objectNames);
                if (ret == 0 && numberItems > 0 && objectNames != null)
                {
                    for (int i = 0; i < numberItems; i++)
                    {
                        // ObjectType = 2 (Frame Object)
                        if (objectTypes[i] == 2)
                        {
                            string frameName = objectNames[i];

                            string pt1 = "";
                            string pt2 = "";
                            int retPoints = sapModel.FrameObj.GetPoints(frameName, ref pt1, ref pt2);

                            if (retPoints == 0)
                            {
                                double x1 = 0, y1 = 0, z1 = 0;
                                double x2 = 0, y2 = 0, z2 = 0;
                                sapModel.PointObj.GetCoordCartesian(pt1, ref x1, ref y1, ref z1);
                                sapModel.PointObj.GetCoordCartesian(pt2, ref x2, ref y2, ref z2);

                                // Lọc dầm: Phần tử nằm ngang (|Z1 - Z2| < 0.001)
                                if (Math.Abs(z1 - z2) < 0.001)
                                {
                                    string secName = "";
                                    string sAuto = "";
                                    sapModel.FrameObj.GetSection(frameName, ref secName, ref sAuto);

                                    list.Add(new EtabsBeam
                                    {
                                        Name = frameName,
                                        Section = secName,
                                        X1 = x1,
                                        Y1 = y1,
                                        Z1 = z1,
                                        X2 = x2,
                                        Y2 = y2,
                                        Z2 = z2
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy danh sách dầm được chọn từ ETABS: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Trích xuất danh sách dầm kèm phân đoạn tải và vị trí Chấm tròn Dot ngưng tải:
        /// Ưu tiên 1: Lấy các dầm ĐANG ĐƯỢC QUÉT CHỌN TRONG ETABS.
        /// Ưu tiên 2: Nếu không quét chọn dầm nào thì lấy toàn bộ dầm theo Tầng (storyName).
        /// </summary>
        public List<CadBeamLoadExportItem> GetSelectedOrStoryBeamsWithDistributedLoads(string storyName, string loadPattern, int direction, double baseX, double baseY, out bool isSelectedMode)
        {
            var result = new List<CadBeamLoadExportItem>();
            var sapModel = CurrentSapModel;
            isSelectedMode = false;
            if (sapModel == null) return result;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");

            try
            {
                // 1. Kiểm tra dầm đang được quét chọn trong ETABS
                var storyBeams = GetBeamsForStory(storyName);
                var storyIds = new HashSet<string>(storyBeams.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
                var targetBeams = GetSelectedBeamsList();
                if (targetBeams.Any(x => !storyIds.Contains(x.Name)))
                    throw new InvalidOperationException("Vùng chọn ETABS có dầm ngoài tầng đang chọn. Hãy chọn lại đúng tầng.");
                if (targetBeams.Count > 0)
                {
                    isSelectedMode = true;
                }
                else
                {
                    // 2. Fallback: Lấy theo tầng nếu không có dầm nào được chọn
                    isSelectedMode = false;
                    targetBeams = GetBeamsForStory(storyName);
                }

                string unitLabel = GetUnitLabel();

                // 3. Xử lý từng dầm và các phân đoạn tải (Quy đổi 1m ETABS -> 1000mm AutoCAD)
                foreach (var beam in targetBeams)
                {
                    double cadX1 = (beam.X1 * 1000.0) + baseX;
                    double cadY1 = (beam.Y1 * 1000.0) + baseY;
                    double cadX2 = (beam.X2 * 1000.0) + baseX;
                    double cadY2 = (beam.Y2 * 1000.0) + baseY;

                    double beamLengthMm = Math.Sqrt((cadX2 - cadX1) * (cadX2 - cadX1) + (cadY2 - cadY1) * (cadY2 - cadY1));
                    if (beamLengthMm < 1.0) continue; // Bỏ qua dầm < 1mm

                    // Tính góc xoay dầm (độ)
                    double angleRad = Math.Atan2(cadY2 - cadY1, cadX2 - cadX1);
                    double angleDeg = angleRad * (180.0 / Math.PI);
                    // Chuẩn hóa góc xoay text theo quy chuẩn kỹ thuật (không bị ngược đầu): -90 đến 90
                    if (angleDeg > 90) angleDeg -= 180;
                    else if (angleDeg < -90) angleDeg += 180;

                    // Tính toán mã màu TrueColor RGB theo tiết diện dầm
                    var (r, g, b) = SectionColorHelper.GetSectionRgb(beam.Section);
                    string hexColor = $"#{r:X2}{g:X2}{b:X2}";

                    var exportItem = new CadBeamLoadExportItem
                    {
                        BeamName = beam.Name,
                        Section = beam.Section,
                        ColorR = r,
                        ColorG = g,
                        ColorB = b,
                        ColorHex = hexColor,
                        X1 = Math.Round(cadX1, 3),
                        Y1 = Math.Round(cadY1, 3),
                        X2 = Math.Round(cadX2, 3),
                        Y2 = Math.Round(cadY2, 3),
                        Length = Math.Round(beamLengthMm, 3),
                        AngleDeg = Math.Round(angleDeg, 2)
                    };

                    // Đọc tải phân bố trên dầm (ETABS trả về khoảng cách mét m)
                    var distributedLoads = GetBeamDistributedLoads(beam.Name, loadPattern, direction);

                    if (distributedLoads.Count == 0)
                    {
                        // Nếu dầm chưa có tải: Đặt phân đoạn mặc định 0 kN/m tại trung điểm
                        double midX = (cadX1 + cadX2) / 2.0;
                        double midY = (cadY1 + cadY2) / 2.0;

                        exportItem.Segments.Add(new CadLoadSegment
                        {
                            StartDist = 0,
                            EndDist = beamLengthMm,
                            Val1 = 0,
                            Val2 = 0,
                            MidX = Math.Round(midX, 3),
                            MidY = Math.Round(midY, 3),
                            AngleDeg = Math.Round(angleDeg, 2),
                            Text = "0"
                        });
                    }
                    else
                    {
                        // Xử lý từng phân đoạn tải
                        var breakDistances = new HashSet<double>();

                        foreach (var load in distributedLoads)
                        {
                            double sDistMm = 0;
                            double eDistMm = beamLengthMm;

                            if (load.Dist2 > load.Dist1)
                            {
                                sDistMm = load.Dist1 * 1000.0;
                                eDistMm = load.Dist2 * 1000.0;
                            }
                            else if (load.RelDist2 > load.RelDist1)
                            {
                                sDistMm = load.RelDist1 * beamLengthMm;
                                eDistMm = load.RelDist2 * beamLengthMm;
                            }

                            if (eDistMm <= 0 || eDistMm > beamLengthMm) eDistMm = beamLengthMm;
                            if (sDistMm < 0) sDistMm = 0;

                            // Vị trí trung điểm phân đoạn trên CAD
                            double segMidDist = sDistMm + (eDistMm - sDistMm) / 2.0;
                            double ratio = beamLengthMm > 0 ? segMidDist / beamLengthMm : 0.5;
                            double segMidX = cadX1 + ratio * (cadX2 - cadX1);
                            double segMidY = cadY1 + ratio * (cadY2 - cadY1);

                            double avgVal = Math.Round((Math.Abs(load.Val1) + Math.Abs(load.Val2)) / 2.0, 2);

                            exportItem.Segments.Add(new CadLoadSegment
                            {
                                StartDist = Math.Round(sDistMm, 3),
                                EndDist = Math.Round(eDistMm, 3),
                                Val1 = Math.Abs(load.Val1),
                                Val2 = Math.Abs(load.Val2),
                                MidX = Math.Round(segMidX, 3),
                                MidY = Math.Round(segMidY, 3),
                                AngleDeg = Math.Round(angleDeg, 2),
                                Text = avgVal > 0 ? $"{avgVal:0.##} {unitLabel}" : "0"
                            });

                            // Ghi nhận điểm ngắt tải (BreakDots) nếu không phải đầu mút 0 hoặc L
                            if (sDistMm > 100.0 && sDistMm < beamLengthMm - 100.0) breakDistances.Add(sDistMm);
                            if (eDistMm > 100.0 && eDistMm < beamLengthMm - 100.0) breakDistances.Add(eDistMm);
                        }

                        // Tạo các chấm tròn (Dot) ngưng tải
                        foreach (var bDist in breakDistances)
                        {
                            double bRatio = beamLengthMm > 0 ? bDist / beamLengthMm : 0;
                            double dotX = cadX1 + bRatio * (cadX2 - cadX1);
                            double dotY = cadY1 + bRatio * (cadY2 - cadY1);

                            exportItem.BreakDots.Add(new CadBreakDot
                            {
                                X = Math.Round(dotX, 3),
                                Y = Math.Round(dotY, 3),
                                DistAlongBeam = Math.Round(bDist, 3)
                            });
                        }
                    }

                    result.Add(exportItem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi trích xuất mặt bằng tải dầm: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gán phân đoạn tải phân bố vào dầm
        /// </summary>
        public bool AssignSegmentedDistributedLoads(string frameName, string loadPattern, int direction, List<CadLoadSegment> segments, bool replace)
        {
            using (BeginWrite())
            {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return false;

            if (!EnsureStandardUnits()) throw new InvalidOperationException("Không chuẩn hóa được đơn vị ETABS.");
            if (!EnsureLoadPatternExists(loadPattern)) throw new InvalidOperationException("Load pattern chưa tồn tại trong ETABS.");

            try
            {
                if (segments == null || segments.Count == 0) return false;

                double previousEnd = 0;
                foreach (var seg in segments.OrderBy(x => x.StartDist)) {
                    if (!LoadTextParser.Finite(seg.StartDist) || !LoadTextParser.Finite(seg.EndDist) || !LoadTextParser.Finite(seg.Val1) || !LoadTextParser.Finite(seg.Val2) || seg.StartDist < previousEnd - 1e-6 || seg.EndDist <= seg.StartDist)
                        throw new ArgumentException("Phân đoạn tải không hợp lệ hoặc chồng lấn.");
                    previousEnd = seg.EndDist;
                }
                bool isFirst = true;
                foreach (var seg in segments)
                {
                    bool repFlag = isFirst ? replace : false;
                    string cSys = LoadDirections.CoordinateSystem(direction);

                    // CSiAPIv1.cFrameObj.SetLoadDistributed:
                    // (string Name, string LoadPat, int MyType, int Dir, double Dist1, double Dist2, double Val1, double Val2, string CSys, bool RelDist, bool Replace, eItemType ItemType)
                    int ret = sapModel.FrameObj.SetLoadDistributed(
                        frameName,
                        loadPattern,
                        1, // 1: Force per unit length
                        direction,
                        seg.StartDist,
                        seg.EndDist,
                        seg.Val1,
                        seg.Val2,
                        cSys,
                        false, // false: Absolute distance (meters)
                        repFlag // Replace on first segment, Add on subsequent
                    );


                    isFirst = false;
                    if (ret != 0) throw new InvalidOperationException("ETABS lỗi " + ret + " sau khi có thể đã thay một phần tải dầm " + frameName);
                    VerifyFrame(frameName, loadPattern, direction, seg.StartDist, seg.EndDist, seg.Val1, seg.Val2, false);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gán tải phân đoạn dầm {frameName}: {ex.Message}");
                return false;
            }

            }
        }

        /// <summary>
        /// Xóa tải phân bố trên dầm trong ETABS (hỗ trợ xóa triệt để hoặc gán 0 với Replace = true)
        /// </summary>
        public bool DeleteFrameDistributedLoads(string frameName, string loadPattern)
        {
            using (BeginWrite())
            {
            var sapModel = CurrentSapModel;
            if (sapModel == null) return false;

            try
            {
                // CSiAPIv1.cFrameObj.DeleteLoadDistributed
                int ret = sapModel.FrameObj.DeleteLoadDistributed(frameName, loadPattern);
                return ret == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi xóa tải dầm {frameName}: {ex.Message}");
                return false;
            }

            }
        }

        #region QKS SHELL LOAD METHODS

        private static readonly Dictionary<string, List<EtabsSlabItem>> _storySlabsDictCache = new Dictionary<string, List<EtabsSlabItem>>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ShellLoadSetItem> _storedLoadSets = new List<ShellLoadSetItem>
        {
            new ShellLoadSetItem
            {
                LoadSetName = "Apartment",
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 1.22, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 2.0, Direction = "Gravity" }
                }
            },
            new ShellLoadSetItem
            {
                LoadSetName = "Balcony",
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 1.5, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 2.0, Direction = "Gravity" }
                }
            },
            new ShellLoadSetItem
            {
                LoadSetName = "WC",
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 1.8, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 2.0, Direction = "Gravity" }
                }
            },
            new ShellLoadSetItem
            {
                LoadSetName = "Lobby",
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 1.5, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 3.0, Direction = "Gravity" }
                }
            },
            new ShellLoadSetItem
            {
                LoadSetName = "Staircase",
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 2.0, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 3.0, Direction = "Gravity" }
                }
            },
            new ShellLoadSetItem
            {
                LoadSetName = "MEP Room",
                Loads = new List<ShellLoadSetDetail>
                {
                    new ShellLoadSetDetail { LoadPattern = "SDL", LoadValue = 1.5, Direction = "Gravity" },
                    new ShellLoadSetDetail { LoadPattern = "LL", LoadValue = 5.0, Direction = "Gravity" }
                }
            }
        };

        private static readonly Dictionary<string, string> _sectionAssignedLoadSets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Chuẩn hóa chuỗi tên (chỉ giữ chữ cái và chữ số viết hoa để đối sánh tiết diện và Load Set)
        /// </summary>
        public static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Tìm Load Set phù hợp nhất theo tên tiết diện (nhận diện tiền tố độ dày sàn S120_, S150_...)
        /// </summary>
        public static ShellLoadSetItem? FindMatchingLoadSetBySectionName(string sectionName, IEnumerable<ShellLoadSetItem> loadSets)
        {
            if (string.IsNullOrWhiteSpace(sectionName) || loadSets == null) return null;

            var validSets = loadSets.Where(s => !string.IsNullOrWhiteSpace(s.LoadSetName)).ToList();
            if (validSets.Count == 0) return null;

            // 1. Khớp chính xác tuyệt đối (không phân biệt hoa thường)
            var exactMatch = validSets.FirstOrDefault(s => s.LoadSetName.Trim().Equals(sectionName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            string normSec = NormalizeName(sectionName);
            if (string.IsNullOrEmpty(normSec)) return null;

            // Sắp xếp các Load Set theo độ dài chuỗi chuẩn hóa giảm dần để ưu tiên tên dài, cụ thể hơn
            // (vd: HANHLANG_CSKD kiểm tra trước CSKD, WC-TUB kiểm tra trước WC)
            var sortedSets = validSets.OrderByDescending(s => NormalizeName(s.LoadSetName).Length).ToList();

            // 2. Khớp chuẩn hóa tuyệt đối (vd: CSKD == CSKD)
            foreach (var ls in sortedSets)
            {
                string lsNorm = NormalizeName(ls.LoadSetName);
                if (normSec == lsNorm) return ls;
            }

            // 3. Khớp Normalized EndsWith (vd: S120_HANHLANG CSKD kết thúc bằng HANHLANGCSKD, S120_CSKD kết thúc bằng CSKD)
            foreach (var ls in sortedSets)
            {
                string lsNorm = NormalizeName(ls.LoadSetName);
                if (lsNorm.Length >= 2 && normSec.EndsWith(lsNorm, StringComparison.OrdinalIgnoreCase))
                {
                    return ls;
                }
            }

            // 4. Khớp Normalized Contains (ưu tiên tên dài >= 3 ký tự)
            foreach (var ls in sortedSets)
            {
                string lsNorm = NormalizeName(ls.LoadSetName);
                if (lsNorm.Length >= 3 && normSec.IndexOf(lsNorm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ls;
                }
            }

            return null;
        }

        /// <summary>
        /// Cập nhật bộ nhớ đệm Load Sets từ UI
        /// </summary>
        public void UpdateStoredLoadSets(IEnumerable<ShellLoadSetItem> loadSets)
        {
            if (loadSets == null) return;
            lock (_storedLoadSets)
            {
                _storedLoadSets.Clear();
                foreach (var ls in loadSets)
                {
                    _storedLoadSets.Add(new ShellLoadSetItem
                    {
                        LoadSetName = ls.LoadSetName,
                        OriginalName = ls.LoadSetName,
                        Loads = ls.Loads != null
                            ? ls.Loads.Select(d => new ShellLoadSetDetail { LoadPattern = d.LoadPattern, LoadValue = d.LoadValue, Direction = d.Direction }).ToList()
                            : new List<ShellLoadSetDetail>()
                    });
                }
            }
        }

        private static ShellLoadSetItem? MatchLoadSet(List<(string pat, double val)> slabLoads, List<ShellLoadSetItem> loadSets)
        {
            if (slabLoads == null || slabLoads.Count == 0 || loadSets == null || loadSets.Count == 0)
                return null;

            var nonZeroSlabLoads = slabLoads.Where(l => Math.Abs(l.val) > 0.001).ToList();
            if (nonZeroSlabLoads.Count == 0) return null;

            foreach (var ls in loadSets)
            {
                if (ls.Loads == null || ls.Loads.Count == 0) continue;
                var nonZeroLsLoads = ls.Loads.Where(l => Math.Abs(l.LoadValue) > 0.001).ToList();
                if (nonZeroLsLoads.Count == 0) continue;

                if (nonZeroLsLoads.Count != nonZeroSlabLoads.Count) continue;

                bool allMatch = true;
                foreach (var lsDetail in nonZeroLsLoads)
                {
                    var match = nonZeroSlabLoads.FirstOrDefault(sl =>
                        sl.pat.Equals(lsDetail.LoadPattern, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(sl.val - lsDetail.LoadValue) < 0.01);

                    if (string.IsNullOrEmpty(match.pat))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return ls;
                }
            }

            return null;
        }

        private static Dictionary<string, string> GetAreaUniformLoadSetAssignments(cSapModel sapModel)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (sapModel == null) return dict;

            try
            {
                int numTables = 0;
                string[] tableKeys = Array.Empty<string>();
                string[] tableNames = Array.Empty<string>();
                int[] importTypes = Array.Empty<int>();
                bool[] isEmpty = Array.Empty<bool>();

                int ret = sapModel.DatabaseTables.GetAllTables(ref numTables, ref tableKeys, ref tableNames, ref importTypes, ref isEmpty);
                if (ret == 0 && numTables > 0 && tableKeys != null)
                {
                    string targetTable = "";
                    foreach (var k in tableKeys)
                    {
                        if (k.IndexOf("Uniform Load Set", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            (k.IndexOf("Assign", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            targetTable = k;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(targetTable))
                    {
                        int tVer = 0;
                        string[] fieldsKeysIncluded = Array.Empty<string>();
                        int numRecords = 0;
                        string[] tableData = Array.Empty<string>();

                        int retData = sapModel.DatabaseTables.GetTableForEditingArray(targetTable, "", ref tVer, ref fieldsKeysIncluded, ref numRecords, ref tableData);
                        if (retData == 0 && numRecords > 0 && tableData != null && fieldsKeysIncluded != null)
                        {
                            int cols = fieldsKeysIncluded.Length;
                            int areaCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Area", StringComparison.OrdinalIgnoreCase) || f.Equals("AreaObject", StringComparison.OrdinalIgnoreCase) || f.Equals("Name", StringComparison.OrdinalIgnoreCase) || f.Equals("AreaName", StringComparison.OrdinalIgnoreCase));
                            int setCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("LoadSet", StringComparison.OrdinalIgnoreCase) || f.Equals("UniformLoadSet", StringComparison.OrdinalIgnoreCase) || f.Equals("SetName", StringComparison.OrdinalIgnoreCase) || f.Equals("UniformLoadSetName", StringComparison.OrdinalIgnoreCase));

                            if (areaCol >= 0 && setCol >= 0)
                            {
                                for (int r = 0; r < numRecords; r++)
                                {
                                    int offset = r * cols;
                                    string area = (offset + areaCol < tableData.Length) ? tableData[offset + areaCol]?.Trim() ?? "" : "";
                                    string set = (offset + setCol < tableData.Length) ? tableData[offset + setCol]?.Trim() ?? "" : "";
                                    if (!string.IsNullOrEmpty(area) && !string.IsNullOrEmpty(set))
                                    {
                                        dict[area] = set;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đọc Area Assignments Uniform Load Sets: {ex.Message}");
            }

            return dict;
        }

        public void ClearSlabCache()
        {
            _storySlabsDictCache.Clear();
        }

        /// <summary>
        /// Lấy danh sách ô sàn (Area/Shell) theo tầng siêu tốc kết hợp In-Memory Cache
        /// </summary>
        public List<EtabsSlabItem> GetSlabsForStory(string storyName, string loadPattern = "", string loadSetName = "")
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null || string.IsNullOrWhiteSpace(storyName)) return new List<EtabsSlabItem>();

            string cacheKey = $"{storyName}|{loadPattern}|{loadSetName}";
            if (_storySlabsDictCache.TryGetValue(cacheKey, out var cachedList) && cachedList != null && cachedList.Count > 0)
            {
                return cachedList;
            }

            var result = new List<EtabsSlabItem>();
            try
            {
                lock (_etabsLock)
                {
                    int numAreas = 0;
                    string[] areaNames = Array.Empty<string>();

                    int ret = sapModel.AreaObj.GetNameListOnStory(storyName, ref numAreas, ref areaNames);
                    if (ret != 0 || numAreas == 0 || areaNames == null)
                    {
                        return result;
                    }

                    var areaLoadSetAssignments = GetAreaUniformLoadSetAssignments(sapModel);

                    foreach (var areaName in areaNames)
                    {
                        if (string.IsNullOrEmpty(areaName)) continue;

                        string propName = "";
                        sapModel.AreaObj.GetProperty(areaName, ref propName);

                        int numPoints = 0;
                        string[] pointNames = Array.Empty<string>();
                        sapModel.AreaObj.GetPoints(areaName, ref numPoints, ref pointNames);

                        if (numPoints < 3 || pointNames == null) continue;

                        var points = new List<(double X, double Y, double Z)>();
                        for (int i = 0; i < numPoints; i++)
                        {
                            double px = 0, py = 0, pz = 0;
                            sapModel.PointObj.GetCoordCartesian(pointNames[i], ref px, ref py, ref pz);
                            points.Add((px, py, pz));
                        }

                        if (points.Count < 3) continue;

                        // LỌC PHẦN TỬ: Chỉ lấy các phần tử tấm sàn nằm ngang trên mặt phẳng (Slab/Floor),
                        // Bỏ qua toàn bộ phần tử vách thẳng đứng (Wall) hoặc tấm đứng nghiêng
                        double minZ = points.Min(p => p.Z);
                        double maxZ = points.Max(p => p.Z);
                        if (Math.Abs(maxZ - minZ) > 0.05)
                        {
                            // Tấm vách thẳng đứng (Wall) nối giữa các cao độ tầng -> bỏ qua
                            continue;
                        }

                        // Tính diện tích và tâm đa giác (Shoelace Formula)
                        double areaM2 = 0.0;
                        double cx = 0.0;
                        double cy = 0.0;
                        int n = points.Count;

                        for (int i = 0; i < n; i++)
                        {
                            int next = (i + 1) % n;
                            double cross = points[i].X * points[next].Y - points[next].X * points[i].Y;
                            areaM2 += cross;
                            cx += (points[i].X + points[next].X) * cross;
                            cy += (points[i].Y + points[next].Y) * cross;
                        }

                        areaM2 = Math.Abs(areaM2) / 2.0;
                        if (areaM2 < 0.005)
                        {
                            // Diện tích chiếu bằng XY xấp xỉ 0 (vách đứng) -> bỏ qua
                            continue;
                        }

                        if (areaM2 > 0.0001)
                        {
                            double sign = crossSign(points);
                            cx = cx / (6.0 * areaM2 * sign);
                            cy = cy / (6.0 * areaM2 * sign);
                        }
                        else
                        {
                            cx = points.Average(p => p.X);
                            cy = points.Average(p => p.Y);
                        }

                        // Lấy màu tiết diện
                        var (cr, cg, cb) = SectionColorHelper.GetSectionRgb(propName);

                        // Đọc tải trọng
                        double assignedLoadVal = 0.0;
                        string assignedPat = "";
                        var allPatLoads = new List<(string Pat, double Val)>();

                        int nLoads = 0;
                        string[] aNames = Array.Empty<string>();
                        string[] pNames = Array.Empty<string>();
                        string[] cSysNames = Array.Empty<string>();
                        int[] dirs = Array.Empty<int>();
                        double[] vals = Array.Empty<double>();

                        int loadRet = sapModel.AreaObj.GetLoadUniform(areaName, ref nLoads, ref aNames, ref pNames, ref cSysNames, ref dirs, ref vals, eItemType.Objects);
                        if (loadRet == 0 && nLoads > 0 && pNames != null && vals != null)
                        {
                            for (int li = 0; li < nLoads; li++)
                            {
                                if (string.IsNullOrEmpty(loadPattern) || pNames[li].Equals(loadPattern, StringComparison.OrdinalIgnoreCase))
                                {
                                    assignedLoadVal += vals[li];
                                    assignedPat = pNames[li];
                                }
                                allPatLoads.Add((pNames[li], vals[li]));
                            }
                        }

                        // 1. Xác định LoadSet thực tế của ô sàn
                        string actualSlabLoadSet = "";
                        ShellLoadSetItem? matchedSet = null;

                        // A. Kiểm tra assignment từ bảng ETABS
                        if (areaLoadSetAssignments != null && areaLoadSetAssignments.TryGetValue(areaName, out string etabsAssignedSet) && !string.IsNullOrEmpty(etabsAssignedSet))
                        {
                            actualSlabLoadSet = etabsAssignedSet;
                            matchedSet = _storedLoadSets.Find(s => s.LoadSetName.Equals(actualSlabLoadSet, StringComparison.OrdinalIgnoreCase));
                        }

                        // B. Kiểm tra assignment theo tiết diện trong phiên làm việc
                        if (string.IsNullOrEmpty(actualSlabLoadSet) && !string.IsNullOrEmpty(propName) && _sectionAssignedLoadSets.TryGetValue(propName, out string sessAssignedSet))
                        {
                            actualSlabLoadSet = sessAssignedSet;
                            matchedSet = _storedLoadSets.Find(s => s.LoadSetName.Equals(actualSlabLoadSet, StringComparison.OrdinalIgnoreCase));
                        }

                        // C. Kiểm tra nếu tên tiết diện khớp một LoadSet đã khai báo (nhận diện cả tiền tố S120_, S150_...)
                        if (string.IsNullOrEmpty(actualSlabLoadSet) && !string.IsNullOrEmpty(propName))
                        {
                            var setBySection = FindMatchingLoadSetBySectionName(propName, _storedLoadSets);
                            if (setBySection != null)
                            {
                                actualSlabLoadSet = setBySection.LoadSetName;
                                matchedSet = setBySection;
                            }
                        }

                        // D. Khớp nối theo chữ ký tải trọng (Load Signature: Patterns & Values)
                        if (string.IsNullOrEmpty(actualSlabLoadSet) && allPatLoads.Count > 0)
                        {
                            matchedSet = MatchLoadSet(allPatLoads, _storedLoadSets);
                            if (matchedSet != null)
                            {
                                actualSlabLoadSet = matchedSet.LoadSetName;
                            }
                        }

                        if (matchedSet == null && !string.IsNullOrEmpty(actualSlabLoadSet))
                        {
                            matchedSet = _storedLoadSets.Find(s => s.LoadSetName.Equals(actualSlabLoadSet, StringComparison.OrdinalIgnoreCase));
                        }

                        string formattedText = "0";
                        string slabAssignedLoadSetName = actualSlabLoadSet;

                        if (!string.IsNullOrEmpty(loadSetName))
                        {
                            bool isAllMode = loadSetName.Equals("<Tất cả Load Sets>", StringComparison.OrdinalIgnoreCase) ||
                                             loadSetName.Equals("ALL", StringComparison.OrdinalIgnoreCase);

                            if (isAllMode)
                            {
                                if (matchedSet != null && matchedSet.Loads != null && matchedSet.Loads.Count > 0)
                                {
                                    var loadStrs = matchedSet.Loads.Select(d => $"{d.LoadPattern} = {d.LoadValue:0.##} kN/m²");
                                    formattedText = $"{matchedSet.LoadSetName}\n" + string.Join("\n", loadStrs);
                                }
                                else if (!string.IsNullOrEmpty(actualSlabLoadSet))
                                {
                                    formattedText = actualSlabLoadSet;
                                }
                                else if (allPatLoads.Count > 0)
                                {
                                    var loadStrs = allPatLoads.Where(p => Math.Abs(p.Val) > 0.001).Select(p => $"{p.Pat} = {p.Val:0.##} kN/m²");
                                    formattedText = string.Join("\n", loadStrs);
                                }
                                else
                                {
                                    formattedText = "0";
                                }
                            }
                            else
                            {
                                // Người dùng chọn lọc theo 1 Load Set cụ thể (vd: CSKD hoặc HANHLANG_CSKD)
                                if (!string.IsNullOrEmpty(actualSlabLoadSet) && actualSlabLoadSet.Equals(loadSetName, StringComparison.OrdinalIgnoreCase))
                                {
                                    var setItem = matchedSet ?? _storedLoadSets.Find(s => s.LoadSetName.Equals(loadSetName, StringComparison.OrdinalIgnoreCase));
                                    if (setItem != null && setItem.Loads != null && setItem.Loads.Count > 0)
                                    {
                                        var loadStrs = setItem.Loads.Select(d => $"{d.LoadPattern} = {d.LoadValue:0.##} kN/m²");
                                        formattedText = $"{loadSetName}\n" + string.Join("\n", loadStrs);
                                    }
                                    else
                                    {
                                        formattedText = loadSetName;
                                    }
                                }
                                else
                                {
                                    // Ô sàn này KHÔNG thuộc Load Set đang chọn (Cầu Thang, Canopy, S120...) -> Không hiển thị text tải!
                                    formattedText = "0";
                                    slabAssignedLoadSetName = "";
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(loadPattern))
                        {
                            // Xuất duy nhất giá trị của Load Pattern được chọn từ Thiết Lập Tải Trọng Chung
                            if (Math.Abs(assignedLoadVal) > 0.001)
                            {
                                formattedText = $"{loadPattern} = {assignedLoadVal:0.##} kN/m²";
                            }
                            else if (matchedSet != null && matchedSet.Loads != null)
                            {
                                var patLoad = matchedSet.Loads.FirstOrDefault(l => l.LoadPattern.Equals(loadPattern, StringComparison.OrdinalIgnoreCase));
                                if (patLoad != null && Math.Abs(patLoad.LoadValue) > 0.001)
                                {
                                    formattedText = $"{loadPattern} = {patLoad.LoadValue:0.##} kN/m²";
                                }
                                else
                                {
                                    formattedText = "0";
                                }
                            }
                            else
                            {
                                formattedText = "0";
                            }
                        }
                        else
                        {
                            formattedText = "0";
                        }

                        result.Add(new EtabsSlabItem
                        {
                            Name = areaName,
                            Story = storyName,
                            Section = string.IsNullOrEmpty(propName) ? "None" : propName,
                            Points = points,
                            CentroidX = cx,
                            CentroidY = cy,
                            Area = areaM2,
                            ColorR = cr,
                            ColorG = cg,
                            ColorB = cb,
                            LoadSetName = slabAssignedLoadSetName,
                            LoadPattern = assignedPat,
                            LoadValue = assignedLoadVal,
                            FormattedLoadText = formattedText
                        });
                    }

                    _storySlabsDictCache[cacheKey] = result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi GetSlabsForStory {storyName}: {ex.Message}");
            }

            return result;
        }

        private static double crossSign(List<(double X, double Y, double Z)> pts)
        {
            double sum = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                int next = (i + 1) % pts.Count;
                sum += (pts[i].X * pts[next].Y - pts[next].X * pts[i].Y);
            }
            return sum >= 0 ? 1.0 : -1.0;
        }

        /// <summary>
        /// Thống kê danh sách tiết diện sàn tại tầng đang chọn
        /// </summary>
        public List<ShellSectionInfo> GetSlabSectionsForStory(string storyName)
        {
            var slabs = GetSlabsForStory(storyName);
            if (slabs == null || slabs.Count == 0) return new List<ShellSectionInfo>();

            var groups = slabs.GroupBy(s => s.Section, StringComparer.OrdinalIgnoreCase);
            var list = new List<ShellSectionInfo>();

            foreach (var g in groups)
            {
                string secName = g.Key;
                var (r, gr, b) = SectionColorHelper.GetSectionRgb(secName);
                string hex = $"#{r:X2}{gr:X2}{b:X2}";
                double totalArea = g.Sum(s => s.Area);
                int count = g.Count();

                list.Add(new ShellSectionInfo
                {
                    SectionName = secName,
                    Count = count,
                    TotalArea = Math.Round(totalArea, 2),
                    ColorR = r,
                    ColorG = gr,
                    ColorB = b,
                    HexColor = hex,
                    ThicknessMm = 120.0
                });
            }

            return list.OrderBy(s => s.SectionName).ToList();
        }

        /// <summary>
        /// Đọc danh sách Shell Uniform Load Sets từ mô hình ETABS (qua DatabaseTables OAPI) và đồng bộ In-Memory Cache
        /// </summary>
        public List<ShellLoadSetItem> GetShellUniformLoadSets()
        {
            var sapModel = CurrentSapModel;
            if (sapModel != null)
            {
                try
                {
                    lock (_etabsLock)
                    {
                        // 1. Tìm table key của bảng Uniform Load Sets trong ETABS
                        int numTables = 0;
                        string[] tableKeys = Array.Empty<string>();
                        string[] tableNames = Array.Empty<string>();
                        int[] importTypes = Array.Empty<int>();
                        bool[] isEmpty = Array.Empty<bool>();

                        string targetTableKey = "Uniform Load Sets";
                        int ret = sapModel.DatabaseTables.GetAllTables(ref numTables, ref tableKeys, ref tableNames, ref importTypes, ref isEmpty);
                        if (ret == 0 && numTables > 0 && tableKeys != null)
                        {
                            foreach (var key in tableKeys)
                            {
                                if (key.Equals("Uniform Load Sets", StringComparison.OrdinalIgnoreCase) ||
                                    key.Equals("Shell Uniform Load Sets", StringComparison.OrdinalIgnoreCase) ||
                                    (key.IndexOf("Uniform", StringComparison.OrdinalIgnoreCase) >= 0 && key.IndexOf("Set", StringComparison.OrdinalIgnoreCase) >= 0 && key.IndexOf("Assignment", StringComparison.OrdinalIgnoreCase) < 0))
                                {
                                    targetTableKey = key;
                                    break;
                                }
                            }
                        }

                        // 2. Đọc dữ liệu bảng
                        int tVer = 0;
                        string[] fieldsKeysIncluded = Array.Empty<string>();
                        int numRecords = 0;
                        string[] tableData = Array.Empty<string>();

                        int retData = sapModel.DatabaseTables.GetTableForEditingArray(targetTableKey, "", ref tVer, ref fieldsKeysIncluded, ref numRecords, ref tableData);
                        if (retData == 0 && numRecords > 0 && tableData != null && fieldsKeysIncluded != null && fieldsKeysIncluded.Length > 0)
                        {
                            int cols = fieldsKeysIncluded.Length;
                            int nameCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Name", StringComparison.OrdinalIgnoreCase) || f.Equals("LoadSet", StringComparison.OrdinalIgnoreCase) || f.Equals("UniformLoadSetName", StringComparison.OrdinalIgnoreCase) || f.Equals("SetName", StringComparison.OrdinalIgnoreCase));
                            int patCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("LoadPat", StringComparison.OrdinalIgnoreCase) || f.Equals("LoadPattern", StringComparison.OrdinalIgnoreCase) || f.Equals("Pattern", StringComparison.OrdinalIgnoreCase));
                            int valCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Value", StringComparison.OrdinalIgnoreCase) || f.Equals("LoadValue", StringComparison.OrdinalIgnoreCase) || f.Equals("UnifLoad", StringComparison.OrdinalIgnoreCase) || f.Equals("Load", StringComparison.OrdinalIgnoreCase));
                            int dirCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Dir", StringComparison.OrdinalIgnoreCase) || f.Equals("Direction", StringComparison.OrdinalIgnoreCase) || f.Equals("CoordSys", StringComparison.OrdinalIgnoreCase));

                            if (nameCol >= 0 && patCol >= 0 && valCol >= 0)
                            {
                                var setsDict = new Dictionary<string, List<ShellLoadSetDetail>>(StringComparer.OrdinalIgnoreCase);

                                for (int r = 0; r < numRecords; r++)
                                {
                                    int offset = r * cols;
                                    string sName = (offset + nameCol < tableData.Length) ? tableData[offset + nameCol]?.Trim() ?? "" : "";
                                    string sPat = (offset + patCol < tableData.Length) ? tableData[offset + patCol]?.Trim() ?? "" : "";
                                    string sValStr = (offset + valCol < tableData.Length) ? tableData[offset + valCol]?.Trim() ?? "0" : "0";
                                    string sDir = (dirCol >= 0 && offset + dirCol < tableData.Length) ? tableData[offset + dirCol]?.Trim() ?? "Gravity" : "Gravity";

                                    if (string.IsNullOrEmpty(sName) || string.IsNullOrEmpty(sPat)) continue;

                                    double.TryParse(sValStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double sVal);

                                    if (!setsDict.TryGetValue(sName, out var detailList))
                                    {
                                        detailList = new List<ShellLoadSetDetail>();
                                        setsDict[sName] = detailList;
                                    }

                                    detailList.Add(new ShellLoadSetDetail
                                    {
                                        LoadPattern = sPat,
                                        LoadValue = sVal,
                                        Direction = string.IsNullOrEmpty(sDir) ? "Gravity" : sDir
                                    });
                                }

                                if (setsDict.Count > 0)
                                {
                                    lock (_storedLoadSets)
                                    {
                                        _storedLoadSets.Clear();
                                        foreach (var kvp in setsDict)
                                        {
                                            _storedLoadSets.Add(new ShellLoadSetItem
                                            {
                                                LoadSetName = kvp.Key,
                                                OriginalName = kvp.Key,
                                                Loads = kvp.Value
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi truy xuất Uniform Load Sets từ ETABS DatabaseTables: {ex.Message}");
                }
            }

            var result = new List<ShellLoadSetItem>();
            lock (_storedLoadSets)
            {
                foreach (var ls in _storedLoadSets)
                {
                    var copy = new ShellLoadSetItem
                    {
                        LoadSetName = ls.LoadSetName,
                        Loads = ls.Loads.Select(d => new ShellLoadSetDetail { LoadPattern = d.LoadPattern, LoadValue = d.LoadValue, Direction = d.Direction }).ToList()
                    };
                    result.Add(copy);
                }
            }
            return result;
        }

        /// <summary>
        /// Khai báo hoặc cập nhật Shell Uniform Load Set vào bộ nhớ và ETABS
        /// </summary>
        public bool SetShellUniformLoadSet(string setName, List<ShellLoadSetDetail> loads)
        {
            using (BeginWrite())
            {
            if (string.IsNullOrWhiteSpace(setName)) return false;

            lock (_storedLoadSets)
            {
                var existing = _storedLoadSets.Find(s => s.LoadSetName.Equals(setName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Loads = loads ?? new List<ShellLoadSetDetail>();
                }
                else
                {
                    _storedLoadSets.Add(new ShellLoadSetItem
                    {
                        LoadSetName = setName,
                        Loads = loads ?? new List<ShellLoadSetDetail>()
                    });
                }
            }

            // Ghi trực tiếp toàn bộ danh sách LoadSets vào ETABS
            return SaveShellUniformLoadSetsToEtabs(_storedLoadSets);

            }
        }

        /// <summary>
        /// Lưu toàn bộ danh mục Shell Uniform Load Sets vào cơ sở dữ liệu mô hình ETABS
        /// </summary>
        public bool SaveShellUniformLoadSetsToEtabs(List<ShellLoadSetItem> loadSets)
        {
            using (BeginWrite())
            {
            if (loadSets == null) return false;

            lock (_storedLoadSets)
            {
                _storedLoadSets.Clear();
                foreach (var ls in loadSets)
                {
                    _storedLoadSets.Add(new ShellLoadSetItem
                    {
                        LoadSetName = ls.LoadSetName,
                        OriginalName = ls.LoadSetName,
                        Loads = ls.Loads.Select(d => new ShellLoadSetDetail { LoadPattern = d.LoadPattern, LoadValue = d.LoadValue, Direction = d.Direction }).ToList()
                    });
                    ls.OriginalName = ls.LoadSetName;
                }
            }

            var sapModel = CurrentSapModel;
            if (sapModel == null) return true;

            try
            {
                lock (_etabsLock)
                {
                    string targetTableKey = "Uniform Load Sets";
                    int numTables = 0;
                    string[] tableKeys = Array.Empty<string>();
                    string[] tableNames = Array.Empty<string>();
                    int[] importTypes = Array.Empty<int>();
                    bool[] isEmpty = Array.Empty<bool>();

                    int ret = sapModel.DatabaseTables.GetAllTables(ref numTables, ref tableKeys, ref tableNames, ref importTypes, ref isEmpty);
                    if (ret == 0 && numTables > 0 && tableKeys != null)
                    {
                        foreach (var key in tableKeys)
                        {
                            if (key.Equals("Uniform Load Sets", StringComparison.OrdinalIgnoreCase) ||
                                key.Equals("Shell Uniform Load Sets", StringComparison.OrdinalIgnoreCase) ||
                                (key.IndexOf("Uniform", StringComparison.OrdinalIgnoreCase) >= 0 && key.IndexOf("Set", StringComparison.OrdinalIgnoreCase) >= 0 && key.IndexOf("Assignment", StringComparison.OrdinalIgnoreCase) < 0))
                            {
                                targetTableKey = key;
                                break;
                            }
                        }
                    }

                    int tVer = 0;
                    int nFields = 0;
                    string[] fKeys = Array.Empty<string>();
                    string[] fNames = Array.Empty<string>();
                    string[] fDescs = Array.Empty<string>();
                    string[] fUnits = Array.Empty<string>();
                    bool[] fImportable = Array.Empty<bool>();

                    sapModel.DatabaseTables.GetAllFieldsInTable(targetTableKey, ref tVer, ref nFields, ref fKeys, ref fNames, ref fDescs, ref fUnits, ref fImportable);

                    string[] fieldsKeysIncluded;
                    if (nFields > 0 && fKeys != null && fKeys.Length > 0)
                    {
                        fieldsKeysIncluded = fKeys;
                    }
                    else
                    {
                        fieldsKeysIncluded = new string[] { "Name", "LoadPat", "Dir", "Value" };
                    }

                    int nameCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Name", StringComparison.OrdinalIgnoreCase) || f.Equals("LoadSet", StringComparison.OrdinalIgnoreCase) || f.Equals("UniformLoadSetName", StringComparison.OrdinalIgnoreCase) || f.Equals("SetName", StringComparison.OrdinalIgnoreCase));
                    int patCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("LoadPat", StringComparison.OrdinalIgnoreCase) || f.Equals("LoadPattern", StringComparison.OrdinalIgnoreCase) || f.Equals("Pattern", StringComparison.OrdinalIgnoreCase));
                    int valCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Value", StringComparison.OrdinalIgnoreCase) || f.Equals("LoadValue", StringComparison.OrdinalIgnoreCase) || f.Equals("UnifLoad", StringComparison.OrdinalIgnoreCase) || f.Equals("Load", StringComparison.OrdinalIgnoreCase));
                    int dirCol = Array.FindIndex(fieldsKeysIncluded, f => f.Equals("Dir", StringComparison.OrdinalIgnoreCase) || f.Equals("Direction", StringComparison.OrdinalIgnoreCase) || f.Equals("CoordSys", StringComparison.OrdinalIgnoreCase));

                    if (nameCol < 0) nameCol = 0;
                    if (patCol < 0) patCol = 1;
                    if (dirCol < 0) dirCol = 2;
                    if (valCol < 0) valCol = 3;

                    var tableDataList = new List<string>();
                    int numRecords = 0;

                    foreach (var ls in loadSets)
                    {
                        if (string.IsNullOrWhiteSpace(ls.LoadSetName) || ls.Loads == null || ls.Loads.Count == 0) continue;

                        foreach (var d in ls.Loads)
                        {
                            if (string.IsNullOrWhiteSpace(d.LoadPattern)) continue;

                            string[] row = new string[fieldsKeysIncluded.Length];
                            for (int i = 0; i < row.Length; i++) row[i] = "";

                            if (nameCol < row.Length) row[nameCol] = ls.LoadSetName;
                            if (patCol < row.Length) row[patCol] = d.LoadPattern;
                            if (dirCol < row.Length) row[dirCol] = string.IsNullOrEmpty(d.Direction) ? "Gravity" : d.Direction;
                            if (valCol < row.Length) row[valCol] = d.LoadValue.ToString(CultureInfo.InvariantCulture);

                            tableDataList.AddRange(row);
                            numRecords++;
                        }
                    }

                    string[] tableData = tableDataList.ToArray();
                    int setRet = sapModel.DatabaseTables.SetTableForEditingArray(targetTableKey, ref tVer, ref fieldsKeysIncluded, numRecords, ref tableData);
                    if (setRet == 0)
                    {
                        int nFatal = 0, nErr = 0, nWarn = 0, nInfo = 0;
                        string importLog = "";
                        int applyRet = sapModel.DatabaseTables.ApplyEditedTables(true, ref nFatal, ref nErr, ref nWarn, ref nInfo, ref importLog);
                        Debug.WriteLine($"ApplyEditedTables {targetTableKey}: ret={applyRet}, fatal={nFatal}, err={nErr}, log={importLog}");
                        return nFatal == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lưu LoadSets vào ETABS: {ex.Message}");
            }

            return true;

            }
        }

        /// <summary>
        /// Xóa Shell Uniform Load Set
        /// </summary>
        public bool DeleteShellUniformLoadSet(string setName)
        {
            using (BeginWrite())
            {
            if (string.IsNullOrWhiteSpace(setName)) return false;

            lock (_storedLoadSets)
            {
                _storedLoadSets.RemoveAll(s => s.LoadSetName.Equals(setName, StringComparison.OrdinalIgnoreCase));
            }

            return SaveShellUniformLoadSetsToEtabs(_storedLoadSets);

            }
        }

        /// <summary>
        /// Gán LoadSet cho toàn bộ ô sàn thuộc tiết diện chỉ định tại tầng
        /// </summary>
        public int AssignShellLoadSet(string storyName, string sectionName, string loadSetName)
        {
            using (BeginWrite())
            {
            var sapModel = CurrentSapModel;
            if (sapModel == null || string.IsNullOrEmpty(storyName) || string.IsNullOrEmpty(sectionName)) return 0;

            ShellLoadSetItem? targetSet = null;
            lock (_storedLoadSets)
            {
                targetSet = _storedLoadSets.Find(s => s.LoadSetName.Equals(loadSetName, StringComparison.OrdinalIgnoreCase));
            }
            if (targetSet == null || targetSet.Loads.Count == 0) return 0;

            int count = 0;
            try
            {
                lock (_etabsLock)
                {
                    var slabs = GetSlabsForStory(storyName);
                    var targetSlabs = slabs.Where(s => s.Section.Equals(sectionName, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (targetSet.Loads.Count == 0 || targetSet.Loads.Select(x => x.LoadPattern).Distinct(StringComparer.OrdinalIgnoreCase).Count() != targetSet.Loads.Count)
                        throw new InvalidOperationException("Load set rỗng hoặc chứa pattern trùng.");
                    foreach (var load in targetSet.Loads)
                        if (!LoadTextParser.Finite(load.LoadValue) || !EnsureLoadPatternExists(load.LoadPattern))
                            throw new InvalidOperationException("Load set chứa giá trị/pattern không hợp lệ.");
                    foreach (var slab in targetSlabs)
                    {
                        foreach (var load in targetSet.Loads)
                        {
                            if (sapModel.AreaObj.SetLoadUniform(slab.Name, load.LoadPattern, load.LoadValue, 10, true, "Global", eItemType.Objects) != 0) throw new InvalidOperationException("Gán load set thất bại tại sàn " + slab.Name);
                            VerifyUniform(slab.Name, load.LoadPattern, load.LoadValue, 10);
                        }
                        count++;
                    }

                    _sectionAssignedLoadSets[sectionName] = loadSetName;
                    ClearSlabCache();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi AssignShellLoadSet {sectionName}: {ex.Message}");
            }
            return count;

            }
        }

        /// <summary>
        /// Gán tải phân bố đều Shell Uniform trực tiếp cho toàn bộ ô sàn thuộc tiết diện
        /// </summary>
        public int AssignShellUniformLoad(string storyName, string sectionName, string loadPattern, double value, int direction = 10, bool replaceExisting = true)
        {
            using (BeginWrite())
            {
            var sapModel = CurrentSapModel;
            if (sapModel == null || string.IsNullOrEmpty(storyName) || string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(loadPattern)) return 0;

            int count = 0;
            try
            {
                lock (_etabsLock)
                {
                    if (!LoadTextParser.Finite(value) || !EnsureLoadPatternExists(loadPattern)) throw new InvalidOperationException("Giá trị/pattern tải không hợp lệ.");
                    var slabs = GetSlabsForStory(storyName);
                    var targetSlabs = slabs.Where(s => s.Section.Equals(sectionName, StringComparison.OrdinalIgnoreCase)).ToList();

                    foreach (var slab in targetSlabs)
                    {
                        int ret = sapModel.AreaObj.SetLoadUniform(slab.Name, loadPattern, value, direction, replaceExisting, LoadDirections.CoordinateSystem(direction), eItemType.Objects);


                        if (ret != 0) throw new InvalidOperationException("Gán tải thất bại tại sàn " + slab.Name + "; ETABS code " + ret);
                        if (replaceExisting) VerifyUniform(slab.Name, loadPattern, value, direction);
                        count++;
                    }

                    _sectionAssignedLoadSets.Remove(sectionName);
                    ClearSlabCache();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi AssignShellUniformLoad {sectionName}: {ex.Message}");
            }
            return count;

            }
        }

        /// <summary>
        /// Gán tải vùng chữ nhật cục bộ (Rectangle Sub-Area Zone) vào ETABS
        /// </summary>
        public bool AssignZoneRectangleLoad(string storyName, double x1, double y1, double x2, double y2, string loadPattern, double value, int direction = 10, bool replaceExisting = true)
        {
            throw new NotSupportedException("Tạm ngừng tạo sàn ảo từ vùng chữ nhật để tránh thay đổi độ cứng/mô hình. Hãy gán tải vào ô sàn hiện có; chức năng vùng tải cần thiết kế riêng.");
        }

        public int AssignImportedShellLoads(List<CadShellImportItem> items, string defaultPattern, int direction = 10)
        {
            var sapModel = CurrentSapModel;
            if (sapModel == null || items == null || items.Count == 0) return 0;
            lock (_etabsLock)
            {
                using (BeginWrite())
                {
                    var plan = new List<(string area, List<ParsedLoad> loads)>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in items)
                    {
                        if (string.IsNullOrWhiteSpace(item.AreaName) || !seen.Add(item.AreaName))
                            throw new InvalidOperationException("Thiếu hoặc trùng AreaName trong dữ liệu CAD.");
                        List<ParsedLoad> loads;
                        string text = (item.RawText ?? "").Trim();
                        var match = System.Text.RegularExpressions.Regex.Match(text, @"^Set\s*[:=]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var set = _storedLoadSets.Find(x => x.LoadSetName.Equals(match.Groups[1].Value.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (set == null) throw new InvalidOperationException("Không tìm thấy load set: " + text);
                            loads = set.Loads.Select(x => new ParsedLoad { Pattern = x.LoadPattern, Value = x.LoadValue }).ToList();
                        }
                        else loads = LoadTextParser.Parse(text, defaultPattern);
                        if (loads.Count == 0 || loads.Select(x => x.Pattern).Distinct(StringComparer.OrdinalIgnoreCase).Count() != loads.Count) throw new InvalidOperationException("Load set rỗng hoặc có pattern trùng.");
                        foreach (var load in loads)
                            if (!LoadTextParser.Finite(load.Value) || !EnsureLoadPatternExists(load.Pattern))
                                throw new InvalidOperationException("Giá trị hoặc pattern không hợp lệ: " + load.Pattern);
                        // Validate actual object existence without inferring an object from display text.
                        int points = 0; string[] pointNames = Array.Empty<string>();
                        if (sapModel.AreaObj.GetPoints(item.AreaName, ref points, ref pointNames) != 0)
                            throw new InvalidOperationException("Không tồn tại sàn: " + item.AreaName);
                        plan.Add((item.AreaName, loads));
                    }
                    int updated = 0;
                    foreach (var row in plan)
                    {
                        foreach (var load in row.loads)
                        {
                            int result = sapModel.AreaObj.SetLoadUniform(row.area, load.Pattern, load.Value, direction, true, LoadDirections.CoordinateSystem(direction), eItemType.Objects);
                            if (result != 0) throw new InvalidOperationException($"Gán tải lỗi {result} tại {row.area}/{load.Pattern}; đã hoàn tất {updated}/{plan.Count} sàn. Kiểm tra bản sao lưu trước khi chạy lại.");
                            VerifyUniform(row.area, load.Pattern, load.Value, direction);
                        }
                        updated++;
                    }
                    ClearSlabCache(); sapModel.View.RefreshView(0, false); return updated;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Model cấu trúc của cấu kiện dầm ngang trong ETABS
    /// </summary>
    public class EtabsBeam
    {
        public string Name { get; set; } = string.Empty;
        public string Point1 { get; set; } = string.Empty;
        public string Point2 { get; set; } = string.Empty;
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double Z1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Z2 { get; set; }
        public string Section { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model cấu trúc thông tin tầng trong ETABS
    /// </summary>
    public class EtabsStoryItem
    {
        public string Name { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Elevation { get; set; }
    }
}
