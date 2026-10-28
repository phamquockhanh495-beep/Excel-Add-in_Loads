using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QksExcelAddIn.Modeling.Core;
using QksExcelAddIn.ViewModels;
namespace QksExcelAddIn.Views
{
    public partial class VisualWorkspaceSelectView : Window
    {
        private readonly VisualWorkspaceViewModel vm;
        private bool ready;
        private readonly ElementKind[] types={ElementKind.Column,ElementKind.Beam,ElementKind.Slab,ElementKind.Wall};
        public VisualWorkspaceSelectView(VisualWorkspaceViewModel model)
        {
            vm=model;InitializeComponent();WorkspacePopupChrome.Attach(this);
            TypeBox.ItemsSource=new[]{"Cột","Dầm","Sàn","Vách"};TypeBox.SelectedIndex=1;
            AttributeBox.ItemsSource=new[]{"Section","UniqueName","Label"};AttributeBox.SelectedIndex=0;
            ScopeBox.ItemsSource=new[]{"Vùng xem hiện tại","Toàn mô hình (trừ đối tượng ẩn)"};ScopeBox.SelectedIndex=0;
            MaxHeight=SystemParameters.WorkArea.Height*.9;MaxWidth=SystemParameters.WorkArea.Width*.9;
            ready=true;RefreshValues();
            vm.PropertyChanged+=ModelChanged;Closed+=(_,__)=>vm.PropertyChanged-=ModelChanged;
        }
        private void ModelChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e)
        { if(e.PropertyName==nameof(vm.SourceText))Close(); }
        private void RefreshValues()
        {
            if(!ready)return;
            ValuesBox.ItemsSource=vm.SelectionValues(types[TypeBox.SelectedIndex],(string)AttributeBox.SelectedItem,ScopeBox.SelectedIndex==0,SearchBox.Text);
            ResultText.Text=ValuesBox.Items.Count+" giá trị. Label có thể trùng giữa các tầng; kiểm tra phạm vi trước khi chọn.";
        }
        private void CriteriaChanged(object sender,SelectionChangedEventArgs e)=>RefreshValues();
        private void SearchChanged(object sender,TextChangedEventArgs e)=>RefreshValues();
        private void ApplySelection(object sender,RoutedEventArgs e)
        {
            var values=ValuesBox.SelectedItems.Cast<string>().ToArray();
            if(values.Length==0){ResultText.Text="Chọn ít nhất một giá trị trong danh sách.";return;}
            vm.SelectByAttribute(types[TypeBox.SelectedIndex],(string)AttributeBox.SelectedItem,values,ScopeBox.SelectedIndex==0,(string)((Button)sender).Tag);
            ResultText.Text=vm.Status;
        }
        private void CloseClick(object sender,RoutedEventArgs e)=>Close();
    }
}
