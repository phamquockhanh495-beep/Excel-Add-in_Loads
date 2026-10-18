using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Views.Controls
{
    public sealed partial class StructuralCanvas : FrameworkElement
    {
        private static DependencyProperty Register(string name,Type type,object? value=null) => DependencyProperty.Register(name,type,typeof(StructuralCanvas),new FrameworkPropertyMetadata(value,FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ItemsProperty=Register(nameof(Items),typeof(IReadOnlyList<ProjectedElement>));
        public static readonly DependencyProperty SelectedIdProperty=Register(nameof(SelectedId),typeof(string),"");
        public static readonly DependencyProperty SelectedIdsProperty=Register(nameof(SelectedIds),typeof(IReadOnlyList<string>));
        public static readonly DependencyProperty ShowLabelsProperty=Register(nameof(ShowLabels),typeof(bool),true);
        public static readonly DependencyProperty ShowSectionsProperty=Register(nameof(ShowSections),typeof(bool),false);
        public static readonly DependencyProperty ShowUniqueNamesProperty=Register(nameof(ShowUniqueNames),typeof(bool),false);
        public static readonly DependencyProperty HasOpeningsProperty=Register(nameof(HasOpenings),typeof(bool),false);
        public static readonly DependencyProperty ColorByPropertyProperty=Register(nameof(ColorByProperty),typeof(bool),false);
        public static readonly DependencyProperty ColorOverridesProperty=Register(nameof(ColorOverrides),typeof(Dictionary<string,string>));
        public static readonly DependencyProperty DraftOverlaysProperty=Register(nameof(DraftOverlays),typeof(IReadOnlyList<DraftOverlay>));
        public static readonly DependencyProperty ShowDraftValuesProperty=Register(nameof(ShowDraftValues),typeof(bool),true);
        public static readonly DependencyProperty ElevationProperty=DependencyProperty.Register(nameof(Elevation),typeof(bool),typeof(StructuralCanvas),new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.AffectsRender,(d,e)=>((StructuralCanvas)d).fitted=false));
        public static readonly DependencyProperty LevelsProperty=Register(nameof(Levels),typeof(IReadOnlyList<ModelLevel>));
        public IReadOnlyList<ProjectedElement>? Items { get=>(IReadOnlyList<ProjectedElement>?)GetValue(ItemsProperty);set=>SetValue(ItemsProperty,value); }
        public string SelectedId { get=>(string)GetValue(SelectedIdProperty);set=>SetValue(SelectedIdProperty,value); }
        public IReadOnlyList<string>? SelectedIds { get=>(IReadOnlyList<string>?)GetValue(SelectedIdsProperty);set=>SetValue(SelectedIdsProperty,value); }
        public bool ShowLabels { get=>(bool)GetValue(ShowLabelsProperty);set=>SetValue(ShowLabelsProperty,value); }
        public bool ShowSections { get=>(bool)GetValue(ShowSectionsProperty);set=>SetValue(ShowSectionsProperty,value); }
        public bool ShowUniqueNames { get=>(bool)GetValue(ShowUniqueNamesProperty);set=>SetValue(ShowUniqueNamesProperty,value); }
        public bool HasOpenings { get=>(bool)GetValue(HasOpeningsProperty);set=>SetValue(HasOpeningsProperty,value); }
        public bool ColorByProperty { get=>(bool)GetValue(ColorByPropertyProperty);set=>SetValue(ColorByPropertyProperty,value); }
        public Dictionary<string,string>? ColorOverrides { get=>(Dictionary<string,string>?)GetValue(ColorOverridesProperty);set=>SetValue(ColorOverridesProperty,value); }
        public IReadOnlyList<DraftOverlay>? DraftOverlays { get=>(IReadOnlyList<DraftOverlay>?)GetValue(DraftOverlaysProperty);set=>SetValue(DraftOverlaysProperty,value); }
        public bool ShowDraftValues { get=>(bool)GetValue(ShowDraftValuesProperty);set=>SetValue(ShowDraftValuesProperty,value); }
        public bool Elevation { get=>(bool)GetValue(ElevationProperty);set=>SetValue(ElevationProperty,value); }
        public IReadOnlyList<ModelLevel>? Levels { get=>(IReadOnlyList<ModelLevel>?)GetValue(LevelsProperty);set=>SetValue(LevelsProperty,value); }
        public event Action<IReadOnlyList<string>,SelectionChange>? SelectionRequested;
        public event Action<Vec2>? PlanePointPicked;
        public bool PickingPlanePoints { get;set; }
        public Vec2? FirstPlanePoint { get;set; }
        private double scale=50,ox=100,oy=300;
        private Point? pan,selectionStart,selectionEnd;
        private bool fitted,dragged,addSelection;
        private ModelSnap? snap;
        public StructuralCanvas() { ClipToBounds=true;Focusable=true; }
        private Point Screen(Vec2 p)=>new Point(ox+p.X*scale,oy-p.Y*scale);
        private Vec2 World(Point p)=>new Vec2((p.X-ox)/scale,(oy-p.Y)/scale);
        private static Vec2 V(Point p)=>new Vec2(p.X,p.Y);
        public void Fit()
        {
            var points=Items?.SelectMany(i=>i.Segments).SelectMany(s=>new[]{s.A,s.B}).ToArray();
            if((points==null||points.Length==0)&&Elevation&&Levels?.Count>0)points=Levels.Select(l=>new Vec2(0,l.Elevation)).ToArray();
            if(points==null||points.Length==0||ActualWidth<10||ActualHeight<10)return;
            double minX=points.Min(p=>p.X),maxX=points.Max(p=>p.X),minY=points.Min(p=>p.Y),maxY=points.Max(p=>p.Y);
            if(Elevation&&Levels?.Count>0) { minY=Math.Min(minY,Levels.Min(l=>l.Elevation));maxY=Math.Max(maxY,Levels.Max(l=>l.Elevation)); }
            double left=Elevation?115:45,right=35;
            scale=Math.Max(.0001,Math.Min(Math.Max(10,ActualWidth-left-right)/Math.Max(.5,maxX-minX),Math.Max(10,ActualHeight-90)/Math.Max(.5,maxY-minY)));
            ox=left+(ActualWidth-left-right)/2-(minX+maxX)/2*scale;oy=ActualHeight/2+(minY+maxY)/2*scale;
            fitted=true;InvalidateVisual();
        }
        private FormattedText Text(string text,Brush brush,double size=10.5)=>new FormattedText(text,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal),size,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.White,null,new Rect(RenderSize));if(!fitted&&Items?.Count>0)Fit();
            var items=Items??Array.Empty<ProjectedElement>();var selected=new HashSet<string>(SelectedIds??Array.Empty<string>(),StringComparer.Ordinal);
            if(Elevation)DrawStories(dc);
            bool openings=HasOpenings||items.Any(i=>i.Source.Kind==ElementKind.Opening);
            var viewport=new Box2(new Vec2(4,4),new Vec2(Math.Max(4,ActualWidth-4),Math.Max(4,ActualHeight-34)));
            var lineIndex=new LineIndex(ActualWidth,ActualHeight);var labels=new List<(ProjectedElement Item,Brush Brush,Segment2[] Lines,Vec2[] Polygon)>();
            foreach(var item in items.OrderBy(i=>i.Source.Vertices.Count==2?1:0).ThenBy(i=>selected.Contains(i.Source.Id)?1:0)) {
                var screenLines=item.Segments.Select(s=>new Segment2(V(Screen(s.A)),V(Screen(s.B)))).ToArray();
                var screenPolygon=item.Polygon.Select(p=>V(Screen(p))).ToArray();
                if(!screenLines.Any(s=>ViewerSelection.Intersects(s,viewport))&&!(screenPolygon.Length>2&&ViewerSelection.Contains(screenPolygon,new Vec2(ActualWidth/2,ActualHeight/2))))continue;
                string key=item.Source.Kind+"|"+item.Source.Property;
                string hex=ColorByProperty?ColorOverrides!=null&&ColorOverrides.TryGetValue(key,out var custom)?custom:PropertyColors.ForProperty(key):PropertyColors.ForKind(item.Source.Kind);
                var color=(Color)ColorConverter.ConvertFromString(hex);Brush brush=new SolidColorBrush(color);bool active=selected.Contains(item.Source.Id);
                var pen=new Pen(active?Brushes.DarkOrange:brush,active?3.5:item.Source.Vertices.Count==2?2:1.2);if(item.IsCut||item.Source.Kind==ElementKind.Opening)pen.DashStyle=DashStyles.Dash;
                if(!openings&&item.Polygon.Count>2) { var path=new StreamGeometry();using(var g=path.Open()) { g.BeginFigure(Screen(item.Polygon[0]),true,true);g.PolyLineTo(item.Polygon.Skip(1).Select(Screen).ToArray(),true,false); }dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(24,color.R,color.G,color.B)),null,path); }
                foreach(var s in screenLines) { dc.DrawLine(pen,new Point(s.A.X,s.A.Y),new Point(s.B.X,s.B.Y));if(item.Source.Vertices.Count==2) { var clipped=ViewerSelection.Clip(s,viewport);if(clipped.HasValue)lineIndex.Add(clipped.Value); } }
                labels.Add((item,active?Brushes.DarkOrange:brush,screenLines,screenPolygon));
            }
            if(ShowLabels||ShowSections||ShowUniqueNames)foreach(var entry in labels.OrderBy(e=>selected.Contains(e.Item.Source.Id)?0:1)) { string value=ElementLabels.Text(entry.Item.Source,ShowLabels,ShowSections,ShowUniqueNames);if(value.Length>0)DrawLabel(dc,entry.Lines,entry.Polygon,entry.Item.Source.Vertices.Count>2,value,entry.Brush,viewport,lineIndex); }
            foreach(var overlay in DraftOverlays??Array.Empty<DraftOverlay>()) {
                var pen=new Pen(Brushes.MediumPurple,3){DashStyle=DashStyles.Dash};foreach(var s in overlay.Geometry.Segments) { if(selected.Contains(overlay.ElementId))dc.DrawLine(new Pen(Brushes.DarkOrange,6),Screen(s.A),Screen(s.B));dc.DrawLine(pen,Screen(s.A),Screen(s.B)); }
                if(ShowDraftValues)DrawLabel(dc,overlay.Geometry.Segments.Select(s=>new Segment2(V(Screen(s.A)),V(Screen(s.B)))).ToArray(),overlay.Geometry.Polygon.Select(p=>V(Screen(p))).ToArray(),overlay.Geometry.Source.Vertices.Count>2,overlay.Text,pen.Brush,viewport,lineIndex);
            }
            if(selectionStart.HasValue&&selectionEnd.HasValue&&dragged) { var rect=new Rect(selectionStart.Value,selectionEnd.Value);bool crossing=selectionEnd.Value.X<selectionStart.Value.X;var color=crossing?Colors.SeaGreen:Colors.RoyalBlue;var pen=new Pen(new SolidColorBrush(color),1){DashStyle=crossing?DashStyles.Dash:DashStyles.Solid};dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(25,color.R,color.G,color.B)),pen,rect); }
            if(FirstPlanePoint.HasValue)dc.DrawEllipse(Brushes.White,new Pen(Brushes.Teal,2),Screen(FirstPlanePoint.Value),5,5);
            if(PickingPlanePoints&&snap!=null) { var p=Screen(snap.Point);dc.DrawRectangle(Brushes.White,new Pen(Brushes.Teal,2),new Rect(p.X-4,p.Y-4,8,8));string name=items.FirstOrDefault(i=>i.Source.Id==snap.ElementId)?.Source.Name??"";dc.DrawText(Text(snap.Kind+" · "+name,Brushes.Teal),new Point(Math.Min(Math.Max(4,p.X+9),Math.Max(4,ActualWidth-180)),Math.Max(4,p.Y-22))); }
            dc.DrawText(Text(items.Count==0?"Chưa có hình học trong vùng xem":"U →   V ↑ · Chọn "+selected.Count+(openings?" · Có opening: chỉ vẽ biên":""),Brushes.SlateGray,11),new Point(12,Math.Max(5,ActualHeight-26)));
        }
        private void DrawLabel(DrawingContext dc,IReadOnlyList<Segment2> lines,IReadOnlyList<Vec2> polygon,bool area,string value,Brush brush,Box2 viewport,LineIndex index)
        {
            var text=Text(value,brush);
            var placed=MemberLabelLayout.Place(lines,polygon,area,text.WidthIncludingTrailingWhitespace,text.Height,viewport,index.Blocks);if(placed==null)return;
            dc.PushTransform(new TranslateTransform(placed.Center.X,placed.Center.Y));dc.PushTransform(new RotateTransform(placed.AngleDegrees));
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(225,255,255,255)),null,new Rect(-placed.Width/2-1,-placed.Height/2,placed.Width+2,placed.Height),1,1);
            dc.DrawText(text,new Point(-placed.Width/2,-placed.Height/2));dc.Pop();dc.Pop();index.AddLabel(placed.Bounds);
        }
        private void DrawStories(DrawingContext dc)
        {
            var levels=Levels??Array.Empty<ModelLevel>();if(levels.Count==0)return;var pen=new Pen(Brushes.LightSlateGray,.7){DashStyle=DashStyles.Dash};double previousY=double.NegativeInfinity;
            foreach(var level in levels.OrderByDescending(l=>l.Elevation)) { double y=Screen(new Vec2(0,level.Elevation)).Y;if(y<6||y>ActualHeight-30)continue;dc.DrawLine(pen,new Point(102,y),new Point(Math.Max(103,ActualWidth-12),y));if(y-previousY<24)continue;var text=Text(level.Name+"\n"+level.Elevation.ToString("0.###",CultureInfo.CurrentCulture)+" m",Brushes.SlateGray,10);text.MaxTextWidth=90;dc.DrawText(text,new Point(6,y-text.Height/2));previousY=y; }
            dc.DrawLine(new Pen(Brushes.LightSlateGray,.7),new Point(98,10),new Point(98,Math.Max(10,ActualHeight-32)));
        }
        private sealed class LineIndex
        {
            private readonly Dictionary<(int,int),List<Segment2>> cells=new Dictionary<(int,int),List<Segment2>>();private readonly Dictionary<(int,int),List<Box2>> labels=new Dictionary<(int,int),List<Box2>>();private readonly int columns,rows;
            internal LineIndex(double width,double height) { columns=Math.Max(1,(int)Math.Ceiling(width/64));rows=Math.Max(1,(int)Math.Ceiling(height/64)); }
            private IEnumerable<(int,int)> Cells(Box2 b) { int x0=Math.Max(0,Math.Min(columns-1,(int)Math.Floor(b.Left/64))),x1=Math.Max(0,Math.Min(columns-1,(int)Math.Floor(b.Right/64))),y0=Math.Max(0,Math.Min(rows-1,(int)Math.Floor(b.Bottom/64))),y1=Math.Max(0,Math.Min(rows-1,(int)Math.Floor(b.Top/64)));for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)yield return (x,y); }
            internal void Add(Segment2 s) { foreach(var key in Cells(new Box2(s.A,s.B))) { if(!cells.TryGetValue(key,out var list))cells.Add(key,list=new List<Segment2>());list.Add(s); } }
            internal void AddLabel(Box2 box) { foreach(var key in Cells(box)) { if(!labels.TryGetValue(key,out var list))labels.Add(key,list=new List<Box2>());list.Add(box); } }
            internal bool Blocks(MemberLabelPlacement label) { foreach(var key in Cells(label.Bounds.Inflate(2))) { if(cells.TryGetValue(key,out var list)&&list.Any(s=>label.Intersects(s)))return true;if(labels.TryGetValue(key,out var boxes)&&boxes.Any(b=>b.Intersects(label.Bounds.Inflate(2))))return true; }return false; }
        }
    }
}
