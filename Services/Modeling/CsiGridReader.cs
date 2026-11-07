using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Services.Modeling
{
    public sealed partial class EtabsSnapshotReader
    {
        private static IReadOnlyList<ModelGridLine> ReadGrids(cSapModel sap,double metres,IReadOnlyList<ModelLevel> levels,Action<int,string> error,CancellationToken token)
        {
            var result=new List<ModelGridLine>();
            try{
                int count=0;string[] names=Array.Empty<string>();int code=sap.GridSys.GetNameList(ref count,ref names);
                if(code!=0){error(code,"Chưa đọc được lưới trục GridSys");return result;}
                foreach(string name in Names(code,count,names,"GridSys.GetNameList")){
                    token.ThrowIfCancellationRequested();
                    try{
                        double x=0,y=0,rz=0,bubble=0;int color=0,nx=0,ny=0,ng=0;bool defaultRange=true;string top="",bottom="",type="";
                        string[] ix=Array.Empty<string>(),iy=Array.Empty<string>(),ig=Array.Empty<string>(),bx=Array.Empty<string>(),by=Array.Empty<string>(),bg=Array.Empty<string>();
                        double[] xx=Array.Empty<double>(),yy=Array.Empty<double>(),gx1=Array.Empty<double>(),gy1=Array.Empty<double>(),gx2=Array.Empty<double>(),gy2=Array.Empty<double>();
                        bool[] vx=Array.Empty<bool>(),vy=Array.Empty<bool>(),vg=Array.Empty<bool>();
                        int rc=sap.GridSys.GetGridSysType(name,ref type);
                        bool polar=rc==0&&type.IndexOf("Cylind",StringComparison.OrdinalIgnoreCase)>=0;
                        rc=polar?sap.GridSys.GetGridSysCylindrical(name,ref x,ref y,ref rz,ref defaultRange,ref top,ref bottom,ref bubble,ref color,ref nx,ref ix,ref xx,ref vx,ref bx,ref ny,ref iy,ref yy,ref vy,ref by):
                            sap.GridSys.GetGridSysCartesian(name,ref x,ref y,ref rz,ref defaultRange,ref top,ref bottom,ref bubble,ref color,ref nx,ref ix,ref xx,ref vx,ref bx,ref ny,ref iy,ref yy,ref vy,ref by,ref ng,ref ig,ref gx1,ref gy1,ref gx2,ref gy2,ref vg,ref bg);
                        if(rc!=0){
                            rc=sap.GridSys.GetGridSys_2(name,ref x,ref y,ref rz,ref type,ref nx,ref ny,ref ix,ref iy,ref xx,ref yy,ref vx,ref vy,ref bx,ref by);
                            if(rc!=0){error(rc,"Chưa đọc được lưới trục "+name);continue;}
                            if(!string.Equals(type,"Cartesian",StringComparison.OrdinalIgnoreCase)){error(-1,"Lưới "+name+": API cũ chưa cung cấp đủ hình học "+type);continue;}
                            defaultRange=true;ng=0;polar=false;error(-1,"Lưới "+name+": dùng GridSys_2; API cũ không cung cấp phạm vi tầng / trục General");
                        }
                        Arrays(nx,ix,xx,vx,bx);Arrays(ny,iy,yy,vy,by);Arrays(ng,ig,gx1,gy1,gx2,gy2,vg,bg);
                        if(!Number.Finite(x)||!Number.Finite(y)||!Number.Finite(rz)||xx.Any(v=>!Number.Finite(v))||yy.Any(v=>!Number.Finite(v)))throw new InvalidOperationException("Tọa độ trục không hữu hạn.");
                        double? min=null,max=null;
                        if(!defaultRange){
                            double LevelZ(string story){var level=levels.FirstOrDefault(l=>l.Name==story);if(level!=null)return level.Elevation;double z=0;Ok(sap.Story.GetElevation(story,ref z),"Grid Story.GetElevation "+story);if(!Number.Finite(z))throw new InvalidOperationException("Cao độ lưới không hữu hạn.");return z*metres;}
                            min=LevelZ(bottom);max=LevelZ(top);if(min>max)throw new InvalidOperationException("Phạm vi tầng của lưới không khớp mô hình.");
                        }
                        var system=new List<ModelGridLine>();Vec2 Map(Vec2 p)=>GridGeometry.Transform(p,x,y,rz,metres);
                        void Add(string label,IEnumerable<Vec2> points,bool visible,string loc,bool infinite=true){var b=GridGeometry.Bubbles(loc);system.Add(new ModelGridLine(name,label,points.Select(Map),infinite,visible,b[0],b[1],min,max));}
                        if(polar){
                            for(int i=0;i<nx;i++){double r=xx[i];if(r<=0)continue;Add(ix[i],Enumerable.Range(0,361).Select(k=>new Vec2(r*Math.Cos(k*Math.PI/180),r*Math.Sin(k*Math.PI/180))),vx[i],bx[i],false);}
                            double outer=xx.Take(nx).DefaultIfEmpty(0).Max(),inner=Math.Max(0,xx.Take(nx).DefaultIfEmpty(0).Min());
                            if(Math.Abs(outer-inner)<1e-9)inner=0;
                            if(outer>0)for(int i=0;i<ny;i++){double a=yy[i]*Math.PI/180;Add(iy[i],new[]{new Vec2(inner*Math.Cos(a),inner*Math.Sin(a)),new Vec2(outer*Math.Cos(a),outer*Math.Sin(a))},vy[i],by[i],false);}
                        }else{
                            for(int i=0;i<nx;i++)Add(ix[i],new[]{new Vec2(xx[i],0),new Vec2(xx[i],1)},vx[i],bx[i]);
                            for(int i=0;i<ny;i++)Add(iy[i],new[]{new Vec2(0,yy[i]),new Vec2(1,yy[i])},vy[i],by[i]);
                            for(int i=0;i<ng;i++)Add(ig[i],new[]{new Vec2(gx1[i],gy1[i]),new Vec2(gx2[i],gy2[i])},vg[i],bg[i],false);
                        }
                        result.AddRange(system);
                    }catch(OperationCanceledException){throw;}catch(Exception ex){error(-1,"Chưa đọc được hệ trục "+name+": "+ex.Message);}
                }
            }catch(OperationCanceledException){throw;}catch(Exception ex){error(-1,"Chưa xác nhận lưới trục: "+ex.Message);}
            return result;
        }
    }
}
