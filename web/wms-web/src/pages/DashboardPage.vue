<template>
  <section class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Overview</p>
        <h2>仓内 AI 工作台</h2>
        <p>聚合审批积压、runtime 异常和两条核心流程入口。</p>
      </div>
      <span class="badge">Today</span>
    </header>

    <section class="stat-grid">
      <article class="stat-card">
        <span>待审批</span>
        <strong>{{ stats.pendingApprovals }}</strong>
      </article>
      <article class="stat-card">
        <span>Runtime 失败</span>
        <strong>{{ stats.runtimeFailures }}</strong>
      </article>
      <article class="stat-card">
        <span>活跃流程</span>
        <strong>{{ stats.activeSessions }}</strong>
      </article>
    </section>

    <section class="panel-grid">
      <article class="panel">
        <p class="eyebrow">Returns</p>
        <h3>退货质检与处置</h3>
        <p>聚合退货单、建议结果、审批状态和引用证据。</p>
        <RouterLink class="badge" to="/returns/11111111-1111-1111-1111-111111111111">进入工作台</RouterLink>
      </article>

      <article class="panel">
        <p class="eyebrow">SOP</p>
        <h3>SOP 辅助执行</h3>
        <p>按步骤推进，并实时接收 heartbeat / tool / citation 事件。</p>
        <RouterLink class="badge" to="/sop/22222222-2222-2222-2222-222222222222">进入会话</RouterLink>
      </article>
    </section>
  </section>
</template>

<script setup lang="ts">
const stats = {
  pendingApprovals: 3,
  runtimeFailures: 1,
  activeSessions: 2
};
</script>
