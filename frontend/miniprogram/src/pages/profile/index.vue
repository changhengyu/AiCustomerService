<script setup lang="ts">
import { ref } from 'vue';

const user = ref(uni.getStorageSync('user') || {});

function logout() {
  uni.clearStorageSync();
  uni.reLaunch({ url: '/pages/login/index' });
}
</script>

<template>
  <view class="profile">
    <view class="avatar">👤</view>
    <view class="name">{{ user.display_name || user.username }}</view>
    <view class="role">{{ user.role }}</view>

    <view class="menu">
      <view class="item">租户：{{ user.tenant_id }}</view>
      <view class="item">设置</view>
      <view class="item">关于</view>
    </view>

    <button @click="logout" class="logout">退出登录</button>
  </view>
</template>

<style scoped>
.profile { padding: 60rpx 40rpx; }
.avatar { width: 140rpx; height: 140rpx; border-radius: 50%; background: #f0f0f0; text-align: center; line-height: 140rpx; font-size: 80rpx; margin: 0 auto; }
.name { text-align: center; font-size: 36rpx; margin-top: 20rpx; }
.role { text-align: center; color: #999; margin-top: 8rpx; }
.menu { margin-top: 60rpx; }
.item { padding: 32rpx; background: #fff; border-bottom: 1rpx solid #f0f0f0; }
.logout { width: 100%; margin-top: 60rpx; background: #ff4d4f; color: #fff; }
</style>