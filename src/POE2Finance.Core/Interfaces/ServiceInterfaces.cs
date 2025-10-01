using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Models;

namespace POE2Finance.Core.Interfaces;

/// <summary>
/// 数据采集服务接口
/// </summary>
public interface IDataCollectionService
{
    /// <summary>
    /// 采集指定通货的价格数据
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据</returns>
    Task<PriceDataDto?> CollectPriceDataAsync(CurrencyType currencyType, DataSource dataSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// 采集所有通货的价格数据
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据列表</returns>
    Task<List<PriceDataDto>> CollectAllPricesAsync(DataSource dataSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证数据源是否可用
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可用</returns>
    Task<bool> ValidateDataSourceAsync(DataSource dataSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查所有数据源的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    Task<bool> CheckAllDataSourcesHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 从所有数据源采集数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>采集结果</returns>
    Task<List<PriceDataDto>> CollectFromAllSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 带回退的价格采集
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格数据</returns>
    Task<PriceDataDto?> CollectPriceWithFallbackAsync(CurrencyType currencyType, CancellationToken cancellationToken = default);
}

/// <summary>
/// 价格分析服务接口
/// </summary>
public interface IPriceAnalysisService
{
    /// <summary>
    /// 分析热点物品
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热点物品分析结果</returns>
    Task<List<HotItemAnalysisDto>> AnalyzeHotItemsAsync(PublishTimeSlot timeSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成市场分析报告
    /// </summary>
    /// <param name="timeSlot">时间段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>市场分析结果</returns>
    Task<MarketAnalysisResultDto> GenerateMarketAnalysisAsync(PublishTimeSlot timeSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算趋势类型
    /// </summary>
    /// <param name="priceHistory">价格历史数据</param>
    /// <param name="hours">分析时间跨度（小时）</param>
    /// <returns>趋势类型</returns>
    TrendType CalculateTrendType(List<CurrencyPrice> priceHistory, int hours = 24);
}

/// <summary>
/// 内容生成服务接口
/// </summary>
public interface IContentGenerationService
{
    /// <summary>
    /// 生成分析报告文本内容
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>报告内容</returns>
    Task<string> GenerateReportContentAsync(MarketAnalysisResultDto analysisResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成视频标题
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="timeSlot">时间段</param>
    /// <returns>视频标题</returns>
    string GenerateVideoTitle(MarketAnalysisResultDto analysisResult, PublishTimeSlot timeSlot);

    /// <summary>
    /// 生成视频描述
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="timeSlot">时间段</param>
    /// <returns>视频描述</returns>
    string GenerateVideoDescription(MarketAnalysisResultDto analysisResult, PublishTimeSlot timeSlot);

    /// <summary>
    /// 生成视频标签
    /// </summary>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="timeSlot">时间段</param>
    /// <returns>视频标签</returns>
    List<string> GenerateVideoTags(MarketAnalysisResultDto analysisResult, PublishTimeSlot timeSlot);
}

/// <summary>
/// 图表生成服务接口
/// </summary>
public interface IChartGenerationService
{
    /// <summary>
    /// 生成价格趋势图
    /// </summary>
    /// <param name="priceData">价格数据</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的图片路径</returns>
    Task<string> GeneratePriceTrendChartAsync(List<PriceDataDto> priceData, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成热点物品对比图
    /// </summary>
    /// <param name="hotItems">热点物品数据</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的图片路径</returns>
    Task<string> GenerateHotItemsChartAsync(List<HotItemAnalysisDto> hotItems, string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// 文本转语音服务接口
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// 生成语音音频
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的音频文件路径</returns>
    Task<string> GenerateAudioAsync(string text, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查TTS服务是否可用
    /// </summary>
    /// <returns>是否可用</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// 获取可用的语音列表
    /// </summary>
    /// <returns>语音列表</returns>
    Task<List<string>> GetAvailableVoicesAsync();
}

/// <summary>
/// 视频制作服务接口
/// </summary>
public interface IVideoCreationService
{
    /// <summary>
    /// 创建视频
    /// </summary>
    /// <param name="config">视频配置</param>
    /// <param name="analysisResult">分析结果</param>
    /// <param name="chartPaths">图表文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的视频文件路径</returns>
    Task<string> CreateVideoAsync(VideoGenerationConfigDto config, MarketAnalysisResultDto analysisResult, List<string> chartPaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成语音音频
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的音频文件路径</returns>
    Task<string> GenerateAudioAsync(string text, string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// 发布服务接口
/// </summary>
public interface IPublishingService
{
    /// <summary>
    /// 发布视频到B站
    /// </summary>
    /// <param name="videoPath">视频文件路径</param>
    /// <param name="title">视频标题</param>
    /// <param name="description">视频描述</param>
    /// <param name="tags">视频标签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布结果</returns>
    Task<(bool Success, string? VideoId, string? ErrorMessage)> PublishToBilibiliAsync(string videoPath, string title, string description, List<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查发布状态
    /// </summary>
    /// <param name="videoId">视频ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布状态</returns>
    Task<VideoStatus> CheckPublishStatusAsync(string videoId, CancellationToken cancellationToken = default);
}