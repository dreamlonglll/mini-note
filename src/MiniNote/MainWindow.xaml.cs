using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiniNote.Helpers;
using MiniNote.Models;
using MiniNote.Services;
using MiniNote.ViewModels;

namespace MiniNote;

public partial class MainWindow : Window
{
    private readonly DesktopEmbedService _embedService;
    private readonly DatabaseService _dbService;
    private readonly MainViewModel _viewModel;
    private readonly TrayIconService _trayService;
    private AppSettings _settings = null!;
    private bool _isClosing = false;

    public MainWindow()
    {
        InitializeComponent();
        Logger.Info("MainWindow initialized");

        _dbService = new DatabaseService();
        _embedService = new DesktopEmbedService();
        _trayService = new TrayIconService();
        _viewModel = new MainViewModel(_dbService);

        DataContext = _viewModel;

        // 初始化系统托盘
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _trayService.Initialize();

        _trayService.OnShowWindow += () =>
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
        };

        _trayService.OnToggleEmbed += () =>
        {
            Dispatcher.Invoke(() =>
            {
                BtnPin_Click(this, new RoutedEventArgs());
            });
        };

        _trayService.OnExit += () =>
        {
            Dispatcher.Invoke(() =>
            {
                _isClosing = true;
                Close();
            });
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Logger.Info("Window_Loaded started");

        // 加载设置
        _settings = await _dbService.GetSettingsAsync();
        Logger.Info($"Settings loaded: EmbedDesktop={_settings.EmbedDesktop}");

        // 应用窗口位置和大小
        Left = _settings.WindowX;
        Top = _settings.WindowY;
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;

        // 启用磨砂效果
        bool acrylicEnabled = AcrylicHelper.EnableAcrylic(this);
        AcrylicHelper.SetDarkMode(this, true);
        Logger.Info($"Acrylic effect: {(acrylicEnabled ? "enabled" : "not available")}");

        // 尝试桌面嵌入
        Logger.Info("Attempting desktop embed...");
        bool embedded = _embedService.EmbedToDesktop(this);

        if (embedded)
        {
            _settings.EmbedDesktop = true;
            SetEmbeddedMode(true);
            Logger.Success("Desktop embed successful");
        }
        else
        {
            Logger.Warn("Desktop embed failed");
            BtnPin.Content = "\uE718";
            BtnPin.ToolTip = "嵌入桌面（当前不可用）";
        }

        // 加载待办数据
        await _viewModel.InitializeAsync();
        Logger.Info($"Loaded {_viewModel.TotalCount} todo items");

        // 更新标题栏计数
        UpdatePendingCount();

        // 监听计数变化
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        Logger.Success("Window_Loaded completed");
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PendingCount) ||
            e.PropertyName == nameof(MainViewModel.TotalCount))
        {
            UpdatePendingCount();
        }
    }

    private void UpdatePendingCount()
    {
        if (_viewModel.TotalCount > 0)
        {
            TxtPendingCount.Text = $"({_viewModel.PendingCount} 项待办)";
        }
        else
        {
            TxtPendingCount.Text = "";
        }
    }

    /// <summary>
    /// 设置嵌入模式的 UI 状态
    /// </summary>
    private void SetEmbeddedMode(bool embedded)
    {
        if (embedded)
        {
            // 嵌入模式：隐藏最小化和关闭按钮，显示提示
            BtnMinimize.Visibility = Visibility.Collapsed;
            BtnClose.Visibility = Visibility.Collapsed;
            BtnPin.Content = "\uE77A";
            BtnPin.ToolTip = "取消嵌入桌面（点击无效，请使用托盘菜单）";

            // 更新托盘菜单
            _trayService.UpdateEmbedMenuText(true);
            _trayService.ShowNotification("MiniNote", "已嵌入桌面，右键托盘图标可取消嵌入");
        }
        else
        {
            // 普通模式：显示所有按钮
            BtnMinimize.Visibility = Visibility.Visible;
            BtnClose.Visibility = Visibility.Visible;
            BtnPin.Content = "\uE718";
            BtnPin.ToolTip = "嵌入桌面";

            // 更新托盘菜单
            _trayService.UpdateEmbedMenuText(false);
        }
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_isClosing) return;

        Logger.Info("Window closing, saving settings...");

        // 释放托盘图标
        _trayService.Dispose();

        // 保存窗口位置和大小
        _settings.WindowX = Left;
        _settings.WindowY = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        await _dbService.SaveSettingsAsync(_settings);

        // 取消桌面嵌入
        if (_embedService.IsEmbedded)
        {
            _embedService.DetachFromDesktop(this);
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 嵌入模式下禁用拖拽
        if (_embedService.IsEmbedded)
        {
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private async void TxtNewTodo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(_viewModel.NewTodoContent))
        {
            Logger.Info($"Adding todo: {_viewModel.NewTodoContent}");
            await _viewModel.AddTodoCommand.ExecuteAsync(null);
            TxtNewTodo.Focus();
        }
    }

    private void Priority_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tagStr && int.TryParse(tagStr, out int priority))
        {
            _viewModel.NewTodoPriority = priority;
        }
    }

    private async void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info($"Pin button clicked, IsEmbedded={_embedService.IsEmbedded}");

        if (_embedService.IsEmbedded)
        {
            // 取消嵌入
            _embedService.DetachFromDesktop(this);
            _settings.EmbedDesktop = false;
            SetEmbeddedMode(false);
            Logger.Info("Detached from desktop");
        }
        else
        {
            // 尝试嵌入
            Logger.Info("Attempting to embed to desktop...");
            bool success = _embedService.EmbedToDesktop(this);

            if (success)
            {
                _settings.EmbedDesktop = true;
                SetEmbeddedMode(true);
                Logger.Success("Desktop embed successful");
            }
            else
            {
                Logger.Error("Desktop embed failed");
                MessageBox.Show(
                    "桌面嵌入失败。\n\n可能的原因：\n1. Windows 11 安全限制\n2. 壁纸软件冲突\n\n建议：尝试关闭壁纸软件后重试。",
                    "嵌入失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        await _dbService.SaveSettingsAsync(_settings);
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("Minimizing window");
        WindowState = WindowState.Minimized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("Close button clicked");
        _isClosing = true;
        Close();
    }
}
