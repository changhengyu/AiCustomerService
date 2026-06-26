# API 接口文档

> 基础地址：`http://localhost:5000/api/v1`
> 所有需要鉴权的接口必须在请求头携带 `Authorization: Bearer {access_token}`
> 数据格式：JSON，字段命名采用 snake_case

## 通用响应格式

### 成功响应
直接返回数据（无包装）：
```json
{
  "access_token": "eyJhbGc...",
  "refresh_token": "...",
  "expires_at": "2026-06-26T20:00:00Z",
  "user": { ... }
}
```

### 分页响应
```json
{
  "items": [...],
  "total": 142,
  "page": 1,
  "page_size": 20,
  "total_pages": 8
}
```

### 错误响应
```json
{
  "code": "not_found",
  "message": "文档不存在",
  "trace_id": "0HMDK8B3N5O7Q:00000001"
}
```

| 状态码 | code | 说明 |
| --- | --- | --- |
| 400 | `validation_error` | 请求参数验证失败 |
| 401 | `unauthorized` | 未登录或 Token 失效 |
| 403 | `forbidden` | 权限不足 |
| 404 | `not_found` | 资源不存在 |
| 429 | `quota_exceeded` | 配额超限 |
| 500 | `internal_error` | 服务器内部错误 |

---

## 1. 认证

### 1.1 注册租户

**POST** `/auth/register`

注册新的租户和管理员账号。

请求体：
```json
{
  "tenant_name": "演示公司",
  "username": "admin",
  "password": "admin123",
  "contact_name": "张三",
  "contact_phone": "13800000000",
  "contact_email": "admin@example.com",
  "industry_code": "general"
}
```

响应：`LoginResponse`

### 1.2 登录

**POST** `/auth/login`

请求体：
```json
{
  "username": "admin",
  "password": "admin123",
  "tenant_id": "11111111-1111-1111-1111-111111111111"
}
```

响应：`LoginResponse`

### 1.3 刷新 Token

**POST** `/auth/refresh`

请求体：
```json
{
  "refresh_token": "..."
}
```

响应：`LoginResponse`

---

## 2. 会话

### 2.1 会话列表

**GET** `/conversations?page=1&page_size=20&status=active`

查询参数：
- `page`：页码，从 1 开始，默认 1
- `page_size`：每页数量，默认 20
- `status`：可选 `active` / `human` / `closed`

响应：`PagedResult<ConversationListItemDto>`

### 2.2 会话详情

**GET** `/conversations/{id}`

响应：`ConversationDetailDto`

### 2.3 客服发送消息

**POST** `/conversations/{id}/messages/agent`

请求体：
```json
{ "content": "您好，我是人工客服小李" }
```

响应：`SendMessageResponse`

### 2.4 转人工

**POST** `/conversations/{id}/handoff`

请求体（可选）：
```json
{ "assigned_to": "用户ID" }
```

### 2.5 关闭会话

**POST** `/conversations/{id}/close`

---

## 3. 知识库

### 3.1 文档列表

**GET** `/knowledge/documents?page=1&page_size=20`

响应：`PagedResult<DocumentDto>`

### 3.2 上传文档

**POST** `/knowledge/documents`

Content-Type：`multipart/form-data`

字段：
- `title`：文档标题（必填）
- `file`：文件（必填，支持 pdf/docx/txt/md/csv）

支持的文件类型：
- PDF（.pdf）
- Word（.docx）
- 纯文本（.txt）
- Markdown（.md）
- CSV（.csv）

文件大小限制：50MB

### 3.3 删除文档

**DELETE** `/knowledge/documents/{id}`

软删除：文档标记为 `deleted`，同时删除所有 chunks。

### 3.4 查询摄取任务状态

**GET** `/knowledge/documents/{id}/job`

响应：
```json
{
  "state": "ready",
  "processed": 24,
  "total": 24,
  "error_message": null
}
```

### 3.5 重建索引

**POST** `/knowledge/documents/{id}/reindex`

将文档重新切片、向量化、入库。

### 3.6 文档分块列表

**GET** `/knowledge/documents/{id}/chunks?page=1&page_size=50`

响应：`List<ChunkDto>`

---

## 4. 客户

### 4.1 客户列表

