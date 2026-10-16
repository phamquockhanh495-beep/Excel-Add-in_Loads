namespace QksExcelAddIn.Services
{
    public static class JsonHelper
    {
        public static string Serialize<T>(T obj) => Qks.Shared.JsonCodec.Serialize(obj);
        public static T? Deserialize<T>(string json) => Qks.Shared.JsonCodec.Deserialize<T>(json);
    }
}
