using Microsoft.EntityFrameworkCore;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Data.DbContexts;

namespace POE2Finance.Data.Repositories;

/// <summary>
/// 分析报告仓储实现
/// </summary>
public class AnalysisReportRepository : Repository<AnalysisReport>, IAnalysisReportRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public AnalysisReportRepository(POE2FinanceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<AnalysisReport?> GetReportByDateAndSlotAsync(DateTime reportDate, PublishTimeSlot timeSlot, CancellationToken cancellationToken = default)
    {
        var date = reportDate.Date; // 确保只比较日期部分
        return await _dbSet
            .Include(r => r.Videos)
            .FirstOrDefaultAsync(r => r.ReportDate.Date == date && r.TimeSlot == timeSlot, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AnalysisReport>> GetRecentReportsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Videos)
            .OrderByDescending(r => r.ReportDate)
            .ThenByDescending(r => r.TimeSlot)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AnalysisReport>> GetReportsByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Videos)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 视频记录仓储实现
/// </summary>
public class VideoRecordRepository : Repository<VideoRecord>, IVideoRecordRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public VideoRecordRepository(POE2FinanceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<List<VideoRecord>> GetVideosByReportIdAsync(int reportId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.AnalysisReport)
            .Where(v => v.AnalysisReportId == reportId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<VideoRecord>> GetVideosByStatusAsync(VideoStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.AnalysisReport)
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VideoRecord?> GetVideoByBvIdAsync(string bvId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.AnalysisReport)
            .FirstOrDefaultAsync(v => v.BilibiliBvId == bvId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<VideoRecord>> GetVideosForCleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Status == VideoStatus.Published && 
                       v.ActualPublishTime.HasValue && 
                       v.ActualPublishTime.Value < olderThan &&
                       !string.IsNullOrEmpty(v.LocalFilePath))
            .ToListAsync(cancellationToken);
    }
}