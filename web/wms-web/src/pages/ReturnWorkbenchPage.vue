<template>
  <section class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Return Workbench</p>
        <h2>退货质检与处置</h2>
        <p>订单、建议、审批与证据在一个页面里闭环。</p>
      </div>
      <span :class="['badge', suggestion?.riskLevel === 'High' ? 'warn' : '']">
        {{ suggestion?.riskLevel ?? "Loading" }}
      </span>
    </header>

    <section class="panel-grid">
      <article class="panel">
        <h3>退货单</h3>
        <p>单号: {{ order?.returnCode }}</p>
        <p>状态: {{ order?.status }}</p>
        <p>质检结论: {{ order?.qualityState }}</p>
        <p>备注: {{ order?.notes }}</p>
      </article>

      <article class="panel">
        <h3>AI 建议</h3>
        <p>建议结果: {{ suggestion?.suggestedOutcome }}</p>
        <p>审批状态: {{ suggestion?.approvalStatus }}</p>
        <div class="toolbar">
          <button>提交审批</button>
          <button class="secondary">人工覆写</button>
        </div>
      </article>
    </section>

    <article class="panel">
      <h3>引用证据</h3>
      <ul class="citation-list">
        <li v-for="citation in suggestion?.citations ?? []" :key="citation.sourceId">
          {{ citation.sourceType }} / {{ citation.sourceId }} / {{ citation.version }} / {{ citation.snippet }}
        </li>
      </ul>
    </article>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRoute } from "vue-router";

interface CitationDto {
  sourceType: string;
  sourceId: string;
  version: string;
  snippet: string;
}

interface ReturnOrderDto {
  returnOrderId: string;
  returnCode: string;
  qualityState: string;
  status: string;
  notes: string;
}

interface DispositionSuggestionDto {
  returnOrderId: string;
  suggestedOutcome: string;
  riskLevel: string;
  citations: CitationDto[];
  approvalStatus: string;
}

interface ReturnWorkbenchViewDto {
  order: ReturnOrderDto;
  suggestion: DispositionSuggestionDto;
}

const route = useRoute();
const order = ref<ReturnOrderDto | null>(null);
const suggestion = ref<DispositionSuggestionDto | null>(null);

onMounted(async () => {
  const response = await fetch(`/api/returns/workbench/${route.params.id}`);
  const payload = (await response.json()) as ReturnWorkbenchViewDto;
  order.value = payload.order;
  suggestion.value = payload.suggestion;
});
</script>
