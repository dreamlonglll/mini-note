using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MiniNote.Helpers;

namespace MiniNote.Services;

/// <summary>
/// 主题服务 - 应用深色主题
/// </summary>
public static class ThemeService
{
    /// <summary>
    /// 应用深色主题
    /// </summary>
    public static void ApplyDarkTheme()
    {
        Logger.Info("ThemeService: Applying dark theme");

        var app = Application.Current;
        
        // Material Design 主题
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(BaseTheme.Dark);
        paletteHelper.SetTheme(theme);

        // 自定义颜色
        app.Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(242, 30, 38, 54)) { Opacity = 0.95 };
        app.Resources["AppBorderBrush"] = new SolidColorBrush(Color.FromArgb(74, 255, 255, 255));
        app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(91, 140, 255));
        app.Resources["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(122, 165, 255));
        app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(245, 248, 255));
        app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromArgb(168, 255, 255, 255));
        app.Resources["TextTertiaryBrush"] = new SolidColorBrush(Color.FromArgb(112, 255, 255, 255));
        app.Resources["SurfaceBrush"] = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));
        app.Resources["SurfaceHoverBrush"] = new SolidColorBrush(Color.FromArgb(58, 255, 255, 255));
        app.Resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(47, 255, 255, 255));
        app.Resources["DividerBrush"] = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));
        app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
        
        // 对话框背景
        app.Resources["DialogBackgroundColor"] = Color.FromArgb(245, 32, 37, 48);
        app.Resources["DialogTitleBarColor"] = Color.FromArgb(37, 255, 255, 255);

        Logger.Success("ThemeService: Dark theme applied");
    }

    /// <summary>
    /// 根据设置应用主题（保留接口兼容性，始终应用深色主题）
    /// </summary>
    public static void ApplyTheme(bool isDark = true)
    {
        ApplyDarkTheme();
    }
}
