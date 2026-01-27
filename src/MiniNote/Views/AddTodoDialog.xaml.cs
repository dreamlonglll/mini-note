using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiniNote.Models;

namespace MiniNote.Views;

public partial class AddTodoDialog : Window
{
    public TodoItem? Result { get; private set; }

    public AddTodoDialog()
    {
        InitializeComponent();
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
        BtnAdd.IsEnabled = !string.IsNullOrWhiteSpace(TxtContent.Text);
    }

    private void ClearReminder_Click(object sender, RoutedEventArgs e)
    {
        DpReminderDate.SelectedDate = null;
        TxtReminderHour.Text = "09";
        TxtReminderMinute.Text = "00";
        BtnClearReminder.Visibility = Visibility.Collapsed;
    }

    private void DpReminderDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        BtnClearReminder.Visibility = DpReminderDate.SelectedDate.HasValue ? Visibility.Visible : Visibility.Collapsed;
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

    private void Add_Click(object sender, RoutedEventArgs e)
    {
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

        // 获取提醒时间
        DateTime? reminderTime = null;
        if (DpReminderDate.SelectedDate.HasValue)
        {
            if (int.TryParse(TxtReminderHour.Text, out int hour) &&
                int.TryParse(TxtReminderMinute.Text, out int minute) &&
                hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
            {
                reminderTime = DpReminderDate.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
                
                if (reminderTime <= DateTime.Now)
                {
                    MessageBox.Show("提醒时间必须在当前时间之后", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show("请输入有效的提醒时间", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        Result = new TodoItem
        {
            Content = content,
            Priority = priority,
            ReminderTime = reminderTime,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // 如果设置了截止日期，可以将其存储到备注或其他字段
        // 目前 TodoItem 模型中没有截止日期字段，这里暂时忽略
        // 如果需要，可以在 TodoItem 中添加 DueDate 字段

        DialogResult = true;
        Close();
    }
}
