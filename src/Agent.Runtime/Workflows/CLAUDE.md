# Workflows

## 结构
```text
Workflows/
├─ RuntimeContext.cs              # workflow 运行上下文，统一工具调用、生成与 checkpoint
├─ WorkflowModels.cs              # 两条 workflow 的输入输出模型与 tool 输入契约
├─ ReturnDispositionWorkflow.cs   # 退货质检与处置主流程
└─ SopAssistWorkflow.cs           # SOP 辅助执行主流程
```

## 原则
- workflow 只编排步骤，不吞业务真相。
- 审批前与步骤切换前必须显式 checkpoint。
- 无证据时降级到人工，不让模型编故事。
