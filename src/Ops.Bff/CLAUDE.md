# Ops.Bff

## 文件
- `Program.cs`: BFF 宿主入口与依赖装配。
- `Clients/`: 到领域服务与 runtime 的薄客户端。
- `Endpoints/`: 面向页面的聚合接口。
- `Presenters/`: 页面占位、降级和输出映射的收口层。

## 原则
- BFF 组织页面数据，不拥有业务真相。
- 外部服务差异在 `Clients/` 收敛，端点只做聚合与映射。
- 降级文案和占位 DTO 要有单一真相源，别散落在 endpoint 里。
