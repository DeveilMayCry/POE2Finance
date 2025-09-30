using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POE2Finance.Core.Enums;

namespace POE2Finance.Core.Entities;

/// <summary>
/// 通货价格实体，存储历史价格数据
/// </summary>
public class CurrencyPrice : BaseEntity
{
    /// <summary>
    /// 通货类型
    /// </summary>
    public CurrencyType CurrencyType { get; set; }

    /// <summary>
    /// 当前价格（以崇高石为计价单位）
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal PriceInExalted { get; set; }

    /// <summary>
    /// 原始价格值（各平台的原始数据）
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// 原始价格的计价单位
    /// </summary>
    [MaxLength(20)]
    public string OriginalPriceUnit { get; set; } = string.Empty;

    /// <summary>
    /// 交易量
    /// </summary>
    public int? TradeVolume { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    public DataSource DataSource { get; set; }

    /// <summary>
    /// 数据采集时间
    /// </summary>
    public DateTime CollectedAt { get; set; }

    /// <summary>
    /// 数据有效性标记
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 备注信息
    /// </summary>
    [MaxLength(200)]
    public string? Notes { get; set; }

    /// <summary>
    /// 关联的通货元数据
    /// </summary>
    public virtual CurrencyMetadata? CurrencyMetadataInfo { get; set; }
}