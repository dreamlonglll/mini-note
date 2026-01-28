using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using MiniNote.Helpers;
using MiniNote.Models;
using MiniNote.Services;
using MiniNote.ViewModels;
using MiniNote.Views;

namespace MiniNote;

public partial class MainWindow : Window
{
    private readonly DesktopEmbedService _embedService;
    private readonly DatabaseService _dbService;
    private readonly MainViewModel _viewModel;
    private readonly TrayIconService _trayService;
    private readonly NotificationService _notificationService;
    private AppSettings _settings = null!;
    private bool _isClosing = false;
    private Brush? _normalBackground;
    private Brush? _normalBorderBrush;
    private Effect? _normalEffect;


    public MainWindow()
    {
        InitializeComponent();
        Logger.Info("MainWindow initialized");

        _normalBackground = MainBorder.Background;
        _normalBorderBrush = MainBorder.BorderBrush;
        _normalEffect = MainBorder.Effect;

        _dbService = new DatabaseService();
        _embedService = new DesktopEmbedService();
        _trayService = new TrayIconService();
        _notificationService = new NotificationService();
        _viewModel = new MainViewModel(_dbService);

        DataContext = _viewModel;

        // 初始化系统托盘
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _trayService.Initialize();

        // 固定/取消固定
        _trayService.OnToggleEmbed += () =>
        {
            Dispatcher.Invoke(() =>
            {
                BtnPin_Click(this, new RoutedEventArgs());
            });
        };

        // 添加待办项
        _trayService.OnAddTodo += () =>
        {
            Dispatcher.Invoke(() =>
            {
                // 打开添加待办对话框
                BtnAddFloat_Click(this, new RoutedEventArgs());
            });
        };

        // 重置位置
        _trayService.OnResetPosition += () =>
        {
            Dispatcher.Invoke(async () =>
            {
                await ResetWindowPosition();
            });
        };

        // 退出
        _trayService.OnExit += () =>
        {
            Dispatcher.Invoke(() =>
            {
                Close();
            });
        };
    }

    private async void ShowReminderDialog(TodoItemViewModel todoVm)
    {
        var dialog = new ReminderDialog
        {
            Owner = this
        };
        dialog.SetExistingReminder(todoVm.ReminderTime);

        if (dialog.ShowDialog() == true)
        {
            if (dialog.WasCleared)
            {
                todoVm.SetReminderTime(null);
                await _dbService.UpdateTodoAsync(todoVm.Model);
                Logger.Info($"Cleared reminder for todo #{todoVm.Id}");
            }
            else if (dialog.Result.HasValue)
            {
                todoVm.SetReminderTime(dialog.Result);
                await _dbService.UpdateTodoAsync(todoVm.Model);
                Logger.Info($"Set reminder for todo #{todoVm.Id} at {dialog.Result.Value}");
            }
        }
    }

    /// <summary>
    /// 安装 WndProc 钩子用于选择性点击穿透
    /// </summary>
    private void InstallClickThroughHook()
    {
        var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        hwndSource?.AddHook(WndProc);
        Logger.Info("Installed click-through hook");
    }

