using System;
using System.Linq;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
internal static partial class Program
{
    private static void ObjectColorTests()
    {
        Check("object colors: defaults preserve existing colors for every kind",()=>{var p=ObjectColorProfile.Default;foreach(ElementKind kind in Enum.GetValues(typeof(ElementKind)))Assert(p.ForKind(kind)==PropertyColors.ForKind(kind));});
        Check("object colors: independent kind overrides survive JSON including mode",()=>{
            var p=new ObjectColorProfile(1,true,new[]{new ObjectColor(ElementKind.Beam,255,0,1),new ObjectColor(ElementKind.Slab,1,128,255)});
            var copy=JsonCodec.Deserialize<ObjectColorProfile>(JsonCodec.Serialize(p));Assert(copy.ColorByProperty&&copy.ForKind(ElementKind.Beam)=="#FF0001"&&copy.ForKind(ElementKind.Slab)=="#0180FF"&&copy.ForKind(ElementKind.Column)==PropertyColors.ForKind(ElementKind.Column));
        });
        Check("object colors: reject invalid RGB, future schema, duplicate and unknown kinds",()=>{
            Throws<ArgumentException>(()=>new ObjectColor(ElementKind.Beam,-1,0,0));Throws<ArgumentException>(()=>new ObjectColor(ElementKind.Beam,0,256,0));Throws<ArgumentException>(()=>new ObjectColor(ElementKind.Beam,0,0,256));Throws<ArgumentException>(()=>new ObjectColor((ElementKind)999,0,0,0));
            var color=new ObjectColor(ElementKind.Beam,0,0,0);Throws<ArgumentException>(()=>new ObjectColorProfile(1,false,new[]{color,color}));Throws<ArgumentException>(()=>new ObjectColorProfile(2,false,new[]{color}));
        });
        Check("object color row: invalid draft input has safe swatch but cannot apply",()=>{var row=new ObjectColorRow(new ObjectColor(ElementKind.Wall,12,34,56),"Vách");Assert(row.PreviewColor=="#0C2238");row.Red="256";Assert(row.PreviewColor=="#E2E8F0");Throws<ArgumentException>(()=>row.ToColor());row.Red="3.5";Throws<ArgumentException>(()=>row.ToColor());row.Red="255";Assert(row.ToColor().Hex=="#FF2238");});
        Check("object color apply updates bindings only; property overrides and model stay intact",()=>{
            var vm=new VisualWorkspaceViewModel();vm.SetSnapshot(SelectFixture(),true);string before=JsonCodec.Serialize(vm.Snapshot);vm.ColorOverrides["Beam|P1"]="#123456";vm.SelectElements(new[]{"Frame:1"});
            var changes=new System.Collections.Generic.List<string>();vm.PropertyChanged+=(_,e)=>changes.Add(e.PropertyName);
            vm.ApplyObjectColors(new ObjectColorProfile(1,false,new[]{new ObjectColor(ElementKind.Beam,255,0,0)}));
            Assert(vm.ObjectColors.ForKind(ElementKind.Beam)=="#FF0000"&&!vm.ColorByProperty&&changes.Contains(nameof(vm.ObjectColors))&&changes.Contains(nameof(vm.ColorByProperty)));
            Assert(vm.ColorOverrides["Beam|P1"]=="#123456"&&JsonCodec.Serialize(vm.Snapshot)==before&&vm.SelectedId=="Frame:1");
            vm.ApplyObjectColors(new ObjectColorProfile(1,true,vm.ObjectColors.Colors));Assert(vm.ColorByProperty&&vm.ObjectColors.ForKind(ElementKind.Beam)=="#FF0000");
        });
        Check("object color profile survives model reread and resets explicitly",()=>{var vm=new VisualWorkspaceViewModel();vm.ApplyObjectColors(new ObjectColorProfile(1,false,new[]{new ObjectColor(ElementKind.Slab,255,100,20)}));vm.SetSnapshot(SelectFixture(),true);Assert(vm.ObjectColors.ForKind(ElementKind.Slab)=="#FF6414");vm.ApplyObjectColors(ObjectColorProfile.Default);Assert(vm.ObjectColors.ForKind(ElementKind.Slab)==PropertyColors.ForKind(ElementKind.Slab));});
    }
}
