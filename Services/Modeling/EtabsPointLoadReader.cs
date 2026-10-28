using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSiAPIv1;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Services.Modeling
{
    public sealed partial class EtabsSnapshotReader
    {
        private static void ReadFramePointLoads(cSapModel sap,ModelElement e,UnitScale scale,List<ExistingLoad> loads,Action<int,string> error)
        {
            int n=0;string[] names=Array.Empty<string>(),patterns=Array.Empty<string>(),systems=Array.Empty<string>();
            int[] types=Array.Empty<int>(),directions=Array.Empty<int>();double[] relative=Array.Empty<double>(),distance=Array.Empty<double>(),values=Array.Empty<double>();
            int rc=sap.FrameObj.GetLoadPoint(e.Name,ref n,ref names,ref patterns,ref types,ref systems,ref directions,ref relative,ref distance,ref values,eItemType.Objects);
            if(rc!=0){error(rc,"Chưa xác nhận tải tập trung frame "+e.Name);return;}
            Arrays(n,names,patterns,types,systems,directions,relative,distance,values);
            for(int i=0;i<n;i++) {
                if(names[i]!=e.Name)throw new InvalidOperationException("Tải point thuộc frame khác: "+e.Name);
                if(types[i]!=1&&types[i]!=2){error(-1,"Loại tải point chưa hỗ trợ: "+e.Name);continue;}
                double value=values[i]*scale.PointFactor(types[i]==2);
                loads.Add(new ExistingLoad(e.Id,patterns[i],types[i]==1?"Frame point force":"Frame point moment",systems[i],directions[i],relative[i],relative[i],value,value,types[i]==1?"kN":"kN·m"));
            }
        }
        private static async Task ReadJointLoadsAsync(cSapModel sap,UnitScale scale,List<ModelElement> elements,List<ExistingLoad> loads,
            Func<string,Vec3> point,Action<int,string> error,Func<string,Task> yield,Action guard)
        {
            var known=new HashSet<string>(StringComparer.Ordinal);
            async Task<int> Read(string target,eItemType type) {
                int n=0;string[] names=Array.Empty<string>(),patterns=Array.Empty<string>(),systems=Array.Empty<string>();int[] steps=Array.Empty<int>();
                double[] f1=Array.Empty<double>(),f2=Array.Empty<double>(),f3=Array.Empty<double>(),m1=Array.Empty<double>(),m2=Array.Empty<double>(),m3=Array.Empty<double>();
                int rc=sap.PointObj.GetLoadForce(target,ref n,ref names,ref patterns,ref steps,ref systems,ref f1,ref f2,ref f3,ref m1,ref m2,ref m3,type);
                if(rc!=0)return rc;
                Arrays(n,names,patterns,systems,steps,f1,f2,f3,m1,m2,m3);
                for(int i=0;i<n;i++) {
                    guard();string name=names[i];
                    if(type==eItemType.Objects && name!=target)throw new InvalidOperationException("Tải nút thuộc đối tượng khác: "+target);
                    if(known.Add(name)) {
                        string label="",story="";error(sap.PointObj.GetLabelFromName(name,ref label,ref story),"Chưa đọc được label/story joint "+name);
                        elements.Add(new ModelElement("Joint:"+name,name,label,story,"",ElementKind.Joint,new[]{point(name)}));
                    }
                    var values=new[]{f1[i],f2[i],f3[i],m1[i],m2[i],m3[i]};
                    for(int component=0;component<6;component++) {
                        bool moment=component>=3;double value=values[component]*scale.PointFactor(moment);
                        loads.Add(new ExistingLoad("Joint:"+name,patterns[i],moment?"Joint moment":"Joint force",systems[i],component+1,0,0,value,value,moment?"kN·m":"kN",steps[i]));
                    }
                    if(i%100==0)await yield("Đọc tải nút "+(i+1)+"/"+n);
                }
                return 0;
            }
            int bulk=await Read("ALL",eItemType.Group);
            if(bulk==0)return;
            int count=0;string[] points=Array.Empty<string>();int code=sap.PointObj.GetNameList(ref count,ref points);
            if(code!=0){error(code,"Chưa đọc được danh sách nút; không coi tải nút thiếu là 0");return;}
            int processed=0;
            foreach(string name in Names(code,count,points,"PointObj.GetNameList")) {
                guard();error(await Read(name,eItemType.Objects),"Chưa xác nhận tải nút "+name);
                if(++processed%25==0)await yield("Đang đọc tải nút theo đối tượng…");
            }
        }
    }
}
