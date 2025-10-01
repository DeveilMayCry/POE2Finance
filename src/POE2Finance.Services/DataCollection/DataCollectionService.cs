using Microsoft.Extensions.Logging;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;
using POE2Finance.Services.DataCollection;

namespace POE2Finance.Services.DataCollection;

/// <summary>
/// 数据采集服务实现
/// </summary>
public class DataCollectionService : IDataCollectionService
{
    private readonly IEnumerable<IDataCollector> _collectors;
    private readonly ILogger<DataCollectionService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="collectors">数据采集器集合</param>
    /// <param name="logger">日志记录器</param>
    public DataCollectionService(IEnumerable<IDataCollector> collectors, ILogger<DataCollectionService> logger)
    {
        _collectors = collectors ?? throw new ArgumentNullException(nameof(collectors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PriceDataDto?> CollectPriceDataAsync(CurrencyType currencyType, DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始从 {DataSource} 采集 {CurrencyType} 价格数据", dataSource, currencyType);

        var collector = _collectors.FirstOrDefault(c => c.DataSource == dataSource && c.IsEnabled);
        if (collector == null)
        {
            _logger.LogWarning("未找到可用的 {DataSource} 数据采集器", dataSource);
            return null;
        }

        try
        {
            var result = await collector.CollectPriceAsync(currencyType, cancellationToken);
            if (result != null)
            {
                _logger.LogInformation("成功从 {DataSource} 采集到 {CurrencyType} 价格: {Price}", 
                    dataSource, currencyType, result.CurrentPriceInExalted);
            }
            else
            {
                _logger.LogWarning("从 {DataSource} 采集 {CurrencyType} 价格失败", dataSource, currencyType);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 {DataSource} 采集 {CurrencyType} 价格时发生异常", dataSource, currencyType);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PriceDataDto>> CollectAllPricesAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始从 {DataSource} 采集所有通货价格数据", dataSource);

        var collector = _collectors.FirstOrDefault(c => c.DataSource == dataSource && c.IsEnabled);
        if (collector == null)
        {
            _logger.LogWarning("未找到可用的 {DataSource} 数据采集器", dataSource);
            return new List<PriceDataDto>();
        }

        try
        {
            var results = await collector.CollectAllPricesAsync(cancellationToken);
            _logger.LogInformation("成功从 {DataSource} 采集到 {Count} 个通货价格", dataSource, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 {DataSource} 采集所有价格时发生异常", dataSource);
            return new List<PriceDataDto>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateDataSourceAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("验证数据源 {DataSource} 可用性", dataSource);

        var collector = _collectors.FirstOrDefault(c => c.DataSource == dataSource && c.IsEnabled);
        if (collector == null)
        {
            _logger.LogWarning("未找到 {DataSource} 数据采集器", dataSource);
            return false;
        }

        try
        {
            var isValid = await collector.ValidateAsync(cancellationToken);
            _logger.LogInformation("数据源 {DataSource} 验证结果: {IsValid}", dataSource, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证数据源 {DataSource} 时发生异常", dataSource);
            return false;
        }
    }

    /// <summary>
    /// 从多个数据源采集价格数据，按优先级顺序尝试
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据</returns>
    public async Task<PriceDataDto?> CollectPriceWithFallbackAsync(CurrencyType currencyType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始从多个数据源采集 {CurrencyType} 价格数据", currencyType);

        var sortedCollectors = _collectors
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.Priority)
            .ToList();

        foreach (var collector in sortedCollectors)
        {
            try
            {
                _logger.LogDebug("尝试从 {DataSource} 采集 {CurrencyType} 价格", collector.DataSource, currencyType);
                
                var result = await collector.CollectPriceAsync(currencyType, cancellationToken);
                if (result != null)
                {
                    _logger.LogInformation("成功从 {DataSource} 采集到 {CurrencyType} 价格: {Price}", 
                        collector.DataSource, currencyType, result.CurrentPriceInExalted);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从 {DataSource} 采集 {CurrencyType} 价格失败，尝试下一个数据源", 
                    collector.DataSource, currencyType);
            }
        }

        _logger.LogError("所有数据源均无法采集到 {CurrencyType} 价格数据", currencyType);
        return null;
    }

    /// <summary>
    /// 从所有可用数据源采集价格数据并合并
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据列表</returns>
    public async Task<List<PriceDataDto>> CollectFromAllSourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始从所有数据源采集价格数据");

        var allResults = new List<PriceDataDto>();
        var enabledCollectors = _collectors.Where(c => c.IsEnabled).ToList();

        var tasks = enabledCollectors.Select(async collector =>
        {
            try
            {
                _logger.LogDebug("从 {DataSource} 采集价格数据", collector.DataSource);
                var results = await collector.CollectAllPricesAsync(cancellationToken);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从 {DataSource} 采集价格数据失败", collector.DataSource);
                return new List<PriceDataDto>();
            }
        });

        var collectorResults = await Task.WhenAll(tasks);
        
        foreach (var results in collectorResults)
        {
            allResults.AddRange(results);
        }

        _logger.LogInformation("从所有数据源共采集到 {Count} 条价格数据", allResults.Count);
        return allResults;
    }

    /// <summary>
    /// 获取可用的数据源列表
    /// </summary>
    /// <returns>可用数据源列表</returns>
    public List<DataSource> GetAvailableDataSources()
    {
        return _collectors
            .Where(c => c.IsEnabled)
            .Select(c => c.DataSource)
            .OrderBy(ds => _collectors.First(c => c.DataSource == ds).Priority)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> CheckAllDataSourcesHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始检查所有数据源健康状态");

        var enabledCollectors = _collectors.Where(c => c.IsEnabled).ToList();
        if (!enabledCollectors.Any())
        {
            _logger.LogWarning("没有可用的数据采集器");
            return false;
        }

        var healthCheckTasks = enabledCollectors.Select(async collector =>
        {
            try
            {
                return await collector.ValidateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查数据源 {DataSource} 健康状态失败", collector.DataSource);
                return false;
            }
        });

        var results = await Task.WhenAll(healthCheckTasks);
        var healthyCount = results.Count(r => r);
        var isHealthy = healthyCount > 0;

        _logger.LogInformation("数据源健康检查完成，{HealthyCount}/{TotalCount} 个数据源正常", 
            healthyCount, enabledCollectors.Count);

        return isHealthy;
    }

}