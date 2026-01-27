using System.ComponentModel.DataAnnotations;

namespace MiniNote.Models;

/// <summary>
/// 应用设置实体
/// </summary>
public class AppSettings
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 窗口 X 坐标
    /// </summary>
    public double WindowX { get; set; } = 100;

    /// <summary>
    /// 窗口 Y 坐标
    /// </summary>
    public double WindowY { get; set; } = 100;

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double WindowWidth { get; set; } = 300;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public double WindowHeight { get; set; } = 400;

    /// <summary>
    /// 是否开机自启
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// 是否嵌入桌面
    /// </summary>
    public bool EmbedDesktop { get; set; } = true;

    /// <summary>
    /// 透明度 (0.0 - 1.0)
    /// </summary>
    public double Opacity { get; set; } = 0.85;
}
