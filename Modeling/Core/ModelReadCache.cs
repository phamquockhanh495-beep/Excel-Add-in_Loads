#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QksExcelAddIn.Modeling.Core
{
    public enum ModelReadScope { Catalog, Viewer, Verified }
    public sealed class CachedModelRead
    {
        public ModelSnapshot Snapshot { get; }
        public bool Reused { get; }
        public CachedModelRead(ModelSnapshot snapshot, bool reused) { Snapshot=snapshot; Reused=reused; }
    }
    /// <summary>One active connection, pure DTOs only. Factories execute on the requesting STA.
    /// Invalidation rejects late completions; cancellation of a follower does not cancel its leader.</summary>
    public sealed class ModelReadCache
    {
        private readonly object sync=new object();
        private object? session;
        private string key="";
        private long generation;
        private readonly Dictionary<ModelReadScope,ModelSnapshot> ready=new Dictionary<ModelReadScope,ModelSnapshot>();
        private readonly Dictionary<ModelReadScope,Task<ModelSnapshot>> pending=new Dictionary<ModelReadScope,Task<ModelSnapshot>>();
        public void Invalidate() { lock(sync) { generation++; ready.Clear(); pending.Clear(); } }
        public async Task<CachedModelRead> ReadAsync(object source,string identity,ModelReadScope scope,
            Func<Task<ModelSnapshot>> read,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Task<ModelSnapshot> task; TaskCompletionSource<ModelSnapshot>? leader=null; long ticket;
            lock(sync) {
                if(!ReferenceEquals(source,session)||key!=identity) { generation++;ready.Clear();pending.Clear();session=source;key=identity; }
                ticket=generation;
                var effective=scope==ModelReadScope.Viewer&&(ready.ContainsKey(ModelReadScope.Verified)||pending.ContainsKey(ModelReadScope.Verified))?ModelReadScope.Verified:scope;
                if(scope==ModelReadScope.Verified&&pending.ContainsKey(ModelReadScope.Viewer))effective=ModelReadScope.Viewer;
                if(ready.TryGetValue(effective,out var cached)&&!(scope==ModelReadScope.Verified&&!cached.LoadBaselineComplete))return new CachedModelRead(cached,true);
                if(!pending.TryGetValue(effective,out task!)) {
                    leader=new TaskCompletionSource<ModelSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
                    task=leader.Task;pending[scope]=task;
                }
            }
            if(leader!=null) {
                try {
                    var snapshot=await read(); token.ThrowIfCancellationRequested();
                    if(snapshot.CatalogOnly!=(scope==ModelReadScope.Catalog))throw new InvalidOperationException("Phạm vi dữ liệu đọc không khớp.");
                    lock(sync) {
                        if(ticket!=generation)throw new OperationCanceledException("Nguồn đã được đồng bộ lại; bỏ kết quả cũ.");
                        if(scope==ModelReadScope.Verified&&!snapshot.LoadBaselineComplete)throw new InvalidOperationException("Chưa đọc đủ baseline tải.");
                        ready[scope]=snapshot;
                        if(scope==ModelReadScope.Viewer&&snapshot.LoadBaselineComplete)ready[ModelReadScope.Verified]=snapshot;
                    }
                    leader.TrySetResult(snapshot);
                } catch(OperationCanceledException) { leader.TrySetCanceled(); }
                catch(Exception ex) { leader.TrySetException(ex); }
                finally { lock(sync) { if(ticket==generation)pending.Remove(scope); } }
            }
            var canceled=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(token.Register(()=>canceled.TrySetResult(true))) {
                if(await Task.WhenAny(task,canceled.Task)!=task)throw new OperationCanceledException(token);
                var result=await task;
                token.ThrowIfCancellationRequested();
                lock(sync) { if(ticket!=generation)throw new OperationCanceledException("Nguồn đã đổi."); }
                if(scope==ModelReadScope.Verified&&!result.LoadBaselineComplete)throw new InvalidOperationException("Lần đọc chung còn thiếu baseline tải. Bấm Đọc lại ETABS.");
                return new CachedModelRead(result,leader==null);
            }
        }
    }
}
