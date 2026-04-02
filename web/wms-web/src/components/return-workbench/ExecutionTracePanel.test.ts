// @vitest-environment jsdom

import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import ExecutionTracePanel from "./ExecutionTracePanel.vue";

describe("ExecutionTracePanel", () => {
  it("should render tool timeline and checkpoints", () => {
    const wrapper = mount(ExecutionTracePanel, {
      props: {
        toolInvocations: [
          {
            toolInvocationId: "tool-1",
            toolName: "GetReturnOrderTool",
            status: "Completed",
            traceId: "trace-1",
            durationMs: 12,
            inputSummary: "{}",
            outputSummary: "order",
            errorMessage: null
          }
        ],
        checkpoints: [
          {
            checkpointId: "cp-1",
            superstep: 1,
            checkpointType: "approval",
            stateJson: "{\"approvalReferenceId\":\"333\"}"
          }
        ]
      }
    });

    expect(wrapper.text()).toContain("Tool Timeline");
    expect(wrapper.text()).toContain("GetReturnOrderTool / Completed / 12 ms");
    expect(wrapper.text()).toContain("Checkpoints");
    expect(wrapper.text()).toContain("step 1 / approval / {\"approvalReferenceId\":\"333\"}");
  });
});
