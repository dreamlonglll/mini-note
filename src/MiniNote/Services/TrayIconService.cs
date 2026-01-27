using System;
using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using MiniNote.Helpers;

namespace MiniNote.Services;

/// <summary>
/// 系统托盘图标服务
/// </summary>
public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;

    public event Action? OnShowWindow;
    public event Action? OnToggleEmbed;
    public event Action? OnExit;

    public void Initialize()
    {
        Logger.Info("TrayIconService: Initializing");

        // 创建右键菜单
        _contextMenu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("显示窗口");
        showItem.Click += (s, e) => OnShowWindow?.Invoke();
        _contextMenu.Items.Add(showItem);

        var toggleItem = new ToolStripMenuItem("切换嵌入模式");
        toggleItem.Click += (s, e) => OnToggleEmbed?.Invoke();
        _contextMenu.Items.Add(toggleItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (s, e) => OnExit?.Invoke();
        _contextMenu.Items.Add(exitItem);

        // 创建托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "MiniNote",
            ContextMenuStrip = _contextMenu
        };

        // 双击显示窗口
        _notifyIcon.DoubleClick += (s, e) => OnShowWindow?.Invoke();

        Logger.Success("TrayIconService: Initialized");
    }

    /// <summary>
    /// 显示气泡通知
    /// </summary>
    public void ShowNotification(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
    }

    /// <summary>
    /// 更新菜单项文本
    /// </summary>
    public void UpdateEmbedMenuText(bool isEmbedded)
    {
        if (_contextMenu?.Items[1] is ToolStripMenuItem item)
        {
            item.Text = isEmbedded ? "取消嵌入桌面" : "嵌入桌面";
        }
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;

        Logger.Info("TrayIconService: Disposed");
    }
}
