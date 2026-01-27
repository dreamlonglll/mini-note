using System;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using MiniNote.Models;
using MiniNote.Services;

namespace MiniNote.Views;

public partial class SettingsWindow : Window
{
    private AppSettings _settings;
    private bool _isInitializing = true;

    public event EventHandler<AppSettings>? SettingsChanged;
    public event EventHandler<bool>? ThemeChanged;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = new AppSettings();
    }

    /// <summary>
    /// 初始化设置值
    /// </summary>
    public void Initialize(AppSettings settings)
    {
        _isInitializing = true;
        _settings = settings;

        TglAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
        TglEmbedDesktop.IsChecked = settings.EmbedDesktop;
        SldOpacity.Value = settings.Opacity;
        TglDarkTheme.IsChecked = settings.IsDarkTheme;

        _isInitializing = false;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TglAutoStart_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var enabled = TglAutoStart.IsChecked ?? false;
        AutoStartService.SetAutoStart(enabled);
        _settings.AutoStart = enabled;
        SettingsChanged?.Invoke(this, _settings);
    }

    private void TglEmbedDesktop_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        _settings.EmbedDesktop = TglEmbedDesktop.IsChecked ?? true;
        SettingsChanged?.Invoke(this, _settings);
    }

    private void SldOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        _settings.Opacity = SldOpacity.Value;
        SettingsChanged?.Invoke(this, _settings);
    }

    private void TglDarkTheme_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var isDark = TglDarkTheme.IsChecked ?? true;
        _settings.IsDarkTheme = isDark;
        
        // 切换 Material Design 主题
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);

        ThemeChanged?.Invoke(this, isDark);
        SettingsChanged?.Invoke(this, _settings);
    }
}
