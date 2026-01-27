using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniNote.Models;

/// <summary>
/// 待办分类实体
/// </summary>
public class TodoCategory
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类颜色（十六进制）
    /// </summary>
    [MaxLength(7)]
    public string Color { get; set; } = "#007ACC";

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 该分类下的待办项
    /// </summary>
    public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
}
