using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MiniNote.Helpers;
using MiniNote.Models;
using MiniNote.Services;

namespace MiniNote;

public partial class MainWindow : Window
{
    private readonly DesktopEmbedService _embedService;
    private readonly DatabaseService _dbService;
    private AppSettings _settings = null!;
    private bool _isClosing = false;

    public MainWindow()
    {
        InitializeComponent();
        _embedService = new DesktopEmbedService();
        _dbService = new DatabaseService();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 加载设置
        _settings = await _dbService.GetSettingsAsync();

        // 应用窗口位置和大小
        Left = _settings.WindowX;
        Top = _settings.WindowY;
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;

        // 启用磨砂效果
        bool acrylicEnabled = AcrylicHelper.EnableAcrylic(this);
        AcrylicHelper.SetDarkMode(this, true);

        // 嵌入桌面
        if (_settings.EmbedDesktop)
        {
            bool embedded = _embedService.EmbedToDesktop(this);
            UpdateStatus(acrylicEnabled, embedded);
        }
        else
        {
            UpdateStatus(acrylicEnabled, false);
        }
    }

    private void UpdateStatus(bool acrylicEnabled, bool embedded)
    {
        string status = "";
        if (acrylicEnabled) status += "磨砂效果已启用";
        else status += "磨砂效果不可用";

        status += " | ";

        if (embedded) status += "已嵌入桌面";
        else status += "普通窗口模式";

        TxtStatus.Text = status;
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_isClosing) return;

        // 保存窗口位置和大小
        _settings.WindowX = Left;
        _settings.WindowY = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        await _dbService.SaveSettingsAsync(_settings);

        // 取消桌面嵌入
        _embedService.DetachFromDesktop(this);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 允许拖动窗口
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private async void Window_LocationChanged(object? sender, System.EventArgs e)
    {
        // 位置变化时不立即保存，等关闭时保存
    }

    private async void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 大小变化时不立即保存，等关闭时保存
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        // TODO: 打开设置窗口
        MessageBox.Show("设置功能将在后续版本中实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        if (_embedService.IsEmbedded)
        {
            // 取消嵌入
            _embedService.DetachFromDesktop(this);
            _settings.EmbedDesktop = false;
            BtnPin.Content = "\uE718"; // 图钉图标
            BtnPin.ToolTip = "嵌入桌面";
        }
        else
        {
            // 嵌入桌面
            bool success = _embedService.EmbedToDesktop(this);
            if (success)
            {
                _settings.EmbedDesktop = true;
                BtnPin.Content = "\uE77A"; // 已固定图标
                BtnPin.ToolTip = "取消嵌入";
            }
        }

        await _dbService.SaveSettingsAsync(_settings);
        UpdateStatus(true, _embedService.IsEmbedded);
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        // 最小化窗口
        WindowState = WindowState.Minimized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _isClosing = true;
        Close();
    }
}
