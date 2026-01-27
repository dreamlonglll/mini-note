using System.Windows;
using MiniNote.Helpers;

namespace MiniNote;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Debug 模式下初始化控制台
        Logger.InitializeConsole();
        Logger.Info("Application starting...");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");
        Logger.CloseConsole();

        base.OnExit(e);
    }
}
