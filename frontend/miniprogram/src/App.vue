<script setup lang="ts">
import { onLaunch } from '@dcloudio/uni-app';
onLaunch(() => {
  console.log('AI Customer Service Workbench');
});
</script>

<style lang="scss">
@import './style/theme.scss';

page {
  background: var(--color-bg);
  color: var(--color-text-primary);
  font-family: -apple-system, BlinkMacSystemFont, "PingFang SC", "Hiragino Sans GB",
               "Microsoft YaHei", "Helvetica Neue", Arial, sans-serif;
  font-size: var(--font-base);
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
}

/* 全局动效（克制） */
.fade-enter-active,
.fade-leave-active { transition: opacity 0.18s ease; }
.fade-enter-from,
.fade-leave-to { opacity: 0; }

/* 状态徽章 - 统一柔和配色 */
.tag {
  display: inline-flex;
  align-items: center;
  padding: 2px 10px;
  font-size: var(--font-xs);
  border-radius: var(--radius-sm);
  font-weight: 500;
  line-height: 1.6;
}
.tag-primary  { background: var(--color-primary-soft);  color: var(--color-primary); }
.tag-success  { background: var(--color-success-soft);  color: var(--color-success); }
.tag-warning  { background: var(--color-warning-soft);  color: var(--color-warning); }
.tag-danger   { background: var(--color-danger-soft);   color: var(--color-danger); }
.tag-info     { background: var(--color-info-soft);     color: var(--color-info); }
.tag-neutral  { background: var(--color-bg-muted);      color: var(--color-text-secondary); }

/* 通用卡片 */
.card {
  background: var(--color-bg-elevated);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xs);
}

/* 通用按钮 */
.btn-primary {
  background: var(--color-primary);
  color: var(--color-text-inverse);
  border-radius: var(--radius-md);
  font-weight: 500;
  transition: background 0.15s ease, transform 0.1s ease;
}
.btn-primary:active { background: var(--color-primary-pressed); transform: scale(0.98); }
.btn-secondary {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  border: 1px solid var(--color-divider);
  border-radius: var(--radius-md);
  font-weight: 500;
  transition: background 0.15s ease;
}
.btn-secondary:active { background: var(--color-bg-muted); }
.btn-ghost {
  background: transparent;
  color: var(--color-primary);
  border-radius: var(--radius-md);
  font-weight: 500;
}
</style>