using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
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
    private HwndSource? _embedSource;
    private FrameworkElement? _embedContent;
    private RenderMode _savedRenderMode = RenderMode.Default;
    private bool _renderModeChanged;
    private FrameworkElement? _hitTestRoot;
    private FrameworkElement? _clickThroughElement;
    private bool _embedHookInstalled;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;
    private const int HTCLIENT = 1;

    public bool IsEmbedded => _isEmbedded;
    public bool IsClickThroughEnabled => _embedHookInstalled;

    public void EnableEmbedClickThrough(FrameworkElement hitTestRoot, FrameworkElement clickableElement)
    {
        _hitTestRoot = hitTestRoot;
        _clickThroughElement = clickableElement;

        if (_embedSource != null)
        {
            if (_embedHookInstalled)
            {
                _embedSource.RemoveHook(EmbedWndProc);
            }
            _embedSource.AddHook(EmbedWndProc);
            _embedHookInstalled = true;
            Logger.Info("EmbedHost: Click-through hook installed");
        }
    }

    public void DisableEmbedClickThrough()
    {
        if (_embedSource != null && _embedHookInstalled)
        {
            _embedSource.RemoveHook(EmbedWndProc);
            _embedHookInstalled = false;
        }

        _hitTestRoot = null;
        _clickThroughElement = null;
    }

    // 保存的窗口位置（DIP）
    private double _savedLeft;
    private double _savedTop;
    private double _savedWidth;
    private double _savedHeight;

    /// <summary>
    /// 将窗口嵌入桌面
    /// </summary>
    public bool EmbedToDesktop(Window window)
    {
        try
        {
            if (_isEmbedded)
            {
                return true;
            }

            _hwnd = new WindowInteropHelper(window).Handle;
            if (_hwnd == IntPtr.Zero)
            {
                Logger.Error("EmbedToDesktop: Window handle is zero");
                return false;
            }

            Logger.Info($"EmbedToDesktop: hwnd = 0x{_hwnd:X}");

            // 保存 WPF 坐标（DIP）
            _savedLeft = window.Left;
            _savedTop = window.Top;
            _savedWidth = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
            _savedHeight = window.ActualHeight > 0 ? window.ActualHeight : window.Height;

            var dpi = VisualTreeHelper.GetDpi(window);
            int left = (int)Math.Round(_savedLeft * dpi.DpiScaleX);
            int top = (int)Math.Round(_savedTop * dpi.DpiScaleY);
            int width = (int)Math.Round(_savedWidth * dpi.DpiScaleX);
            int height = (int)Math.Round(_savedHeight * dpi.DpiScaleY);

            Logger.Info($"EmbedToDesktop: Position(DIP)=({_savedLeft:F0}, {_savedTop:F0}), Size(DIP)=({_savedWidth:F0}, {_savedHeight:F0})");
            Logger.Info($"EmbedToDesktop: Position(PX)=({left}, {top}), Size(PX)=({width}, {height})");

            var centerPoint = new System.Drawing.Point(left + (width / 2), top + (height / 2));
            var screenBounds = Screen.FromPoint(centerPoint).Bounds;
            double leftPercent = screenBounds.Width > 0 ? (left - screenBounds.Left) / (double)screenBounds.Width : 0;
            double topPercent = screenBounds.Height > 0 ? (top - screenBounds.Top) / (double)screenBounds.Height : 0;
            double widthPercent = screenBounds.Width > 0 ? width / (double)screenBounds.Width : 0;
            double heightPercent = screenBounds.Height > 0 ? height / (double)screenBounds.Height : 0;
            Logger.Info($"EmbedToDesktop: ScreenRect=({screenBounds.Left}, {screenBounds.Top})-({screenBounds.Right}, {screenBounds.Bottom}), Percent=({leftPercent:P1}, {topPercent:P1}, {widthPercent:P1}, {heightPercent:P1})");

            _targetParent = GetDesktopWorkerW(out string hostType);
            if (_targetParent == IntPtr.Zero)
            {
                Logger.Error("EmbedToDesktop: WorkerW not found");
                return false;
            }

            Logger.Info($"EmbedToDesktop: Using {hostType} = 0x{_targetParent:X}");
            LogWindowRect(_targetParent, $"HostParent({hostType})");
            if (TryGetWindowRect(_targetParent, out var hostRect))
            {
                int hostOffsetX = screenBounds.Left - hostRect.Left;
                int hostOffsetY = screenBounds.Top - hostRect.Top;
                int percentLeft = (int)Math.Round(leftPercent * screenBounds.Width);
                int percentTop = (int)Math.Round(topPercent * screenBounds.Height);
                int percentWidth = (int)Math.Round(widthPercent * screenBounds.Width);
                int percentHeight = (int)Math.Round(heightPercent * screenBounds.Height);
                int relativeLeft = hostOffsetX + percentLeft;
                int relativeTop = hostOffsetY + percentTop;

                Logger.Info($"EmbedToDesktop: HostRect=({hostRect.Left}, {hostRect.Top})-({hostRect.Right}, {hostRect.Bottom}), HostOffset=({hostOffsetX}, {hostOffsetY})");
                Logger.Info($"EmbedToDesktop: PercentPx=({percentLeft}, {percentTop}), SizePx=({percentWidth}, {percentHeight})");
                Logger.Info($"EmbedToDesktop: RelativePx=({relativeLeft}, {relativeTop})");

                left = relativeLeft;
                top = relativeTop;
                width = percentWidth;
                height = percentHeight;
            }
            else
            {
                Logger.Warn("EmbedToDesktop: GetWindowRect failed, using screen coordinates");
            }

            if (TryCreateEmbedHost(window, left, top, width, height, hostType))
            {
                return true;
            }

            Logger.Error("EmbedToDesktop: Create embed host failed");
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
            Logger.Info($"DetachFromDesktop: Saved position(DIP) = ({_savedLeft:F0}, {_savedTop:F0})");

            DisableEmbedClickThrough();

            if (_embedSource != null)
            {
                _embedSource.RootVisual = null;
                _embedSource.Dispose();
                _embedSource = null;
            }

            if (_embedContent != null)
            {
                window.Content = _embedContent;
                _embedContent = null;
            }

            // 更新 WPF 窗口属性
            window.Left = _savedLeft;
            window.Top = _savedTop;
            window.Width = _savedWidth;
            window.Height = _savedHeight;
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Show();
            window.Activate();

            _isEmbedded = false;
            _targetParent = IntPtr.Zero;
            RestoreRenderMode();
            Logger.Success("DetachFromDesktop: Complete");
        }
        catch (Exception ex)
        {
            Logger.Error("DetachFromDesktop failed", ex);
        }
    }

    /// <summary>
    /// 获取桌面 WorkerW 窗口
    /// </summary>
    private IntPtr GetDesktopWorkerW(out string hostType)
    {
        hostType = "Unknown";
        IntPtr progman = Win32Api.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        // 发送消息创建 WorkerW
        Win32Api.SendMessageTimeout(
            progman,
            Win32Api.WM_SPAWN_WORKER,
            new IntPtr(0x0D),
            new IntPtr(0x01),
            Win32Api.SMTO_NORMAL,
            1000,
            out _
        );

        IntPtr workerWithoutDefView;
        IntPtr workerWithDefView;

        bool found = TryFindWorkerW(out workerWithoutDefView, out workerWithDefView);
        if (!found)
        {
            // 兼容部分系统：尝试使用 0 参数再次触发 WorkerW
            Win32Api.SendMessageTimeout(
                progman,
                Win32Api.WM_SPAWN_WORKER,
                IntPtr.Zero,
                IntPtr.Zero,
                Win32Api.SMTO_NORMAL,
                1000,
                out _
            );

            TryFindWorkerW(out workerWithoutDefView, out workerWithDefView);
        }

        if (workerWithDefView != IntPtr.Zero)
        {
            hostType = "WorkerW(DefView)";
            return workerWithDefView;
        }

        if (workerWithoutDefView != IntPtr.Zero)
        {
            IntPtr defView = FindShellDefView();
            if (defView != IntPtr.Zero)
            {
                hostType = "SHELLDLL_DefView";
                return defView;
            }

            hostType = "WorkerW";
            return workerWithoutDefView;
        }

        return IntPtr.Zero;
    }

    private IntPtr FindShellDefView()
    {
        IntPtr progman = Win32Api.FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            IntPtr defView = Win32Api.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                return defView;
            }
        }

        IntPtr workerW = Win32Api.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
        while (workerW != IntPtr.Zero)
        {
            IntPtr defView = Win32Api.FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                return defView;
            }
            workerW = Win32Api.FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
        }

        return IntPtr.Zero;
    }

    private bool TryCreateEmbedHost(Window window, int left, int top, int width, int height, string hostType)
    {
        if (window.Content is not FrameworkElement content)
        {
            Logger.Error("EmbedToDesktop: Window content is not FrameworkElement");
            return false;
        }

        _savedRenderMode = RenderOptions.ProcessRenderMode;
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        _renderModeChanged = true;

        content.DataContext = window.DataContext;
        window.Content = null;

        try
        {
            var parameters = new HwndSourceParameters("MiniNoteDesktopHost")
            {
                ParentWindow = _targetParent,
                WindowStyle = WS_CHILD | WS_VISIBLE,
                PositionX = left,
                PositionY = top,
                Width = width,
                Height = height
            };

            _embedSource = new HwndSource(parameters);
            _embedContent = content;
            _embedSource.RootVisual = _embedContent;
            _embedContent.Measure(new Size(_savedWidth, _savedHeight));
            _embedContent.Arrange(new Rect(0, 0, _savedWidth, _savedHeight));
            _embedContent.UpdateLayout();
            LogWindowRect(_embedSource.Handle, "EmbedHost");
            IntPtr zOrder = hostType == "SHELLDLL_DefView" ? IntPtr.Zero : Win32Api.HWND_BOTTOM;
            Win32Api.SetWindowPos(
                _embedSource.Handle,
                zOrder,
                0,
                0,
                0,
                0,
                Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE
            );

            _isEmbedded = true;
            Logger.Success($"EmbedToDesktop: Embedded to {hostType} (HwndSource)");
            window.Hide();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("EmbedToDesktop: Create HwndSource failed", ex);
            RestoreRenderMode();
            window.Content = content;
            _embedContent = null;
            _embedSource?.Dispose();
            _embedSource = null;
            return false;
        }
    }

    private bool TryFindWorkerW(out IntPtr workerWithoutDefView, out IntPtr workerWithDefView)
    {
        IntPtr without = IntPtr.Zero;
        IntPtr with = IntPtr.Zero;

        for (int i = 0; i < 5; i++)
        {
            EnumWindows((hWnd, lParam) =>
            {
                var className = GetWindowClassName(hWnd);
                if (className == "WorkerW")
                {
                    var defView = Win32Api.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                    {
                        with = hWnd;
                    }
                    else if (without == IntPtr.Zero)
                    {
                        without = hWnd;
                    }
                }

                return !(without != IntPtr.Zero && with != IntPtr.Zero);
            }, IntPtr.Zero);

            if (without != IntPtr.Zero || with != IntPtr.Zero)
            {
                if (with != IntPtr.Zero)
                {
                    Logger.Info($"FindWorkerW: Found WorkerW (with DefView) = 0x{with:X}");
                }
                if (without != IntPtr.Zero)
                {
                    Logger.Info($"FindWorkerW: Found WorkerW (no DefView) = 0x{without:X}");
                }

                workerWithoutDefView = without;
                workerWithDefView = with;
                return true;
            }

            Thread.Sleep(50);
        }

        workerWithoutDefView = without;
        workerWithDefView = with;
        return false;
    }

    private IntPtr EmbedWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST && _hitTestRoot != null && _clickThroughElement != null)
        {
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)(lParam.ToInt32() >> 16);
            var point = _hitTestRoot.PointFromScreen(new Point(x, y));

            if (IsPointOverElement(_clickThroughElement, _hitTestRoot, point))
            {
                handled = false;
                return IntPtr.Zero;
            }

            handled = true;
            return new IntPtr(HTTRANSPARENT);
        }

        return IntPtr.Zero;
    }

    private static bool IsPointOverElement(FrameworkElement element, FrameworkElement root, Point point)
    {
        if (element.Visibility != Visibility.Visible)
        {
            return false;
        }

        try
        {
            var transform = element.TransformToAncestor(root);
            var topLeft = transform.Transform(new Point(0, 0));
            var rect = new Rect(topLeft, new Size(element.ActualWidth, element.ActualHeight));
            return rect.Contains(point);
        }
        catch
        {
            return false;
        }
    }

    private void LogWindowRect(IntPtr hWnd, string name)
    {
        if (hWnd == IntPtr.Zero) return;
        if (Win32Api.GetWindowRect(hWnd, out var rect))
        {
            Logger.Info($"{name} Rect=({rect.Left}, {rect.Top})-({rect.Right}, {rect.Bottom})");
        }
        else
        {
            Logger.Warn($"{name} GetWindowRect failed");
        }
    }

    private bool TryGetWindowRect(IntPtr hWnd, out Win32Api.RECT rect)
    {
        if (hWnd == IntPtr.Zero)
        {
            rect = default;
            return false;
        }

        return Win32Api.GetWindowRect(hWnd, out rect);
    }

    private void RestoreRenderMode()
    {
        if (_renderModeChanged)
        {
            RenderOptions.ProcessRenderMode = _savedRenderMode;
            _renderModeChanged = false;
        }
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
