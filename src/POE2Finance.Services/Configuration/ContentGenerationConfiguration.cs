namespace POE2Finance.Services.Configuration;

/// <summary>
/// 内容生成配置
/// </summary>
public class ContentGenerationConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ContentGeneration";

    /// <summary>
    /// 是否启用AI内容生成
    /// </summary>
    public bool EnableAiGeneration { get; set; } = true;

    /// <summary>
    /// 内容模板配置
    /// </summary>
    public ContentTemplateConfiguration Templates { get; set; } = new();

    /// <summary>
    /// 标题生成配置
    /// </summary>
    public TitleGenerationConfiguration TitleGeneration { get; set; } = new();

    /// <summary>
    /// 描述生成配置
    /// </summary>
    public DescriptionGenerationConfiguration DescriptionGeneration { get; set; } = new();

    /// <summary>
    /// 标签生成配置
    /// </summary>
    public TagGenerationConfiguration TagGeneration { get; set; } = new();
}

/// <summary>
/// 内容模板配置
/// </summary>
public class ContentTemplateConfiguration
{
    /// <summary>
    /// 开场白模板
    /// </summary>
    public List<string> OpeningTemplates { get; set; } = new()
    {
        "{greeting}欢迎收看POE2国服{timeSlot}市场分析。现在是{time}，我将为大家带来最新的通货价格分析和交易建议。",
        "{greeting}这里是POE2国服{timeSlot}市场快报。当前时间{time}，让我们一起来看看今天的市场表现。"
    };

    /// <summary>
    /// 结束语模板
    /// </summary>
    public List<string> ClosingTemplates { get; set; } = new()
    {
        "以上就是本期{timeSlot}市场分析的全部内容。下一次更新将在{nextUpdate}，请大家持续关注。投资有风险，请根据自身情况合理配置。感谢收看！",
        "好的，{timeSlot}市场分析就到这里。{nextUpdate}我们再见，记得关注我们获取最新的行情分析。谢谢大家！"
    };

    /// <summary>
    /// 热点物品描述模板
    /// </summary>
    public List<string> HotItemTemplates { get; set; } = new()
    {
        "{itemName}表现{intensity}，热度评分{score}，价格波动幅度{volatility}%，呈现{trend}态势。{advice}",
        "{itemName}当前{intensity}，热度达到{score}分，波动幅度{volatility}%，展现出{trend}的特征。{advice}"
    };

    /// <summary>
    /// 趋势分析模板
    /// </summary>
    public List<string> TrendAnalysisTemplates { get; set; } = new()
    {
        "从整体市场来看，当前呈现{trend}格局。在监控的主要通货中，有{activeCount}种表现较为活跃。{implication}",
        "市场整体表现为{trend}状态。{activeCount}种主要通货显示出明显的价格波动。{implication}"
    };
}

/// <summary>
/// 标题生成配置
/// </summary>
public class TitleGenerationConfiguration
{
    /// <summary>
    /// 最大标题长度
    /// </summary>
    public int MaxLength { get; set; } = 80;

    /// <summary>
    /// 标题模板
    /// </summary>
    public List<string> Templates { get; set; } = new()
    {
        "【POE2国服】{timeSlot}市场速报 {date} | {topItem}{change} {trendIcon} | 热度{score}",
        "【POE2国服】{date} {timeSlot}分析 | {topItem}{change} {trendIcon} 热度爆表",
        "【POE2国服】{timeSlot}行情 {date} | {topItem}价格{change} {trendIcon}"
    };

    /// <summary>
    /// 平稳市场标题模板
    /// </summary>
    public List<string> StableMarketTemplates { get; set; } = new()
    {
        "【POE2国服】{timeSlot}市场速报 {date} | 市场平稳观望中",
        "【POE2国服】{date} {timeSlot}分析 | 整体平稳，耐心等待",
        "【POE2国服】{timeSlot}行情 {date} | 横盘整理，静待方向"
    };
}

/// <summary>
/// 描述生成配置
/// </summary>
public class DescriptionGenerationConfiguration
{
    /// <summary>
    /// 最大描述长度
    /// </summary>
    public int MaxLength { get; set; } = 2000;

    /// <summary>
    /// 是否包含emoji
    /// </summary>
    public bool IncludeEmoji { get; set; } = true;

    /// <summary>
    /// 是否包含标签
    /// </summary>
    public bool IncludeHashtags { get; set; } = true;

