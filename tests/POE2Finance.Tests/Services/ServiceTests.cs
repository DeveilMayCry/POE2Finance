using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Models;
using POE2Finance.Services.Analysis;
using POE2Finance.Services.Configuration;
using POE2Finance.Data.Repositories;
using POE2Finance.Core.Entities;

namespace POE2Finance.Tests.Services;

/// <summary>
/// 价格分析服务单元测试
/// </summary>
public class PriceAnalysisServiceTests
{
    private readonly Mock<ICurrencyPriceRepository> _mockPriceRepository;
    private readonly Mock<ILogger<PriceAnalysisService>> _mockLogger;
    private readonly IOptions<AnalysisConfiguration> _config;
    private readonly PriceAnalysisService _service;

    public PriceAnalysisServiceTests()
    {
        _mockPriceRepository = new Mock<ICurrencyPriceRepository>();
        _mockLogger = new Mock<ILogger<PriceAnalysisService>>();
        _config = Options.Create(new AnalysisConfiguration
        {
            StrongTrendThreshold = 10.0m,
            ModerateTrendThreshold = 5.0m,
            HotScoreWeights = new HotScoreWeights
            {
                VolatilityWeight = 0.4m,
                VolumeWeight = 0.35m,
                TrendWeight = 0.25m
            }
        });

        _service = new PriceAnalysisService(_mockPriceRepository.Object, _mockLogger.Object, _config);
    }

