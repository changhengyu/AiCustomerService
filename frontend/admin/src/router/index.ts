import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const routes = [
  { path: '/login', component: () => import('@/views/Login.vue') },
  {
    path: '/',
    component: () => import('@/views/Layout.vue'),
    redirect: '/dashboard',
    children: [
      { path: 'dashboard', component: () => import('@/views/Dashboard.vue') },
      { path: 'conversations', component: () => import('@/views/Conversations.vue') },
      { path: 'conversations/:id', component: () => import('@/views/ConversationDetail.vue') },
      { path: 'knowledge', component: () => import('@/views/Knowledge.vue') },
      { path: 'customers', component: () => import('@/views/Customers.vue') },
      { path: 'settings', component: () => import('@/views/Settings.vue') }
    ]
  }
];

const router = createRouter({ history: createWebHistory(), routes });

router.beforeEach((to) => {
  const auth = useAuthStore();
  if (!auth.accessToken && to.path !== '/login') return '/login';
});

export default router;