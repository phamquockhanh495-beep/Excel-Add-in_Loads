using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Services.Modeling
{
    public sealed class EtabsWindTableHost : IWindUserTableHost
    {
        private readonly EtabsLoadApplyHost model;
        private readonly cSapModel sap;
        private readonly int units;
        private readonly string forceName,lengthName;
        private readonly UnitScale scale;
        public ILoadApplyHost Model=>model;
        public EtabsWindTableHost(Action<string> progress)
        {
            model=new EtabsLoadApplyHost(progress);sap=AppConnectionManager.GetSapModel()??throw new InvalidOperationException("Chưa kết nối ETABS.");
            units=(int)sap.GetPresentUnits();eForce f=default;eLength l=default;eTemperature t=default;Ok(sap.GetPresentUnits_2(ref f,ref l,ref t),"GetPresentUnits");
            forceName=f.ToString();lengthName=l.ToString();scale=new UnitScale(forceName,lengthName);
        }
        private static void Ok(int rc,string op){if(rc!=0)throw new InvalidOperationException(op+" trả mã "+rc);}
        private void Guard()
        {
            model.Guard();eForce f=default;eLength l=default;eTemperature t=default;Ok(sap.GetPresentUnits_2(ref f,ref l,ref t),"GetPresentUnits");
            if((int)sap.GetPresentUnits()!=units||f.ToString()!=forceName||l.ToString()!=lengthName)throw new InvalidOperationException("Đơn vị ETABS đã đổi; đóng bảng và đọc lại.");
        }
        public string[] Candidates()
        {
            Guard();int n=0;string[] keys=Array.Empty<string>(),names=Array.Empty<string>();int[] types=Array.Empty<int>();bool[] empty=Array.Empty<bool>();
            Ok(sap.DatabaseTables.GetAllTables(ref n,ref keys,ref names,ref types,ref empty),"GetAllTables");
            if(n<0||keys==null||keys.Length<n)throw new InvalidOperationException("Danh sách bảng không hợp lệ.");
            return keys.Take(n).Where(k=>k.IndexOf("Wind",StringComparison.OrdinalIgnoreCase)>=0&&k.IndexOf("User",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        }
        public void ValidatePattern(string pattern)
        {
            Guard();eLoadPatternType type=default;string code="";Ok(sap.LoadPatterns.GetLoadType(pattern,ref type),"GetLoadType");Ok(sap.LoadPatterns.GetAutoWindCode(pattern,ref code),"GetAutoWindCode");
            if(type!=eLoadPatternType.Wind||!code.Trim().Equals("User Loads",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Chọn pattern Wind / User Loads trong ETABS.");
        }
        public WindTableSnapshot Read(string key)
        {
            Guard();if(!Candidates().Contains(key,StringComparer.Ordinal))throw new InvalidOperationException("Bảng không thuộc User Wind của host hiện tại.");
            int v=0,n=0;string[] all=Array.Empty<string>(),labels=Array.Empty<string>(),desc=Array.Empty<string>(),u=Array.Empty<string>();bool[] edit=Array.Empty<bool>();
            Ok(sap.DatabaseTables.GetAllFieldsInTable(key,ref v,ref n,ref all,ref labels,ref desc,ref u,ref edit),"GetAllFieldsInTable");
            if(n<0||new Array[]{all,u,edit}.Any(a=>a==null||a.Length<n))throw new InvalidOperationException("Metadata bảng không hợp lệ.");
            int count=0;string[] included=Array.Empty<string>(),data=Array.Empty<string>();
            Ok(sap.DatabaseTables.GetTableForEditingArray(key,"",ref v,ref included,ref count,ref data),"GetTableForEditingArray");
            if(count>100000)throw new InvalidOperationException("Bảng vượt 100.000 hàng.");var table=new LoadTable(included,count,data);var cols=included.Select(k=>Array.IndexOf(all,k)).ToArray();
            if(cols.Any(i=>i<0))throw new InvalidOperationException("Field trả về không có metadata.");var unitsByColumn=cols.Select(i=>u[i]).ToArray();var schema=new WindTableSchema(included);
            void Unit(int index,string expected){if(WindTableSchema.Normalize(unitsByColumn[index])!=WindTableSchema.Normalize(expected))throw new InvalidOperationException("Đơn vị field "+included[index]+" chưa xác minh: "+unitsByColumn[index]+"; cần "+expected);}
            Unit(schema.Fx,forceName);Unit(schema.Fy,forceName);Unit(schema.Mz,forceName+lengthName);Unit(schema.X,lengthName);Unit(schema.Y,lengthName);Guard();
            return new WindTableSnapshot(key,v,included,unitsByColumn,cols.Select(i=>edit[i]).ToArray(),table.Rows,units,scale.ForceToKn,scale.LengthToMetres);
        }
        public async Task<IReadOnlyDictionary<string,string[]>> ReadAssignments(IEnumerable<string> stories,Action<string> progress,CancellationToken token)
        {
            Guard();int n=0;string[] definitions=Array.Empty<string>();Ok(sap.Diaphragm.GetNameList(ref n,ref definitions),"Diaphragm.GetNameList");
            if(n<0||definitions==null||definitions.Length<n)throw new InvalidOperationException("Danh sách diaphragm không hợp lệ.");var known=new HashSet<string>(definitions.Take(n),StringComparer.Ordinal);var result=new Dictionary<string,string[]>(StringComparer.Ordinal);
            foreach(string story in stories.Distinct(StringComparer.Ordinal)){
                token.ThrowIfCancellationRequested();Guard();var assigned=new HashSet<string>(StringComparer.Ordinal);int na=0,np=0;string[] areas=Array.Empty<string>(),points=Array.Empty<string>();
                Ok(sap.AreaObj.GetNameListOnStory(story,ref na,ref areas),"Area names / "+story);Ok(sap.PointObj.GetNameListOnStory(story,ref np,ref points),"Point names / "+story);
                if(na<0||np<0||areas==null||areas.Length<na||points==null||points.Length<np)throw new InvalidOperationException("Danh sách đối tượng tầng không hợp lệ.");
                foreach(var area in areas.Take(na)){token.ThrowIfCancellationRequested();string d="";Ok(sap.AreaObj.GetDiaphragm(area,ref d),"Area diaphragm");if(known.Contains(d))assigned.Add(d);}
                foreach(var point in points.Take(np)){token.ThrowIfCancellationRequested();string d="";eDiaphragmOption option=default;Ok(sap.PointObj.GetDiaphragm(point,ref option,ref d),"Point diaphragm");if(known.Contains(d))assigned.Add(d);}
                result.Add(story,assigned.OrderBy(x=>x,StringComparer.Ordinal).ToArray());progress("Đã xác minh diaphragm tầng "+story);await Dispatcher.Yield(DispatcherPriority.Background);Guard();
            }
            return result;
        }
        public WindTableWriteResult Write(WindTableSnapshot table,bool replaceEditingSession)
        {
            Guard();if(model.IsLocked||table.HostUnits!=units||!replaceEditingSession)throw new InvalidOperationException("Model khóa, đơn vị đổi hoặc chưa xác nhận phiên chỉnh sửa bảng.");
            // CSI applies its global edit buffer, not a single-table transaction. The UI explicitly
            // requires consent to discard other unapplied table edits before starting this session.
            Ok(sap.DatabaseTables.CancelTableEditing(),"Bắt đầu phiên QKS / CancelTableEditing");
            var result=new WindTableWriteResult();
            try{
                int version=table.Version;var fields=table.Fields.ToArray();var data=table.Rows.SelectMany(r=>r).ToArray();
                Ok(sap.DatabaseTables.SetTableForEditingArray(table.Key,ref version,ref fields,table.Rows.Length,ref data),"SetTableForEditingArray");Guard();if(model.IsLocked)throw new InvalidOperationException("Model vừa bị khóa; chưa Apply bảng.");
                int fatal=0,errors=0,warnings=0,info=0;string log="";
                result.ReturnCode=sap.DatabaseTables.ApplyEditedTables(true,ref fatal,ref errors,ref warnings,ref info,ref log);
                result.FatalErrors=fatal;result.Errors=errors;result.Warnings=warnings;result.Log=log;Guard();return result;
            }finally{int rc=sap.DatabaseTables.CancelTableEditing();if(rc!=0){result.Warnings++;result.Log+="\nCancelTableEditing trả mã "+rc;}}
        }
        public async Task ValidateAssignments(WindTableSnapshot table,string pattern,CancellationToken token)
        {
            var rows=table.PatternRows(pattern);var allowed=await ReadAssignments(rows.Select(r=>r.Story),_=>{},token);
            if(rows.Any(r=>!allowed.TryGetValue(r.Story,out var names)||!names.Contains(r.Diaphragm,StringComparer.Ordinal)))throw new InvalidOperationException("Gán diaphragm trong model đã đổi; đọc lại danh sách tầng/diaphragm.");
        }
        public void Journal(WindTableReport report)=>model.SaveWindTableJournal(report);
    }
}
