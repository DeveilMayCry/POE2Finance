using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Models;
using POE2Finance.Services.Configuration;
using POE2Finance.Services.Infrastructure;
using System.Text.Json;

namespace POE2Finance.Services.DataCollection.Collectors;

/// <summary>
/// 腾讯官方POE2数据采集器
/// </summary>
public class TencentOfficialCollector : BaseDataCollector
{
    private readonly ResilientHttpClient _httpClient;
    private readonly TencentOfficialConfiguration _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">配置</param>
    public TencentOfficialCollector(
        ResilientHttpClient httpClient, 
        ILogger<TencentOfficialCollector> logger,
        IOptions<DataCollectionConfiguration> config) : base(logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config.Value?.DataSources?.TencentOfficial ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public override DataSource DataSource => DataSource.TencentOfficial;

    /// <inheritdoc/>
    public override int Priority => _config.Priority;

    /// <inheritdoc/>
    public override bool IsEnabled => _config.Enabled;

    /// <inheritdoc/>
    protected override async Task<bool> PerformValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_config.BaseUrl}/api/health";
            var response = await _httpClient.GetStringWithRetryAsync(url, _config.Headers, cancellationToken);
            return !string.IsNullOrEmpty(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "腾讯官方数据源验证失败");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<PriceDataDto?> PerformPriceCollectionAsync(CurrencyType currencyType, CancellationToken cancellationToken)
    {
        try
        {
            var itemId = GetTencentItemId(currencyType);
            if (string.IsNullOrEmpty(itemId))
            {
                _logger.LogWarning("不支持的通货类型: {CurrencyType}", currencyType);
                return null;
            }

            var url = $"{_config.BaseUrl}{_config.TradeApiEndpoint}?item={itemId}";
            var response = await _httpClient.GetJsonWithRetryAsync<TencentPriceResponse>(url, _config.Headers, cancellationToken);

            if (response?.Data == null)
            {
                _logger.LogWarning("腾讯官方API返回空数据: {CurrencyType}", currencyType);
                return null;
            }

            return ParseTencentResponse(currencyType, response.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从腾讯官方采集 {CurrencyType} 价格失败", currencyType);
            return null;
        }
    }

    /// <inheritdoc/>
    protected override async Task<List<PriceDataDto>> PerformAllPricesCollectionAsync(CancellationToken cancellationToken)
    {
        var results = new List<PriceDataDto>();
        var currencies = new[] { CurrencyType.ExaltedOrb, CurrencyType.DivineOrb, CurrencyType.ChaosOrb };

        foreach (var currency in currencies)
        {
            var priceData = await PerformPriceCollectionAsync(currency, cancellationToken);
            if (priceData != null)
            {
                results.Add(priceData);
            }
            
            // 在请求之间添加小延迟
            await Task.Delay(1000, cancellationToken);
        }

        return results;
    }

    /// <summary>
    /// 获取腾讯官方物品ID
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <returns>物品ID</returns>
    private static string? GetTencentItemId(CurrencyType currencyType)
    {
        return currencyType switch
        {
            CurrencyType.ExaltedOrb => "exalted_orb",
            CurrencyType.DivineOrb => "divine_orb",
            CurrencyType.ChaosOrb => "chaos_orb",
            _ => null
        };
    }

    /// <summary>
    /// 解析腾讯响应数据
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="data">响应数据</param>
    /// <returns>价格数据DTO</returns>
    private PriceDataDto? ParseTencentResponse(CurrencyType currencyType, TencentPriceData data)
    {
        try
        {
            // 腾讯官方数据已经是以崇高石计价
            var priceInExalted = currencyType switch
            {
                CurrencyType.ExaltedOrb => 1.0m, // 崇高石本身
                CurrencyType.DivineOrb => data.Price,
                CurrencyType.ChaosOrb => data.Price,
                _ => data.Price
            };

            return CreatePriceDataDto(
                currencyType,
                priceInExalted,
                data.Price,
                "E", // 腾讯官方以崇高石为计价单位
                data.Volume
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析腾讯官方 {CurrencyType} 响应数据失败", currencyType);
            return null;
        }
    }
}

/// <summary>
/// 腾讯价格响应模型
/// </summary>
public class TencentPriceResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public TencentPriceData? Data { get; set; }
}

/// <summary>
/// 腾讯价格数据模型
/// </summary>
public class TencentPriceData
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PriceUnit { get; set; } = string.Empty;
    public int Volume { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsActive { get; set; }
}