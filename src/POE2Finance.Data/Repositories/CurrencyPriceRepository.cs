using Microsoft.EntityFrameworkCore;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Data.DbContexts;

namespace POE2Finance.Data.Repositories;

/// <summary>
/// 通货价格仓储实现
/// </summary>
public class CurrencyPriceRepository : Repository<CurrencyPrice>, ICurrencyPriceRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public CurrencyPriceRepository(POE2FinanceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<CurrencyPrice?> GetLatestPriceAsync(CurrencyType currencyType, DataSource? dataSource = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(p => p.CurrencyType == currencyType && p.IsValid);

        if (dataSource.HasValue)
        {
            query = query.Where(p => p.DataSource == dataSource.Value);
        }

        return await query
            .OrderByDescending(p => p.CollectedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<CurrencyPrice>> GetPriceHistoryAsync(CurrencyType currencyType, DateTime startTime, DateTime endTime, DataSource? dataSource = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(p => 
            p.CurrencyType == currencyType && 
            p.IsValid &&
            p.CollectedAt >= startTime && 
            p.CollectedAt <= endTime);

        if (dataSource.HasValue)
        {
            query = query.Where(p => p.DataSource == dataSource.Value);
        }

        return await query
            .OrderBy(p => p.CollectedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<CurrencyPrice>> GetLatestPricesAsync(DataSource? dataSource = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(p => p.IsValid);

        if (dataSource.HasValue)
        {
            query = query.Where(p => p.DataSource == dataSource.Value);
        }

        // 获取每种通货的最新价格
        var latestPrices = await query
            .GroupBy(p => p.CurrencyType)
            .Select(g => g.OrderByDescending(p => p.CollectedAt).First())
            .ToListAsync(cancellationToken);

        return latestPrices;
    }

    /// <inheritdoc/>
    public async Task BulkInsertPricesAsync(List<CurrencyPrice> prices, CancellationToken cancellationToken = default)
    {
        if (prices.Count == 0) return;

        await _dbSet.AddRangeAsync(prices, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteExpiredPricesAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var expiredPrices = await _dbSet
            .Where(p => p.CollectedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (expiredPrices.Count > 0)
        {
            _dbSet.RemoveRange(expiredPrices);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}