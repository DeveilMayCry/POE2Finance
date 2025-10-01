using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;
using POE2Finance.Core.Enums;
using POE2Finance.Services.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace POE2Finance.Services.AI;

/// <summary>
/// 内容生成服务实现
/// </summary>
public class ContentGenerationService : IContentGenerationService
{
    private readonly ILogger<ContentGenerationService> _logger;
    private readonly ContentGenerationConfiguration _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">内容生成配置</param>
    public ContentGenerationService(ILogger<ContentGenerationService> logger, IOptions<ContentGenerationConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public Task<string> GenerateReportContentAsync(MarketAnalysisResultDto analysisResult, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成分析报告文本内容，时间段: {TimeSlot}", analysisResult.TimeSlot);

        try
        {
            var timeSlotName = GetTimeSlotDisplayName(analysisResult.TimeSlot);
            var currentTime = analysisResult.AnalysisTime;

            var contentBuilder = new StringBuilder();

            // 生成开场白
            contentBuilder.AppendLine(GenerateOpening(timeSlotName, currentTime));
            contentBuilder.AppendLine();

            // 生成热点物品分析
            if (analysisResult.HotItems.Count > 0)
            {
                contentBuilder.AppendLine("【热点通货分析】");
                foreach (var item in analysisResult.HotItems.Take(3))
                {
                    contentBuilder.AppendLine(GenerateHotItemDescription(item));
                }
                contentBuilder.AppendLine();
            }

            // 生成市场动态分析
            contentBuilder.AppendLine("【市场动态】");
            contentBuilder.AppendLine(analysisResult.MarketDynamics);
            contentBuilder.AppendLine();

            // 生成整体趋势分析
            contentBuilder.AppendLine("【整体趋势】");
            contentBuilder.AppendLine(GenerateTrendAnalysis(analysisResult.OverallTrend, analysisResult.HotItems));
            contentBuilder.AppendLine();

            // 生成交易建议
            contentBuilder.AppendLine("【交易建议】");
            contentBuilder.AppendLine(analysisResult.TradingAdvice);
            contentBuilder.AppendLine();

            // 生成风险提示
            contentBuilder.AppendLine("【风险提示】");
            contentBuilder.AppendLine(analysisResult.RiskWarning);
            contentBuilder.AppendLine();

            // 生成结束语
            contentBuilder.AppendLine(GenerateClosing(timeSlotName));

            var content = contentBuilder.ToString();
            _logger.LogInformation("报告内容生成完成，总长度: {Length} 字符", content.Length);
            
            return Task.FromResult(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成报告内容失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public string GenerateVideoTitle(MarketAnalysisResultDto analysisResult, PublishTimeSlot timeSlot)
    {
        try
        {
            var timeSlotName = GetTimeSlotDisplayName(timeSlot);
            var date = analysisResult.AnalysisTime.ToString("MM-dd");
            var topItem = analysisResult.HotItems.FirstOrDefault();

            if (topItem != null)
            {
                var trendIcon = GetTrendIcon(topItem.TrendType);
                var changeDesc = GetChangeDescription(topItem.PriceVolatility);
                
                return $"【POE2国服】{timeSlotName}市场速报 {date} | {topItem.CurrencyName}{changeDesc} {trendIcon} | 热度{topItem.HotScore:F0}";
            }
            else
            {
                return $"【POE2国服】{timeSlotName}市场速报 {date} | 市场平稳观望中";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成视频标题失败");
            return $"【POE2国服】市场分析 {analysisResult.AnalysisTime:MM-dd}";
        }
    }

    /// <inheritdoc/>
    public string GenerateVideoDescription(MarketAnalysisResultDto analysisResult, PublishTimeSlot timeSlot)
    {
        try
        {
            var descBuilder = new StringBuilder();

            // 添加视频简介
            var timeSlotName = GetTimeSlotDisplayName(timeSlot);
            descBuilder.AppendLine($"🎯 {timeSlotName}市场分析 - {analysisResult.AnalysisTime:yyyy年MM月dd日 HH时mm分}");
            descBuilder.AppendLine();

            // 添加热点通货信息
            if (analysisResult.HotItems.Count > 0)
            {
                descBuilder.AppendLine("🔥 热点通货：");
                foreach (var item in analysisResult.HotItems.Take(3))
                {
                    var trendIcon = GetTrendIcon(item.TrendType);
                    descBuilder.AppendLine($"• {item.CurrencyName}: 热度{item.HotScore:F1} {trendIcon} 波动{item.PriceVolatility:F2}%");
                }
                descBuilder.AppendLine();
            }

            // 添加市场趋势
            var overallTrendIcon = GetTrendIcon(analysisResult.OverallTrend);
            descBuilder.AppendLine($"📈 整体趋势：{GetTrendDisplayName(analysisResult.OverallTrend)} {overallTrendIcon}");
            descBuilder.AppendLine();

            // 添加核心建议
            descBuilder.AppendLine("💡 核心建议：");
            var adviceLines = analysisResult.TradingAdvice.Split('。', StringSplitOptions.RemoveEmptyEntries);
            foreach (var advice in adviceLines.Take(2))
            {
                if (!string.IsNullOrWhiteSpace(advice))
                    descBuilder.AppendLine($"• {advice.Trim()}");
            }
            descBuilder.AppendLine();

            // 添加标准结尾
            descBuilder.AppendLine("⚠️ 投资有风险，请根据自身情况合理配置");
            descBuilder.AppendLine("📺 每日三次更新：上午09:00 | 下午15:00 | 晚间21:00");
            descBuilder.AppendLine("🔔 记得点赞关注，不错过每日行情分析！");
            descBuilder.AppendLine();
            descBuilder.AppendLine("#POE2 #流放之路2 #国服 #价格分析 #通货市场");

            return descBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成视频描述失败");
            return $"POE2国服市场分析 - {analysisResult.AnalysisTime:yyyy-MM-dd}";
        }
    }

    /// <inheritdoc/>
    public List<string> GenerateVideoTags(MarketAnalysisResultDto analysisResult, PublishTimeSlot timeSlot)
    {
        try
        {
            var tags = new List<string>
            {
                "POE2",
                "流放之路2",
                "POE2国服", 
                "通货分析",
                "价格分析",
                "市场行情"
            };

            // 添加时间段标签
            var timeSlotName = GetTimeSlotDisplayName(timeSlot);
            tags.Add($"{timeSlotName}分析");

            // 添加热点通货标签
            foreach (var item in analysisResult.HotItems.Take(3))
            {
                tags.Add(item.CurrencyName);
            }

            // 添加趋势标签
            var trendName = GetTrendDisplayName(analysisResult.OverallTrend);
            tags.Add(trendName);

            // 添加通用标签
            tags.AddRange(new[]
            {
                "游戏经济",
                "投资理财",
                "数据分析",
                "腾讯游戏"
            });

            return tags.Distinct().Take(10).ToList(); // B站标签限制
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成视频标签失败");
            return new List<string> { "POE2", "流放之路2", "国服", "价格分析" };
        }
    }

    /// <summary>
    /// 生成开场白
    /// </summary>
    /// <param name="timeSlotName">时间段名称</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>开场白内容</returns>
    private string GenerateOpening(string timeSlotName, DateTime currentTime)
    {
        var greetings = GetTimeSlotGreeting(timeSlotName);
        return $"{greetings}欢迎收看POE2国服{timeSlotName}市场分析。" +
               $"现在是{currentTime:MM月dd日HH点mm分}，我将为大家带来最新的通货价格分析和交易建议。";
    }

    /// <summary>
    /// 生成热点物品描述
    /// </summary>
    /// <param name="item">热点物品</param>
    /// <returns>描述文本</returns>
    private string GenerateHotItemDescription(HotItemAnalysisDto item)
    {
        var trendDesc = GetTrendDisplayName(item.TrendType);
        var intensityDesc = GetIntensityDescription(item.HotScore);
        
        return $"{item.CurrencyName}表现{intensityDesc}，热度评分{item.HotScore:F1}，" +
               $"价格波动幅度{item.PriceVolatility:F2}%，呈现{trendDesc}态势。{item.RecommendedAction}";
    }

    /// <summary>
    /// 生成趋势分析
    /// </summary>
    /// <param name="overallTrend">整体趋势</param>
    /// <param name="hotItems">热点物品列表</param>
    /// <returns>趋势分析内容</returns>
    private string GenerateTrendAnalysis(TrendType overallTrend, List<HotItemAnalysisDto> hotItems)
    {
        var trendDesc = GetTrendDisplayName(overallTrend);
        var activeItemsCount = hotItems.Count(item => item.HotScore > 50);
        
        return $"从整体市场来看，当前呈现{trendDesc}格局。" +
               $"在监控的主要通货中，有{activeItemsCount}种表现较为活跃。" +
               GetTrendImplication(overallTrend);
    }

    /// <summary>
    /// 生成结束语
    /// </summary>
    /// <param name="timeSlotName">时间段名称</param>
    /// <returns>结束语内容</returns>
    private string GenerateClosing(string timeSlotName)
    {
        var nextUpdateTime = GetNextUpdateTime(timeSlotName);
        return $"以上就是本期{timeSlotName}市场分析的全部内容。" +
               $"下一次更新将在{nextUpdateTime}，请大家持续关注。" +
               "投资有风险，请根据自身情况合理配置。感谢收看！";
    }

    /// <summary>
    /// 获取时间段问候语
    /// </summary>
    /// <param name="timeSlotName">时间段名称</param>
    /// <returns>问候语</returns>
    private static string GetTimeSlotGreeting(string timeSlotName)
    {
        return timeSlotName switch
        {
            "上午场" => "大家上午好！",
            "下午场" => "大家下午好！",
            "晚间场" => "大家晚上好！",
            _ => "大家好！"
        };
    }

    /// <summary>
    /// 获取强度描述
    /// </summary>
    /// <param name="hotScore">热度评分</param>
    /// <returns>强度描述</returns>
    private static string GetIntensityDescription(decimal hotScore)
    {
        return hotScore switch
        {
            >= 80 => "非常活跃",
            >= 60 => "较为活跃",
            >= 40 => "一般活跃",
            >= 20 => "相对平稳",
            _ => "表现平淡"
        };
    }

    /// <summary>
    /// 获取趋势图标
    /// </summary>
    /// <param name="trendType">趋势类型</param>
    /// <returns>趋势图标</returns>
    private static string GetTrendIcon(TrendType trendType)
    {
        return trendType switch
        {
            TrendType.StrongUptrend => "🚀",
            TrendType.ModerateUptrend => "📈",
            TrendType.Sideways => "➡️",
            TrendType.ModerateDowntrend => "📉",
            TrendType.StrongDowntrend => "⬇️",
            _ => "❓"
        };
    }

    /// <summary>
    /// 获取变化描述
    /// </summary>
    /// <param name="volatility">波动率</param>
    /// <returns>变化描述</returns>
    private static string GetChangeDescription(decimal volatility)
    {
        return volatility switch
        {
            >= 15 => "大幅波动",
            >= 10 => "明显波动",
            >= 5 => "小幅波动",
            _ => "微幅变化"
        };
    }

    /// <summary>
    /// 获取趋势影响描述
    /// </summary>
    /// <param name="trendType">趋势类型</param>
    /// <returns>影响描述</returns>
    private static string GetTrendImplication(TrendType trendType)
    {
        return trendType switch
        {
            TrendType.StrongUptrend => "市场情绪较为乐观，但需要注意高位风险。",
            TrendType.ModerateUptrend => "市场保持温和上涨趋势，适合逢低布局。",
            TrendType.Sideways => "市场处于整理阶段，建议耐心等待方向选择。",
            TrendType.ModerateDowntrend => "市场出现调整，可关注支撑位附近的机会。",
            TrendType.StrongDowntrend => "市场下跌压力较大，建议控制风险为主。",
            _ => "市场方向尚不明确，建议谨慎观望。"
        };
    }

    /// <summary>
    /// 获取下次更新时间
    /// </summary>
    /// <param name="timeSlotName">时间段名称</param>
    /// <returns>下次更新时间描述</returns>
    private static string GetNextUpdateTime(string timeSlotName)
    {
        return timeSlotName switch
        {
            "上午场" => "下午15点",
            "下午场" => "晚上21点",
            "晚间场" => "明天上午9点",
            _ => "下一个时间段"
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
            PublishTimeSlot.Morning => "上午场",
            PublishTimeSlot.Afternoon => "下午场",
            PublishTimeSlot.Evening => "晚间场",
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
            _ => "未知趋势"
        };
    }
}