using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
namespace QksExcelAddIn.Services.Modeling
{
    /// <summary>Read-only capability evidence from the actual connected host, never guessed table writes.</summary>
    public static class WindHostInspection
    {
        public static string[] Patterns()
        {
            var sap=AppConnectionManager.GetSapModel()??throw new InvalidOperationException("Kết nối ETABS trước.");
            int n=0;string[] names=Array.Empty<string>();
            if(sap.LoadPatterns.GetNameList(ref n,ref names)!=0||n<0||names==null||names.Length<n)throw new InvalidOperationException("Không đọc được Load Patterns.");
            var result=new List<string>();
            foreach(var name in names.Take(n)){eLoadPatternType type=default;if(sap.LoadPatterns.GetLoadType(name,ref type)!=0)throw new InvalidOperationException("Không đọc được loại pattern "+name);if(type==eLoadPatternType.Wind)result.Add(name);}
            return result.ToArray();
        }
        public static object Probe()
        {
            var sap=AppConnectionManager.GetSapModel()??throw new InvalidOperationException("Kết nối ETABS trước.");
            int n=0;string[] keys=Array.Empty<string>(),names=Array.Empty<string>();int[] types=Array.Empty<int>();bool[] empty=Array.Empty<bool>();
            int rc=sap.DatabaseTables.GetAllTables(ref n,ref keys,ref names,ref types,ref empty);
            if(rc!=0||n<0||new Array[]{keys,names,types,empty}.Any(a=>a==null||a.Length<n))throw new InvalidOperationException("GetAllTables: "+rc);
            var tables=new List<object>();
            for(int i=0;i<n;i++)if(keys[i].IndexOf("Wind",StringComparison.OrdinalIgnoreCase)>=0&&keys[i].IndexOf("User",StringComparison.OrdinalIgnoreCase)>=0){
                int version=0,count=0;string[] fields=Array.Empty<string>(),labels=Array.Empty<string>(),desc=Array.Empty<string>(),units=Array.Empty<string>();bool[] importable=Array.Empty<bool>();
                int schemaRc=sap.DatabaseTables.GetAllFieldsInTable(keys[i],ref version,ref count,ref fields,ref labels,ref desc,ref units,ref importable);
                int records=0;string[] included=Array.Empty<string>(),data=Array.Empty<string>();
                int readRc=sap.DatabaseTables.GetTableForEditingArray(keys[i],"",ref version,ref included,ref records,ref data);
                tables.Add(new{Key=keys[i],Name=names[i],ImportType=types[i],Version=version,SchemaReturnCode=schemaRc,Fields=fields,Labels=labels,Descriptions=desc,Units=units,Importable=importable,ReadReturnCode=readRc,Included=included,Records=records,Data=data});
            }
            return new{CapturedUtc=DateTime.UtcNow,Model=AppConnectionManager.ModelIdentity,SDK=typeof(cAutoWind).Assembly.FullName,Methods=typeof(cAutoWind).GetMethods().Where(m=>m.Name.Contains("User")).Select(m=>m.ToString()).ToArray(),Tables=tables,Note="Read-only probe. Includes model identifiers and load data. No table setter or unit changes."};
        }
    }
}