    [Fact]
    public async Task AnalyzeHotItemsAsync_WithValidData_ShouldReturnHotItems()
    {
        // Arrange
        var timeSlot = PublishTimeSlot.Morning;
        var priceHistory = CreateMockPriceHistory();
        
        _mockPriceRepository
            .Setup(x => x.GetPriceHistoryAsync(It.IsAny<CurrencyType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceHistory);

        // Act
        var result = await _service.AnalyzeHotItemsAsync(timeSlot, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.First().CurrencyType.Should().Be(CurrencyType.ExaltedOrb);
    }

    [Fact]
    public void CalculateTrendType_WithUpwardTrend_ShouldReturnUptrend()
    {
        // Arrange
        var priceHistory = new List<CurrencyPrice>
        {
            new() { PriceInExalted = 1.0m, CollectedAt = DateTime.UtcNow.AddHours(-3) },
            new() { PriceInExalted = 1.05m, CollectedAt = DateTime.UtcNow.AddHours(-2) },
            new() { PriceInExalted = 1.12m, CollectedAt = DateTime.UtcNow.AddHours(-1) },
            new() { PriceInExalted = 1.15m, CollectedAt = DateTime.UtcNow }
        };

        // Act
        var result = _service.CalculateTrendType(priceHistory, 4);

        // Assert
        result.Should().Be(TrendType.StrongUptrend);
    }

    [Fact]
    public void CalculateTrendType_WithDownwardTrend_ShouldReturnDowntrend()
    {
        // Arrange
        var priceHistory = new List<CurrencyPrice>
        {
            new() { PriceInExalted = 1.15m, CollectedAt = DateTime.UtcNow.AddHours(-3) },
            new() { PriceInExalted = 1.10m, CollectedAt = DateTime.UtcNow.AddHours(-2) },
            new() { PriceInExalted = 1.03m, CollectedAt = DateTime.UtcNow.AddHours(-1) },
            new() { PriceInExalted = 1.0m, CollectedAt = DateTime.UtcNow }
        };

        // Act
        var result = _service.CalculateTrendType(priceHistory, 4);

        // Assert
        result.Should().Be(TrendType.StrongDowntrend);
    }

    [Fact]
    public void CalculateTrendType_WithStablePrice_ShouldReturnSideways()
    {
        // Arrange
        var priceHistory = new List<CurrencyPrice>
        {
            new() { PriceInExalted = 1.0m, CollectedAt = DateTime.UtcNow.AddHours(-3) },
            new() { PriceInExalted = 1.01m, CollectedAt = DateTime.UtcNow.AddHours(-2) },
            new() { PriceInExalted = 0.99m, CollectedAt = DateTime.UtcNow.AddHours(-1) },
            new() { PriceInExalted = 1.0m, CollectedAt = DateTime.UtcNow }
        };

        // Act
        var result = _service.CalculateTrendType(priceHistory, 4);

        // Assert
        result.Should().Be(TrendType.Sideways);
    }

    [Fact]
    public async Task GenerateMarketAnalysisAsync_WithValidData_ShouldReturnAnalysis()
    {
        // Arrange
        var timeSlot = PublishTimeSlot.Afternoon;
        var priceHistory = CreateMockPriceHistory();
        
        _mockPriceRepository
            .Setup(x => x.GetPriceHistoryAsync(It.IsAny<CurrencyType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceHistory);

        // Act
        var result = await _service.GenerateMarketAnalysisAsync(timeSlot, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TimeSlot.Should().Be(timeSlot);
        result.AnalysisTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.HotItems.Should().NotBeNull();
        result.MarketDynamics.Should().NotBeNullOrEmpty();
        result.TradingAdvice.Should().NotBeNullOrEmpty();
        result.RiskWarning.Should().NotBeNullOrEmpty();
    }

    private static List<CurrencyPrice> CreateMockPriceHistory()
    {
        return new List<CurrencyPrice>
        {
            new()
            {
                CurrencyType = CurrencyType.ExaltedOrb,
                PriceInExalted = 1.0m,
                TradeVolume = 100,
                CollectedAt = DateTime.UtcNow.AddHours(-6),
                IsValid = true
            },
            new()
            {
                CurrencyType = CurrencyType.ExaltedOrb,
                PriceInExalted = 1.05m,
                TradeVolume = 120,
                CollectedAt = DateTime.UtcNow.AddHours(-3),
                IsValid = true
            },
            new()
            {
                CurrencyType = CurrencyType.ExaltedOrb,
                PriceInExalted = 1.12m,
                TradeVolume = 150,
                CollectedAt = DateTime.UtcNow,
                IsValid = true
            }
        };
    }
}

/// <summary>
/// 内容生成服务单元测试
/// </summary>
public class ContentGenerationServiceTests
{
    private readonly Mock<ILogger<ContentGenerationService>> _mockLogger;
    private readonly IOptions<ContentGenerationConfiguration> _config;
    private readonly ContentGenerationService _service;

    public ContentGenerationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ContentGenerationService>>();
        _config = Options.Create(new ContentGenerationConfiguration
        {
            EnableAiGeneration = true
        });

        _service = new ContentGenerationService(_mockLogger.Object, _config);
    }

    [Fact]
    public void GenerateVideoTitle_WithHotItems_ShouldReturnValidTitle()
    {
        // Arrange
        var analysisResult = CreateMockAnalysisResult();
        var timeSlot = PublishTimeSlot.Morning;

        // Act
        var result = _service.GenerateVideoTitle(analysisResult, timeSlot);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("POE2国服");
        result.Should().Contain("上午场");
        result.Length.Should().BeLessOrEqualTo(80);
    }

    [Fact]
    public void GenerateVideoDescription_WithAnalysisResult_ShouldReturnValidDescription()
    {
        // Arrange
        var analysisResult = CreateMockAnalysisResult();
        var timeSlot = PublishTimeSlot.Evening;

        // Act
        var result = _service.GenerateVideoDescription(analysisResult, timeSlot);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("晚间场");
        result.Should().Contain("热点通货");
        result.Should().Contain("#POE2");
        result.Length.Should().BeLessOrEqualTo(2000);
    }

    [Fact]
    public void GenerateVideoTags_WithAnalysisResult_ShouldReturnValidTags()
    {
        // Arrange
        var analysisResult = CreateMockAnalysisResult();
        var timeSlot = PublishTimeSlot.Afternoon;

        // Act
        var result = _service.GenerateVideoTags(analysisResult, timeSlot);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(10);
        result.Should().Contain("POE2");
        result.Should().Contain("流放之路2");
        result.Should().Contain("POE2国服");
    }

    [Fact]
    public async Task GenerateReportContentAsync_WithAnalysisResult_ShouldReturnContent()
    {
        // Arrange
        var analysisResult = CreateMockAnalysisResult();

        // Act
        var result = await _service.GenerateReportContentAsync(analysisResult, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("热点通货分析");
        result.Should().Contain("市场动态");
        result.Should().Contain("交易建议");
        result.Should().Contain("风险提示");
    }

    private static MarketAnalysisResultDto CreateMockAnalysisResult()
    {
        return new MarketAnalysisResultDto
        {
            AnalysisTime = DateTime.UtcNow,
            TimeSlot = PublishTimeSlot.Morning,
            OverallTrend = TrendType.ModerateUptrend,
            MarketDynamics = "市场整体呈现温和上涨趋势",
            TradingAdvice = "建议适当增加持仓，注意风险控制",
            RiskWarning = "市场波动较大，请谨慎投资",
            HotItems = new List<HotItemAnalysisDto>
            {
                new()
                {
                    CurrencyType = CurrencyType.ExaltedOrb,
                    CurrencyName = "崇高石",
                    HotScore = 75.5m,
                    PriceVolatility = 12.3m,
                    VolumeChangePercent = 25.0m,
                    TrendDurationHours = 6,
                    TrendType = TrendType.StrongUptrend,
                    RecommendedAction = "建议关注，可考虑适量买入"
                }
            }
        };
    }
}