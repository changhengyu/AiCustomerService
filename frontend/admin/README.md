# AI 客服 SaaS - 管理后台

Vue 3 + Vite + Element Plus + Pinia

## 启动
```bash
npm install
npm run dev   # http://localhost:5173
```

## 功能
- 仪表盘：租户信息、用量统计
- 会话管理：列表、详情、AI/客服对话、转人工、关闭
- 知识库：上传 PDF/Word/TXT/MD/CSV，自动切片+Embedding+入库
- 客户管理：意向分级、标签管理
- 设置：System Prompt、欢迎语、转人工关键词
- Hangfire 集成：访问 `/hangfire` 查看后台任务
- OpenAPI：访问 `/openapi/v1.json`

## 默认账号
- 首次启动请通过 `/api/v1/auth/register` 注册