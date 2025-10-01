using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using POE2Finance.Data.DbContexts;
using POE2Finance.Core.Entities;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Interfaces;
using POE2Finance.Data.Repositories;

namespace POE2Finance.Tests.Integration;

/// <summary>
/// 集成测试基类
/// </summary>
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 移除现有的数据库上下文
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<POE2FinanceDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // 添加内存数据库用于测试
                services.AddDbContext<POE2FinanceDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        Client = Factory.CreateClient();
    }
}

/// <summary>
/// 数据库集成测试
/// </summary>
public class DatabaseIntegrationTests : IntegrationTestBase
{
    public DatabaseIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Database_ShouldCreateAndSeedData()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POE2FinanceDbContext>();

        // Act
        await context.Database.EnsureCreatedAsync();

        // Assert
        var currencies = await context.CurrencyMetadata.ToListAsync();
        currencies.Should().HaveCount(3);
        currencies.Should().Contain(c => c.CurrencyType == CurrencyType.ExaltedOrb);
        currencies.Should().Contain(c => c.CurrencyType == CurrencyType.DivineOrb);
        currencies.Should().Contain(c => c.CurrencyType == CurrencyType.ChaosOrb);
    }

    [Fact]
    public async Task CurrencyPriceRepository_ShouldInsertAndRetrieveData()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICurrencyPriceRepository>();

        var priceData = new CurrencyPrice
        {
            CurrencyType = CurrencyType.ExaltedOrb,
            PriceInExalted = 1.0m,
            OriginalPrice = 1.0m,
            OriginalPriceUnit = "E",
            TradeVolume = 100,
            DataSource = DataSource.TencentOfficial,
            CollectedAt = DateTime.UtcNow,
            IsValid = true
        };

        // Act
        var insertedPrice = await repository.AddAsync(priceData);
        var retrievedPrice = await repository.GetLatestPriceAsync(CurrencyType.ExaltedOrb, DataSource.TencentOfficial);

        // Assert
        insertedPrice.Should().NotBeNull();
        insertedPrice.Id.Should().BeGreaterThan(0);
        retrievedPrice.Should().NotBeNull();
        retrievedPrice!.PriceInExalted.Should().Be(1.0m);
        retrievedPrice.TradeVolume.Should().Be(100);
    }

    [Fact]
    public async Task AnalysisReportRepository_ShouldHandleReportLifecycle()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalysisReportRepository>();

        var report = new AnalysisReport
        {
            ReportDate = DateTime.Today,
            TimeSlot = PublishTimeSlot.Morning,
            Title = "测试报告",
            Summary = "测试摘要",
            DetailedAnalysis = "详细分析内容",
            Status = ReportStatus.Generating,
            HotItemsData = "{}",
            TrendData = "{}"
        };

        // Act
        var insertedReport = await repository.AddAsync(report);
        
        insertedReport.Status = ReportStatus.Completed;
        await repository.UpdateAsync(insertedReport);

        var retrievedReport = await repository.GetReportByDateAndSlotAsync(DateTime.Today, PublishTimeSlot.Morning);

        // Assert
        insertedReport.Should().NotBeNull();
        retrievedReport.Should().NotBeNull();
        retrievedReport!.Status.Should().Be(ReportStatus.Completed);
        retrievedReport.Title.Should().Be("测试报告");
    }
}

/// <summary>
/// 服务集成测试
/// </summary>
public class ServiceIntegrationTests : IntegrationTestBase
{
    public ServiceIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task PriceAnalysisService_ShouldAnalyzeEmptyData()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IPriceAnalysisService>();

        // Act
        var hotItems = await analysisService.AnalyzeHotItemsAsync(PublishTimeSlot.Morning);

