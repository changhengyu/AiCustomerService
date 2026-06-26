# 更新日志

所有重要变更会记录在此文件。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)。

## [Unreleased]

### Planned
- 客户画像与精准营销（v0.4.0）
- 语音消息支持（v0.4.0）
- SaaS 计费与订阅中心（v0.4.0）

## [0.3.0] - 2026-08-01

### Added
- 多 LLM 适配器：`IAiProviderFactory` 统一通义/OpenAI/DeepSeek/智谱，OpenAI 兼容协议复用同一 OpenAIClient
- Function Calling 智能体：`AgentService` 基于 MEAI `FunctionInvokingChatClient`，
  内置 5 个客服工具（订单/物流/退款/转人工/客户历史），自动多轮调用最多 5 次
- BI 报表与 Dashboard：`BiController` 提供 5 个端点（overview/trend/intention/hot-questions/ai-usage），
  会话趋势按天聚合、意向度分布、高频问题聚合、AI 用量 P95 延迟
- 开放 API（API Key + Webhook）：`OpenApiController` 颁发 `ak_live_` 前缀 Key，
  Webhook Outbox 投递（指数退避重试 + HMAC-SHA256 签名 + 投递日志）
- 3 个新 EF Core 实体：`ApiKey` / `WebhookConfig` / `WebhookDelivery`
- 2 个新 Controller：`AgentController` / `BiController` / `OpenApiController`
- 智能体端点：`GET /api/v1/agent/providers` 列出全部 LLM 提供商

## [0.2.0] - 2026-07-15

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
