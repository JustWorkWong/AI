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

      <ReturnDispositionCard
        :suggested-outcome="suggestion?.suggestedOutcome"
        :approval-status="suggestion?.approvalStatus"
        :execution-status="executionResult?.status"
        :approval-reference-id="executionResult?.approvalReferenceId"
        :outcome="executionResult?.outcome"
        :error-message="errorMessage"
        :is-executing="isExecuting"
        :is-approving="isApproving"
        :can-approve="canApprove"
        @execute="execute"
        @approve="decideApproval('Approve')"
        @reject="decideApproval('Reject')"
      />
    </section>

    <article class="panel">
      <h3>引用证据</h3>
      <ul class="citation-list">
        <li v-for="citation in suggestion?.citations ?? []" :key="citation.sourceId">
          {{ citation.sourceType }} / {{ citation.sourceId }} / {{ citation.version }} / {{ citation.snippet }}
        </li>
      </ul>
    </article>

    <ExecutionTracePanel
      v-if="executionTrace"
      :tool-invocations="executionTrace.toolInvocations"
      :checkpoints="executionTrace.checkpoints"
    />
  </section>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import { useRoute } from "vue-router";
import ExecutionTracePanel from "../components/return-workbench/ExecutionTracePanel.vue";
import ReturnDispositionCard from "../components/return-workbench/ReturnDispositionCard.vue";
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
