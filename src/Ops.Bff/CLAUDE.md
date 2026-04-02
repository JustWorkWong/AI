# Ops.Bff

## 文件
- `Program.cs`: BFF 宿主入口与依赖装配。
- `Clients/`: 到领域服务与 runtime 的薄客户端。
- `Endpoints/`: 面向页面的聚合接口。

## 原则
- BFF 组织页面数据，不拥有业务真相。
- 外部服务差异在 `Clients/` 收敛，端点只做聚合与映射。
