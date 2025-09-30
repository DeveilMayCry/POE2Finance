namespace POE2Finance.Services.Configuration;

/// <summary>
/// 图表生成配置
/// </summary>
public class ChartConfiguration
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Charts";

    /// <summary>
    /// 图表宽度
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// 图表高度
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// 仪表盘宽度
    /// </summary>
    public int DashboardWidth { get; set; } = 1920;

    /// <summary>
    /// 仪表盘高度
    /// </summary>
    public int DashboardHeight { get; set; } = 1080;

    /// <summary>
    /// 线条宽度
    /// </summary>
    public float LineWidth { get; set; } = 2.0f;

    /// <summary>
    /// 标记点大小
    /// </summary>
    public float MarkerSize { get; set; } = 5.0f;

    /// <summary>
    /// 标题字体大小
    /// </summary>
    public float TitleFontSize { get; set; } = 16.0f;

    /// <summary>
    /// 坐标轴标签字体大小
    /// </summary>
    public float AxisLabelFontSize { get; set; } = 12.0f;

    /// <summary>
    /// 图例字体大小
    /// </summary>
    public float LegendFontSize { get; set; } = 10.0f;

    /// <summary>
    /// 背景颜色（十六进制）
    /// </summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// 绘图区背景颜色（十六进制）
    /// </summary>
    public string PlotBackgroundColor { get; set; } = "#F8F8F8";

    /// <summary>
    /// 网格颜色（十六进制）
    /// </summary>
    public string GridColor { get; set; } = "#E0E0E0";

    /// <summary>
    /// 通货颜色配置
    /// </summary>
    public CurrencyColorConfiguration CurrencyColors { get; set; } = new();

    /// <summary>
    /// 字体配置
    /// </summary>
    public FontConfiguration Fonts { get; set; } = new();

    /// <summary>
    /// 输出配置
    /// </summary>
    public OutputConfiguration Output { get; set; } = new();
}

/// <summary>
/// 通货颜色配置
/// </summary>
public class CurrencyColorConfiguration
{
    /// <summary>
    /// 崇高石颜色（十六进制）
    /// </summary>
    public string ExaltedOrb { get; set; } = "#FFD700"; // 金色

    /// <summary>
    /// 神圣石颜色（十六进制）
    /// </summary>
    public string DivineOrb { get; set; } = "#8A2BE2"; // 紫色

    /// <summary>
    /// 混沌石颜色（十六进制）
    /// </summary>
    public string ChaosOrb { get; set; } = "#FF6347"; // 橙红色

    /// <summary>
    /// 上涨趋势颜色（十六进制）
    /// </summary>
    public string UptrendColor { get; set; } = "#00FF00"; // 绿色

    /// <summary>
    /// 下跌趋势颜色（十六进制）
    /// </summary>
    public string DowntrendColor { get; set; } = "#FF0000"; // 红色

    /// <summary>
    /// 横盘趋势颜色（十六进制）
    /// </summary>
    public string SidewaysColor { get; set; } = "#808080"; // 灰色
}

/// <summary>
/// 字体配置
/// </summary>
public class FontConfiguration
{
    /// <summary>
    /// 默认字体系列
    /// </summary>
    public string DefaultFamily { get; set; } = "Microsoft YaHei";

    /// <summary>
    /// 标题字体系列
    /// </summary>
    public string TitleFamily { get; set; } = "Microsoft YaHei";

    /// <summary>
    /// 标签字体系列
    /// </summary>
    public string LabelFamily { get; set; } = "Microsoft YaHei";

    /// <summary>
    /// 是否使用粗体标题
    /// </summary>
    public bool BoldTitle { get; set; } = true;

    /// <summary>
    /// 是否使用粗体标签
    /// </summary>
    public bool BoldLabels { get; set; } = false;
}

/// <summary>
/// 输出配置
/// </summary>
public class OutputConfiguration
{
    /// <summary>
    /// 图片质量（1-100）
    /// </summary>
    public int Quality { get; set; } = 95;

    /// <summary>
    /// DPI设置
    /// </summary>
    public int Dpi { get; set; } = 300;

    /// <summary>
    /// 输出格式
    /// </summary>
    public ImageFormat Format { get; set; } = ImageFormat.PNG;

    /// <summary>
    /// 是否包含透明背景
    /// </summary>
    public bool TransparentBackground { get; set; } = false;

    /// <summary>
    /// 是否添加水印
    /// </summary>
    public bool AddWatermark { get; set; } = true;

    /// <summary>
    /// 水印文本
    /// </summary>
    public string WatermarkText { get; set; } = "POE2Finance";

    /// <summary>
    /// 水印位置
    /// </summary>
    public WatermarkPosition WatermarkPosition { get; set; } = WatermarkPosition.BottomRight;

    /// <summary>
    /// 水印透明度（0-1）
    /// </summary>
    public float WatermarkOpacity { get; set; } = 0.3f;
}

/// <summary>
/// 图片格式枚举
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// PNG格式
    /// </summary>
    PNG = 1,

    /// <summary>
    /// JPEG格式
    /// </summary>
    JPEG = 2,

    /// <summary>
    /// SVG格式
    /// </summary>
    SVG = 3
}

/// <summary>
/// 水印位置枚举
/// </summary>
public enum WatermarkPosition
{
    /// <summary>
    /// 左上角
    /// </summary>
    TopLeft = 1,

    /// <summary>
    /// 右上角
    /// </summary>
    TopRight = 2,

    /// <summary>
    /// 左下角
    /// </summary>
    BottomLeft = 3,

    /// <summary>
    /// 右下角
    /// </summary>
    BottomRight = 4,

    /// <summary>
    /// 中央
    /// </summary>
    Center = 5
}