namespace POE2Finance.Core.Enums;

/// <summary>
/// 通货类型枚举，对应POE2中的主要通货
/// </summary>
public enum CurrencyType
{
    /// <summary>
    /// 崇高石 (Exalted Orb) - 基准计价单位
    /// </summary>
    ExaltedOrb = 1,
    
    /// <summary>
    /// 神圣石 (Divine Orb)
    /// </summary>
    DivineOrb = 2,
    
    /// <summary>
    /// 混沌石 (Chaos Orb)
    /// </summary>
    ChaosOrb = 3
}

/// <summary>
/// 数据源类型
/// </summary>
public enum DataSource
{
    /// <summary>
    /// 腾讯官方POE2交易平台
    /// </summary>
    TencentOfficial = 1,
    
    /// <summary>
    /// DD373国服区
    /// </summary>
    DD373 = 2,
    
    /// <summary>
    /// 千岛电玩国服
    /// </summary>
    QiandaoGaming = 3,
    
    /// <summary>
    /// WeGame市场数据
    /// </summary>
    WeGame = 4
}

/// <summary>
/// 价格趋势类型
/// </summary>
public enum TrendType
{
    /// <summary>
    /// 强势上涨
    /// </summary>
    StrongUptrend = 1,
    
    /// <summary>
    /// 温和上涨
    /// </summary>
    ModerateUptrend = 2,
    
    /// <summary>
    /// 横盘整理
    /// </summary>
    Sideways = 3,
    
    /// <summary>
    /// 温和下跌
    /// </summary>
    ModerateDowntrend = 4,
    
    /// <summary>
    /// 强势下跌
    /// </summary>
    StrongDowntrend = 5
}

/// <summary>
/// 分析报告状态
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// 生成中
    /// </summary>
    Generating = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// 已发布
    /// </summary>
    Published = 3,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed = 4
}

/// <summary>
/// 视频状态
/// </summary>
public enum VideoStatus
{
    /// <summary>
    /// 制作中
    /// </summary>
    Creating = 1,
    
    /// <summary>
    /// 制作完成
    /// </summary>
    Created = 2,
    
    /// <summary>
    /// 上传中
    /// </summary>
    Uploading = 3,
    
    /// <summary>
    /// 已发布
    /// </summary>
    Published = 4,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed = 5
}

/// <summary>
/// 发布时间段
/// </summary>
public enum PublishTimeSlot
{
    /// <summary>
    /// 上午场 09:00
    /// </summary>
    Morning = 1,
    
    /// <summary>
    /// 下午场 15:00
    /// </summary>
    Afternoon = 2,
    
    /// <summary>
    /// 晚间场 21:00
    /// </summary>
    Evening = 3
}