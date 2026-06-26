export default {
  app: {
    title: 'AI 客服管理后台',
    tagline: '多租户智能客服运营中心'
  },
  nav: {
    dashboard: '数据看板',
    conversations: '会话',
    knowledge: '知识库',
    customers: '客户',
    settings: '设置'
  },
  common: {
    confirm: '确定',
    cancel: '取消',
    save: '保存',
    delete: '删除',
    edit: '编辑',
    create: '新建',
    search: '搜索',
    loading: '加载中...',
    noData: '暂无数据',
    language: '语言'
  },
  login: {
    title: '登录',
    email: '邮箱',
    password: '密码',
    submit: '登录',
    failed: '登录失败，请检查账号密码'
  },
  dashboard: {
    overview: '总览',
    totalConversations: '总会话数',
    aiHandled: 'AI 处理',
    humanHandoff: '人工转接',
    minutesSaved: '节省人工（分钟）'
  },
  conversations: {
    title: '会话管理',
    status: { active: '进行中', human: '人工', closed: '已关闭' },
    customer: '客户',
    messages: '消息数',
    lastActive: '最后活跃',
    viewDetail: '查看详情'
  },
  knowledge: {
    title: '知识库',
    upload: '上传文档',
    docName: '文档名',
    status: { processing: '处理中', ready: '就绪', failed: '失败' },
    chunks: '切片数'
  },
  customers: {
    title: '客户',
    nickname: '昵称',
    intention: { high: '高意向', medium: '中意向', low: '低意向', cold: '未评分' },
    tags: '标签'
  },
  settings: {
    title: '设置',
    tenant: '租户信息',
    apiKeys: 'API Key',
    webhooks: 'Webhook 订阅'
  }
}