        // Assert
        hotItems.Should().NotBeNull();
        // 由于没有价格数据，应该返回空列表
        hotItems.Should().BeEmpty();
    }

    [Fact]
    public async Task PriceAnalysisService_ShouldGenerateMarketAnalysis()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IPriceAnalysisService>();

        // Act
        var marketAnalysis = await analysisService.GenerateMarketAnalysisAsync(PublishTimeSlot.Afternoon);

        // Assert
        marketAnalysis.Should().NotBeNull();
        marketAnalysis.TimeSlot.Should().Be(PublishTimeSlot.Afternoon);
        marketAnalysis.AnalysisTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        marketAnalysis.MarketDynamics.Should().NotBeNullOrEmpty();
        marketAnalysis.TradingAdvice.Should().NotBeNullOrEmpty();
        marketAnalysis.RiskWarning.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ContentGenerationService_ShouldGenerateContent()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentGenerationService>();

        var mockAnalysisResult = new POE2Finance.Core.Models.MarketAnalysisResultDto
        {
            AnalysisTime = DateTime.UtcNow,
            TimeSlot = PublishTimeSlot.Evening,
            OverallTrend = TrendType.ModerateUptrend,
            MarketDynamics = "市场测试数据",
            TradingAdvice = "测试建议",
            RiskWarning = "测试风险提示",
            HotItems = new List<POE2Finance.Core.Models.HotItemAnalysisDto>()
        };

        // Act
        var title = contentService.GenerateVideoTitle(mockAnalysisResult, PublishTimeSlot.Evening);
        var description = contentService.GenerateVideoDescription(mockAnalysisResult, PublishTimeSlot.Evening);
        var tags = contentService.GenerateVideoTags(mockAnalysisResult, PublishTimeSlot.Evening);

        // Assert
        title.Should().NotBeNullOrEmpty();
        title.Should().Contain("POE2国服");
        title.Should().Contain("晚间场");

        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("晚间场");

        tags.Should().NotBeNull();
        tags.Should().Contain("POE2");
        tags.Should().Contain("流放之路2");
    }
}

/// <summary>
/// API集成测试
/// </summary>
public class ApiIntegrationTests : IntegrationTestBase
{
    public ApiIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnSuccess()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.Should().BeSuccessful();
    }

    [Fact]
    public async Task SwaggerUI_ShouldBeAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger");

        // Assert
        response.Should().BeSuccessful();
    }
}

/// <summary>
/// 端到端集成测试
/// </summary>
public class EndToEndIntegrationTests : IntegrationTestBase
{
    public EndToEndIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CompleteWorkflow_ShouldExecuteSuccessfully()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var priceRepository = scope.ServiceProvider.GetRequiredService<ICurrencyPriceRepository>();
        var analysisService = scope.ServiceProvider.GetRequiredService<IPriceAnalysisService>();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentGenerationService>();

        // 1. 插入测试价格数据
        var testPrices = new List<CurrencyPrice>
        {
            new()
            {
                CurrencyType = CurrencyType.ExaltedOrb,
                PriceInExalted = 1.0m,
                OriginalPrice = 1.0m,
                OriginalPriceUnit = "E",
                TradeVolume = 100,
                DataSource = DataSource.TencentOfficial,
                CollectedAt = DateTime.UtcNow.AddHours(-2),
                IsValid = true
            },
            new()
            {
                CurrencyType = CurrencyType.DivineOrb,
                PriceInExalted = 2.5m,
                OriginalPrice = 2.5m,
                OriginalPriceUnit = "E",
                TradeVolume = 80,
                DataSource = DataSource.TencentOfficial,
                CollectedAt = DateTime.UtcNow.AddHours(-1),
                IsValid = true
            }
        };

        await priceRepository.BulkInsertPricesAsync(testPrices);

        // 2. 执行分析
        var analysisResult = await analysisService.GenerateMarketAnalysisAsync(PublishTimeSlot.Morning);

        // 3. 生成内容
        var title = contentService.GenerateVideoTitle(analysisResult, PublishTimeSlot.Morning);
        var description = contentService.GenerateVideoDescription(analysisResult, PublishTimeSlot.Morning);
        var reportContent = await contentService.GenerateReportContentAsync(analysisResult);

        // Assert
        analysisResult.Should().NotBeNull();
        analysisResult.TimeSlot.Should().Be(PublishTimeSlot.Morning);

        title.Should().NotBeNullOrEmpty();
        title.Should().Contain("POE2国服");
        title.Should().Contain("上午场");

        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("上午场");

        reportContent.Should().NotBeNullOrEmpty();
        reportContent.Should().Contain("市场分析");
    }
}