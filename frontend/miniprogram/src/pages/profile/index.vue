<script setup lang="ts">
import { ref } from 'vue';

const user = ref<any>(uni.getStorageSync('user') || {});
const stats = ref({
  handledToday: 12,
  handledTotal: 248,
  avgResponseSec: 18
});

function avatarInitial(name?: string) {
  return (name || 'U')[0].toUpperCase();
}

function logout() {
  uni.showModal({
    title: '退出登录',
    content: '确定要退出当前账号吗？',
    confirmColor: '#F43F5E',
    success: (res) => {
      if (res.confirm) {
        uni.clearStorageSync();
        uni.reLaunch({ url: '/pages/login/index' });
      }
    }
  });
}

function goPage(path: string) {
  uni.showToast({ title: '功能开发中', icon: 'none' });
}

const menus = [
  {
    group: '工作台',
    items: [
      { icon: '📊', label: '工单数据', desc: '查看个人数据', color: '#4F6EF7', bg: '#EEF1FE' },
      { icon: '🏷️', label: '标签管理', desc: '管理客户标签', color: '#10B981', bg: '#E6F8F1' },
      { icon: '📚', label: '我的知识库', desc: '快速检索 FAQ', color: '#F59E0B', bg: '#FEF5E5' }
    ]
  },
  {
    group: '设置',
    items: [
      { icon: '🔔', label: '消息通知', desc: '推送与提醒设置', color: '#06B6D4', bg: '#E0F7FB' },
      { icon: '🎨', label: '外观主题', desc: '浅色（默认）', color: '#8B5CF6', bg: '#F3EEFE' },
      { icon: '🔐', label: '账号安全', desc: '修改密码', color: '#475569', bg: '#F1F3F7' }
    ]
  },
  {
    group: '其他',
    items: [
      { icon: '❓', label: '帮助中心', desc: '常见问题', color: '#475569', bg: '#F1F3F7' },
      { icon: 'ℹ️', label: '关于', desc: 'v0.1.0 · MIT', color: '#475569', bg: '#F1F3F7' }
    ]
  }
];
</script>

<template>
  <view class="page">
    <!-- 顶部资料卡 -->
    <view class="profile-card">
      <view class="card-bg"></view>
      <view class="card-content">
        <view class="avatar">
          <text>{{ avatarInitial(user.display_name || user.username) }}</text>
        </view>
        <view class="info">
          <text class="name">{{ user.display_name || user.username || '客服' }}</text>
          <view class="role-pill">
            <text class="role-text">{{ user.role === 'owner' ? '主管理员' : (user.role || '客服') }}</text>
          </view>
        </view>
        <view class="tenant-tag">
          <text class="tenant-icon">🏢</text>
          <text class="tenant-text">{{ user.tenant_id?.slice(0, 8) || 'tenant' }}</text>
        </view>
      </view>
    </view>

    <!-- 数据概览 -->
    <view class="stats-card">
      <view class="stat-item">
        <text class="stat-num">{{ stats.handledToday }}</text>
        <text class="stat-label">今日处理</text>
      </view>
      <view class="stat-divider"></view>
      <view class="stat-item">
        <text class="stat-num">{{ stats.handledTotal }}</text>
        <text class="stat-label">累计会话</text>
      </view>
      <view class="stat-divider"></view>
      <view class="stat-item">
        <text class="stat-num">{{ stats.avgResponseSec }}<text class="unit">s</text></text>
        <text class="stat-label">平均响应</text>
      </view>
    </view>

    <!-- 菜单 -->
    <view v-for="(group, gi) in menus" :key="gi" class="menu-group">
      <text class="group-title">{{ group.group }}</text>
      <view class="menu-card">
        <view
          v-for="(item, i) in group.items"
          :key="i"
          class="menu-item"
          :class="{ last: i === group.items.length - 1 }"
          @click="goPage"
        >
          <view class="menu-icon" :style="{ background: item.bg }">
            <text>{{ item.icon }}</text>
          </view>
          <view class="menu-content">
            <text class="menu-label">{{ item.label }}</text>
            <text class="menu-desc">{{ item.desc }}</text>
          </view>
          <view class="menu-arrow">
            <text>›</text>
          </view>
        </view>
      </view>
    </view>

    <!-- 退出按钮 -->
    <view class="logout-btn" @click="logout">
      <text>退出登录</text>
    </view>

    <text class="footer">v0.1.0 · Made with ❤️</text>
  </view>
</template>

<style lang="scss" scoped>
@import '@/style/theme.scss';

.page {
  min-height: 100vh;
  background: var(--color-bg);
  padding-bottom: calc(40rpx + env(safe-area-inset-bottom, 0px));
}

