using Microsoft.Win32;
using MiniNote.Helpers;
using System;
using System.Reflection;

namespace MiniNote.Services;

/// <summary>
/// 开机自启动服务
/// </summary>
public static class AutoStartService
{
    private const string AppName = "MiniNote";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// 检查是否已设置开机自启
    /// </summary>
    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            Logger.Error($"AutoStartService: Failed to check auto start status - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 启用开机自启
    /// </summary>
    public static bool EnableAutoStart()
    {
        try
        {
            var exePath = GetExecutablePath();
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\" --startup");
            Logger.Info("AutoStartService: Auto start enabled");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"AutoStartService: Failed to enable auto start - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 禁用开机自启
    /// </summary>
    public static bool DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
            Logger.Info("AutoStartService: Auto start disabled");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"AutoStartService: Failed to disable auto start - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 切换开机自启状态
    /// </summary>
    public static bool ToggleAutoStart()
    {
        if (IsAutoStartEnabled())
        {
            return DisableAutoStart();
        }
        else
        {
            return EnableAutoStart();
        }
    }

    /// <summary>
    /// 设置开机自启状态
    /// </summary>
    public static bool SetAutoStart(bool enabled)
    {
        if (enabled)
        {
            return EnableAutoStart();
        }
        else
        {
            return DisableAutoStart();
        }
    }

    private static string GetExecutablePath()
    {
        return Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
    }
}
