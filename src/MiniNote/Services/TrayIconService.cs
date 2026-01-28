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

    public event Action? OnToggleEmbed;
    public event Action? OnAddTodo;
    public event Action? OnResetPosition;
    public event Action? OnExit;

    private ToolStripMenuItem? _embedMenuItem;

    public void Initialize()
    {
        Logger.Info("TrayIconService: Initializing");

        // 创建右键菜单
        _contextMenu = new ContextMenuStrip();

        // 固定/取消固定
        _embedMenuItem = new ToolStripMenuItem("固定到桌面");
        _embedMenuItem.Click += (s, e) => OnToggleEmbed?.Invoke();
        _contextMenu.Items.Add(_embedMenuItem);

        // 添加待办项
        var addTodoItem = new ToolStripMenuItem("添加待办项");
        addTodoItem.Click += (s, e) => OnAddTodo?.Invoke();
        _contextMenu.Items.Add(addTodoItem);

        // 重置位置
        var resetPositionItem = new ToolStripMenuItem("重置位置");
        resetPositionItem.Click += (s, e) => OnResetPosition?.Invoke();
        _contextMenu.Items.Add(resetPositionItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 退出
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

        // 双击固定/取消固定
        _notifyIcon.DoubleClick += (s, e) => OnToggleEmbed?.Invoke();

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
        if (_embedMenuItem != null)
        {
            _embedMenuItem.Text = isEmbedded ? "取消固定" : "固定到桌面";
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
