using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNote.Models;
using MiniNote.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MiniNote.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DatabaseService _dbService;

    public MainViewModel()
    {
        _dbService = new DatabaseService();
        TodoItems = new ObservableCollection<TodoItemViewModel>();
        Categories = new ObservableCollection<TodoCategory>();
    }

    public MainViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        TodoItems = new ObservableCollection<TodoItemViewModel>();
        Categories = new ObservableCollection<TodoCategory>();
    }

    [ObservableProperty]
    private ObservableCollection<TodoItemViewModel> _todoItems;

    [ObservableProperty]
    private ObservableCollection<TodoCategory> _categories;

    [ObservableProperty]
    private TodoCategory? _selectedCategory;

    [ObservableProperty]
    private string _newTodoContent = string.Empty;

    [ObservableProperty]
    private int _newTodoPriority = 1;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PendingCount))]
    private int _completedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PendingCount))]
    private int _totalCount;

    public int PendingCount => TotalCount - CompletedCount;

    public async Task InitializeAsync()
    {
        IsLoading = true;

        try
        {
            // 加载分类
            var categories = await _dbService.GetAllCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            // 加载待办项
            await RefreshTodosAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshTodosAsync()
    {
        var todos = await _dbService.GetAllTodosAsync();

        TodoItems.Clear();
        foreach (var todo in todos)
        {
            var vm = CreateTodoViewModel(todo);
            TodoItems.Add(vm);
        }

        UpdateCounts();
    }

    [RelayCommand]
    private async Task AddTodoAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTodoContent))
            return;

        var todo = new TodoItem
        {
            Content = NewTodoContent.Trim(),
            Priority = NewTodoPriority,
            CategoryId = SelectedCategory?.Id,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _dbService.AddTodoAsync(todo);

        // 加载分类信息
        todo.Category = SelectedCategory;

        var vm = CreateTodoViewModel(todo);

        // 插入到合适的位置（未完成项的开头）
        int insertIndex = 0;
        for (int i = 0; i < TodoItems.Count; i++)
        {
            if (TodoItems[i].IsCompleted)
            {
                insertIndex = i;
                break;
            }
            if (TodoItems[i].Priority < todo.Priority)
            {
                insertIndex = i;
                break;
            }
            insertIndex = i + 1;
        }

        TodoItems.Insert(insertIndex, vm);

        // 清空输入
        NewTodoContent = string.Empty;
        NewTodoPriority = 1;

        UpdateCounts();
    }

    [RelayCommand]
    private async Task DeleteTodoAsync(TodoItemViewModel? todoVm)
    {
        if (todoVm == null) return;

        await _dbService.DeleteTodoAsync(todoVm.Id);
        TodoItems.Remove(todoVm);

        UpdateCounts();
    }

    [RelayCommand]
    private async Task ToggleCompleteAsync(TodoItemViewModel? todoVm)
    {
        if (todoVm == null) return;

        todoVm.IsCompleted = !todoVm.IsCompleted;
        await _dbService.UpdateTodoAsync(todoVm.Model);

        // 重新排序
        await RefreshTodosAsync();
    }

    [RelayCommand]
    private async Task UpdateTodoAsync(TodoItemViewModel? todoVm)
    {
        if (todoVm == null) return;

        await _dbService.UpdateTodoAsync(todoVm.Model);
    }

    [RelayCommand]
    private async Task AddCategoryAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var category = new TodoCategory
        {
            Name = name.Trim(),
            SortOrder = Categories.Count
        };

        await _dbService.AddCategoryAsync(category);
        Categories.Add(category);
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        var completedItems = TodoItems.Where(t => t.IsCompleted).ToList();

        foreach (var item in completedItems)
        {
            await _dbService.DeleteTodoAsync(item.Id);
            TodoItems.Remove(item);
        }

        UpdateCounts();
    }

    [RelayCommand]
    private void SetPriority(int priority)
    {
        NewTodoPriority = priority;
    }

    private TodoItemViewModel CreateTodoViewModel(TodoItem todo)
    {
        var vm = new TodoItemViewModel(todo);

        vm.CompletedChanged += async (sender) =>
        {
            await _dbService.UpdateTodoAsync(sender.Model);
            UpdateCounts();
        };

        vm.DeleteRequested += async (sender) =>
        {
            await DeleteTodoAsync(sender);
        };

        return vm;
    }

    private void UpdateCounts()
    {
        TotalCount = TodoItems.Count;
        CompletedCount = TodoItems.Count(t => t.IsCompleted);
    }
}
