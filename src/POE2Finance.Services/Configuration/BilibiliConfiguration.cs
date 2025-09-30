namespace POE2Finance.Services.Configuration;

/// <summary>
/// B站发布配置
/// </summary>
public class BilibiliConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Bilibili";

    /// <summary>
    /// 是否启用B站发布
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// B站API基础URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://member.bilibili.com/x";

    /// <summary>
    /// 会话Cookie（包含SESSDATA等认证信息）
    /// </summary>
    public string SessionCookie { get; set; } = "";

    /// <summary>
    /// CSRF令牌
    /// </summary>
    public string CsrfToken { get; set; } = "";

    /// <summary>
    /// 分区ID（游戏区为4）
    /// </summary>
    public int CategoryId { get; set; } = 4;

    /// <summary>
    /// 分片上传大小（字节）
    /// </summary>
    public int ChunkSize { get; set; } = 4 * 1024 * 1024; // 4MB

    /// <summary>
    /// 上传超时时间（秒）
    /// </summary>
    public int UploadTimeoutSeconds { get; set; } = 3600; // 1小时

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 视频质量配置
    /// </summary>
    public VideoQualityConfiguration VideoQuality { get; set; } = new();

    /// <summary>
    /// 发布策略配置
    /// </summary>
    public PublishingStrategyConfiguration PublishingStrategy { get; set; } = new();

    /// <summary>
    /// 内容审核配置
    /// </summary>
    public ContentModerationConfiguration ContentModeration { get; set; } = new();

    /// <summary>
    /// 封面图配置
    /// </summary>
    public CoverImageConfiguration CoverImage { get; set; } = new();
}

/// <summary>
/// 视频质量配置
/// </summary>
public class VideoQualityConfiguration
{
    /// <summary>
    /// 最大文件大小（字节）
    /// </summary>
    public long MaxFileSize { get; set; } = 8L * 1024 * 1024 * 1024; // 8GB

    /// <summary>
    /// 支持的视频格式
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new()
    {
        "mp4", "avi", "wmv", "mov", "flv", "f4v", "mkv", "webm"
    };

    /// <summary>
    /// 推荐分辨率
    /// </summary>
    public List<string> RecommendedResolutions { get; set; } = new()
    {
        "1920x1080", "1280x720", "720x480"
    };

    /// <summary>
    /// 推荐帧率
    /// </summary>
    public List<double> RecommendedFrameRates { get; set; } = new()
    {
        30.0, 25.0, 24.0, 60.0
    };

    /// <summary>
    /// 推荐比特率（kbps）
    /// </summary>
    public BitrateRecommendations BitrateRecommendations { get; set; } = new();
}

/// <summary>
/// 比特率推荐配置
/// </summary>
public class BitrateRecommendations
{
    /// <summary>
    /// 1080p推荐比特率
    /// </summary>
    public int Video1080p { get; set; } = 6000;

    /// <summary>
    /// 720p推荐比特率
    /// </summary>
    public int Video720p { get; set; } = 3000;

    /// <summary>
    /// 480p推荐比特率
    /// </summary>
    public int Video480p { get; set; } = 1500;

    /// <summary>
    /// 音频推荐比特率
    /// </summary>
    public int Audio { get; set; } = 128;
}

/// <summary>
/// 发布策略配置
/// </summary>
public class PublishingStrategyConfiguration
{
    /// <summary>
    /// 是否自动发布
    /// </summary>
    public bool AutoPublish { get; set; } = true;

    /// <summary>
    /// 发布延迟（分钟）
    /// </summary>
    public int PublishDelayMinutes { get; set; } = 0;

    /// <summary>
    /// 是否定时发布
    /// </summary>
    public bool EnableScheduledPublish { get; set; } = false;

    /// <summary>
    /// 定时发布时间配置
    /// </summary>
    public ScheduledPublishConfiguration ScheduledPublish { get; set; } = new();

    /// <summary>
    /// 发布失败重试策略
    /// </summary>
    public RetryStrategyConfiguration RetryStrategy { get; set; } = new();
}

/// <summary>
/// 定时发布配置
/// </summary>
public class ScheduledPublishConfiguration
{
    /// <summary>
    /// 上午场发布时间
    /// </summary>
    public TimeOnly MorningPublishTime { get; set; } = new(9, 0);

    /// <summary>
    /// 下午场发布时间
    /// </summary>
    public TimeOnly AfternoonPublishTime { get; set; } = new(15, 0);

    /// <summary>
    /// 晚间场发布时间
    /// </summary>
    public TimeOnly EveningPublishTime { get; set; } = new(21, 0);

    /// <summary>
    /// 是否启用工作日发布
    /// </summary>
    public bool EnableWeekdayPublish { get; set; } = true;