    /// <summary>
    /// 固定结尾文本
    /// </summary>
    public List<string> StandardEndings { get; set; } = new()
    {
        "⚠️ 投资有风险，请根据自身情况合理配置",
        "📺 每日三次更新：上午09:00 | 下午15:00 | 晚间21:00",
        "🔔 记得点赞关注，不错过每日行情分析！"
    };
}

/// <summary>
/// 标签生成配置
/// </summary>
public class TagGenerationConfiguration
{
    /// <summary>
    /// 最大标签数量
    /// </summary>
    public int MaxTags { get; set; } = 10;

    /// <summary>
    /// 核心标签
    /// </summary>
    public List<string> CoreTags { get; set; } = new()
    {
        "POE2",
        "流放之路2",
        "POE2国服",
        "通货分析",
        "价格分析",
        "市场行情"
    };

    /// <summary>
    /// 通用标签
    /// </summary>
    public List<string> GeneralTags { get; set; } = new()
    {
        "游戏经济",
        "投资理财",
        "数据分析",
        "腾讯游戏",
        "在线游戏",
        "策略分析"
    };

    /// <summary>
    /// 时间段标签映射
    /// </summary>
    public Dictionary<string, string> TimeSlotTags { get; set; } = new()
    {
        { "Morning", "上午分析" },
        { "Afternoon", "下午分析" },
        { "Evening", "晚间分析" }
    };
}

/// <summary>
/// Edge-TTS配置
/// </summary>
public class EdgeTtsConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "EdgeTts";

    /// <summary>
    /// Edge-TTS命令路径
    /// </summary>
    public string EdgeTtsCommand { get; set; } = "edge-tts";

    /// <summary>
    /// 语音名称
    /// </summary>
    public string VoiceName { get; set; } = "zh-CN-XiaoxiaoNeural";

    /// <summary>
    /// 语速设置（如：+20%、-10%、slow、fast）
    /// </summary>
    public string Rate { get; set; } = "+10%";

    /// <summary>
    /// 音量设置（如：+20%、-10%、loud、quiet）
    /// </summary>
    public string Volume { get; set; } = "+0%";

    /// <summary>
    /// 音调设置（如：+10Hz、-5Hz、high、low）
    /// </summary>
    public string Pitch { get; set; } = "+0Hz";

    /// <summary>
    /// 输出音频格式
    /// </summary>
    public string OutputFormat { get; set; } = "audio-16khz-32kbitrate-mono-mp3";

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 语音合成选项
    /// </summary>
    public VoiceOptions VoiceOptions { get; set; } = new();
}

/// <summary>
/// 语音选项配置
/// </summary>
public class VoiceOptions
{
    /// <summary>
    /// 可用的中文语音列表
    /// </summary>
    public List<string> ChineseVoices { get; set; } = new()
    {
        "zh-CN-XiaoxiaoNeural",  // 女声，自然
        "zh-CN-YunyangNeural",   // 男声，专业
        "zh-CN-XiaohanNeural",   // 女声，温和
        "zh-CN-YunjianNeural",   // 男声，稳重
        "zh-CN-XiaomengNeural",  // 女声，活泼
        "zh-CN-XiaoruiNeural"    // 女声，清晰
    };

    /// <summary>
    /// 根据时间段选择的语音
    /// </summary>
    public Dictionary<string, string> TimeSlotVoices { get; set; } = new()
    {
        { "Morning", "zh-CN-XiaohanNeural" },    // 上午：温和女声
        { "Afternoon", "zh-CN-YunyangNeural" },  // 下午：专业男声
        { "Evening", "zh-CN-XiaoxiaoNeural" }    // 晚间：自然女声
    };

    /// <summary>
    /// 语音情感设置
    /// </summary>
    public Dictionary<string, EmotionSettings> EmotionMappings { get; set; } = new()
    {
        { "Uptrend", new EmotionSettings { Rate = "+15%", Pitch = "+2Hz" } },
        { "Downtrend", new EmotionSettings { Rate = "-5%", Pitch = "-2Hz" } },
        { "Neutral", new EmotionSettings { Rate = "+0%", Pitch = "+0Hz" } }
    };
}

/// <summary>
/// 情感设置
/// </summary>
public class EmotionSettings
{
    /// <summary>
    /// 语速
    /// </summary>
    public string Rate { get; set; } = "+0%";

    /// <summary>
    /// 音调
    /// </summary>
    public string Pitch { get; set; } = "+0Hz";

    /// <summary>
    /// 音量
    /// </summary>
    public string Volume { get; set; } = "+0%";
}