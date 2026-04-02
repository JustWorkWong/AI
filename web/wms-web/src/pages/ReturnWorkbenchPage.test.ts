// @vitest-environment jsdom

import { computed, nextTick, ref } from "vue";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { mount } from "@vue/test-utils";
import ReturnWorkbenchPage from "./ReturnWorkbenchPage.vue";

const mockLoad = vi.fn(async () => {});
const mockExecute = vi.fn(async () => {});
const mockDecideApproval = vi.fn(async (_action: "Approve" | "Reject", _actor: string) => {});

const order = ref({
  returnOrderId: "11111111-1111-1111-1111-111111111111",
  returnCode: "RMA-001",
  qualityState: "Broken",
  status: "PendingInspection",
  notes: "Damaged shell"
});

const suggestion = ref({
  returnOrderId: "11111111-1111-1111-1111-111111111111",
  suggestedOutcome: "Scrap",
  riskLevel: "High",
  citations: [],
  approvalStatus: "Pending"
});

const executionResult = ref<{
  workflowInstanceId: string;
  status: string;
  approvalReferenceId: string | null;
  outcome: string | null;
} | null>(null);

const executionTrace = ref({
  workflowInstanceId: "22222222-2222-2222-2222-222222222222",
  workflowCode: "return-disposition-execute",
  status: "WaitingApproval",
  approvalReferenceId: "33333333-3333-3333-3333-333333333333",
  toolInvocations: [],
  checkpoints: []
});

const isExecuting = ref(false);
const isApproving = ref(false);
const errorMessage = ref("");
const canApprove = computed(
  () => executionResult.value?.status === "WaitingForApproval" && executionResult.value.approvalReferenceId !== null
);

vi.mock("vue-router", () => ({
  useRoute: () => ({
    params: {
      id: "11111111-1111-1111-1111-111111111111"
    }
  })
}));

vi.mock("../composables/useReturnWorkbench", () => ({
  createReturnWorkbench: () => ({
    order,
    suggestion,
    executionResult,
    executionTrace,
    isExecuting,
    isApproving,
    errorMessage,
    canApprove,
    load: mockLoad,
    execute: mockExecute,
    decideApproval: mockDecideApproval
  })
}));

describe("ReturnWorkbenchPage", () => {
  beforeEach(() => {
    mockLoad.mockClear();
    mockExecute.mockClear();
    mockDecideApproval.mockClear();

    order.value = {
      returnOrderId: "11111111-1111-1111-1111-111111111111",
      returnCode: "RMA-001",
      qualityState: "Broken",
      status: "PendingInspection",
      notes: "Damaged shell"
    };
    suggestion.value = {
      returnOrderId: "11111111-1111-1111-1111-111111111111",
      suggestedOutcome: "Scrap",
      riskLevel: "High",
      citations: [],
      approvalStatus: "Pending"
    };
    executionResult.value = null;
    errorMessage.value = "";

    mockExecute.mockImplementation(async () => {
      executionResult.value = {
        workflowInstanceId: "22222222-2222-2222-2222-222222222222",
        status: "WaitingForApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: null
      };
    });

    mockDecideApproval.mockImplementation(async () => {
      executionResult.value = {
        workflowInstanceId: "22222222-2222-2222-2222-222222222222",
        status: "Completed",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: "Scrap"
      };
      order.value = {
        ...order.value,
        status: "Disposed"
      };
      suggestion.value = {
        ...suggestion.value,
        approvalStatus: "Completed"
      };
    });
  });

  it("should wire execute and approve actions into the page", async () => {
    const wrapper = mount(ReturnWorkbenchPage);
    await nextTick();

    expect(mockLoad).toHaveBeenCalledWith("11111111-1111-1111-1111-111111111111");

    await wrapper.get("button").trigger("click");
    await nextTick();

    expect(mockExecute).toHaveBeenCalledTimes(1);
    expect(wrapper.text()).toContain("执行状态: WaitingForApproval");
    expect(wrapper.text()).toContain("审批单: 33333333-3333-3333-3333-333333333333");

    const approveButton = wrapper.findAll("button").find((button) => button.text() === "审批通过");
    expect(approveButton).toBeDefined();

    await approveButton!.trigger("click");
    await nextTick();

    expect(mockDecideApproval).toHaveBeenCalledWith("Approve", "manager-web");
    expect(wrapper.text()).toContain("状态: Disposed");
    expect(wrapper.text()).toContain("执行状态: Completed");
    expect(wrapper.text()).toContain("落单结果: Scrap");
  });
});
