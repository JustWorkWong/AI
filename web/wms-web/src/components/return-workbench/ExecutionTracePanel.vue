<template>
  <section class="panel-grid">
    <article class="panel">
      <h3>Tool Timeline</h3>
      <ul class="citation-list">
        <li v-for="tool in toolInvocations" :key="tool.toolInvocationId">
          {{ tool.toolName }} / {{ tool.status }} / {{ tool.durationMs }} ms
        </li>
      </ul>
    </article>

    <article class="panel">
      <h3>Checkpoints</h3>
      <ul class="citation-list">
        <li v-for="checkpoint in checkpoints" :key="checkpoint.checkpointId">
          step {{ checkpoint.superstep }} / {{ checkpoint.checkpointType }} / {{ checkpoint.stateJson }}
        </li>
      </ul>
    </article>
  </section>
</template>

<script setup lang="ts">
import type { ToolInvocationDto, WorkflowCheckpointDto } from "../../lib/api";

defineProps<{
  toolInvocations: ToolInvocationDto[];
  checkpoints: WorkflowCheckpointDto[];
}>();
</script>
