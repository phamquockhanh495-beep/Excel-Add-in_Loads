#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QksExcelAddIn.Modeling.Core
{
    /// <summary>Wind editing state is independent of the read-only load inventory viewer.</summary>
    public sealed class WindProject
    {
        public int Version { get; set; } = 1;
        public string Identity { get; set; } = "";
        public DraftDocument? Beams { get; set; }
        public UserWindState? UserBaseline { get; set; }
        public StoryWindInput[] UserRows { get; set; } = Array.Empty<StoryWindInput>();
        public string UserFingerprint { get; set; } = "";
    }
    public sealed class WindUiSettings
    {
        public int Tab { get; set; }
        public string Story { get; set; } = "";
        public string BeamPattern { get; set; } = "";
        public string UserPattern { get; set; } = "";
        public string Direction { get; set; } = "Global-X";
        public string Q1 { get; set; } = "";
        public string Q2 { get; set; } = "";
        public bool Grids { get; set; } = true;
        public bool Extrude { get; set; } = true;
    }
    public sealed class WindResultant
    {
        public double Fx { get; }
        public double Fy { get; }
        public double Mz { get; }
        public WindResultant(double fx,double fy,double mz) { Fx=fx;Fy=fy;Mz=mz; }
        public override string ToString()=>string.Format(CultureInfo.CurrentCulture,"Fx = {0:0.###} kN · Fy = {1:0.###} kN · Mz(O) = {2:0.###} kN·m",Fx,Fy,Mz);
    }
    public static class WindBeamEditing
    {
        public static IEnumerable<ExistingLoad> Outside(ExistingLoad load,double start,double end)
        {
            if(end<=load.Start||start>=load.End){yield return load;yield break;}
            double Value(double r)=>load.Value1+(load.Value2-load.Value1)*(r-load.Start)/(load.End-load.Start);
            ExistingLoad Part(double a,double b)=>new ExistingLoad(load.ElementId,load.Pattern,load.Kind,load.CoordinateSystem,load.Direction,a,b,Value(a),Value(b),load.Unit,load.Step);
            if(start>load.Start)yield return Part(load.Start,Math.Min(start,load.End));
            if(end<load.End)yield return Part(Math.Max(end,load.Start),load.End);
        }
        public static IReadOnlyList<DraftLoad> Stage(DraftWorkspace workspace,IEnumerable<string> selectedIds,string pattern,string direction,
            double q1,double q2,double start,double end,bool relative,DraftAction action)
        {
            if(!Number.Finite(start)||!Number.Finite(end)||!Number.Finite(q1)||!Number.Finite(q2))throw new ArgumentException("Đoạn tải và q phải là số hữu hạn.");
            int code=direction=="Global-X"?4:direction=="Global-Y"?5:throw new ArgumentException("Chọn Global-X hoặc Global-Y.");
            var ids=new HashSet<string>(selectedIds,StringComparer.Ordinal);
            var targets=workspace.Snapshot.Elements.Where(e=>ids.Contains(e.Id)).ToArray();
            if(targets.Length==0||targets.Length!=ids.Count||targets.Any(e=>e.Kind!=ElementKind.Beam||!(e.Length3D>1e-9)))
                throw new ArgumentException("Chọn ít nhất một dầm hợp lệ; không gán cùng cột, sàn hoặc vách.");
            var loads=workspace.Loads.ToList();
            foreach(var target in targets) {
                double a=relative?start:start/target.Length3D!.Value,b=relative?end:end/target.Length3D!.Value;
                var old=loads.SingleOrDefault(l=>l.Kind==DraftKind.WindLine&&l.ElementId==target.Id&&l.Pattern==pattern&&l.Direction==code&&l.Start==a&&l.End==b);
                if(old!=null)loads.Remove(old);
                loads.Add(new DraftLoad(old?.Id??Guid.NewGuid().ToString("N"),target.Id,DraftKind.WindLine,pattern,code,a,b,q1,q2,action,"QKS Wind · nhập q trực tiếp · chiều dài thực I→J"));
            }
            // Validate the whole batch before mutating the live workspace.
            var candidate=new DraftWorkspace(workspace.Snapshot);candidate.ReplaceLoads(loads);
            foreach(var group in candidate.Loads.GroupBy(l=>l.Pattern))Resultant(workspace.Snapshot,group);
            return candidate.Loads;
        }
        public static WindResultant Resultant(ModelSnapshot snapshot,IEnumerable<DraftLoad> source,double originX=0,double originY=0)
        {
            double fx=0,fy=0,mz=0;var elements=snapshot.Elements.ToDictionary(e=>e.Id,StringComparer.Ordinal);
            foreach(var load in source) {
                if(load.Kind!=DraftKind.WindLine||load.Direction!=4&&load.Direction!=5)throw new ArgumentException("Chỉ tính tổng tải đường gió Global X/Y.");
                var e=elements[load.ElementId];var d=e.Vertices[1]-e.Vertices[0];var a=e.Vertices[0]+d*load.Start;
                var delta=d*(load.End-load.Start);double length=delta.Length;
                double force=length*(load.Value1+load.Value2)/2;
                // Integral of t*q(t) remains valid when the net force is zero.
                double first=length*(load.Value1+2*load.Value2)/6;
                if(load.Direction==4){fx+=force;mz-=(a.Y-originY)*force+delta.Y*first;}
                else {fy+=force;mz+=(a.X-originX)*force+delta.X*first;}
            }
            if(!Number.Finite(fx)||!Number.Finite(fy)||!Number.Finite(mz))throw new ArgumentException("Tổng lực/mô men vượt phạm vi số. Kiểm tra giá trị tải và đơn vị.");
            return new WindResultant(fx,fy,mz);
        }
    }
}
