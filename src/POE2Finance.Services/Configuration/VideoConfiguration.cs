namespace POE2Finance.Services.Configuration;

/// <summary>
/// 视频制作配置
/// </summary>
public class VideoConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Video";

    /// <summary>
    /// 视频宽度
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// 视频高度
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// 帧率
    /// </summary>
    public double FrameRate { get; set; } = 30.0;

    /// <summary>
    /// 视频比特率（kbps）
    /// </summary>
    public int VideoBitrate { get; set; } = 5000;

    /// <summary>
    /// 音频比特率（kbps）
    /// </summary>
    public int AudioBitrate { get; set; } = 128;

    /// <summary>
    /// 视频编码器
    /// </summary>
    public string VideoCodec { get; set; } = "libx264";

    /// <summary>
    /// 音频编码器
    /// </summary>
    public string AudioCodec { get; set; } = "aac";

    /// <summary>
    /// CRF质量参数（0-51，越小质量越高）
    /// </summary>
    public int CrfValue { get; set; } = 23;

    /// <summary>
    /// 预设编码速度
    /// </summary>
    public string Preset { get; set; } = "medium";

    /// <summary>
    /// 段落时长配置
    /// </summary>
    public SegmentDurationConfiguration SegmentDurations { get; set; } = new();

    /// <summary>
    /// 视觉效果配置
    /// </summary>
    public VisualEffectsConfiguration VisualEffects { get; set; } = new();

    /// <summary>
    /// 字体配置
    /// </summary>
    public VideoFontConfiguration Fonts { get; set; } = new();

    /// <summary>
    /// 背景配置
    /// </summary>
    public BackgroundConfiguration Background { get; set; } = new();

    /// <summary>
    /// 转场效果配置
    /// </summary>
    public TransitionConfiguration Transitions { get; set; } = new();

    /// <summary>
    /// 临时文件配置
    /// </summary>
    public TempFileConfiguration TempFiles { get; set; } = new();
}

/// <summary>
/// 段落时长配置
/// </summary>
public class SegmentDurationConfiguration
{
    /// <summary>
    /// 开场动画时长（秒）
    /// </summary>
    public int OpeningDuration { get; set; } = 5;

    /// <summary>
    /// 每个图表展示时长（秒）
    /// </summary>
    public int ChartDisplayDuration { get; set; } = 10;

    /// <summary>
    /// 热点物品分析时长（秒）
    /// </summary>
    public int HotItemsAnalysisDuration { get; set; } = 15;

    /// <summary>
    /// 市场总结时长（秒）
    /// </summary>
    public int MarketSummaryDuration { get; set; } = 8;

    /// <summary>
    /// 结束动画时长（秒）
    /// </summary>
    public int EndingDuration { get; set; } = 2;

    /// <summary>
    /// 最小视频总时长（秒）
    /// </summary>
    public int MinTotalDuration { get; set; } = 60;

    /// <summary>
    /// 最大视频总时长（秒）
    /// </summary>
    public int MaxTotalDuration { get; set; } = 180;
}

/// <summary>
/// 视觉效果配置
/// </summary>
public class VisualEffectsConfiguration
{
    /// <summary>
    /// 是否启用渐变动画
    /// </summary>
    public bool EnableFadeAnimations { get; set; } = true;

    /// <summary>
    /// 是否启用缩放动画
    /// </summary>
    public bool EnableScaleAnimations { get; set; } = true;

    /// <summary>
    /// 是否启用移动动画
    /// </summary>
    public bool EnableMoveAnimations { get; set; } = false;

    /// <summary>
    /// 动画持续时间（毫秒）
    /// </summary>
    public int AnimationDurationMs { get; set; } = 500;

    /// <summary>
    /// 图表缩放比例
    /// </summary>
    public float ChartScaleFactor { get; set; } = 0.9f;

    /// <summary>
    /// 文字阴影效果
    /// </summary>
    public bool EnableTextShadow { get; set; } = true;

    /// <summary>
    /// 阴影偏移量
    /// </summary>
    public int ShadowOffset { get; set; } = 2;

    /// <summary>
    /// 阴影颜色（十六进制）
    /// </summary>
    public string ShadowColor { get; set; } = "#000000";

    /// <summary>
    /// 阴影透明度（0-1）
    /// </summary>
    public float ShadowOpacity { get; set; } = 0.5f;
}

/// <summary>
/// 视频字体配置
/// </summary>
public class VideoFontConfiguration
{
    /// <summary>
    /// 主标题字体大小
    /// </summary>
    public int MainTitleFontSize { get; set; } = 48;

    /// <summary>
    /// 副标题字体大小
    /// </summary>
    public int SubTitleFontSize { get; set; } = 32;

    /// <summary>
    /// 正文字体大小
    /// </summary>
    public int ContentFontSize { get; set; } = 24;

    /// <summary>
    /// 小字体大小
    /// </summary>
    public int SmallFontSize { get; set; } = 16;

    /// <summary>
    /// 主要字体系列
    /// </summary>
    public string PrimaryFontFamily { get; set; } = "Microsoft YaHei";

