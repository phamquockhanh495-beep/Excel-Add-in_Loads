#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class SlabLoadRegion
    {
        public string ElementId { get; }
        public string Key { get; }
        public string Label { get; }
        public string Color { get; }
        public string LegendLabel { get; }
        private SlabLoadRegion(string id,string key,string label,string legend){ElementId=id;Key=key;Label=label;LegendLabel=legend;using(var sha=System.Security.Cryptography.SHA256.Create()){var bytes=sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));Color=$"#{64+bytes[0]%144:X2}{64+bytes[1]%144:X2}{64+bytes[2]%144:X2}";}}
        public static IReadOnlyList<SlabLoadRegion> Build(IEnumerable<ReadLoadDisplay> rows,bool sets)
        {
            return rows.Where(r=>r.Source.Kind==ElementKind.Slab).GroupBy(r=>r.ElementId).Select(g=>{
                var sorted=g.OrderBy(r=>r.Load.Pattern,StringComparer.Ordinal).ThenBy(r=>r.Load.Kind,StringComparer.Ordinal).ThenBy(r=>r.Load.Direction).ThenBy(r=>r.Load.Value1).ThenBy(r=>r.Load.Value2).ThenBy(r=>r.Load.CoordinateSystem,StringComparer.Ordinal).ThenBy(r=>r.Load.Unit,StringComparer.Ordinal).ToArray();
                string label=string.Join(" / ",sorted.Select(r=>sets?r.UniformLoadSet:r.Load.Pattern).Distinct().OrderBy(n=>n,StringComparer.Ordinal));
                string Values(ReadLoadDisplay r)=>r.Load.Pattern+": "+r.Load.Value1.ToString("0.###",CultureInfo.CurrentCulture)+(r.Load.Value1!=r.Load.Value2?" → "+r.Load.Value2.ToString("0.###",CultureInfo.CurrentCulture):"")+" "+r.Load.Unit+" · "+r.Load.CoordinateSystem+" / "+r.DirectionText;
                string legend=sets?string.Join("\n",sorted.GroupBy(r=>r.UniformLoadSet).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>g.Key+"\n"+string.Join("\n",g.Select(Values)))):
                    string.Join("\n",sorted.Select(r=>Values(r)+" · "+r.Load.Kind));
                string key=sets?Qks.Shared.JsonCodec.Serialize(sorted.Select(r=>r.UniformLoadSet).Distinct().OrderBy(n=>n,StringComparer.Ordinal)):
                    Qks.Shared.JsonCodec.Serialize(sorted.Select(r=>new{r.Load.Pattern,r.Load.Kind,r.Load.CoordinateSystem,r.Load.Direction,r.Load.Value1,r.Load.Value2,r.Load.Unit}));
                return new SlabLoadRegion(g.Key,key,label,legend);
            }).ToArray();
        }
    }
}
