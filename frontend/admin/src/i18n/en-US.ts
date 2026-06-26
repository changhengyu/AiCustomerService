export default {
  app: {
    title: 'AI Customer Service Admin',
    tagline: 'Multi-tenant Operations Console'
  },
  nav: {
    dashboard: 'Dashboard',
    conversations: 'Conversations',
    knowledge: 'Knowledge',
    customers: 'Customers',
    settings: 'Settings'
  },
  common: {
    confirm: 'Confirm',
    cancel: 'Cancel',
    save: 'Save',
    delete: 'Delete',
    edit: 'Edit',
    create: 'Create',
    search: 'Search',
    loading: 'Loading...',
    noData: 'No data',
    language: 'Language'
  },
  login: {
    title: 'Login',
    email: 'Email',
    password: 'Password',
    submit: 'Sign in',
    failed: 'Login failed. Check your credentials.'
  },
  dashboard: {
    overview: 'Overview',
    totalConversations: 'Total Conversations',
    aiHandled: 'AI Handled',
    humanHandoff: 'Human Handoff',
    minutesSaved: 'Minutes Saved'
  },
  conversations: {
    title: 'Conversations',
    status: { active: 'Active', human: 'Human', closed: 'Closed' },
    customer: 'Customer',
    messages: 'Messages',
    lastActive: 'Last Active',
    viewDetail: 'View'
  },
  knowledge: {
    title: 'Knowledge Base',
    upload: 'Upload',
    docName: 'Document',
    status: { processing: 'Processing', ready: 'Ready', failed: 'Failed' },
    chunks: 'Chunks'
  },
  customers: {
    title: 'Customers',
    nickname: 'Nickname',
    intention: { high: 'High', medium: 'Medium', low: 'Low', cold: 'Cold' },
    tags: 'Tags'
  },
  settings: {
    title: 'Settings',
    tenant: 'Tenant',
    apiKeys: 'API Keys',
    webhooks: 'Webhook Subscriptions'
  }
}
