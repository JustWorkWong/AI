# Ops.Bff.Tests

## 文件
- `Ops.Bff.Tests.csproj`: BFF 测试项目。
- `TestHost/`: BFF 测试共享宿主，收口 `WebApplicationFactory` 样板。
- `TestDoubles/`: BFF 端点测试共享替身，收敛重复接口实现。
- `DashboardEndpointsTests.cs`: 校验首页聚合接口不会退化。
- `DomainServiceClientTests.cs`: 校验下游 404 不会被 BFF 放大成 500。
- `ReturnWorkbenchPresenterTests.cs`: 校验退货工作台降级 suggestion 的映射规则固定不漂移。
- `ReturnWorkbenchQueryServiceTests.cs`: 校验退货工作台查询与降级策略不会散落到 endpoint。
- `ReturnWorkbenchEndpointsTests.cs`: 校验退货工作台聚合视图。
- `SseBridgeTests.cs`: 校验 SSE 事件桥接。
- `ReturnDispositionExecutionEndpointsTests.cs`: 校验 BFF 透传退货处置执行命令。
- `ReturnDispositionTraceEndpointsTests.cs`: 校验 BFF 透传退货执行轨迹。
- `ReturnDispositionApprovalEndpointsTests.cs`: 校验 BFF 透传审批动作。

## 原则
- 这里优先测页面聚合行为，不测领域规则。
- 外部依赖一律替身化，别让网络成为测试前提。