    /// <summary>
    /// 备用字体系列
    /// </summary>
    public string FallbackFontFamily { get; set; } = "Arial";

    /// <summary>
    /// 是否使用粗体标题
    /// </summary>
    public bool BoldTitles { get; set; } = true;

    /// <summary>
    /// 字体颜色配置
    /// </summary>
    public FontColorConfiguration Colors { get; set; } = new();
}

/// <summary>
/// 字体颜色配置
/// </summary>
public class FontColorConfiguration
{
    /// <summary>
    /// 主标题颜色（十六进制）
    /// </summary>
    public string MainTitle { get; set; } = "#FFFFFF";

    /// <summary>
    /// 副标题颜色（十六进制）
    /// </summary>
    public string SubTitle { get; set; } = "#CCCCCC";

    /// <summary>
    /// 正文颜色（十六进制）
    /// </summary>
    public string Content { get; set; } = "#FFFFFF";

    /// <summary>
    /// 强调文字颜色（十六进制）
    /// </summary>
    public string Highlight { get; set; } = "#FFD700";

    /// <summary>
    /// 警告文字颜色（十六进制）
    /// </summary>
    public string Warning { get; set; } = "#FF6B47";

    /// <summary>
    /// 成功文字颜色（十六进制）
    /// </summary>
    public string Success { get; set; } = "#4CAF50";
}

/// <summary>
/// 背景配置
/// </summary>
public class BackgroundConfiguration
{
    /// <summary>
    /// 背景类型
    /// </summary>
    public BackgroundType Type { get; set; } = BackgroundType.Gradient;

    /// <summary>
    /// 纯色背景颜色（十六进制）
    /// </summary>
    public string SolidColor { get; set; } = "#1A1A2E";

    /// <summary>
    /// 渐变背景配置
    /// </summary>
    public GradientConfiguration Gradient { get; set; } = new();

    /// <summary>
    /// 背景图片路径
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// 背景图片透明度（0-1）
    /// </summary>
    public float ImageOpacity { get; set; } = 0.3f;

    /// <summary>
    /// 是否启用动态背景
    /// </summary>
    public bool EnableDynamicBackground { get; set; } = false;
}

/// <summary>
/// 渐变配置
/// </summary>
public class GradientConfiguration
{
    /// <summary>
    /// 起始颜色（十六进制）
    /// </summary>
    public string StartColor { get; set; } = "#141E30";

    /// <summary>
    /// 结束颜色（十六进制）
    /// </summary>
    public string EndColor { get; set; } = "#243B55";

    /// <summary>
    /// 渐变方向（角度）
    /// </summary>
    public int Direction { get; set; } = 45;
}

/// <summary>
/// 转场效果配置
/// </summary>
public class TransitionConfiguration
{
    /// <summary>
    /// 是否启用转场效果
    /// </summary>
    public bool EnableTransitions { get; set; } = true;

    /// <summary>
    /// 转场持续时间（毫秒）
    /// </summary>
    public int TransitionDurationMs { get; set; } = 1000;

    /// <summary>
    /// 转场类型
    /// </summary>
    public TransitionType Type { get; set; } = TransitionType.Fade;

    /// <summary>
    /// 淡入淡出配置
    /// </summary>
    public FadeConfiguration Fade { get; set; } = new();
}

/// <summary>
/// 淡入淡出配置
/// </summary>
public class FadeConfiguration
{
    /// <summary>
    /// 淡入时间（毫秒）
    /// </summary>
    public int FadeInDurationMs { get; set; } = 500;

    /// <summary>
    /// 淡出时间（毫秒）
    /// </summary>
    public int FadeOutDurationMs { get; set; } = 500;
}

/// <summary>
/// 临时文件配置
/// </summary>
public class TempFileConfiguration
{
    /// <summary>
    /// 临时文件根目录
    /// </summary>
    public string TempDirectory { get; set; } = "";

    /// <summary>
    /// 是否自动清理临时文件
    /// </summary>
    public bool AutoCleanup { get; set; } = true;

    /// <summary>
    /// 临时文件保留时间（小时）
    /// </summary>
    public int RetentionHours { get; set; } = 2;

    /// <summary>
    /// 最大临时文件数量
    /// </summary>
    public int MaxTempFiles { get; set; } = 1000;
}

/// <summary>
/// 背景类型枚举
/// </summary>
public enum BackgroundType
{
    /// <summary>
    /// 纯色背景
    /// </summary>
    Solid = 1,

    /// <summary>
    /// 渐变背景
    /// </summary>
    Gradient = 2,

    /// <summary>
    /// 图片背景
    /// </summary>
    Image = 3,

    /// <summary>
    /// 动态背景
    /// </summary>
    Dynamic = 4
}

/// <summary>
/// 转场类型枚举
/// </summary>
public enum TransitionType
{
    /// <summary>
    /// 淡入淡出
    /// </summary>
    Fade = 1,

    /// <summary>
    /// 滑动
    /// </summary>
    Slide = 2,

    /// <summary>
    /// 缩放
    /// </summary>
    Scale = 3,

    /// <summary>
    /// 无转场
    /// </summary>
    None = 4
}