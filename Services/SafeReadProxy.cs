using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using CSiAPIv1;
using Qks.Shared;
namespace QksExcelAddIn.Services
{
    /// <summary>Read-only interface bridge. SAFE's installed SDK supplies its COM IIDs and enum types.</summary>
    internal sealed class SafeReadProxy:RealProxy
    {
        private readonly object? target;
        private readonly Type? native;
        private SafeReadProxy(Type contract,object? target,Type? native):base(contract){this.target=target;this.native=native;}
        internal static cSapModel Create(object model,Type native)=>(cSapModel)new SafeReadProxy(typeof(cSapModel),model,native).GetTransparentProxy();
        public override IMessage Invoke(IMessage message)
        {
            var call=(IMethodCallMessage)message;var method=(MethodInfo)call.MethodBase;var args=(object?[])call.Args.Clone();
            try {
                if(!CsiReadDispatch.IsRead(method.Name))throw new InvalidOperationException("SAFE: kết nối chỉ đọc, không cho phép "+method.Name+".");
                var actual=native==null?null:CsiReadDispatch.Match(native,method);
                object? result=null;
                if(target!=null&&actual!=null)result=CsiReadDispatch.Invoke(target,actual,method,args);
                else if(method.ReturnType==typeof(int))result=CsiReadDispatch.Unsupported;
                else if(!method.ReturnType.IsInterface)throw new NotSupportedException("SAFE API không hỗ trợ "+method.Name+".");
                if(method.ReturnType.IsInterface)result=new SafeReadProxy(method.ReturnType,result,actual?.ReturnType).GetTransparentProxy();
                return new ReturnMessage(result,args,args.Length,call.LogicalCallContext,call);
            } catch(NotSupportedException ex) {
                if(method.ReturnType==typeof(int))return new ReturnMessage(CsiReadDispatch.Unsupported,call.Args,call.ArgCount,call.LogicalCallContext,call);
                return new ReturnMessage(ex,call);
            } catch(Exception ex){return new ReturnMessage(ex,call);}
        }
    }
}
