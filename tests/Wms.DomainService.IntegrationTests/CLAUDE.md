# Wms.DomainService.IntegrationTests

## 文件
- `PostgresFixture.cs`: 提供数据库测试资源。
- `TestAppFactory.cs`: 定制领域服务测试宿主。
- `AuthProjectionTests.cs`: 校验用户同步投影链路。

## 原则
- 集成测试验证 HTTP + 持久化边界，不做纯规则断言。
