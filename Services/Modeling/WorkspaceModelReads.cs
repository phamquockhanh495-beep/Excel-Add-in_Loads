#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;
namespace QksExcelAddIn.Services.Modeling
{
    /// <summary>Shared session cache; apply hosts deliberately bypass it.</summary>
    public static class WorkspaceModelReads
    {
        private static readonly ModelReadCache Cache=new ModelReadCache();
        private static readonly object LogGate=new object();
        private static int changeReported;
        public static event Action? ModelChanged;
        public static void Invalidate()=>Cache.Invalidate();
        public static void NotifyModelChanged() {
            Cache.Invalidate();
            if(Interlocked.Exchange(ref changeReported,1)!=0)return;
            var handlers=ModelChanged;if(handlers==null)return;
            foreach(Action handler in handlers.GetInvocationList())try{handler();}catch{ }
        }
        public static async Task<CachedModelRead> ReadAsync(ModelReadScope scope,bool refresh,Action<string> progress,CancellationToken token)
        {
            var watch=Stopwatch.StartNew();
            var source=AppConnectionManager.ActiveEtabs??throw new InvalidOperationException("Chưa kết nối CSI.");
            var sap=AppConnectionManager.GetReadSapModel()??throw new InvalidOperationException("Chưa kết nối CSI.");
            eForce f=default;eLength l=default;eTemperature t=default;
            if(sap.GetPresentUnits_2(ref f,ref l,ref t)!=0)throw new InvalidOperationException("Chưa xác nhận đơn vị CSI.");
            string identity=AppConnectionManager.ModelIdentity;
            string key=identity+"|"+source.Version+"|"+f+"/"+l+"/"+t+"|reader-2";
            if(refresh)Cache.Invalidate();
            var result=await Cache.ReadAsync(source,key,scope,()=>new EtabsSnapshotReader().ReadAsync(progress,token,scope==ModelReadScope.Verified,scope==ModelReadScope.Catalog),token);
            if(!ReferenceEquals(source,AppConnectionManager.ActiveEtabs)||identity!=AppConnectionManager.ModelIdentity||
                !Qks.Shared.Protocol.SamePath(sap.GetModelFilename(),source.ModelPath))throw new InvalidOperationException("Mô hình đã đổi trong khi đọc.");
            Interlocked.Exchange(ref changeReported,0);
            string message=(result.Reused?"Dùng dữ liệu đã lưu trong phiên":"Đã đồng bộ CSI")+" · "+(scope==ModelReadScope.Catalog?"Tầng/pattern":scope==ModelReadScope.Viewer?"Mô hình 2D":"Mô hình gán tải")+" · "+watch.Elapsed.TotalSeconds.ToString("0.00")+" s · "+
                result.Snapshot.CapturedUtc.ToLocalTime().ToString("dd/MM HH:mm:ss")+(result.Reused?" · Bấm Đọc lại nếu đã sửa trong CSI.":"");
            progress(message);
            Log(source.Product+" "+message+" objects="+result.Snapshot.Elements.Count);
            return result;
        }
        public static void RecordViewDuration(string view,TimeSpan elapsed,int visible)=>Log(view+" setup/project="+elapsed.TotalMilliseconds.ToString("0.00")+" ms; visible="+visible+"; excludes WPF render/compositor");
        private static void Log(string message)
        {
            // Best-effort, bounded log; no model paths or load values.
            try {lock(LogGate) {
                string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QKS","Logs","model-read.log");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if(File.Exists(path)&&new FileInfo(path).Length>1024*1024)File.Delete(path);
                File.AppendAllText(path,DateTime.UtcNow.ToString("O")+" "+message+Environment.NewLine);
            }}catch { }
        }

    }
}