/* 顶部资料卡 */
.profile-card {
  position: relative;
  margin: 0;
  padding: 48rpx 32rpx 96rpx;
  background: linear-gradient(135deg, var(--color-primary) 0%, #6B83FA 100%);
  overflow: hidden;
}
.card-bg {
  position: absolute;
  top: -100rpx;
  left: -100rpx;
  width: 400rpx;
  height: 400rpx;
  background: radial-gradient(circle, rgba(255,255,255,0.15) 0%, transparent 70%);
  border-radius: 50%;
}
.card-content {
  position: relative;
  display: flex;
  align-items: center;
  gap: 24rpx;
}
.avatar {
  flex-shrink: 0;
  width: 128rpx;
  height: 128rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10rpx);
  display: flex;
  align-items: center;
  justify-content: center;
  border: 3rpx solid rgba(255, 255, 255, 0.3);
}
.avatar text {
  font-size: 56rpx;
  color: #FFFFFF;
  font-weight: 600;
}
.info {
  flex: 1;
  min-width: 0;
}
.name {
  display: block;
  font-size: 38rpx;
  font-weight: 600;
  color: #FFFFFF;
  margin-bottom: 10rpx;
}
.role-pill {
  display: inline-block;
  padding: 6rpx 16rpx;
  background: rgba(255, 255, 255, 0.25);
  backdrop-filter: blur(10rpx);
  border-radius: var(--radius-full);
  border: 1rpx solid rgba(255, 255, 255, 0.3);
}
.role-text {
  font-size: 22rpx;
  color: #FFFFFF;
  font-weight: 500;
}
.tenant-tag {
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 8rpx 16rpx;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10rpx);
  border-radius: var(--radius-full);
  border: 1rpx solid rgba(255, 255, 255, 0.3);
}
.tenant-icon { font-size: 22rpx; }
.tenant-text { font-size: 20rpx; color: #FFFFFF; }

/* 数据概览 */
.stats-card {
  display: flex;
  align-items: center;
  margin: -56rpx 24rpx 32rpx;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-xl);
  padding: 32rpx 0;
  box-shadow: var(--shadow-md);
  position: relative;
  z-index: 2;
}
.stat-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6rpx;
}
.stat-num {
  font-size: 40rpx;
  font-weight: 700;
  color: var(--color-text-primary);
  line-height: 1.2;
}
.stat-num .unit {
  font-size: 22rpx;
  font-weight: 500;
  color: var(--color-text-tertiary);
  margin-left: 2rpx;
}
.stat-label {
  font-size: 22rpx;
  color: var(--color-text-tertiary);
}
.stat-divider {
  width: 1rpx;
  height: 56rpx;
  background: var(--color-divider);
}

/* 菜单 */
.menu-group {
  margin: 0 24rpx 32rpx;
}
.group-title {
  display: block;
  font-size: 22rpx;
  color: var(--color-text-tertiary);
  font-weight: 500;
  margin-bottom: 12rpx;
  padding: 0 8rpx;
  text-transform: uppercase;
  letter-spacing: 1rpx;
}
.menu-card {
  background: var(--color-bg-elevated);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xs);
  overflow: hidden;
}
.menu-item {
  display: flex;
  align-items: center;
  gap: 20rpx;
  padding: 24rpx;
  border-bottom: 1rpx solid var(--color-divider-light);
  transition: background 0.2s;
}
.menu-item:active {
  background: var(--color-bg-muted);
}
.menu-item.last {
  border-bottom: none;
}
.menu-icon {
  flex-shrink: 0;
  width: 72rpx;
  height: 72rpx;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 36rpx;
}
.menu-content {
  flex: 1;
  min-width: 0;
}
.menu-label {
  display: block;
  font-size: 28rpx;
  color: var(--color-text-primary);
  font-weight: 500;
}
.menu-desc {
  display: block;
  font-size: 22rpx;
  color: var(--color-text-tertiary);
  margin-top: 4rpx;
}
.menu-arrow {
  flex-shrink: 0;
  width: 32rpx;
  text-align: center;
  color: var(--color-text-placeholder);
  font-size: 36rpx;
  line-height: 1;
}

/* 退出按钮 */
.logout-btn {
  margin: 48rpx 24rpx 24rpx;
  height: 88rpx;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: var(--shadow-xs);
  transition: all 0.2s;
}
.logout-btn text {
  font-size: 28rpx;
  color: var(--color-danger);
  font-weight: 500;
}
.logout-btn:active {
  background: var(--color-danger-soft);
  transform: scale(0.99);
}

/* 页脚 */
.footer {
  display: block;
  text-align: center;
  font-size: 22rpx;
  color: var(--color-text-placeholder);
  margin-top: 16rpx;
}
</style>
