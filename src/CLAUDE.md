# src

## 结构
```text
src/
├─ Wms.AppHost/          # Aspire 编排入口，本地拓扑与 K8s 发布源头
├─ Wms.ServiceDefaults/  # 观测、健康检查、发现、通用宿主默认值
├─ Shared.Contracts/     # 跨服务 DTO 与实时事件契约
├─ Auth.Service/         # OIDC / Keycloak 对接与用户同步入口
├─ Gateway.Yarp/         # 北向反向代理与统一入口
├─ Ops.Bff/              # 面向 Vue 的聚合 API 与 SSE 桥接
├─ Wms.DomainService/    # 业务真相源，审批、退货、SOP、RAG 元数据都在这里
└─ Agent.Runtime/        # MAF workflow、模型网关、checkpoint、tool 观测
```

## 原则
- `Wms.DomainService` 持有业务状态，别把业务真相泄漏到 BFF 或 Runtime。
- `Agent.Runtime` 只做 AI 执行与恢复，不直接篡改领域状态。
- `Shared.Contracts` 保持薄，放契约，不放业务实现。
- `Wms.AppHost` 是开发编排与 K8s 产物生成入口，不承载业务逻辑。
