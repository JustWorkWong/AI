<template>
  <article class="panel">
    <h3>AI 建议</h3>
    <p>建议结果: {{ suggestedOutcome }}</p>
    <p>审批状态: {{ approvalStatus }}</p>
    <p v-if="executionStatus">执行状态: {{ executionStatus }}</p>
    <p v-if="approvalReferenceId">审批单: {{ approvalReferenceId }}</p>
    <p v-if="outcome">落单结果: {{ outcome }}</p>
    <p v-if="errorMessage" class="error-text">{{ errorMessage }}</p>
    <div class="toolbar">
      <button :disabled="isExecuting" @click="$emit('execute')">
        {{ isExecuting ? "执行中..." : "执行处置" }}
      </button>
      <button
        v-if="canApprove"
        class="secondary"
        :disabled="isApproving"
        @click="$emit('approve')"
      >
        {{ isApproving ? "处理中..." : "审批通过" }}
      </button>
      <button
        v-if="canApprove"
        class="secondary danger"
        :disabled="isApproving"
        @click="$emit('reject')"
      >
        {{ isApproving ? "处理中..." : "驳回" }}
      </button>
      <button class="secondary">人工覆写</button>
    </div>
  </article>
</template>

<script setup lang="ts">
defineProps<{
  suggestedOutcome?: string;
  approvalStatus?: string;
  executionStatus?: string;
  approvalReferenceId?: string | null;
  outcome?: string | null;
  errorMessage?: string;
  isExecuting: boolean;
  isApproving: boolean;
  canApprove: boolean;
}>();

defineEmits<{
  execute: [];
  approve: [];
  reject: [];
}>();
</script>
