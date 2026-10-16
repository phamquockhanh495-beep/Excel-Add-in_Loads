using System;
namespace Qks.Shared
{
    public static class LoadDirections
    {
        public static int Code(string name)
        {
            switch (name?.Trim())
            {
                case "Local-1": return 1;
                case "Local-2": return 2;
                case "Local-3": return 3;
                case "Global-X": return 4;
                case "Global-Y": return 5;
                case "Global-Z": return 6;
                case "Global-X Proj": return 7;
                case "Global-Y Proj": return 8;
                case "Global-Z Proj": return 9;
                case "Gravity": return 10;
                case "Gravity Proj": return 11;
                default: throw new ArgumentException("Unknown load direction: " + name);
            }
        }
        public static string CoordinateSystem(int code)
        {
            if (code < 1 || code > 11) throw new ArgumentOutOfRangeException(nameof(code));
            return code <= 3 ? "Local" : "Global";
        }
    }
}
