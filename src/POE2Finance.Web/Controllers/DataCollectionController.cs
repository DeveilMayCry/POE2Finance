using Microsoft.AspNetCore.Mvc;
using POE2Finance.Core.Enums;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Models;

namespace POE2Finance.Web.Controllers;

/// <summary>
/// 数据采集控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataCollectionController : ControllerBase
{
    private readonly IDataCollectionService _dataCollectionService;
    private readonly ILogger<DataCollectionController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dataCollectionService">数据采集服务</param>
    /// <param name="logger">日志记录器</param>
    public DataCollectionController(
        IDataCollectionService dataCollectionService, 
        ILogger<DataCollectionController> logger)
    {
        _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 手动触发从所有数据源采集价格数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>采集结果</returns>
    [HttpPost("trigger-all")]
    public async Task<ActionResult<CollectionResultDto>> TriggerCollectAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到手动触发全量采集请求");

        try
        {
            var result = await _dataCollectionService.CollectFromAllSourcesAsync(cancellationToken);
            
            var response = new CollectionResultDto
            {
                Success = true,
                TotalCount = result.Count,
                Message = $"成功从所有数据源采集到 {result.Count} 条价格数据",
                Data = result,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("手动采集完成，采集到 {Count} 条数据", result.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动采集过程中发生异常");
            
            var errorResponse = new CollectionResultDto
            {
                Success = false,
                TotalCount = 0,
                Message = $"采集失败：{ex.Message}",
                Data = new List<PriceDataDto>(),
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 手动触发从指定数据源采集价格数据
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>采集结果</returns>
    [HttpPost("trigger/{dataSource}")]
    public async Task<ActionResult<CollectionResultDto>> TriggerCollectFromSourceAsync(
        [FromRoute] DataSource dataSource,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到手动触发采集请求，数据源: {DataSource}", dataSource);

        try
        {
            var result = await _dataCollectionService.CollectAllPricesAsync(dataSource, cancellationToken);
            
            var response = new CollectionResultDto
            {
                Success = true,
                TotalCount = result.Count,
                Message = $"成功从 {dataSource} 采集到 {result.Count} 条价格数据",
                Data = result,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("手动采集完成，从 {DataSource} 采集到 {Count} 条数据", dataSource, result.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 {DataSource} 手动采集过程中发生异常", dataSource);
            
            var errorResponse = new CollectionResultDto
            {
                Success = false,
                TotalCount = 0,
                Message = $"从 {dataSource} 采集失败：{ex.Message}",
                Data = new List<PriceDataDto>(),
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 手动触发采集指定通货的价格数据（带回退机制）
    /// </summary>
    /// <param name="currencyType">通货类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>采集结果</returns>
    [HttpPost("trigger/currency/{currencyType}")]
    public async Task<ActionResult<SinglePriceResultDto>> TriggerCollectCurrencyAsync(
        [FromRoute] CurrencyType currencyType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到手动触发采集请求，通货: {CurrencyType}", currencyType);

        try
        {
            var result = await _dataCollectionService.CollectPriceWithFallbackAsync(currencyType, cancellationToken);
            
            if (result != null)
            {
                var response = new SinglePriceResultDto
                {
                    Success = true,
                    Message = $"成功采集到 {currencyType} 价格数据",
                    Data = result,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("手动采集完成，采集到 {CurrencyType} 价格: {Price}", 
                    currencyType, result.CurrentPriceInExalted);
                return Ok(response);
            }
            else
            {
                var errorResponse = new SinglePriceResultDto
                {
                    Success = false,
                    Message = $"无法从任何数据源采集到 {currencyType} 价格数据",
                    Data = null,
                    Timestamp = DateTime.UtcNow
                };

                return NotFound(errorResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "采集 {CurrencyType} 价格过程中发生异常", currencyType);
            
            var errorResponse = new SinglePriceResultDto
            {
                Success = false,
                Message = $"采集 {currencyType} 价格失败：{ex.Message}",
                Data = null,
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 获取可用的数据源列表
    /// </summary>
    /// <returns>数据源列表</returns>
    [HttpGet("sources")]
    public ActionResult<DataSourceStatusDto> GetAvailableDataSources()
    {
        try
        {
            var dataSources = _dataCollectionService.GetAvailableDataSources();
            
            var response = new DataSourceStatusDto
            {
                Success = true,
                Message = $"当前有 {dataSources.Count} 个可用数据源",
                DataSources = dataSources,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据源列表时发生异常");
            
            var errorResponse = new DataSourceStatusDto
            {
                Success = false,
                Message = $"获取数据源列表失败：{ex.Message}",
                DataSources = new List<DataSource>(),
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 检查所有数据源的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    [HttpGet("health")]
    public async Task<ActionResult<HealthCheckResultDto>> CheckDataSourcesHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到数据源健康检查请求");

        try
        {
            var isHealthy = await _dataCollectionService.CheckAllDataSourcesHealthAsync(cancellationToken);
            
            var response = new HealthCheckResultDto
            {
                Success = true,
                IsHealthy = isHealthy,
                Message = isHealthy ? "所有数据源状态正常" : "存在不可用的数据源",
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查数据源健康状态时发生异常");
            
            var errorResponse = new HealthCheckResultDto
            {
                Success = false,
                IsHealthy = false,
                Message = $"健康检查失败：{ex.Message}",
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }
}