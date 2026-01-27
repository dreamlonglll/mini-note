using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNote.Models;
using System;
using System.Windows;

namespace MiniNote.ViewModels;

public partial class TodoItemViewModel : ObservableObject
{
    private readonly TodoItem _model;

    public TodoItemViewModel(TodoItem model)
    {
        _model = model;
        _isCompleted = model.IsCompleted;
        _content = model.Content;
        _priority = model.Priority;
        _reminderTime = model.ReminderTime;
        _categoryName = model.Category?.Name ?? "默认";
        _categoryColor = model.Category?.Color ?? "#007ACC";
    }

    public int Id => _model.Id;

    public TodoItem Model => _model;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrikethroughVisibility))]
    [NotifyPropertyChangedFor(nameof(ContentOpacity))]
    private bool _isCompleted;

    partial void OnIsCompletedChanged(bool value)
    {
        _model.IsCompleted = value;
        _model.UpdatedAt = DateTime.Now;
        CompletedChanged?.Invoke(this);
    }

    [ObservableProperty]
    private string _content = string.Empty;

    partial void OnContentChanged(string value)
    {
        _model.Content = value;
        _model.UpdatedAt = DateTime.Now;
    }

    [ObservableProperty]
    private int _priority;

    partial void OnPriorityChanged(int value)
    {
        _model.Priority = value;
    }

    [ObservableProperty]
    private DateTime? _reminderTime;

    partial void OnReminderTimeChanged(DateTime? value)
    {
        _model.ReminderTime = value;
    }

    [ObservableProperty]
    private string _categoryName = "默认";

    [ObservableProperty]
    private string _categoryColor = "#007ACC";

    public Visibility StrikethroughVisibility =>
        IsCompleted ? Visibility.Visible : Visibility.Collapsed;

    public double ContentOpacity => IsCompleted ? 0.5 : 1.0;

    public string PriorityText => Priority switch
    {
        2 => "高",
        1 => "中",
        _ => "低"
    };

    public string PriorityColor => Priority switch
    {
        2 => "#FF5252",
        1 => "#FFC107",
        _ => "#4CAF50"
    };

    public bool HasReminder => ReminderTime.HasValue;

    public string ReminderTimeText => ReminderTime?.ToString("MM/dd HH:mm") ?? "";

    // 事件
    public event Action<TodoItemViewModel>? CompletedChanged;
    public event Action<TodoItemViewModel>? DeleteRequested;
    public event Action<TodoItemViewModel>? EditRequested;

    [RelayCommand]
    private void Delete()
    {
        DeleteRequested?.Invoke(this);
    }

    [RelayCommand]
    private void Edit()
    {
        EditRequested?.Invoke(this);
    }

    [RelayCommand]
    private void ToggleComplete()
    {
        IsCompleted = !IsCompleted;
    }
}
