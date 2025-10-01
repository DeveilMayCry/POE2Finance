using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POE2Finance.Core.Interfaces;
using POE2Finance.Services.Configuration;
using System.Diagnostics;
using System.Text;

namespace POE2Finance.Services.AI;

/// <summary>
/// Edge-TTS语音合成服务实现
/// </summary>
public class EdgeTtsService : ITextToSpeechService
{
    private readonly ILogger<EdgeTtsService> _logger;
    private readonly EdgeTtsConfiguration _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">Edge-TTS配置</param>
    public EdgeTtsService(ILogger<EdgeTtsService> logger, IOptions<EdgeTtsConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAudioAsync(string text, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成语音音频，文本长度: {Length} 字符", text.Length);

        try
        {
            // 确保输出目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 预处理文本
            var processedText = PreprocessText(text);
            
            // 创建临时文件保存文本
            var tempTextFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempTextFile, processedText, Encoding.UTF8, cancellationToken);

            try
            {
                // 调用Edge-TTS命令行工具
                var success = await ExecuteEdgeTtsAsync(tempTextFile, outputPath, cancellationToken);
                
                if (!success)
                {
                    throw new InvalidOperationException("Edge-TTS语音合成失败");
                }

                // 验证输出文件
                if (!File.Exists(outputPath))
                {
                    throw new FileNotFoundException($"语音文件生成失败: {outputPath}");
                }

                var fileInfo = new FileInfo(outputPath);
                _logger.LogInformation("语音文件生成成功: {OutputPath}, 大小: {Size} KB", 
                    outputPath, fileInfo.Length / 1024);

                return outputPath;
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempTextFile))
                {
                    File.Delete(tempTextFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成语音音频失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync()
    {
        return await IsEdgeTtsAvailableAsync();
    }

    /// <summary>
    /// 执行Edge-TTS命令
    /// </summary>
    /// <param name="textFilePath">文本文件路径</param>
    /// <param name="outputPath">输出音频路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    private async Task<bool> ExecuteEdgeTtsAsync(string textFilePath, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            var arguments = BuildEdgeTtsArguments(textFilePath, outputPath);
            _logger.LogDebug("执行Edge-TTS命令: {Command} {Arguments}", _config.EdgeTtsCommand, arguments);

            var processInfo = new ProcessStartInfo
            {
                FileName = _config.EdgeTtsCommand,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            };

            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug("Edge-TTS输出: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogWarning("Edge-TTS错误: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var exitCode = process.ExitCode;
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (exitCode == 0)
            {
                _logger.LogInformation("Edge-TTS执行成功");
                return true;
            }
            else
            {
                _logger.LogError("Edge-TTS执行失败，退出代码: {ExitCode}, 错误信息: {Error}", exitCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Edge-TTS命令时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 构建Edge-TTS命令参数
    /// </summary>
    /// <param name="textFilePath">文本文件路径</param>
    /// <param name="outputPath">输出路径</param>
    /// <returns>命令参数</returns>
    private string BuildEdgeTtsArguments(string textFilePath, string outputPath)
    {
        var args = new List<string>
        {
            "--file", $"\"{textFilePath}\"",
            "--write-media", $"\"{outputPath}\"",
            "--voice", _config.VoiceName
        };

        if (!string.IsNullOrEmpty(_config.Rate))
        {
            args.AddRange(new[] { "--rate", _config.Rate });
        }

        if (!string.IsNullOrEmpty(_config.Volume))
        {
            args.AddRange(new[] { "--volume", _config.Volume });
        }

        if (!string.IsNullOrEmpty(_config.Pitch))
        {
            args.AddRange(new[] { "--pitch", _config.Pitch });
        }

        return string.Join(" ", args);
    }

    /// <summary>
    /// 预处理文本内容
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>处理后的文本</returns>
    private string PreprocessText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var processed = text;

        // 处理特殊字符和符号
        var replacements = new Dictionary<string, string>
        {
            { "【", "" },
            { "】", "，" },
            { "（", "，" },
            { "）", "，" },
            { "、", "，" },
            { "；", "，" },
            { "：", "，" },
            { "！", "。" },
            { "？", "。" },
            { "…", "。" },
            { "——", "，" },
            { "－", "到" },
            { "%", "百分之" },
            { "POE2", "P O E 2" },
            { "API", "A P I" },
            { "WeGame", "We Game" }
        };

        foreach (var replacement in replacements)
        {
            processed = processed.Replace(replacement.Key, replacement.Value);
        }

        // 处理数字读音
        processed = ProcessNumbers(processed);

        // 处理通货名称
        processed = ProcessCurrencyNames(processed);

        // 清理多余的标点符号
        processed = CleanupPunctuation(processed);

        // 添加适当的停顿
        processed = AddPauses(processed);

        _logger.LogDebug("文本预处理完成，原长度: {OriginalLength}，处理后长度: {ProcessedLength}", 
            text.Length, processed.Length);

        return processed;
    }

    /// <summary>
    /// 处理数字读音
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>处理后的文本</returns>
    private static string ProcessNumbers(string text)
    {
        // 处理小数点
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(\d+)\.(\d+)", "$1点$2");
        
        // 处理百分比
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(\d+(?:\.\d+)?)%", "$1百分之");
        
        return text;
    }

    /// <summary>
    /// 处理通货名称
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>处理后的文本</returns>
    private static string ProcessCurrencyNames(string text)
    {
        var currencyReplacements = new Dictionary<string, string>
        {
            { "崇高石", "崇高石" },
            { "神圣石", "神圣石" },
            { "混沌石", "混沌石" },
            { "ExaltedOrb", "崇高石" },
            { "DivineOrb", "神圣石" },
            { "ChaosOrb", "混沌石" }
        };

        foreach (var replacement in currencyReplacements)
        {
            text = text.Replace(replacement.Key, replacement.Value);
        }

        return text;
    }

    /// <summary>
    /// 清理标点符号
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>处理后的文本</returns>
    private static string CleanupPunctuation(string text)
    {
        // 清理连续的逗号
        text = System.Text.RegularExpressions.Regex.Replace(text, @"，+", "，");
        
        // 清理连续的句号
        text = System.Text.RegularExpressions.Regex.Replace(text, @"。+", "。");
        
        // 清理行首和行尾的标点符号
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^[，。\s]+|[，。\s]+$", "", 
            System.Text.RegularExpressions.RegexOptions.Multiline);
        
        return text;
    }

    /// <summary>
    /// 添加适当的停顿
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>处理后的文本</returns>
    private static string AddPauses(string text)
    {
        // 在重要信息后添加停顿
        var pausePatterns = new Dictionary<string, string>
        {
            { "热度评分", "热度评分，" },
            { "价格波动", "价格波动，" },
            { "交易建议", "交易建议，" },
            { "风险提示", "风险提示，" },
            { "市场动态", "市场动态，" }
        };

        foreach (var pattern in pausePatterns)
        {
            text = text.Replace(pattern.Key, pattern.Value);
        }

        return text;
    }

    /// <summary>
    /// 检查Edge-TTS是否可用
    /// </summary>
    /// <returns>是否可用</returns>
    public async Task<bool> IsEdgeTtsAvailableAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _config.EdgeTtsCommand,
                Arguments = "--help",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查Edge-TTS可用性失败");
            return false;
        }
    }

    /// <summary>
    /// 获取可用的语音列表
    /// </summary>
    /// <returns>语音列表</returns>
    public async Task<List<string>> GetAvailableVoicesAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _config.EdgeTtsCommand,
                Arguments = "--list-voices",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            var output = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            var voices = new List<string>();
            var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                // 解析语音信息
                if (line.Contains("zh-CN") && line.Contains("Female") || line.Contains("Male"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 0)
                    {
                        voices.Add(parts[0].Trim());
                    }
                }
            }

            return voices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用语音列表失败");
            return new List<string>();
        }
    }
}