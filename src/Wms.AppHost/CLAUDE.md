# Wms.AppHost

## 文件
- `Wms.AppHost.csproj`: Aspire AppHost 项目与资源引用根。
- `AppHost.cs`: 本地编排拓扑与 Kubernetes 发布拓扑定义。
- `Properties/launchSettings.json`: 本地 `dotnet run` 所需的 dashboard 与 OTLP 启动环境。

## 原则
- 这里只描述资源连接关系，不写业务逻辑。
- 所有有状态基础设施都要显式持久化，别把数据命运交给匿名 volume。
- 有状态存储一旦持久化，连接凭据也必须稳定；数据库密码不能每次启动随机生成，否则你保住的是数据，丢掉的是可用性。
- 所有本地依赖和 K8s 发布入口都从这里发源，别再造第二套拓扑。
