# Clients

## 文件
- `DomainServiceClient.cs`: 调业务真相源的薄客户端。
- `AgentRuntimeClient.cs`: 调 runtime 的薄客户端。

## 原则
- 这里收敛外部依赖差异，不放页面聚合逻辑。
- 返回类型必须是显式 DTO，不返回 `object`。
