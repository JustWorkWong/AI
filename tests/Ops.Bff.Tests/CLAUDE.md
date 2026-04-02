# Ops.Bff.Tests

## 文件
- `Ops.Bff.Tests.csproj`: BFF 测试项目。
- `DashboardEndpointsTests.cs`: 校验首页聚合接口不会退化。

## 原则
- 这里优先测页面聚合行为，不测领域规则。
- 外部依赖一律替身化，别让网络成为测试前提。
