#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class StoryWindInput
    {
        public string Target { get; set; }="Beam";
        public string Story { get; set; }="";
        public string Pattern { get; set; }="";
        public string UniqueName { get; set; }="";
        public string DiaphragmKey { get; set; }="";
        public string Direction { get; set; }="Global-X";
        public string Q1 { get; set; }="";
        public string Q2 { get; set; }="";
        public string Fx { get; set; }="";
        public string Fy { get; set; }="";
        public string Mz { get; set; }="";
        public string X { get; set; }="";
        public string Y { get; set; }="";
        public string Action { get; set; }="ReplaceMatching";
        public bool HasInput=>new[]{UniqueName,DiaphragmKey,Q1,Q2,Fx,Fy,Mz,X,Y}.Any(v=>!string.IsNullOrWhiteSpace(v));
    }
    public sealed class StoryWindPlan
    {
        public IReadOnlyList<DraftLoad> DraftLoads { get; }
        public IReadOnlyList<UserWindRow> UserRows { get; }
        public IReadOnlyDictionary<string,string> StoryAssignments { get; }
        public int InputCount { get; }
        private StoryWindPlan(DraftLoad[] drafts,UserWindRow[] user,Dictionary<string,string> stories,int count)
        { DraftLoads=Array.AsReadOnly(drafts);UserRows=Array.AsReadOnly(user);StoryAssignments=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(stories);InputCount=count; }
        public static StoryWindPlan Create(DraftWorkspace workspace,IEnumerable<StoryWindInput> inputs,IEnumerable<string> selectedIds,UserWindState? userBaseline,string basis)
        {
            var rows=inputs.Where(r=>r.HasInput).ToArray();if(rows.Length==0||rows.Length>10000)throw new ArgumentException("Nhập từ 1 đến 10.000 dòng tải gió.");
            var snapshot=workspace.Snapshot;var patterns=new HashSet<string>(snapshot.LoadPatterns,StringComparer.Ordinal);var levels=new HashSet<string>(snapshot.Levels.Select(l=>l.Name),StringComparer.Ordinal);
            var beams=snapshot.Elements.Where(e=>e.Kind==ElementKind.Beam).ToArray();var selected=new HashSet<string>(selectedIds,StringComparer.Ordinal);
            var namedBeams=beams.ToLookup(e=>(e.Story,e.Name));var selectedBeams=beams.Where(e=>selected.Contains(e.Id)).ToLookup(e=>e.Story,StringComparer.Ordinal);
            var knownKeys=new HashSet<string>(userBaseline?.Rows.Select(r=>r.DiaphragmKey)??Array.Empty<string>(),StringComparer.Ordinal);
            var draft=workspace.Loads.ToList();var user=new List<UserWindRow>();var stories=new Dictionary<string,string>(StringComparer.Ordinal);
            var scopes=new HashSet<(string,string,int)>();var userKeys=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<rows.Length;i++) {
                var row=rows[i];string story=(row.Story??"").Trim(),pattern=(row.Pattern??"").Trim();
                if(!levels.Contains(story))throw new ArgumentException("Dòng "+(i+1)+": Story không có trong mô hình: "+story);
                if(!patterns.Contains(pattern))throw new ArgumentException("Dòng "+(i+1)+": Pattern chưa có trong mô hình: "+pattern);
                if(string.Equals(row.Target,"Beam",StringComparison.OrdinalIgnoreCase)) {
                    if(new[]{row.Fx,row.Fy,row.Mz,row.X,row.Y,row.DiaphragmKey}.Any(v=>!string.IsNullOrWhiteSpace(v)))throw new ArgumentException("Dòng dầm chỉ nhận q1/q2 (kN/m), không nhận lực/áp lực tầng.");
                    int direction=row.Direction=="Global-X"?4:row.Direction=="Global-Y"?5:throw new ArgumentException("Gió dầm cần Global-X hoặc Global-Y.");
                    double q1=NumberValue(row.Q1,"q1"),q2=string.IsNullOrWhiteSpace(row.Q2)?q1:NumberValue(row.Q2,"q2");
                    var action=row.Action=="Add"?DraftAction.Add:row.Action=="ReplaceMatching"?DraftAction.ReplaceMatching:throw new ArgumentException("Action cần Add hoặc ReplaceMatching.");
                    var targets=(string.IsNullOrWhiteSpace(row.UniqueName)?selectedBeams[story]:namedBeams[(story,row.UniqueName.Trim())]).ToArray();
                    if(targets.Length==0)throw new ArgumentException("Không có dầm "+row.UniqueName+" thuộc Story "+story+". Nếu để trống UniqueName, phải chọn dầm biên trên view trước.");
                    foreach(var beam in targets) {
                        if(!scopes.Add((beam.Id,pattern,direction)))throw new ArgumentException("Trùng dòng dầm/pattern/hướng trong dữ liệu nhập: "+beam.Name);
                        var old=draft.Where(d=>d.Kind==DraftKind.WindLine&&d.ElementId==beam.Id&&d.Pattern==pattern&&d.Direction==direction).ToArray();
                        draft.RemoveAll(d=>old.Contains(d));
                        draft.Add(new DraftLoad(old.FirstOrDefault()?.Id??Guid.NewGuid().ToString("N"),beam.Id,DraftKind.WindLine,pattern,direction,0,1,q1,q2,action,basis));
                    }
                } else if(string.Equals(row.Target,"UserLoads",StringComparison.OrdinalIgnoreCase)) {
                    if(!string.IsNullOrWhiteSpace(row.Q1)||!string.IsNullOrWhiteSpace(row.Q2)||!string.IsNullOrWhiteSpace(row.UniqueName))throw new ArgumentException("Dòng User Loads nhận Fx/Fy/Mz/X/Y, không nhận q hoặc UniqueName dầm.");
                    string key=(row.DiaphragmKey??"").Trim();
                    if(userBaseline==null||userBaseline.Pattern!=pattern)throw new ArgumentException("Đọc User Loads của pattern "+pattern+" trước. Mỗi lần nhập chỉ sửa một User Loads pattern.");
                    if(!string.Equals(userBaseline.Code,"User Loads",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Pattern chưa có chế độ User Loads.");
                    if(!knownKeys.Contains(key))throw new ArgumentException("Chọn khóa diaphragm đã đọc từ API; không tự ghép tên tầng: "+key);
                    if(!userKeys.Add(key))throw new ArgumentException("Trùng khóa diaphragm trong dữ liệu nhập: "+key);
                    user.Add(new UserWindRow(key,NumberValue(row.Fx,"Fx"),NumberValue(row.Fy,"Fy"),NumberValue(row.Mz,"Mz"),NumberValue(row.X,"X"),NumberValue(row.Y,"Y")));stories.Add(key,story);
                } else throw new ArgumentException("Target cần Beam hoặc UserLoads.");
            }
            // Validate every row and overlap before the caller mutates either editor.
            var candidate=new DraftWorkspace(snapshot,workspace.Fingerprint);candidate.Import(new DraftDocument(1,snapshot.Identity,workspace.Fingerprint,draft));
            return new StoryWindPlan(draft.ToArray(),user.ToArray(),stories,rows.Length);
        }
        public static double NumberValue(string text,string field)
        {
            string value=(text??"").Trim();
            if(!double.TryParse(value.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double result)||!Number.Finite(result))throw new ArgumentException(field+" cần số hữu hạn và đơn vị như heading; không bỏ trống (có thể nhập 0).");
            return result;
        }
    }
    public static class WindInputTable
    {
        private static string Normalize(string value)
        {
            var text=new StringBuilder();foreach(char c in value.ToLowerInvariant().Replace('đ','d').Normalize(NormalizationForm.FormD))if(char.IsLetterOrDigit(c)&&CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)text.Append(c);return text.ToString();
        }
        public static IReadOnlyList<StoryWindInput> Parse(IReadOnlyList<string[]> table,string defaultPattern="")
        {
            int header=-1;Dictionary<string,int>? columns=null;
            for(int r=0;r<Math.Min(20,table.Count);r++) {
                var candidate=new Dictionary<string,int>(StringComparer.Ordinal);
                bool duplicate=false;
                for(int c=0;c<table[r].Length;c++) { string key=Normalize(table[r][c]);if(key.Length>0) { if(candidate.ContainsKey(key))duplicate=true;else candidate.Add(key,c); } }
                if((candidate.ContainsKey("story")||candidate.ContainsKey("tang"))&&candidate.Keys.Any(k=>new[]{"q1","q1knm","qknm","fx","fxkn","fy","fykn"}.Contains(k))) {
                    if(duplicate)throw new ArgumentException("Heading tải gió bị trùng.");header=r;columns=candidate;break;
                }
            }
            if(columns==null)throw new ArgumentException("Không tìm thấy heading Story và q1 hoặc Fx/Fy. Dùng file mẫu WindBeams/WindStories.");
            foreach(var aliases in new[]{new[]{"story","tang"},new[]{"pattern","loadpattern","loadpat"},new[]{"uniquename","uniqueid","beam"},new[]{"diaphragmkey","diaphragm"},new[]{"direction","phuong","huong"},new[]{"target","method"},new[]{"action","thaotac"},new[]{"q1knm","q1","qknm"},new[]{"q2knm","q2"},new[]{"fxkn","fx"},new[]{"fykn","fy"},new[]{"mzknm","mz"},new[]{"xm","x"},new[]{"ym","y"}})
                if(aliases.Count(columns.ContainsKey)>1)throw new ArgumentException("Nhiều cột cùng thuộc tính tải: "+aliases[0]);
            string Get(string[] row,params string[] aliases) { foreach(string alias in aliases)if(columns.TryGetValue(alias,out int index)&&index<row.Length)return row[index].Trim();return ""; }
            var result=new List<StoryWindInput>();
            foreach(var row in table.Skip(header+1)) {
                if(row.All(string.IsNullOrWhiteSpace))continue;
                var input=new StoryWindInput { Story=Get(row,"story","tang"),Pattern=Get(row,"pattern","loadpattern","loadpat"),UniqueName=Get(row,"uniquename","uniqueid","beam"),DiaphragmKey=Get(row,"diaphragmkey","diaphragm"),Q1=Get(row,"q1knm","q1","qknm"),Q2=Get(row,"q2knm","q2"),Fx=Get(row,"fxkn","fx"),Fy=Get(row,"fykn","fy"),Mz=Get(row,"mzknm","mz"),X=Get(row,"xm","x"),Y=Get(row,"ym","y") };
                if(input.Pattern.Length==0)input.Pattern=defaultPattern;
                string target=Get(row,"target","method");
                if(target.Length==0) {
                    bool line=new[]{input.Q1,input.Q2,input.UniqueName}.Any(v=>v.Length>0),force=new[]{input.Fx,input.Fy,input.Mz,input.X,input.Y,input.DiaphragmKey}.Any(v=>v.Length>0);
                    if(line&&force)throw new ArgumentException("Một dòng không được trộn q và Fx/Fy/Mz. Tách Beam và UserLoads.");
                    target=force?"UserLoads":"Beam";
                }
                input.Target=target;
                string direction=Get(row,"direction","phuong","huong");if(direction.Length>0)input.Direction=direction;
                string action=Get(row,"action","thaotac");if(action.Length>0)input.Action=action;
                result.Add(input);if(result.Count>10000)throw new ArgumentException("File vượt 10.000 dòng tải.");
            }
            return result.AsReadOnly();
        }
    }
}
