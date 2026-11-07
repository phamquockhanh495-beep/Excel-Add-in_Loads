#nullable enable
using System;
using System.Linq;
namespace QksExcelAddIn.Modeling.Core
{
    public sealed class StoryBand
    {
        public double Bottom {get;} public double Top {get;}
        private StoryBand(double bottom,double top){Bottom=bottom;Top=top;}
        public static StoryBand? For(ModelSnapshot? model,ModelLevel? level)
        {
            if(model==null||level==null)return null;
            var lower=model.Levels.Where(l=>l.Elevation<level.Elevation-1e-6).OrderByDescending(l=>l.Elevation).FirstOrDefault();
            // Lowest level has no implied story height: show its structure's true lower envelope.
            double bottom=lower?.Elevation??model.Elements.Where(e=>e.Vertices.Any(v=>Math.Abs(v.Z-level.Elevation)<1e-6)).SelectMany(e=>e.Vertices.Select(v=>v.Z-(e.Kind==ElementKind.Beam||e.Kind==ElementKind.Slab?e.Section?.Depth??0:0))).DefaultIfEmpty(level.Elevation-.15).Min();
            return new StoryBand(Math.Min(bottom,level.Elevation-1e-6),level.Elevation);
        }
        public bool Includes(ModelElement e)
        {
            if(e.Vertices.Count==0)return false;double min=e.Vertices.Min(p=>p.Z),max=e.Vertices.Max(p=>p.Z);
            if(max<=Bottom+1e-7||min>Top+1e-7)return false;
            return !(min>=Top-1e-7&&max>Top+1e-7);
        }
    }
}
