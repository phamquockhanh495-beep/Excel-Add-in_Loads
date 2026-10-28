using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Services.Modeling
{
    public sealed partial class EtabsSnapshotReader
    {
        private static void ReadUniformToFrame(cSapModel sap,ModelElement e,UnitScale scale,List<ExistingLoad> loads,Action<int,string> error)
        {
            int n=0;string[] names=Array.Empty<string>(),patterns=Array.Empty<string>(),systems=Array.Empty<string>();
            int[] directions=Array.Empty<int>(),distribution=Array.Empty<int>();double[] values=Array.Empty<double>();
            int rc=sap.AreaObj.GetLoadUniformToFrame(e.Name,ref n,ref names,ref patterns,ref systems,ref directions,ref values,ref distribution,eItemType.Objects);
            if(rc!=0){error(rc,"Chưa xác nhận uniform-to-frame "+e.Id);return;}
            Arrays(n,names,patterns,systems,directions,values,distribution);
            for(int i=0;i<n;i++) {
                if(names[i]!=e.Name)throw new InvalidOperationException("Uniform-to-frame thuộc area khác: "+e.Id);
                double value=values[i]*scale.Pressure;
                // Show the original shell pressure, never invent calculated transferred frame loads.
                loads.Add(new ExistingLoad(e.Id,patterns[i],"Area uniform to frame (DistType "+distribution[i]+")",systems[i],directions[i],0,1,value,value,"kN/m²"));
            }
        }
        private static void ReadUniformSets(cSapModel sap,UnitScale scale,List<ModelElement> elements,List<ExistingLoad> loads,Action<int,string> error)
        {
            try {
                int count=0;string[] keys=Array.Empty<string>(),names=Array.Empty<string>();int[] types=Array.Empty<int>();bool[] empty=Array.Empty<bool>();
                int rc=sap.DatabaseTables.GetAllTables(ref count,ref keys,ref names,ref types,ref empty);
                if(rc!=0){error(rc,"Chưa kiểm tra được Uniform Load Sets");return;}Arrays(count,keys,names,types,empty);
                var nonempty=Enumerable.Range(0,count).Where(i=>!empty[i]&&keys[i].IndexOf("Uniform Load Set",StringComparison.OrdinalIgnoreCase)>=0).Select(i=>keys[i]).ToArray();
                var assignmentKeys=nonempty.Where(k=>k.IndexOf("Assign",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
                if(assignmentKeys.Length==0)return;
                var definitionKeys=nonempty.Except(assignmentKeys).ToArray();
                if(definitionKeys.Length!=1||assignmentKeys.Length!=1){error(-1,"Schema Uniform Load Sets không rõ ràng; chưa mở rộng tải từ set");return;}
                LoadTable Read(string key) {
                    int version=0,n=0;string[] fields=Array.Empty<string>(),data=Array.Empty<string>();
                    // This getter reads complete assignments without changing ETABS display selections or tables.
                    Ok(sap.DatabaseTables.GetTableForEditingArray(key,"",ref version,ref fields,ref n,ref data),key);
                    return new LoadTable(fields,n,data);
                }
                var expanded=UniformSetLoads.Expand(Read(assignmentKeys[0]),Read(definitionKeys[0]),elements,scale);
                loads.AddRange(expanded);
            } catch(Exception ex) { error(-1,"Chưa xác nhận tải Uniform Load Sets: "+ex.Message); }
        }
    }
}
