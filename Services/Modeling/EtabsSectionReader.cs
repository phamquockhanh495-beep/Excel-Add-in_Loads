using System;
using System.Collections.Generic;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Services.Modeling
{
    /// <summary>Cached per model read, in the caller's current units. Does not change ETABS units.</summary>
    internal sealed class EtabsSectionReader
    {
        private readonly cSapModel sap;
        private readonly double metres;
        private readonly Action<int,string> diagnostic;
        private readonly Dictionary<string,SectionGeometry?> cache = new Dictionary<string,SectionGeometry?>();
        internal EtabsSectionReader(cSapModel sap, double metres, Action<int,string> diagnostic) { this.sap=sap;this.metres=metres;this.diagnostic=diagnostic; }
        internal SectionGeometry? Read(string property, ElementKind kind, bool frame)
        {
            if (!frame && kind != ElementKind.Slab && kind != ElementKind.Wall) return null;
            string key=(frame?"Frame":kind.ToString())+"|"+property;
            if(cache.TryGetValue(key,out var cached))return cached;
            SectionGeometry? result=null;
            try {
                if(string.IsNullOrWhiteSpace(property)) throw new InvalidOperationException("Chưa có property.");
                int rc,color=0;string file="",material="",notes="",guid="";double depth=0,width=0;
                if(frame) {
                    eFramePropType type=default;rc=sap.PropFrame.GetTypeOAPI(property,ref type);
                    if(rc!=0)throw new InvalidOperationException("GetTypeOAPI: "+rc);
                    if(type==eFramePropType.Rectangular) {
                        rc=sap.PropFrame.GetRectangle(property,ref file,ref material,ref depth,ref width,ref color,ref notes,ref guid);
                        if(rc==0)result=new SectionGeometry("Rectangle",width*metres,depth*metres);
                    } else if(type==eFramePropType.Circle) {
                        rc=sap.PropFrame.GetCircle(property,ref file,ref material,ref depth,ref color,ref notes,ref guid);
                        if(rc==0)result=new SectionGeometry("Circle",depth*metres,depth*metres);
                    } else throw new InvalidOperationException("Chưa hỗ trợ hình tiết diện "+type+".");
                } else if(kind==ElementKind.Slab) {
                    eSlabType type=default;eShellType shell=default;
                    rc=sap.PropArea.GetSlab(property,ref type,ref shell,ref material,ref depth,ref color,ref notes,ref guid);
                    if(rc==0 && (type==eSlabType.Ribbed || type==eSlabType.Waffle || type==eSlabType.Stiff_DO_NOT_USE))
                        throw new InvalidOperationException("Sàn "+type+": chưa dựng hình gân/chiều dày biến đổi.");
                    if(rc==0)result=new SectionGeometry("Shell",depth*metres,depth*metres);
                } else if(kind==ElementKind.Wall) {
                    eWallPropType type=default;eShellType shell=default;
                    rc=sap.PropArea.GetWall(property,ref type,ref shell,ref material,ref depth,ref color,ref notes,ref guid);
                    if(rc==0 && type!=eWallPropType.Specified)throw new InvalidOperationException("Vách AutoSelect: chưa có chiều dày cố định.");
                    if(rc==0)result=new SectionGeometry("Shell",depth*metres,depth*metres);
                } else { cache[key]=null;return null; }
                if(rc!=0)throw new InvalidOperationException("API trả mã "+rc);
            } catch(Exception ex) { diagnostic(-1,"Extrude "+key+": giữ đường trục/biên. "+ex.Message); }
            cache[key]=result;return result;
        }
    }
}
