using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;
using POE2Finance.Data.Repositories;
using POE2Finance.Services.Configuration;

namespace POE2Finance.Services.Analysis;

/// <summary>
/// 价格分析服务实现
/// </summary>
public class PriceAnalysisService : IPriceAnalysisService
{
    private readonly ICurrencyPriceRepository _priceRepository;
    private readonly ILogger<PriceAnalysisService> _logger;
    private readonly AnalysisConfiguration _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="priceRepository">价格仓储</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">分析配置</param>
    public PriceAnalysisService(
        ICurrencyPriceRepository priceRepository,
        ILogger<PriceAnalysisService> logger,
        IOptions<AnalysisConfiguration> config)
    {
        _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public async Task<List<HotItemAnalysisDto>> AnalyzeHotItemsAsync(PublishTimeSlot timeSlot, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始分析热点物品，时间段: {TimeSlot}", timeSlot);

        var hotItems = new List<HotItemAnalysisDto>();
        var currencies = new[] { CurrencyType.ExaltedOrb, CurrencyType.DivineOrb, CurrencyType.ChaosOrb };

        foreach (var currency in currencies)
        {
            var hotItem = await AnalyzeSingleCurrencyAsync(currency, timeSlot, cancellationToken);
            if (hotItem != null)
            {
                hotItems.Add(hotItem);
            }
        }

        // 按热度评分排序
        hotItems.Sort((x, y) => y.HotScore.CompareTo(x.HotScore));

        _logger.LogInformation("完成热点物品分析，找到 {Count} 个热点物品", hotItems.Count);
        return hotItems;
    }

    /// <inheritdoc/>
    public async Task<MarketAnalysisResultDto> GenerateMarketAnalysisAsync(PublishTimeSlot timeSlot, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成市场分析报告，时间段: {TimeSlot}", timeSlot);

        var hotItems = await AnalyzeHotItemsAsync(timeSlot, cancellationToken);
        var overallTrend = CalculateOverallTrend(hotItems);
        var marketDynamics = GenerateMarketDynamicsDescription(hotItems, timeSlot);
        var tradingAdvice = GenerateTradingAdvice(hotItems, overallTrend);
        var riskWarning = GenerateRiskWarning(hotItems, overallTrend);

        var result = new MarketAnalysisResultDto
        {
            AnalysisTime = DateTime.UtcNow,
            TimeSlot = timeSlot,
            HotItems = hotItems,
            OverallTrend = overallTrend,
            MarketDynamics = marketDynamics,
            TradingAdvice = tradingAdvice,
            RiskWarning = riskWarning
        };

        _logger.LogInformation("完成市场分析报告生成");
        return result;
    }

    /// <inheritdoc/>
    public TrendType CalculateTrendType(List<CurrencyPrice> priceHistory, int hours = 24)
    {
        if (priceHistory.Count < 2)
            return TrendType.Sideways;

        var sortedPrices = priceHistory.OrderBy(p => p.CollectedAt).ToList();
        var recentPrices = sortedPrices.TakeLast(Math.Min(sortedPrices.Count, hours)).ToList();

        if (recentPrices.Count < 2)
            return TrendType.Sideways;

        var firstPrice = recentPrices.First().PriceInExalted;
        var lastPrice = recentPrices.Last().PriceInExalted;
        var changePercent = (lastPrice - firstPrice) / firstPrice * 100;

        // 计算价格波动的一致性
        var upCount = 0;
        var downCount = 0;

        for (int i = 1; i < recentPrices.Count; i++)
        {
            var change = recentPrices[i].PriceInExalted - recentPrices[i - 1].PriceInExalted;
            if (change > 0) upCount++;
            else if (change < 0) downCount++;
        }

        // 使用if-else结构替代switch表达式，因为配置值不能用作常量
        if (changePercent > _config.StrongTrendThreshold && upCount > downCount * 2)
            return TrendType.StrongUptrend;
        else if (changePercent > _config.ModerateTrendThreshold && upCount > downCount)
            return TrendType.ModerateUptrend;
        else if (changePercent < -_config.StrongTrendThreshold && downCount > upCount * 2)
            return TrendType.StrongDowntrend;
        else if (changePercent < -_config.ModerateTrendThreshold && downCount > upCount)
            return TrendType.ModerateDowntrend;
        else
            return TrendType.Sideways;
    }

    /// <summary>
    /// 分析单个通货
    /// </summary>
    /// <param name="currency">通货类型</param>
    /// <param name="timeSlot">时间段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热点物品分析结果</returns>
    private async Task<HotItemAnalysisDto?> AnalyzeSingleCurrencyAsync(CurrencyType currency, PublishTimeSlot timeSlot, CancellationToken cancellationToken)
    {
        try
        {
            var analysisHours = GetAnalysisHours(timeSlot);
            var startTime = DateTime.UtcNow.AddHours(-analysisHours);
            var endTime = DateTime.UtcNow;

            var priceHistory = await _priceRepository.GetPriceHistoryAsync(currency, startTime, endTime, null, cancellationToken);
            
            if (priceHistory.Count < 2)
            {
                _logger.LogWarning("通货 {Currency} 价格历史数据不足", currency);
                return null;
            }

            var hotScore = CalculateHotScore(priceHistory);
            var volatility = CalculatePriceVolatility(priceHistory);
            var volumeChange = CalculateVolumeChange(priceHistory);
            var trendDuration = CalculateTrendDuration(priceHistory);
            var trendType = CalculateTrendType(priceHistory, analysisHours);
            var recommendedAction = GenerateRecommendedAction(trendType, hotScore, volatility);

            return new HotItemAnalysisDto
            {
                CurrencyType = currency,
                CurrencyName = GetCurrencyDisplayName(currency),
                HotScore = hotScore,
                PriceVolatility = volatility,
                VolumeChangePercent = volumeChange,
                TrendDurationHours = trendDuration,
                TrendType = trendType,
                RecommendedAction = recommendedAction
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析通货 {Currency} 失败", currency);
            return null;
        }
    }

    /// <summary>
    /// 计算热度评分
    /// </summary>
    /// <param name="priceHistory">价格历史</param>
    /// <returns>热度评分（0-100）</returns>
    private decimal CalculateHotScore(List<CurrencyPrice> priceHistory)
    {
        if (priceHistory.Count < 2) return 0;

        var volatility = CalculatePriceVolatility(priceHistory);
        var volumeChange = CalculateVolumeChange(priceHistory) ?? 0;
        var trendConsistency = CalculateTrendConsistency(priceHistory);

        // 加权计算热度评分
        var score = (volatility * _config.HotScoreWeights.VolatilityWeight) +
                   (Math.Abs(volumeChange) * _config.HotScoreWeights.VolumeWeight) +
                   (trendConsistency * _config.HotScoreWeights.TrendWeight);

        return Math.Min(100, Math.Max(0, score));
    }

    /// <summary>
    /// 计算价格波动率
    /// </summary>
    /// <param name="priceHistory">价格历史</param>
    /// <returns>波动率百分比</returns>
    private decimal CalculatePriceVolatility(List<CurrencyPrice> priceHistory)
    {
        if (priceHistory.Count < 2) return 0;

        var prices = priceHistory.Select(p => p.PriceInExalted).ToList();
        var minPrice = prices.Min();
        var maxPrice = prices.Max();

        if (minPrice == 0) return 0;

        return (maxPrice - minPrice) / minPrice * 100;
    }

    /// <summary>
    /// 计算交易量变化
    /// </summary>
    /// <param name="priceHistory">价格历史</param>
    /// <returns>交易量变化百分比</returns>
    private decimal? CalculateVolumeChange(List<CurrencyPrice> priceHistory)
    {
        var volumeData = priceHistory.Where(p => p.TradeVolume.HasValue).ToList();
        if (volumeData.Count < 2) return null;

        var sortedVolumes = volumeData.OrderBy(p => p.CollectedAt).ToList();
        var firstHalf = sortedVolumes.Take(sortedVolumes.Count / 2).ToList();
        var secondHalf = sortedVolumes.Skip(sortedVolumes.Count / 2).ToList();

        var avgFirst = firstHalf.Average(p => p.TradeVolume!.Value);
        var avgSecond = secondHalf.Average(p => p.TradeVolume!.Value);

        if (avgFirst == 0) return null;

        return (decimal)(avgSecond - avgFirst) / (decimal)avgFirst * 100;
    }

    /// <summary>
    /// 计算趋势持续时长
    /// </summary>
    /// <param name="priceHistory">价格历史</param>
    /// <returns>趋势持续小时数</returns>
    private int CalculateTrendDuration(List<CurrencyPrice> priceHistory)
    {
        if (priceHistory.Count < 2) return 0;

        var sortedPrices = priceHistory.OrderByDescending(p => p.CollectedAt).ToList();
        var currentTrend = DetermineTrendDirection(sortedPrices[0], sortedPrices[1]);
        
        int duration = 1;
        for (int i = 1; i < sortedPrices.Count - 1; i++)
        {
            var trend = DetermineTrendDirection(sortedPrices[i], sortedPrices[i + 1]);
            if (trend == currentTrend)
                duration++;
            else
                break;
        }

        return duration;
    }

    /// <summary>
    /// 计算趋势一致性
    /// </summary>
    /// <param name="priceHistory">价格历史</param>
    /// <returns>一致性评分（0-100）</returns>
    private decimal CalculateTrendConsistency(List<CurrencyPrice> priceHistory)
    {
        if (priceHistory.Count < 3) return 0;

        var sortedPrices = priceHistory.OrderBy(p => p.CollectedAt).ToList();
        var changes = new List<decimal>();

        for (int i = 1; i < sortedPrices.Count; i++)
        {
            var change = sortedPrices[i].PriceInExalted - sortedPrices[i - 1].PriceInExalted;
            changes.Add(change);
        }

        var positiveCount = changes.Count(c => c > 0);
        var negativeCount = changes.Count(c => c < 0);
        var neutralCount = changes.Count(c => c == 0);

        var maxCount = Math.Max(positiveCount, Math.Max(negativeCount, neutralCount));
        return (decimal)maxCount / changes.Count * 100;
    }

    /// <summary>
    /// 确定趋势方向
    /// </summary>
    /// <param name="current">当前价格</param>
    /// <param name="previous">前一个价格</param>
    /// <returns>趋势方向</returns>
    private static int DetermineTrendDirection(CurrencyPrice current, CurrencyPrice previous)
    {
        var change = current.PriceInExalted - previous.PriceInExalted;
        return change > 0 ? 1 : change < 0 ? -1 : 0;
    }

    /// <summary>
    /// 获取分析时间跨度
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <returns>分析小时数</returns>
    private static int GetAnalysisHours(PublishTimeSlot timeSlot)
    {
        return timeSlot switch
        {
            PublishTimeSlot.Morning => 12,   // 分析过去12小时
            PublishTimeSlot.Afternoon => 6,  // 分析过去6小时
            PublishTimeSlot.Evening => 24,   // 分析全天24小时
            _ => 24
        };
    }

    /// <summary>
    /// 计算整体市场趋势
    /// </summary>
    /// <param name="hotItems">热点物品列表</param>
    /// <returns>整体趋势</returns>
    private static TrendType CalculateOverallTrend(List<HotItemAnalysisDto> hotItems)
    {
        if (hotItems.Count == 0) return TrendType.Sideways;

        var trendScores = new Dictionary<TrendType, int>();
        foreach (var item in hotItems)
        {
            trendScores[item.TrendType] = trendScores.GetValueOrDefault(item.TrendType, 0) + 1;
        }

        return trendScores.OrderByDescending(kv => kv.Value).First().Key;
    }

    /// <summary>
    /// 生成市场动态描述
    /// </summary>
    /// <param name="hotItems">热点物品列表</param>
    /// <param name="timeSlot">时间段</param>
    /// <returns>市场动态描述</returns>
    private static string GenerateMarketDynamicsDescription(List<HotItemAnalysisDto> hotItems, PublishTimeSlot timeSlot)
    {
        if (hotItems.Count == 0)
            return "当前市场整体平稳，各主要通货价格波动较小。";

        var topItem = hotItems.First();
        var timeSlotName = GetTimeSlotDisplayName(timeSlot);

        return $"{timeSlotName}市场中，{topItem.CurrencyName}表现最为活跃，热度评分达到{topItem.HotScore:F1}。" +
               $"价格波动幅度为{topItem.PriceVolatility:F2}%，呈现{GetTrendDisplayName(topItem.TrendType)}态势。";
    }

    /// <summary>
    /// 生成交易建议
    /// </summary>
    /// <param name="hotItems">热点物品列表</param>
    /// <param name="overallTrend">整体趋势</param>
    /// <returns>交易建议</returns>
    private static string GenerateTradingAdvice(List<HotItemAnalysisDto> hotItems, TrendType overallTrend)
    {
        if (hotItems.Count == 0)
            return "建议继续观望，等待更明确的市场信号。";

        var advice = overallTrend switch
        {
            TrendType.StrongUptrend => "市场呈强势上涨趋势，建议适当增加持仓，但注意风险控制。",
            TrendType.ModerateUptrend => "市场温和上涨，可考虑逢低买入，分批建仓。",
            TrendType.StrongDowntrend => "市场下跌趋势明显，建议减仓观望，避免追高。",
            TrendType.ModerateDowntrend => "市场小幅下跌，可考虑分批买入，等待反弹机会。",
            _ => "市场横盘整理，建议高抛低吸，控制仓位。"
        };

        var topItem = hotItems.First();
        return $"{advice} 特别关注{topItem.CurrencyName}的价格动向，{topItem.RecommendedAction}";
    }

    /// <summary>
    /// 生成风险提示
    /// </summary>
    /// <param name="hotItems">热点物品列表</param>
    /// <param name="overallTrend">整体趋势</param>
    /// <returns>风险提示</returns>
    private static string GenerateRiskWarning(List<HotItemAnalysisDto> hotItems, TrendType overallTrend)
    {
        var warnings = new List<string>();

        if (hotItems.Any(item => item.PriceVolatility > 15))
        {
            warnings.Add("部分通货价格波动较大，请注意风险控制");
        }

        if (overallTrend == TrendType.StrongUptrend || overallTrend == TrendType.StrongDowntrend)
        {
            warnings.Add("市场趋势较为强烈，注意可能的反转风险");
        }

        if (hotItems.Any(item => item.VolumeChangePercent > 50))
        {
            warnings.Add("交易量出现异常变化，可能存在市场操纵风险");
        }

        if (warnings.Count == 0)
        {
            warnings.Add("市场风险相对较低，但仍需谨慎投资");
        }

        return string.Join("；", warnings) + "。投资有风险，请根据自身情况合理配置。";
    }

    /// <summary>
    /// 生成推荐操作
    /// </summary>
    /// <param name="trendType">趋势类型</param>
    /// <param name="hotScore">热度评分</param>
    /// <param name="volatility">波动率</param>
    /// <returns>推荐操作</returns>
    private static string GenerateRecommendedAction(TrendType trendType, decimal hotScore, decimal volatility)
    {
        return (trendType, hotScore, volatility) switch
        {
            (TrendType.StrongUptrend, > 70, _) => "强烈建议关注，可考虑适量买入",
            (TrendType.ModerateUptrend, > 50, _) => "建议关注，可考虑分批买入",
            (TrendType.StrongDowntrend, > 70, _) => "建议减仓或观望，等待企稳信号",
            (TrendType.ModerateDowntrend, > 50, _) => "可考虑逢低买入，控制仓位",
            (_, _, > 20) => "价格波动较大，建议谨慎操作",
            _ => "建议继续观望，等待更明确信号"
        };
    }

    /// <summary>
    /// 获取通货显示名称
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <returns>显示名称</returns>
    private static string GetCurrencyDisplayName(CurrencyType currencyType)
    {
        return currencyType switch
        {
            CurrencyType.ExaltedOrb => "崇高石",
            CurrencyType.DivineOrb => "神圣石",
            CurrencyType.ChaosOrb => "混沌石",
            _ => currencyType.ToString()
        };
    }

    /// <summary>
    /// 获取时间段显示名称
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <returns>显示名称</returns>
    private static string GetTimeSlotDisplayName(PublishTimeSlot timeSlot)
    {
        return timeSlot switch
        {
            PublishTimeSlot.Morning => "上午",
            PublishTimeSlot.Afternoon => "下午",
            PublishTimeSlot.Evening => "晚间",
            _ => "当前"
        };
    }

    /// <summary>
    /// 获取趋势显示名称
    /// </summary>
    /// <param name="trendType">趋势类型</param>
    /// <returns>显示名称</returns>
    private static string GetTrendDisplayName(TrendType trendType)
    {
        return trendType switch
        {
            TrendType.StrongUptrend => "强势上涨",
            TrendType.ModerateUptrend => "温和上涨",
            TrendType.Sideways => "横盘整理",
            TrendType.ModerateDowntrend => "温和下跌",
            TrendType.StrongDowntrend => "强势下跌",
            _ => "未知"
        };
    }
}