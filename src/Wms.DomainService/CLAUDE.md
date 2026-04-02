# Wms.DomainService

## 结构
```text
Wms.DomainService/
├─ Auth/         # 用户、角色、权限与投影关联
├─ Returns/      # 退货、质检、处置与历史案例投影
├─ Sop/          # SOP 文档与切块只读模型
├─ Endpoints/    # 内部读写 API 边界
├─ Persistence/  # DbContext 与数据库边界
├─ Seed/         # 稳定参考数据、开发自举与演示路径
└─ Program.cs    # 宿主入口与内部 API
```

## 原则
- 这里是业务真相源，外部系统只能通过明确端点写入投影。
- 读模型和写模型都留在同一个服务里，但真相源只能有一个。
- 开发环境必须能自举出最小可演示数据，别让前端请求空气。
