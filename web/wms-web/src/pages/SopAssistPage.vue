<template>
  <section class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow">SOP Session</p>
        <h2>SOP 辅助执行</h2>
        <p>页面同时看步骤、引用和实时事件，不用切多个窗口。</p>
      </div>
      <span class="badge">{{ model.currentStepCode }}</span>
    </header>

    <section class="panel-grid">
      <article class="panel">
        <h3>当前步骤</h3>
        <p>Operation: {{ model.operationCode }}</p>
        <p>Step: {{ model.currentStepCode }}</p>
        <div class="toolbar">
          <button @click="advance">推进一步</button>
          <button class="secondary">人工接管</button>
        </div>
      </article>

      <article class="panel">
        <h3>实时事件</h3>
        <ul class="event-list">
          <li v-for="event in model.events" :key="event">{{ event }}</li>
        </ul>
      </article>
    </section>

    <article class="panel">
      <h3>引用</h3>
      <ul class="citation-list">
        <li v-for="citation in model.citations" :key="citation.sourceId">
          {{ citation.version }} / {{ citation.snippet }}
        </li>
      </ul>
    </article>
  </section>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive } from "vue";
import { useRoute } from "vue-router";
import { subscribeToSopSession, type AgUiEvent } from "../lib/agui";

interface CitationDto {
  sourceId: string;
  version: string;
  snippet: string;
}

interface SopExecutionViewDto {
  sessionId: string;
  operationCode: string;
  currentStepCode: string;
  citations: CitationDto[];
  requiresAcknowledgement: boolean;
}

const route = useRoute();
const model = reactive({
  operationCode: "RETURNS",
  currentStepCode: "STEP-01",
  citations: [] as CitationDto[],
  events: [] as string[]
});

let source: EventSource | null = null;

async function advance() {
  const response = await fetch(`/api/sop/sessions/${route.params.sessionId}/steps`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ stepCode: "STEP-02", userInput: "confirmed" })
  });

  const payload = (await response.json()) as SopExecutionViewDto;
  model.operationCode = payload.operationCode;
  model.currentStepCode = payload.currentStepCode;
  model.citations = payload.citations;
}

onMounted(() => {
  source = subscribeToSopSession(String(route.params.sessionId), (event: AgUiEvent) => {
    model.events.unshift(`${event.type} / ${event.traceId}`);
    model.events.splice(8);
  });
});

onBeforeUnmount(() => {
  source?.close();
});
</script>
