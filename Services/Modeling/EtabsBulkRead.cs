using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Services.Modeling
{
    public sealed partial class EtabsSnapshotReader
    {
        private static bool TryLoadBatch(cSapModel sap,string prefix,string[] names,UnitScale scale,List<ExistingLoad> loads,List<string> diagnostics,
            Action<cSapModel,ModelElement,UnitScale,List<ExistingLoad>,Action<int,string>,bool> read)
        {
            var all=new ModelElement(prefix+"ALL","ALL","","","",ElementKind.Other,Array.Empty<Vec3>());
            bool success=LoadBatch.TryRead(rows=>read(sap,all,scale,rows,(code,message)=>{if(code!=0)throw new InvalidOperationException(message+" (API "+code+")");},true),names.Select(n=>prefix+n),loads,out string reason);
            diagnostics.Add(read.Method.Name+": "+(success?"đọc theo nhóm ALL":"đọc theo đối tượng; "+reason));
            return success;
        }
        private static void ReadPointsBulk(cSapModel sap,UnitScale scale,Dictionary<string,Vec3> points,List<string> diagnostics)
        {
            try {
                int n=0;string[] names=Array.Empty<string>();double[] x=Array.Empty<double>(),y=Array.Empty<double>(),z=Array.Empty<double>();
                Ok(sap.PointObj.GetAllPoints(ref n,ref names,ref x,ref y,ref z,"Global"),"PointObj.GetAllPoints");Arrays(n,names,x,y,z);
                var candidate=new Dictionary<string,Vec3>(StringComparer.Ordinal);
                for(int i=0;i<n;i++) {
                    var point=new Vec3(x[i]*scale.LengthToMetres,y[i]*scale.LengthToMetres,z[i]*scale.LengthToMetres);
                    if(string.IsNullOrWhiteSpace(names[i])||!point.IsFinite)throw new InvalidOperationException("Tọa độ bulk không hợp lệ.");
                    candidate.Add(names[i],point);
                }
                foreach(var pair in candidate)points.Add(pair.Key,pair.Value);
                diagnostics.Add("Tọa độ nút: GetAllPoints · "+n+" nút.");
            }catch(Exception ex) {points.Clear();diagnostics.Add("Tọa độ nút: đọc từng nút; "+ex.GetBaseException().Message);}
        }
    }
}
