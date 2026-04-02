<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div>
        <p class="eyebrow">WMS AI</p>
        <h1 class="brand">Operations Console</h1>
      </div>

      <nav class="nav">
        <RouterLink to="/">工作台</RouterLink>
        <RouterLink to="/returns/11111111-1111-1111-1111-111111111111">退货质检</RouterLink>
        <RouterLink to="/sop/22222222-2222-2222-2222-222222222222">SOP 辅助</RouterLink>
      </nav>
    </aside>

    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
</script>
