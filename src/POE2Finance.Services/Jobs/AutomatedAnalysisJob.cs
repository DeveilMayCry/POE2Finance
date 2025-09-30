using Microsoft.Extensions.Logging;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;
using POE2Finance.Data.Repositories;
using POE2Finance.Services.Charts;
using POE2Finance.Services.Video;
using Quartz;

namespace POE2Finance.Services.Jobs;

/// <summary>
/// 自动化分析任务 - 执行完整的数据分析到视频发布流程
/// </summary>
[DisallowConcurrentExecution]
public class AutomatedAnalysisJob : IJob
{
    private readonly ILogger<AutomatedAnalysisJob> _logger;
    private readonly IDataCollectionService _dataCollectionService;
    private readonly IPriceAnalysisService _analysisService;
    private readonly IChartGenerationService _chartService;
    private readonly IContentGenerationService _contentService;
    private readonly IVideoCreationService _videoService;
    private readonly IPublishingService _publishingService;
    private readonly IAnalysisReportRepository _reportRepository;
    private readonly IVideoRecordRepository _videoRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AutomatedAnalysisJob(
        ILogger<AutomatedAnalysisJob> logger,
        IDataCollectionService dataCollectionService,
        IPriceAnalysisService analysisService,
        IChartGenerationService chartService,
        IContentGenerationService contentService,
        IVideoCreationService videoService,
        IPublishingService publishingService,
        IAnalysisReportRepository reportRepository,
        IVideoRecordRepository videoRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        _chartService = chartService ?? throw new ArgumentNullException(nameof(chartService));
        _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        _videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
        _publishingService = publishingService ?? throw new ArgumentNullException(nameof(publishingService));
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="context">任务执行上下文</param>
    public async Task Execute(IJobExecutionContext context)
    {
        var timeSlotStr = context.JobDetail.JobDataMap.GetString("TimeSlot") ?? "Morning";
        if (!Enum.TryParse<PublishTimeSlot>(timeSlotStr, out var timeSlot))
        {
            timeSlot = PublishTimeSlot.Morning;
        }

        _logger.LogInformation("开始执行自动化分析任务，时间段: {TimeSlot}", timeSlot);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        AnalysisReport? report = null;
        VideoRecord? videoRecord = null;

        try
        {
            // 1. 检查是否已经生成过今天的报告
            var today = DateTime.Today;
            var existingReport = await _reportRepository.GetReportByDateAndSlotAsync(today, timeSlot, context.CancellationToken);
            if (existingReport != null && existingReport.Status == ReportStatus.Published)
            {
                _logger.LogInformation("今日{TimeSlot}报告已存在且已发布，跳过执行", timeSlot);
                return;
            }

            // 2. 创建新的分析报告记录
            report = new AnalysisReport
            {
                ReportDate = today,
                TimeSlot = timeSlot,
                Status = ReportStatus.Generating,
                Title = "",
                Summary = "",
                DetailedAnalysis = "",
                HotItemsData = "",
                TrendData = ""
            };

            report = await _reportRepository.AddAsync(report, context.CancellationToken);
            _logger.LogInformation("创建分析报告记录，ID: {ReportId}", report.Id);

            // 3. 收集最新数据
            await CollectLatestDataAsync(context.CancellationToken);

            // 4. 执行价格分析
            var analysisResult = await _analysisService.GenerateMarketAnalysisAsync(timeSlot, context.CancellationToken);
            _logger.LogInformation("价格分析完成，热点物品数: {HotItemsCount}", analysisResult.HotItems.Count);

            // 5. 生成内容
            var title = _contentService.GenerateVideoTitle(analysisResult, timeSlot);
            var description = _contentService.GenerateVideoDescription(analysisResult, timeSlot);
            var tags = _contentService.GenerateVideoTags(analysisResult, timeSlot);
            var reportContent = await _contentService.GenerateReportContentAsync(analysisResult, context.CancellationToken);

            // 6. 更新报告信息
            report.Title = title;
            report.Summary = description.Length > 500 ? description.Substring(0, 500) : description;
            report.DetailedAnalysis = reportContent;
            report.HotItemsData = System.Text.Json.JsonSerializer.Serialize(analysisResult.HotItems);
            report.TrendData = System.Text.Json.JsonSerializer.Serialize(analysisResult);
            await _reportRepository.UpdateAsync(report, context.CancellationToken);

            // 7. 生成图表
            var chartPaths = await GenerateChartsAsync(analysisResult, context.CancellationToken);
            _logger.LogInformation("生成图表完成，图表数: {ChartCount}", chartPaths.Count);

            // 8. 创建视频记录
            videoRecord = new VideoRecord
            {
                AnalysisReportId = report.Id,
                Title = title,
                Description = description,
                Tags = string.Join(",", tags),
                TimeSlot = timeSlot,
                Status = VideoStatus.Creating,
                ScheduledPublishTime = GetScheduledPublishTime(timeSlot),
                DurationSeconds = 90, // 预估时长
                FileSizeBytes = 0
            };

            videoRecord = await _videoRepository.AddAsync(videoRecord, context.CancellationToken);
            _logger.LogInformation("创建视频记录，ID: {VideoId}", videoRecord.Id);

            // 9. 生成视频
            var videoPath = await CreateVideoAsync(analysisResult, chartPaths, title, timeSlot, context.CancellationToken);
            
            // 更新视频记录
            var videoFileInfo = new FileInfo(videoPath);
            videoRecord.LocalFilePath = videoPath;
            videoRecord.FileSizeBytes = videoFileInfo.Length;
            videoRecord.Status = VideoStatus.Created;
            await _videoRepository.UpdateAsync(videoRecord, context.CancellationToken);

            _logger.LogInformation("视频生成完成: {VideoPath}, 大小: {Size} MB", 
                videoPath, videoFileInfo.Length / (1024 * 1024));

            // 10. 发布到B站
            videoRecord.Status = VideoStatus.Uploading;
            await _videoRepository.UpdateAsync(videoRecord, context.CancellationToken);

            var publishResult = await _publishingService.PublishToBilibiliAsync(
                videoPath, title, description, tags, context.CancellationToken);

            if (publishResult.Success)
            {
                videoRecord.BilibiliBvId = publishResult.VideoId;
                videoRecord.BilibiliUrl = $"https://www.bilibili.com/video/{publishResult.VideoId}";
                videoRecord.Status = VideoStatus.Published;
                videoRecord.ActualPublishTime = DateTime.UtcNow;
                
                report.Status = ReportStatus.Published;
                
                _logger.LogInformation("视频发布成功，BV号: {BvId}", publishResult.VideoId);
            }
            else
            {
                videoRecord.Status = VideoStatus.Failed;
                videoRecord.ErrorMessage = publishResult.ErrorMessage;
                
                report.Status = ReportStatus.Failed;
                
                _logger.LogError("视频发布失败: {ErrorMessage}", publishResult.ErrorMessage);
            }

            await _videoRepository.UpdateAsync(videoRecord, context.CancellationToken);
            await _reportRepository.UpdateAsync(report, context.CancellationToken);

            // 11. 清理本地文件
            if (File.Exists(videoPath))
            {
                File.Delete(videoPath);
                _logger.LogInformation("已清理本地视频文件: {VideoPath}", videoPath);
            }

            foreach (var chartPath in chartPaths)
            {
                if (File.Exists(chartPath))
                {
                    File.Delete(chartPath);
                }
            }

            stopwatch.Stop();

            // 12. 更新执行时长
            if (report != null)
            {
                report.GenerationTimeMs = stopwatch.ElapsedMilliseconds;
                await _reportRepository.UpdateAsync(report, context.CancellationToken);
            }

            if (videoRecord != null)
            {
                videoRecord.CreationTimeMs = stopwatch.ElapsedMilliseconds;
                await _videoRepository.UpdateAsync(videoRecord, context.CancellationToken);
            }

            _logger.LogInformation("自动化分析任务完成，总耗时: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "自动化分析任务执行失败");

            // 更新失败状态
            if (report != null)
            {
                report.Status = ReportStatus.Failed;
                report.GenerationTimeMs = stopwatch.ElapsedMilliseconds;
                await _reportRepository.UpdateAsync(report, context.CancellationToken);
            }

            if (videoRecord != null)
            {
                videoRecord.Status = VideoStatus.Failed;
                videoRecord.ErrorMessage = ex.Message;
                videoRecord.CreationTimeMs = stopwatch.ElapsedMilliseconds;
                await _videoRepository.UpdateAsync(videoRecord, context.CancellationToken);
            }

            throw;
        }
    }

    /// <summary>
    /// 收集最新数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task CollectLatestDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始收集最新价格数据");

        // 从所有可用数据源收集数据
        var allPrices = await _dataCollectionService.CollectFromAllSourcesAsync(cancellationToken);
        
        _logger.LogInformation("数据收集完成，获得 {Count} 条价格数据", allPrices.Count);
    }

    /// <summary>
    /// 生成图表
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图表文件路径列表</returns>
    private async Task<List<string>> GenerateChartsAsync(MarketAnalysisResultDto analysisResult, CancellationToken cancellationToken)
    {
        var chartPaths = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), $"charts_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // 生成热点物品对比图
            if (analysisResult.HotItems.Count > 0)
            {
                var hotItemsChartPath = Path.Combine(tempDir, "hot_items_chart.png");
                await _chartService.GenerateHotItemsChartAsync(analysisResult.HotItems, hotItemsChartPath, cancellationToken);
                chartPaths.Add(hotItemsChartPath);
            }

            // 生成市场仪表盘
            var dashboardPath = Path.Combine(tempDir, "market_dashboard.png");
            if (_chartService is ChartGenerationService chartGenerationService)
            {
                await chartGenerationService.GenerateMarketDashboardAsync(analysisResult, dashboardPath, cancellationToken);
                chartPaths.Add(dashboardPath);
            }

            return chartPaths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成图表失败");
            
            // 清理已生成的图表文件
            foreach (var path in chartPaths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            
            throw;
        }
    }

