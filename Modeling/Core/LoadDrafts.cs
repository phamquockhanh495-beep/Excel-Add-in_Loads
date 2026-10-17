#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Qks.Shared;

namespace QksExcelAddIn.Modeling.Core
{
    public enum DraftKind { WallLine, SlabUniform }
    public enum DraftAction { Add, ReplaceMatching }

    public sealed class DraftLoad
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; }
        [JsonProperty(Required = Required.Always)]
        public string ElementId { get; }
        [JsonProperty(Required = Required.Always)]
        public DraftKind Kind { get; }
        [JsonProperty(Required = Required.Always)]
        public string Pattern { get; }
        [JsonProperty(Required = Required.Always)]
        public int Direction { get; }
        [JsonIgnore]
        public string CoordinateSystem => "Global";
        [JsonProperty(Required = Required.Always)]
        public double Start { get; }
        [JsonProperty(Required = Required.Always)]
        public double End { get; }
        [JsonProperty(Required = Required.Always)]
        public double Value1 { get; }
        [JsonProperty(Required = Required.Always)]
        public double Value2 { get; }
        [JsonProperty(Required = Required.Always)]
        public DraftAction Action { get; }
        [JsonIgnore]
        public string Unit => Kind == DraftKind.WallLine ? "kN/m" : "kN/m²";
        [JsonIgnore]
        public string KindName => Kind == DraftKind.WallLine ? "Tường" : "Sàn";
        [JsonIgnore]
        public string ActionName => Action == DraftAction.Add ? "Thêm tải" : "Thay cùng phạm vi";
        [JsonIgnore]
        public string DirectionName => Direction == 10 ? "Gravity" : Direction == 4 ? "Global-X" : Direction == 5 ? "Global-Y" : "Global-Z";
        public DraftLoad(string id, string elementId, DraftKind kind, string pattern, int direction,
            double start, double end, double value1, double value2, DraftAction action)
        {
            Id = id; ElementId = elementId; Kind = kind; Pattern = pattern; Direction = direction;
            Start = start; End = end; Value1 = value1; Value2 = value2; Action = action;
        }
        public static DraftLoad WallFromMetres(ModelElement frame, string pattern, int direction, double startMetres,
            double endMetres, double value1, double value2, DraftAction action, string? id = null)
        {
            double length = frame.Length3D ?? 0;
            if (!Number.Finite(length) || length <= 1e-9) throw new ArgumentException("Dầm không có chiều dài 3D hợp lệ.");
            return new DraftLoad(id ?? Guid.NewGuid().ToString("N"), frame.Id, DraftKind.WallLine, pattern, direction,
                startMetres / length, endMetres / length, value1, value2, action);
        }
        public ExistingLoad AsLoad() => new ExistingLoad(ElementId, Pattern, Kind == DraftKind.WallLine ? "Frame force/length" : "Area uniform",
            CoordinateSystem, Direction, Start, End, Value1, Value2, Unit);
    }

    public sealed class DraftDocument
    {
        [JsonProperty(Required = Required.Always)]
        public int SchemaVersion { get; }
        [JsonProperty(Required = Required.Always)]
        public string SnapshotIdentity { get; }
        [JsonProperty(Required = Required.Always)]
        public string SnapshotFingerprint { get; }
        [JsonProperty(Required = Required.Always)]
        public IReadOnlyList<DraftLoad> Loads { get; }
        public DraftDocument(int schemaVersion, string snapshotIdentity, string snapshotFingerprint, IEnumerable<DraftLoad> loads)
        {
            SchemaVersion = schemaVersion; SnapshotIdentity = snapshotIdentity; SnapshotFingerprint = snapshotFingerprint;
            Loads = new ReadOnlyCollection<DraftLoad>(loads.ToArray());
        }
    }

    public sealed class LoadComparison
    {
        public string Phase { get; }
        public string Origin { get; }
        public ExistingLoad Load { get; }
        public LoadComparison(string phase, string origin, ExistingLoad load) { Phase = phase; Origin = origin; Load = load; }
    }

    public static class SnapshotFingerprint
    {
        public static string Of(ModelSnapshot snapshot)
        {
            // Ignore capture time and API read order. Include source identity, geometry, patterns and the load baseline.
            var canonical = new {
                snapshot.SchemaVersion, snapshot.Source, snapshot.Version, snapshot.Identity,
                Elements = snapshot.Elements.OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => new {
                    e.Id, e.Name, e.Label, e.Story, e.Property, e.Kind, e.LocalAxisDegrees, e.AdvancedLocalAxes,
                    Points = e.Vertices.Select(p => new { p.X, p.Y, p.Z }) }),
                Patterns = snapshot.LoadPatterns.OrderBy(p => p, StringComparer.Ordinal),
                Levels = snapshot.Levels.OrderBy(l => l.Name, StringComparer.Ordinal),
                Loads = snapshot.Loads.Select(JsonCodec.Serialize).OrderBy(s => s, StringComparer.Ordinal)
            };
            return Hash(JsonCodec.Serialize(canonical));
        }
        internal static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>Pure data editor. No CSI dependency or model write operations.</summary>
    public sealed class DraftWorkspace
    {
        public ModelSnapshot Snapshot { get; }
        public string Fingerprint { get; }
        private DraftLoad[] current = Array.Empty<DraftLoad>();
        private readonly Stack<DraftLoad[]> undo = new Stack<DraftLoad[]>(), redo = new Stack<DraftLoad[]>();
        private string savedState;
        public IReadOnlyList<DraftLoad> Loads => Array.AsReadOnly(current);
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;
        public bool IsDirty => State(current) != savedState;
        public DraftWorkspace(ModelSnapshot snapshot)
        {
            snapshot.Validate(); Snapshot = snapshot; Fingerprint = SnapshotFingerprint.Of(snapshot); savedState = State(current);
        }
        private static string State(IEnumerable<DraftLoad> loads) => SnapshotFingerprint.Hash(JsonCodec.Serialize(loads.OrderBy(l => l.Id, StringComparer.Ordinal)));
        public void MarkSaved() => savedState = State(current);
        public DraftDocument Document() => new DraftDocument(1, Snapshot.Identity, Fingerprint, current);
        public void Import(DraftDocument document)
        {
            if (document.SchemaVersion != 1 || document.SnapshotIdentity != Snapshot.Identity || document.SnapshotFingerprint != Fingerprint)
                throw new ArgumentException("Bộ nháp không thuộc snapshot này. Mở đúng snapshot nguồn; chưa hỗ trợ tự rebase.");
            var candidate = document.Loads.ToArray(); Validate(candidate); Commit(candidate); MarkSaved();
        }
        public void Put(DraftLoad load)
        {
            int index = Array.FindIndex(current, l => l.Id == load.Id);
            var candidate = index < 0 ? current.Concat(new[] { load }).ToArray() : current.ToArray();
            if (index >= 0) candidate[index] = load;
            Validate(candidate); Commit(candidate);
        }
        public void Remove(string id)
        {
            var candidate = current.Where(l => l.Id != id).ToArray(); if (candidate.Length != current.Length) Commit(candidate);
        }
        private void Commit(DraftLoad[] candidate)
        {
            if (State(candidate) == State(current)) return;
            undo.Push(current); current = candidate; redo.Clear();
            if (undo.Count > 100) { var keep = undo.Take(100).Reverse().ToArray(); undo.Clear(); foreach (var state in keep) undo.Push(state); }
        }
        public void Undo() { if (CanUndo) { redo.Push(current); current = undo.Pop(); } }
        public void Redo() { if (CanRedo) { undo.Push(current); current = redo.Pop(); } }
        private void Validate(DraftLoad[] loads)
        {
            if (loads.Length > 10000) throw new ArgumentException("Bộ nháp vượt giới hạn 10.000 dòng.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var elements = Snapshot.Elements.ToDictionary(e => e.Id, StringComparer.Ordinal);
            foreach (var l in loads) {
                if (l == null || !Guid.TryParseExact(l.Id, "N", out _) || !ids.Add(l.Id)) throw new ArgumentException("ID dòng nháp thiếu hoặc trùng.");
                if (!Enum.IsDefined(typeof(DraftKind), l.Kind) || !Enum.IsDefined(typeof(DraftAction), l.Action)) throw new ArgumentException("Loại/thao tác nháp chưa hỗ trợ.");
                if (!elements.TryGetValue(l.ElementId, out var e)) throw new ArgumentException("Không có đối tượng nguồn: " + l.ElementId);
                if (!Snapshot.LoadPatterns.Contains(l.Pattern, StringComparer.Ordinal)) throw new ArgumentException("Pattern chưa có trong snapshot: " + l.Pattern);
                if (l.Direction != 4 && l.Direction != 5 && l.Direction != 6 && l.Direction != 10) throw new ArgumentException("Nháp hiện hỗ trợ Global X/Y/Z và Gravity.");
                if (!Number.Finite(l.Start) || !Number.Finite(l.End) || !Number.Finite(l.Value1) || !Number.Finite(l.Value2) || l.Start < 0 || l.End > 1 || l.Start >= l.End)
                    throw new ArgumentException("Đoạn tải/giá trị phải hữu hạn; 0 ≤ r1 < r2 ≤ 1.");
                if (l.Kind == DraftKind.WallLine && (e.Kind != ElementKind.Beam || e.Vertices.Count != 2 || !(e.Length3D > 1e-9)))
                    throw new ArgumentException("Tải tường nháp cần dầm có chiều dài 3D hợp lệ.");
                if (l.Kind == DraftKind.SlabUniform) {
                    if (e.Kind != ElementKind.Slab || e.Vertices.Count < 3 || l.Start != 0 || l.End != 1 || l.Value1 != l.Value2)
                        throw new ArgumentException("Tải sàn nháp cần slab và một giá trị uniform cho toàn area.");
                    Vec3 normal = new Vec3(0,0,0);
                    for (int i=1; i+1<e.Vertices.Count; i++) normal += (e.Vertices[i]-e.Vertices[0]).Cross(e.Vertices[i+1]-e.Vertices[0]);
                    if (!Number.Finite(normal.Length) || normal.Length < 1e-9) throw new ArgumentException("Area suy biến, không thể soạn tải diện.");
                }
            }
            foreach (var group in loads.GroupBy(l => new {l.ElementId,l.Kind,l.Pattern,l.Direction})) {
                var ordered = group.OrderBy(l => l.Start).ToArray();
                if (ordered.Select(l => l.Action).Distinct().Count() > 1) throw new ArgumentException("Cùng phạm vi tải không được trộn Add và ReplaceMatching.");
                for (int i=1; i<ordered.Length; i++)
                    if (ordered[i].Start < ordered[i-1].End) throw new ArgumentException("Các đoạn nháp cùng đối tượng/pattern/hướng bị chồng nhau.");
            }
        }
        public IReadOnlyList<LoadComparison> Compare()
        {
            var rows = Snapshot.Loads.Select(l => new LoadComparison("Trước", "Snapshot", l)).ToList();
            var replacements = current.Where(d => d.Action == DraftAction.ReplaceMatching).Select(d => d.AsLoad())
                .ToLookup(l => new { l.ElementId, l.Pattern, l.Direction, l.Kind, CoordinateSystem = l.CoordinateSystem.ToUpperInvariant() });
            rows.AddRange(Snapshot.Loads.Where(l => !replacements.Contains(new { l.ElementId, l.Pattern, l.Direction, l.Kind, CoordinateSystem = (l.CoordinateSystem ?? "").ToUpperInvariant() })).Select(l => new LoadComparison("Sau", "Giữ từ snapshot", l)));
            rows.AddRange(current.Select(l => new LoadComparison("Sau", l.Action == DraftAction.Add ? "Nháp · thêm" : "Nháp · thay cùng phạm vi", l.AsLoad())));
            return rows.AsReadOnly();
        }
    }
}
