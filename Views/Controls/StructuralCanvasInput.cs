using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Views.Controls
{
    public sealed partial class StructuralCanvas
    {
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var p=e.GetPosition(this);double next=Math.Max(.0001,Math.Min(100000,scale*(e.Delta>0?1.2:1/1.2))),ratio=next/scale;
            ox=p.X-(p.X-ox)*ratio;oy=p.Y-(p.Y-oy)*ratio;scale=next;if(PickingPlanePoints) { snap=ModelSnap.Find(Items??Array.Empty<ProjectedElement>(),World(p),8/scale);cursorPlanePoint=snap?.Point??World(p); }InvalidateVisual();e.Handled=true;
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();var p=e.GetPosition(this);
            if(e.ChangedButton==MouseButton.Middle||e.ChangedButton==MouseButton.Right) { CancelInteraction();pan=p;CaptureMouse();Cursor=Cursors.Hand; }
            else if(e.ChangedButton==MouseButton.Left) {
                if(PickingPlanePoints) { snap=ModelSnap.Find(Items??Array.Empty<ProjectedElement>(),World(p),8/scale);cursorPlanePoint=snap?.Point??World(p);PlanePointPicked?.Invoke(cursorPlanePoint.Value); }
                else { selectionStart=p;selectionEnd=p;dragged=false;addSelection=(Keyboard.Modifiers&ModifierKeys.Control)!=0;CaptureMouse(); }
            }
            e.Handled=true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var p=e.GetPosition(this);
            if(pan.HasValue) { ox+=p.X-pan.Value.X;oy+=p.Y-pan.Value.Y;pan=p;InvalidateVisual(); }
            else if(selectionStart.HasValue&&IsMouseCaptured) { selectionEnd=p;if((p-selectionStart.Value).Length>4)dragged=true;InvalidateVisual(); }
            else if(PickingPlanePoints) { snap=ModelSnap.Find(Items??Array.Empty<ProjectedElement>(),World(p),8/scale);cursorPlanePoint=snap?.Point??World(p);InvalidateVisual(); }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var start=selectionStart;var end=e.GetPosition(this);bool box=dragged,add=addSelection;CancelInteraction();
            if(e.ChangedButton!=MouseButton.Left||!start.HasValue)return;var items=Items??Array.Empty<ProjectedElement>();
            if(box) {
                var rect=new Box2(World(start.Value),World(end));bool crossing=end.X<start.Value.X;
                SelectionRequested?.Invoke(items.Where(i=>ViewerSelection.InBox(i,rect,crossing)).Select(i=>i.Source.Id).ToArray(),add?SelectionChange.Add:SelectionChange.Replace);
            } else {
                var world=World(end);var hits=items.Where(i=>ViewerSelection.Hit(i,world,7/scale)).OrderBy(i=>i.Source.Kind==ElementKind.Joint?0:i.Source.Vertices.Count==2?1:2).ThenBy(i=>i.Segments.Min(s=>Projection.Distance(world,s))).ThenBy(i=>i.Source.Id,StringComparer.Ordinal).ToArray();
                int index=(Keyboard.Modifiers&ModifierKeys.Alt)!=0?Array.FindIndex(hits,i=>i.Source.Id==SelectedId)+1:0;
                if(hits.Length>0)SelectionRequested?.Invoke(new[]{hits[index%hits.Length].Source.Id},add?SelectionChange.Toggle:SelectionChange.Replace);
                else if(!add)SelectionRequested?.Invoke(Array.Empty<string>(),SelectionChange.Replace);
            }
            e.Handled=true;
        }
        public void CancelInteraction() { pan=null;selectionStart=null;selectionEnd=null;dragged=false;snap=null;cursorPlanePoint=null;if(IsMouseCaptured)ReleaseMouseCapture();Cursor=PickingPlanePoints?Cursors.Cross:Cursors.Arrow;InvalidateVisual(); }
        protected override void OnLostMouseCapture(MouseEventArgs e) { pan=null;selectionStart=null;selectionEnd=null;dragged=false;Cursor=PickingPlanePoints?Cursors.Cross:Cursors.Arrow;InvalidateVisual(); }
    }
}
