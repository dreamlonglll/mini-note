using Microsoft.EntityFrameworkCore;
using MiniNote.Data;
using MiniNote.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniNote.Services;

public class DatabaseService
{
    private readonly AppDbContext _context;

    public DatabaseService()
    {
        _context = new AppDbContext();
        _context.Database.EnsureCreated();
    }

    #region TodoItem 操作

    public async Task<List<TodoItem>> GetAllTodosAsync()
    {
        return await _context.TodoItems
            .Include(t => t.Category)
            .OrderBy(t => t.IsCompleted)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<TodoItem?> GetTodoByIdAsync(int id)
    {
        return await _context.TodoItems
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<TodoItem>> GetTodosByCategoryAsync(int categoryId)
    {
        return await _context.TodoItems
            .Include(t => t.Category)
            .Where(t => t.CategoryId == categoryId)
            .OrderBy(t => t.IsCompleted)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<List<TodoItem>> GetPendingRemindersAsync()
    {
        var now = DateTime.Now;
        return await _context.TodoItems
            .Where(t => !t.IsCompleted
                        && !t.IsReminded
                        && t.ReminderTime.HasValue
                        && t.ReminderTime.Value <= now)
            .ToListAsync();
    }

    public async Task<TodoItem> AddTodoAsync(TodoItem todo)
    {
        _context.TodoItems.Add(todo);
        await _context.SaveChangesAsync();
        return todo;
    }

    public async Task UpdateTodoAsync(TodoItem todo)
    {
        todo.UpdatedAt = DateTime.Now;
        _context.TodoItems.Update(todo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTodoAsync(int id)
    {
        var todo = await _context.TodoItems.FindAsync(id);
        if (todo != null)
        {
            _context.TodoItems.Remove(todo);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteCompletedTodosAsync()
    {
        var completedTodos = await _context.TodoItems
            .Where(t => t.IsCompleted)
            .ToListAsync();

        _context.TodoItems.RemoveRange(completedTodos);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Category 操作

    public async Task<List<TodoCategory>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task<TodoCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<TodoCategory> AddCategoryAsync(TodoCategory category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(TodoCategory category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Settings 操作

    public async Task<AppSettings> GetSettingsAsync()
    {
        return await _context.Settings.FirstOrDefaultAsync()
               ?? new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var existing = await _context.Settings.FirstOrDefaultAsync();
        if (existing != null)
        {
            existing.WindowX = settings.WindowX;
            existing.WindowY = settings.WindowY;
            existing.WindowWidth = settings.WindowWidth;
            existing.WindowHeight = settings.WindowHeight;
            existing.AutoStart = settings.AutoStart;
            existing.EmbedDesktop = settings.EmbedDesktop;
            existing.Opacity = settings.Opacity;
            existing.IsDarkTheme = settings.IsDarkTheme;
        }
        else
        {
            _context.Settings.Add(settings);
        }
        await _context.SaveChangesAsync();
    }

    #endregion
}
