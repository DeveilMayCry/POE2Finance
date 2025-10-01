# API参考文档

<cite>
**本文档中引用的文件**  
- [Program.cs](file://src/POE2Finance.Web/Program.cs)
- [appsettings.json](file://src/POE2Finance.Web/appsettings.json)
- [DataTransferObjects.cs](file://src/POE2Finance.Core/Models/DataTransferObjects.cs)
- [AutomatedAnalysisJob.cs](file://src/POE2Finance.Services/Jobs/AutomatedAnalysisJob.cs)
- [AntiBanHttpClient.cs](file://src/POE2Finance.Services/Infrastructure/AntiBanHttpClient.cs)
- [CommonEnums.cs](file://src/POE2Finance.Core/Enums/CommonEnums.cs)
- [IntegrationTests.cs](file://tests/POE2Finance.Tests/Integration/IntegrationTests.cs)
</cite>

## 目录
1. [简介](#简介)
2. [API端点概览](#api端点概览)
3. [健康检查端点](#健康检查端点)
4. [数据查询端点](#数据查询端点)
5. [系统状态获取端点](#系统状态获取端点)
6. [手动任务触发端点](#手动任务触发端点)
7. [请求与响应格式](#请求与响应格式)
8. [安全机制](#安全机制)
9. [API设计原则](#api设计原则)
10. [错误处理模式](#错误处理模式)
11. [常见问题排查](#常见问题排查)
12. [调用示例](#调用示例)

## 简介
POE2Finance是一个自动化分析《流放之路2》游戏内经济系统的RESTful API服务。该API提供市场数据分析、价格趋势预测和视频内容生成等功能。系统通过定时任务自动执行完整的分析流程，同时也提供手动触发和状态查询接口。本参考文档详细描述了所有可用的API端点、请求响应格式、安全机制和使用示例。

## API端点概览
POE2Finance API提供以下主要功能类别：
- 健康检查：验证系统运行状态
- 数据查询：获取历史价格数据和分析结果
- 系统状态：监控服务运行状况和任务执行情况
- 任务控制：手动触发数据采集和分析任务

所有API端点均返回JSON格式响应，遵循RESTful设计原则，使用标准HTTP状态码表示请求结果。

**Section sources**
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L1-L145)

## 健康检查端点
### GET /health
检查API服务的健康状态。

**请求参数**
- 无

**响应格式**
```json
{
  "status": "string",
  "timestamp": "datetime",
  "version": "string"
}
```

**可能的HTTP状态码**
- 200 OK：服务正常运行
- 503 Service Unavailable：服务不可用

此端点用于监控服务的可用性，集成测试中已验证其功能。

**Section sources**
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L135-L145)
- [IntegrationTests.cs](file://tests/POE2Finance.Tests/Integration/IntegrationTests.cs#L210-L218)

## 数据查询端点
### GET /api/prices/latest
获取最新的通货价格数据。

**请求参数**
- 无

**响应格式**
包含`PriceDataDto`对象数组，每个对象包含：
- `currencyType`：通货类型（ExaltedOrb, DivineOrb, ChaosOrb）
- `currencyName`：通货名称
- `currentPriceInExalted`：当前价格（以崇高石计价）
- `previousPriceInExalted`：前一期价格
- `changePercent`：价格变动百分比
- `tradeVolume`：交易量
- `dataSource`：数据来源
- `collectedAt`：采集时间

**可能的HTTP状态码**
- 200 OK：成功返回数据
- 500 Internal Server Error：数据查询失败

**Section sources**
- [DataTransferObjects.cs](file://src/POE2Finance.Core/Models/DataTransferObjects.cs#L5-L50)
- [CommonEnums.cs](file://src/POE2Finance.Core/Enums/CommonEnums.cs#L5-L25)

### GET /api/analysis/reports
获取历史分析报告列表。

**请求参数**
- `date`（可选）：指定日期（YYYY-MM-DD格式）
- `timeSlot`（可选）：时间段（Morning, Afternoon, Evening）

**响应格式**
包含`MarketAnalysisResultDto`对象数组，每个对象包含：
- `analysisTime`：分析时间
- `timeSlot`：时间段
- `hotItems`：热点物品列表
- `overallTrend`：市场整体趋势
- `marketDynamics`：市场动态描述
- `tradingAdvice`：交易建议
- `riskWarning`：风险提示

**可能的HTTP状态码**
- 200 OK：成功返回报告列表
- 400 Bad Request：参数格式错误
- 500 Internal Server Error：数据库查询失败

**Section sources**
- [DataTransferObjects.cs](file://src/POE2Finance.Core/Models/DataTransferObjects.cs#L95-L130)
- [CommonEnums.cs](file://src/POE2Finance.Core/Enums/CommonEnums.cs#L75-L95)

## 系统状态获取端点
### GET /api/status/data-sources
获取所有数据源的健康状态。

**请求参数**
- 无

**响应格式**
```json
{
  "dataSourceStatus": {
    "TencentOfficial": true,
    "DD373": true,
    "QiandaoGaming": false,
    "WeGame": true
  },
  "lastChecked": "datetime"
}
```

**可能的HTTP状态码**
- 200 OK：成功返回状态
- 500 Internal Server Error：健康检查失败

该端点通过`DataCollectionService`检查所有配置的数据源的可用性。

**Section sources**
- [AutomatedAnalysisJob.cs](file://src/POE2Finance.Services/Jobs/AutomatedAnalysisJob.cs#L223-L240)
- [CommonEnums.cs](file://src/POE2Finance.Core/Enums/CommonEnums.cs#L45-L55)

### GET /api/status/system
获取系统资源使用情况。

**请求参数**
- 无

**响应格式**
包含以下系统指标：
- 磁盘空间使用情况
- 内存使用情况
- CPU使用率
- 服务运行时长
- 当前任务队列状态

**可能的HTTP状态码**
- 200 OK：成功返回系统状态
- 500 Internal Server Error：系统信息获取失败

**Section sources**
- [MaintenanceJobs.cs](file://src/POE2Finance.Services/Jobs/MaintenanceJobs.cs#L335-L387)

## 手动任务触发端点
### POST /api/jobs/trigger-data-collection
手动触发数据采集任务。

**请求参数**
- 无

**请求体**
- 无

**响应格式**
```json
{
  "jobId": "string",
  "jobName": "DataCollectionJob",
  "status": "Scheduled",
  "scheduledTime": "datetime"
}
```

**可能的HTTP状态码**
- 202 Accepted：任务已接受并安排执行
- 500 Internal Server Error：任务调度失败

此端点对应Quartz定时任务中的数据采集任务。

**Section sources**
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L105-L110)
- [AutomatedAnalysisJob.cs](file://src/POE2Finance.Services/Jobs/AutomatedAnalysisJob.cs#L223-L240)

### POST /api/jobs/trigger-analysis/{timeSlot}
手动触发指定时间段的市场分析任务。

**路径参数**
- `timeSlot`：时间段（Morning, Afternoon, Evening）

**请求参数**
- 无

**请求体**
- 无

**响应格式**
```json
{
  "jobId": "string",
  "timeSlot": "Morning",
  "status": "Executing",
  "startTime": "datetime"
}
```

**可能的HTTP状态码**
- 202 Accepted：分析任务已启动
- 400 Bad Request：时间段参数无效
- 500 Internal Server Error：任务执行失败

此端点触发完整的自动化分析流程，包括数据采集、价格分析、图表生成、视频制作和发布。

**Section sources**
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L75-L100)
- [AutomatedAnalysisJob.cs](file://src/POE2Finance.Services/Jobs/AutomatedAnalysisJob.cs#L1-L350)

## 请求与响应格式
### 请求格式
所有API请求使用标准HTTP方法和URL路径。GET请求的查询参数使用标准URL编码，POST请求的请求体使用application/json格式。

### 响应格式
所有API响应遵循统一的JSON格式：
```json
{
  "success": true,
  "data": {},
  "message": "string",
  "timestamp": "datetime"
}
```

当请求失败时，响应包含错误信息：
```json
{
  "success": false,
  "data": null,
  "message": "错误描述",
  "errorCode": "string",
  "timestamp": "datetime"
}
```

**Section sources**
- [DataTransferObjects.cs](file://src/POE2Finance.Core/Models/DataTransferObjects.cs#L5-L176)
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L1-L145)

## 安全机制
### 认证与授权
当前版本的API未实现用户认证机制，所有端点均可匿名访问。这适用于内部网络环境下的使用场景。

### 安全考虑
- **防Ban机制**：数据采集服务使用`AntiBanHttpClient`实现请求频率控制和反爬虫规避。
- **请求限制**：客户端实现请求间隔控制，避免对目标网站造成过大压力。
- **配置安全**：敏感配置（如B站发布凭证）通过配置文件管理，不应硬编码在代码中。

未来版本计划添加API密钥认证机制以增强安全性。

**Section sources**
- [AntiBanHttpClient.cs](file://src/POE2Finance.Services/Infrastructure/AntiBanHttpClient.cs#L0-L272)
- [appsettings.json](file://src/POE2Finance.Web/appsettings.json#L1-L130)

## API设计原则
### 资源命名规范
API遵循RESTful设计原则，使用名词复数形式表示资源集合：
- `/api/prices`：价格数据资源
- `/api/analysis`：分析结果资源
- `/api/jobs`：任务控制资源
- `/api/status`：系统状态资源

### 版本控制策略
当前API未实现版本控制，所有端点位于根路径下。建议在生产环境中实现版本控制，例如使用`/api/v1/`前缀。

### 错误处理一致性
所有API端点返回统一的响应格式，包含`success`标志位、`data`数据体、`message`消息和`timestamp`时间戳，便于客户端统一处理。

**Section sources**
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L1-L145)
- [DataTransferObjects.cs](file://src/POE2Finance.Core/Models/DataTransferObjects.cs#L5-L176)

## 错误处理模式
### HTTP状态码使用
- 200 OK：请求成功，返回预期数据
- 202 Accepted：请求已接受，正在处理中
- 400 Bad Request：客户端请求参数错误
- 404 Not Found：请求的资源不存在
- 500 Internal Server Error：服务器内部错误
- 503 Service Unavailable：服务暂时不可用

### 错误响应结构
所有错误响应包含详细的错误信息，帮助客户端开发者快速定位问题。对于任务执行类端点，即使返回202状态码，后续任务执行仍可能失败，需要通过状态查询端点获取最终结果。

**Section sources**
- [AutomatedAnalysisJob.cs](file://src/POE2Finance.Services/Jobs/AutomatedAnalysisJob.cs#L1-L350)
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L135-L145)

## 常见问题排查
### 数据采集失败
**症状**：`/api/prices/latest`返回空数据或旧数据
**可能原因**：
- 数据源网站不可访问或结构变更
- 网络连接问题
- 请求频率过高被目标网站封禁

**解决方案**：
1. 检查`/api/status/data-sources`确认数据源健康状态
2. 查看日志文件中的详细错误信息
3. 调整`appsettings.json`中的采集间隔和重试配置

### 分析任务执行缓慢
**症状**：手动触发分析任务后长时间无响应
**可能原因**：
- 系统资源不足（CPU、内存、磁盘）
- 数据源响应缓慢
- 视频生成过程耗时较长

**解决方案**：
1. 检查系统资源使用情况
2. 优化`appsettings.json`中的图表和视频配置
3. 确保有足够的磁盘空间用于临时文件存储

### B站发布失败
**症状**：分析任务完成但视频未发布到B站
**可能原因**：
- B站API凭证过期或无效
- 网络连接问题
- 视频文件上传超时

**解决方案**：
1. 更新`appsettings.json`中的B站会话Cookie和CSRF令牌
2. 检查网络连接稳定性
3. 增加上传超时时间配置

**Section sources**
- [AutomatedAnalysisJob.cs](file://src/POE2Finance.Services/Jobs/AutomatedAnalysisJob.cs#L1-L350)
- [appsettings.json](file://src/POE2Finance.Web/appsettings.json#L1-L130)

## 调用示例
### curl命令示例
**获取健康状态**
```bash
curl -X GET "http://localhost:5000/health" -H "accept: application/json"
```

**获取最新价格数据**
```bash
curl -X GET "http://localhost:5000/api/prices/latest" -H "accept: application/json"
```

**手动触发上午场分析任务**
```bash
curl -X POST "http://localhost:5000/api/jobs/trigger-analysis/Morning" -H "accept: */*"
```

### C#客户端代码示例
```csharp
using System.Net.Http.Json;

var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5000/");

// 获取健康状态
var healthResponse = await client.GetAsync("/health");
if (healthResponse.IsSuccessStatusCode)
{
    var healthData = await healthResponse.Content.ReadFromJsonAsync<HealthStatus>();
    Console.WriteLine($"服务状态: {healthData.Status}");
}

// 获取最新价格数据
var pricesResponse = await client.GetAsync("/api/prices/latest");
if (pricesResponse.IsSuccessStatusCode)
{
    var prices = await pricesResponse.Content.ReadFromJsonAsync<List<PriceDataDto>>();
    foreach (var price in prices)
    {
        Console.WriteLine($"{price.CurrencyName}: {price.CurrentPriceInExalted} 崇高石");
    }
}

// 手动触发分析任务
var analysisResponse = await client.PostAsync("/api/jobs/trigger-analysis/Evening", null);
if (analysisResponse.IsSuccessStatusCode)
{
    Console.WriteLine("晚间场分析任务已启动");
}
```

**Section sources**
- [Program.cs](file://src/POE2Finance.Web/Program.cs#L1-L145)
- [DataTransferObjects.cs](file://src/POE2Finance.Core/Models/DataTransferObjects.cs#L5-L176)
- [IntegrationTests.cs](file://tests/POE2Finance.Tests/Integration/IntegrationTests.cs#L210-L218)