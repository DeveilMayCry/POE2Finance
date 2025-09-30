using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;
using POE2Finance.Services.Configuration;
using POE2Finance.Services.AI;
using POE2Finance.Services.Charts;
using FFMpegCore;
using FFMpegCore.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

namespace POE2Finance.Services.Video;

/// <summary>
/// 视频制作服务实现
/// </summary>
public class VideoCreationService : IVideoCreationService
{
    private readonly ILogger<VideoCreationService> _logger;
    private readonly VideoConfiguration _config;
    private readonly EdgeTtsService _ttsService;
    private readonly ChartGenerationService _chartService;
    private readonly IContentGenerationService _contentService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">视频配置</param>
    /// <param name="ttsService">TTS服务</param>
    /// <param name="chartService">图表服务</param>
    /// <param name="contentService">内容服务</param>
    public VideoCreationService(
        ILogger<VideoCreationService> logger,
        IOptions<VideoConfiguration> config,
        EdgeTtsService ttsService,
        ChartGenerationService chartService,
        IContentGenerationService contentService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
        _chartService = chartService ?? throw new ArgumentNullException(nameof(chartService));
        _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
    }

    /// <inheritdoc/>
    public async Task<string> CreateVideoAsync(VideoGenerationConfigDto config, MarketAnalysisResultDto analysisResult, List<string> chartPaths, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始创建视频，时间段: {TimeSlot}", config.TimeSlot);

        try
        {
            // 确保输出目录存在
            var directory = Path.GetDirectoryName(config.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 生成报告内容
            var reportContent = await _contentService.GenerateReportContentAsync(analysisResult, cancellationToken);

            // 创建临时文件夹
            var tempDir = Path.Combine(Path.GetTempPath(), $"poe2video_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. 生成语音音频
                var audioPath = Path.Combine(tempDir, "narration.mp3");
                await _ttsService.GenerateAudioAsync(reportContent, audioPath, cancellationToken);

                // 2. 创建视频帧序列
                var framesPaths = await CreateVideoFramesAsync(analysisResult, chartPaths, tempDir, cancellationToken);

                // 3. 合成最终视频
                var finalVideoPath = await ComposeVideoAsync(framesPaths, audioPath, config.OutputPath, cancellationToken);

                _logger.LogInformation("视频创建完成: {OutputPath}", finalVideoPath);
                return finalVideoPath;
            }
            finally
            {
                // 清理临时文件
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建视频失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAudioAsync(string text, string outputPath, CancellationToken cancellationToken = default)
    {
        return await _ttsService.GenerateAudioAsync(text, outputPath, cancellationToken);
    }

    /// <summary>
    /// 创建视频帧序列
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="chartPaths">图表路径列表</param>
    /// <param name="tempDir">临时目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>帧文件路径列表</returns>
    private async Task<List<string>> CreateVideoFramesAsync(MarketAnalysisResultDto analysisResult, List<string> chartPaths, string tempDir, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始创建视频帧序列");

        var framePaths = new List<string>();
        var frameIndex = 0;

        // 1. 创建开场帧（5秒）
        for (int i = 0; i < _config.FrameRate * 5; i++)
        {
            var framePath = await CreateOpeningFrameAsync(analysisResult, tempDir, frameIndex++);
            framePaths.Add(framePath);
        }

        // 2. 创建图表展示帧（每个图表10秒）
        foreach (var chartPath in chartPaths)
        {
            for (int i = 0; i < _config.FrameRate * 10; i++)
            {
                var framePath = await CreateChartFrameAsync(chartPath, analysisResult, tempDir, frameIndex++);
                framePaths.Add(framePath);
            }
        }

        // 3. 创建热点物品分析帧（15秒）
        for (int i = 0; i < _config.FrameRate * 15; i++)
        {
            var framePath = await CreateHotItemsFrameAsync(analysisResult.HotItems, tempDir, frameIndex++);
            framePaths.Add(framePath);
        }

        // 4. 创建市场总结帧（8秒）
        for (int i = 0; i < _config.FrameRate * 8; i++)
        {
            var framePath = await CreateSummaryFrameAsync(analysisResult, tempDir, frameIndex++);
            framePaths.Add(framePath);
        }

        // 5. 创建结束帧（2秒）
        for (int i = 0; i < _config.FrameRate * 2; i++)
        {
            var framePath = await CreateEndingFrameAsync(analysisResult, tempDir, frameIndex++);
            framePaths.Add(framePath);
        }

        _logger.LogInformation("视频帧序列创建完成，总帧数: {FrameCount}", framePaths.Count);
        return framePaths;
    }

    /// <summary>
    /// 创建开场帧
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="tempDir">临时目录</param>
    /// <param name="frameIndex">帧索引</param>
    /// <returns>帧文件路径</returns>
    private async Task<string> CreateOpeningFrameAsync(MarketAnalysisResultDto analysisResult, string tempDir, int frameIndex)
    {
        var framePath = Path.Combine(tempDir, $"frame_{frameIndex:D6}.png");

        using var image = new Image<Rgba32>(_config.Width, _config.Height);
        
        // 设置背景渐变
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgb(20, 30, 50)); // 深蓝色背景
        });

        // 添加标题
        var titleFont = SystemFonts.CreateFont("Arial", 48, FontStyle.Bold);
        var timeSlotName = GetTimeSlotDisplayName(analysisResult.TimeSlot);
        var title = $"POE2国服{timeSlotName}市场分析";

        image.Mutate(ctx =>
        {
            var titleSize = TextMeasurer.MeasureSize(title, new TextOptions(titleFont));
            var titleX = (_config.Width - titleSize.Width) / 2;
            var titleY = _config.Height / 3;
            
            ctx.DrawText(title, titleFont, Color.White, new PointF(titleX, titleY));
        });

        // 添加日期时间
        var dateFont = SystemFonts.CreateFont("Arial", 24);
        var dateText = analysisResult.AnalysisTime.ToString("yyyy年MM月dd日 HH:mm");

        image.Mutate(ctx =>
        {
            var dateSize = TextMeasurer.MeasureSize(dateText, new TextOptions(dateFont));
            var dateX = (_config.Width - dateSize.Width) / 2;
            var dateY = _config.Height / 2;
            
            ctx.DrawText(dateText, dateFont, Color.LightGray, new PointF(dateX, dateY));
        });

        // 添加装饰元素
        await AddDecorationElementsAsync(image);

        await image.SaveAsPngAsync(framePath);
        return framePath;
    }

    /// <summary>
    /// 创建图表展示帧
    /// </summary>
    /// <param name="chartPath">图表路径</param>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="tempDir">临时目录</param>
    /// <param name="frameIndex">帧索引</param>
    /// <returns>帧文件路径</returns>
    private async Task<string> CreateChartFrameAsync(string chartPath, MarketAnalysisResultDto analysisResult, string tempDir, int frameIndex)
    {
        var framePath = Path.Combine(tempDir, $"frame_{frameIndex:D6}.png");

        using var backgroundImage = new Image<Rgba32>(_config.Width, _config.Height);
        
        // 设置背景
        backgroundImage.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgb(30, 35, 45));
        });

        // 加载并缩放图表
        if (File.Exists(chartPath))
        {
            using var chartImage = await Image.LoadAsync<Rgba32>(chartPath);
            
            // 计算缩放比例以适应画面
            var maxWidth = _config.Width - 100;
            var maxHeight = _config.Height - 200;
            
            var scaleX = (float)maxWidth / chartImage.Width;
            var scaleY = (float)maxHeight / chartImage.Height;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(chartImage.Width * scale);
            var newHeight = (int)(chartImage.Height * scale);

            chartImage.Mutate(ctx => ctx.Resize(newWidth, newHeight));

            // 计算居中位置
            var chartX = (_config.Width - newWidth) / 2;
            var chartY = (_config.Height - newHeight) / 2;

            backgroundImage.Mutate(ctx =>
            {
                ctx.DrawImage(chartImage, new Point(chartX, chartY), 1.0f);
            });
        }

        // 添加标题
        var titleFont = SystemFonts.CreateFont("Arial", 32, FontStyle.Bold);
        var title = "实时价格趋势分析";

        backgroundImage.Mutate(ctx =>
        {
            ctx.DrawText(title, titleFont, Color.White, new PointF(50, 30));
        });

        await backgroundImage.SaveAsPngAsync(framePath);
        return framePath;
    }

    /// <summary>
    /// 创建热点物品帧
    /// </summary>
    /// <param name="hotItems">热点物品列表</param>
    /// <param name="tempDir">临时目录</param>
    /// <param name="frameIndex">帧索引</param>
    /// <returns>帧文件路径</returns>
    private async Task<string> CreateHotItemsFrameAsync(List<HotItemAnalysisDto> hotItems, string tempDir, int frameIndex)
    {
        var framePath = Path.Combine(tempDir, $"frame_{frameIndex:D6}.png");

        using var image = new Image<Rgba32>(_config.Width, _config.Height);
        
        // 设置背景
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgb(25, 30, 40));
        });

        // 添加标题
        var titleFont = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);
        image.Mutate(ctx =>
        {
            ctx.DrawText("🔥 热点通货排行", titleFont, Color.Orange, new PointF(50, 50));
        });

        // 添加热点物品信息
        var itemFont = SystemFonts.CreateFont("Arial", 24);
        var y = 150;

        for (int i = 0; i < Math.Min(3, hotItems.Count); i++)
        {
            var item = hotItems[i];
            var rankColor = i == 0 ? Color.Gold : i == 1 ? Color.Silver : Color.FromRgb(205, 127, 50);
            
            var rankText = $"#{i + 1}";
            var itemText = $"{item.CurrencyName} - 热度: {item.HotScore:F1} | 波动: {item.PriceVolatility:F2}%";
            var trendText = GetTrendDisplayName(item.TrendType);

            image.Mutate(ctx =>
            {
                ctx.DrawText(rankText, titleFont, rankColor, new PointF(50, y));
                ctx.DrawText(itemText, itemFont, Color.White, new PointF(150, y + 5));
                ctx.DrawText(trendText, itemFont, GetTrendColor(item.TrendType), new PointF(150, y + 35));
            });

            y += 100;
        }

        await image.SaveAsPngAsync(framePath);
        return framePath;
    }

    /// <summary>
    /// 创建市场总结帧
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="tempDir">临时目录</param>
    /// <param name="frameIndex">帧索引</param>
    /// <returns>帧文件路径</returns>
    private async Task<string> CreateSummaryFrameAsync(MarketAnalysisResultDto analysisResult, string tempDir, int frameIndex)
    {
        var framePath = Path.Combine(tempDir, $"frame_{frameIndex:D6}.png");

        using var image = new Image<Rgba32>(_config.Width, _config.Height);
        
        // 设置背景
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgb(30, 40, 50));
        });

        // 添加标题
        var titleFont = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);
        image.Mutate(ctx =>
        {
            ctx.DrawText("📈 市场总结", titleFont, Color.LightBlue, new PointF(50, 50));
        });

        // 添加整体趋势
        var contentFont = SystemFonts.CreateFont("Arial", 20);
        var trendText = $"整体趋势: {GetTrendDisplayName(analysisResult.OverallTrend)}";
        
        image.Mutate(ctx =>
        {
            ctx.DrawText(trendText, contentFont, Color.White, new PointF(50, 150));
        });

        // 添加核心建议（换行处理）
        var adviceLines = WrapText(analysisResult.TradingAdvice, 60);
        var y = 220;
        
        image.Mutate(ctx =>
        {
            ctx.DrawText("💡 交易建议:", contentFont, Color.Yellow, new PointF(50, y));
        });

        y += 40;
        foreach (var line in adviceLines.Take(4))
        {
            image.Mutate(ctx =>
            {
                ctx.DrawText(line, contentFont, Color.White, new PointF(70, y));
            });
            y += 30;
        }

        await image.SaveAsPngAsync(framePath);
        return framePath;
    }

    /// <summary>
    /// 创建结束帧
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="tempDir">临时目录</param>
    /// <param name="frameIndex">帧索引</param>
    /// <returns>帧文件路径</returns>
    private async Task<string> CreateEndingFrameAsync(MarketAnalysisResultDto analysisResult, string tempDir, int frameIndex)
    {
        var framePath = Path.Combine(tempDir, $"frame_{frameIndex:D6}.png");

        using var image = new Image<Rgba32>(_config.Width, _config.Height);
        
        // 设置背景
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgb(20, 25, 35));
        });

        // 添加感谢文字
        var titleFont = SystemFonts.CreateFont("Arial", 42, FontStyle.Bold);
        var thanksText = "感谢观看";

        image.Mutate(ctx =>
        {
            var textSize = TextMeasurer.MeasureSize(thanksText, new TextOptions(titleFont));
            var x = (_config.Width - textSize.Width) / 2;
            var y = _config.Height / 3;
            
            ctx.DrawText(thanksText, titleFont, Color.White, new PointF(x, y));
        });

        // 添加订阅提醒
        var subtitleFont = SystemFonts.CreateFont("Arial", 24);
        var subscribeText = "记得点赞关注，获取每日行情分析";

        image.Mutate(ctx =>
        {
            var textSize = TextMeasurer.MeasureSize(subscribeText, new TextOptions(subtitleFont));
            var x = (_config.Width - textSize.Width) / 2;
            var y = _config.Height / 2;
            
            ctx.DrawText(subscribeText, subtitleFont, Color.LightGray, new PointF(x, y));
        });

        // 添加下次更新时间
        var nextUpdateText = GetNextUpdateText(analysisResult.TimeSlot);
        image.Mutate(ctx =>
        {
            var textSize = TextMeasurer.MeasureSize(nextUpdateText, new TextOptions(subtitleFont));
            var x = (_config.Width - textSize.Width) / 2;
            var y = _config.Height * 2 / 3;
            
            ctx.DrawText(nextUpdateText, subtitleFont, Color.Yellow, new PointF(x, y));
        });

        await image.SaveAsPngAsync(framePath);
        return framePath;
    }

    /// <summary>
    /// 合成最终视频
    /// </summary>
    /// <param name="framePaths">帧文件路径列表</param>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>输出视频路径</returns>
    private async Task<string> ComposeVideoAsync(List<string> framePaths, string audioPath, string outputPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始合成视频，帧数: {FrameCount}", framePaths.Count);

        try
        {
            // 创建帧序列视频
            var tempVideoPath = Path.Combine(Path.GetTempPath(), $"temp_video_{Guid.NewGuid():N}.mp4");

            // 使用FFmpeg将图片序列转换为视频
            await FFMpegArguments
                .FromFileInput(Path.Combine(Path.GetDirectoryName(framePaths[0])!, "frame_%06d.png"), options => options
                    .WithFramerate(_config.FrameRate))
                .OutputToFile(tempVideoPath, true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithConstantRateFactor(23)
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(_config.Width, _config.Height))
                    .WithFramerate(_config.FrameRate))
                .ProcessAsynchronously();

            // 如果有音频，则合并音频和视频
            if (File.Exists(audioPath))
            {
                await FFMpegArguments
                    .FromFileInput(tempVideoPath)
                    .AddFileInput(audioPath)
                    .OutputToFile(outputPath, true, options => options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithConstantRateFactor(23)
                        .WithAudioBitrate(128)
                        .WithVariableBitrate(4)
                        .WithFastStart())
                    .ProcessAsynchronously();

                // 删除临时视频文件
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                }
            }
            else
            {
                // 没有音频，直接移动视频文件
                File.Move(tempVideoPath, outputPath, true);
            }

            _logger.LogInformation("视频合成完成: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频合成失败");
            throw;
        }
    }

    /// <summary>
    /// 添加装饰元素
    /// </summary>
    /// <param name="image">图片对象</param>
    private async Task AddDecorationElementsAsync(Image<Rgba32> image)
    {
        // 添加简单的装饰线条
        image.Mutate(ctx =>
        {
            var pen = new Pen(Color.Gold, 3);
            
            // 顶部装饰线
            ctx.DrawLine(pen, new PointF(100, 100), new PointF(_config.Width - 100, 100));
            
            // 底部装饰线
            ctx.DrawLine(pen, new PointF(100, _config.Height - 100), new PointF(_config.Width - 100, _config.Height - 100));
        });
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
    /// 获取趋势颜色
    /// </summary>
    /// <param name="trendType">趋势类型</param>
    /// <returns>颜色</returns>
    private static Color GetTrendColor(Core.Enums.TrendType trendType)
    {
        return trendType switch
        {
            Core.Enums.TrendType.StrongUptrend => Color.LimeGreen,
            Core.Enums.TrendType.ModerateUptrend => Color.LightGreen,
            Core.Enums.TrendType.Sideways => Color.Yellow,
            Core.Enums.TrendType.ModerateDowntrend => Color.Orange,
            Core.Enums.TrendType.StrongDowntrend => Color.Red,
            _ => Color.Gray
        };
    }

    /// <summary>
    /// 获取下次更新文本
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <returns>更新文本</returns>
    private static string GetNextUpdateText(Core.Enums.PublishTimeSlot timeSlot)
    {
        return timeSlot switch
        {
            Core.Enums.PublishTimeSlot.Morning => "下次更新：下午15:00",
            Core.Enums.PublishTimeSlot.Afternoon => "下次更新：晚间21:00",
            Core.Enums.PublishTimeSlot.Evening => "下次更新：明天上午09:00",
            _ => "敬请期待下次更新"
        };
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
            _ => "未知趋势"
        };
    }
}