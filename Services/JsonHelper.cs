using System;
using System.Web.Script.Serialization;

namespace QksExcelAddIn.Services
{
    /// <summary>
    /// Bộ chuyển đổi JSON độc lập sử dụng JavaScriptSerializer (.NET Framework 4.8)
    /// Loại bỏ hoàn toàn lỗi TypeInitializationException của System.Text.Json trong môi trường Excel-DNA
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer
        {
            MaxJsonLength = int.MaxValue
        };

        public static string Serialize<T>(T obj)
        {
            if (obj == null) return string.Empty;
            return _serializer.Serialize(obj);
        }

        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return _serializer.Deserialize<T>(json);
        }
    }
}
