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
        <p v-if="executionResult">执行状态: {{ executionResult.status }}</p>
        <p v-if="executionResult?.approvalReferenceId">审批单: {{ executionResult.approvalReferenceId }}</p>
        <p v-if="executionResult?.outcome">落单结果: {{ executionResult.outcome }}</p>
        <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>
        <div class="toolbar">
          <button :disabled="isExecuting" @click="execute">
            {{ isExecuting ? "执行中..." : "执行处置" }}
          </button>
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
import {
  executeDisposition,
  getReturnWorkbench,
  type DispositionExecutionResultDto,
  type DispositionSuggestionDto,
  type ReturnOrderDto
} from "../lib/api";

const route = useRoute();
const order = ref<ReturnOrderDto | null>(null);
const suggestion = ref<DispositionSuggestionDto | null>(null);
const executionResult = ref<DispositionExecutionResultDto | null>(null);
const isExecuting = ref(false);
const errorMessage = ref("");

async function loadWorkbench() {
  const payload = await getReturnWorkbench(String(route.params.id));
  order.value = payload.order;
  suggestion.value = payload.suggestion;
}

async function execute() {
  isExecuting.value = true;
  errorMessage.value = "";

  try {
    const result = await executeDisposition(
      String(route.params.id),
      `web-${crypto.randomUUID()}`
    );

    executionResult.value = result;

    if (suggestion.value) {
      suggestion.value = {
        ...suggestion.value,
        approvalStatus: result.status === "WaitingForApproval" ? "Pending" : "Completed",
        suggestedOutcome: result.outcome ?? suggestion.value.suggestedOutcome
      };
    }

    if (order.value && result.status === "Completed") {
      order.value = {
        ...order.value,
        status: "Disposed"
      };
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : "执行失败";
  } finally {
    isExecuting.value = false;
  }
}

onMounted(async () => {
  await loadWorkbench();
});
</script>
