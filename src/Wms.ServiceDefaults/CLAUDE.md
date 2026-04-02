# Wms.ServiceDefaults

## 文件
- `Wms.ServiceDefaults.csproj`: Aspire 共享默认值项目。
- `Extensions.cs`: 观测、健康检查、服务发现、HTTP 默认值扩展。

## 原则
- 这里只放宿主默认值，不放 DTO，不放业务工具。
- 每个服务都应通过同一套扩展获得一致的观测和发现能力。
