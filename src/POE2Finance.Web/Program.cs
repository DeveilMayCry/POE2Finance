using POE2Finance.Data.Extensions;
using POE2Finance.Services.Configuration;
using POE2Finance.Services.Jobs;
using POE2Finance.Core.Interfaces;
using POE2Finance.Services.DataCollection;
using POE2Finance.Services.Analysis;
using POE2Finance.Services.Charts;
using POE2Finance.Services.AI;
using POE2Finance.Services.Video;
using POE2Finance.Services.Publishing;
using POE2Finance.Services.Infrastructure;
using POE2Finance.Services.DataCollection.Collectors;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog日志
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 配置选项
builder.Services.Configure<DataCollectionConfiguration>(
    builder.Configuration.GetSection(DataCollectionConfiguration.SectionName));
builder.Services.Configure<AnalysisConfiguration>(
    builder.Configuration.GetSection(AnalysisConfiguration.SectionName));
builder.Services.Configure<ChartConfiguration>(
    builder.Configuration.GetSection(ChartConfiguration.SectionName));
builder.Services.Configure<ContentGenerationConfiguration>(
    builder.Configuration.GetSection(ContentGenerationConfiguration.SectionName));
builder.Services.Configure<EdgeTtsConfiguration>(
    builder.Configuration.GetSection(EdgeTtsConfiguration.SectionName));
builder.Services.Configure<VideoConfiguration>(
    builder.Configuration.GetSection(VideoConfiguration.SectionName));
builder.Services.Configure<BilibiliConfiguration>(
    builder.Configuration.GetSection(BilibiliConfiguration.SectionName));

// 添加数据层服务
builder.Services.AddDataServices(builder.Configuration);

// 添加HTTP客户端
builder.Services.AddHttpClient<AntiBanHttpClient>();
builder.Services.AddScoped<ResilientHttpClient>();

// 添加数据采集器
builder.Services.AddScoped<IDataCollector, TencentOfficialCollector>();
builder.Services.AddScoped<IDataCollector, DD373Collector>();

// 添加核心服务
builder.Services.AddScoped<IDataCollectionService, DataCollectionService>();
builder.Services.AddScoped<IPriceAnalysisService, PriceAnalysisService>();
builder.Services.AddScoped<IChartGenerationService, ChartGenerationService>();
builder.Services.AddScoped<IContentGenerationService, ContentGenerationService>();
builder.Services.AddScoped<ITextToSpeechService, EdgeTtsService>();
builder.Services.AddScoped<IVideoCreationService, VideoCreationService>();
builder.Services.AddScoped<IPublishingService, BilibiliPublishingService>();

// 添加定时任务服务
builder.Services.AddQuartz(q =>
{
    // 在新版本的Quartz中，DI集成已经默认启用，不再需要显式调用
    // q.UseMicrosoftDependencyInjection(); // 这行在新版本中已经不需要了
    
    // 上午场任务 - 每天09:00
    var morningJobKey = new JobKey("MorningAnalysisJob");
    q.AddJob<AutomatedAnalysisJob>(opts => opts.WithIdentity(morningJobKey));
    q.AddTrigger(opts => opts
        .ForJob(morningJobKey)
        .WithIdentity("MorningAnalysisTrigger")
        .WithCronSchedule("0 0 9 * * ?")
        .UsingJobData("TimeSlot", "Morning"));
    
    // 下午场任务 - 每天15:00
    var afternoonJobKey = new JobKey("AfternoonAnalysisJob");
    q.AddJob<AutomatedAnalysisJob>(opts => opts.WithIdentity(afternoonJobKey));
    q.AddTrigger(opts => opts
        .ForJob(afternoonJobKey)
        .WithIdentity("AfternoonAnalysisTrigger")
        .WithCronSchedule("0 0 15 * * ?")
        .UsingJobData("TimeSlot", "Afternoon"));
    
    // 晚间场任务 - 每天21:00
    var eveningJobKey = new JobKey("EveningAnalysisJob");
    q.AddJob<AutomatedAnalysisJob>(opts => opts.WithIdentity(eveningJobKey));
    q.AddTrigger(opts => opts
        .ForJob(eveningJobKey)
        .WithIdentity("EveningAnalysisTrigger")
        .WithCronSchedule("0 0 21 * * ?")
        .UsingJobData("TimeSlot", "Evening"));
    
    // 数据采集任务 - 每小时一次（仅08:00-23:00）
    var collectionJobKey = new JobKey("DataCollectionJob");
    q.AddJob<DataCollectionJob>(opts => opts.WithIdentity(collectionJobKey));
    q.AddTrigger(opts => opts
        .ForJob(collectionJobKey)
        .WithIdentity("DataCollectionTrigger")
        .WithCronSchedule("0 0 8-23 * * ?"));
    
    // 清理任务 - 每天凌晨02:00
    var cleanupJobKey = new JobKey("CleanupJob");
    q.AddJob<CleanupJob>(opts => opts.WithIdentity(cleanupJobKey));
    q.AddTrigger(opts => opts
        .ForJob(cleanupJobKey)
        .WithIdentity("CleanupTrigger")
        .WithCronSchedule("0 0 2 * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// 确保数据库创建
await app.Services.EnsureDatabaseCreatedAsync();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("POE2Finance应用程序启动");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
}
finally
{
    Log.CloseAndFlush();
}

// 使Program类对测试项目可见
public partial class Program { }