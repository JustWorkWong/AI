# Auth.Service

## 文件
- `Program.cs`: Auth 宿主入口，拼装认证、授权、端点与默认值。
- `Options/KeycloakOptions.cs`: OIDC 配置模型。
- `Clients/DomainUserSyncClient.cs`: 调 `wms-domain-service` 同步用户投影。
- `Endpoints/SessionEndpoints.cs`: 返回前端所需 OIDC 配置。
- `Endpoints/SyncEndpoints.cs`: 登录后把用户信息同步进业务域。

## 原则
- 这里只做身份边界与用户同步，不承载 RBAC 真相。
- 对前端暴露稳定配置，对内部服务只暴露最小同步入口。
