#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Qks.Shared;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.ViewModels
{
    public sealed class WindUserEditorRow : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public event PropertyChangedEventHandler? PropertyChanged;
        private string Get([CallerMemberName]string key="")=>values.TryGetValue(key,out var v)?v:"";
        private void Set(string value,[CallerMemberName]string key=""){if(Get(key)==value)return;values[key]=value;PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(key));}
        public string Key {get;}
        public string Story {get=>Get();set=>Set(value);}
        public string Fx {get=>Get();set=>Set(value);}
        public string Fy {get=>Get();set=>Set(value);}
        public string Mz {get=>Get();set=>Set(value);}
        public string X {get=>Get();set=>Set(value);}
        public string Y {get=>Get();set=>Set(value);}
        public string Error=>"";
        public string this[string columnName] {get{if(new[]{"Fx","Fy","Mz","X","Y"}.Contains(columnName))try{StoryWindPlan.NumberValue(Get(columnName),columnName);}catch(Exception ex){return ex.Message;}return "";}}
        public WindUserEditorRow(UserWindRow row){Key=row.DiaphragmKey;Fx=F(row.Fx);Fy=F(row.Fy);Mz=F(row.Mz);X=F(row.X);Y=F(row.Y);}
        private static string F(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
        public UserWindRow Load()=>new UserWindRow(Key,StoryWindPlan.NumberValue(Fx,"Fx"),StoryWindPlan.NumberValue(Fy,"Fy"),StoryWindPlan.NumberValue(Mz,"Mz"),StoryWindPlan.NumberValue(X,"X"),StoryWindPlan.NumberValue(Y,"Y"));
        public StoryWindInput Input(string pattern)=>new StoryWindInput{Target="UserLoads",Pattern=pattern,DiaphragmKey=Key,Story=Story,Fx=Fx,Fy=Fy,Mz=Mz,X=X,Y=Y};
    }
    public sealed class WindWorkspaceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Changed([CallerMemberName]string name="")=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
        public VisualWorkspaceViewModel Viewer {get;}=new VisualWorkspaceViewModel();
        public ModelSnapshot? Snapshot=>Viewer.Snapshot;
        public DraftWorkspace? Beams {get;private set;}
        public UserWindState? UserBaseline {get;private set;}
        public ObservableCollection<WindUserEditorRow> UserRows {get;}=new ObservableCollection<WindUserEditorRow>();
        private readonly Stack<StoryWindInput[]> userUndo=new Stack<StoryWindInput[]>(),userRedo=new Stack<StoryWindInput[]>();
        private StoryWindInput[] lastUsers=Array.Empty<StoryWindInput>();
        private bool restoringUsers;
        private string? editingBeamId;
        public string UserFingerprint {get;private set;}="";
        public string[] Directions {get;}={"Global-X","Global-Y"};
        public IReadOnlyList<string> Patterns {get;private set;}=Array.Empty<string>();
        public IReadOnlyList<DraftLoad> BeamRows=>Beams?.Loads??Array.Empty<DraftLoad>();
        public IReadOnlyList<DraftOverlay> BeamOverlays {get;private set;}=Array.Empty<DraftOverlay>();
        public IReadOnlyList<ReadLoadDisplay> PreviewLoads {get;private set;}=Array.Empty<ReadLoadDisplay>();
        public IReadOnlyList<ReadLoadDisplay> ExistingLoads {get;private set;}=Array.Empty<ReadLoadDisplay>();
        private string status="Kết nối ETABS rồi đọc mô hình. Đơn vị nhập: kN, m.";
        public string Status {get=>status;set{status=value;Changed();}}
        private bool busy,stale;
        public bool Busy {get=>busy;set{busy=value;Changed();Changed(nameof(CanEdit));Changed(nameof(CanApply));}}
        public bool CanEdit=>!Busy&&!stale&&Snapshot!=null;
        public bool CanApply=>CanEdit&&reviewed!=null&&reviewed==Signature();
        private string userPattern="",beamPattern="",reviewedTab="";
        private string? reviewed;
        public string UserPattern {get=>userPattern;set{userPattern=value??"";Invalidate();Changed();}}
        public string BeamPattern {get=>beamPattern;set{beamPattern=value??"";Invalidate();Changed();RefreshOverlays();}}
        private int tab;
        public int Tab {get=>tab;set{tab=value;Invalidate();Changed();}}
        private bool mappingConfirmed;
        public bool MappingConfirmed {get=>mappingConfirmed;set{mappingConfirmed=value;Invalidate();Changed();}}
        public string Direction {get;set;}="Global-X";
        public string Q1 {get;set;}="";
        public string Q2 {get;set;}="";
        public string Start {get;set;}="0";
        public string End {get;set;}="1";
        public bool Relative {get;set;}=true;
        public bool Append {get;set;}
        public string BeamSummary=>Snapshot==null||Beams==null?"Chưa có bộ tải.":$"{BeamRows.Count} đoạn tải · O=(0,0)\n"+string.Join("\n",BeamRows.GroupBy(l=>l.Pattern).Select(g=>g.Key+": "+WindBeamEditing.Resultant(Snapshot,g)));
        public string UserSummary {get{try{foreach(var row in UserRows)row.Load();return UserRows.Count+" khóa · Giá trị hợp lệ · Chưa tổng hợp lực khi API chưa phân biệt Load Sets.";}catch{return "Có ô chưa nhập hoặc sai định dạng số.";}}}
        public string SelectionSummary=>Viewer.SelectedElements.Count(e=>e.Kind==ElementKind.Beam)+" dầm đang chọn · chỉ gán các dầm hiện trong vùng xem";
        public WindWorkspaceViewModel()
        {
            Viewer.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(Viewer.Scene))RefreshOverlays();if(e.PropertyName==nameof(Viewer.SelectedIds)){editingBeamId=null;Changed(nameof(SelectionSummary));}};
        }
        public void Invalidate(){reviewed=null;Changed(nameof(CanApply));}
        public void MarkStale(){stale=true;Invalidate();Changed(nameof(CanEdit));Status="Nguồn đã đổi hoặc vừa ghi tải. Đọc lại ETABS trước lần ghi tiếp; dữ liệu soạn vẫn có thể lưu.";}
        public void SetSnapshot(ModelSnapshot snapshot,IEnumerable<string> windPatterns)
        {
            if(snapshot.Source!="ETABS")throw new InvalidOperationException("Module gán gió chỉ hỗ trợ ETABS.");
            Beams=null;BeamOverlays=Array.Empty<DraftOverlay>();PreviewLoads=Array.Empty<ReadLoadDisplay>();ExistingLoads=Array.Empty<ReadLoadDisplay>();
            Viewer.SetSnapshot(snapshot,false);Beams=new DraftWorkspace(snapshot);UserBaseline=null;UserRows.Clear();userUndo.Clear();userRedo.Clear();lastUsers=Array.Empty<StoryWindInput>();UserFingerprint="";MappingConfirmed=false;
            Patterns=windPatterns.ToArray();stale=false;Invalidate();
            if(!Patterns.Contains(UserPattern))UserPattern=Patterns.FirstOrDefault()??"";
            if(!Patterns.Contains(BeamPattern))BeamPattern=Patterns.FirstOrDefault()??"";
            Changed(nameof(Patterns));Changed(nameof(CanEdit));NotifyBeams();Changed(nameof(UserSummary));
        }
        public void SetUserBaseline(UserWindState state)
        {
            if(Snapshot==null)throw new InvalidOperationException("Đọc mô hình trước.");
            UserBaseline=state;UserFingerprint=SnapshotFingerprint.Of(Snapshot);UserPattern=state.Pattern;UserRows.Clear();MappingConfirmed=false;
            foreach(var load in state.Rows){var row=new WindUserEditorRow(load);row.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(row.Story))MappingConfirmed=false;if(!restoringUsers){userUndo.Push(lastUsers);userRedo.Clear();TrimHistory();lastUsers=CaptureUsers();}Invalidate();Changed(nameof(UserSummary));};UserRows.Add(row);}
            userUndo.Clear();userRedo.Clear();lastUsers=CaptureUsers();
            Invalidate();Changed(nameof(UserSummary));
        }
        private StoryWindInput[] CaptureUsers()=>UserRows.Select(r=>r.Input(UserBaseline?.Pattern??UserPattern)).ToArray();
        private void TrimHistory(){if(userUndo.Count<=100)return;var keep=userUndo.Take(100).Reverse().ToArray();userUndo.Clear();foreach(var entry in keep)userUndo.Push(entry);}
        private void SetUserValues(IEnumerable<StoryWindInput> values)
        {
            restoringUsers=true;
            try{foreach(var input in values){var row=UserRows.Single(r=>r.Key==input.DiaphragmKey);row.Story=input.Story;row.Fx=input.Fx;row.Fy=input.Fy;row.Mz=input.Mz;row.X=input.X;row.Y=input.Y;}}
            finally{restoringUsers=false;lastUsers=CaptureUsers();MappingConfirmed=false;Invalidate();Changed(nameof(UserSummary));}
        }
        public void UndoUsers(){RequireReady();if(userUndo.Count==0)return;userRedo.Push(CaptureUsers());SetUserValues(userUndo.Pop());}
        public void RedoUsers(){RequireReady();if(userRedo.Count==0)return;userUndo.Push(CaptureUsers());SetUserValues(userRedo.Pop());}
        public void StageSelected()
        {
            RequireReady();var visible=new HashSet<string>(Viewer.VisibleElements.Select(e=>e.Id));
            if(Viewer.SelectedIds.Any(id=>!visible.Contains(id)))throw new InvalidOperationException("Có đối tượng chọn ngoài vùng xem. Bỏ chọn rồi chọn lại dầm hiện trên mặt bằng.");
            var q=StoryWindPlan.NumberValue(Q1,"q1");
            var candidate=new DraftWorkspace(Snapshot!);candidate.ReplaceLoads(Beams!.Loads.Where(l=>l.Id!=editingBeamId));
            var result=WindBeamEditing.Stage(candidate,Viewer.SelectedIds,BeamPattern,Direction,q,string.IsNullOrWhiteSpace(Q2)?q:StoryWindPlan.NumberValue(Q2,"q2"),StoryWindPlan.NumberValue(Start,"Bắt đầu"),StoryWindPlan.NumberValue(End,"Kết thúc"),Relative,Append?DraftAction.Add:DraftAction.ReplaceMatching);
            Beams.ReplaceLoads(result);editingBeamId=null;
            NotifyBeams();Status="Đã cập nhật bộ tải chuẩn bị gán. Kiểm tra → Xem trước trước khi Gán ETABS.";
        }
        public void EditBeam(DraftLoad row)
        {
            if(Snapshot==null||!BeamRows.Any(l=>l.Id==row.Id))return;
            var target=Snapshot.Elements.Single(e=>e.Id==row.ElementId);Viewer.Level=Viewer.Levels.FirstOrDefault(l=>l.Name==target.Story)??Viewer.Level;
            Viewer.SelectElements(new[]{row.ElementId});BeamPattern=row.Pattern;Direction=row.DirectionName;Q1=row.Value1.ToString("R",CultureInfo.InvariantCulture);Q2=row.Value2.ToString("R",CultureInfo.InvariantCulture);Relative=true;Start=row.Start.ToString("R",CultureInfo.InvariantCulture);End=row.End.ToString("R",CultureInfo.InvariantCulture);Append=row.Action==DraftAction.Add;editingBeamId=row.Id;
            foreach(var name in new[]{nameof(Direction),nameof(Q1),nameof(Q2),nameof(Relative),nameof(Start),nameof(End),nameof(Append)})Changed(name);
            Status="Đang sửa đoạn tải đã chọn. Đưa vào bộ gán để cập nhật; chọn dầm trên hình để tạo đoạn mới.";
        }
        public void NotifyBeams(){Invalidate();Changed(nameof(BeamRows));Changed(nameof(BeamSummary));RefreshOverlays();}
        private void RefreshOverlays()
        {
            if(Snapshot==null||Beams==null)return;
            var plane=Viewer.CurrentPlane();var byId=Snapshot.Elements.ToDictionary(e=>e.Id);var visible=new HashSet<string>(Viewer.VisibleElements.Select(e=>e.Id));
            BeamOverlays=BeamRows.Where(l=>visible.Contains(l.ElementId)).Select(l=>DraftOverlay.Build(l,byId[l.ElementId],plane)).Where(o=>o!=null).Cast<DraftOverlay>().ToArray();
            var preview=new ModelSnapshot(1,Snapshot.Source,Snapshot.Version,Snapshot.Identity,"kN/m",Snapshot.CapturedUtc,Snapshot.Elements,Snapshot.Levels,BeamRows.Select(l=>l.AsLoad()),Array.Empty<string>(),Snapshot.LoadPatterns);
            PreviewLoads=ReadLoadDisplay.Build(preview,Viewer.Scene,plane,null,true,false,false,false,Array.Empty<string>(),true);
            ExistingLoads=ReadLoadDisplay.Build(Snapshot,Viewer.Scene,plane,BeamPattern,true,false,false,false,Array.Empty<string>(),true);
            Changed(nameof(BeamOverlays));Changed(nameof(PreviewLoads));Changed(nameof(ExistingLoads));
        }
        private void RequireReady(){if(!CanEdit)throw new InvalidOperationException("Đọc lại model ETABS trước khi chỉnh/gán.");}
        public UserWindDocument UserDocument()
        {
            RequireReady();if(UserBaseline==null||UserPattern!=UserBaseline.Pattern)throw new InvalidOperationException("Đọc User Loads của pattern đang chọn trước.");
            if(!MappingConfirmed)throw new InvalidOperationException("Đối chiếu khóa API và tầng trong ETABS, sau đó xác nhận ánh xạ trước khi gán.");
            var loads=UserRows.Select(r=>r.Load()).ToArray();
            if(loads.Length!=UserBaseline.Rows.Count)throw new InvalidOperationException("SDK hiện chỉ hỗ trợ sửa khóa đã đọc.");
            if(UserRows.Any(r=>!Snapshot!.Levels.Any(l=>l.Name==r.Story)))throw new InvalidOperationException("Chọn Story hợp lệ cho từng khóa diaphragm; không suy đoán từ tên D1.");
            var before=UserBaseline.Rows.ToDictionary(r=>r.DiaphragmKey);
            var changed=loads.Where(r=>JsonCodec.Serialize(r)!=JsonCodec.Serialize(before[r.DiaphragmKey])).ToArray();
            var doc=new UserWindDocument(Snapshot!.Identity,UserFingerprint,UserBaseline,changed,"QKS Wind · giá trị nhập trực tiếp; ánh xạ tầng do người dùng đối chiếu",storyAssignments:UserRows.ToDictionary(r=>r.Key,r=>r.Story));
            doc.Expected();if(changed.Length==0)throw new InvalidOperationException("Không có giá trị User Loads thay đổi.");return doc;
        }
        public DraftDocument BeamDocument(){RequireReady();if(Beams==null||BeamRows.Count==0)throw new InvalidOperationException("Chưa có tải dầm chuẩn bị gán.");return Beams.Document();}
        private string Signature()=>JsonCodec.Serialize(new{Tab,UserPattern,BeamPattern,MappingConfirmed,UserFingerprint,User=UserRows.Select(r=>r.Input(UserPattern)).ToArray(),Beams=Beams?.Document()});
        public void AcceptReview(){reviewedTab=Tab.ToString();reviewed=Signature();Changed(nameof(CanApply));}
        public void AssertReviewed(){if(!CanApply||reviewedTab!=Tab.ToString())throw new InvalidOperationException("Dữ liệu đã đổi. Kiểm tra và xem trước lại.");}
        public void ImportRows(IEnumerable<StoryWindInput> rows)
        {
            RequireReady();var input=rows.Where(r=>r.HasInput).ToArray();
            if(input.Length==0)throw new ArgumentException("Không có dòng tải.");
            if(input.Any(r=>!string.Equals(r.Target,Tab==0?"UserLoads":"Beam",StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Chỉ nhập dữ liệu của tab đang mở. Dùng sheet hoặc vùng Excel riêng cho phương thức này.");
            var plan=StoryWindPlan.Create(Beams!,input,Viewer.SelectedIds,UserBaseline,"QKS Wind · Excel");
            if(Tab==0){userUndo.Push(CaptureUsers());userRedo.Clear();TrimHistory();SetUserValues(plan.UserRows.Select(load=>{var row=new WindUserEditorRow(load){Story=plan.StoryAssignments[load.DiaphragmKey]};return row.Input(UserBaseline!.Pattern);}).ToArray());}
            else {Beams!.ReplaceLoads(plan.DraftLoads);NotifyBeams();}
            Invalidate();Status="Đã nhập dữ liệu; chưa ghi ETABS.";
        }
        public WindProject Project()=>new WindProject{Identity=Snapshot?.Identity??"",Beams=Beams?.Document(),UserBaseline=UserBaseline,UserRows=CaptureUsers(),UserFingerprint=UserFingerprint};
        public void Restore(WindProject project)
        {
            RequireReady();if(project.Version!=1||project.Identity!=Snapshot!.Identity)throw new ArgumentException("File gió không thuộc model đang đọc hoặc version chưa hỗ trợ.");
            var candidate=new DraftWorkspace(Snapshot);if(project.Beams!=null)candidate.Import(project.Beams);
            if(project.UserBaseline!=null&&project.UserFingerprint!=SnapshotFingerprint.Of(Snapshot))throw new ArgumentException("Nguồn đã đổi; chưa thể khôi phục User Loads từ bộ cũ.");
            if(project.UserBaseline!=null){
                if(project.UserRows.Length!=project.UserBaseline.Rows.Count||project.UserRows.Any(r=>r.Pattern!=project.UserBaseline.Pattern||r.Target!="UserLoads")||project.UserRows.Select(r=>r.DiaphragmKey).Distinct(StringComparer.Ordinal).Count()!=project.UserRows.Length||project.UserRows.Any(r=>!project.UserBaseline.Rows.Any(b=>b.DiaphragmKey==r.DiaphragmKey)))throw new ArgumentException("Danh sách khóa User Loads trong file không khớp baseline.");
                // Invalid/blank editing cells are retained on reopen, but cannot pass UserDocument validation.
                SetUserBaseline(project.UserBaseline);SetUserValues(project.UserRows);
            }
            else {UserBaseline=null;UserRows.Clear();UserFingerprint="";userUndo.Clear();userRedo.Clear();lastUsers=Array.Empty<StoryWindInput>();}
            Beams=candidate;NotifyBeams();MappingConfirmed=false;
        }
    }
}
