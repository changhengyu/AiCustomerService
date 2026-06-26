# AI 客服 SaaS - 客服工作台

uni-app 跨端客服工作台（支持 H5、微信小程序、APP）

## 启动
```bash
npm install
# H5 开发
npm run dev:h5   # http://localhost:5174
# 微信小程序
npm run dev:mp-weixin
```

## 功能
- 登录（账号密码，演示版）
- 会话列表（按状态筛选）
- 聊天详情（接收用户消息 / 主动回复 / 转人工 / 关闭会话）
- 我的页面

## 编译为小程序
```bash
npm run build:mp-weixin
# 产物在 dist/build/mp-weixin/，用微信开发者工具打开
```