#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qks.Shared;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class WindTableSchema
    {
        public int Pattern {get;} public int Story {get;} public int Diaphragm {get;} public int Set {get;}
        public int Fx {get;} public int Fy {get;} public int Mz {get;} public int X {get;} public int Y {get;}
        public int Count {get;} public int Reversible {get;}
        public int[] Known=>new[]{Pattern,Story,Diaphragm,Set,Fx,Fy,Mz,X,Y,Count,Reversible}.Where(i=>i>=0).ToArray();
        public bool HasSettings=>Count>=0&&Reversible>=0;
        public WindTableSchema(string[] fields)
        {
            if(fields.Length==0||fields.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=fields.Length)throw new ArgumentException("Schema trống hoặc trùng field.");
            int Find(bool required,params string[] aliases){var indexes=Enumerable.Range(0,fields.Length).Where(i=>aliases.Contains(Normalize(fields[i]),StringComparer.Ordinal)).ToArray();if(indexes.Length>1||required&&indexes.Length==0)throw new ArgumentException("Schema User Wind thiếu/mơ hồ: "+aliases[0]);return indexes.Length==0?-1:indexes[0];}
            Pattern=Find(true,"name","loadpat","loadpattern","pattern");Story=Find(true,"story","storyname");Diaphragm=Find(true,"diaph","diaphragm","diaphragmname");Set=Find(true,"loadset","loadsetnumber","loadsetnum","setnumber","setnum");
            Fx=Find(true,"fx");Fy=Find(true,"fy");Mz=Find(true,"mz");X=Find(true,"x","xord","xordinate","xcoord");Y=Find(true,"y","yord","yordinate","ycoord");
            Count=Find(false,"numsets","numloadsets","numberofloadsets");Reversible=Find(false,"reversible","revcomb","reversibleforcombos","loadsarereversibleforcombos");
        }
        public static string Normalize(string value)=>new string((value??"").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }
    public sealed class WindSetRow
    {
        public int LoadSet {get;set;}=1;
        public string Story {get;set;}="";
        public string Diaphragm {get;set;}="";
        public string Fx {get;set;}="0";
        public string Fy {get;set;}="0";
        public string Mz {get;set;}="0";
        public string X {get;set;}="0";
        public string Y {get;set;}="0";
        public string Key=>LoadSet+"\0"+Story+"\0"+Diaphragm;
        public WindSetRow Copy()=>new WindSetRow{LoadSet=LoadSet,Story=Story,Diaphragm=Diaphragm,Fx=Fx,Fy=Fy,Mz=Mz,X=X,Y=Y};
    }
    public sealed class WindTableSnapshot
    {
        public string Key {get;} public int Version {get;} public string[] Fields {get;} public string[] Units {get;} public bool[] Importable {get;} public string[][] Rows {get;}
        public int HostUnits {get;} public double ForceToKn {get;} public double LengthToM {get;}
        private readonly WindTableSchema schema;
        [Newtonsoft.Json.JsonIgnore]
        public WindTableSchema Schema=>schema;
        [Newtonsoft.Json.JsonIgnore]
        public string Fingerprint=>SnapshotFingerprint.Hash(JsonCodec.Serialize(new{Key,Version,Fields,Units,Importable,Rows,HostUnits,ForceToKn,LengthToM}));
        public WindTableSnapshot(string key,int version,string[] fields,string[] units,bool[] importable,string[][] rows,int hostUnits,double forceToKn,double lengthToM)
        {
            if(string.IsNullOrWhiteSpace(key)||fields.Length!=units.Length||fields.Length!=importable.Length||rows.Any(r=>r.Length!=fields.Length)||!Number.Finite(forceToKn)||forceToKn<=0||!Number.Finite(lengthToM)||lengthToM<=0)throw new ArgumentException("Bảng User Wind hoặc đơn vị không hợp lệ.");
            Key=key;Version=version;Fields=fields.ToArray();Units=units.ToArray();Importable=importable.ToArray();Rows=rows.Select(r=>r.ToArray()).ToArray();HostUnits=hostUnits;ForceToKn=forceToKn;LengthToM=lengthToM;
            schema=new WindTableSchema(Fields);var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in Rows){if(string.IsNullOrWhiteSpace(row[schema.Pattern])||string.IsNullOrWhiteSpace(row[schema.Story])||string.IsNullOrWhiteSpace(row[schema.Diaphragm])||!ids.Add(Identity(row)))throw new ArgumentException("Bảng thiếu identity hoặc trùng Pattern/Set/Story/Diaphragm.");ParseRow(row);}
        }
        public string Identity(string[] row){var c=Schema;return row[c.Pattern]+"\0"+Positive(row[c.Set],"Load Set")+"\0"+row[c.Story]+"\0"+row[c.Diaphragm];}
        public static int Positive(string value,string field){if(!int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out int n)||n<1||n>1000)throw new ArgumentException(field+" cần số nguyên từ 1 đến 1000.");return n;}
        public static bool Bool(string value){if(string.Equals(value,"Yes",StringComparison.OrdinalIgnoreCase)||string.Equals(value,"True",StringComparison.OrdinalIgnoreCase)||value=="1")return true;if(string.Equals(value,"No",StringComparison.OrdinalIgnoreCase)||string.Equals(value,"False",StringComparison.OrdinalIgnoreCase)||value=="0")return false;throw new ArgumentException("Giá trị Reversible chưa nhận diện: "+value);}
        public WindSetRow ParseRow(string[] row)
        {
            var c=Schema;string F(int col,double factor){double value=StoryWindPlan.NumberValue(row[col],Fields[col])*factor;if(!Number.Finite(value))throw new ArgumentException("Giá trị vượt phạm vi số: "+Fields[col]);return value.ToString("R",CultureInfo.InvariantCulture);}
            return new WindSetRow{LoadSet=Positive(row[c.Set],"Load Set"),Story=row[c.Story],Diaphragm=row[c.Diaphragm],Fx=F(c.Fx,ForceToKn),Fy=F(c.Fy,ForceToKn),Mz=F(c.Mz,ForceToKn*LengthToM),X=F(c.X,LengthToM),Y=F(c.Y,LengthToM)};
        }
        public WindSetRow[] PatternRows(string pattern)=>Rows.Where(r=>r[Schema.Pattern]==pattern).Select(ParseRow).ToArray();
        public int SetCount(string pattern)
        {
            var c=Schema;var rows=Rows.Where(r=>r[c.Pattern]==pattern).ToArray();if(rows.Length==0)throw new ArgumentException("Pattern chưa có hàng trong bảng User Wind; khởi tạo trong ETABS trước.");
            var counts=c.Count>=0?rows.Select(r=>Positive(r[c.Count],"Number of Load Sets")).Distinct().ToArray():new[]{rows.Max(r=>Positive(r[c.Set],"Load Set"))};
            if(counts.Length!=1||rows.Any(r=>Positive(r[c.Set],"Load Set")>counts[0]))throw new ArgumentException("Số Load Sets không nhất quán.");return counts[0];
        }
        public bool ReversibleValue(string pattern)
        {
            var c=Schema;if(c.Reversible<0)throw new ArgumentException("Schema chưa có Reversible.");var values=Rows.Where(r=>r[c.Pattern]==pattern).Select(r=>Bool(r[c.Reversible])).Distinct().ToArray();if(values.Length!=1)throw new ArgumentException("Reversible không nhất quán.");return values[0];
        }
        [Newtonsoft.Json.JsonIgnore]
        public bool CanAdd=>Schema.Known.Length==Fields.Length&&Importable.All(v=>v);
        public WindTableSnapshot ReplacePattern(string pattern,IEnumerable<WindSetRow> edits,int sets,bool reversible,IReadOnlyDictionary<string,string[]> allowed)
        {
            var c=Schema;var rows=edits.Select(r=>r.Copy()).ToArray();
            if(!Importable.All(v=>v))throw new ArgumentException("Bảng có field không importable; chưa mở ghi để tránh mất dữ liệu.");
            if(rows.Length==0||rows.Length>10000||sets<1||sets>1000)throw new ArgumentException("Cần 1–10.000 dòng và 1–1000 Load Sets.");
            if(rows.Select(r=>r.Key).Distinct(StringComparer.Ordinal).Count()!=rows.Length)throw new ArgumentException("Trùng Set / Story / Diaphragm.");
            if(rows.Any(r=>r.LoadSet<1||r.LoadSet>sets||!allowed.TryGetValue(r.Story,out var names)||!names.Contains(r.Diaphragm,StringComparer.Ordinal)))throw new ArgumentException("Story/Diaphragm chưa được xác nhận trong model hoặc Load Set ngoài phạm vi.");
            if(!c.HasSettings&&(sets!=SetCount(pattern)||c.Reversible>=0&&reversible!=ReversibleValue(pattern)))throw new ArgumentException("Schema chưa đủ trường để đổi số set/Reversible.");
            var old=Rows.Where(r=>r[c.Pattern]==pattern).ToDictionary(r=>ParseRow(r).Key,StringComparer.Ordinal);
            if(old.Count==0)throw new ArgumentException("Khởi tạo pattern User Loads trong ETABS trước.");
            if(!CanAdd&&rows.Any(r=>!old.ContainsKey(r.Key)))throw new ArgumentException("Có field bổ sung chưa xác minh; chỉ sửa/xóa identity đã đọc, chưa tự tạo hàng mới.");
            var output=new List<string[]>();
            foreach(var edit in rows){
                var row=old.TryGetValue(edit.Key,out var prior)?prior.ToArray():Enumerable.Repeat("",Fields.Length).ToArray();
                row[c.Pattern]=pattern;row[c.Story]=edit.Story;row[c.Diaphragm]=edit.Diaphragm;row[c.Set]=edit.LoadSet.ToString(CultureInfo.InvariantCulture);
                string V(string text,double factor)=>(StoryWindPlan.NumberValue(text,"Giá trị tải")/factor).ToString("R",CultureInfo.InvariantCulture);
                row[c.Fx]=V(edit.Fx,ForceToKn);row[c.Fy]=V(edit.Fy,ForceToKn);row[c.Mz]=V(edit.Mz,ForceToKn*LengthToM);row[c.X]=V(edit.X,LengthToM);row[c.Y]=V(edit.Y,LengthToM);
                if(c.Count>=0)row[c.Count]=sets.ToString(CultureInfo.InvariantCulture);
                if(c.Reversible>=0){string sample=Rows.First(r=>r[c.Pattern]==pattern)[c.Reversible];row[c.Reversible]=sample=="1"||sample=="0"?(reversible?"1":"0"):sample.Equals("true",StringComparison.OrdinalIgnoreCase)||sample.Equals("false",StringComparison.OrdinalIgnoreCase)?(reversible?"True":"False"):(reversible?"Yes":"No");}
                output.Add(row);
            }
            // Preserve every other pattern and every unedited field verbatim.
            output.AddRange(Rows.Where(r=>r[c.Pattern]!=pattern).Select(r=>r.ToArray()));
            return new WindTableSnapshot(Key,Version,Fields,Units,Importable,output.ToArray(),HostUnits,ForceToKn,LengthToM);
        }
        public static bool Equivalent(WindTableSnapshot expected,WindTableSnapshot actual)
        {
            if(expected.Key!=actual.Key||expected.Version!=actual.Version||expected.HostUnits!=actual.HostUnits||!expected.Fields.SequenceEqual(actual.Fields)||!expected.Units.SequenceEqual(actual.Units)||expected.Rows.Length!=actual.Rows.Length)return false;
            var c=expected.Schema;var numeric=new[]{c.Fx,c.Fy,c.Mz,c.X,c.Y,c.Set,c.Count}.Where(i=>i>=0).ToArray();
            var b=actual.Rows.ToDictionary(actual.Identity,StringComparer.Ordinal);
            foreach(var row in expected.Rows){if(!b.TryGetValue(expected.Identity(row),out var other))return false;for(int i=0;i<row.Length;i++){
                if(numeric.Contains(i)){double x=StoryWindPlan.NumberValue(row[i],expected.Fields[i]),y=StoryWindPlan.NumberValue(other[i],actual.Fields[i]);if(Math.Abs(x-y)>1e-8+1e-6*Math.Max(Math.Abs(x),Math.Abs(y)))return false;}
                else if(i==c.Reversible){if(Bool(row[i])!=Bool(other[i]))return false;}
                else if(row[i]!=other[i])return false;
            }}return true;
        }
    }
    public sealed class WindTableChange
    {
        public string Identity {get;} public string ModelFingerprint {get;} public string Pattern {get;}
        public WindTableSnapshot Before {get;} public WindTableSnapshot After {get;}
        public bool ReplaceEditingSession {get;}
        public WindTableChange(string identity,string modelFingerprint,string pattern,WindTableSnapshot before,WindTableSnapshot after,bool replaceEditingSession)
        {Identity=identity;ModelFingerprint=modelFingerprint;Pattern=pattern;Before=before;After=after;ReplaceEditingSession=replaceEditingSession;}
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Pattern)||Before.Key!=After.Key||Before.Version!=After.Version||Before.HostUnits!=After.HostUnits||Before.ForceToKn!=After.ForceToKn||Before.LengthToM!=After.LengthToM||!Before.Fields.SequenceEqual(After.Fields)||!Before.Units.SequenceEqual(After.Units)||!Before.Importable.SequenceEqual(After.Importable)||!Before.Importable.All(v=>v))throw new ArgumentException("Schema/đơn vị/phạm vi bảng không khớp.");
            var c=Before.Schema;
            string Others(WindTableSnapshot t)=>JsonCodec.Serialize(t.Rows.Where(r=>r[c.Pattern]!=Pattern).OrderBy(t.Identity,StringComparer.Ordinal));
            if(Others(Before)!=Others(After))throw new ArgumentException("Không được thay đổi pattern ngoài phạm vi đã chọn.");
            if(Before.PatternRows(Pattern).Length==0||After.PatternRows(Pattern).Length==0)throw new ArgumentException("Pattern phải có ít nhất một hàng trước/sau.");
            int sets=After.SetCount(Pattern);if(c.Reversible>=0)After.ReversibleValue(Pattern);
            if(!c.HasSettings&&(sets!=Before.SetCount(Pattern)||c.Reversible>=0&&After.ReversibleValue(Pattern)!=Before.ReversibleValue(Pattern)))throw new ArgumentException("Schema thiếu trường cấu hình set; giữ nguyên số set và Reversible.");
            if(!Before.CanAdd&&After.PatternRows(Pattern).Any(r=>!Before.PatternRows(Pattern).Any(b=>b.Key==r.Key)))throw new ArgumentException("Schema có field mở rộng; chưa hỗ trợ tạo identity mới.");
            var numeric=new[]{c.Fx,c.Fy,c.Mz,c.X,c.Y,c.Set,c.Count,c.Reversible,c.Story,c.Diaphragm,c.Pattern}.Where(i=>i>=0).ToArray();
            var old=Before.Rows.ToDictionary(Before.Identity,StringComparer.Ordinal);
            foreach(var row in After.Rows.Where(r=>r[c.Pattern]==Pattern))if(old.TryGetValue(After.Identity(row),out var prior))for(int i=0;i<row.Length;i++)if(!numeric.Contains(i)&&row[i]!=prior[i])throw new ArgumentException("Không được thay field chưa xác minh: "+Before.Fields[i]);
        }
    }
    public sealed class WindTableWriteResult
    {
        public int ReturnCode {get;set;} public int FatalErrors {get;set;} public int Errors {get;set;} public int Warnings {get;set;} public string Log {get;set;}="";
    }
    public sealed class WindTableReport
    {
        public string OperationId {get;}=Guid.NewGuid().ToString("N");public DateTime StartedUtc {get;}=DateTime.UtcNow;
        public WindTableChange Change {get;} public string BackupPath {get;set;}="";public string Outcome {get;set;}="BlockedBeforeWrite";public string Error {get;set;}="";public bool MayHaveChanges {get;set;} public WindTableWriteResult? Import {get;set;}
        public WindTableReport(WindTableChange change){Change=change;}
    }
    public interface IWindUserTableHost
    {
        ILoadApplyHost Model {get;}
        WindTableSnapshot Read(string key);
        void ValidatePattern(string pattern);
        Task ValidateAssignments(WindTableSnapshot table,string pattern,CancellationToken token);
        WindTableWriteResult Write(WindTableSnapshot table,bool replaceEditingSession);
        void Journal(WindTableReport report);
    }
    public static class WindTableApply
    {
        public static async Task<WindTableReport> Execute(WindTableChange change,IWindUserTableHost host,CancellationToken token)
        {
            var report=new WindTableReport(change);
            try{
                change.Validate();host.Model.Guard();if(host.Model.IsLocked)throw new InvalidOperationException("Model đang khóa.");host.ValidatePattern(change.Pattern);
                if(!change.ReplaceEditingSession)throw new InvalidOperationException("Chưa xác nhận dùng phiên chỉnh sửa bảng riêng của QKS.");
                if(WindTableSnapshot.Equivalent(change.Before,change.After))throw new InvalidOperationException("Không có thay đổi để ghi.");
                var fresh=await host.Model.ReadSnapshotAsync(token);
                if(fresh.Identity!=change.Identity||SnapshotFingerprint.Of(fresh)!=change.ModelFingerprint)throw new InvalidOperationException("Model đã đổi; đọc lại trước khi gán.");
                if(fresh.Loads.Any(l=>l.Pattern==change.Pattern&&(l.Value1!=0||l.Value2!=0)))throw new InvalidOperationException("Pattern có tải trực tiếp; dùng pattern riêng cho User Loads.");
                await host.ValidateAssignments(change.After,change.Pattern,token);
                if(host.Read(change.Before.Key).Fingerprint!=change.Before.Fingerprint)throw new InvalidOperationException("Bảng User Wind đã đổi sau lần đọc.");
                token.ThrowIfCancellationRequested();report.BackupPath=host.Model.CreateVerifiedBackup();if(string.IsNullOrWhiteSpace(report.BackupPath))throw new InvalidOperationException("Chưa có backup xác minh.");host.Journal(report);
                host.Model.Guard();host.ValidatePattern(change.Pattern);if(host.Model.IsLocked||host.Read(change.Before.Key).Fingerprint!=change.Before.Fingerprint)throw new InvalidOperationException("Model/bảng đổi trong lúc backup.");
                token.ThrowIfCancellationRequested();report.MayHaveChanges=true;host.Journal(report);
                report.Import=host.Write(change.After,change.ReplaceEditingSession);
                if(report.Import.ReturnCode!=0||report.Import.FatalErrors>0||report.Import.Errors>0)throw new InvalidOperationException("ETABS import báo lỗi: "+report.Import.Log);
                host.Model.Guard();if(!WindTableSnapshot.Equivalent(change.After,host.Read(change.After.Key)))throw new InvalidOperationException("Read-back không khớp, gồm cả pattern và field phải giữ nguyên.");
                report.Outcome=report.Import.Warnings>0?"VerifiedWithWarnings":"Succeeded";
            }catch(Exception ex){report.Error=ex.GetBaseException().Message;report.Outcome=report.MayHaveChanges?"StoppedMayHaveChanges":"BlockedBeforeWrite";}
            finally{if(report.BackupPath.Length>0)try{host.Journal(report);}catch(Exception ex){report.Error+=" | Journal: "+ex.Message;report.Outcome="NeedsInspection";}}
            return report;
        }
    }
}
