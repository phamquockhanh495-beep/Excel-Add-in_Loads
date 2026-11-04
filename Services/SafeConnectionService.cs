using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CSiAPIv1;
using Qks.Shared;
namespace QksExcelAddIn.Services
{
    public static class SafeConnectionService
    {
        private const string ProgId="CSI.SAFE.API.ETABSObject";
        public static string ScanWarning {get;private set;}="";
        [DllImport("ole32.dll")]private static extern int GetRunningObjectTable(uint reserved,out IRunningObjectTable table);
        [DllImport("ole32.dll")]private static extern int CreateBindCtx(uint reserved,out IBindCtx context);
        private static void Release(object? value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
        private sealed class Sdk
        {
            public Type Api=null!,Model=null!,Helper=null!;
        }
        private static Sdk InstalledSdk(Process process)
        {
            string folder=Path.GetDirectoryName(process.MainModule!.FileName)!;
            // Load each installation by path: SAFE versions can expose different COM interface GUIDs.
            string file=Path.Combine(folder,"SAFEv1.dll");
            if(!File.Exists(file))file=Path.Combine(folder,"CSiAPIv1.dll");
            if(!File.Exists(file))throw new FileNotFoundException("Không tìm thấy SDK SAFEv1.dll / CSiAPIv1.dll cạnh SAFE.exe: "+folder);
            var assembly=Assembly.LoadFile(file);var types=assembly.GetExportedTypes();
            return new Sdk{Api=types.Single(t=>t.IsInterface&&t.Name=="cOAPI"),Model=types.Single(t=>t.IsInterface&&t.Name=="cSapModel"),Helper=types.Single(t=>t.IsInterface&&t.Name=="cHelper")};
        }
        private static object? Call(object obj,Type type,string name,params object?[] args)
        {
            var methods=type.GetMethods().Where(m=>m.Name==name).Where(m=>m.GetParameters().Length>=args.Length&&m.GetParameters().Skip(args.Length).All(p=>p.IsOptional)).ToArray();
            if(methods.Length!=1)throw new NotSupportedException(type.Name+"."+name+": chữ ký API không hỗ trợ.");
            var method=methods[0];var parameters=method.GetParameters();var full=args.Concat(parameters.Skip(args.Length).Select(p=>p.DefaultValue)).ToArray();
            object? result;try{result=method.Invoke(obj,full);}catch(TargetInvocationException ex){throw ex.InnerException??ex;}
            for(int i=0;i<args.Length;i++)args[i]=full[i];return result;
        }
        private static EtabsInstanceInfo Describe(object api,int pid,Sdk sdk)
        {
            object model=Call(api,sdk.Api,"get_SapModel")??throw new InvalidOperationException("SAFE không trả SapModel.");
            object?[] versionArgs={"",0d};int code=Convert.ToInt32(Call(model,sdk.Model,"GetVersion",versionArgs));
            string version=Convert.ToString(versionArgs[0])??"";
            if(code!=0||!CsiReadDispatch.SupportedSafeVersion(version))throw new NotSupportedException("Chỉ kết nối SAFE v18–v23; phiên bản trả về: "+version+" (API "+code+").");
            string path=Convert.ToString(Call(model,sdk.Model,"GetModelFilename",true))??"";
            return new EtabsInstanceInfo{Product="SAFE",ProcessId=pid,ModelPath=path,ModelFileName=string.IsNullOrWhiteSpace(path)?"Untitled":Path.GetFileNameWithoutExtension(path),Version=version,NativeApi=api,SapModel=SafeReadProxy.Create(model,sdk.Model),DisplayName=pid+"_SAFE "+version+" - "+(string.IsNullOrWhiteSpace(path)?"Untitled":Path.GetFileName(path))};
        }
        public static List<EtabsInstanceInfo> GetRunningInstances()
        {
            var found=new List<EtabsInstanceInfo>();var errors=new List<string>();var processes=Process.GetProcessesByName("SAFE");
            var sdks=new Dictionary<int,Sdk>();
            foreach(var process in processes)try{if(!process.HasExited&&process.Responding)sdks[process.Id]=InstalledSdk(process);else errors.Add("SAFE PID "+process.Id+": đang bận hoặc chưa sẵn sàng; thử Quét lại sau.");}catch(Exception ex){errors.Add("SAFE PID "+process.Id+": "+ex.Message);}
            IRunningObjectTable? rot=null;IEnumMoniker? enumerator=null;IBindCtx? context=null;
            try {
                if(sdks.Count>0&&GetRunningObjectTable(0,out rot)==0&&CreateBindCtx(0,out context)==0){
                    rot.EnumRunning(out enumerator);var monikers=new IMoniker[1];
                    while(enumerator.Next(1,monikers,IntPtr.Zero)==0){try{
                        monikers[0].GetDisplayName(context,null,out string name);
                        if(!name.StartsWith("!",StringComparison.Ordinal)||name.IndexOf("SAFE",StringComparison.OrdinalIgnoreCase)<0)continue;
                        int colon=name.LastIndexOf(':');if(colon<0||!int.TryParse(name.Substring(colon+1),out int pid)||!sdks.TryGetValue(pid,out var sdk)||found.Any(x=>x.ProcessId==pid))continue;
                        rot.GetObject(monikers[0],out object obj);found.Add(Describe(obj,pid,sdk));
                    }catch(Exception ex){errors.Add("SAFE ROT: "+ex.Message);}finally{Release(monikers[0]);}}
                }
            }catch(Exception ex){errors.Add("SAFE ROT: "+ex.Message);}finally{Release(context);Release(enumerator);Release(rot);}
            foreach(var process in processes)try{
                if(found.Any(x=>x.ProcessId==process.Id)||!sdks.TryGetValue(process.Id,out var sdk))continue;
                object? api=null;Exception? failure=null;
                foreach(string helperName in new[]{"SAFEv1.Helper","CSiAPIv1.Helper"}){
                    object? helper=null;
                    try{var type=Type.GetTypeFromProgID(helperName);if(type==null)continue;helper=Activator.CreateInstance(type)!;
                        try{api=Call(helper,sdk.Helper,"GetObjectProcess",ProgId,process.Id);}
                        catch when(processes.Length==1){api=Call(helper,sdk.Helper,"GetObject",ProgId);}
                        if(api!=null)break;
                    }catch(Exception ex){failure=ex;}finally{Release(helper);}
                }
                // The non-PID API is only unambiguous when exactly one SAFE process exists.
                if(api==null&&processes.Length==1)try{api=Marshal.GetActiveObject(ProgId);}catch(Exception ex){failure=ex;}
                if(api!=null)found.Add(Describe(api,process.Id,sdk));else errors.Add("SAFE PID "+process.Id+": "+(failure?.Message??"Không có API hoạt động. Bật API / đăng ký SAFE và quét lại."));
            }catch(Exception ex){errors.Add("SAFE PID "+process.Id+": "+ex.Message);}finally{process.Dispose();}
            ScanWarning=string.Join("\n",errors.Distinct().Take(12));return found;
        }
        public static List<EtabsInstanceInfo> GetRunningModels()
        {
            var result=EtabsService.GetRunningEtabsInstances();result.AddRange(GetRunningInstances());return result;
        }
        public static void OpenModelFile(string path)
        {
            // Use the installed Windows .fdb association; never open the file inside another active model.
            if(!string.Equals(Path.GetExtension(path),".fdb",StringComparison.OrdinalIgnoreCase)||!File.Exists(path))throw new ArgumentException("Chọn mô hình SAFE .fdb hợp lệ.");
            Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
        }
    }
}
