using System.ComponentModel.DataAnnotations;
using POE2Finance.Core.Enums;

namespace POE2Finance.Core.Entities;

/// <summary>
/// 通货元数据实体，存储通货的基础信息
/// </summary>
public class CurrencyMetadata : BaseEntity
{
    /// <summary>
    /// 通货类型
    /// </summary>
    public CurrencyType CurrencyType { get; set; }

    /// <summary>
    /// 通货中文名称
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ChineseName { get; set; } = string.Empty;

    /// <summary>
    /// 通货英文名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EnglishName { get; set; } = string.Empty;

    /// <summary>
    /// 通货简称（如E、D、C）
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// 通货描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否为基准计价单位（崇高石为true）
    /// </summary>
    public bool IsBaseCurrency { get; set; }

    /// <summary>
    /// 是否启用价格监控
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 相关的价格记录
    /// </summary>
    public virtual ICollection<CurrencyPrice> Prices { get; set; } = new List<CurrencyPrice>();
}