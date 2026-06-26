# AI 客服 SaaS — 基于 .NET 10 + uni-app + 通义千问 + pgvector

> 微信生态 AI 客服系统：多租户、RAG 检索增强、人工接管、向量知识库、后台管理、跨端工作台

## 技术栈

| 层 | 技术 |
| --- | --- |
| 后端 | .NET 10 + ASP.NET Core Minimal Hosting + Clean Architecture |
| ORM | EF Core 10 + Npgsql + pgvector 0.3 |
| 数据库 | PostgreSQL 16 (含 pgvector) + Redis 7 |
| AI | 通义千问 qwen-plus + text-embedding-v3 |
| 后台任务 | Hangfire + PostgreSQL 存储 |
| 认证 | JWT (Access + Refresh) |
| 文档解析 | PdfPig + DocumentFormat.OpenXml |
| 后台前端 | Vue 3 + Vite + Element Plus + Pinia |
| 客服端 | uni-app (H5 + 微信小程序 + APP) |

## 目录结构

```
AiCustomerService/
├── backend/                          # .NET 后端
│   ├── src/
│   │   ├── AiCustomerService.Core/          # 领域实体、接口、DTO
│   │   ├── AiCustomerService.Infrastructure/  # EF、Redis、AI、WeChat、JWT、Jobs
│   │   ├── AiCustomerService.Api/           # Controllers、Program.cs
│   │   └── AiCustomerService.Worker/        # (预留)独立 Worker
│   └── Dockerfile
├── frontend/
│   ├── admin/                        # Vue 3 后台管理
│   └── miniprogram/                  # uni-app 客服工作台
├── docker-compose.yml                # Postgres+pgvector + Redis + API + Nginx
├── nginx.conf
├── .env.example
├── start.sh / start.bat              # 本地一键启动
└── README.md
```

## 快速开始

### 方式 1：本地开发

#### 1. 前置条件
- .NET 10 SDK
- Node.js 20+
- Docker Desktop（启动 Postgres + Redis）
- 通义千问 API Key（阿里云 DashScope）

#### 2. 启动

**Linux/macOS：**
```bash
cp .env.example .env
# 编辑 .env 填入真实 TONGYI_API_KEY
./start.sh
```

**Windows：**
```bat
copy .env.example .env
start.bat
```

#### 3. 访问
- API: http://localhost:5000
- Admin: http://localhost:5173
- MiniApp (H5): http://localhost:5174
- Hangfire: http://localhost:5000/hangfire
- OpenAPI JSON: http://localhost:5000/openapi/v1.json

#### 4. 注册第一个租户
```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "tenant_name": "演示公司",
    "username": "admin",
    "password": "admin123",
    "contact_name": "张三",
    "contact_phone": "13800000000",
    "contact_email": "admin@example.com",
    "industry_code": "general"
  }'
```

### 方式 2：Docker 一键部署

```bash
cp .env.example .env
# 填入 TONGYI_API_KEY
docker compose up -d
docker compose logs -f api
```

## 核心 API

| 模块 | 端点 | 说明 |
| --- | --- | --- |
| 认证 | `POST /api/v1/auth/register` | 注册租户+管理员 |
| 认证 | `POST /api/v1/auth/login` | 登录 |
| 认证 | `POST /api/v1/auth/refresh` | 刷新 Token |
| 会话 | `GET /api/v1/conversations` | 会话列表 |
| 会话 | `GET /api/v1/conversations/{id}` | 会话详情 |
| 会话 | `POST /api/v1/conversations/{id}/messages/agent` | 客服发送 |
| 会话 | `POST /api/v1/conversations/{id}/handoff` | 转人工 |
| 知识库 | `POST /api/v1/knowledge/documents` | 上传文档（PDF/Word/TXT/MD/CSV） |
| 知识库 | `GET /api/v1/knowledge/documents` | 文档列表 |
| 知识库 | `GET /api/v1/knowledge/documents/{id}/job` | 摄取任务状态 |
| 知识库 | `POST /api/v1/knowledge/documents/{id}/reindex` | 重建索引 |
| 客户 | `GET /api/v1/customers` | 客户列表（含意向筛选） |
| 客户 | `PUT /api/v1/customers/{id}/tags` | 更新标签 |
| 租户 | `GET /api/v1/tenant` | 当前租户信息 |
| 租户 | `GET/PUT /api/v1/tenant/settings` | System Prompt / 欢迎语 |
| 微信 | `GET/POST /api/v1/wechat/{appId}` | 公众号回调（验签+收消息） |

