using System;
using System.Linq;
using System.Reflection;
namespace Qks.Shared
{
    /// <summary>Maps only matching read signatures to the installed product's own API types.</summary>
    public static class CsiReadDispatch
    {
        public const int Unsupported = -32001;
        public static bool IsRead(string name) => name.StartsWith("Get",StringComparison.Ordinal) || name.StartsWith("get_",StringComparison.Ordinal);
        public static object? ConvertValue(object? value,Type type)
        {
            if(type.IsByRef)type=type.GetElementType()!;
            if(value==null)return null;
            if(type.IsInstanceOfType(value))return value;
            if(type.IsEnum&&value.GetType().IsEnum){string name=Enum.GetName(value.GetType(),value)??throw new NotSupportedException("Giá trị enum API không được định nghĩa.");if(!Enum.IsDefined(type,name))throw new NotSupportedException("API không hỗ trợ enum "+name);return Enum.Parse(type,name);}
            throw new NotSupportedException("Kiểu API không khớp: "+value.GetType().Name+" → "+type.Name);
        }
        private static bool Compatible(Type a,Type b)
        {
            if(a.IsByRef!=b.IsByRef)return false;if(a.IsByRef){a=a.GetElementType()!;b=b.GetElementType()!;}
            return a==b || a.IsEnum&&b.IsEnum&&a.Name==b.Name;
        }
        public static MethodInfo? Match(Type native,MethodInfo contract)
        {
            var source=contract.GetParameters();
            return native.GetMethods().SingleOrDefault(m=>m.Name==contract.Name&&(Compatible(m.ReturnType,contract.ReturnType)||m.ReturnType.IsInterface&&contract.ReturnType.IsInterface&&m.ReturnType.Name==contract.ReturnType.Name)&&m.GetParameters().Length==source.Length&&m.GetParameters().Select((p,i)=>Compatible(source[i].ParameterType,p.ParameterType)).All(b=>b));
        }
        public static object? Invoke(object target,MethodInfo native,MethodInfo contract,object?[] args)
        {
            if(!IsRead(contract.Name))throw new InvalidOperationException("Kết nối SAFE của Model 2D chỉ đọc; không hỗ trợ "+contract.Name+".");
            var ps=native.GetParameters();var call=new object?[args.Length];for(int i=0;i<call.Length;i++){
                var type=ps[i].ParameterType;
                if(type.IsByRef&&type.GetElementType()!.IsEnum&&args[i]!=null&&args[i]!.GetType().IsEnum&&!Enum.IsDefined(args[i]!.GetType(),args[i]!))call[i]=Enum.ToObject(type.GetElementType()!,0);
                else call[i]=ConvertValue(args[i],type);
            }
            object? result;try{result=native.Invoke(target,call);}catch(TargetInvocationException ex){throw ex.InnerException??ex;}
            // Failed calls must never leak partially populated arrays into the reader.
            if(result is int code && code!=0)return code;
            var source=contract.GetParameters();for(int i=0;i<args.Length;i++)if(source[i].ParameterType.IsByRef)args[i]=ConvertValue(call[i],source[i].ParameterType);
            return result;
        }
        public static bool SupportedSafeVersion(string version)
        {
            var match=System.Text.RegularExpressions.Regex.Match(version??"",@"^\s*[vV]?(\d+)(?:\.|\s|$)");
            return match.Success&&int.TryParse(match.Groups[1].Value,out int major)&&major>=18&&major<=23;
        }
    }
}