    private const int WM_NCHITTEST = 0x0084;
    private const int WM_DPICHANGED = 0x02E0;
    private const int HTTRANSPARENT = -1;
    private const int HTCLIENT = 1;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DPICHANGED)
        {
            ApplyDpiChanged(wParam, lParam);
            handled = true;
            return IntPtr.Zero;
        }

        // 只在嵌入模式下处理点击穿透
        if (msg == WM_NCHITTEST && _embedService.IsEmbedded && _embedService.IsClickThroughEnabled)
        {
            // 获取鼠标位置
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)(lParam.ToInt32() >> 16);

            // 转换为窗口坐标
            var point = PointFromScreen(new Point(x, y));

            // 检查是否点击在取消固定按钮上
            if (IsPointOverButton(BtnPin, point))
            {
                // 按钮区域可点击
                handled = false;
                return IntPtr.Zero;
            }

            // 其他区域穿透
            handled = true;
            return new IntPtr(HTTRANSPARENT);
        }

        return IntPtr.Zero;
    }

    private void ApplyDpiChanged(IntPtr wParam, IntPtr lParam)
    {
        int dpiX = wParam.ToInt32() & 0xFFFF;
        int dpiY = (wParam.ToInt32() >> 16) & 0xFFFF;
        if (dpiX <= 0 || dpiY <= 0)
        {
            return;
        }

        double scaleX = dpiX / 96.0;
        double scaleY = dpiY / 96.0;
        var rect = Marshal.PtrToStructure<Win32Api.RECT>(lParam);
        int widthPx = rect.Right - rect.Left;
        int heightPx = rect.Bottom - rect.Top;

        Left = rect.Left / scaleX;
        Top = rect.Top / scaleY;
        Width = widthPx / scaleX;
        Height = heightPx / scaleY;

        Logger.Info($"DPI changed: {dpiX}x{dpiY}, rect=({rect.Left}, {rect.Top})-({rect.Right}, {rect.Bottom})");
        Dispatcher.BeginInvoke(() =>
        {
            MainBorder.InvalidateMeasure();
            MainBorder.InvalidateArrange();
            MainBorder.UpdateLayout();
        });
    }

    private bool IsPointOverButton(Button button, Point point)
    {
        if (button.Visibility != Visibility.Visible)
            return false;

        try
        {
            // 获取按钮相对于窗口的位置和大小
            var transform = button.TransformToAncestor(this);
            var buttonTopLeft = transform.Transform(new Point(0, 0));
            var buttonRect = new Rect(buttonTopLeft, new Size(button.ActualWidth, button.ActualHeight));

            return buttonRect.Contains(point);
        }
        catch
        {
            return false;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Logger.Info("Window_Loaded started");

        // 加载设置
        _settings = await _dbService.GetSettingsAsync();
        Logger.Info($"Settings loaded: EmbedDesktop={_settings.EmbedDesktop}, IsDarkTheme={_settings.IsDarkTheme}");

        // 应用主题
        ThemeService.ApplyTheme(_settings.IsDarkTheme);

        // 应用窗口位置和大小
        Left = _settings.WindowX;
        Top = _settings.WindowY;
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;

        // 启用磨砂效果
        bool acrylicEnabled = AcrylicHelper.EnableAcrylic(this);
        AcrylicHelper.SetDarkMode(this, _settings.IsDarkTheme);
        Logger.Info($"Acrylic effect: {(acrylicEnabled ? "enabled" : "not available")}");

        // 安装点击穿透钩子
        InstallClickThroughHook();

        if (_settings.EmbedDesktop)
        {
            // 尝试桌面嵌入
            Logger.Info("Attempting desktop embed...");
            bool embedded = _embedService.EmbedToDesktop(this);

            if (embedded)
            {
                SetEmbeddedMode(true);
                Logger.Success("Desktop embed successful");
            }
            else
            {
                Logger.Warn("Desktop embed failed");
                _settings.EmbedDesktop = false;
                SetEmbeddedMode(false);
                BtnPin.ToolTip = "嵌入桌面（当前不可用）";
                await _dbService.SaveSettingsAsync(_settings);
            }
        }
        else
        {
            Logger.Info("EmbedDesktop disabled, skipping embed");
            SetEmbeddedMode(false);
        }

        // 启动提醒服务
        _notificationService.StartReminderService();
        Logger.Info("Notification service started");

        // 加载待办数据
        await _viewModel.InitializeAsync();
        Logger.Info($"Loaded {_viewModel.TotalCount} todo items");

        // 注册事件
        foreach (var todoVm in _viewModel.TodoItems)
        {
            todoVm.ReminderRequested += OnTodoReminderRequested;
            todoVm.EditRequested += OnTodoEditRequested;
        }

        // 监听计数变化
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // 监听待办项集合变化
        _viewModel.TodoItems.CollectionChanged += TodoItems_CollectionChanged;

        Logger.Success("Window_Loaded completed");
    }

    private void TodoItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TodoItemViewModel todoVm in e.NewItems)
            {
                todoVm.ReminderRequested += OnTodoReminderRequested;
                todoVm.EditRequested += OnTodoEditRequested;
            }
        }
    }

    private void OnTodoReminderRequested(TodoItemViewModel todoVm)
    {
        ShowReminderDialog(todoVm);
    }

    private async void OnTodoEditRequested(TodoItemViewModel todoVm)
    {
        var dialog = new EditTodoDialog
        {
            Owner = this
        };
        dialog.SetTodoItem(todoVm);

        if (dialog.ShowDialog() == true)
        {
            if (dialog.DeleteRequested)
            {
                // 删除待办
                await _dbService.DeleteTodoAsync(todoVm.Id);
                _viewModel.TodoItems.Remove(todoVm);
                _viewModel.UpdateCountsPublic();
                Logger.Info($"Deleted todo #{todoVm.Id}");
            }
            else
            {
                // 处理提醒时间变更
                if (dialog.ReminderTimeChanged)
                {
                    todoVm.SetReminderTime(dialog.NewReminderTime);
                }
                
                // 更新待办
                await _dbService.UpdateTodoAsync(todoVm.Model);
                Logger.Info($"Updated todo #{todoVm.Id}");
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PendingCount) ||
            e.PropertyName == nameof(MainViewModel.TotalCount))
        {
            // 数据更新时刷新界面状态
        }
    }

    /// <summary>
    /// 设置嵌入模式的 UI 状态
    /// </summary>
    private void SetEmbeddedMode(bool embedded)
    {
        if (embedded)
        {
            // 嵌入模式：保持背景颜色一致，只移除边框和阴影
            MainBorder.BorderBrush = Brushes.Transparent;
            MainBorder.Effect = null;
            _embedService.DisableEmbedClickThrough();

            // 嵌入模式：隐藏最小化和关闭按钮、悬浮按钮，显示提示
            BtnMinimize.Visibility = Visibility.Collapsed;
            BtnClose.Visibility = Visibility.Collapsed;
            BtnAddFloat.Visibility = Visibility.Collapsed;
            IconPin.Kind = MaterialDesignThemes.Wpf.PackIconKind.PinOff;
            BtnPin.ToolTip = "取消嵌入桌面";

            // 更新托盘菜单
            _trayService.UpdateEmbedMenuText(true);
            _trayService.ShowNotification("MiniNote", "已嵌入桌面，右键托盘图标可取消嵌入");
        }
        else
        {
            if (_normalBorderBrush != null)
            {
                MainBorder.BorderBrush = _normalBorderBrush;
            }
            MainBorder.Effect = _normalEffect;
            _embedService.DisableEmbedClickThrough();

            // 普通模式：显示所有按钮
            BtnMinimize.Visibility = Visibility.Visible;
            BtnClose.Visibility = Visibility.Visible;
            BtnAddFloat.Visibility = Visibility.Visible;
            IconPin.Kind = MaterialDesignThemes.Wpf.PackIconKind.PinOutline;
            BtnPin.ToolTip = "嵌入桌面";

            // 更新托盘菜单
            _trayService.UpdateEmbedMenuText(false);
        }
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        // 防止重复处理
        if (_isClosing) return;
        _isClosing = true;

        Logger.Info("Window closing, saving settings...");

        // 保存窗口位置、大小和固定状态
        _settings.WindowX = Left;
        _settings.WindowY = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.EmbedDesktop = _embedService.IsEmbedded;
        
        Logger.Info($"Saving settings: X={_settings.WindowX}, Y={_settings.WindowY}, W={_settings.WindowWidth}, H={_settings.WindowHeight}, Embed={_settings.EmbedDesktop}");
        await _dbService.SaveSettingsAsync(_settings);
        Logger.Info("Settings saved successfully");

        // 释放托盘图标
        _trayService.Dispose();

        // 停止提醒服务
        _notificationService.StopReminderService();
        _notificationService.Dispose();

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

    private async void BtnAddFloat_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("Opening add todo dialog");

        var dialog = new AddTodoDialog
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var todo = dialog.Result;
            await _dbService.AddTodoAsync(todo);

            // 加载分类信息
            todo.Category = _viewModel.SelectedCategory;

            var vm = CreateTodoViewModelWithEvents(todo);

            // 插入到合适的位置
            int insertIndex = 0;
            for (int i = 0; i < _viewModel.TodoItems.Count; i++)
            {
                if (_viewModel.TodoItems[i].IsCompleted)
                {
                    insertIndex = i;
                    break;
                }
                if (_viewModel.TodoItems[i].Priority < todo.Priority)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }

            _viewModel.TodoItems.Insert(insertIndex, vm);
            _viewModel.UpdateCountsPublic();

            Logger.Info($"Added todo: {todo.Content}");
        }
    }

    private TodoItemViewModel CreateTodoViewModelWithEvents(Models.TodoItem todo)
    {
        var vm = new TodoItemViewModel(todo);
        vm.ReminderRequested += OnTodoReminderRequested;
        vm.EditRequested += OnTodoEditRequested;
        vm.CompletedChanged += async (sender) =>
        {
            await _dbService.UpdateTodoAsync(sender.Model);
            _viewModel.UpdateCountsPublic();
        };
        vm.DeleteRequested += async (sender) =>
        {
            await _dbService.DeleteTodoAsync(sender.Id);
            _viewModel.TodoItems.Remove(sender);
            _viewModel.UpdateCountsPublic();
        };
        return vm;
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
                    "桌面嵌入失败。\n\n可能的原因：\n1. Explorer 未创建 WorkerW 桌面层\n2. Windows 11 安全限制\n3. 壁纸软件冲突\n\n建议：\n- 尝试重启资源管理器后重试\n- 关闭壁纸/桌面美化软件后重试",
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
        Close();
    }

    /// <summary>
    /// 重置窗口位置到最左边屏幕的左边距10%、上边距10%位置，并取消固定状态
    /// </summary>
    private async Task ResetWindowPosition()
    {
        Logger.Info("Resetting window position");

        // 获取DPI缩放比例
        var source = PresentationSource.FromVisual(this);
        double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        Logger.Info($"DPI scale: {dpiScaleX}x{dpiScaleY}");

        // 获取所有屏幕，找到最左边的屏幕
        var screens = System.Windows.Forms.Screen.AllScreens;
        var leftmostScreen = screens[0];
        foreach (var screen in screens)
        {
            if (screen.Bounds.X < leftmostScreen.Bounds.X)
            {
                leftmostScreen = screen;
            }
        }

        // 计算目标位置：左边距10%、上边距10%（物理像素）
        var workingArea = leftmostScreen.WorkingArea;
        double targetXPixel = workingArea.X + workingArea.Width * 0.1;
        double targetYPixel = workingArea.Y + workingArea.Height * 0.1;

        // 转换为WPF设备无关像素(DIP)
        double targetX = targetXPixel / dpiScaleX;
        double targetY = targetYPixel / dpiScaleY;

        Logger.Info($"Target screen: {leftmostScreen.DeviceName}, WorkingArea: ({workingArea.X}, {workingArea.Y}, {workingArea.Width}, {workingArea.Height})");
        Logger.Info($"Target position (pixels): ({targetXPixel}, {targetYPixel}), (DIP): ({targetX}, {targetY})");

        // 如果当前是固定状态，先取消固定
        if (_embedService.IsEmbedded)
        {
            _embedService.DetachFromDesktop(this);
            _settings.EmbedDesktop = false;
            SetEmbeddedMode(false);
            Logger.Info("Detached from desktop for position reset");
        }

        // 设置窗口位置
        Left = targetX;
        Top = targetY;

        // 保存设置
        _settings.WindowX = Left;
        _settings.WindowY = Top;
        _settings.EmbedDesktop = false;
        await _dbService.SaveSettingsAsync(_settings);

        Logger.Info($"Window position reset to ({Left}, {Top})");
        _trayService.ShowNotification("MiniNote", "窗口位置已重置");
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("Opening settings window");

        var settingsWindow = new SettingsWindow
        {
            Owner = this
        };

        settingsWindow.Initialize(_settings);

        settingsWindow.SettingsChanged += async (s, newSettings) =>
        {
            _settings = newSettings;
            Opacity = newSettings.Opacity;

            // 处理嵌入桌面设置变化
            if (newSettings.EmbedDesktop != _embedService.IsEmbedded)
            {
                if (newSettings.EmbedDesktop)
                {
                    bool success = _embedService.EmbedToDesktop(this);
                    if (success)
                    {
                        SetEmbeddedMode(true);
                    }
                    else
                    {
                        _settings.EmbedDesktop = false;
                    }
                }
                else
                {
                    _embedService.DetachFromDesktop(this);
                    SetEmbeddedMode(false);
                }
            }

            await _dbService.SaveSettingsAsync(_settings);
        };

        settingsWindow.ShowDialog();
    }
}
