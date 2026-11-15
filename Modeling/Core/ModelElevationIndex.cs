#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Interval tree over physical Z extents, conservatively expanded for extrusion.
    /// Story labels are deliberately not used: columns/walls and sloped members cross levels.</summary>
    public sealed class ModelElevationIndex
    {
        private sealed class Entry {
            public ModelElement Element=null!; public int Order; public double Min,Max;
        }
        private sealed class Node {
            public double Center; public Entry[] ByMin=Array.Empty<Entry>(),ByMax=Array.Empty<Entry>(); public Node? Left,Right;
        }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ModelSnapshot,ModelElevationIndex> Indexes = new System.Runtime.CompilerServices.ConditionalWeakTable<ModelSnapshot,ModelElevationIndex>();
        public static ModelElevationIndex For(ModelSnapshot snapshot)=>Indexes.GetValue(snapshot,s=>new ModelElevationIndex(s.Elements));
        private readonly Node? root;
        public ModelElevationIndex(IEnumerable<ModelElement> elements) {
            root=Build(elements.Select((e,i)=>{double pad=e.Section==null?0:e.Section.Width+e.Section.Depth;
                return new Entry{Element=e,Order=i,Min=e.Vertices.Min(p=>p.Z)-pad,Max=e.Vertices.Max(p=>p.Z)+pad};}).ToArray());
        }
        private static Node? Build(Entry[] items) {
            if(items.Length==0)return null;
            double center=items.Select(e=>(e.Min+e.Max)/2).OrderBy(z=>z).ElementAt(items.Length/2);
            var overlap=items.Where(e=>e.Min<=center&&e.Max>=center).ToArray();
            return new Node{Center=center,ByMin=overlap.OrderBy(e=>e.Min).ToArray(),ByMax=overlap.OrderByDescending(e=>e.Max).ToArray(),
                Left=Build(items.Where(e=>e.Max<center).ToArray()),Right=Build(items.Where(e=>e.Min>center).ToArray())};
        }
        public IReadOnlyList<ModelElement> Query(double min,double max) {
            if(!Number.Finite(min)||!Number.Finite(max)||min>max)throw new ArgumentException("Khoảng cao độ không hợp lệ.");
            var result=new List<Entry>();
            void Visit(Node? node) {
                if(node==null)return;
                if(max<node.Center) { foreach(var e in node.ByMin){if(e.Min>max)break;result.Add(e);}Visit(node.Left); }
                else if(min>node.Center) { foreach(var e in node.ByMax){if(e.Max<min)break;result.Add(e);}Visit(node.Right); }
                else {result.AddRange(node.ByMin);Visit(node.Left);Visit(node.Right);}
            }
            Visit(root);return result.OrderBy(e=>e.Order).Select(e=>e.Element).ToArray();
        }
    }
}
