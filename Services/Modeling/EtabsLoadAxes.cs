using System;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Services.Modeling
{
    public sealed partial class EtabsSnapshotReader
    {
        private static Vec3[]? ReadAxes(cSapModel sap,string kind,string name,Action<int,string> error)
        {
            double[] values=Array.Empty<double>();
            int code=kind=="Frame"?sap.FrameObj.GetTransformationMatrix(name,ref values,true):kind=="Area"?sap.AreaObj.GetTransformationMatrix(name,ref values,true):sap.PointObj.GetTransformationMatrix(name,ref values,true);
            if(code!=0||values==null||values.Length!=9){error(code!=0?code:-1,"Chưa đọc được hướng trục "+kind+":"+name);return null;}
            var axes=new[]{new Vec3(values[0],values[1],values[2]),new Vec3(values[3],values[4],values[5]),new Vec3(values[6],values[7],values[8])};
            if(!LoadGlyphs.ValidAxes(axes)){error(-1,"Ma trận trục không hợp lệ "+kind+":"+name);return null;}
            return axes;
        }
    }
}
