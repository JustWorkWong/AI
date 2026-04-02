# Wms.DomainService.IntegrationTests

## 文件
- `PostgresFixture.cs`: 提供数据库测试资源。
- `TestAppFactory.cs`: 定制领域服务测试宿主。
- `AuthProjectionTests.cs`: 校验用户同步投影链路。
- `DispositionEndpointsTests.cs`: 校验处置审批与幂等落单边界。
- `AttachmentEndpointsTests.cs`: 校验附件上传、对象存储与 outbox 事件。
- `ReturnReadEndpointsTests.cs`: 校验退货单读取与历史案例投影。
- `SopReadEndpointsTests.cs`: 校验 SOP 候选检索与切块读取。
- `SopPublishEndpointsTests.cs`: 校验 SOP 发布后可被候选与切块查询命中。

## 原则
- 集成测试验证 HTTP + 持久化边界，不做纯规则断言。
