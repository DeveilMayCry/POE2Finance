# POE2Finance 部署指南

## 系统要求

### 最低配置
- **CPU**: 2核心
- **内存**: 4GB RAM
- **存储**: 20GB可用空间
- **操作系统**: Linux Ubuntu 22.04+ 或 CentOS 8+

### 推荐配置
- **CPU**: 4核心
- **内存**: 8GB RAM
- **存储**: 50GB SSD
- **带宽**: 10Mbps

## 腾讯云轻量级服务器部署

### 1. 服务器准备

```bash
# 更新系统
sudo apt update && sudo apt upgrade -y

# 安装必要的软件
sudo apt install -y docker.io docker-compose git curl

# 启动Docker服务
sudo systemctl start docker
sudo systemctl enable docker

# 添加用户到docker组
sudo usermod -aG docker $USER
```

### 2. 获取代码

```bash
# 克隆项目
git clone <your-repository-url> POE2Finance
cd POE2Finance

# 或者通过FTP上传项目文件
```

### 3. 配置应用

编辑配置文件 `src/POE2Finance.Web/appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/app/data/poe2finance.db"
  },
  "DataCollection": {
    "CollectionIntervalHours": 1,
    "MinRequestIntervalSeconds": 3600
  },
  "Bilibili": {
    "Enabled": true,
    "SessionCookie": "your-bilibili-session-cookie",
    "CsrfToken": "your-csrf-token"
  }
}
```

### 4. 构建和启动

```bash
# 构建Docker镜像
docker-compose build

# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f poe2finance
```

### 5. 验证部署

```bash
# 检查服务状态
docker-compose ps

# 检查健康状态
curl http://localhost:8080/health

# 访问Swagger文档
curl http://localhost:8080/swagger
```

## 环境变量配置

创建 `.env` 文件：

```bash
# 应用配置
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# 数据库配置
DB_CONNECTION_STRING=Data Source=/app/data/poe2finance.db

# B站配置（需要实际获取）
BILIBILI_SESSION_COOKIE=your_session_cookie_here
BILIBILI_CSRF_TOKEN=your_csrf_token_here

# 日志级别
LOG_LEVEL=Information
```

## 数据备份策略

### 自动备份脚本

创建 `backup.sh`:

```bash
#!/bin/bash
BACKUP_DIR="/backup/poe2finance"
DATE=$(date +%Y%m%d_%H%M%S)

# 创建备份目录
mkdir -p $BACKUP_DIR

# 备份数据库
cp /data/poe2finance.db $BACKUP_DIR/poe2finance_$DATE.db

# 备份日志（最近7天）
tar -czf $BACKUP_DIR/logs_$DATE.tar.gz /logs

# 清理7天前的备份
find $BACKUP_DIR -name "*.db" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "备份完成: $DATE"
```

添加到定时任务:

```bash
# 编辑crontab
crontab -e

# 添加每日备份任务（凌晨3点）
0 3 * * * /path/to/backup.sh >> /var/log/backup.log 2>&1
```

## 监控和维护

### 1. 系统监控

```bash
# 检查Docker容器状态
docker stats poe2finance-app

# 检查系统资源使用
htop
df -h
free -h
```

### 2. 日志监控

```bash
# 实时查看应用日志
docker-compose logs -f poe2finance

# 查看错误日志
docker-compose logs poe2finance | grep ERROR

# 查看特定时间的日志
docker-compose logs --since="2024-01-01T00:00:00" poe2finance
```

### 3. 性能优化

#### 数据库优化
```bash
# 进入容器
docker exec -it poe2finance-app bash

# 检查数据库大小
ls -lh /app/data/poe2finance.db

# 清理过期数据（应用会自动执行）
```

#### 内存优化
```bash
# 限制容器内存使用
docker-compose.yml 中添加：
deploy:
  resources:
    limits:
      memory: 2G
```

## 故障排除

### 常见问题

1. **应用无法启动**
   ```bash
   # 检查容器日志
   docker-compose logs poe2finance
   
   # 检查端口占用
   netstat -tulpn | grep 8080
   ```

2. **数据采集失败**
   ```bash
   # 检查网络连接
   docker exec poe2finance-app curl -I https://poe2.qq.com
   
   # 检查配置
   docker exec poe2finance-app cat /app/appsettings.json
   ```

3. **视频生成失败**
   ```bash
   # 检查FFmpeg
   docker exec poe2finance-app ffmpeg -version
   
   # 检查Edge-TTS
   docker exec poe2finance-app edge-tts --help
   ```

4. **B站发布失败**
   ```bash
   # 检查B站配置
   # 确保SessionCookie和CsrfToken正确
   # 检查网络连接到B站API
   ```

### 重启服务

```bash
# 重启特定服务
docker-compose restart poe2finance

# 完全重建服务
docker-compose down
docker-compose up -d --build
```

## 更新部署

```bash
# 1. 停止服务
docker-compose down

# 2. 备份数据
cp -r data data_backup_$(date +%Y%m%d)

# 3. 更新代码
git pull origin main

# 4. 重新构建和启动
docker-compose build --no-cache
docker-compose up -d

# 5. 验证更新
docker-compose logs -f poe2finance
```

## 安全建议

1. **防火墙配置**
   ```bash
   # 只开放必要端口
   sudo ufw allow 80
   sudo ufw allow 443
   sudo ufw allow 22
   sudo ufw enable
   ```

2. **SSL证书**
   ```bash
   # 使用Let's Encrypt获取免费证书
   sudo apt install certbot
   sudo certbot certonly --standalone -d your-domain.com
   ```

3. **定期更新**
   ```bash
   # 定期更新系统和Docker镜像
   sudo apt update && sudo apt upgrade -y
   docker-compose pull
   ```

## 成本控制

### 腾讯云轻量级服务器优化

1. **选择合适的配置**
   - 2核4GB：适合测试和轻量使用
   - 4核8GB：适合生产环境

2. **流量优化**
   - 数据采集错峰进行
   - 压缩日志文件
   - 定期清理临时文件

3. **存储优化**
   - 定期清理过期数据
   - 压缩备份文件
   - 使用轻量级基础镜像

## 支持和维护

如有问题，请检查：
1. 应用日志: `/logs/poe2finance-*.log`
2. Docker日志: `docker-compose logs`
3. 系统日志: `/var/log/syslog`

联系方式：[your-contact-info]