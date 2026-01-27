using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MiniNote.Helpers;

/// <summary>
/// 磨砂效果助手类
/// </summary>
public static class AcrylicHelper
{
    /// <summary>
    /// 为窗口启用 Acrylic 磨砂效果 (Windows 11)
    /// </summary>
    public static bool EnableAcrylic(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return false;

            // 设置窗口背景为透明
            window.Background = Brushes.Transparent;

            // 尝试启用 Acrylic 效果 (Windows 11)
            int backdropType = Win32Api.DWMSBT_TRANSIENTWINDOW;
            int result = Win32Api.DwmSetWindowAttribute(
                hwnd,
                Win32Api.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropType,
                sizeof(int)
            );

            if (result == 0)
            {
                // 扩展帧到客户区
                var margins = new Win32Api.MARGINS { cxLeftWidth = -1 };
                Win32Api.DwmExtendFrameIntoClientArea(hwnd, ref margins);
                return true;
            }

            // 如果 Acrylic 失败，尝试 Mica
            return EnableMica(window);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 为窗口启用 Mica 效果 (Windows 11)
    /// </summary>
    public static bool EnableMica(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return false;

            window.Background = Brushes.Transparent;

            int backdropType = Win32Api.DWMSBT_MAINWINDOW;
            int result = Win32Api.DwmSetWindowAttribute(
                hwnd,
                Win32Api.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdropType,
                sizeof(int)
            );

            if (result == 0)
            {
                var margins = new Win32Api.MARGINS { cxLeftWidth = -1 };
                Win32Api.DwmExtendFrameIntoClientArea(hwnd, ref margins);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置深色模式
    /// </summary>
    public static void SetDarkMode(Window window, bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int value = isDark ? 1 : 0;
            Win32Api.DwmSetWindowAttribute(
                hwnd,
                Win32Api.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref value,
                sizeof(int)
            );
        }
        catch
        {
            // 忽略错误
        }
    }
}
