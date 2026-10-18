#nullable enable
using System;

namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Coalesces connection changes and rejects a read completed for an older source.</summary>
    public sealed class WorkspaceReadLifecycle
    {
        private object? connection;
        private string identity="";
        private long generation;
        public bool Pending { get; private set; }
        public bool Update(object? source,string sourceIdentity,bool automatic=true)
        {
            if(ReferenceEquals(source,connection)&&string.Equals(sourceIdentity,identity,StringComparison.Ordinal))return false;
            connection=source;identity=sourceIdentity;generation++;Pending=source!=null&&automatic;return true;
        }
        public void Request() { Pending=connection!=null; }
        public void CancelPending() { Pending=false; }
        public bool TryBegin(bool busy,out long ticket)
        {
            ticket=generation;if(busy||!Pending||connection==null)return false;Pending=false;return true;
        }
        public bool Accept(long ticket,object? active,string activeIdentity)=>ticket==generation&&connection!=null&&ReferenceEquals(connection,active)&&string.Equals(identity,activeIdentity,StringComparison.Ordinal);
    }
}