**GET** `/customers?page=1&page_size=20&intention_level=high&keyword=张三`

查询参数：
- `intention_level`：可选 `cold` / `low` / `medium` / `high`
- `keyword`：搜索昵称、external_id

响应：`PagedResult<CustomerListItemDto>`

### 4.2 客户详情

**GET** `/customers/{id}`

### 4.3 更新客户标签

**PUT** `/customers/{id}/tags`

请求体：
```json
{ "tags": ["VIP", "高意向", "已联系"] }
```

---

## 5. 租户

### 5.1 当前租户信息

**GET** `/tenant`

```json
{
  "id": "...",
  "name": "演示公司",
  "plan": "trial",
  "status": "active",
  "monthly_message_quota": 1000,
  "monthly_message_used": 142
}
```

### 5.2 获取租户设置

**GET** `/tenant/settings`

```json
{
  "system_prompt": "你是 AI 客服...",
  "welcome_message": "您好，请问有什么可以帮您？",
  "handoff_keywords": ["人工", "转人工"],
  "industry_id": null,
  "use_industry_faq": false
}
```

### 5.3 更新租户设置

**PUT** `/tenant/settings`

请求体：同获取返回

---

## 6. 微信回调

### 6.1 验证 URL

**GET** `/wechat/{appId}?signature=xxx&timestamp=xxx&nonce=xxx&echostr=xxx`

微信公众号接入验证，成功返回 `echostr` 原文。

### 6.2 接收消息

**POST** `/wechat/{appId}`

Content-Type：`application/xml`

接收微信公众号推送的 XML 消息，自动处理并通过客服接口回复。

---

## 7. 内部聊天（测试用）

### 7.1 发送用户消息

**POST** `/chat`

请求体：
```json
{
  "tenant_id": "...",
  "customer_id": "...",
  "content": "请问产品价格是多少？",
  "conversation_id": null
}
```

响应：`SendMessageResponse`

> 此接口用于内部测试和前端调试，绕过微信直接调用 RAG 流水线。

---

## 8. 行业冷启动 FAQ（v0.2.0）

### 8.1 列出本租户行业全部 FAQ

**GET** `/industry-faqs`

### 8.2 列出所有行业代码

**GET** `/industry-faqs/industries`（无需鉴权）

### 8.3 关键词检索

**GET** `/industry-faqs/search?industryCode=ecommerce&q=退款&topK=3`

---

## 9. RAGAS 评测（v0.2.0 内部用）

### 9.1 运行评测

**POST** `/eval/run`

请求体：
```json
{
  "dataset_name": "smoke-test",
  "cases": [
    { "question": "...", "ground_truth_answer": "..." }
  ]
}
```

### 9.2 获取报告

**GET** `/eval/reports/{id}`

### 9.3 报告历史

**GET** `/eval/reports?limit=20`

---

## 错误码参考

| code | HTTP | 触发条件 |
| --- | --- | --- |
| `validation_error` | 400 | DTO 验证失败、必填字段缺失 |
| `unauthorized` | 401 | JWT 无效/过期、密码错误 |
| `forbidden` | 403 | 租户停用、用户停用、权限不足 |
| `not_found` | 404 | 资源不存在 |
| `quota_exceeded` | 429 | 月度消息配额超限 |
| `internal_error` | 500 | 未捕获异常 |

## 限流

- 登录接口：每 IP 5 次/分钟
- 文档上传：每租户 20 次/小时
- 聊天接口：每租户按 plan 限制（trial: 100 次/小时）

## OpenAPI 文档

开发环境访问：`http://localhost:5000/openapi/v1.json`

