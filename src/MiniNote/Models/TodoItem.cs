using System;
using System.ComponentModel.DataAnnotations;

namespace MiniNote.Models;

/// <summary>
/// 待办项实体
/// </summary>
public class TodoItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 待办内容
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 优先级：0-低，1-中，2-高
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// 分类ID
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 分类导航属性
    /// </summary>
    public TodoCategory? Category { get; set; }

    /// <summary>
    /// 提醒时间
    /// </summary>
    public DateTime? ReminderTime { get; set; }

    /// <summary>
    /// 是否已提醒
    /// </summary>
    public bool IsReminded { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }
}
