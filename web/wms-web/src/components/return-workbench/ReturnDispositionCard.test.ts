// @vitest-environment jsdom

import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import ReturnDispositionCard from "./ReturnDispositionCard.vue";

describe("ReturnDispositionCard", () => {
  it("should render execution state and emit approve action", async () => {
    const wrapper = mount(ReturnDispositionCard, {
      props: {
        suggestedOutcome: "Scrap",
        approvalStatus: "Pending",
        executionStatus: "WaitingForApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: null,
        errorMessage: "",
        isExecuting: false,
        isApproving: false,
        canApprove: true
      }
    });

    expect(wrapper.text()).toContain("建议结果: Scrap");
    expect(wrapper.text()).toContain("执行状态: WaitingForApproval");
    expect(wrapper.text()).toContain("审批单: 33333333-3333-3333-3333-333333333333");

    const buttons = wrapper.findAll("button");
    await buttons[1].trigger("click");

    expect(wrapper.emitted("approve")).toHaveLength(1);
  });
});
