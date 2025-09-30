using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Interfaces;
using POE2Finance.Core.Enums;
using POE2Finance.Services.Configuration;
using POE2Finance.Services.Infrastructure;
using System.Text;
using System.Text.Json;

namespace POE2Finance.Services.Publishing;

/// <summary>
/// B站发布服务实现
/// </summary>
public class BilibiliPublishingService : IPublishingService
{
    private readonly ILogger<BilibiliPublishingService> _logger;
    private readonly BilibiliConfiguration _config;
    private readonly ResilientHttpClient _httpClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">B站配置</param>
    /// <param name="httpClient">HTTP客户端</param>
    public BilibiliPublishingService(
        ILogger<BilibiliPublishingService> logger,
        IOptions<BilibiliConfiguration> config,
        ResilientHttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? VideoId, string? ErrorMessage)> PublishToBilibiliAsync(
        string videoPath, string title, string description, List<string> tags, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始发布视频到B站: {Title}", title);

        try
        {
            if (!File.Exists(videoPath))
            {
                var error = $"视频文件不存在: {videoPath}";
                _logger.LogError(error);
                return (false, null, error);
            }

            // 1. 预上传视频文件
            var uploadResult = await UploadVideoFileAsync(videoPath, cancellationToken);
            if (!uploadResult.Success)
            {
                return (false, null, uploadResult.ErrorMessage);
            }

            // 2. 提交视频信息
            var submitResult = await SubmitVideoAsync(uploadResult.FileKey!, title, description, tags, cancellationToken);
            if (!submitResult.Success)
            {
                return (false, null, submitResult.ErrorMessage);
            }

            _logger.LogInformation("视频发布成功，视频ID: {VideoId}", submitResult.VideoId);
            return (true, submitResult.VideoId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布视频到B站失败");
            return (false, null, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<VideoStatus> CheckPublishStatusAsync(string videoId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("检查视频发布状态: {VideoId}", videoId);

        try
        {
            var url = $"{_config.ApiBaseUrl}/video/status?bvid={videoId}";
            var headers = GetAuthHeaders();

            var response = await _httpClient.GetJsonWithRetryAsync<BilibiliStatusResponse>(url, headers, cancellationToken);
            
            if (response?.Code == 0 && response.Data != null)
            {
                return response.Data.State switch
                {
                    "open" => VideoStatus.Published,
                    "pubing" => VideoStatus.Uploading,
                    "pending" => VideoStatus.Creating,
                    _ => VideoStatus.Failed
                };
            }
            else
            {
                _logger.LogWarning("获取视频状态失败: {VideoId}, 响应: {Response}", videoId, response?.Message);
                return VideoStatus.Failed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查视频状态失败: {VideoId}", videoId);
            return VideoStatus.Failed;
        }
    }

    /// <summary>
    /// 上传视频文件
    /// </summary>
    /// <param name="videoPath">视频文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传结果</returns>
    private async Task<UploadResult> UploadVideoFileAsync(string videoPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始上传视频文件: {VideoPath}", videoPath);

        try
        {
            var fileInfo = new FileInfo(videoPath);
            var fileName = Path.GetFileName(videoPath);

            // 1. 获取上传凭证
            var preUploadResult = await GetUploadCredentialsAsync(fileName, fileInfo.Length, cancellationToken);
            if (!preUploadResult.Success)
            {
                return new UploadResult { Success = false, ErrorMessage = preUploadResult.ErrorMessage };
            }

            // 2. 分片上传视频文件
            var uploadFileResult = await UploadFileInChunksAsync(videoPath, preUploadResult.UploadUrl!, preUploadResult.UploadToken!, cancellationToken);
            if (!uploadFileResult.Success)
            {
                return uploadFileResult;
            }

            _logger.LogInformation("视频文件上传完成: {FileName}", fileName);
            return new UploadResult 
            { 
                Success = true, 
                FileKey = uploadFileResult.FileKey,
                FileName = fileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传视频文件失败");
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 获取上传凭证
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileSize">文件大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预上传结果</returns>
    private async Task<PreUploadResult> GetUploadCredentialsAsync(string fileName, long fileSize, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_config.ApiBaseUrl}/video/preupload";
            var headers = GetAuthHeaders();

            var requestData = new
            {
                name = fileName,
                size = fileSize,
                r = "upos",
                profile = "ugcupos/yb",
                ssl = 0,
                version = "2.10.4.0",
                build = 2100400
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var response = await _httpClient.GetJsonWithRetryAsync<BilibiliPreUploadResponse>(
                $"{url}?{BuildQueryString(requestData)}", headers, cancellationToken);

            if (response?.Code == 0 && response.Data != null)
            {
                return new PreUploadResult
                {
                    Success = true,
                    UploadUrl = response.Data.Endpoint,
                    UploadToken = response.Data.UposUri,
                    BizId = response.Data.BizId
                };
            }
            else
            {
                var error = $"获取上传凭证失败: {response?.Message}";
                _logger.LogError(error);
                return new PreUploadResult { Success = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上传凭证失败");
            return new PreUploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 分片上传文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="uploadUrl">上传URL</param>
    /// <param name="uploadToken">上传令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传结果</returns>
    private async Task<UploadResult> UploadFileInChunksAsync(string filePath, string uploadUrl, string uploadToken, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var chunkSize = _config.ChunkSize;
            var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

            _logger.LogInformation("开始分片上传，文件大小: {Size} bytes, 分片数: {Chunks}", fileInfo.Length, totalChunks);

            var uploadId = Guid.NewGuid().ToString("N");
            var uploadedParts = new List<UploadedPart>();

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                var buffer = new byte[chunkSize];
                var bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize, cancellationToken);
                
                if (bytesRead == 0) break;

                var chunkData = bytesRead == chunkSize ? buffer : buffer.Take(bytesRead).ToArray();
                var partNumber = chunkIndex + 1;

                var partResult = await UploadChunkAsync(uploadUrl, uploadToken, uploadId, partNumber, chunkData, cancellationToken);
                if (!partResult.Success)
                {
                    return new UploadResult { Success = false, ErrorMessage = partResult.ErrorMessage };
                }

                uploadedParts.Add(new UploadedPart { PartNumber = partNumber, ETag = partResult.ETag! });
                
                _logger.LogDebug("分片 {ChunkIndex}/{TotalChunks} 上传完成", chunkIndex + 1, totalChunks);
            }

            // 完成多分片上传
            var completeResult = await CompleteMultipartUploadAsync(uploadUrl, uploadToken, uploadId, uploadedParts, cancellationToken);
            if (!completeResult.Success)
            {
                return completeResult;
            }

            return new UploadResult 
            { 
                Success = true, 
                FileKey = completeResult.FileKey,
                FileName = Path.GetFileName(filePath)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分片上传失败");
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 上传单个分片
    /// </summary>
    /// <param name="uploadUrl">上传URL</param>
    /// <param name="uploadToken">上传令牌</param>
    /// <param name="uploadId">上传ID</param>
    /// <param name="partNumber">分片号</param>
    /// <param name="chunkData">分片数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分片上传结果</returns>
    private async Task<ChunkUploadResult> UploadChunkAsync(string uploadUrl, string uploadToken, string uploadId, int partNumber, byte[] chunkData, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{uploadUrl}?uploadId={uploadId}&partNumber={partNumber}&chunk={partNumber-1}&chunks=total";
            
            using var httpClient = new HttpClient();
            using var content = new ByteArrayContent(chunkData);
            
            content.Headers.Add("Authorization", uploadToken);
            content.Headers.Add("Content-Type", "application/octet-stream");

            var response = await httpClient.PutAsync(url, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var etag = response.Headers.ETag?.Tag?.Trim('"') ?? $"chunk-{partNumber}";
                return new ChunkUploadResult { Success = true, ETag = etag };
            }
            else
            {
                var error = $"分片上传失败: {response.StatusCode}";
                _logger.LogError(error);
                return new ChunkUploadResult { Success = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败: {PartNumber}", partNumber);
            return new ChunkUploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 完成多分片上传
    /// </summary>
    /// <param name="uploadUrl">上传URL</param>
    /// <param name="uploadToken">上传令牌</param>
    /// <param name="uploadId">上传ID</param>
    /// <param name="uploadedParts">已上传分片列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完成结果</returns>
    private async Task<UploadResult> CompleteMultipartUploadAsync(string uploadUrl, string uploadToken, string uploadId, List<UploadedPart> uploadedParts, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{uploadUrl}?uploadId={uploadId}";
            
            using var httpClient = new HttpClient();
            
            var requestBody = new
            {
                parts = uploadedParts.Select(p => new { partNumber = p.PartNumber, etag = p.ETag })
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            content.Headers.Add("Authorization", uploadToken);

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return new UploadResult { Success = true, FileKey = uploadId };
            }
            else
            {
                var error = $"完成上传失败: {response.StatusCode}";
                _logger.LogError(error);
                return new UploadResult { Success = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成多分片上传失败");
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 提交视频信息
    /// </summary>
    /// <param name="fileKey">文件标识</param>
    /// <param name="title">标题</param>
    /// <param name="description">描述</param>
    /// <param name="tags">标签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提交结果</returns>
    private async Task<SubmitResult> SubmitVideoAsync(string fileKey, string title, string description, List<string> tags, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_config.ApiBaseUrl}/video/add";
            var headers = GetAuthHeaders();

            var videoData = new
            {
                cover = "",
                title = title,
                desc = description,
                tag = string.Join(",", tags.Take(10)), // B站标签限制
                videos = new[]
                {
                    new
                    {
                        filename = fileKey,
                        title = "P1",
                        desc = ""
                    }
                },
                copyright = 1, // 原创
                source = "",
                tid = _config.CategoryId, // 游戏分区
                no_reprint = 1,
                open_elec = 1,
                max_age = 0,
                dynamic = ""
            };

            var response = await _httpClient.GetJsonWithRetryAsync<BilibiliSubmitResponse>(
                url, headers, cancellationToken);

            if (response?.Code == 0)
            {
                return new SubmitResult 
                { 
                    Success = true, 
                    VideoId = response.Data?.Bvid 
                };
            }
            else
            {
                var error = $"提交视频失败: {response?.Message}";
                _logger.LogError(error);
                return new SubmitResult { Success = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交视频信息失败");
            return new SubmitResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 获取认证请求头
    /// </summary>
    /// <returns>请求头字典</returns>
    private Dictionary<string, string> GetAuthHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            { "Cookie", _config.SessionCookie },
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" },
            { "Referer", "https://member.bilibili.com/v2" },
            { "Origin", "https://member.bilibili.com" }
        };

        if (!string.IsNullOrEmpty(_config.CsrfToken))
        {
            headers.Add("X-CSRF-TOKEN", _config.CsrfToken);
        }

        return headers;
    }

    /// <summary>
    /// 构建查询字符串
    /// </summary>
    /// <param name="parameters">参数对象</param>
    /// <returns>查询字符串</returns>
    private static string BuildQueryString(object parameters)
    {
        var properties = parameters.GetType().GetProperties();
        var queryParams = properties.Select(p => $"{p.Name}={Uri.EscapeDataString(p.GetValue(parameters)?.ToString() ?? "")}");
        return string.Join("&", queryParams);
    }
}

// 响应模型类
public class BilibiliPreUploadResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
    public PreUploadData? Data { get; set; }
}

public class PreUploadData
{
    public string Endpoint { get; set; } = "";
    public string UposUri { get; set; } = "";
    public long BizId { get; set; }
}

public class BilibiliSubmitResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
    public SubmitData? Data { get; set; }
}

public class SubmitData
{
    public string Bvid { get; set; } = "";
    public long Aid { get; set; }
}

public class BilibiliStatusResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
    public StatusData? Data { get; set; }
}

public class StatusData
{
    public string State { get; set; } = "";
    public string StateDesc { get; set; } = "";
}

// 结果类
public class PreUploadResult
{
    public bool Success { get; set; }
    public string? UploadUrl { get; set; }
    public string? UploadToken { get; set; }
    public long BizId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UploadResult
{
    public bool Success { get; set; }
    public string? FileKey { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ChunkUploadResult
{
    public bool Success { get; set; }
    public string? ETag { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SubmitResult
{
    public bool Success { get; set; }
    public string? VideoId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UploadedPart
{
    public int PartNumber { get; set; }
    public string ETag { get; set; } = "";
}