## 工作流

### 用户消息处理（RAG 流水线）
```
微信/小程序/API
   │
   ▼
WeChatController ─► WeChatService.HandleMessageAsync()
   │
   ▼
ConversationService.HandleUserMessageAsync()
   │
   ├─► HybridRetriever.RetrieveAsync()
   │     ├─► Vector search (pgvector <=>)
   │     └─► Keyword search (PostgreSQL ILIKE)
   │
   ├─► TongyiAIService.ChatAsync() ─► 通义千问 qwen-plus
   │
   ├─► 保存 user/assistant Message
   ├─► 更新 Customer.IntentionScore
   └─► 记录 AiUsageLog
```

### 文档摄取（Hangfire 后台任务）
```
POST /api/v1/knowledge/documents (multipart)
   │
   ▼
KnowledgeService.UploadAsync() ─► 保存文件 + 创建 doc(pending)
   │
   ▼
Enqueue<IngestDocumentJob>()
   │
   ▼
Hangfire Worker ─► IngestDocumentJob.ExecuteAsync()
   ├─► DocumentLoader (PDF/Word/TXT/CSV)
   ├─► TextCleaner (去除 HTML/Markdown/控制字符)
   ├─► TextSplitter (按段落+滑动窗口)
   ├─► EmbeddingBatcher (批次 25，失败 3 次重试)
   ├─► PgVectorStore (写入 knowledge_chunks 表)
   └─► doc.Status = ready
```

## 多租户隔离

- JWT 中携带 `tenant_id` 声明
- `TenantContext` 自动从 `HttpContext.User` 解析
- 所有查询/写入都通过 `tenant_id` 过滤
- 知识库按租户隔离：`WHERE tenant_id = ?`

## 微信接入步骤

1. 在公众号后台设置回调 URL：`https://your-domain/api/v1/wechat/{appId}`
2. 在租户设置或数据库 `channel_configs` 表中维护 `{appId}` 与租户的绑定关系
3. 系统自动完成签名验证 + 消息解密（简化版）

## 数据模型（核心表）

- `tenants` — 租户
- `users` — 用户（含 `tenant_id`）
- `customers` — 客户（按 `tenant_id + external_id` 唯一）
- `conversations` — 会话
- `messages` — 消息（含 retrieval_chunks JSON）
- `knowledge_documents` — 文档
- `knowledge_chunks` — 文档切片（含 `vector(1024)` 列）
- `refresh_tokens` — 刷新 Token

## 监控

- Hangfire Dashboard：`/hangfire`（开发环境无需鉴权，生产环境建议加 BasicAuth）
- 健康检查：`/health`
- 日志：Serilog 写入 `logs/app-{date}.log`
- OpenAPI：`/openapi/v1.json`

## 生产部署建议

1. **数据库**：阿里云 RDS PostgreSQL（含 pgvector 插件）或自建 + WAL-G 备份
2. **缓存**：阿里云 Redis 或自建 Redis Cluster
3. **API**：4 核 8G × 2 节点，用 Nginx 负载均衡
4. **静态资源**：管理后台用 CDN
5. **微信公众号**：必须 HTTPS，需 ICP 备案域名
6. **监控**：Prometheus + Grafana + AlertManager
7. **日志**：Seq / ELK

## 路线图

- [x] 阶段 1-6：基础架构 + API + 微信
- [x] 阶段 7：Vue 3 后台
- [x] 阶段 8：uni-app 工作台
- [x] 阶段 9：Docker 部署
- [ ] 阶段 10：评测框架（用 RAGAS 跑测试集）
- [ ] 阶段 11：多模态文档（图、表识别）
- [ ] 阶段 12：冷启动引导（行业 FAQ 包）

## 许可

MIT