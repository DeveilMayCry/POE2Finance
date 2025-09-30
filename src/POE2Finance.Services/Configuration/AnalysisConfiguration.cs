namespace POE2Finance.Services.Configuration;

/// <summary>
/// 分析引擎配置
/// </summary>
public class AnalysisConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Analysis";

    /// <summary>
    /// 强势趋势阈值（百分比）
    /// </summary>
    public decimal StrongTrendThreshold { get; set; } = 10.0m;

    /// <summary>
    /// 温和趋势阈值（百分比）
    /// </summary>
    public decimal ModerateTrendThreshold { get; set; } = 5.0m;

    /// <summary>
    /// 高波动率阈值（百分比）
    /// </summary>
    public decimal HighVolatilityThreshold { get; set; } = 15.0m;

    /// <summary>
    /// 交易量异常变化阈值（百分比）
    /// </summary>
    public decimal VolumeAnomalyThreshold { get; set; } = 50.0m;

    /// <summary>
    /// 热点物品最小热度评分
    /// </summary>
    public decimal MinHotScore { get; set; } = 30.0m;

    /// <summary>
    /// 热度评分权重配置
    /// </summary>
    public HotScoreWeights HotScoreWeights { get; set; } = new();

    /// <summary>
    /// 趋势分析配置
    /// </summary>
    public TrendAnalysisConfig TrendAnalysis { get; set; } = new();

    /// <summary>
    /// 价格预测配置
    /// </summary>
    public PricePredictionConfig PricePrediction { get; set; } = new();
}

/// <summary>
/// 热度评分权重配置
/// </summary>
public class HotScoreWeights
{
    /// <summary>
    /// 价格波动权重
    /// </summary>
    public decimal VolatilityWeight { get; set; } = 0.4m;

    /// <summary>
    /// 交易量变化权重
    /// </summary>
    public decimal VolumeWeight { get; set; } = 0.35m;

    /// <summary>
    /// 趋势一致性权重
    /// </summary>
    public decimal TrendWeight { get; set; } = 0.25m;
}

/// <summary>
/// 趋势分析配置
/// </summary>
public class TrendAnalysisConfig
{
    /// <summary>
    /// 短期趋势分析小时数
    /// </summary>
    public int ShortTermHours { get; set; } = 6;

    /// <summary>
    /// 中期趋势分析小时数
    /// </summary>
    public int MediumTermHours { get; set; } = 24;

    /// <summary>
    /// 长期趋势分析小时数
    /// </summary>
    public int LongTermHours { get; set; } = 168; // 7天

    /// <summary>
    /// 趋势确认所需的最小数据点数
    /// </summary>
    public int MinDataPointsForTrend { get; set; } = 3;

    /// <summary>
    /// 趋势反转检测敏感度
    /// </summary>
    public decimal ReversalSensitivity { get; set; } = 0.8m;
}

/// <summary>
/// 价格预测配置
/// </summary>
public class PricePredictionConfig
{
    /// <summary>
    /// 是否启用价格预测
    /// </summary>
    public bool EnablePrediction { get; set; } = true;

    /// <summary>
    /// 预测时间范围（小时）
    /// </summary>
    public int PredictionHours { get; set; } = 24;

    /// <summary>
    /// 历史数据样本大小
    /// </summary>
    public int HistoricalDataSampleSize { get; set; } = 168; // 7天

    /// <summary>
    /// 预测模型类型
    /// </summary>
    public PredictionModelType ModelType { get; set; } = PredictionModelType.LinearRegression;

    /// <summary>
    /// 预测置信度阈值
    /// </summary>
    public decimal ConfidenceThreshold { get; set; } = 0.6m;
}

/// <summary>
/// 预测模型类型
/// </summary>
public enum PredictionModelType
{
    /// <summary>
    /// 线性回归
    /// </summary>
    LinearRegression = 1,

    /// <summary>
    /// 移动平均
    /// </summary>
    MovingAverage = 2,

    /// <summary>
    /// 指数平滑
    /// </summary>
    ExponentialSmoothing = 3,

    /// <summary>
    /// 趋势分解
    /// </summary>
    TrendDecomposition = 4
}