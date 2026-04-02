# Endpoints

## 文件
- `DispositionEndpoints.cs`: 处置审批申请与最终落单端点。
- `AttachmentEndpoints.cs`: 退货附件上传与对象存储边界。
- `ReturnReadEndpoints.cs`: 退货单视图与历史案例读取。
- `SopReadEndpoints.cs`: SOP 候选与切块读取。

## 原则
- 端点只收命令和协调持久化，不持有业务规则。
