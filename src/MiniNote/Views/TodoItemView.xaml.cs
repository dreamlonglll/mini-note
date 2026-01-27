using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiniNote.ViewModels;

namespace MiniNote.Views;

public partial class TodoItemView : UserControl
{
    public TodoItemView()
    {
        InitializeComponent();
    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // 绑定已经处理了状态变更，这里只需要触发保存
        if (DataContext is TodoItemViewModel vm)
        {
            vm.NotifyCompletedChanged();
        }
    }

    private void ItemBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && DataContext is TodoItemViewModel vm)
        {
            // 双击编辑
            vm.EditCommand.Execute(null);
            e.Handled = true;
        }
    }
}
