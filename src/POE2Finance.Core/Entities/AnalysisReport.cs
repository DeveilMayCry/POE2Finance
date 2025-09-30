using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POE2Finance.Core.Enums;

namespace POE2Finance.Core.Entities;

/// <summary>
/// 分析报告实体，存储每次生成的价格分析报告
/// </summary>
public class AnalysisReport : BaseEntity
{
    /// <summary>
    /// 报告日期
    /// </summary>
    public DateTime ReportDate { get; set; }

    /// <summary>
    /// 发布时间段
    /// </summary>
    public PublishTimeSlot TimeSlot { get; set; }

    /// <summary>
    /// 报告标题
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 报告摘要
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 详细分析内容
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")]
    public string DetailedAnalysis { get; set; } = string.Empty;

    /// <summary>
    /// 交易建议
    /// </summary>
    [MaxLength(1000)]
    public string? TradingAdvice { get; set; }

    /// <summary>
    /// 风险提示
    /// </summary>
    [MaxLength(500)]
    public string? RiskWarning { get; set; }

    /// <summary>
    /// 热点物品数据（JSON格式）
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string HotItemsData { get; set; } = string.Empty;

    /// <summary>
    /// 趋势数据（JSON格式）
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string TrendData { get; set; } = string.Empty;

    /// <summary>
    /// 报告状态
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Generating;

    /// <summary>
    /// 报告生成时长（毫秒）
    /// </summary>
    public long? GenerationTimeMs { get; set; }

    /// <summary>
    /// 相关的视频记录
    /// </summary>
    public virtual ICollection<VideoRecord> Videos { get; set; } = new List<VideoRecord>();
}