using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POE2Finance.Data.DbContexts;
using POE2Finance.Data.Repositories;

namespace POE2Finance.Data.Extensions;

/// <summary>
/// 数据层依赖注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加数据层服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加数据库上下文
        services.AddDbContext<POE2FinanceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=poe2finance.db";
            options.UseSqlite(connectionString);
            
            // 开发环境启用敏感数据日志
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // 注册仓储服务
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICurrencyPriceRepository, CurrencyPriceRepository>();
        services.AddScoped<IAnalysisReportRepository, AnalysisReportRepository>();
        services.AddScoped<IVideoRecordRepository, VideoRecordRepository>();

        return services;
    }

    /// <summary>
    /// 确保数据库创建和迁移
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>异步任务</returns>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POE2FinanceDbContext>();
        
        try
        {
            // 确保数据库创建
            await context.Database.EnsureCreatedAsync();
            
            // 检查是否有挂起的迁移
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常，避免应用启动失败
            Console.WriteLine($"Database initialization error: {ex.Message}");
        }
    }
}