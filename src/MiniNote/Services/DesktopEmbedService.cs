using System;
using System.Windows;
using System.Windows.Interop;
using MiniNote.Helpers;

namespace MiniNote.Services;

/// <summary>
/// 桌面嵌入服务 - 将窗口嵌入到桌面壁纸层
/// </summary>
public class DesktopEmbedService
{
    private IntPtr _workerW = IntPtr.Zero;
    private IntPtr _originalParent = IntPtr.Zero;
    private bool _isEmbedded = false;

    /// <summary>
    /// 是否已嵌入桌面
    /// </summary>
    public bool IsEmbedded => _isEmbedded;

    /// <summary>
    /// 将窗口嵌入桌面
    /// </summary>
    public bool EmbedToDesktop(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return false;

            // 保存原始父窗口
            _originalParent = Win32Api.GetParent(hwnd);

            // 获取桌面 WorkerW 窗口
            _workerW = GetDesktopWorkerW();
            if (_workerW == IntPtr.Zero) return false;

            // 设置窗口样式，避免在任务栏显示
            int exStyle = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_EXSTYLE);
            exStyle |= Win32Api.WS_EX_TOOLWINDOW;
            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_EXSTYLE, exStyle);

            // 将窗口设置为 WorkerW 的子窗口
            Win32Api.SetParent(hwnd, _workerW);

            _isEmbedded = true;
            return true;
        }
        catch
        {
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

            // 恢复原始父窗口
            Win32Api.SetParent(hwnd, _originalParent);

            // 移除工具窗口样式
            int exStyle = Win32Api.GetWindowLong(hwnd, Win32Api.GWL_EXSTYLE);
            exStyle &= ~Win32Api.WS_EX_TOOLWINDOW;
            Win32Api.SetWindowLong(hwnd, Win32Api.GWL_EXSTYLE, exStyle);

            _isEmbedded = false;
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 获取桌面 WorkerW 窗口句柄
    /// </summary>
    private IntPtr GetDesktopWorkerW()
    {
        // 获取 Progman 窗口
        IntPtr progman = Win32Api.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        // 发送消息让 Windows 创建 WorkerW
        Win32Api.SendMessageTimeout(
            progman,
            Win32Api.WM_SPAWN_WORKER,
            IntPtr.Zero,
            IntPtr.Zero,
            Win32Api.SMTO_NORMAL,
            1000,
            out _
        );

        // 查找 WorkerW 窗口
        IntPtr workerW = IntPtr.Zero;
        IntPtr shell = Win32Api.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);

        while (shell != IntPtr.Zero)
        {
            IntPtr defView = Win32Api.FindWindowEx(shell, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                // 找到包含 SHELLDLL_DefView 的 WorkerW，我们需要的是下一个
                workerW = Win32Api.FindWindowEx(IntPtr.Zero, shell, "WorkerW", null);
                break;
            }
            shell = Win32Api.FindWindowEx(IntPtr.Zero, shell, "WorkerW", null);
        }

        return workerW;
    }
}
