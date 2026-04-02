# Auth

## 文件
- `User.cs`: 外部身份在业务域内的投影。
- `Role.cs`: 角色定义。
- `Permission.cs`: 权限定义。
- `UserRole.cs`: 用户与角色关联。
- `RolePermission.cs`: 角色与权限关联。

## 原则
- 这里持有业务授权投影，不替代外部 IdP 的身份认证。
