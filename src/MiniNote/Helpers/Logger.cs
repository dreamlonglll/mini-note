using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MiniNote.Helpers;

/// <summary>
/// 日志帮助类 - Debug 模式下输出到控制台
/// </summary>
public static class Logger
{
    private static bool _consoleAllocated = false;
    private static StreamWriter? _consoleWriter;

    /// <summary>
    /// 初始化控制台（仅 Debug 模式）
    /// </summary>
    [Conditional("DEBUG")]
    public static void InitializeConsole()
    {
        if (_consoleAllocated) return;

        try
        {
            // 分配控制台
            Win32Api.AllocConsole();
            Win32Api.SetConsoleTitle("MiniNote Debug Console");

            // 重定向标准输出到控制台
            var stdOut = new IntPtr(7); // STD_OUTPUT_HANDLE
            var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE = -11

            if (handle != IntPtr.Zero)
            {
                var fs = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false), FileAccess.Write);
                _consoleWriter = new StreamWriter(fs) { AutoFlush = true };
                Console.SetOut(_consoleWriter);
            }

            _consoleAllocated = true;

            Log("Debug console initialized");
            Log($"Application started at {DateTime.Now}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize console: {ex.Message}");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>
    /// 释放控制台
    /// </summary>
    [Conditional("DEBUG")]
    public static void CloseConsole()
    {
        if (!_consoleAllocated) return;

        try
        {
            Log("Closing debug console...");
            _consoleWriter?.Dispose();
            Win32Api.FreeConsole();
            _consoleAllocated = false;
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 输出日志
    /// </summary>
    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        Console.WriteLine(logMessage);
        Debug.WriteLine(logMessage);
    }

    /// <summary>
    /// 输出信息日志
    /// </summary>
    [Conditional("DEBUG")]
    public static void Info(string message)
    {
        Log($"[INFO] {message}");
    }

    /// <summary>
    /// 输出警告日志
    /// </summary>
    [Conditional("DEBUG")]
    public static void Warn(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Log($"[WARN] {message}");
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// 输出错误日志
    /// </summary>
    [Conditional("DEBUG")]
    public static void Error(string message, Exception? ex = null)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Log($"[ERROR] {message}");
        if (ex != null)
        {
            Log($"[ERROR] Exception: {ex.Message}");
            Log($"[ERROR] StackTrace: {ex.StackTrace}");
        }
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// 输出成功日志
    /// </summary>
    [Conditional("DEBUG")]
    public static void Success(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Log($"[OK] {message}");
        Console.ForegroundColor = originalColor;
    }
}
