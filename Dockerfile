# 使用官方的 .NET 9 Runtime 作为基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 安装系统依赖
RUN apt-get update && apt-get install -y \
    ffmpeg \
    python3 \
    python3-pip \
    fonts-wqy-zenhei \
    fonts-wqy-microhei \
    && rm -rf /var/lib/apt/lists/*

# 安装 Edge-TTS
RUN pip3 install edge-tts

# 使用官方的 .NET 9 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 复制项目文件
COPY ["src/POE2Finance.Web/POE2Finance.Web.csproj", "src/POE2Finance.Web/"]
COPY ["src/POE2Finance.Core/POE2Finance.Core.csproj", "src/POE2Finance.Core/"]
COPY ["src/POE2Finance.Data/POE2Finance.Data.csproj", "src/POE2Finance.Data/"]
COPY ["src/POE2Finance.Services/POE2Finance.Services.csproj", "src/POE2Finance.Services/"]

# 还原依赖
RUN dotnet restore "src/POE2Finance.Web/POE2Finance.Web.csproj"

# 复制所有源代码
COPY . .

# 构建应用
WORKDIR "/src/src/POE2Finance.Web"
RUN dotnet build "POE2Finance.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 发布应用
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "POE2Finance.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 最终镜像
FROM base AS final
WORKDIR /app

# 复制发布的应用
COPY --from=publish /app/publish .

# 创建必要的目录
RUN mkdir -p /app/logs
RUN mkdir -p /app/data
RUN mkdir -p /app/temp

# 设置权限
RUN chmod +x /app/POE2Finance.Web

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "POE2Finance.Web.dll"]