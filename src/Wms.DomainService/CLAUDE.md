# Wms.DomainService

## 结构
```text
Wms.DomainService/
├─ Auth/         # 用户、角色、权限与投影关联
├─ Persistence/  # DbContext 与数据库边界
├─ Seed/         # 稳定参考数据与默认角色
└─ Program.cs    # 宿主入口与内部 API
```

## 原则
- 这里是业务真相源，外部系统只能通过明确端点写入投影。
- 先收敛用户投影和 RBAC，再扩展退货与 SOP 领域。
