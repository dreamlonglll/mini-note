using System;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using MiniNote.Helpers;
using MiniNote.Services;

namespace MiniNote;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static DatabaseService? _dbService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Debug 模式下初始化控制台
        Logger.InitializeConsole();
        Logger.Info("Application starting...");

        // 初始化数据库服务（用于通知激活处理）
        _dbService = new DatabaseService();

        // 注册通知激活处理
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            OnToastActivated(toastArgs.Argument);
        };
        Logger.Info("Toast notification handler registered");

        // 如果是从通知启动的
        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            Logger.Info("Application was activated from toast notification");
        }
    }

    private void OnToastActivated(string argument)
    {
        Logger.Info($"Toast activated with arguments: {argument}");

        // 解析参数
        var args = ToastArguments.Parse(argument);

        Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                var action = args.Contains("action") ? args["action"] : "";
                var todoIdStr = args.Contains("todoId") ? args["todoId"] : "";

                if (int.TryParse(todoIdStr, out int todoId) && _dbService != null)
                {
                    switch (action)
                    {
                        case "complete":
                            // 标记完成
                            var todo = await _dbService.GetTodoByIdAsync(todoId);
                            if (todo != null)
                            {
                                todo.IsCompleted = true;
                                await _dbService.UpdateTodoAsync(todo);
                                Logger.Info($"Todo #{todoId} marked as completed via notification");
                            }
                            break;

                        case "snooze":
                            // 稍后提醒（延后15分钟）
                            var snoozeTodo = await _dbService.GetTodoByIdAsync(todoId);
                            if (snoozeTodo != null)
                            {
                                snoozeTodo.ReminderTime = DateTime.Now.AddMinutes(15);
                                snoozeTodo.IsReminded = false;
                                await _dbService.UpdateTodoAsync(snoozeTodo);
                                Logger.Info($"Todo #{todoId} snoozed for 15 minutes");
                            }
                            break;

                        case "viewTodo":
                            // 显示主窗口
                            var mainWindow = Current.MainWindow;
                            if (mainWindow != null)
                            {
                                mainWindow.Show();
                                mainWindow.Activate();
                                Logger.Info("Main window activated from notification");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling toast activation: {ex.Message}");
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");

        try
        {
            // 清理通知
            ToastNotificationManagerCompat.Uninstall();
            Logger.Info("Toast notifications uninstalled");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error uninstalling toast notifications: {ex.Message}");
        }

        Logger.CloseConsole();
        base.OnExit(e);
    }
}
