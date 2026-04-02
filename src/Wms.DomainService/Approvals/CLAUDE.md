# Approvals

## 文件
- `ApprovalTask.cs`: 审批任务真相源。
- `ApprovalAction.cs`: 审批动作留痕。

## 原则
- 审批真相在领域服务，不在 runtime。
- 审批状态迁移由领域实体自己收口，不让端点到处改字符串。
