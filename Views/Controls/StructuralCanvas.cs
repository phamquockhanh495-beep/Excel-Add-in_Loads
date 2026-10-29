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
        public static readonly DependencyProperty ReadLoadItemsProperty=Register(nameof(ReadLoadItems),typeof(IReadOnlyList<ReadLoadDisplay>));
        public static readonly DependencyProperty ShowLoadValuesProperty=Register(nameof(ShowLoadValues),typeof(bool),true);
        public IReadOnlyList<ReadLoadDisplay>? ReadLoadItems { get=>(IReadOnlyList<ReadLoadDisplay>?)GetValue(ReadLoadItemsProperty);set=>SetValue(ReadLoadItemsProperty,value); }
        public bool ShowLoadValues { get=>(bool)GetValue(ShowLoadValuesProperty);set=>SetValue(ShowLoadValuesProperty,value); }
        public static readonly DependencyProperty StructureTextProperty=Register(nameof(StructureText),typeof(TextAppearance),TextAppearance.StructureDefault);
        public static readonly DependencyProperty LoadTextProperty=Register(nameof(LoadText),typeof(TextAppearance),TextAppearance.LoadsDefault);
        public TextAppearance StructureText { get=>(TextAppearance)GetValue(StructureTextProperty);set=>SetValue(StructureTextProperty,value); }
        public TextAppearance LoadText { get=>(TextAppearance)GetValue(LoadTextProperty);set=>SetValue(LoadTextProperty,value); }
        private FormattedText ModelText(string value,Brush brush,bool load=false)
        {
            var style=load?LoadText:StructureText;
            if(style.UseColor&&(load||!ReferenceEquals(brush,SelectionBrush)))brush=new SolidColorBrush(Color.FromRgb((byte)style.Red,(byte)style.Green,(byte)style.Blue));
            return Text(value,brush,style.Size);
        }
        public static readonly DependencyProperty SlabRegionsProperty=Register(nameof(SlabRegions),typeof(IReadOnlyList<SlabLoadRegion>));
        public IReadOnlyList<SlabLoadRegion>? SlabRegions {get=>(IReadOnlyList<SlabLoadRegion>?)GetValue(SlabRegionsProperty);set=>SetValue(SlabRegionsProperty,value);}
        private static readonly Brush SelectionBrush = new SolidColorBrush(Color.FromRgb(128,0,32));
        public static readonly DependencyProperty ExtrudeEnabledProperty=Register(nameof(ExtrudeEnabled),typeof(bool),true);
        public static readonly DependencyProperty CutStartProperty=Register(nameof(CutStart),typeof(Vec2?));
        public static readonly DependencyProperty CutEndProperty=Register(nameof(CutEnd),typeof(Vec2?));
        public bool ExtrudeEnabled { get=>(bool)GetValue(ExtrudeEnabledProperty);set=>SetValue(ExtrudeEnabledProperty,value); }
        public Vec2? CutStart { get=>(Vec2?)GetValue(CutStartProperty);set=>SetValue(CutStartProperty,value); }
        public Vec2? CutEnd { get=>(Vec2?)GetValue(CutEndProperty);set=>SetValue(CutEndProperty,value); }
        private Vec2? cursorPlanePoint;
        public static readonly DependencyProperty ItemsProperty=Register(nameof(Items),typeof(IReadOnlyList<ProjectedElement>));
        public static readonly DependencyProperty SelectedIdProperty=Register(nameof(SelectedId),typeof(string),"");
        public static readonly DependencyProperty SelectedIdsProperty=Register(nameof(SelectedIds),typeof(IReadOnlyList<string>));
        public static readonly DependencyProperty ShowLabelsProperty=Register(nameof(ShowLabels),typeof(bool),true);
        public static readonly DependencyProperty ShowSectionsProperty=Register(nameof(ShowSections),typeof(bool),false);
        public static readonly DependencyProperty ShowUniqueNamesProperty=Register(nameof(ShowUniqueNames),typeof(bool),false);
        public static readonly DependencyProperty LabelSettingsProperty=Register(nameof(LabelSettings),typeof(IReadOnlyList<LabelDisplaySetting>));
        public static readonly DependencyProperty HasOpeningsProperty=Register(nameof(HasOpenings),typeof(bool),false);
        public static readonly DependencyProperty ColorByPropertyProperty=Register(nameof(ColorByProperty),typeof(bool),false);
        public static readonly DependencyProperty ObjectColorsProperty=Register(nameof(ObjectColors),typeof(ObjectColorProfile),ObjectColorProfile.Default);
        public ObjectColorProfile ObjectColors { get=>(ObjectColorProfile)GetValue(ObjectColorsProperty);set=>SetValue(ObjectColorsProperty,value); }
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
        public IReadOnlyList<LabelDisplaySetting>? LabelSettings { get=>(IReadOnlyList<LabelDisplaySetting>?)GetValue(LabelSettingsProperty);set=>SetValue(LabelSettingsProperty,value); }
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
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(237,244,248)),null,new Rect(RenderSize));if(!fitted&&Items?.Count>0)Fit();
            var items=Items??Array.Empty<ProjectedElement>();var selected=new HashSet<string>(SelectedIds??Array.Empty<string>(),StringComparer.Ordinal);
            if(Elevation)DrawStories(dc);
            bool openings=HasOpenings||items.Any(i=>i.Source.Kind==ElementKind.Opening);
            var viewport=new Box2(new Vec2(4,4),new Vec2(Math.Max(4,ActualWidth-4),Math.Max(4,ActualHeight-34)));
            var slabColors=(SlabRegions??Array.Empty<SlabLoadRegion>()).ToDictionary(r=>r.ElementId);
            var labels=new List<(ProjectedElement Item,Brush Brush,Segment2[] Lines,Vec2[] Polygon)>();
            foreach(var item in items.OrderBy(i=>i.Source.Kind==ElementKind.Slab?0:i.Source.Kind==ElementKind.Wall?2:i.Source.Kind==ElementKind.Column?3:1).ThenBy(i=>selected.Contains(i.Source.Id)?1:0)) {
                var screenLines=item.Segments.Select(s=>new Segment2(V(Screen(s.A)),V(Screen(s.B)))).ToArray();
                var screenPolygon=item.Polygon.Select(p=>V(Screen(p))).ToArray();
                if(!screenLines.Any(s=>ViewerSelection.Intersects(s,viewport))&&!(screenPolygon.Length>2&&ViewerSelection.Contains(screenPolygon,new Vec2(ActualWidth/2,ActualHeight/2)))&&!item.Faces.Any(f=>ViewerSelection.Contains(f,World(new Point(ActualWidth/2,ActualHeight/2)))))continue;
                string key=item.Source.Kind+"|"+item.Source.Property;
                string hex=ColorByProperty?ColorOverrides!=null&&ColorOverrides.TryGetValue(key,out var custom)?custom:PropertyColors.ForProperty(key):ObjectColors.ForKind(item.Source.Kind);
                if(slabColors.TryGetValue(item.Source.Id,out var loadRegion))hex=loadRegion.Color;
                var color=(Color)ColorConverter.ConvertFromString(hex);Brush brush=new SolidColorBrush(color);bool active=selected.Contains(item.Source.Id);
                var pen=new Pen(active?SelectionBrush:brush,active?2.8:item.Faces.Count>0?1:1.2);if((item.IsCut&&item.Faces.Count==0)||item.Source.Kind==ElementKind.Opening)pen.DashStyle=DashStyles.Dash;
                if(active)color=Color.FromRgb(128,0,32);
                bool outlineOnly=(item.Source.Kind==ElementKind.Column||item.Source.Kind==ElementKind.Wall)&&(!ExtrudeEnabled||LabelSettings?.FirstOrDefault(s=>s.Kind==item.Source.Kind)?.Extrude==false);
                var fill=new SolidColorBrush(Color.FromArgb(item.Source.Kind==ElementKind.Slab?(loadRegion!=null?(byte)110:(byte)48):(byte)110,color.R,color.G,color.B));
                if(!outlineOnly&&item.Faces.Count>0) foreach(var face in item.Faces) {
                    Geometry geometry=Path(face);foreach(var hole in item.Voids)geometry=new CombinedGeometry(GeometryCombineMode.Exclude,geometry,Path(hole));dc.DrawGeometry(fill,null,geometry);
                }
                else if(!outlineOnly&&item.Polygon.Count>2) {
                    Geometry geometry=Path(item.Polygon);
                    foreach(var hole in item.Voids)geometry=new CombinedGeometry(GeometryCombineMode.Exclude,geometry,Path(hole));
                    dc.DrawGeometry(fill,null,geometry);
                }
                // Hide ordinary slab edges in Plan so they cannot be mistaken for beams.
                // Selection outlines and elevation thickness remain visible; hit testing uses geometry.
                if(Elevation||item.Source.Kind!=ElementKind.Slab||active)
                foreach(var line in screenLines) {
                    dc.DrawLine(pen,new Point(line.A.X,line.A.Y),new Point(line.B.X,line.B.Y));
                }
                if(item.Source.Vertices.Count<=2&&screenLines.All(l=>Math.Abs(l.A.X-l.B.X)+Math.Abs(l.A.Y-l.B.Y)<.1)&&screenLines.Length>0)
                    { var at=new Point(screenLines[0].A.X,screenLines[0].A.Y);if(item.Source.Kind==ElementKind.Joint)DrawCross(dc,pen,at,2.5);else dc.DrawEllipse(brush,pen,at,3,3); }
                var labelLines=item.AxisSegments.Select(line=>new Segment2(V(Screen(line.A)),V(Screen(line.B)))).ToArray();
                bool endOn=item.AxisSegments.All(l=>Math.Abs(l.A.X-l.B.X)+Math.Abs(l.A.Y-l.B.Y)<1e-9);
                labels.Add((item,active?SelectionBrush:new SolidColorBrush(Color.FromRgb(15,34,64)),endOn?screenLines:labelLines,screenPolygon));
            }
            var settings=LabelSettings?.ToDictionary(s=>s.Kind);
            foreach(var entry in labels.OrderBy(e=>selected.Contains(e.Item.Source.Id)?0:1)) { string value=settings!=null&&settings.TryGetValue(entry.Item.Source.Kind,out var setting)?setting.Text(entry.Item.Source):ElementLabels.Text(entry.Item.Source,ShowLabels,ShowSections,ShowUniqueNames);if(slabColors.TryGetValue(entry.Item.Source.Id,out var region))value=ShowLoadValues?region.Label:"";if(value.Length>0)DrawLabel(dc,entry.Lines,entry.Polygon,(entry.Item.Polygon.Count>2&&(entry.Item.Source.Vertices.Count>2&&entry.Item.Source.Kind!=ElementKind.Wall||entry.Item.Source.Kind==ElementKind.Column&&!Elevation)),value,entry.Brush,viewport,region!=null); }
            foreach(var overlay in DraftOverlays??Array.Empty<DraftOverlay>()) {
                var pen=new Pen(Brushes.MediumPurple,3){DashStyle=DashStyles.Dash};foreach(var s in overlay.Geometry.Segments) { if(selected.Contains(overlay.ElementId))dc.DrawLine(new Pen(SelectionBrush,6),Screen(s.A),Screen(s.B));dc.DrawLine(pen,Screen(s.A),Screen(s.B)); }
                if(ShowDraftValues)DrawLabel(dc,overlay.Geometry.Segments.Select(s=>new Segment2(V(Screen(s.A)),V(Screen(s.B)))).ToArray(),overlay.Geometry.Polygon.Select(p=>V(Screen(p))).ToArray(),overlay.Geometry.Source.Vertices.Count>2,overlay.Text,pen.Brush,viewport);
            }
            DrawReadLoads(dc,viewport);
            if(selectionStart.HasValue&&selectionEnd.HasValue&&dragged) { var rect=new Rect(selectionStart.Value,selectionEnd.Value);bool crossing=selectionEnd.Value.X<selectionStart.Value.X;var color=crossing?Colors.SeaGreen:Colors.RoyalBlue;var pen=new Pen(new SolidColorBrush(color),1){DashStyle=crossing?DashStyles.Dash:DashStyles.Solid};dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(25,color.R,color.G,color.B)),pen,rect); }
            DrawCutGuide(dc);
            if(FirstPlanePoint.HasValue)dc.DrawEllipse(Brushes.White,new Pen(Brushes.Teal,2),Screen(FirstPlanePoint.Value),5,5);
            if(PickingPlanePoints&&snap!=null) { var p=Screen(snap.Point);dc.DrawRectangle(Brushes.White,new Pen(Brushes.Teal,2),new Rect(p.X-4,p.Y-4,8,8));string name=items.FirstOrDefault(i=>i.Source.Id==snap.ElementId)?.Source.Name??"";dc.DrawText(Text(snap.Kind+" · "+name,Brushes.Teal),new Point(Math.Min(Math.Max(4,p.X+9),Math.Max(4,ActualWidth-180)),Math.Max(4,p.Y-22))); }
        }
        private void DrawCutGuide(DrawingContext dc)
        {
            if(Elevation||!PickingPlanePoints)return;
            var start=FirstPlanePoint;
            var end=cursorPlanePoint;
            if(!start.HasValue||!end.HasValue)return;
            var a=Screen(start.Value);var b=Screen(end.Value);
            dc.DrawLine(new Pen(SelectionBrush,1.8){DashStyle=DashStyles.Dash},a,b);
            dc.DrawEllipse(null,new Pen(SelectionBrush,1.5),a,4,4);dc.DrawEllipse(null,new Pen(SelectionBrush,1.5),b,4,4);
            dc.DrawText(Text("A",SelectionBrush,11),new Point(a.X+6,a.Y+4));dc.DrawText(Text("B",SelectionBrush,11),new Point(b.X+6,b.Y+4));
        }
        private StreamGeometry Path(IReadOnlyList<Vec2> points)
        {
            var path=new StreamGeometry();using(var g=path.Open()) { g.BeginFigure(Screen(points[0]),true,true);g.PolyLineTo(points.Skip(1).Select(Screen).ToArray(),true,false); }path.Freeze();return path;
        }
        private void DrawLabel(DrawingContext dc,IReadOnlyList<Segment2> lines,IReadOnlyList<Vec2> polygon,bool area,string value,Brush brush,Box2 viewport,bool load=false)
        {
            var text=ModelText(value,brush,load);
            var placed=MemberLabelLayout.Place(lines,polygon,area,text.WidthIncludingTrailingWhitespace,text.Height,viewport);if(placed==null)return;
            dc.PushTransform(new TranslateTransform(placed.Center.X,placed.Center.Y));dc.PushTransform(new RotateTransform(placed.AngleDegrees));
            dc.DrawText(text,new Point(-placed.Width/2,-placed.Height/2));dc.Pop();dc.Pop();
        }
        private void DrawStories(DrawingContext dc)
        {
            var levels=Levels??Array.Empty<ModelLevel>();if(levels.Count==0)return;var pen=new Pen(Brushes.LightSlateGray,.7){DashStyle=DashStyles.Dash};double previousY=double.NegativeInfinity;
            foreach(var level in levels.OrderByDescending(l=>l.Elevation)) { double y=Screen(new Vec2(0,level.Elevation)).Y;if(y<6||y>ActualHeight-30)continue;dc.DrawLine(pen,new Point(102,y),new Point(Math.Max(103,ActualWidth-12),y));if(y-previousY<24)continue;var text=Text(level.Name+"\n"+level.Elevation.ToString("0.###",CultureInfo.CurrentCulture)+" m",Brushes.SlateGray,10);text.MaxTextWidth=90;dc.DrawText(text,new Point(6,y-text.Height/2));previousY=y; }
            dc.DrawLine(new Pen(Brushes.LightSlateGray,.7),new Point(98,10),new Point(98,Math.Max(10,ActualHeight-32)));
        }
    }
}
