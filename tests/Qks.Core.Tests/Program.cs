using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using Qks.Shared;

internal static partial class Program
{
    private static int passed, failed;
    private static void Check(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    }
    private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    public static int Main(string[] args)
    {
        string root = args.Length == 0 ? Directory.GetCurrentDirectory() : Path.GetFullPath(args[0]);
        if (args.Length == 3 && args[1] == "--benchmark") return Benchmarks(args[2]);
        Check("negative named load", () => { var v = LoadTextParser.Parse("SDL=-2.5 kN/m²", "D").Single(); Assert(v.Pattern == "SDL" && v.Value == -2.5); });
        Check("decimal comma", () => Assert(LoadTextParser.Parse("Tải tường = +2,75", "D").Single().Value == 2.75));
        Check("scientific notation", () => Assert(LoadTextParser.Parse("W-X=-1.5e2", "D").Single().Value == -150));
        Check("MText paragraph", () => Assert(LoadTextParser.Parse("SDL=2\\PLL=3", "D").Count == 2));
        Check("explicit zero", () => Assert(LoadTextParser.Parse("0", "SDL").Single().Value == 0));
        foreach (string value in new[] { "", "abc 2", "NaN", "Infinity", "2 3", "SDL=2 junk", "SDL=1.2.3", "SDL=2\nSDL=3", "1e999", "{\\H2;2}" })
        { string input = value; Check("reject ambiguous input " + value, () => Throws<FormatException>(() => LoadTextParser.Parse(input, "SDL"))); }
        Check("local/global mapping", () => Assert(LoadDirections.Code("Local-1") == 1 && LoadDirections.Code("Global-X") == 4 && LoadDirections.Code("Global-Z") == 6 && LoadDirections.Code("Gravity") == 10));
        Check("coordinate system", () => Assert(LoadDirections.CoordinateSystem(1) == "Local" && LoadDirections.CoordinateSystem(10) == "Global"));
        Check("unknown direction rejected", () => Throws<ArgumentException>(() => LoadDirections.Code("typo")));
        Check("invalid direction code rejected", () => Throws<ArgumentOutOfRangeException>(() => LoadDirections.CoordinateSystem(12)));
        Check("quoted Unicode argument", () => Assert(Protocol.Tokenize("QKS_WALL_SELECT_SECTION \"Dầm 200x400\"").Last() == "Dầm 200x400"));
        Check("broken quotes rejected", () => Throws<InvalidDataException>(() => Protocol.Tokenize("\"unclosed")));
        Check("path traversal rejected", () => Assert(!Protocol.IsInside(Path.Combine(Protocol.Root, "..", "escape.json"), Protocol.Root)));
        Check("prefix sibling rejected", () => Assert(!Protocol.IsInside(Protocol.Root + "-other/file", Protocol.Root)));
        var request = new CadRequest { DocumentPath = Path.Combine(root, "test.dwg"), ModelId = "model", ExportId = Guid.NewGuid().ToString("N"), ResponsePath = Path.Combine(Protocol.Root, Guid.NewGuid().ToString("N"), "response.json") };
        Check("valid request", () => Protocol.Validate(request));
        Func<CadResponse> response = () => new CadResponse { RequestId = request.RequestId, DocumentPath = request.DocumentPath, ExportId = request.ExportId, Status = "succeeded" };
        Check("matched response", () => Protocol.Validate(request, response()));
        Check("stale response rejected", () => { var r = response(); r.RequestId = Guid.NewGuid().ToString("N"); Throws<InvalidDataException>(() => Protocol.Validate(request, r)); });
        Check("wrong drawing rejected", () => { var r = response(); r.DocumentPath += ".other"; Throws<InvalidDataException>(() => Protocol.Validate(request, r)); });
        Check("wrong export rejected", () => { var r = response(); r.ExportId = Guid.NewGuid().ToString("N"); Throws<InvalidDataException>(() => Protocol.Validate(request, r)); });
        Check("canceled response rejected", () => { var r = response(); r.Status = "canceled"; Throws<InvalidOperationException>(() => Protocol.Validate(request, r)); });
        Check("future protocol rejected", () => { var r = JsonCodec.Deserialize<CadRequest>(JsonCodec.Serialize(request)); r.ProtocolVersion = 2; Throws<InvalidDataException>(() => Protocol.Validate(r)); });
        Check("JSON response round trip", () => { var r = response(); r.Error = "C:\\đường dẫn\\file \"x\""; var copy = JsonCodec.Deserialize<CadResponse>(JsonCodec.Serialize(r)); Assert(copy.Error == r.Error); });
        Check("atomic write never overwrites a response", () => {
            string dir = Path.Combine(Path.GetTempPath(), "qks-tests-" + Guid.NewGuid().ToString("N"));
            try { string file = Path.Combine(dir, "r.json"); JsonCodec.WriteAtomic(file, "{\"a\":1}"); Throws<IOException>(() => JsonCodec.WriteAtomic(file, "{\"a\":2}")); Assert(JsonCodec.Read(file).Contains(":1")); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        });
        foreach (string file in Directory.GetFiles(Path.Combine(root, "Loads"), "*.json"))
            Check("fixture round trip " + Path.GetFileName(file), () => { var token = JsonCodec.Deserialize<JToken>(JsonCodec.Read(file)); Assert(JToken.DeepEquals(token, JsonCodec.Deserialize<JToken>(JsonCodec.Serialize(token)))); });
        Check("all source files parse", () => {
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Where(p => !p.Split(Path.DirectorySeparatorChar).Any(s => s == "obj" || s == "artifacts" || s == "Loads"));
            foreach (var file in files) {
                var errors = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                if (errors.Count != 0) throw new Exception(string.Join("\n", errors));
            }
        });
        ViewerTestRoot = root;
        ViewerTests();
        DraftTests();
        LayoutTests();
        ApplyTests();
        WindTests();
        InteractionTests();
        StoryWindTests();
        Console.WriteLine($"Completed: {passed} passed, {failed} failed; host integration tests are separate.");
        return failed == 0 ? 0 : 1;
    }
}
