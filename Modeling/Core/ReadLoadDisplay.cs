#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class ReadLoadDisplay
    {
        public string ElementId { get; }
        public string Category { get; }
        public ProjectedElement Geometry { get; }
        public ExistingLoad Load { get; }
        public ModelElement Source { get; }
        public ViewPlane Plane { get; }
        public string UniformLoadSet=>LoadSetName(Load);
        public static string LoadSetName(ExistingLoad load)=>load.Kind.StartsWith("Area uniform set: ",StringComparison.Ordinal)?load.Kind.Substring("Area uniform set: ".Length):"";
        public bool NamesOnly { get; private set; }
        public string Title => NamesOnly?UniformLoadSet: (UniformLoadSet.Length>0?UniformLoadSet+" · ":"")+Category+" · "+Geometry.Source.Name+" · "+Load.Pattern;
        public string DirectionText {
            get {
                if(Category=="Joint") {
                    bool global=string.Equals(Load.CoordinateSystem,"Global",StringComparison.OrdinalIgnoreCase);
                    var names=global?new[]{"Fx","Fy","Fz","Mx","My","Mz"}:new[]{"F1","F2","F3","M1","M2","M3"};
                    return Load.Direction>=1&&Load.Direction<=6?names[Load.Direction-1]:"Dir "+Load.Direction;
                }
                var directions=new[]{"","Local 1","Local 2","Local 3","X","Y","Z","X projected","Y projected","Z projected","Gravity","Gravity projected"};
                return Load.Direction>0&&Load.Direction<directions.Length?directions[Load.Direction]:"Dir "+Load.Direction;
            }
        }
        public string Text => NamesOnly?UniformLoadSet: (UniformLoadSet.Length>0?"["+UniformLoadSet+"] ":"")+Load.Pattern+": "+Values+" "+Load.Unit+" · "+Load.CoordinateSystem+" / "+DirectionText+(Category=="Joint"?" · step "+Load.Step:Category=="Frame"?" · r="+Load.Start.ToString("0.###",CultureInfo.CurrentCulture)+(Load.Start!=Load.End?"→"+Load.End.ToString("0.###",CultureInfo.CurrentCulture):""):"");
        public string PlanLineLabel => Load.Pattern + " - " + Values + " " + Load.Unit;
        public string Detail => NamesOnly?UniformLoadSet: Text+"\n"+Load.Kind+(Category=="Frame"?" · I→J":"")+(LoadGlyphs.Direction(this).HasValue?"":" · chưa xác định hướng để vẽ");
        private string Values => Load.Value1.ToString("0.###",CultureInfo.CurrentCulture)+(Load.Value1!=Load.Value2?" → "+Load.Value2.ToString("0.###",CultureInfo.CurrentCulture):"");
        private ReadLoadDisplay(string category,ExistingLoad load,ProjectedElement geometry,ModelElement source,ViewPlane plane) { Category=category;Load=load;Geometry=geometry;ElementId=load.ElementId;Source=source;Plane=plane; }
        public static IReadOnlyList<ReadLoadDisplay> Build(ModelSnapshot? snapshot,IReadOnlyList<ProjectedElement> scene,ViewPlane plane,string? pattern,
            bool frames,bool shells,bool joints,bool onlySelected,IReadOnlyList<string> selected,bool showZero,bool namesOnly=false)
        {
            if(snapshot==null||(!frames&&!shells&&!joints))return Array.Empty<ReadLoadDisplay>();
            var visible=scene.ToDictionary(i=>i.Source.Id,StringComparer.Ordinal);var selectedSet=new HashSet<string>(selected,StringComparer.Ordinal);var result=new List<ReadLoadDisplay>();
            foreach(var load in snapshot.Loads) {
                if(pattern!=null&&load.Pattern!=pattern || onlySelected&&!selectedSet.Contains(load.ElementId) || !showZero&&load.Value1==0&&load.Value2==0 || !visible.TryGetValue(load.ElementId,out var item))continue;
                string category=item.Source.Kind==ElementKind.Joint?"Joint":item.Source.Vertices.Count==2?"Frame":"Shell";
                if(category=="Frame"&&!frames||category=="Shell"&&!shells||category=="Joint"&&!joints)continue;
                ProjectedElement? geometry=category=="Shell"?Projection.Build(item.Source,plane):item;
                if(category=="Frame") {
                    var source=item.Source;var a=source.Vertices[0];var d=source.Vertices[1]-a;
                    var points=load.Start==load.End?new[]{a+d*load.Start}:new[]{a+d*load.Start,a+d*load.End};
                    geometry=Projection.Build(new ModelElement(source.Id,source.Name,source.Label,source.Story,source.Property,source.Kind,points),plane);
                }
                if(geometry!=null)result.Add(new ReadLoadDisplay(category,load,geometry,item.Source,plane){NamesOnly=namesOnly});
            }
            return result.OrderBy(r=>r.Category,StringComparer.Ordinal).ThenBy(r=>r.ElementId,StringComparer.Ordinal).ThenBy(r=>r.Load.Pattern,StringComparer.Ordinal).ThenBy(r=>r.Load.Step).ThenBy(r=>r.Load.Direction).ToArray();
        }
    }
}
