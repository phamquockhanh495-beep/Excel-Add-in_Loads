using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public static class SafeModelGeometry
    {
        // Geometric fallback only when SAFE does not expose design orientation / Story metadata.
        public static ElementKind Frame(Vec3 a,Vec3 b)
        {
            var d=b-a;if(d.Length<1e-8)return ElementKind.Other;
            if(Math.Abs(d.Z)<1e-6)return ElementKind.Beam;
            if(Math.Sqrt(d.X*d.X+d.Y*d.Y)<1e-6)return ElementKind.Column;
            return ElementKind.Brace;
        }
        public static ElementKind Area(IReadOnlyList<Vec3> vertices)
        {
            if(vertices.Count<3)return ElementKind.Other;
            if(vertices.Max(p=>p.Z)-vertices.Min(p=>p.Z)<1e-6)return ElementKind.Slab;
            var normal=new Vec3();for(int i=1;i+1<vertices.Count;i++)normal=normal+(vertices[i]-vertices[0]).Cross(vertices[i+1]-vertices[0]);
            return normal.Length>1e-8&&Math.Abs(normal.Z)/normal.Length<1e-6?ElementKind.Wall:ElementKind.Other;
        }
        public static ModelLevel[] Levels(IEnumerable<ModelElement> elements) => elements.SelectMany(e=>e.Vertices).Select(p=>Math.Round(p.Z,6)).Distinct().OrderBy(z=>z).Select(z=>new ModelLevel("Z="+z.ToString("0.######",CultureInfo.InvariantCulture)+" m",z)).ToArray();
        public static void AssignLevels(List<ModelElement> elements,IReadOnlyList<ModelLevel> levels)
        {
            for(int i=0;i<elements.Count;i++){
                var e=elements[i];if(e.Story.Length>0||e.Vertices.Count==0)continue;
                double z=e.Vertices.Max(p=>p.Z);var level=levels.OrderBy(l=>Math.Abs(l.Elevation-z)).FirstOrDefault();
                if(level==null||Math.Abs(level.Elevation-z)>1e-5)continue;
                elements[i]=new ModelElement(e.Id,e.Name,e.Label,level.Name,e.Property,e.Kind,e.Vertices,e.LocalAxisDegrees,e.AdvancedLocalAxes,e.Section,e.LocalAxes);
            }
        }
    }
}
