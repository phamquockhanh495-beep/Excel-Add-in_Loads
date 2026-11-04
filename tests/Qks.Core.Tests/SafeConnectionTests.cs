using System;
using System.Collections.Generic;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.Modeling.Printing;
internal static partial class Program
{
    private static class ContractApi
    {
        public enum eForce { kN=4,N=3 }
        public interface IModel { int GetUnits(ref eForce force); int GetRows(ref int count,ref double[] values); int SetUnits(eForce force); }
    }
    private static class InstalledApi
    {
        public enum eForce { kN=14,N=13,Unknown=99 }
        public sealed class Model
        {
            public int Writes;public bool Fail,Unknown;
            public int GetUnits(ref eForce force){force=Unknown?eForce.Unknown:eForce.kN;return 0;}
            public int GetRows(ref int count,ref double[] values){count=2;values=new[]{-3.5,7d};return Fail?5:0;}
            public int SetUnits(eForce force){Writes++;return 0;}
        }
    }
    private static void SafeConnectionTests()
    {
        Check("SAFE: version range includes 18 through 23 only",()=>{
            foreach(int n in Enumerable.Range(18,6))Assert(CsiReadDispatch.SupportedSafeVersion(n+".2.0"));
            foreach(string s in new[]{"17.0","24.0","118.0","unknown","","SAFE 23","18x"})Assert(!CsiReadDispatch.SupportedSafeVersion(s));
        });
        Check("SAFE: enum outputs map names, not numeric values",()=>{
            var method=typeof(ContractApi.IModel).GetMethod("GetUnits")!;var native=CsiReadDispatch.Match(typeof(InstalledApi.Model),method)!;object?[] args={(ContractApi.eForce)0};
            Assert((int)CsiReadDispatch.Invoke(new InstalledApi.Model(),native,method,args)! ==0);
            Assert((ContractApi.eForce)args[0]! ==ContractApi.eForce.kN);
        });
        Check("SAFE: unknown native enum is rejected",()=>{
            var method=typeof(ContractApi.IModel).GetMethod("GetUnits")!;var native=CsiReadDispatch.Match(typeof(InstalledApi.Model),method)!;object?[] args={(ContractApi.eForce)0};
            Throws<NotSupportedException>(()=>CsiReadDispatch.Invoke(new InstalledApi.Model{Unknown=true},native,method,args));
        });
        Check("SAFE: signed read arrays survive adapter; failed result does not publish rows",()=>{
            var method=typeof(ContractApi.IModel).GetMethod("GetRows")!;var native=CsiReadDispatch.Match(typeof(InstalledApi.Model),method)!;object?[] args={0,Array.Empty<double>()};
            Assert((int)CsiReadDispatch.Invoke(new InstalledApi.Model(),native,method,args)! ==0);Assert((int)args[0]! ==2&&((double[])args[1]!)[0]==-3.5);
            object?[] failedArgs={0,Array.Empty<double>()};Assert((int)CsiReadDispatch.Invoke(new InstalledApi.Model{Fail=true},native,method,failedArgs)! ==5);Assert((int)failedArgs[0]! ==0&&((double[])failedArgs[1]!).Length==0);
        });
        Check("SAFE: write request blocked before calling installed API",()=>{
            var method=typeof(ContractApi.IModel).GetMethod("SetUnits")!;var native=CsiReadDispatch.Match(typeof(InstalledApi.Model),method)!;var model=new InstalledApi.Model();
            Throws<InvalidOperationException>(()=>CsiReadDispatch.Invoke(model,native,method,new object?[]{ContractApi.eForce.kN}));Assert(model.Writes==0);
        });
        Check("SAFE: geometry fallback distinguishes planar slab, column, beam and wall",()=>{
            Assert(SafeModelGeometry.Frame(new Vec3(),new Vec3(0,0,3))==ElementKind.Column);
            Assert(SafeModelGeometry.Frame(new Vec3(),new Vec3(3,0,0))==ElementKind.Beam);
            Assert(SafeModelGeometry.Area(new[]{new Vec3(),new Vec3(3,0,0),new Vec3(3,0,3),new Vec3(0,0,3)})==ElementKind.Wall);
            Assert(SafeModelGeometry.Area(new[]{new Vec3(),new Vec3(3,0,0),new Vec3(3,2,0)})==ElementKind.Slab);
        });
        Check("SAFE: missing Story metadata yields stable levels for report filters",()=>{
            var elements=new List<ModelElement>{new ModelElement("Area:1","1","","","S200",ElementKind.Slab,new[]{new Vec3(0,0,3),new Vec3(4,0,3.00000001),new Vec3(4,3,3)})};
            var levels=SafeModelGeometry.Levels(elements);Assert(levels.Length==1&&levels[0].Elevation==3);SafeModelGeometry.AssignLevels(elements,levels);Assert(elements[0].Story==levels[0].Name&&elements[0].Vertices[1].Z==3.00000001);
        });
        Check("PDF preview: landscape and portrait fit both viewport dimensions",()=>{
            double landscape=PrintPreviewScale.Fit(1191,842,700,600),portrait=PrintPreviewScale.Fit(842,1191,700,600);
            Assert(1191*landscape<=700+.001&&842*landscape<=600+.001);Assert(842*portrait<=700+.001&&1191*portrait<=600+.001);
        });
    }
}
