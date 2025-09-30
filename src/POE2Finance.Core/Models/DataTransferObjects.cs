using POE2Finance.Core.Enums;

namespace POE2Finance.Core.Models;

/// <summary>
/// 价格数据传输对象
/// </summary>
public class PriceDataDto
{
    /// <summary>
    /// 通货类型
    /// </summary>
    public CurrencyType CurrencyType { get; set; }

    /// <summary>
    /// 通货名称
    /// </summary>
    public string CurrencyName { get; set; } = string.Empty;

    /// <summary>
    /// 当前价格（以崇高石计价）
    /// </summary>
    public decimal CurrentPriceInExalted { get; set; }

    /// <summary>
    /// 前一期价格（以崇高石计价）
    /// </summary>
    public decimal? PreviousPriceInExalted { get; set; }

    /// <summary>
    /// 价格变动百分比
    /// </summary>
    public decimal? ChangePercent { get; set; }

    /// <summary>
    /// 交易量
    /// </summary>
    public int? TradeVolume { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    public DataSource DataSource { get; set; }

    /// <summary>
    /// 采集时间
    /// </summary>
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// 热点物品分析数据
/// </summary>
public class HotItemAnalysisDto
{
    /// <summary>
    /// 通货类型
    /// </summary>
    public CurrencyType CurrencyType { get; set; }

    /// <summary>
    /// 通货名称
    /// </summary>
    public string CurrencyName { get; set; } = string.Empty;

    /// <summary>
    /// 热度评分（0-100）
    /// </summary>
    public decimal HotScore { get; set; }

    /// <summary>
    /// 价格波动幅度
    /// </summary>
    public decimal PriceVolatility { get; set; }

    /// <summary>
    /// 交易量变化百分比
    /// </summary>
    public decimal? VolumeChangePercent { get; set; }

    /// <summary>
    /// 趋势持续时长（小时）
    /// </summary>
    public int TrendDurationHours { get; set; }

    /// <summary>
    /// 趋势类型
    /// </summary>
    public TrendType TrendType { get; set; }

    /// <summary>
    /// 推荐操作
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// 市场分析结果数据
/// </summary>
public class MarketAnalysisResultDto
{
    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalysisTime { get; set; }

    /// <summary>
    /// 时间段
    /// </summary>
    public PublishTimeSlot TimeSlot { get; set; }

    /// <summary>
    /// 热点物品列表
    /// </summary>
    public List<HotItemAnalysisDto> HotItems { get; set; } = new();

    /// <summary>
    /// 市场整体趋势
    /// </summary>
    public TrendType OverallTrend { get; set; }

    /// <summary>
    /// 主要市场动态描述
    /// </summary>
    public string MarketDynamics { get; set; } = string.Empty;

    /// <summary>
    /// 交易建议
    /// </summary>
    public string TradingAdvice { get; set; } = string.Empty;

    /// <summary>
    /// 风险提示
    /// </summary>
    public string RiskWarning { get; set; } = string.Empty;
}

/// <summary>
/// 视频生成配置数据
/// </summary>
public class VideoGenerationConfigDto
{
    /// <summary>
    /// 视频标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 时间段
    /// </summary>
    public PublishTimeSlot TimeSlot { get; set; }

    /// <summary>
    /// 背景音乐文件路径
    /// </summary>
    public string? BackgroundMusicPath { get; set; }

    /// <summary>
    /// 视频时长（秒）
    /// </summary>
    public int DurationSeconds { get; set; } = 90;

    /// <summary>
    /// 视频分辨率宽度
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// 视频分辨率高度
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// 输出文件路径
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
}