    /// <summary>
    /// 是否启用周末发布
    /// </summary>
    public bool EnableWeekendPublish { get; set; } = true;
}

/// <summary>
/// 重试策略配置
/// </summary>
public class RetryStrategyConfiguration
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 重试间隔（分钟）
    /// </summary>
    public int RetryIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// 是否使用指数退避
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// 失败通知配置
    /// </summary>
    public FailureNotificationConfiguration FailureNotification { get; set; } = new();
}

/// <summary>
/// 失败通知配置
/// </summary>
public class FailureNotificationConfiguration
{
    /// <summary>
    /// 是否启用失败通知
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 通知方式
    /// </summary>
    public List<NotificationMethod> Methods { get; set; } = new()
    {
        NotificationMethod.Log,
        NotificationMethod.Email
    };

    /// <summary>
    /// 邮件通知配置
    /// </summary>
    public EmailNotificationConfiguration Email { get; set; } = new();
}

/// <summary>
/// 邮件通知配置
/// </summary>
public class EmailNotificationConfiguration
{
    /// <summary>
    /// 收件人邮箱列表
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// 邮件主题模板
    /// </summary>
    public string SubjectTemplate { get; set; } = "POE2Finance - B站发布失败通知";

    /// <summary>
    /// 邮件内容模板
    /// </summary>
    public string BodyTemplate { get; set; } = "视频发布失败：{title}\n错误信息：{error}\n时间：{timestamp}";
}

/// <summary>
/// 内容审核配置
/// </summary>
public class ContentModerationConfiguration
{
    /// <summary>
    /// 是否启用自动审核
    /// </summary>
    public bool EnableAutoModeration { get; set; } = true;

    /// <summary>
    /// 标题长度限制
    /// </summary>
    public int MaxTitleLength { get; set; } = 80;

    /// <summary>
    /// 描述长度限制
    /// </summary>
    public int MaxDescriptionLength { get; set; } = 2000;

    /// <summary>
    /// 标签数量限制
    /// </summary>
    public int MaxTagCount { get; set; } = 10;

    /// <summary>
    /// 禁用词汇列表
    /// </summary>
    public List<string> ProhibitedWords { get; set; } = new();

    /// <summary>
    /// 敏感词汇检查
    /// </summary>
    public SensitiveWordConfiguration SensitiveWords { get; set; } = new();
}

/// <summary>
/// 敏感词汇配置
/// </summary>
public class SensitiveWordConfiguration
{
    /// <summary>
    /// 是否启用敏感词检查
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 敏感词汇列表
    /// </summary>
    public List<string> Words { get; set; } = new();

    /// <summary>
    /// 替换策略
    /// </summary>
    public SensitiveWordReplacement ReplacementStrategy { get; set; } = SensitiveWordReplacement.Remove;

    /// <summary>
    /// 替换字符
    /// </summary>
    public string ReplacementChar { get; set; } = "*";
}

/// <summary>
/// 封面图配置
/// </summary>
public class CoverImageConfiguration
{
    /// <summary>
    /// 是否自动生成封面
    /// </summary>
    public bool AutoGenerate { get; set; } = true;

    /// <summary>
    /// 封面图模板路径
    /// </summary>
    public string TemplatePath { get; set; } = "";

    /// <summary>
    /// 封面图尺寸
    /// </summary>
    public CoverImageSize Size { get; set; } = new();

    /// <summary>
    /// 封面图质量
    /// </summary>
    public int Quality { get; set; } = 95;

    /// <summary>
    /// 封面图格式
    /// </summary>
    public string Format { get; set; } = "jpg";
}

/// <summary>
/// 封面图尺寸配置
/// </summary>
public class CoverImageSize
{
    /// <summary>
    /// 宽度
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// 高度
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// 宽高比
    /// </summary>
    public double AspectRatio => (double)Width / Height;
}

/// <summary>
/// 通知方式枚举
/// </summary>
public enum NotificationMethod
{
    /// <summary>
    /// 日志记录
    /// </summary>
    Log = 1,

    /// <summary>
    /// 邮件通知
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信通知
    /// </summary>
    Sms = 3,

    /// <summary>
    /// 微信通知
    /// </summary>
    WeChat = 4,

    /// <summary>
    /// 钉钉通知
    /// </summary>
    DingTalk = 5
}

/// <summary>
/// 敏感词替换策略
/// </summary>
public enum SensitiveWordReplacement
{
    /// <summary>
    /// 移除敏感词
    /// </summary>
    Remove = 1,

    /// <summary>
    /// 替换为指定字符
    /// </summary>
    Replace = 2,

    /// <summary>
    /// 拒绝发布
    /// </summary>
    Reject = 3
}