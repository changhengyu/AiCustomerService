# 更新日志

所有重要变更会记录在此文件。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)。

## [Unreleased]

### Added
- 后端：.NET 10 + Clean Architecture 完整骨架
- 后端：EF Core 10 + pgvector 向量检索
- 后端：Hangfire 后台任务（文档摄取、租户重建）
- 后端：通义千问 AI 集成（qwen-plus + text-embedding-v3）
- 后端：JWT 认证 + Refresh Token 机制
- 后端：多租户架构（TenantContext）
- 后端：8 个 REST API Controller
- 后端：微信公众台回调（验签 + XML 消息接收 + 完整 AES-256-CBC 加密）
- 前端：Vue 3 + Element Plus 后台管理
- 前端：uni-app 跨端客服工作台（精修版）
- 部署：Docker Compose 一键启动
- 文档：完整 README + 开发指南 + API 文档 + 架构设计

## [0.2.0] - 2026-07-15

### Added
- 行业冷启动 FAQ 包：6 个行业 50+ 条（general/ecommerce/education/saas/finance/medical），
  启动时自动 seed，支持按 industry_code + 关键词检索
- 微信消息加密完整实现：WeChatMessageCryptor 支持 AES-256-CBC + PKCS#7，
  兼容明文与加密双模式
- PDF 多模态识别：图像探测 + 表格启发式抽取（Tab 分隔 + 多空格对齐）
- RAGAS 评测端点：faithfulness / answer_relevancy / context_precision 三指标，
  支持评测集运行与历史报告查询
- Microsoft.Extensions.AI 重构：IChatClient / IEmbeddingGenerator 抽象
- Rate Limiting 中间件：4 个命名策略（登录/注册/上传/聊天）
- TrialEndsAt + IndustryCode 字段：注册自动设 14 天试用
- 2 个 EF Core 迁移（AddTenantTrialFields + AddIndustryFaq）

## [0.1.0] - 2026-06-26

### Added
- 项目初始化
- 阶段 1：搭建 .NET 10 项目骨架（4 层 Clean Architecture）
- 阶段 2：核心实体 + EF Core + 2 个数据库迁移
- 阶段 3：基础设施层（AI / Hangfire / WeChat / JWT / 多租户）
- 阶段 4：业务 Service 层（Auth / Conversation / Knowledge / Customer / Tenant / WeChat）
- 阶段 5：API Controllers + JWT + OpenAPI + Hangfire + 全局异常
- 阶段 6：微信接入（验签 + 消息接收 + 自动回复）
- 阶段 7：Vue 3 后台（Dashboard / 会话 / 知识库 / 客户 / 设置）
- 阶段 8：uni-app 客服工作台（5 个页面）
- 阶段 9：Docker Compose + Dockerfile + nginx.conf + 启动脚本

### Technical Details
- 后端解决方案：4 个 .NET 10 项目（Core / Infrastructure / Api / Worker）
- 数据库迁移：2 个（InitialSchema + AddRefreshTokens）
- REST 端点：30+
- 后台前端页面：8 个 Vue 单文件组件
- 移动端页面：5 个 uni-app 页面（含登录页精修版）

[Unreleased]: https://github.com/changhengyu/AiCustomerService/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/changhengyu/AiCustomerService/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/changhengyu/AiCustomerService/releases/tag/v0.1.0
