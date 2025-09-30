using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POE2Finance.Core.Enums;

namespace POE2Finance.Core.Entities;

/// <summary>
/// 视频记录实体，存储视频制作和发布信息
/// </summary>
public class VideoRecord : BaseEntity
{
    /// <summary>
    /// 关联的分析报告ID
    /// </summary>
    public int AnalysisReportId { get; set; }

    /// <summary>
    /// 视频标题
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 视频描述
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// 视频标签（以逗号分隔）
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 本地视频文件路径
    /// </summary>
    [MaxLength(500)]
    public string? LocalFilePath { get; set; }

    /// <summary>
    /// 视频时长（秒）
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// 视频文件大小（字节）
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// 视频状态
    /// </summary>
    public VideoStatus Status { get; set; } = VideoStatus.Creating;

    /// <summary>
    /// 发布时间段
    /// </summary>
    public PublishTimeSlot TimeSlot { get; set; }

    /// <summary>
    /// B站视频AV号
    /// </summary>
    [MaxLength(50)]
    public string? BilibiliAvId { get; set; }

    /// <summary>
    /// B站视频BV号
    /// </summary>
    [MaxLength(50)]
    public string? BilibiliBvId { get; set; }

    /// <summary>
    /// B站发布URL
    /// </summary>
    [MaxLength(500)]
    public string? BilibiliUrl { get; set; }

    /// <summary>
    /// 预计发布时间
    /// </summary>
    public DateTime ScheduledPublishTime { get; set; }

    /// <summary>
    /// 实际发布时间
    /// </summary>
    public DateTime? ActualPublishTime { get; set; }

    /// <summary>
    /// 制作时长（毫秒）
    /// </summary>
    public long? CreationTimeMs { get; set; }

    /// <summary>
    /// 上传时长（毫秒）
    /// </summary>
    public long? UploadTimeMs { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 关联的分析报告
    /// </summary>
    [ForeignKey(nameof(AnalysisReportId))]
    public virtual AnalysisReport? AnalysisReport { get; set; }
}