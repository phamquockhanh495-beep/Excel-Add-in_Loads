#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Qks.Shared
{
    public static class JsonCodec
    {
        public const int MaxBytes = 32 * 1024 * 1024;
        public static string Serialize(object? value) => JsonConvert.SerializeObject(value,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 64 });
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaxBytes)
                throw new InvalidDataException("JSON empty or exceeds 32 MiB.");
            var result = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.None, MaxDepth = 64,
                FloatParseHandling = FloatParseHandling.Double });
            if (result is null) throw new InvalidDataException("JSON null is not a valid payload.");
            return result;
        }
        public static string Read(string path)
        {
            if (new FileInfo(path).Length > MaxBytes) throw new InvalidDataException("JSON exceeds 32 MiB.");
            return File.ReadAllText(path);
        }
        public static void WriteAtomic(string path, string json)
        {
            if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("JSON exceeds 32 MiB.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temp, json, new UTF8Encoding(false)); File.Move(temp, path); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
    public sealed class CadRequest
    {
        public int ProtocolVersion { get; set; } = 1;
        public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
        public string DocumentPath { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string ExportId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string[] Arguments { get; set; } = new string[0];
        public string ResponsePath { get; set; } = string.Empty;
    }
    public sealed class CadResponse
    {
        public int ProtocolVersion { get; set; } = 1;
        public string RequestId { get; set; } = string.Empty;
        public string DocumentPath { get; set; } = string.Empty;
        public string ExportId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
    }
    public static class Protocol
    {
        public static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QKS", "Exchange");
        public static bool SamePath(string a, string b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) &&
            string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        public static bool IsInside(string path, string directory) => Path.GetFullPath(path).StartsWith(
            Path.GetFullPath(directory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        public static void Validate(CadRequest request)
        {
            Guid id;
            if (request.ProtocolVersion != 1 || !Guid.TryParseExact(request.RequestId, "N", out id) ||
                string.IsNullOrWhiteSpace(request.DocumentPath) || string.IsNullOrWhiteSpace(request.ModelId) ||
                !Guid.TryParseExact(request.ExportId, "N", out id) || !IsInside(request.ResponsePath, Root))
                throw new InvalidDataException("Unsupported protocol, context or response path.");
        }
        public static void Validate(CadRequest request, CadResponse response)
        {
            if (response == null || response.ProtocolVersion != 1 || response.RequestId != request.RequestId ||
                response.ExportId != request.ExportId || !SamePath(response.DocumentPath, request.DocumentPath))
                throw new InvalidDataException("Response does not match the request/document/export.");
            if (response.Status != "succeeded") throw new InvalidOperationException(response.Error ?? response.Status);
        }
        // Parse only the arguments after a known QKS command; never execute user text as CAD commands.
        public static string[] Tokenize(string input)
        {
            var tokens = new List<string>(); var token = new StringBuilder(); bool quoted = false, started = false;
            foreach (char c in input.Trim())
            {
                if (c == '"') { quoted = !quoted; started = true; }
                else if (char.IsWhiteSpace(c) && !quoted) { if (started) { tokens.Add(token.ToString()); token.Clear(); started = false; } }
                else { if (char.IsControl(c)) throw new InvalidDataException("Control character in argument."); token.Append(c); started = true; }
            }
            if (quoted) throw new InvalidDataException("Unclosed quote in argument.");
            if (started) tokens.Add(token.ToString());
            return tokens.ToArray();
        }
    }
}
