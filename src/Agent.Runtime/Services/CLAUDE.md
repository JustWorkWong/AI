# Services

## 文件
- `ReturnDispositionAdvisor.cs`: 聚合退货单、案例与 SOP 证据，生成建议视图。
- `SopAssistService.cs`: 串联候选、切块与证据排序，生成 SOP 执行视图。

## 原则
- service 负责编排 IO 与视图组装，不篡改业务真相。
