# Shared.Contracts

## 文件
- `Common/`: 跨流程都会复用的轻量契约。
- `Returns/`: 退货质检与处置相关 DTO。
- `Sop/`: SOP 辅助执行相关 DTO。
- `Approvals/`: 审批与用户同步契约。

## 原则
- 这里只放契约，不放默认值，不放客户端实现。
- DTO 服务于边界稳定，别把领域对象直接泄漏出来。
