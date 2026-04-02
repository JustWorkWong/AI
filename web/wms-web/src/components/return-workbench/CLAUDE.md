# return-workbench

## 文件
- `ReturnDispositionCard.vue`: 展示 AI 建议、执行状态、审批按钮与错误信息。
- `ReturnDispositionCard.test.ts`: 校验卡片的展示与事件抛出边界。
- `ExecutionTracePanel.vue`: 展示 tool timeline 与 checkpoint 列表。
- `ExecutionTracePanel.test.ts`: 校验轨迹面板的展示边界。

## 原则
- 卡片不拥有业务状态，只消费 props 并通过 emit 反馈用户动作。
