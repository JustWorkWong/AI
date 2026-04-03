# Remove Ops.Bff, Keep Swagger, Add pgAdmin Design

## Goal

把系统收敛成更直接的结构：

- 彻底删除 `Ops.Bff`
- 前端通过 `Gateway.Yarp` 直接访问 `Wms.DomainService` 与 `Agent.Runtime`
- 后端服务保留可访问的 Swagger/OpenAPI
- 在 Aspire 本地编排里加入 `pgAdmin 4`，用于开发和演示时直接查询 PostgreSQL

目标不是“多加几个工具”，而是让系统边界更清晰，同时保留足够好的演示和运维体验。

## Decision

采用一次性收口方案：

- 删掉 `Ops.Bff`
- `Gateway` 只做入口和转发
- 前端自己聚合页面数据
- `Domain` 和 `Runtime` 都继续公开 OpenAPI
- `pgAdmin 4` 作为开发期数据库管理容器，由 `Wms.AppHost` 统一编排

## Scope

本次变更包含：

- 删除 `src/Ops.Bff`
- 删除 `tests/Ops.Bff.Tests`
- 调整 `Gateway.Yarp` 路由，直连 `Domain` 与 `Runtime`
- 调整 `Vue` 前端 API 层，改为直连两个后端
- 保持 `Wms.DomainService` 与 `Agent.Runtime` 的 Swagger 可访问
- 在 `Wms.AppHost` 中添加 `pgAdmin 4`
- 更新 README 和相关 `CLAUDE.md`

本次变更不包含：

- 引入新的业务服务
- 聚合 Swagger 文档
- 自建数据库管理后台
- 修改现有业务流程本身

## Architecture

### Before

- `Vue -> Gateway -> Ops.Bff -> Domain/Runtime`

### After

- `Vue -> Gateway -> Domain`
- `Vue -> Gateway -> Runtime`

同时开发编排层增加：

- `AppHost -> postgres`
- `AppHost -> pgAdmin 4`

`pgAdmin 4` 不参与业务请求链路，只是开发和演示辅助工具。

## Backend Boundary

### Gateway.Yarp

职责收紧为：

- north-south 入口
- 路由转发
- 鉴权透传
- SSE 转发
- 健康检查

网关不再承担：

- 页面聚合
- DTO 翻译
- 页面降级

建议路径：

- `/api/domain/{**catch-all} -> wms-domain-service`
- `/api/runtime/{**catch-all} -> agent-runtime`

### Wms.DomainService

继续作为业务真相源，公开：

- 退货单读取
- 审批读取与动作
- SOP 文档读取
- 业务侧其他内部接口

同时保留 OpenAPI，以便开发、演示、调试。

### Agent.Runtime

继续作为 workflow 真相源，公开：

- 处置建议
- 处置执行
- 审批恢复
- trace 查询
- SOP step 推进
- SSE/AG-UI 事件流

同时保留 OpenAPI。

## Swagger Strategy

Swagger 保留在真实服务上，不做 Gateway 聚合。

理由：

- 聚合 Swagger 会重新引入一层“伪 BFF”味道
- 真实服务边界更适合面试展示
- 直连服务文档更利于调试

开发期预期：

- `Wms.DomainService` 可访问 OpenAPI
- `Agent.Runtime` 可访问 OpenAPI

README 里明确这两个入口，而不是再造统一文档门户。

## pgAdmin 4

### Role

`pgAdmin 4` 只用于：

- 浏览 `wmsdb`
- 浏览 `aidb`
- 手动验证表结构、索引和 workflow 记录
- 面试演示时快速展示持久化数据

### Non-Goals

- 不进入业务网关
- 不作为项目业务模块
- 不接入前端应用

### AppHost Integration

在 `Wms.AppHost` 中增加 `pgAdmin 4` 容器：

- 镜像：`dpage/pgadmin4`
- 配置默认登录邮箱和密码
- 持久化 volume
- 暴露 HTTP 端口
- 连接现有 `postgres`

### Persistence

`pgAdmin 4` 需要持久化：

- server connection profile
- 登录配置

否则每次重启又要重新配库，没有演示价值。

## Frontend Changes

### Return Workbench

前端继续在 `useReturnWorkbench` 中完成聚合：

- 从 `Domain` 读退货单
- 从 `Runtime` 读建议、执行结果、trace

不再依赖 `ReturnWorkbenchViewDto` 这类 BFF 聚合模型。

### SOP Assist

前端直接访问：

- `Runtime` 的 step 推进
- `Runtime` 的 SSE 流
- `Domain` 的必要读取接口

### Error Handling

沿用之前锁定的直连错误契约：

- `404`
- `409`
- `422`
- `5xx`

前端只做有限映射，不再做 BFF 式降级。

## AppHost Changes

`Wms.AppHost` 需要：

- 移除 `ops-bff`
- `gateway-yarp` 仅依赖 `auth/domain/runtime`
- 新增 `pgadmin`
- 给 `pgadmin` 配置持久化 volume

这会直接改变本地编排拓扑，所以对应文档必须同步更新。

## Testing

### Backend

- `Gateway` 路由测试：不再引用 `ops-bff`
- `Domain` / `Runtime` 现有测试保持通过
- Swagger/OpenAPI 入口可访问测试

### Frontend

- API 层改为直连 `domain/runtime`
- 退货页和 SOP 页仍能跑通主流程

### AppHost / Tooling

- `AppHost` 能启动 `pgAdmin 4`
- `pgAdmin 4` 可连接 `wmsdb` 与 `aidb`
- 重启后 `pgAdmin` 配置和 Postgres 数据都保留

## Risks

### Risk 1: Frontend Knows More About Backend Topology

这是刻意接受的 trade-off。边界必须收在两个后端：

- `Domain`
- `Runtime`

不能继续扩散成更多直连目标。

### Risk 2: Swagger 暴露面过宽

开发期保留 Swagger 是有价值的，但后续如果进入公网部署，需要再做环境门控。

### Risk 3: pgAdmin 被误当成业务模块

必须在文档里明确它只是开发期工具，不属于业务系统。

## Success Criteria

满足以下条件才算完成：

- `Ops.Bff` 与 `Ops.Bff.Tests` 完全删除
- `Gateway` 直连 `Domain` 与 `Runtime`
- `Vue` 前端不再依赖 `Ops.Bff`
- `Wms.DomainService` Swagger 可访问
- `Agent.Runtime` Swagger 可访问
- `pgAdmin 4` 在 Aspire 中可启动并连接 PostgreSQL
- 自动化测试通过
- 本地 smoke 流程可复现
