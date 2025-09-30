using Microsoft.Extensions.Logging;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Models;

namespace POE2Finance.Services.DataCollection;

/// <summary>
/// 数据采集器接口
/// </summary>
public interface IDataCollector
{
    /// <summary>
    /// 数据源类型
    /// </summary>
    DataSource DataSource { get; }

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 是否可用
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 验证数据源是否可用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 采集指定通货的价格数据
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据</returns>
    Task<PriceDataDto?> CollectPriceAsync(CurrencyType currencyType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 采集所有通货的价格数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据列表</returns>
    Task<List<PriceDataDto>> CollectAllPricesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 数据采集器基类
/// </summary>
public abstract class BaseDataCollector : IDataCollector
{
    protected readonly ILogger _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    protected BaseDataCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public abstract DataSource DataSource { get; }

    /// <inheritdoc/>
    public abstract int Priority { get; }

    /// <inheritdoc/>
    public abstract bool IsEnabled { get; }

    /// <inheritdoc/>
    public virtual async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("验证数据源 {DataSource} 可用性", DataSource);
            return await PerformValidationAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证数据源 {DataSource} 失败", DataSource);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<PriceDataDto?> CollectPriceAsync(CurrencyType currencyType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("从 {DataSource} 采集 {CurrencyType} 价格数据", DataSource, currencyType);
            var result = await PerformPriceCollectionAsync(currencyType, cancellationToken);
            
            if (result != null)
            {
                _logger.LogInformation("成功从 {DataSource} 采集到 {CurrencyType} 价格: {Price}", 
                    DataSource, currencyType, result.CurrentPriceInExalted);
            }
            else
            {
                _logger.LogWarning("从 {DataSource} 采集 {CurrencyType} 价格失败", DataSource, currencyType);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 {DataSource} 采集 {CurrencyType} 价格时发生异常", DataSource, currencyType);
            return null;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<PriceDataDto>> CollectAllPricesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("从 {DataSource} 采集所有通货价格数据", DataSource);
            var results = await PerformAllPricesCollectionAsync(cancellationToken);
            
            _logger.LogInformation("成功从 {DataSource} 采集到 {Count} 个通货价格", DataSource, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 {DataSource} 采集所有价格时发生异常", DataSource);
            return new List<PriceDataDto>();
        }
    }

    /// <summary>
    /// 执行验证逻辑
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    protected abstract Task<bool> PerformValidationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 执行单个通货价格采集
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据</returns>
    protected abstract Task<PriceDataDto?> PerformPriceCollectionAsync(CurrencyType currencyType, CancellationToken cancellationToken);

    /// <summary>
    /// 执行所有通货价格采集
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据列表</returns>
    protected abstract Task<List<PriceDataDto>> PerformAllPricesCollectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 创建价格数据DTO
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="priceInExalted">价格（以崇高石计算）</param>
    /// <param name="originalPrice">原始价格</param>
    /// <param name="originalUnit">原始计价单位</param>
    /// <param name="tradeVolume">交易量</param>
    /// <returns>价格数据DTO</returns>
    protected PriceDataDto CreatePriceDataDto(
        CurrencyType currencyType,
        decimal priceInExalted,
        decimal originalPrice,
        string originalUnit,
        int? tradeVolume = null)
    {
        return new PriceDataDto
        {
            CurrencyType = currencyType,
            CurrencyName = GetCurrencyDisplayName(currencyType),
            CurrentPriceInExalted = priceInExalted,
            TradeVolume = tradeVolume,
            DataSource = DataSource,
            CollectedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 获取通货显示名称
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <returns>显示名称</returns>
    protected static string GetCurrencyDisplayName(CurrencyType currencyType)
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
    /// 转换价格为崇高石计价
    /// </summary>
    /// <param name="originalPrice">原始价格</param>
    /// <param name="originalCurrency">原始通货类型</param>
    /// <param name="exaltedPrice">崇高石当前价格（如果原始通货不是崇高石）</param>
    /// <returns>以崇高石计价的价格</returns>
    protected static decimal ConvertToExaltedPrice(decimal originalPrice, CurrencyType originalCurrency, decimal exaltedPrice = 1.0m)
    {
        return originalCurrency switch
        {
            CurrencyType.ExaltedOrb => 1.0m, // 崇高石作为基准
            CurrencyType.DivineOrb => originalPrice / exaltedPrice, // 神圣石相对于崇高石的价格
            CurrencyType.ChaosOrb => originalPrice / exaltedPrice, // 混沌石相对于崇高石的价格
            _ => originalPrice / exaltedPrice
        };
    }
}