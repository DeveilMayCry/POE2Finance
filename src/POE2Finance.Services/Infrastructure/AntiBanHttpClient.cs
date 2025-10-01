using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Services.Configuration;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace POE2Finance.Services.Infrastructure;

/// <summary>
/// 防Ban HTTP客户端，实现请求频率控制和防反爬机制
/// </summary>
public class AntiBanHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AntiBanHttpClient> _logger;
    private readonly DataCollectionConfiguration _config;
    private readonly Random _random = new();
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly object _rateLimitLock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">配置</param>
    public AntiBanHttpClient(HttpClient httpClient, ILogger<AntiBanHttpClient> logger, IOptions<DataCollectionConfiguration> config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);

        ConfigureHttpClient();
    }

    /// <summary>
    /// 配置HTTP客户端
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
        
        // 设置默认请求头
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("DNT", "1");
    }

    /// <summary>
    /// 发送GET请求
    /// </summary>
    /// <param name="url">请求URL</param>
    /// <param name="headers">额外请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP响应消息</returns>
    public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync(cancellationToken);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // 设置随机User-Agent
        SetRandomUserAgent(request);
        
        // 添加额外请求头
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        _logger.LogDebug("发送GET请求到: {Url}", url);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        _logger.LogDebug("收到响应: {StatusCode} from {Url}", response.StatusCode, url);
        
        return response;
    }

    /// <summary>
    /// 发送GET请求并返回字符串内容
    /// </summary>
    /// <param name="url">请求URL</param>
    /// <param name="headers">额外请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应内容字符串</returns>
    public async Task<string> GetStringAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(url, headers, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// 发送GET请求并返回JSON对象
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="url">请求URL</param>
    /// <param name="headers">额外请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化的对象</returns>
    public async Task<T?> GetJsonAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(url, headers, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<T>(content);
    }

    /// <summary>
    /// 确保请求频率限制
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimitSemaphore.WaitAsync(cancellationToken);
        try
        {
            lock (_rateLimitLock)
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                var minInterval = TimeSpan.FromSeconds(_config.MinRequestIntervalSeconds);
                
                if (timeSinceLastRequest < minInterval)
                {
                    var delay = minInterval - timeSinceLastRequest;
                    _logger.LogDebug("等待速率限制: {DelayMs}ms", delay.TotalMilliseconds);
                    Thread.Sleep(delay);
                }
                
                _lastRequestTime = DateTime.UtcNow;
            }

            // 添加随机延迟
            await AddRandomDelayAsync(cancellationToken);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// 添加随机延迟
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task AddRandomDelayAsync(CancellationToken cancellationToken)
    {
        var delaySeconds = _random.Next(_config.RandomDelay.MinSeconds, _config.RandomDelay.MaxSeconds + 1);
        var delay = TimeSpan.FromSeconds(delaySeconds);
        
        _logger.LogDebug("随机延迟: {DelaySeconds}秒", delaySeconds);
        await Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// 设置随机User-Agent
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    private void SetRandomUserAgent(HttpRequestMessage request)
    {
        if (_config.UserAgents.Count > 0)
        {
            var userAgent = _config.UserAgents[_random.Next(_config.UserAgents.Count)];
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            _logger.LogDebug("使用User-Agent: {UserAgent}", userAgent);
        }
    }
}

/// <summary>
/// 带重试策略的HTTP客户端包装器
/// </summary>
public class ResilientHttpClient
{
    private readonly AntiBanHttpClient _client;
    private readonly ILogger<ResilientHttpClient> _logger;
    private readonly DataCollectionConfiguration _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="client">防Ban HTTP客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">配置</param>
    public ResilientHttpClient(AntiBanHttpClient client, ILogger<ResilientHttpClient> logger, IOptions<DataCollectionConfiguration> config)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// 带重试的GET请求
    /// </summary>
    /// <param name="url">请求URL</param>
    /// <param name="headers">额外请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应内容字符串</returns>
    public async Task<string?> GetStringWithRetryAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var retryPolicy = CreateRetryPolicy();

        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                return await _client.GetStringAsync(url, headers, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求失败，已达最大重试次数: {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// 带重试的JSON请求
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="url">请求URL</param>
    /// <param name="headers">额外请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化的对象</returns>
    public async Task<T?> GetJsonWithRetryAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var retryPolicy = CreateRetryPolicy();

        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                return await _client.GetJsonAsync<T>(url, headers, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON请求失败，已达最大重试次数: {Url}", url);
            return default;
        }
    }

    /// <summary>
    /// 创建重试策略
    /// </summary>
    /// <returns>重试策略</returns>
    private IAsyncPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(_config.RetryDelayBaseSeconds * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("重试请求 #{RetryCount}，等待 {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
    }
}