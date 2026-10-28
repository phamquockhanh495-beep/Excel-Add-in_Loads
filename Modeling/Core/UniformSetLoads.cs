#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class LoadTable
    {
        public string[] Fields { get; }
        public string[][] Rows { get; }
        public LoadTable(string[] fields,int count,string[] data)
        {
            if(count<0||fields==null||data==null||count>0&&fields.Length==0||(long)count*fields.Length!=data.Length)throw new ArgumentException("Mảng bảng tải không hợp lệ.");
            Fields=fields;Rows=Enumerable.Range(0,count).Select(r=>data.Skip(r*fields.Length).Take(fields.Length).ToArray()).ToArray();
        }
        public int Column(params string[] names) { foreach(var name in names){int index=Array.FindIndex(Fields,f=>string.Equals(f,name,StringComparison.OrdinalIgnoreCase));if(index>=0)return index;}return -1; }
        public int Required(params string[] names) { int index=Column(names);if(index<0)throw new ArgumentException("Thiếu cột "+string.Join("/",names));return index; }
    }
    public static class UniformSetLoads
    {
        public static IReadOnlyList<ExistingLoad> Expand(LoadTable assignments,LoadTable definitions,IReadOnlyList<ModelElement> elements,UnitScale units)
        {
            int area=assignments.Required("UniqueName","Area","AreaObject","AreaName","Name"),assignedSet=assignments.Required("LoadSet","UniformLoadSet","SetName","UniformLoadSetName","LoadSetName","UnifLoadSet");
            int set=definitions.Required("Name","LoadSet","UniformLoadSetName","SetName","LoadSetName"),pattern=definitions.Required("LoadPat","LoadPattern","Pattern"),value=definitions.Required("Value","LoadValue","UnifLoad","Load");
            int direction=definitions.Column("Dir","Direction"),system=definitions.Column("CSys","CoordSys","CoordinateSystem");
            var areas=elements.Where(e=>e.Id.StartsWith("Area:",StringComparison.Ordinal)).ToDictionary(e=>e.Name,StringComparer.Ordinal);
            var result=new List<ExistingLoad>();var pairs=new HashSet<string>(StringComparer.Ordinal);
            foreach(var assignment in assignments.Rows) {
                string name=assignment[area].Trim(),setName=assignment[assignedSet].Trim();
                if(!areas.TryGetValue(name,out var element))throw new ArgumentException("Không khớp UniqueName area: "+name);
                if(!pairs.Add(name+"\0"+setName))throw new ArgumentException("Gán Load Set trùng: "+name);
                var rows=definitions.Rows.Where(r=>r[set].Trim()==setName).ToArray();if(rows.Length==0)throw new ArgumentException("Thiếu định nghĩa Load Set: "+setName);
                foreach(var row in rows) {
                    if(!double.TryParse(row[value],NumberStyles.Float,CultureInfo.InvariantCulture,out var q)||!Number.Finite(q))throw new ArgumentException("Giá trị Load Set không hợp lệ: "+setName);
                    string pat=row[pattern].Trim();if(pat.Length==0)throw new ArgumentException("Thiếu pattern: "+setName);
                    // CSI Uniform Load Sets default to Gravity when the schema has no Direction column.
                    int dir=direction<0?10:ParseDirection(row[direction]);string cs=system<0?(dir<=3?"Local":"Global"):row[system].Trim();
                    if(cs.Length==0)throw new ArgumentException("Thiếu hệ trục: "+setName);
                    q*=units.Pressure;
                    result.Add(new ExistingLoad(element.Id,pat,"Area uniform set: "+setName,cs,dir,0,1,q,q,"kN/m²"));
                }
            }
            return result;
        }
        public static int ParseDirection(string value)
        {
            string text=value.Trim().Replace(" ","").Replace("-","").ToUpperInvariant();
            if(int.TryParse(text,out var code)&&code>=1&&code<=11)return code;
            switch(text){case "LOCAL1":return 1;case "LOCAL2":return 2;case "LOCAL3":return 3;case "X":case "GLOBALX":return 4;case "Y":case "GLOBALY":return 5;case "Z":case "GLOBALZ":return 6;case "XPROJ":case "XPROJECTED":return 7;case "YPROJ":case "YPROJECTED":return 8;case "ZPROJ":case "ZPROJECTED":return 9;case "GRAVITY":return 10;case "GRAVITYPROJ":case "GRAVITYPROJECTED":return 11;default:throw new ArgumentException("Hướng tải chưa hỗ trợ: "+value);}
        }
    }
}
