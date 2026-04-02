# Services

## 文件
- `ReturnDispositionAdvisor.cs`: 聚合退货单、案例与 SOP 证据，生成建议视图，并保留 tool 证据链。
- `ReturnDispositionExecutor.cs`: 运行退货处置 workflow，负责审批或落单，并持久化执行状态。
- `ReturnDispositionTraceReader.cs`: 聚合 workflow、tool timeline 与 checkpoint，供工作台展示执行轨迹。
- `ReturnDispositionApprovalService.cs`: 在审批通过或拒绝后恢复等待中的处置 workflow。
- `SopAssistService.cs`: 用真实 workflow 串联候选、切块、重排与 checkpoint，生成 SOP 执行视图。

## 原则
- service 负责编排 IO 与视图组装，不篡改业务真相。
