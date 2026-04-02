# tests

## 结构
```text
tests/
├─ Wms.DomainService.UnitTests/         # 领域规则与纯内存单元测试
├─ Wms.DomainService.IntegrationTests/  # 数据库、HTTP、存储、事件链路集成测试
├─ Agent.Runtime.Tests/                 # workflow、checkpoint、tool logging、压缩测试
├─ Auth.Service.Tests/                  # OIDC 配置与用户同步边界测试
├─ Ops.Bff.Tests/                       # 页面聚合、SSE 桥接、契约映射测试
└─ Architecture.Tests/                  # 目录、项目引用、发布与架构约束测试
```

## 原则
- 单元测试验证规则，集成测试验证边界，架构测试防止结构退化。
- 任何跨服务协作先写契约测试，别让前后端靠猜。
- 测试命名直接描述行为，不写实现细节。
