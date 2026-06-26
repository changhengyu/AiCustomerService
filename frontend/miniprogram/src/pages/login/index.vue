<script setup lang="ts">
import { ref } from 'vue';
import { authApi } from '@/api';
import { realtime } from '@/realtime';

const form = ref({
  username: '',
  password: '',
  tenantId: ''
});
const loading = ref(false);
const errorMsg = ref('');
const rememberMe = ref(true);

async function submit() {
  if (!form.value.username || !form.value.password) {
    errorMsg.value = '请填写用户名和密码';
    return;
  }
  loading.value = true;
  errorMsg.value = '';
  try {
    const r = await authApi.login(form.value);
    uni.setStorageSync('access_token', r.access_token);
    uni.setStorageSync('refresh_token', r.refresh_token);
    uni.setStorageSync('user', r.user);
    // 登录成功后建立实时连接，让会话列表与详情页能收到推送
    realtime.connect();
    uni.switchTab({ url: '/pages/index/index' });
  } catch (e: any) {
    errorMsg.value = e?.message || '登录失败，请检查账号信息';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <view class="login-page">
    <!-- 顶部品牌区 -->
    <view class="brand-section">
      <view class="logo-mark">
        <view class="logo-icon">
          <text class="logo-glyph">AI</text>
        </view>
        <view class="logo-meta">
          <text class="logo-title">Inbox Co-Pilot</text>
          <text class="logo-subtitle">智能客服工作台</text>
        </view>
      </view>
    </view>

    <!-- 主卡片 -->
    <view class="login-card">
      <view class="card-head">
        <text class="card-title">欢迎回来</text>
        <text class="card-desc">登录以继续管理客户会话</text>
      </view>

      <view class="form-stack">
        <view class="field">
          <text class="field-label">租户 ID</text>
          <view class="field-input">
            <text class="field-icon">🏢</text>
            <input v-model="form.tenantId"
                   placeholder="请输入租户标识"
                   placeholder-class="field-placeholder" />
          </view>
        </view>

        <view class="field">
          <text class="field-label">账号</text>
          <view class="field-input">
            <text class="field-icon">👤</text>
            <input v-model="form.username"
                   placeholder="用户名或邮箱"
                   placeholder-class="field-placeholder" />
          </view>
        </view>

        <view class="field">
          <text class="field-label">密码</text>
          <view class="field-input">
            <text class="field-icon">🔒</text>
            <input v-model="form.password"
                   password
                   placeholder="输入密码"
                   placeholder-class="field-placeholder" />
          </view>
        </view>

        <view class="field-row">
          <view class="checkbox" @click="rememberMe = !rememberMe">
            <view class="checkbox-box" :class="{ checked: rememberMe }">
              <text v-if="rememberMe" class="checkbox-tick">✓</text>
            </view>
            <text class="checkbox-label">记住我</text>
          </view>
          <text class="link">忘记密码</text>
        </view>

        <view v-if="errorMsg" class="error-banner">
          <text class="error-icon">⚠</text>
          <text class="error-text">{{ errorMsg }}</text>
        </view>

        <button class="btn-primary submit-btn"
                :loading="loading"
                :disabled="loading"
                @click="submit">
          {{ loading ? '正在登录' : '登录工作台' }}
        </button>

        <view class="divider-row">
          <view class="divider-line"></view>
          <text class="divider-text">或</text>
          <view class="divider-line"></view>
        </view>

        <view class="alt-actions">
          <text class="link-strong">企业 SSO 登录</text>
          <text class="link-muted">新租户注册</text>
        </view>
      </view>
    </view>

    <!-- 底部信任标识 -->
    <view class="trust-strip">
      <view class="trust-item">
        <text class="trust-dot"></text>
        <text class="trust-label">端到端加密</text>
      </view>
      <view class="trust-item">
        <text class="trust-dot"></text>
        <text class="trust-label">SOC 2 合规</text>
      </view>
      <view class="trust-item">
        <text class="trust-dot"></text>
        <text class="trust-label">99.95% 可用</text>
      </view>
    </view>

    <text class="footer-note">© 2026 Inbox Co-Pilot · v1.4.2</text>
  </view>
</template>

<style lang="scss" scoped>
@import '../../style/theme.scss';

.login-page {
  min-height: 100vh;
  padding: 80rpx 48rpx 60rpx;
  background:
    radial-gradient(circle at 20% 0%, rgba(79, 110, 247, 0.08), transparent 50%),
    radial-gradient(circle at 100% 100%, rgba(16, 185, 129, 0.06), transparent 50%),
    var(--color-bg);
  display: flex;
  flex-direction: column;
  align-items: stretch;
}

/* 品牌区 */
.brand-section {
  margin-bottom: 56rpx;
}
.logo-mark {
  display: flex;
  align-items: center;
  gap: 20rpx;
}
.logo-icon {
  width: 72rpx;
  height: 72rpx;
  border-radius: var(--radius-lg);
  background: linear-gradient(135deg, var(--color-primary), #6B8AF9);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8rpx 24rpx rgba(79, 110, 247, 0.32);
}
.logo-glyph {
  font-size: 26rpx;
  font-weight: 700;
  color: var(--color-text-inverse);
  letter-spacing: -0.5px;
}
.logo-meta {
  display: flex;
  flex-direction: column;
}
.logo-title {
  font-size: 32rpx;
  font-weight: 600;
  color: var(--color-text-primary);
  letter-spacing: -0.3px;
}
.logo-subtitle {
  font-size: var(--font-sm);
  color: var(--color-text-tertiary);
  margin-top: 4rpx;
}

/* 主卡片 */
.login-card {
  background: var(--color-bg-elevated);
  border-radius: var(--radius-2xl);
  padding: 48rpx 40rpx;
  box-shadow: var(--shadow-lg);
  border: 1px solid rgba(255, 255, 255, 0.6);
}

.card-head {
  margin-bottom: 40rpx;
}
.card-title {
  display: block;
  font-size: 40rpx;
  font-weight: 600;
  color: var(--color-text-primary);
  letter-spacing: -0.5px;
}
.card-desc {
  display: block;
  font-size: var(--font-md);
  color: var(--color-text-secondary);
  margin-top: 8rpx;
}

/* 表单 */
.form-stack {
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.field-label {
  font-size: var(--font-sm);
  font-weight: 500;
  color: var(--color-text-secondary);
  margin-left: 4rpx;
}
.field-input {
  display: flex;
  align-items: center;
  gap: 16rpx;
  background: var(--color-bg-muted);
  border: 1px solid transparent;
  border-radius: var(--radius-lg);
  padding: 24rpx 24rpx;
  transition: all 0.18s ease;

  &:focus-within {
    background: var(--color-bg-elevated);
    border-color: var(--color-primary);
    box-shadow: var(--shadow-focus);
  }
}
.field-icon {
  font-size: 24rpx;
  opacity: 0.7;
}
.field-input input {
  flex: 1;
  font-size: var(--font-md);
  color: var(--color-text-primary);
  background: transparent;
}
.field-placeholder {
  color: var(--color-text-placeholder);
}

.field-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 4rpx;
}
.checkbox {
  display: flex;
  align-items: center;
  gap: 12rpx;
}
.checkbox-box {
  width: 32rpx;
  height: 32rpx;
  border-radius: 8rpx;
  border: 1.5px solid var(--color-divider);
  background: var(--color-bg-elevated);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
}
.checkbox-box.checked {
  background: var(--color-primary);
  border-color: var(--color-primary);
}
.checkbox-tick {
  color: var(--color-text-inverse);
  font-size: 20rpx;
  font-weight: 700;
  line-height: 1;
}
.checkbox-label {
  font-size: var(--font-sm);
  color: var(--color-text-secondary);
}
.link {
  font-size: var(--font-sm);
  color: var(--color-primary);
  font-weight: 500;
}

.error-banner {
  display: flex;
  align-items: center;
  gap: 12rpx;
  background: var(--color-danger-soft);
  color: var(--color-danger);
  padding: 18rpx 20rpx;
  border-radius: var(--radius-md);
  font-size: var(--font-sm);
}
.error-icon { font-size: 24rpx; }
.error-text { flex: 1; }

.submit-btn {
  height: 88rpx;
  font-size: var(--font-lg);
  font-weight: 600;
  letter-spacing: 0.2px;
  margin-top: 16rpx;
  box-shadow: 0 8rpx 20rpx rgba(79, 110, 247, 0.28);
}

.divider-row {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin: 8rpx 0;
}
.divider-line {
  flex: 1;
  height: 1px;
  background: var(--color-divider);
}
.divider-text {
  font-size: var(--font-xs);
  color: var(--color-text-tertiary);
  letter-spacing: 1px;
}

.alt-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.link-strong {
  font-size: var(--font-sm);
  color: var(--color-primary);
  font-weight: 600;
}
.link-muted {
  font-size: var(--font-sm);
  color: var(--color-text-tertiary);
}

/* 信任标识 */
.trust-strip {
  display: flex;
  justify-content: center;
  gap: 32rpx;
  margin-top: 48rpx;
  flex-wrap: wrap;
}
.trust-item {
  display: flex;
  align-items: center;
  gap: 8rpx;
}
.trust-dot {
  width: 12rpx;
  height: 12rpx;
  border-radius: 50%;
  background: var(--color-success);
  box-shadow: 0 0 0 4rpx rgba(16, 185, 129, 0.16);
}
.trust-label {
  font-size: var(--font-xs);
  color: var(--color-text-tertiary);
}

.footer-note {
  display: block;
  text-align: center;
  font-size: var(--font-xs);
  color: var(--color-text-tertiary);
  margin-top: 32rpx;
  letter-spacing: 0.3px;
}
</style>