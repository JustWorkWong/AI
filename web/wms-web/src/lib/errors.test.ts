import { describe, expect, it } from "vitest";
import { toUserMessage } from "./errors";

describe("toUserMessage", () => {
  it("should return the original error message when the error is a real Error", () => {
    expect(toUserMessage(new Error("approval failed"), "审批失败")).toBe("approval failed");
  });

  it("should fall back to the provided message for unknown thrown values", () => {
    expect(toUserMessage("boom", "执行失败")).toBe("执行失败");
  });
});