可使用 [Swagger Editor](https://editor.swagger.io/) 导入查看。

## 10. AI 智能体（v0.3.0+）

> 基于 MEAI FunctionInvokingChatClient 自动工具调用，最多 5 轮迭代。

### POST `/api/v1/agent/chat`

请求体：

```json
{
  "question": "帮我查订单 12345 的物流",
  "model": "qwen-plus",
  "provider": "tongyi",
  "temperature": 0.7
}
```

响应：

```json
{
  "content": "已为您查询订单 12345 的物流：顺丰快递 SF1234567890，运输中。",
  "toolCalls": ["QueryLogistics"],
  "inputTokens": 256,
  "outputTokens": 128,
  "latencyMs": 1820,
  "provider": "tongyi"
}
```

可用 Provider：`tongyi` / `openai` / `deepseek` / `zhipu`。

### GET `/api/v1/agent/providers`

列出全部可用 LLM Provider 及其默认模型（公开接口，无需鉴权）。

## 11. BI 报表（v0.3.0+）

> 供后台 Dashboard 使用，所有接口按当前用户租户隔离。

### GET `/api/v1/bi/overview?days=30`

返回 Dashboard 总览数据：

```json
{
  "totalConversations": 1240,
  "activeConversations": 18,
  "humanConversations": 42,
  "closedConversations": 1180,
  "aiHandledConversations": 1198,
  "totalCustomers": 856,
  "totalDocuments": 32,
  "totalMessages": 9842,
  "estimatedMinutesSaved": 5990,
  "periodDays": 30
}
```

### GET `/api/v1/bi/trend?days=7`

按天聚合的会话趋势（自动补齐缺失日期）。

### GET `/api/v1/bi/intention`

客户意向度分布（high/medium/low/none + 占比百分比）。

### GET `/api/v1/bi/hot-questions?topN=10&days=7`

高频问题 Top N（按消息内容归一化聚合）。

### GET `/api/v1/bi/ai-usage?days=30`

AI 用量与延迟统计（含 P95 延迟）。

## 12. 开放 API（v0.3.0+）

### API Key 管理

| Method | Path | 说明 |
| --- | --- | --- |
| `POST` | `/api/v1/open/keys` | 创建 Key（明文仅返回一次） |
| `GET`  | `/api/v1/open/keys` | 列出当前租户的 Key |
| `DELETE` | `/api/v1/open/keys/{id}` | 吊销 Key |

创建请求：

```json
{ "name": "BI 数据同步", "scopes": "read", "expiresAt": "2027-01-01T00:00:00Z" }
```

创建响应（`plainTextKey` 仅返回一次，需立即保存）：

```json
{
  "id": "...",
  "prefix": "ak_live_a1b2c3d4",
  "name": "BI 数据同步",
  "scopes": "read",
  "expiresAt": "2027-01-01T00:00:00Z",
  "createdAt": "2026-06-26T12:00:00Z",
  "plainTextKey": "ak_live_a1b2c3d4e5f6..."
}
```

### Webhook 管理

| Method | Path | 说明 |
| --- | --- | --- |
| `POST` | `/api/v1/open/webhooks` | 创建订阅（生成 HMAC secret） |
| `GET`  | `/api/v1/open/webhooks` | 列出当前租户的订阅 |
| `DELETE` | `/api/v1/open/webhooks/{id}` | 删除订阅 |
| `POST` | `/api/v1/open/webhooks/dispatch` | 手动触发一次投递（管理用） |

订阅事件示例：`conversation.created`、`message.received`、`evaluation.completed`，
使用 `*` 订阅所有事件。

**Webhook 请求头**：

```
X-Webhook-Event: conversation.created
X-Webhook-Signature: sha256=<hmac-hex>
X-Webhook-Delivery: <delivery-uuid>
```

**签名算法**：`HMAC-SHA256(secret, raw_body)` → hex（小写）。

**重试策略**：指数退避 1 → 2 → 4 → 8 → 16 → 32 分钟，最多 6 次；6 次后状态置 `failed`。

## 13. 客户画像（v0.4.0+）

### GET `/api/v1/profile/{customerId}`

返回客户完整画像（含 notes、timeline、segments、完整度评分 0-100）。

### PATCH `/api/v1/profile/{customerId}`

更新画像（Email / Nickname / Phone / Region / Gender / Source / LifecycleStage / Tags）；
自动写入 `customer.profile_updated` 时间线事件 + 触发 `customer.profile_updated` webhook。

### POST `/api/v1/profile/{customerId}/notes`

```json
{ "content": "客户偏好中文沟通" }
```

返回新增备注 + 触发 `customer.note_added` 时间线事件。

## 14. 客户分群与营销触发器（v0.4.0+）

### 分群

| Method | Path | 说明 |
| --- | --- | --- |
| `GET` | `/api/v1/segments` | 列出全部 segment |
| `POST` | `/api/v1/segments` | 创建 segment |
| `DELETE` | `/api/v1/segments/{id}` | 删除 |
| `POST` | `/api/v1/segments/{id}/evaluate` | 重新计算 member_count |
| `GET` | `/api/v1/segments/{id}/members?limit=200` | 列出命中客户 |

规则示例：

```json
{
  "name": "高意向 VIP 客户",
  "rules": {
    "intention": ["high"],
    "lifecycle": ["customer"],
    "tags": ["vip"],
    "min_score": 80
  }
}
```

支持的内置字段：`intention`、`lifecycle`、`region`、`tags`、`min_score`。

### 触发器

| Method | Path | 说明 |
| --- | --- | --- |
| `GET` | `/api/v1/marketing/triggers` | 列出全部 trigger |
| `POST` | `/api/v1/marketing/triggers` | 创建 trigger |
| `DELETE` | `/api/v1/marketing/triggers/{id}` | 删除 |

创建示例：

```json
{
  "name": "意向升级到高自动打 VIP 标签",
  "eventType": "customer.intention_changed",
  "conditions": { "to": "high" },
  "actions": [
    { "type": "add_tag", "tag": "vip" },
    { "type": "add_note", "content": "客户已成为高意向客户，请销售跟进" },
    { "type": "webhook" }
  ]
}
```

支持的 action type：`add_tag`、`add_note`、`webhook`。

## 15. 语音消息（v0.4.0+）

### POST `/api/v1/chat/voice`（multipart/form-data）

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `audio` | file | ✓ | 音频文件（≤ 20 MB） |
| `tenantId` | Guid | ✓ | 租户 ID |
| `customerId` | Guid | ✓ | 客户 ID |
| `conversationId` | Guid | — | 可选 |
| `format` | string | — | 默认 `wav`，支持 pcm/wav/amr/mp3 |

响应：

```json
{
  "transcript": "你们支持哪些支付方式",
  "sttProvider": "aliyun",
  "sttLatencyMs": 420,
  "reply": "支持微信、支付宝和银行卡...",
  "conversationId": "...",
  "messageId": "...",
  "latencyMs": 1820
}
```

未配置阿里云 AppKey 时自动降级到 Noop（返回占位文本，流程仍跑通）。

### 微信语音

`WeChatService` 自动处理 `msgType=voice` 微信消息：下载微信临时素材 → STT → 走 chat pipeline → 回复。

## 16. 计费与订阅（v0.4.0+）

### GET `/api/v1/billing/plans`（公开）

返回全部 plan 及其价格、限额：

```json
[
  { "name": "trial", "priceCents": 0, "monthlyMessageQuota": 500, "chatRateLimit": 100, ... },
  { "name": "pro",   "priceCents": 9900, "monthlyMessageQuota": 5000, "chatRateLimit": 500, ... }
]
```

### POST `/api/v1/billing/checkout`

```json
{ "plan": "pro", "provider": "stripe" }
```

Provider 可选：`stripe` / `wechat_pay` / `noop`（开发用）。

### POST `/api/v1/billing/webhook/{provider}`（公开）

Stripe 验签走 `Stripe-Signature` header；微信支付 V3 验签走 `Wechatpay-Signature` header。

### GET `/api/v1/billing/history`

当前租户订阅历史。

### POST `/api/v1/billing/cancel`

取消订阅；触发 `subscription.cancelled` webhook 事件。

## 17. 多语言（v0.4.0+）

### GET `/api/v1/culture/current`（公开）

返回当前 culture + 支持列表。切换方式：HTTP `Accept-Language` header。

后端错误消息自动按 culture 翻译；前端 admin 通过顶栏 🌐 切换；前端 uni-app 通过设置页切换。

## 18. Agent 可观测（v0.4.0+）

OpenTelemetry 全栈接入。`ActivitySource` = `AiCustomerService`，OTLP exporter 默认指向
`http://localhost:4317`（可配 `OpenTelemetry:OtlpEndpoint`）。

**Span 层级**：

```
chat.handle_user_message
├── rag.retrieve       (tags: rag.top_k, rag.hit_count)
└── llm.chat           (tags: llm.model, llm.tokens_*)
agent.chat
└── tool.query_order   (tags: tool.input.order_no)
```

**Metrics**：见 CHANGELOG v0.4.0 段。

---
