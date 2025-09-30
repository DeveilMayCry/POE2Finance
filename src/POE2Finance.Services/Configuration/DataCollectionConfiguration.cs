namespace POE2Finance.Services.Configuration;

/// <summary>
/// 数据采集配置
/// </summary>
public class DataCollectionConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "DataCollection";

    /// <summary>
    /// 采集间隔（小时）
    /// </summary>
    public int CollectionIntervalHours { get; set; } = 1;

    /// <summary>
    /// 最小请求间隔（秒）
    /// </summary>
    public int MinRequestIntervalSeconds { get; set; } = 3600; // 1小时

    /// <summary>
    /// 随机延迟范围（秒）
    /// </summary>
    public RandomDelayConfiguration RandomDelay { get; set; } = new();

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 重试延迟基数（秒）
    /// </summary>
    public int RetryDelayBaseSeconds { get; set; } = 5;

    /// <summary>
    /// User-Agent池
    /// </summary>
    public List<string> UserAgents { get; set; } = new()
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/120.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    /// <summary>
    /// 数据源配置
    /// </summary>
    public DataSourceConfigurations DataSources { get; set; } = new();
}

/// <summary>
/// 随机延迟配置
/// </summary>
public class RandomDelayConfiguration
{
    /// <summary>
    /// 最小延迟（秒）
    /// </summary>
    public int MinSeconds { get; set; } = 30;

    /// <summary>
    /// 最大延迟（秒）
    /// </summary>
    public int MaxSeconds { get; set; } = 180; // 3分钟
}

/// <summary>
/// 数据源配置集合
/// </summary>
public class DataSourceConfigurations
{
    /// <summary>
    /// 腾讯官方配置
    /// </summary>
    public TencentOfficialConfiguration TencentOfficial { get; set; } = new();

    /// <summary>
    /// DD373配置
    /// </summary>
    public DD373Configuration DD373 { get; set; } = new();

    /// <summary>
    /// 千岛电玩配置
    /// </summary>
    public QiandaoGamingConfiguration QiandaoGaming { get; set; } = new();

    /// <summary>
    /// WeGame配置
    /// </summary>
    public WeGameConfiguration WeGame { get; set; } = new();
}

/// <summary>
/// 腾讯官方数据源配置
/// </summary>
public class TencentOfficialConfiguration
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://poe2.qq.com";

    /// <summary>
    /// 交易API端点
    /// </summary>
    public string TradeApiEndpoint { get; set; } = "/api/trade";

    /// <summary>
    /// 请求头配置
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new()
    {
        { "Accept", "application/json, text/plain, */*" },
        { "Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8" },
        { "Referer", "https://poe2.qq.com/trade" },
        { "Origin", "https://poe2.qq.com" }
    };
}

/// <summary>
/// DD373数据源配置
/// </summary>
public class DD373Configuration
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 2;

    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://www.dd373.com";

    /// <summary>
    /// POE2区域页面
    /// </summary>
    public string Poe2Section { get; set; } = "/poe2";

    /// <summary>
    /// 请求头配置
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new()
    {
        { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" },
        { "Accept-Language", "zh-CN,zh;q=0.8,zh-TW;q=0.7,zh-HK;q=0.5,en-US;q=0.3,en;q=0.2" },
        { "Referer", "https://www.dd373.com" }
    };
}

/// <summary>
/// 千岛电玩数据源配置
/// </summary>
public class QiandaoGamingConfiguration
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://qiandao.com";

    /// <summary>
    /// POE2专区
    /// </summary>
    public string Poe2Section { get; set; } = "/poe2";
}

/// <summary>
/// WeGame数据源配置
/// </summary>
public class WeGameConfiguration
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 4;

    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://wegame.qq.com";

    /// <summary>
    /// POE2市场API
    /// </summary>
    public string MarketApiEndpoint { get; set; } = "/api/market/poe2";
}