    /// <summary>
    /// 创建视频
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="chartPaths">图表路径列表</param>
    /// <param name="title">视频标题</param>
    /// <param name="timeSlot">时间段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>视频文件路径</returns>
    private async Task<string> CreateVideoAsync(MarketAnalysisResultDto analysisResult, List<string> chartPaths, string title, PublishTimeSlot timeSlot, CancellationToken cancellationToken)
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"videos_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDir);

        var videoFileName = $"poe2_analysis_{DateTime.Now:yyyyMMdd_HHmm}_{timeSlot}.mp4";
        var videoPath = Path.Combine(outputDir, videoFileName);

        var videoConfig = new VideoGenerationConfigDto
        {
            Title = title,
            TimeSlot = timeSlot,
            DurationSeconds = 90,
            Width = 1920,
            Height = 1080,
            OutputPath = videoPath
        };

        return await _videoService.CreateVideoAsync(videoConfig, analysisResult, chartPaths, cancellationToken);
    }

    /// <summary>
    /// 获取预定发布时间
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <returns>预定发布时间</returns>
    private static DateTime GetScheduledPublishTime(PublishTimeSlot timeSlot)
    {
        var today = DateTime.Today;
        return timeSlot switch
        {
            PublishTimeSlot.Morning => today.AddHours(9),
            PublishTimeSlot.Afternoon => today.AddHours(15),
            PublishTimeSlot.Evening => today.AddHours(21),
            _ => DateTime.UtcNow
        };
    }
}