using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniNote.Views;

public partial class ReminderPicker : UserControl, INotifyPropertyChanged
{
    public event EventHandler<DateTime?>? ReminderSelected;
    public event EventHandler? Cancelled;
    public event PropertyChangedEventHandler? PropertyChanged;

    private DateTime _selectedDate = DateTime.Today;
    private string _hour = DateTime.Now.Hour.ToString("00");
    private string _minute = "00";
    private bool _hasExistingReminder;

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            _selectedDate = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDate)));
        }
    }

    public string Hour
    {
        get => _hour;
        set
        {
            _hour = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hour)));
        }
    }

    public string Minute
    {
        get => _minute;
        set
        {
            _minute = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Minute)));
        }
    }

    public bool HasExistingReminder
    {
        get => _hasExistingReminder;
        set
        {
            _hasExistingReminder = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasExistingReminder)));
        }
    }

    public ReminderPicker()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>
    /// 设置现有的提醒时间
    /// </summary>
    public void SetExistingReminder(DateTime? reminderTime)
    {
        if (reminderTime.HasValue)
        {
            SelectedDate = reminderTime.Value.Date;
            Hour = reminderTime.Value.Hour.ToString("00");
            Minute = reminderTime.Value.Minute.ToString("00");
            HasExistingReminder = true;
        }
        else
        {
            SelectedDate = DateTime.Today;
            Hour = DateTime.Now.Hour.ToString("00");
            Minute = "00";
            HasExistingReminder = false;
        }
    }

    private void SetReminder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            DateTime reminderTime;
            var tag = btn.Tag.ToString();

            if (tag == "tomorrow")
            {
                reminderTime = DateTime.Today.AddDays(1).AddHours(9); // 明天早上9点
            }
            else if (int.TryParse(tag, out int minutes))
            {
                reminderTime = DateTime.Now.AddMinutes(minutes);
            }
            else
            {
                return;
            }

            ReminderSelected?.Invoke(this, reminderTime);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ReminderSelected?.Invoke(this, null);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(Hour, out int hour) || hour < 0 || hour > 23)
        {
            MessageBox.Show("请输入有效的小时 (0-23)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(Minute, out int minute) || minute < 0 || minute > 59)
        {
            MessageBox.Show("请输入有效的分钟 (0-59)", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reminderTime = SelectedDate.Date.AddHours(hour).AddMinutes(minute);

        if (reminderTime <= DateTime.Now)
        {
            MessageBox.Show("提醒时间必须在当前时间之后", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ReminderSelected?.Invoke(this, reminderTime);
    }

    private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 只允许数字输入
        e.Handled = !IsTextAllowed(e.Text);
    }

    private static bool IsTextAllowed(string text)
    {
        return Regex.IsMatch(text, @"^[0-9]+$");
    }
}
