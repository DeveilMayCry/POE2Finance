using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Models;
using POE2Finance.Services.Configuration;
using POE2Finance.Services.Infrastructure;
using System.Globalization;
using System.Text.RegularExpressions;

namespace POE2Finance.Services.DataCollection.Collectors;

/// <summary>
/// DD373数据采集器
/// </summary>
public class DD373Collector : BaseDataCollector
{
    private readonly ResilientHttpClient _httpClient;
    private readonly DD373Configuration _config;
    private readonly Regex _priceRegex = new(@"(\d+(?:\.\d+)?)", RegexOptions.Compiled);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">配置</param>
    public DD373Collector(
        ResilientHttpClient httpClient, 
        ILogger<DD373Collector> logger,
        IOptions<DataCollectionConfiguration> config) : base(logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config.Value?.DataSources?.DD373 ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public override DataSource DataSource => DataSource.DD373;

    /// <inheritdoc/>
    public override int Priority => _config.Priority;

    /// <inheritdoc/>
    public override bool IsEnabled => _config.Enabled;

    /// <inheritdoc/>
    protected override async Task<bool> PerformValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_config.BaseUrl}{_config.Poe2Section}";
            var response = await _httpClient.GetStringWithRetryAsync(url, _config.Headers, cancellationToken);
            return !string.IsNullOrEmpty(response) && (response.Contains("POE2") || response.Contains("流放之路"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DD373数据源验证失败");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<PriceDataDto?> PerformPriceCollectionAsync(CurrencyType currencyType, CancellationToken cancellationToken)
    {
        try
        {
            var currencyPath = GetDD373CurrencyPath(currencyType);
            if (string.IsNullOrEmpty(currencyPath))
            {
                _logger.LogWarning("不支持的通货类型: {CurrencyType}", currencyType);
                return null;
            }

            var url = $"{_config.BaseUrl}{_config.Poe2Section}/{currencyPath}";
            var html = await _httpClient.GetStringWithRetryAsync(url, _config.Headers, cancellationToken);

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("DD373返回空内容: {CurrencyType}", currencyType);
                return null;
            }

            return ParseDD373Html(currencyType, html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从DD373采集 {CurrencyType} 价格失败", currencyType);
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
            
            // 在请求之间添加延迟
            await Task.Delay(2000, cancellationToken);
        }

        return results;
    }

    /// <summary>
    /// 获取DD373通货路径
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <returns>通货路径</returns>
    private static string? GetDD373CurrencyPath(CurrencyType currencyType)
    {
        return currencyType switch
        {
            CurrencyType.ExaltedOrb => "exalted-orb",
            CurrencyType.DivineOrb => "divine-orb", 
            CurrencyType.ChaosOrb => "chaos-orb",
            _ => null
        };
    }

    /// <summary>
    /// 解析DD373 HTML内容
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="html">HTML内容</param>
    /// <returns>价格数据DTO</returns>
    private PriceDataDto? ParseDD373Html(CurrencyType currencyType, string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 尝试多种选择器来找到价格信息
            var priceNode = doc.DocumentNode.SelectSingleNode("//span[@class='price']") ??
                           doc.DocumentNode.SelectSingleNode("//div[@class='price-info']//span") ??
                           doc.DocumentNode.SelectSingleNode("//td[contains(@class,'price')]") ??
                           doc.DocumentNode.SelectSingleNode("//*[contains(text(),'¥') or contains(text(),'元')]");

            if (priceNode == null)
            {
                _logger.LogWarning("DD373未找到 {CurrencyType} 价格节点", currencyType);
                return null;
            }

            var priceText = priceNode.InnerText?.Trim() ?? string.Empty;
            var price = ExtractPriceFromText(priceText);
            
            if (price == null)
            {
                _logger.LogWarning("DD373无法解析 {CurrencyType} 价格: {PriceText}", currencyType, priceText);
                return null;
            }

            // DD373通常以人民币计价，需要转换为崇高石计价
            var priceInExalted = ConvertFromRmbToExalted(price.Value, currencyType);

            // 尝试获取交易量
            var volumeNode = doc.DocumentNode.SelectSingleNode("//span[@class='volume']") ??
                            doc.DocumentNode.SelectSingleNode("//*[contains(text(),'成交') or contains(text(),'交易')]");
            
            int? volume = null;
            if (volumeNode != null)
            {
                var volumeText = volumeNode.InnerText?.Trim() ?? string.Empty;
                var volumeMatch = _priceRegex.Match(volumeText);
                if (volumeMatch.Success && int.TryParse(volumeMatch.Value, out var vol))
                {
                    volume = vol;
                }
            }

            return CreatePriceDataDto(
                currencyType,
                priceInExalted,
                price.Value,
                "RMB",
                volume
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析DD373 {CurrencyType} HTML失败", currencyType);
            return null;
        }
    }

    /// <summary>
    /// 从文本中提取价格
    /// </summary>
    /// <param name="text">包含价格的文本</param>
    /// <returns>价格值</returns>
    private decimal? ExtractPriceFromText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var match = _priceRegex.Match(text);
        if (match.Success && decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return price;
        }

        return null;
    }

    /// <summary>
    /// 将人民币价格转换为崇高石计价
    /// </summary>
    /// <param name="rmbPrice">人民币价格</param>
    /// <param name="currencyType">通货类型</param>
    /// <returns>崇高石计价</returns>
    private decimal ConvertFromRmbToExalted(decimal rmbPrice, CurrencyType currencyType)
    {
        // 这里需要一个固定的或者动态的汇率转换
        // 假设1崇高石 = 100人民币（这个需要根据实际情况调整）
        const decimal exaltedToRmbRate = 100m;

        return currencyType switch
        {
            CurrencyType.ExaltedOrb => 1.0m, // 崇高石本身
            CurrencyType.DivineOrb => rmbPrice / exaltedToRmbRate,
            CurrencyType.ChaosOrb => rmbPrice / exaltedToRmbRate,
            _ => rmbPrice / exaltedToRmbRate
        };
    }
}