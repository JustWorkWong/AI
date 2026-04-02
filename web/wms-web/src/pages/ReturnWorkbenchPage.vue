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
          <button
            v-if="canApprove"
            class="secondary"
            :disabled="isApproving"
            @click="decideApproval('Approve')"
          >
            {{ isApproving ? "处理中..." : "审批通过" }}
          </button>
          <button
            v-if="canApprove"
            class="secondary danger"
            :disabled="isApproving"
            @click="decideApproval('Reject')"
          >
            {{ isApproving ? "处理中..." : "驳回" }}
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

    <section v-if="executionTrace" class="panel-grid">
      <article class="panel">
        <h3>Tool Timeline</h3>
        <ul class="citation-list">
          <li v-for="tool in executionTrace.toolInvocations" :key="tool.toolInvocationId">
            {{ tool.toolName }} / {{ tool.status }} / {{ tool.durationMs }} ms
          </li>
        </ul>
      </article>

      <article class="panel">
        <h3>Checkpoints</h3>
        <ul class="citation-list">
          <li v-for="checkpoint in executionTrace.checkpoints" :key="checkpoint.checkpointId">
            step {{ checkpoint.superstep }} / {{ checkpoint.checkpointType }} / {{ checkpoint.stateJson }}
          </li>
        </ul>
      </article>
    </section>
  </section>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import { useRoute } from "vue-router";
import { createReturnWorkbench } from "../composables/useReturnWorkbench";

const route = useRoute();
const approvalActor = "manager-web";
const {
  order,
  suggestion,
  executionResult,
  executionTrace,
  isExecuting,
  isApproving,
  errorMessage,
  canApprove,
  load,
  execute: runDisposition,
  decideApproval: runApprovalDecision
} = createReturnWorkbench();

async function execute() {
  await runDisposition(String(route.params.id), `web-${crypto.randomUUID()}`);
}

async function decideApproval(action: "Approve" | "Reject") {
  await runApprovalDecision(action, approvalActor);
}

onMounted(async () => {
  await load(String(route.params.id));
});
</script>
