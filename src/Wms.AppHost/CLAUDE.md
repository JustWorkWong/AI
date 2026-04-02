# Wms.AppHost

## 文件
- `Wms.AppHost.csproj`: Aspire AppHost 项目与资源引用根。
- `AppHost.cs`: 本地编排拓扑与 Kubernetes 发布拓扑定义。

## 原则
- 这里只描述资源连接关系，不写业务逻辑。
- 所有本地依赖和 K8s 发布入口都从这里发源，别再造第二套拓扑。
