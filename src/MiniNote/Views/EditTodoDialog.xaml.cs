using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiniNote.ViewModels;

namespace MiniNote.Views;

public partial class EditTodoDialog : Window
{
    private TodoItemViewModel? _todoVm;
    
    /// <summary>
    /// 是否请求删除
    /// </summary>
    public bool DeleteRequested { get; private set; }
    
    /// <summary>
    /// 新的提醒时间
    /// </summary>
    public DateTime? NewReminderTime { get; private set; }
    
    /// <summary>
    /// 提醒时间是否被修改
    /// </summary>
    public bool ReminderTimeChanged { get; private set; }

    public EditTodoDialog()
    {
        InitializeComponent();
    }

    public void SetTodoItem(TodoItemViewModel todoVm)
    {
        _todoVm = todoVm;
        TxtContent.Text = todoVm.Content;
        
        switch (todoVm.Priority)
        {
            case 0:
                RbLow.IsChecked = true;
                break;
            case 2:
                RbHigh.IsChecked = true;
                break;
            default:
                RbMedium.IsChecked = true;
                break;
        }

        // 设置提醒时间
        if (todoVm.ReminderTime.HasValue)
        {
            DpReminderDate.SelectedDate = todoVm.ReminderTime.Value.Date;
            TxtReminderHour.Text = todoVm.ReminderTime.Value.Hour.ToString("00");
            TxtReminderMinute.Text = todoVm.ReminderTime.Value.Minute.ToString("00");
            BtnClearReminder.Visibility = Visibility.Visible;
        }
        else
        {
            DpReminderDate.SelectedDate = null;
            TxtReminderHour.Text = DateTime.Now.Hour.ToString("00");
            TxtReminderMinute.Text = "00";
            BtnClearReminder.Visibility = Visibility.Collapsed;
        }

        BtnSave.IsEnabled = true;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TxtContent_TextChanged(object sender, TextChangedEventArgs e)
    {
        BtnSave.IsEnabled = !string.IsNullOrWhiteSpace(TxtContent.Text);
    }

    private void QuickReminder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            var tag = btn.Tag.ToString();
            DateTime reminderTime;

            if (tag == "tomorrow")
            {
                reminderTime = DateTime.Today.AddDays(1).AddHours(9);
            }
            else if (int.TryParse(tag, out int minutes))
            {
                reminderTime = DateTime.Now.AddMinutes(minutes);
            }
            else
            {
                return;
            }

            DpReminderDate.SelectedDate = reminderTime.Date;
            TxtReminderHour.Text = reminderTime.Hour.ToString("00");
            TxtReminderMinute.Text = reminderTime.Minute.ToString("00");
            BtnClearReminder.Visibility = Visibility.Visible;
        }
    }

    private void ClearReminder_Click(object sender, RoutedEventArgs e)
    {
        DpReminderDate.SelectedDate = null;
        TxtReminderHour.Text = DateTime.Now.Hour.ToString("00");
        TxtReminderMinute.Text = "00";
        BtnClearReminder.Visibility = Visibility.Collapsed;
    }

    private void DpReminderDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DpReminderDate.SelectedDate.HasValue)
        {
            BtnClearReminder.Visibility = Visibility.Visible;
        }
    }

    private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要删除这个待办事项吗？", "确认删除", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            DeleteRequested = true;
            DialogResult = true;
            Close();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_todoVm == null) return;

        var content = TxtContent.Text.Trim();
        if (string.IsNullOrEmpty(content))
        {
            MessageBox.Show("请输入待办内容", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 获取优先级
        int priority = 1;
        if (RbLow.IsChecked == true) priority = 0;
        else if (RbHigh.IsChecked == true) priority = 2;

        _todoVm.Content = content;
        _todoVm.Priority = priority;

        // 处理提醒时间
        if (DpReminderDate.SelectedDate.HasValue)
        {
            if (!int.TryParse(TxtReminderHour.Text, out int hour) || hour < 0 || hour > 23)
            {
                MessageBox.Show("请输入有效的小时 (0-23)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtReminderMinute.Text, out int minute) || minute < 0 || minute > 59)
            {
                MessageBox.Show("请输入有效的分钟 (0-59)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reminderTime = DpReminderDate.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);

            if (reminderTime <= DateTime.Now)
            {
                MessageBox.Show("提醒时间必须在当前时间之后", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewReminderTime = reminderTime;
            ReminderTimeChanged = true;
        }
        else
        {
            // 清除提醒时间
            if (_todoVm.ReminderTime.HasValue)
            {
                NewReminderTime = null;
                ReminderTimeChanged = true;
            }
        }

        DialogResult = true;
        Close();
    }
}
