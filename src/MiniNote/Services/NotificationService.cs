using Microsoft.Toolkit.Uwp.Notifications;
using MiniNote.Helpers;
using MiniNote.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiniNote.Services;

/// <summary>
/// 通知服务 - 处理 Windows Toast 通知
/// </summary>
public class NotificationService : IDisposable
{
    private readonly DatabaseService _dbService;
    private CancellationTokenSource? _cts;
    private Task? _reminderTask;

    public NotificationService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// 启动提醒检查任务
    /// </summary>
    public void StartReminderService()
    {
        Logger.Info("NotificationService: Starting reminder service");
        _cts = new CancellationTokenSource();
        _reminderTask = Task.Run(async () => await CheckRemindersLoop(_cts.Token));
    }

    /// <summary>
    /// 停止提醒检查任务
    /// </summary>
    public void StopReminderService()
    {
        Logger.Info("NotificationService: Stopping reminder service");
        _cts?.Cancel();
    }

    /// <summary>
    /// 循环检查需要提醒的待办项
    /// </summary>
    private async Task CheckRemindersLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendReminders();
                await Task.Delay(TimeSpan.FromSeconds(30), token); // 每30秒检查一次
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"NotificationService: Error in reminder loop - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 检查并发送提醒
    /// </summary>
    private async Task CheckAndSendReminders()
    {
        var dueReminders = await _dbService.GetPendingRemindersAsync();

        foreach (var todo in dueReminders)
        {
            SendToastNotification(todo);

            // 标记为已提醒
            todo.IsReminded = true;
            await _dbService.UpdateTodoAsync(todo);
            Logger.Info($"NotificationService: Sent reminder for todo #{todo.Id}");
        }
    }

    /// <summary>
    /// 发送 Toast 通知
    /// </summary>
    public void SendToastNotification(TodoItem todo)
    {
        try
        {
            var builder = new ToastContentBuilder()
                .AddArgument("action", "viewTodo")
                .AddArgument("todoId", todo.Id.ToString())
                .AddText("MiniNote 提醒")
                .AddText(todo.Content);

            // 添加优先级信息
            string priorityText = todo.Priority switch
            {
                2 => "高优先级",
                1 => "中优先级",
                _ => "低优先级"
            };
            builder.AddText(priorityText);

            // 添加操作按钮
            builder.AddButton(new ToastButton()
                .SetContent("标记完成")
                .AddArgument("action", "complete")
                .AddArgument("todoId", todo.Id.ToString()));

            builder.AddButton(new ToastButton()
                .SetContent("稍后提醒")
                .AddArgument("action", "snooze")
                .AddArgument("todoId", todo.Id.ToString()));

            builder.Show();
        }
        catch (Exception ex)
        {
            Logger.Error($"NotificationService: Failed to send toast - {ex.Message}");
        }
    }

    /// <summary>
    /// 发送简单通知
    /// </summary>
    public void SendSimpleNotification(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            Logger.Error($"NotificationService: Failed to send simple notification - {ex.Message}");
        }
    }

    /// <summary>
    /// 清除所有通知
    /// </summary>
    public void ClearAllNotifications()
    {
        try
        {
            ToastNotificationManagerCompat.History.Clear();
        }
        catch (Exception ex)
        {
            Logger.Error($"NotificationService: Failed to clear notifications - {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopReminderService();
        _cts?.Dispose();
        Logger.Info("NotificationService: Disposed");
    }
}
