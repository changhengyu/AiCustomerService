<div align="center">

# 🤖 AI 客服 SaaS 平台

> 基于 .NET 10 + uni-app 构建的多租户 AI 客服系统  
> 集成 RAG 检索增强生成、微信公众号接入、智能转人工、客户意向度分析

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Vue](https://img.shields.io/badge/Vue-3.5-4FC08D?logo=vue.js)](https://vuejs.org/)
[![uni-app](https://img.shields.io/badge/uni--app-3.0-2C8EFF)](https://uniapp.dcloud.net.cn/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![pgvector](https://img.shields.io/badge/pgvector-0.7-orange)](https://github.com/pgvector/pgvector)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)

[功能特性](#-功能特性) • [快速开始](#-快速开始) • [架构设计](#-架构设计) • [文档](#-项目文档) • [部署](#-部署) • [路线图](#-路线图)

</div>

---

## 📖 项目简介

**AI 客服 SaaS** 是一套面向中小企业的智能客服解决方案，提供从知识库管理、对话引擎到微信生态接入的完整能力。系统采用 RAG（检索增强生成）架构，基于企业自有知识库回答用户问题，准确率高、可控可审计；支持 AI 自动回复与人工客服无缝协作；多租户设计让 SaaS 化运营开箱即用。

### 🎯 解决什么问题

- 客服人员成本高、培训周期长 → AI 7×24 自动应答
- FAQ 散落在 Word、PDF、聊天记录中 → 统一知识库管理
- 客户线索质量难判断 → 实时意向度评分
- 微信生态接入开发成本高 → 一行配置完成公众号对接
- 多个业务线要隔离数据 → 多租户架构按需授权

### ✨ 核心亮点

- **🧠 RAG 引擎**：向量检索 + 关键词混合排序，答案来源可追溯
- **📚 多格式知识库**：PDF / Word / Markdown / TXT / CSV 智能解析
- **💬 多渠道接入**：微信公众号 + H5 + 小程序 + 网页（即将支持）
- **🤝 人机协作**：触发关键词自动转人工，AI 上下文无缝交接
- **📊 客户洞察**：自动评估客户意向度，按等级打标签
- **🏢 多租户 SaaS**：注册即可独立使用，资源按租户隔离

---

## 🚀 功能特性

### 🏗️ 后台管理（Vue 3 + Element Plus）

- 📊 **数据看板** - 会话量、响应时间、转化率一目了然
- 💼 **会话管理** - 实时查看 AI 与客户的完整对话，可接管
- 📚 **知识库** - 拖拽上传文档，自动切片 + 向量化，可视化检索
- 👥 **客户管理** - 客户画像、意向度评分、标签体系
- ⚙️ **租户设置** - 系统 Prompt、欢迎语、关键词、FAQ 行业包

### 📱 客服工作台（uni-app 跨端）

- 🎨 **精美商业化 UI** - 浅色调专业设计语言
- 🏠 **首页工作台** - 待处理会话、待办提醒、关键指标卡片
- 💬 **实时聊天** - 与客户 AI 协作或独立回复
- 👤 **个人中心** - 工单数据、设置、状态切换

### 🔌 后端服务（.NET 10 Clean Architecture）

- 🏛️ **分层架构** - Api / Application / Domain / Infrastructure 清晰隔离
- 🗄️ **EF Core 10 + pgvector** - 关系型与向量统一存储
- 🔄 **Hangfire 后台任务** - 文档摄取、租户重建，自动重试
- 🔐 **JWT + Refresh Token** - 无状态鉴权，Token 轮换
- 🤖 **通义千问集成** - qwen-plus + text-embedding-v3
- 🌐 **多租户** - 数据库行级隔离，ITenantContext 上下文传递
- 🛡️ **安全加固** - PBKDF2 密码哈希、限流、审计日志

### 🛠️ 运维部署

- 🐳 **Docker Compose** - 一键拉起 Postgres + Redis + API + Nginx
- 📈 **Hangfire Dashboard** - 任务状态可视化
- 📜 **Serilog 结构化日志** - JSON 格式便于聚合
- 🔍 **OpenAPI 3.0** - `/openapi/v1.json` 自动化接口描述

---

## 🏛️ 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                      客户端 / 渠道                           │
│  📱 微信公众号   🌐 H5 / 小程序   🖥️ 后台 (Vue 3)          │
└──────────────────┬──────────────────────────┬───────────────┘
                   │                          │
                   ▼                          ▼
┌─────────────────────────────────────────────────────────────┐
│              Nginx 反向代理 (SSL + 负载均衡)                │
└──────────────────┬──────────────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────────────┐
│         .NET 10 API (Clean Architecture)                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  Controllers (Auth / Conversation / Knowledge / ...)  │ │
│  ├───────────────────────────────────────────────────────┤ │
│  │  Services (Auth / Conversation / Knowledge / RAG)     │ │
│  ├───────────────────────────────────────────────────────┤ │
│  │  Infrastructure (EF Core / Redis / Tongyi / Hangfire) │ │
│  └───────────────────────────────────────────────────────┘ │
└──┬─────────────┬─────────────┬─────────────┬──────────────┘
   ▼             ▼             ▼             ▼
┌──────┐   ┌──────────┐   ┌─────────┐   ┌──────────┐
│  PG  │   │  Redis   │   │ Tongyi  │   │ Hangfire │
│+pvec │   │  Cache   │   │   AI    │   │  Worker  │
└──────┘   └──────────┘   └─────────┘   └──────────┘
```

详细架构：[docs/架构设计.md](docs/架构设计.md)

---

## 🚀 快速开始

### 前置条件

- Docker Desktop 4.x+
- 通义千问 API Key（[申请](https://dashscope.aliyun.com/)）

### 一键启动

```bash
# 1. 克隆项目
git clone https://github.com/changhengyu/AiCustomerService.git
cd AiCustomerService

# 2. 复制环境变量模板
cp .env.example .env
# 编辑 .env，填入 TONGYI_API_KEY=sk-xxxxx

# 3. 一键启动
./start.sh         # Linux / macOS
start.bat          # Windows
```

启动成功后访问：

| 服务 | 地址 | 说明 |
| --- | --- | --- |
| 🖥️ 后台 | http://localhost:5173 | 管理员登录页 |
| 📱 MiniApp | http://localhost:5174 | uni-app H5 |
| 🔌 API | http://localhost:5000 | 后端 API |
| 📊 Hangfire | http://localhost:5000/hangfire | 任务监控 |
| 📜 OpenAPI | http://localhost:5000/openapi/v1.json | 接口规范 |

### 默认账号

首次启动会输出默认管理员账号到日志，或通过 `docker compose logs api` 查看：

```
Tenant: 演示公司
Username: admin
Password: admin123
```

### 5 分钟体验流程

1. **打开后台** http://localhost:5173 → 登录
2. **上传文档**：知识库 → 上传 → 拖入 PDF（系统自动摄取）
3. **配置客服**：设置 → 系统 Prompt → "你是 XX 公司的客服"
4. **测试对话**：在 MiniApp 工作台用任意 customer 发起问题
5. **查看会话**：后台会话列表 → 进入详情看 AI 回复

---

## 🛠️ 技术栈

### 后端

| 类别 | 技术 | 用途 |
| --- | --- | --- |
| 运行时 | .NET 10 | LTS、原生 AOT、高性能 |
| Web | ASP.NET Core 10 | Web API |
| ORM | EF Core 10 + Npgsql | 数据访问 |
| 向量库 | pgvector 0.7 | 语义检索 |
| 任务调度 | Hangfire + PostgreSQL | 后台任务 |
| 鉴权 | JWT + Refresh Token | 无状态认证 |
| 缓存 | StackExchange.Redis | 分布式缓存 |
| AI | 通义千问 (DashScope) | LLM + Embedding |
| 文档解析 | PdfPig + OpenXML | PDF / Word 解析 |
| 日志 | Serilog + Console | 结构化日志 |

### 前端

| 类别 | 技术 | 用途 |
| --- | --- | --- |
| 后台 | Vue 3 + Vite 5 + Element Plus + Pinia | 管理员 SPA |
| 移动端 | uni-app 3 + Vue 3 + TypeScript | 跨端 H5 / 小程序 |
| 状态 | Pinia | 客户端状态管理 |
| HTTP | Axios | API 请求 |
| 样式 | SCSS + 设计令牌 | 主题系统 |

### 基础设施

| 类别 | 技术 | 用途 |
| --- | --- | --- |
| 数据库 | PostgreSQL 16 | 关系数据 + 向量数据 |
| 缓存 | Redis 7 | 会话、限流、缓存 |
| 反向代理 | Nginx | 静态资源、SSL 终止 |
| 容器化 | Docker Compose | 一键部署 |
| 监控 | Hangfire Dashboard | 任务状态 |

---

## ⚙️ 配置说明

### 环境变量

| 变量 | 必填 | 默认 | 说明 |
| --- | --- | --- | --- |
| `TONGYI_API_KEY` | ✅ | - | 通义千问 API Key |
| `TONGYI_BASE_URL` | ❌ | `https://dashscope.aliyuncs.com/compatible-mode/v1` | API 地址 |
| `TONGYI_CHAT_MODEL` | ❌ | `qwen-plus` | 对话模型 |
| `TONGYI_EMBED_MODEL` | ❌ | `text-embedding-v3` | 向量模型 |
| `JWT_SECRET` | ✅ | - | JWT 签名密钥（≥32 字符） |
| `DATABASE_URL` | ❌ | `Host=postgres;Database=aics;...` | PG 连接串 |
| `REDIS_CONNECTION` | ❌ | `redis:6379` | Redis 地址 |
| `ASPNETCORE_ENVIRONMENT` | ❌ | `Production` | 运行环境 |

完整配置见 [`.env.example`](.env.example)

---

## 📦 部署

### Docker Compose 部署（推荐）

```bash
# 生产环境
docker compose -f docker-compose.yml up -d

# 升级
docker compose pull && docker compose up -d

# 查看日志
docker compose logs -f api
```

### Kubernetes 部署

参考 [`docs/deployment/k8s.md`](docs)（规划中）

### 反向代理 + SSL

使用 Nginx + Certbot 自动签发 Let's Encrypt 证书：

```nginx
server {
    listen 443 ssl http2;
    server_name api.example.com;
    ssl_certificate /etc/letsencrypt/live/.../fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/.../privkey.pem;
    
    location / {
        proxy_pass http://api:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 性能与扩展

- API 横向扩展：多实例 + Nginx upstream
- Hangfire 集群：自动选主，无需额外配置
- PostgreSQL 优化：连接池 100，`ivfflat` 索引向量列
- Redis 集群：超过 25GB 数据时考虑

---

## 📚 项目文档

| 文档 | 内容 |
| --- | --- |
| [📘 开发指南](docs/开发指南.md) | 环境搭建、代码组织、调试技巧、规范 |
| [📗 API 接口文档](docs/API接口文档.md) | 30+ REST 端点完整参考 |
| [📕 架构设计](docs/架构设计.md) | Clean Architecture、RAG、多租户、认证 |
| [📙 更新日志](docs/CHANGELOG.md) | 版本变更记录 |

---

## 🧪 演示截图

> 完整截图见 [`docs/screenshots/`](docs)

| 后台 Dashboard | 知识库管理 | 移动工作台 |
| --- | --- | --- |
| （截图占位） | （截图占位） | （截图占位） |

---

## 🗺️ 路线图

### ✅ v0.1.0（已发布 2026-06-26）

- [x] Clean Architecture 后端骨架
- [x] 多租户 + JWT 认证
- [x] RAG 知识库（PDF/Word/CSV 等）
- [x] 通义千问 AI 集成
- [x] 微信公众台接入
- [x] Vue 3 后台管理
- [x] uni-app 移动工作台
- [x] Docker Compose 部署

### 🚧 v0.2.0（计划 2026-07）

- [ ] 评测框架（RAGAS）支持
- [ ] 行业冷启动 FAQ 包（电商、教育、SaaS）
- [ ] 多模态文档（图、表识别）
- [ ] Rate Limiting 中间件
- [ ] 微信消息加密完整支持

### 🔮 v0.3.0+

- [ ] 多 LLM 适配器（OpenAI、DeepSeek、Claude）
- [ ] Function Calling 智能体工作流
- [ ] 客户画像与精准营销
- [ ] 语音消息支持
- [ ] BI 报表
- [ ] 开放 API 与 Webhook

---

## 🤝 参与贡献

欢迎 PR、Issue 与建议！

### 提交规范

采用 [Conventional Commits](https://www.conventionalcommits.org/zh-hans/)：

```bash
feat(knowledge): add PDF parsing for DocumentLoader
fix(auth): handle null tenant in refresh token flow
docs: update API documentation
refactor(ai): extract embedding retry logic
```

### 提交流程

1. Fork 本仓库
2. 创建分支：`git checkout -b feat/awesome-feature`
3. 提交代码：`git commit -m 'feat: add awesome feature'`
4. 推送分支：`git push origin feat/awesome-feature`
5. 创建 Pull Request

### 本地开发

```bash
# 后端
cd backend/src/AiCustomerService.Api
dotnet run

# 后台
cd frontend/admin
npm install && npm run dev

# 移动端
cd frontend/miniprogram
npm install && npm run dev:h5
```

详细开发流程见 [docs/开发指南.md](docs/开发指南.md)

---

## 🐛 问题反馈

- 🐞 [提交 Bug](https://github.com/changhengyu/AiCustomerService/issues/new?template=bug.md)
- 💡 [功能建议](https://github.com/changhengyu/AiCustomerService/issues/new?template=feature.md)
- 💬 [Discussion 讨论区](https://github.com/changhengyu/AiCustomerService/discussions)

---

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。

---

## 🙏 致谢

- [pgvector](https://github.com/pgvector/pgvector) - 优秀的 PG 向量扩展
- [Hangfire](https://www.hangfire.io/) - 强大的 .NET 后台任务
- [Element Plus](https://element-plus.org/) - 优雅的 Vue 3 组件库
- [uni-app](https://uniapp.dcloud.net.cn/) - 跨端开发利器
- [DashScope](https://dashscope.aliyun.com/) - 通义千问开放平台

---

## 📮 联系方式

- 作者：changhengyu
- 邮箱：changhengyu@users.noreply.github.com
- 仓库：https://github.com/changhengyu/AiCustomerService

---

<div align="center">

**⭐ 如果这个项目对您有帮助，请给一个 Star！**

Made with ❤️ by [changhengyu](https://github.com/changhengyu)

</div>
