using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace MiniNote.Views;

public partial class ReminderDialog : Window
{
    public DateTime? Result { get; private set; }
    public bool WasCleared { get; private set; }

    public ReminderDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 设置现有的提醒时间
    /// </summary>
    public void SetExistingReminder(DateTime? reminderTime)
    {
        if (reminderTime.HasValue)
        {
            DpReminderDate.SelectedDate = reminderTime.Value.Date;
            TxtHour.Text = reminderTime.Value.Hour.ToString("00");
            TxtMinute.Text = reminderTime.Value.Minute.ToString("00");
            BtnClear.Visibility = Visibility.Visible;
        }
        else
        {
            DpReminderDate.SelectedDate = null;
            TxtHour.Text = DateTime.Now.Hour.ToString("00");
            TxtMinute.Text = "00";
            BtnClear.Visibility = Visibility.Collapsed;
        }
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

    private void QuickReminder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
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

            Result = reminderTime;
            DialogResult = true;
            Close();
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

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        WasCleared = true;
        DialogResult = true;
        Close();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!DpReminderDate.SelectedDate.HasValue)
        {
            MessageBox.Show("请选择日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtHour.Text, out int hour) || hour < 0 || hour > 23)
        {
            MessageBox.Show("请输入有效的小时 (0-23)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtMinute.Text, out int minute) || minute < 0 || minute > 59)
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

        Result = reminderTime;
        DialogResult = true;
        Close();
    }
}
