using Microsoft.EntityFrameworkCore;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;

namespace POE2Finance.Data.DbContexts;

/// <summary>
/// POE2 Finance 数据库上下文
/// </summary>
public class POE2FinanceDbContext : DbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库选项</param>
    public POE2FinanceDbContext(DbContextOptions<POE2FinanceDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 通货元数据
    /// </summary>
    public DbSet<CurrencyMetadata> CurrencyMetadata { get; set; }

    /// <summary>
    /// 通货价格
    /// </summary>
    public DbSet<CurrencyPrice> CurrencyPrices { get; set; }

    /// <summary>
    /// 分析报告
    /// </summary>
    public DbSet<AnalysisReport> AnalysisReports { get; set; }

    /// <summary>
    /// 视频记录
    /// </summary>
    public DbSet<VideoRecord> VideoRecords { get; set; }

    /// <summary>
    /// 模型配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置通货元数据
        modelBuilder.Entity<CurrencyMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyType).HasConversion<int>();
            entity.HasIndex(e => e.CurrencyType).IsUnique();
            entity.HasIndex(e => e.ShortName).IsUnique();
        });

        // 配置通货价格
        modelBuilder.Entity<CurrencyPrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyType).HasConversion<int>();
            entity.Property(e => e.DataSource).HasConversion<int>();
            entity.HasIndex(e => new { e.CurrencyType, e.CollectedAt });
            entity.HasIndex(e => new { e.DataSource, e.CollectedAt });
            
            // 与通货元数据的关系
            entity.HasOne(e => e.CurrencyMetadataInfo)
                  .WithMany(c => c.Prices)
                  .HasForeignKey(e => e.CurrencyType)
                  .HasPrincipalKey(c => c.CurrencyType)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置分析报告
        modelBuilder.Entity<AnalysisReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TimeSlot).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => new { e.ReportDate, e.TimeSlot }).IsUnique();
        });

        // 配置视频记录
        modelBuilder.Entity<VideoRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.TimeSlot).HasConversion<int>();
            entity.HasIndex(e => e.AnalysisReportId);
            entity.HasIndex(e => e.BilibiliBvId).IsUnique();
            
            // 与分析报告的关系
            entity.HasOne(e => e.AnalysisReport)
                  .WithMany(r => r.Videos)
                  .HasForeignKey(e => e.AnalysisReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 初始化种子数据
        SeedData(modelBuilder);
    }

    /// <summary>
    /// 种子数据配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // 初始化通货元数据
        modelBuilder.Entity<CurrencyMetadata>().HasData(
            new CurrencyMetadata
            {
                Id = 1,
                CurrencyType = CurrencyType.ExaltedOrb,
                ChineseName = "崇高石",
                EnglishName = "Exalted Orb",
                ShortName = "E",
                Description = "POE2中的顶级通货，用作基准计价单位",
                IsBaseCurrency = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CurrencyMetadata
            {
                Id = 2,
                CurrencyType = CurrencyType.DivineOrb,
                ChineseName = "神圣石",
                EnglishName = "Divine Orb",
                ShortName = "D",
                Description = "POE2中的高级通货，价值仅次于崇高石",
                IsBaseCurrency = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CurrencyMetadata
            {
                Id = 3,
                CurrencyType = CurrencyType.ChaosOrb,
                ChineseName = "混沌石",
                EnglishName = "Chaos Orb",
                ShortName = "C",
                Description = "POE2中的中级通货，交易活跃度最高",
                IsBaseCurrency = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }

    /// <summary>
    /// 保存更改前的处理
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动更新时间戳
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}