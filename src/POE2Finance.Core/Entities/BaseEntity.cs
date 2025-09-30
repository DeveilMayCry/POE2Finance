using System.ComponentModel.DataAnnotations;

namespace POE2Finance.Core.Entities;

/// <summary>
/// 基础实体类，提供通用的主键和时间戳字段
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键标识
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}