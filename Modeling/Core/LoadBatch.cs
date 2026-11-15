#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public static class LoadBatch
    {
        /// <summary>All-or-nothing batch: unsupported/malformed bulk responses must not leave
        /// partial rows that would be counted twice by the per-object fallback.</summary>
        public static bool TryRead(Action<List<ExistingLoad>> read,IEnumerable<string> targetIds,List<ExistingLoad> destination,out string reason)
        {
            try {
                var candidate=new List<ExistingLoad>();read(candidate);
                var known=new HashSet<string>(targetIds,StringComparer.Ordinal);
                if(candidate.Any(l=>!known.Contains(l.ElementId)||string.IsNullOrWhiteSpace(l.Pattern)||!Number.Finite(l.Value1)||!Number.Finite(l.Value2)||!Number.Finite(l.Start)||!Number.Finite(l.End)))
                    throw new InvalidOperationException("Bulk trả đối tượng/giá trị không hợp lệ.");
                destination.AddRange(candidate);reason="";return true;
            } catch(OperationCanceledException) {throw;}
            catch(Exception ex) {reason=ex.GetBaseException().Message;return false;}
        }
    }
}
