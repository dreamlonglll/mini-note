using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using MiniNote.Helpers;

namespace MiniNote.Services;

/// <summary>
/// 桌面嵌入服务 - 将窗口嵌入到桌面层级（类似壁纸软件）
/// </summary>
public class DesktopEmbedService
{
    private IntPtr _targetParent = IntPtr.Zero;
    private bool _isEmbedded = false;
    private IntPtr _hwnd = IntPtr.Zero;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int SW_SHOW = 5;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    public bool IsEmbedded => _isEmbedded;

    // 保存的窗口位置
    private int _savedLeft;
    private int _savedTop;
    private int _savedWidth;
    private int _savedHeight;

    /// <summary>
    /// 将窗口嵌入桌面
    /// </summary>
    public bool EmbedToDesktop(Window window)
    {
        try
        {
            _hwnd = new WindowInteropHelper(window).Handle;
            if (_hwnd == IntPtr.Zero)
            {
                Logger.Error("EmbedToDesktop: Window handle is zero");
                return false;
            }

            Logger.Info($"EmbedToDesktop: hwnd = 0x{_hwnd:X}");

            // 直接保存 WPF 坐标（WPF 和 Win32 对于顶级窗口使用相同的坐标系统）
            _savedLeft = (int)window.Left;
            _savedTop = (int)window.Top;
            _savedWidth = (int)window.ActualWidth;
            _savedHeight = (int)window.ActualHeight;

            int left = _savedLeft;
            int top = _savedTop;
            int width = _savedWidth;
            int height = _savedHeight;

            Logger.Info($"EmbedToDesktop: Position = ({left}, {top}), Size = ({width}, {height})");

            // 方法1：尝试找到 SHELLDLL_DefView 并嵌入
            _targetParent = FindShellDefView();
            if (_targetParent != IntPtr.Zero)
            {
                Logger.Info($"EmbedToDesktop: Found SHELLDLL_DefView = 0x{_targetParent:X}");

                // 设置窗口样式：工具窗口（不显示在任务栏）
                int exStyle = Win32Api.GetWindowLong(_hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                Win32Api.SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);
                Logger.Info("EmbedToDesktop: Set toolwindow style");

                // 嵌入到 SHELLDLL_DefView
                IntPtr result = Win32Api.SetParent(_hwnd, _targetParent);
                if (result != IntPtr.Zero)
                {
                    MoveWindow(_hwnd, left, top, width, height, true);
                    ShowWindow(_hwnd, SW_SHOW);
                    _isEmbedded = true;
                    Logger.Success("EmbedToDesktop: Embedded to SHELLDLL_DefView");
                    return true;
                }
                Logger.Warn($"EmbedToDesktop: SetParent to DefView failed");
            }

            // 方法2：尝试 WorkerW
            _targetParent = GetWorkerW();
            if (_targetParent != IntPtr.Zero)
            {
                Logger.Info($"EmbedToDesktop: Using WorkerW = 0x{_targetParent:X}");

                int exStyle = Win32Api.GetWindowLong(_hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                Win32Api.SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);

                IntPtr result = Win32Api.SetParent(_hwnd, _targetParent);
                if (result != IntPtr.Zero)
                {
                    MoveWindow(_hwnd, left, top, width, height, true);
                    ShowWindow(_hwnd, SW_SHOW);
                    _isEmbedded = true;
                    Logger.Success("EmbedToDesktop: Embedded to WorkerW");
                    return true;
                }
            }

            // 方法3：尝试 Progman
            IntPtr progman = Win32Api.FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
            {
                Logger.Info($"EmbedToDesktop: Trying Progman = 0x{progman:X}");

                int exStyle = Win32Api.GetWindowLong(_hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                Win32Api.SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);

                IntPtr result = Win32Api.SetParent(_hwnd, progman);
                if (result != IntPtr.Zero)
                {
                    MoveWindow(_hwnd, left, top, width, height, true);
                    ShowWindow(_hwnd, SW_SHOW);
                    _isEmbedded = true;
                    Logger.Success("EmbedToDesktop: Embedded to Progman");
                    return true;
                }
            }

            Logger.Error("EmbedToDesktop: All methods failed");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error("EmbedToDesktop failed", ex);
            return false;
        }
    }

    /// <summary>
    /// 取消桌面嵌入
    /// </summary>
    public void DetachFromDesktop(Window window)
    {
        if (!_isEmbedded) return;

        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            Logger.Info($"DetachFromDesktop: Saved position = ({_savedLeft}, {_savedTop})");

            // 先脱离父窗口
            Win32Api.SetParent(hwnd, IntPtr.Zero);

            // 移除工具窗口样式
            int exStyle = Win32Api.GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle &= ~WS_EX_TOOLWINDOW;
            Win32Api.SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
            Logger.Info("DetachFromDesktop: Removed toolwindow style");

            // 使用保存的位置恢复
            MoveWindow(hwnd, _savedLeft, _savedTop, _savedWidth, _savedHeight, true);
            ShowWindow(hwnd, SW_SHOW);

            // 更新 WPF 窗口属性
            window.Left = _savedLeft;
            window.Top = _savedTop;
            window.Width = _savedWidth;
            window.Height = _savedHeight;

            _isEmbedded = false;
            _targetParent = IntPtr.Zero;
            Logger.Success("DetachFromDesktop: Complete");
        }
        catch (Exception ex)
        {
            Logger.Error("DetachFromDesktop failed", ex);
        }
    }

    /// <summary>
    /// 查找 SHELLDLL_DefView 窗口
    /// </summary>
    private IntPtr FindShellDefView()
    {
        IntPtr defView = IntPtr.Zero;

        // 首先在 Progman 中查找
        IntPtr progman = Win32Api.FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            defView = Win32Api.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                Logger.Info($"FindShellDefView: Found in Progman = 0x{defView:X}");
                return defView;
            }
        }

        // 在所有 WorkerW 中查找
        EnumWindows((hWnd, lParam) =>
        {
            var className = GetWindowClassName(hWnd);
            if (className == "WorkerW")
            {
                var found = Win32Api.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (found != IntPtr.Zero)
                {
                    defView = found;
                    Logger.Info($"FindShellDefView: Found in WorkerW 0x{hWnd:X} = 0x{found:X}");
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        return defView;
    }

    /// <summary>
    /// 获取 WorkerW 窗口
    /// </summary>
    private IntPtr GetWorkerW()
    {
        IntPtr progman = Win32Api.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        // 发送消息创建 WorkerW
        Win32Api.SendMessageTimeout(
            progman,
            0x052C,
            new IntPtr(0x0D),
            new IntPtr(0x01),
            0x0000,
            1000,
            out _
        );

        IntPtr workerW = IntPtr.Zero;
        IntPtr defViewParent = IntPtr.Zero;

        EnumWindows((hWnd, lParam) =>
        {
            var className = GetWindowClassName(hWnd);
            if (className == "WorkerW")
            {
                var defView = Win32Api.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    defViewParent = hWnd;
                    // 找到包含 DefView 的 WorkerW，获取下一个 WorkerW
                    workerW = Win32Api.FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        // 如果没找到独立的 WorkerW，创建一个新的或返回 defViewParent
        if (workerW == IntPtr.Zero && defViewParent != IntPtr.Zero)
        {
            // 尝试使用包含 DefView 的 WorkerW
            workerW = defViewParent;
        }

        return workerW;
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
