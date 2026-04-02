import { computed, ref } from "vue";
import {
  decideDispositionApproval,
  executeDisposition,
  getDispositionTrace,
  getReturnWorkbench,
  type ApprovalDecisionRequest,
  type DispositionExecutionResultDto,
  type DispositionExecutionTraceDto,
  type DispositionSuggestionDto,
  type ReturnOrderDto,
  type ReturnWorkbenchViewDto
} from "../lib/api";

export interface ReturnWorkbenchApi {
  getReturnWorkbench(returnOrderId: string): Promise<ReturnWorkbenchViewDto>;
  executeDisposition(returnOrderId: string, idempotencyKey: string): Promise<DispositionExecutionResultDto>;
  getDispositionTrace(workflowInstanceId: string): Promise<DispositionExecutionTraceDto>;
  decideDispositionApproval(
    workflowInstanceId: string,
    request: ApprovalDecisionRequest
  ): Promise<DispositionExecutionResultDto>;
}

const defaultApi: ReturnWorkbenchApi = {
  getReturnWorkbench,
  executeDisposition,
  getDispositionTrace,
  decideDispositionApproval
};

export function createReturnWorkbench(api: ReturnWorkbenchApi = defaultApi) {
  const order = ref<ReturnOrderDto | null>(null);
  const suggestion = ref<DispositionSuggestionDto | null>(null);
  const executionResult = ref<DispositionExecutionResultDto | null>(null);
  const executionTrace = ref<DispositionExecutionTraceDto | null>(null);
  const isExecuting = ref(false);
  const isApproving = ref(false);
  const errorMessage = ref("");

  const canApprove = computed(() =>
    executionResult.value?.status === "WaitingForApproval" &&
    executionResult.value.approvalReferenceId !== null
  );

  async function load(returnOrderId: string) {
    const payload = await api.getReturnWorkbench(returnOrderId);
    order.value = payload.order;
    suggestion.value = payload.suggestion;
  }

  async function execute(returnOrderId: string, idempotencyKey: string) {
    isExecuting.value = true;
    errorMessage.value = "";

    try {
      const result = await api.executeDisposition(returnOrderId, idempotencyKey);
      await syncResult(result);
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : "执行失败";
    } finally {
      isExecuting.value = false;
    }
  }

  async function decideApproval(action: ApprovalDecisionRequest["action"], actor: string) {
    if (!executionResult.value) {
      return;
    }

    isApproving.value = true;
    errorMessage.value = "";

    try {
      const result = await api.decideDispositionApproval(executionResult.value.workflowInstanceId, {
        action,
        actor
      });

      await syncResult(result);
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : "审批失败";
    } finally {
      isApproving.value = false;
    }
  }

  async function syncResult(result: DispositionExecutionResultDto) {
    executionResult.value = result;
    executionTrace.value = await api.getDispositionTrace(result.workflowInstanceId);

    if (suggestion.value) {
      suggestion.value = {
        ...suggestion.value,
        approvalStatus:
          result.status === "WaitingForApproval"
            ? "Pending"
            : result.status === "Rejected"
              ? "Rejected"
              : "Completed",
        suggestedOutcome: result.outcome ?? suggestion.value.suggestedOutcome
      };
    }

    if (order.value && result.status === "Completed") {
      order.value = {
        ...order.value,
        status: "Disposed"
      };
    }
  }

  return {
    order,
    suggestion,
    executionResult,
    executionTrace,
    isExecuting,
    isApproving,
    errorMessage,
    canApprove,
    load,
    execute,
    decideApproval
  };
}
