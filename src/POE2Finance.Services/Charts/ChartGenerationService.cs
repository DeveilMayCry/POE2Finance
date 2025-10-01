using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;
using ScottPlot;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using POE2Finance.Services.Configuration;
using ScottColor = ScottPlot.Color;
using ImageColor = SixLabors.ImageSharp.Color;

namespace POE2Finance.Services.Charts;

/// <summary>
/// 图表生成服务实现
/// </summary>
public class ChartGenerationService : IChartGenerationService
{
    private readonly ILogger<ChartGenerationService> _logger;
    private readonly ChartConfiguration _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">图表配置</param>
    public ChartGenerationService(ILogger<ChartGenerationService> logger, IOptions<ChartConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public Task<string> GeneratePriceTrendChartAsync(List<PriceDataDto> priceData, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成价格趋势图，数据点数: {Count}", priceData.Count);

        try
        {
            if (priceData.Count == 0)
            {
                throw new ArgumentException("价格数据不能为空", nameof(priceData));
            }

            // 确保输出目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建ScottPlot图表
            var plt = new Plot();
            ConfigurePlotAppearance(plt);

            // 按通货类型分组数据
            var groupedData = priceData
                .GroupBy(p => p.CurrencyType)
                .OrderBy(g => g.Key)
                .ToList();

            var colors = GetCurrencyColors();
            var colorIndex = 0;

            foreach (var group in groupedData)
            {
                var sortedData = group.OrderBy(p => p.CollectedAt).ToList();
                if (sortedData.Count == 0) continue;

                var times = sortedData.Select(p => p.CollectedAt.ToOADate()).ToArray();
                var prices = sortedData.Select(p => (double)p.CurrentPriceInExalted).ToArray();

                var color = colorIndex < colors.Length ? colors[colorIndex] : ScottColor.FromHex("#808080");
                
                // 添加价格线
                var scatter = plt.Add.Scatter(times, prices);
                scatter.Color = color;
                scatter.LineWidth = _config.LineWidth;
                scatter.MarkerSize = _config.MarkerSize;
                scatter.LegendText = GetCurrencyDisplayName(group.Key);

                colorIndex++;
            }

            // 配置坐标轴
            plt.Axes.Left.Label.Text = "价格 (崇高石)";
            plt.Axes.Bottom.Label.Text = "时间";
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();

            // 添加图例
            plt.ShowLegend();

            // 设置标题
            plt.Title($"POE2通货价格趋势图 - {DateTime.Now:yyyy-MM-dd HH:mm}");

            // 保存图表
            plt.SavePng(outputPath, _config.Width, _config.Height);

            _logger.LogInformation("价格趋势图已保存到: {OutputPath}", outputPath);
            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成价格趋势图失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> GenerateHotItemsChartAsync(List<HotItemAnalysisDto> hotItems, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成热点物品对比图，物品数: {Count}", hotItems.Count);

        try
        {
            if (hotItems.Count == 0)
            {
                throw new ArgumentException("热点物品数据不能为空", nameof(hotItems));
            }

            // 确保输出目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建ScottPlot图表
            var plt = new Plot();
            ConfigurePlotAppearance(plt);

            // 准备数据
            var labels = hotItems.Select(item => item.CurrencyName).ToArray();
            var scores = hotItems.Select(item => (double)item.HotScore).ToArray();
            var positions = Enumerable.Range(0, hotItems.Count).Select(i => (double)i).ToArray();

            // 创建柱状图
            var bars = plt.Add.Bars(positions, scores);
            
            // 设置颜色
            var colors = GetHotItemColors(hotItems);
            for (int i = 0; i < bars.Bars.Count && i < colors.Length; i++)
            {
                bars.Bars[i].FillColor = colors[i];
            }

            // 配置坐标轴
            plt.Axes.Left.Label.Text = "热度评分";
            plt.Axes.Bottom.Label.Text = "通货类型";
            
            // 设置X轴标签
            plt.Axes.Bottom.SetTicks(positions, labels);
            plt.Axes.Bottom.TickLabelStyle.Rotation = -45;

            // 设置标题
            plt.Title($"热点通货对比图 - {DateTime.Now:yyyy-MM-dd HH:mm}");

            // 添加数值标签
            for (int i = 0; i < hotItems.Count; i++)
            {
                var text = plt.Add.Text($"{hotItems[i].HotScore:F1}", positions[i], scores[i] + 2);
                text.LabelAlignment = Alignment.MiddleCenter;
                text.LabelFontSize = 10;
            }

            // 保存图表
            plt.SavePng(outputPath, _config.Width, _config.Height);

            _logger.LogInformation("热点物品对比图已保存到: {OutputPath}", outputPath);
            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成热点物品对比图失败");
            throw;
        }
    }

    /// <summary>
    /// 生成市场趋势仪表盘
    /// </summary>
    /// <param name="analysisResult">市场分析结果</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的图片路径</returns>
    public async Task<string> GenerateMarketDashboardAsync(MarketAnalysisResultDto analysisResult, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成市场趋势仪表盘");

        try
        {
            // 确保输出目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 使用ImageSharp创建复合图表
            using var image = new Image<Rgba32>(_config.DashboardWidth, _config.DashboardHeight);
            
            // 设置背景色
            image.Mutate(ctx => ctx.Fill(ImageColor.White));

            // 添加标题
            await AddDashboardTitle(image, analysisResult);

            // 添加热点物品信息
            await AddHotItemsSection(image, analysisResult.HotItems);

            // 添加市场趋势信息
            await AddMarketTrendSection(image, analysisResult);

            // 保存图片
            await image.SaveAsPngAsync(outputPath, cancellationToken);

            _logger.LogInformation("市场趋势仪表盘已保存到: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成市场趋势仪表盘失败");
            throw;
        }
    }

    /// <summary>
    /// 配置图表外观
    /// </summary>
    /// <param name="plt">图表对象</param>
    private void ConfigurePlotAppearance(Plot plt)
    {
        // 设置背景色
        plt.FigureBackground.Color = ScottColor.FromHex(_config.BackgroundColor);
        plt.DataBackground.Color = ScottColor.FromHex(_config.PlotBackgroundColor);

        // 设置网格
        plt.Grid.MajorLineColor = ScottColor.FromHex(_config.GridColor);
        plt.Grid.MinorLineColor = ScottColor.FromHex(_config.GridColor).WithAlpha(0.3f);

        // 设置字体
        plt.Axes.Title.Label.FontSize = _config.TitleFontSize;
        plt.Axes.Left.Label.FontSize = _config.AxisLabelFontSize;
        plt.Axes.Bottom.Label.FontSize = _config.AxisLabelFontSize;
    }

    /// <summary>
    /// 获取通货颜色配置
    /// </summary>
    /// <returns>颜色数组</returns>
    private static ScottColor[] GetCurrencyColors()
    {
        return new[]
        {
            ScottColor.FromHex("#FFD700"), // 崇高石 - 金色
            ScottColor.FromHex("#8A2BE2"), // 神圣石 - 紫色
            ScottColor.FromHex("#FF6347")  // 混沌石 - 橙红色
        };
    }

    /// <summary>
    /// 获取热点物品颜色
    /// </summary>
    /// <param name="hotItems">热点物品列表</param>
    /// <returns>颜色数组</returns>
    private static ScottColor[] GetHotItemColors(List<HotItemAnalysisDto> hotItems)
    {
        var colors = new List<ScottColor>();
        var baseColors = GetCurrencyColors();

        for (int i = 0; i < hotItems.Count; i++)
        {
            var baseColor = i < baseColors.Length ? baseColors[i] : ScottColor.FromHex("#808080");
            
            // 根据热度评分调整颜色强度
            var intensity = Math.Min(1.0f, (float)(hotItems[i].HotScore / 100));
            colors.Add(baseColor.WithAlpha(0.5f + intensity * 0.5f));
        }

        return colors.ToArray();
    }

    /// <summary>
    /// 获取通货显示名称
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <returns>显示名称</returns>
    private static string GetCurrencyDisplayName(Core.Enums.CurrencyType currencyType)
    {
        return currencyType switch
        {
            Core.Enums.CurrencyType.ExaltedOrb => "崇高石",
            Core.Enums.CurrencyType.DivineOrb => "神圣石",
            Core.Enums.CurrencyType.ChaosOrb => "混沌石",
            _ => currencyType.ToString()
        };
    }

    /// <summary>
    /// 添加仪表盘标题
    /// </summary>
    /// <param name="image">图片对象</param>
    /// <param name="analysisResult">分析结果</param>
    private Task AddDashboardTitle(Image<Rgba32> image, MarketAnalysisResultDto analysisResult)
    {
        var timeSlotName = GetTimeSlotDisplayName(analysisResult.TimeSlot);
        var title = $"POE2国服市场分析 - {timeSlotName} ({analysisResult.AnalysisTime:yyyy-MM-dd HH:mm})";

        image.Mutate(ctx =>
        {
            ctx.DrawText(title, SystemFonts.CreateFont("Arial", 24, SixLabors.Fonts.FontStyle.Bold), 
                ImageColor.Black, new PointF(20, 20));
        });
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 添加热点物品信息区域
    /// </summary>
    /// <param name="image">图片对象</param>
    /// <param name="hotItems">热点物品列表</param>
    private Task AddHotItemsSection(Image<Rgba32> image, List<HotItemAnalysisDto> hotItems)
    {
        var y = 80;
        var font = SystemFonts.CreateFont("Arial", 16, SixLabors.Fonts.FontStyle.Bold);
        
        image.Mutate(ctx =>
        {
            ctx.DrawText("🔥 热点通货", font, ImageColor.Red, new PointF(20, y));
        });

        y += 30;
        var itemFont = SystemFonts.CreateFont("Arial", 14);

        foreach (var item in hotItems.Take(3))
        {
            var text = $"• {item.CurrencyName}: 热度{item.HotScore:F1} | 波动{item.PriceVolatility:F2}% | {GetTrendDisplayName(item.TrendType)}";
            
            image.Mutate(ctx =>
            {
                ctx.DrawText(text, itemFont, ImageColor.Black, new PointF(40, y));
            });
            
            y += 25;
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 添加市场趋势信息区域
    /// </summary>
    /// <param name="image">图片对象</param>
    /// <param name="analysisResult">分析结果</param>
    private Task AddMarketTrendSection(Image<Rgba32> image, MarketAnalysisResultDto analysisResult)
    {
        var y = 220;
        var font = SystemFonts.CreateFont("Arial", 16, SixLabors.Fonts.FontStyle.Bold);
        
        image.Mutate(ctx =>
        {
            ctx.DrawText("📈 市场动态", font, ImageColor.Blue, new PointF(20, y));
        });

        y += 30;
        var contentFont = SystemFonts.CreateFont("Arial", 12);

        // 添加市场动态描述
        var lines = WrapText(analysisResult.MarketDynamics, 80);
        foreach (var line in lines)
        {
            image.Mutate(ctx =>
            {
                ctx.DrawText(line, contentFont, ImageColor.Black, new PointF(40, y));
            });
            y += 20;
        }

        y += 10;
        
        // 添加交易建议
        image.Mutate(ctx =>
        {
            ctx.DrawText("💡 交易建议", font, ImageColor.Green, new PointF(20, y));
        });

        y += 30;
        var adviceLines = WrapText(analysisResult.TradingAdvice, 80);
        foreach (var line in adviceLines)
        {
            image.Mutate(ctx =>
            {
                ctx.DrawText(line, contentFont, ImageColor.Black, new PointF(40, y));
            });
            y += 20;
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 文本换行处理
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>换行后的文本行</returns>
    private static List<string> WrapText(string text, int maxLength)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 <= maxLength)
            {
                currentLine += (currentLine.Length > 0 ? " " : "") + word;
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    lines.Add(word);
                }
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    /// <summary>
    /// 获取时间段显示名称
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <returns>显示名称</returns>
    private static string GetTimeSlotDisplayName(Core.Enums.PublishTimeSlot timeSlot)
    {
        return timeSlot switch
        {
            Core.Enums.PublishTimeSlot.Morning => "上午场",
            Core.Enums.PublishTimeSlot.Afternoon => "下午场",
            Core.Enums.PublishTimeSlot.Evening => "晚间场",
            _ => "当前"
        };
    }

    /// <summary>
    /// 获取趋势显示名称
    /// </summary>
    /// <param name="trendType">趋势类型</param>
    /// <returns>显示名称</returns>
    private static string GetTrendDisplayName(Core.Enums.TrendType trendType)
    {
        return trendType switch
        {
            Core.Enums.TrendType.StrongUptrend => "强势上涨",
            Core.Enums.TrendType.ModerateUptrend => "温和上涨",
            Core.Enums.TrendType.Sideways => "横盘整理",
            Core.Enums.TrendType.ModerateDowntrend => "温和下跌",
            Core.Enums.TrendType.StrongDowntrend => "强势下跌",
            _ => "未知"
        };
    }
}