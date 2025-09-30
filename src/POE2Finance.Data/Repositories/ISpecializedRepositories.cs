using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;

namespace POE2Finance.Data.Repositories;

/// <summary>
/// 通货价格仓储接口
/// </summary>
public interface ICurrencyPriceRepository : IRepository<CurrencyPrice>
{
    /// <summary>
    /// 获取指定通货的最新价格
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新价格</returns>
    Task<CurrencyPrice?> GetLatestPriceAsync(CurrencyType currencyType, DataSource? dataSource = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间范围内的价格历史
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>价格历史列表</returns>
    Task<List<CurrencyPrice>> GetPriceHistoryAsync(CurrencyType currencyType, DateTime startTime, DateTime endTime, DataSource? dataSource = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有通货的最新价格
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新价格列表</returns>
    Task<List<CurrencyPrice>> GetLatestPricesAsync(DataSource? dataSource = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入价格数据
    /// </summary>
    /// <param name="prices">价格数据列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BulkInsertPricesAsync(List<CurrencyPrice> prices, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除过期价格数据
    /// </summary>
    /// <param name="cutoffDate">截止日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteExpiredPricesAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// 分析报告仓储接口
/// </summary>
public interface IAnalysisReportRepository : IRepository<AnalysisReport>
{
    /// <summary>
    /// 获取指定日期和时间段的报告
    /// </summary>
    /// <param name="reportDate">报告日期</param>
    /// <param name="timeSlot">时间段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析报告</returns>
    Task<AnalysisReport?> GetReportByDateAndSlotAsync(DateTime reportDate, PublishTimeSlot timeSlot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近的报告
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>报告列表</returns>
    Task<List<AnalysisReport>> GetRecentReportsAsync(int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定状态的报告
    /// </summary>
    /// <param name="status">报告状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>报告列表</returns>
    Task<List<AnalysisReport>> GetReportsByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);
}

/// <summary>
/// 视频记录仓储接口
/// </summary>
public interface IVideoRecordRepository : IRepository<VideoRecord>
{
    /// <summary>
    /// 获取指定报告的视频记录
    /// </summary>
    /// <param name="reportId">报告ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>视频记录列表</returns>
    Task<List<VideoRecord>> GetVideosByReportIdAsync(int reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定状态的视频记录
    /// </summary>
    /// <param name="status">视频状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>视频记录列表</returns>
    Task<List<VideoRecord>> GetVideosByStatusAsync(VideoStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据B站视频ID获取视频记录
    /// </summary>
    /// <param name="bvId">B站BV号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>视频记录</returns>
    Task<VideoRecord?> GetVideoByBvIdAsync(string bvId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取需要清理的本地视频文件
    /// </summary>
    /// <param name="olderThan">早于指定时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>视频记录列表</returns>
    Task<List<VideoRecord>> GetVideosForCleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}