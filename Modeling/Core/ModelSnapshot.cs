#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    public readonly struct Vec3
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        [Newtonsoft.Json.JsonConstructor]
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double k) => new Vec3(a.X * k, a.Y * k, a.Z * k);
        public double Dot(Vec3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public Vec3 Cross(Vec3 b) => new Vec3(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
        public double Length => Math.Sqrt(Dot(this));
        public bool IsFinite => Number.Finite(X) && Number.Finite(Y) && Number.Finite(Z);
    }
    public static class Number
    {
        public static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
    public enum ElementKind { Beam, Column, Brace, Slab, Wall, Opening, Other, Joint }
    public sealed class ModelElement
    {
        public string Id { get; }
        public string Name { get; }
        public string Label { get; }
        public string Story { get; }
        public string Property { get; }
        public ElementKind Kind { get; }
        public IReadOnlyList<Vec3> Vertices { get; }
        public double? LocalAxisDegrees { get; }
        public bool AdvancedLocalAxes { get; }
        public SectionGeometry? Section { get; }
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public IReadOnlyList<Vec3>? LocalAxes { get; }
        public double? Length3D => Vertices.Count == 2 ? (double?)(Vertices[1] - Vertices[0]).Length : null;
        public ModelElement(string id, string name, string label, string story, string property,
            ElementKind kind, IEnumerable<Vec3> vertices, double? localAxisDegrees = null, bool advancedLocalAxes = false, SectionGeometry? section = null, IEnumerable<Vec3>? localAxes = null)
        {
            Id = id; Name = name; Label = label; Story = story; Property = property; Kind = kind;
            Vertices = new ReadOnlyCollection<Vec3>(vertices.ToArray());
            LocalAxisDegrees = localAxisDegrees; AdvancedLocalAxes = advancedLocalAxes; Section = section;
            LocalAxes = localAxes == null ? null : new ReadOnlyCollection<Vec3>(localAxes.ToArray());
        }
    }
    public sealed class ModelLevel
    {
        public string Name { get; }
        public double Elevation { get; }
        public ModelLevel(string name, double elevation) { Name = name; Elevation = elevation; }
        public override string ToString() => Name + " · " + Elevation.ToString("0.###") + " m";
    }
    // Values remain signed. Kind records dimensions; relative distances refer to the TRUE 3D frame.
    public sealed class ExistingLoad
    {
        public string ElementId { get; }
        public string Pattern { get; }
        public string Kind { get; }
        public string CoordinateSystem { get; }
        public int Direction { get; }
        public double Start { get; }
        public double End { get; }
        public double Value1 { get; }
        public double Value2 { get; }
        public string Unit { get; }
        [Newtonsoft.Json.JsonProperty(DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
        public int Step { get; }
        public ExistingLoad(string elementId, string pattern, string kind, string coordinateSystem, int direction,
            double start, double end, double value1, double value2, string unit, int step = 0)
        {
            ElementId = elementId; Pattern = pattern; Kind = kind; CoordinateSystem = coordinateSystem;
            Direction = direction; Start = start; End = end; Value1 = value1; Value2 = value2; Unit = unit; Step = step;
        }
    }
    public sealed class ModelSnapshot
    {
        public int SchemaVersion { get; }
        public string Source { get; }
        public string Version { get; }
        public string Identity { get; }
        public string SourceUnits { get; }
        public DateTime CapturedUtc { get; }
        public IReadOnlyList<ModelElement> Elements { get; }
        public IReadOnlyList<ModelLevel> Levels { get; }
        public IReadOnlyList<ExistingLoad> Loads { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public IReadOnlyList<string> LoadPatterns { get; }
        public ModelSnapshot(int schemaVersion, string source, string version, string identity, string sourceUnits,
            DateTime capturedUtc, IEnumerable<ModelElement> elements, IEnumerable<ModelLevel> levels,
            IEnumerable<ExistingLoad> loads, IEnumerable<string> diagnostics, IEnumerable<string>? loadPatterns = null)
        {
            SchemaVersion = schemaVersion; Source = source; Version = version; Identity = identity;
            SourceUnits = sourceUnits; CapturedUtc = capturedUtc;
            Elements = new ReadOnlyCollection<ModelElement>(elements.ToArray());
            Levels = new ReadOnlyCollection<ModelLevel>(levels.ToArray());
            Loads = new ReadOnlyCollection<ExistingLoad>(loads.ToArray());
            Diagnostics = new ReadOnlyCollection<string>(diagnostics.ToArray());
            LoadPatterns = new ReadOnlyCollection<string>((loadPatterns ?? Loads.Select(l => l.Pattern)).Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToArray());
            Validate();
        }
        public void Validate()
        {
            if (SchemaVersion != 1 || string.IsNullOrWhiteSpace(Identity)) throw new ArgumentException("Snapshot version/identity không hợp lệ.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in Elements)
                if (string.IsNullOrWhiteSpace(e.Id) || !ids.Add(e.Id) || (e.Kind == ElementKind.Joint ? e.Vertices.Count != 1 : e.Vertices.Count < 2) ||
                    e.Vertices.Any(p => !p.IsFinite) || e.LocalAxisDegrees.HasValue && !Number.Finite(e.LocalAxisDegrees.Value) || !Enum.IsDefined(typeof(ElementKind), e.Kind))
                    throw new ArgumentException("Đối tượng snapshot không hợp lệ: " + e.Id);
            if (Levels.Any(l => !Number.Finite(l.Elevation))) throw new ArgumentException("Cao độ không hợp lệ.");
            if (LoadPatterns.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Tên load pattern không hợp lệ.");
            foreach (var l in Loads)
                if (!ids.Contains(l.ElementId) || !Number.Finite(l.Start) || !Number.Finite(l.End) ||
                    !Number.Finite(l.Value1) || !Number.Finite(l.Value2)) throw new ArgumentException("Tải snapshot không hợp lệ.");
        }
    }
    public sealed class UnitScale
    {
        public double LengthToMetres { get; }
        public double ForceToKn { get; }
        public double PointFactor(bool moment) => ForceToKn * (moment ? LengthToMetres : 1);
        public double LineForce => ForceToKn / LengthToMetres;
        public double Pressure => ForceToKn / (LengthToMetres * LengthToMetres);
        public double DistributedFactor(int apiType)
        {
            if (apiType == 1) return LineForce;
            if (apiType == 2) return ForceToKn;
            throw new ArgumentOutOfRangeException(nameof(apiType));
        }
        public UnitScale(string force, string length)
        {
            switch (length) {
                case "m": LengthToMetres = 1; break; case "cm": LengthToMetres = .01; break;
                case "mm": LengthToMetres = .001; break; case "micron": LengthToMetres = .000001; break;
                case "inch": LengthToMetres = .0254; break; case "ft": LengthToMetres = .3048; break;
                default: throw new ArgumentException("Đơn vị chiều dài chưa hỗ trợ: " + length);
            }
            switch (force) {
                case "kN": ForceToKn = 1; break; case "N": ForceToKn = .001; break;
                case "kgf": ForceToKn = .00980665; break; case "tonf": ForceToKn = 9.80665; break;
                case "lb": ForceToKn = .0044482216152605; break; case "kip": ForceToKn = 4.4482216152605; break;
                default: throw new ArgumentException("Đơn vị lực chưa hỗ trợ: " + force);
            }
        }
    }
}
