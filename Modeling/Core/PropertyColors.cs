using System;
using System.Security.Cryptography;
using System.Text;

namespace QksExcelAddIn.Modeling.Core
{
    public static class PropertyColors
    {
        private static readonly string[] Palette = { "#2563EB", "#7C3AED", "#0F766E", "#C2410C", "#BE185D", "#4338CA", "#0369A1", "#4D7C0F", "#A21CAF", "#B45309", "#475569", "#15803D" };
        // Stable across processes, order of API reads and model refreshes.
        public static string ForProperty(string key)
        {
            using (var sha = SHA256.Create()) {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key ?? ""));
                uint n = (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
                return Palette[n % (uint)Palette.Length];
            }
        }
        public static string ForKind(ElementKind kind)
        {
            switch (kind) {
                case ElementKind.Column: return "#DC2626"; case ElementKind.Wall: return "#7C3AED";
                case ElementKind.Slab: return "#0F766E"; case ElementKind.Opening: return "#64748B";
                case ElementKind.Brace: return "#C2410C"; case ElementKind.Other: return "#475569"; default: return "#2563EB";
            }
        }
    }
}
