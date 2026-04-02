# Auth.Service.Tests

## 文件
- `Auth.Service.Tests.csproj`: Auth 测试项目。
- `SessionEndpointsTests.cs`: 校验前端配置端点没有退化。

## 原则
- 先锁配置端点，再继续扩展同步与认证测试。
- 这里测 Auth 边界，不测 Keycloak 本体。
