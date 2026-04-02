# Remove Ops.Bff Design

## Goal

彻底删除 `Ops.Bff` 项目与其测试项目，让前端通过 `Gateway.Yarp` 直接访问 `Wms.DomainService` 与 `Agent.Runtime`。

目标不是减少文件数，而是消除一层重复适配。用户已经明确接受以下边界：

- 前端直接理解 `Domain` 与 `Runtime` 两个后端边界
- 前端自己组装页面 DTO
- 不做页面级降级，失败直接暴露
- 错误语义应由真实后端接口保证清晰，而不是由 `BFF` 二次翻译

## Decision

采用“彻底删除 `Ops.Bff`”方案。

删除后请求路径变为：

- `Vue -> Gateway.Yarp -> Wms.DomainService`
- `Vue -> Gateway.Yarp -> Agent.Runtime`

`Gateway.Yarp` 只保留入口层职责：

- 路由转发
- 鉴权透传
- SSE 转发
- 健康检查

它不承担页面聚合，不承担错误翻译，也不承担业务降级。

## Scope

本次变更包含：

- 删除 `src/Ops.Bff`
- 删除 `tests/Ops.Bff.Tests`
- 删除 solution、AppHost、Gateway 中对 `Ops.Bff` 的引用
- 前端改为直接请求 `Wms.DomainService` 与 `Agent.Runtime`
- 统一 `Wms.DomainService` 与 `Agent.Runtime` 的错误契约
- 保持现有退货工作台与 SOP 页面功能可用

本次变更不包含：

- 新增第三个后端边界
- 重新设计前端视觉层
- 引入新的聚合后端
- 改写现有业务规则

## Architecture

### Before

- `Vue -> Gateway -> Ops.Bff -> Domain/Runtime`

`Ops.Bff` 负责：

- 页面聚合
- DTO 适配
- 页面级降级
- SSE 桥接

### After

- `Vue -> Gateway -> Domain`
- `Vue -> Gateway -> Runtime`

前端职责上移：

- `api.ts` 直接请求真实后端路径
- `useReturnWorkbench` 负责退货工作台页面状态聚合
- `SopAssistPage` 与 `agui.ts` 直接消费 runtime 接口与事件流

后端职责收紧：

- `Domain` 只给出业务真相
- `Runtime` 只给出 AI workflow 真相
- `Gateway` 只做入口与转发

## API Boundary

### Wms.DomainService

保留或补齐以下类型接口：

- 退货单详情查询
- 审批任务查询
- 审批动作提交
- SOP 文档/会话读取

`Domain` 返回业务真相，不返回前端聚合视图。

### Agent.Runtime

保留或补齐以下类型接口：

- 处置建议查询
- 处置执行
- workflow trace 查询
- 审批恢复/继续执行
- SOP step 推进
- SOP SSE/AG-UI 事件流

`Runtime` 返回 workflow 真相，不返回页面聚合视图。

### Gateway.Yarp

网关按路径显式转发：

- `/api/domain/... -> wms-domain-service`
- `/api/runtime/... -> agent-runtime`

如果现有前端路径需要兼容，可在迁移期保留极薄的 rewrite，但不保留任何聚合逻辑。

## Frontend Changes

### Return Workbench

`useReturnWorkbench` 直接并发读取：

- 订单详情：`Domain`
- 建议：`Runtime`
- 执行结果：`Runtime`
- trace：`Runtime`
- 审批动作：优先调用 `Runtime` 恢复 workflow；若审批真相由 `Domain` 提供，则前端按顺序调用对应接口

页面模型在前端组装，替代原来的 `ReturnWorkbenchViewDto`。

### SOP Assist

`SopAssistPage` 直接对接：

- session/step 读取：`Domain` 或 `Runtime` 中的真实所有者
- step 推进：`Runtime`
- SSE/AG-UI：`Runtime`

页面不再经过 `Ops.Bff` 中转。

### Dashboard

仪表盘如果仍需要跨服务聚合，也由前端自行并发读取并拼装。当前 dashboard 规模很小，不足以保留一个独立 `BFF`。

## Error Contract

删除 `BFF` 的前提是错误语义清晰，不能把歧义扔给前端猜。

`Wms.DomainService` 与 `Agent.Runtime` 必须统一遵守：

- `404`: 资源不存在
- `409`: 状态冲突
- `422`: 输入或业务约束不合法
- `5xx`: 系统故障

错误响应使用统一 `ProblemDetails` 结构，至少包含：

- `type`
- `title`
- `status`
- `detail`
- `traceId`

前端不做“猜测式翻译”，只做有限映射和直接展示。

## Data Flow

### Return Workbench

1. 前端读取退货单详情
2. 前端读取处置建议
3. 前端组装页面状态
4. 用户执行处置
5. 前端读取执行结果
6. 如需审批，前端提交审批动作并再次读取执行结果或 trace
7. 前端读取 workflow trace 展示工具调用与 checkpoint

### SOP Assist

1. 前端读取当前 SOP 会话
2. 前端订阅 runtime 事件流
3. 用户推进步骤
4. runtime 返回当前步骤与引用
5. 页面直接展示 evidence 与状态变化

## Testing

### Backend

- `Gateway.Yarp` 路由测试，验证路径直达 `Domain` 与 `Runtime`
- `Domain` 与 `Runtime` 的错误契约测试
- 现有 `Domain` 与 `Runtime` 功能测试保持通过

### Frontend

- `api.ts` 直连后端路径的 client 测试
- `useReturnWorkbench` 并发读取、执行、审批、trace 测试
- `SOP` 页面事件流和步骤推进测试
- 现有页面级 wiring 测试保持通过

### End-to-End Smoke

必须验证：

- 退货工作台加载
- 处置执行
- 审批通过
- trace 展示
- SOP 页面推进一步
- 应用重启后数据库与 runtime 持久化记录仍可读取

## Migration Plan

1. 统一 `Domain` 与 `Runtime` 错误契约
2. 在 `Gateway` 加入直达路由
3. 修改前端 `api.ts`、composable、页面，改为直接请求真实后端
4. 跑前端与后端回归
5. 删除 `Ops.Bff` 与 `Ops.Bff.Tests`
6. 更新 `AppHost`、solution、相关 `CLAUDE.md`

## Risks

### Risk 1: Frontend Knows Too Much

删除 `BFF` 后，前端天然更了解后端边界。这是接受的 trade-off，但要守住上限：前端只允许理解 `Domain` 与 `Runtime` 两个边界，不能继续扩散。

### Risk 2: Error Semantics Drift

如果 `Domain` 与 `Runtime` 错误语义不统一，前端会重新变脏。这个风险必须靠契约测试锁住。

### Risk 3: SSE Contract Drift

`SOP` 页直接连接 `Runtime` 后，事件格式必须稳定。任何字段命名变化都需要测试护栏。

## Success Criteria

满足以下条件才算完成：

- `Ops.Bff` 与 `Ops.Bff.Tests` 完全删除
- solution 与 AppHost 不再引用 `Ops.Bff`
- 前端可以直接通过网关跑通退货工作台和 SOP 页面
- 所有自动化测试通过
- 本地 smoke 流程可复现
- 重启后持久化数据仍然存在
