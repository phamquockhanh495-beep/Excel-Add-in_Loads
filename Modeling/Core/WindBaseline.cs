#nullable enable
using System;
using System.Linq;
using Qks.Shared;
namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Catalog drafts track stories and patterns. Apply still reads a fresh full model,
    /// checks direct-load conflicts and re-reads the exact User Loads / table before writing.</summary>
    public static class WindBaseline
    {
        private const string Prefix="catalog-v1:";
        private static string Catalog(ModelSnapshot s)=>Prefix+SnapshotFingerprint.Hash(JsonCodec.Serialize(new {
            s.Source,s.Version,s.Identity,
            Levels=s.Levels.OrderBy(l=>l.Name,StringComparer.Ordinal),
            Patterns=s.LoadPatterns.OrderBy(p=>p,StringComparer.Ordinal)
        }));
        public static string Of(ModelSnapshot s)=>s.CatalogOnly?Catalog(s):SnapshotFingerprint.Of(s);
        public static bool Matches(ModelSnapshot s,string fingerprint)=>fingerprint.StartsWith(Prefix,StringComparison.Ordinal)?Catalog(s)==fingerprint:!s.CatalogOnly&&SnapshotFingerprint.Of(s)==fingerprint;
    }
}
