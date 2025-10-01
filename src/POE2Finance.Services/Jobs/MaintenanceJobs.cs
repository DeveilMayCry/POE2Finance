using Microsoft.Extensions.Logging;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Interfaces;
using POE2Finance.Data.Repositories;
using Quartz;

namespace POE2Finance.Services.Jobs;

/// <summary>
/// 数据收集任务 - 定期收集价格数据
/// </summary>
[DisallowConcurrentExecution]
public class DataCollectionJob : IJob
{
    private readonly ILogger<DataCollectionJob> _logger;
    private readonly IDataCollectionService _dataCollectionService;
    private readonly ICurrencyPriceRepository _priceRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataCollectionJob(
        ILogger<DataCollectionJob> logger,
        IDataCollectionService dataCollectionService,
        ICurrencyPriceRepository priceRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
        _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
    }

    /// <summary>
    /// 执行数据收集任务
    /// </summary>
    /// <param name="context">任务执行上下文</param>
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("开始执行数据收集任务");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var totalCollected = 0;

        try
        {
            // 检查数据源健康状态
            var isHealthy = await _dataCollectionService.CheckAllDataSourcesHealthAsync(context.CancellationToken);
            
            _logger.LogInformation("数据源健康检查完成，状态: {IsHealthy}", isHealthy);

            if (!isHealthy)
            {
                _logger.LogWarning("没有可用的数据源，跳过数据收集");
                return;
            }

            // 收集各种通货的价格数据
            var currencies = new[] { CurrencyType.ExaltedOrb, CurrencyType.DivineOrb, CurrencyType.ChaosOrb };
            var collectedPrices = new List<CurrencyPrice>();

            foreach (var currency in currencies)
            {
                try
                {
                    var priceData = await _dataCollectionService.CollectPriceWithFallbackAsync(currency, context.CancellationToken);
                    
                    if (priceData != null)
                    {
                        var priceEntity = new CurrencyPrice
                        {
                            CurrencyType = priceData.CurrencyType,
                            PriceInExalted = priceData.CurrentPriceInExalted,
                            OriginalPrice = priceData.CurrentPriceInExalted, // 假设已转换
                            OriginalPriceUnit = "E",
                            TradeVolume = priceData.TradeVolume,
                            DataSource = priceData.DataSource,
                            CollectedAt = priceData.CollectedAt,
                            IsValid = true
                        };

                        collectedPrices.Add(priceEntity);
                        totalCollected++;

                        _logger.LogInformation("成功收集 {Currency} 价格数据: {Price}E from {DataSource}", 
                            currency, priceData.CurrentPriceInExalted, priceData.DataSource);
                    }
                    else
                    {
                        _logger.LogWarning("无法收集 {Currency} 价格数据", currency);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "收集 {Currency} 价格数据失败", currency);
                }

                // 在请求之间添加延迟，避免过于频繁
                await Task.Delay(2000, context.CancellationToken);
            }

            // 批量保存价格数据
            if (collectedPrices.Count > 0)
            {
                await _priceRepository.BulkInsertPricesAsync(collectedPrices, context.CancellationToken);
                _logger.LogInformation("成功保存 {Count} 条价格数据到数据库", collectedPrices.Count);
            }

            stopwatch.Stop();
            _logger.LogInformation("数据收集任务完成，收集 {TotalCollected} 条数据，耗时: {ElapsedTime}ms", 
                totalCollected, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "数据收集任务执行失败，耗时: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// 清理任务 - 定期清理过期数据和临时文件
/// </summary>
[DisallowConcurrentExecution]
public class CleanupJob : IJob
{
    private readonly ILogger<CleanupJob> _logger;
    private readonly ICurrencyPriceRepository _priceRepository;
    private readonly IVideoRecordRepository _videoRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CleanupJob(
        ILogger<CleanupJob> logger,
        ICurrencyPriceRepository priceRepository,
        IVideoRecordRepository videoRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
        _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));
    }

    /// <summary>
    /// 执行清理任务
    /// </summary>
    /// <param name="context">任务执行上下文</param>
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("开始执行清理任务");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 1. 清理过期价格数据（保留7天）
            var priceDataCutoff = DateTime.UtcNow.AddDays(-7);
            await _priceRepository.DeleteExpiredPricesAsync(priceDataCutoff, context.CancellationToken);
            _logger.LogInformation("清理 {Date} 之前的价格数据", priceDataCutoff.ToString("yyyy-MM-dd"));

            // 2. 清理已发布的本地视频文件（发布后立即清理）
            var videosForCleanup = await _videoRepository.GetVideosForCleanupAsync(
                DateTime.UtcNow.AddHours(-1), context.CancellationToken);

            var cleanedVideos = 0;
            foreach (var video in videosForCleanup)
            {
                if (!string.IsNullOrEmpty(video.LocalFilePath) && File.Exists(video.LocalFilePath))
                {
                    try
                    {
                        File.Delete(video.LocalFilePath);
                        
                        // 更新数据库记录
                        video.LocalFilePath = null;
                        await _videoRepository.UpdateAsync(video, context.CancellationToken);
                        
                        cleanedVideos++;
                        _logger.LogDebug("已清理视频文件: {FilePath}", video.LocalFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "清理视频文件失败: {FilePath}", video.LocalFilePath);
                    }
                }
            }

            _logger.LogInformation("清理了 {Count} 个本地视频文件", cleanedVideos);

            // 3. 清理临时目录
            await CleanTempDirectoriesAsync();

            // 4. 清理系统临时文件
            await CleanSystemTempFilesAsync();

            stopwatch.Stop();
            _logger.LogInformation("清理任务完成，耗时: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "清理任务执行失败，耗时: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// 清理临时目录
    /// </summary>
    private Task CleanTempDirectoriesAsync()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var tempDirs = Directory.GetDirectories(tempPath, "poe2*")
                .Concat(Directory.GetDirectories(tempPath, "charts_*"))
                .Concat(Directory.GetDirectories(tempPath, "videos_*"))
                .Where(dir => Directory.GetCreationTime(dir) < DateTime.Now.AddHours(-2));

            var cleanedDirs = 0;
            foreach (var dir in tempDirs)
            {
                try
                {
                    Directory.Delete(dir, true);
                    cleanedDirs++;
                    _logger.LogDebug("已清理临时目录: {Directory}", dir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清理临时目录失败: {Directory}", dir);
                }
            }

            _logger.LogInformation("清理了 {Count} 个临时目录", cleanedDirs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理临时目录失败");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 清理系统临时文件
    /// </summary>
    private Task CleanSystemTempFilesAsync()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var tempFiles = Directory.GetFiles(tempPath, "tmp*")
                .Concat(Directory.GetFiles(tempPath, "*.tmp"))
                .Where(file => File.GetCreationTime(file) < DateTime.Now.AddHours(-6));

            var cleanedFiles = 0;
            foreach (var file in tempFiles)
            {
                try
                {
                    File.Delete(file);
                    cleanedFiles++;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "清理临时文件失败: {File}", file);
                }
            }

            if (cleanedFiles > 0)
            {
                _logger.LogInformation("清理了 {Count} 个临时文件", cleanedFiles);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理系统临时文件失败");
        }
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// 健康检查任务 - 监控系统运行状态
/// </summary>
[DisallowConcurrentExecution]
public class HealthCheckJob : IJob
{
    private readonly ILogger<HealthCheckJob> _logger;
    private readonly IDataCollectionService _dataCollectionService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public HealthCheckJob(
        ILogger<HealthCheckJob> logger,
        IDataCollectionService dataCollectionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <param name="context">任务执行上下文</param>
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("开始执行健康检查任务");

        try
        {
            // 检查数据源健康状态
            var isHealthy = await _dataCollectionService.CheckAllDataSourcesHealthAsync(context.CancellationToken);

            _logger.LogInformation("数据源健康状态: {IsHealthy}", isHealthy);

            // 如果数据源不健康，记录警告
            if (!isHealthy)
            {
                _logger.LogWarning("数据源健康状态异常，没有可用的数据源");
            }

            // 检查磁盘空间
            CheckDiskSpace();

            // 检查内存使用情况
            CheckMemoryUsage();

            _logger.LogInformation("健康检查任务完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查任务执行失败");
            throw;
        }
    }

    /// <summary>
    /// 检查磁盘空间
    /// </summary>
    private void CheckDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory)!);
            var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            var totalSpaceGB = drive.TotalSize / (1024 * 1024 * 1024);

            _logger.LogInformation("磁盘空间: {FreeSpace}GB / {TotalSpace}GB 可用", freeSpaceGB, totalSpaceGB);

            if (freeSpaceGB < 5) // 少于5GB时警告
            {
                _logger.LogWarning("磁盘空间不足，仅剩 {FreeSpace}GB", freeSpaceGB);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查磁盘空间失败");
        }
    }

    /// <summary>
    /// 检查内存使用情况
    /// </summary>
    private void CheckMemoryUsage()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024 * 1024);

            _logger.LogInformation("内存使用: {MemoryUsage}MB", memoryMB);

            if (memoryMB > 1024) // 超过1GB时警告
            {
                _logger.LogWarning("内存使用过高: {MemoryUsage}MB", memoryMB);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查内存使用失败");
        }
    }
}