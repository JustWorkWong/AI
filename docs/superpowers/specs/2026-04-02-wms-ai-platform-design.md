# WMS AI Platform Design

**主题**: 面向面试展示的生产级 WMS AI 平台  
**日期**: 2026-04-02  
**范围**: `退货质检与处置`、`SOP 辅助执行` 两条业务链路  
**核心决策**: 所有业务域逻辑合并到一个业务微服务；AI runtime、网关、鉴权、治理、前端保持独立。

---

## 1. Goal

构建一个可私有化部署、可审计、可恢复、可观察的 WMS AI 平台，用于展示以下能力：

- `C# + .NET 10 + EF Core` 的企业级后端能力
- `MAF workflow + agent + tool` 的生产级 AI 编排能力
- `Vue + AG-UI` 的实时交互前端能力
- `YARP + Kubernetes + OpenTelemetry` 的云原生工程能力
- `审批、checkpoint、消息压缩、trace、种子数据` 的生产治理能力

这个系统不是聊天机器人，而是一个把 AI 放进业务闭环的作业平台。

---

## 2. Scope

### 2.1 Included

- 退货质检与处置流程
- SOP 辅助执行流程
- AI tool 调用记录
- workflow checkpoint 持久化与恢复
- conversation compaction
- SOP 文档 RAG 检索链路
- 模型、prompt、tool policy、风险策略的后台治理
- Kubernetes 部署设计

### 2.2 Excluded In V1

- PDA 原生端
- 多租户
- 自动执行高风险库存动作
- 补货建议与仓内调度优化
- ERP/MES 的深度双向集成

---

## 3. Architecture

### 3.1 Topology

```text
[Vue Web + AG-UI Client]
          |
   [Ingress Nginx]
          |
       [YARP Gateway]
          |
  -----------------------------------------------
  |                |               |             |
[Auth Service] [Ops BFF] [Trace UI/Admin UI] [Agent Runtime]
                    |                               |
                    |                          [Model Gateway]
                    |                            |        |
                    |                       [Bailian] [Ollama]
                    |
             [Wms Domain Service]
                    |
    ------------------------------------------------
    |                    |                         |
[PostgreSQL + pgvector] [RabbitMQ + MassTransit] [MinIO]

Observability: OpenTelemetry -> Jaeger + Prometheus + Grafana
```

### 3.2 Service Boundaries

#### `gateway-yarp`

- 统一北向入口
- JWT/OIDC 透传
- SSE/流式事件转发
- 灰度、限流、审计头注入

#### `auth-service`

- 用户认证
- Token 签发
- 推荐使用 `Keycloak`
- 负责 `OIDC discovery`、`JWKS`、claims 映射规则、登录后用户同步触发
- 对前端提供最小会话信息，不承载业务授权真相

权限真相源规则：

- `Keycloak` 负责身份认证与用户登录
- `wms-domain-service` 维护本地授权投影
- 本地 `roles/permissions/role_permissions/user_roles` 是业务授权真相源
- 登录后通过同步器把 `Keycloak user` 映射到本地 `users`
- 用户首次登录后由 `auth-service` 发布 `user-synced` 事件，驱动本地投影创建

#### `ops-bff`

- 给前端提供聚合 API
- 聚合业务查询、审批、trace 查看、workflow 发起
- 隔离前端与内部服务
- 代理 AG-UI/SSE 事件流，不让前端直接连内部 runtime

#### `wms-domain-service`

唯一业务域服务，承载全部业务逻辑：

- 退货单
- 质检单
- 处置决策
- 审批任务
- 附件元数据
- outbox 消息
- inbox 去重
- SOP 文档与步骤
- SOP 检索索引元数据
- 仓库、库区、库位、SKU
- 审批结果应用

设计理由：

- 当前项目目标是面试展示，不是组织级拆分
- 单业务服务更利于控制复杂度和代码演进
- 能保留微服务架构外形，同时避免“按名词拆服务”的伪拆分

#### `agent-runtime`

- 承载 `MAF agents` 与 `MAF workflows`
- 执行 tool 调用
- 保留 checkpoint
- 保留 tool logs、agent runs、消息压缩摘要
- 处理 human-in-the-loop

状态真相源规则：

- `workflow` 的执行图与节点逻辑以代码为主
- 数据库中的 `workflow_definitions` 只保存已发布版本元数据、显示名、版本号、启停状态
- `agent-runtime` 不拥有审批任务真相源
- `agent-runtime` 只保留 `approval_reference_id` 与暂停原因

#### `governance/admin`

可以先并入 `ops-bff`，后续再拆。负责：

- 模型档案配置
- prompt 版本管理
- tool policy
- 风险策略
- trace 检索
- 审计查询

---

## 4. Frontend Design

### 4.1 Stack

- `Vue 3`
- `TypeScript`
- `Vite`
- `Pinia`
- `Vue Router`
- `TanStack Query for Vue`
- `AG-UI JavaScript Client`
- `Element Plus` 或 `Naive UI`

### 4.2 Pages

#### `运营总览`

- 当日退货单
- 待审批建议
- SOP 执行异常
- 模型调用与失败率摘要

#### `退货质检工作台`

- 退货单列表
- 质检录入表单
- 附件上传与预览
- AI 处置建议卡片
- 证据引用
- 审批入口

#### `SOP 执行助手`

- 当前作业上下文
- 分步指引
- 命中片段与证据引用
- 每步确认
- 偏差提示
- 升级处理入口

#### `AI Trace Center`

- workflow timeline
- tool call 时间线
- checkpoint 列表
- 消息压缩前后视图
- prompt/model/version 标签

### 4.3 AG-UI Event Contract

前端不直接消费内部对象，而是消费稳定事件流。

事件类型最小集合：

- `workflow.started`
- `message.delta`
- `tool.started`
- `tool.completed`
- `checkpoint.created`
- `approval.requested`
- `approval.completed`
- `citation.updated`
- `session.completed`
- `session.failed`
- `heartbeat`

契约规则：

- 每个事件都必须带 `session_id`、`workflow_instance_id`、`trace_id`
- `tool.*` 事件必须带 `tool_name`
- `citation.updated` 必须带 `source_id`、`version`、`step_code`
- `heartbeat` 用于长连接保活，不更新业务状态

#### `Admin`

- 模型档案
- prompt 模板与版本
- tool 白名单
- 风险策略
- 种子数据初始化状态

---

## 5. Workflow Design

### 5.1 Return Quality And Disposition

#### Trigger

- 质检员打开退货单并提交质检结果

#### Steps

1. 读取退货单、SKU、品类规则、历史案例
2. 读取质检录入结果与附件
3. 检索处置 SOP 和相关案例
4. Agent 生成处置候选与原因说明
5. 规则引擎校验候选结果是否合法
6. 风险策略判断是否需要审批
7. 需要审批时调用 `wms-domain-service` 创建审批任务并回填 `approval_reference_id`；不需要审批时直接生成待落地决策
8. 审批通过后由 `wms-domain-service` 幂等落地处置单

#### Evidence Rule

- 处置建议引用来源只允许来自 `已发布 SOP` 与 `已结案历史案例`
- 历史案例不是新真相源，而是由 `quality_inspections + disposition_decisions` 生成的只读投影视图
- 无命中证据时只允许输出“需要人工复核”，不得生成具体处置结论

#### Attachment Rule

- 质检附件原文件存 `MinIO`
- 业务库只保存附件元数据、对象键、内容类型、上传人、哈希值
- 生成建议时只读取授权后的附件引用，不直接暴露对象存储地址

#### Tools

- `GetReturnOrderTool`
- `GetSkuPolicyTool`
- `SearchSopTool`
- `SearchHistoricalCasesTool`
- `ValidateDispositionTool`
- `RequestDispositionApprovalTool`
- `ApplyDispositionDecisionTool`

#### Output

- 建议处置结果
- 风险等级
- 证据来源
- 处置理由
- 审批状态

### 5.2 SOP Assisted Execution

#### Trigger

- 操作员在执行作业时请求指导，或系统检测到流程偏差

#### Steps

1. 根据作业类型、库区、角色、设备类型构造检索上下文
2. 先用过滤条件命中候选 SOP 版本，再从知识分块中检索相关步骤片段
3. 重排检索结果，生成当前步骤需要的最小证据集
4. Agent 基于 `SOP step + evidence chunks + current state` 生成执行指引
5. 每步等待现场确认，并记录确认结果
6. 出现偏差时重新检索纠正片段与异常处理片段，给出解释、纠正建议、升级入口
7. 结束后生成完整执行记录与引用清单

#### Tools

- `ResolveOperationContextTool`
- `SearchSopCandidatesTool`
- `RetrieveSopChunksTool`
- `RankSopEvidenceTool`
- `LogStepConfirmationTool`
- `DetectDeviationTool`
- `RetrieveDeviationHandlingTool`
- `EscalateIssueTool`
- `CompleteSopSessionTool`

#### Output

- 实时步骤引导
- 证据引用
- 偏差说明
- 升级记录
- 完整执行日志

### 5.3 SOP RAG Design

#### Retrieval Goal

让 `SOP 辅助执行` 不是直接把整份 SOP 塞进上下文，而是只取当前步骤真正需要的证据。

#### Ingestion Flow

1. 上传 SOP 文档或维护结构化步骤
2. 生成 `document -> version -> step -> chunk` 层级
3. 对 chunk 生成 embedding 与关键词索引
4. 写入可检索元数据：作业类型、库区、角色、设备、风险等级、版本状态
5. 发布后才能被 runtime 检索

#### Retrieval Flow

1. 根据作业上下文先过滤候选文档和版本
2. 用当前步骤目标、偏差描述、设备状态做向量检索 + 关键词检索
3. 重排后保留 Top-K chunk
4. 生成 citations，返回给 agent 与前端
5. 前端展示引用来源、版本、步骤号、片段摘要

#### RAG Rules

- 未发布 SOP 不参与检索
- 检索必须返回版本号与步骤号
- 无证据时不得编造操作步骤
- 命中冲突片段时优先最新已发布版本，并保留冲突提示
- 运行时只传当前步骤和相邻步骤所需片段，避免上下文污染

---

## 6. MAF Runtime Design

### 6.1 Core Principles

- LLM 负责解释、归因、检索整合、例外处理
- 规则负责数量计算、合法性校验、状态流转约束
- tool 负责读取和写入外部系统
- 所有副作用前必须有 checkpoint
- 所有写操作必须是幂等的

### 6.2 Runtime Pipeline

1. `ops-bff` 创建 `workflow_instance`
2. `agent-runtime` 装载代码中的 workflow 执行图，并从数据库读取已发布版本元数据、model profile、tool policy
3. 载入会话历史与最近 checkpoint
4. 执行当前 superstep
5. 每次 tool call 记录输入摘要、输出摘要、耗时、结果状态
6. 达到阈值时执行 conversation compaction
7. 遇到审批点时调用 `wms-domain-service` 创建审批任务，冻结运行状态并持久化 checkpoint
8. 审批结果回来后通过 `approval_reference_id` 恢复 workflow

### 6.3 Required Persistence

- 原始消息
- 压缩摘要
- tool invocation
- agent run
- checkpoint snapshot
- workflow state
- prompt version
- model profile
- approval reference

### 6.4 Message Compaction

#### Trigger Rules

- token 数超阈值
- 对话轮次超阈值
- 进入审批前
- 进入新业务阶段前

#### Preserve

- system 指令
- 当前任务目标
- 未完成 action
- 最近关键工具结果
- 审批上下文
- 当前步骤状态

#### Rule

- 原始消息永不覆盖
- 压缩结果单独落表
- 恢复时优先使用 `raw + summary` 混合加载
- 原始消息按保留策略归档，不做无限期热存储

### 6.5 Checkpoint Policy

必须创建 checkpoint 的时机：

- 每个 workflow 阶段结束
- 每个审批前
- 每个外部副作用前
- 长时间等待用户输入前

### 6.6 Idempotency Policy

所有写操作 tool 必须携带 `idempotency_key`。

规则：

- `agent-runtime` 生成一次性命令键并随 tool request 下发
- `wms-domain-service` 用 `command_deduplications` 表做幂等去重
- checkpoint 恢复后允许重复发送命令，但同一 `idempotency_key` 只能产生一次副作用
- 非幂等写接口禁止暴露给 agent

### 6.7 Tool Observability

每次 tool 调用保留：

- tool 名称
- 参数摘要
- 调用人/agent
- 调用时间
- 耗时
- 成功/失败
- 输出摘要
- 关联 trace id

---

## 7. Data Management

### 7.1 Storage Strategy

- 业务主库：`PostgreSQL`
- 向量索引：`pgvector`
- 文件：`MinIO`
- 消息总线：`RabbitMQ`
- 缓存：`Redis`

### 7.2 EF Core Strategy

- 每个服务单独 `DbContext`
- `wms-domain-service` 维护业务 schema
- `agent-runtime` 维护 AI schema
- 禁止跨服务共享实体
- 审批表属于 `wms-domain-service`

### 7.3 Transaction Rule

- 业务更新采用本地事务 + outbox
- 异步事件通过 `MassTransit` 分发
- Agent runtime 不直接修改核心库存状态
- 消费方维护 `inbox` 或等价去重表，避免重复处理

### 7.4 Object Storage Rule

- 原始附件存对象存储，数据库只存元数据
- 下载通过受控签名 URL 或受控代理接口
- 附件对象键必须可追踪到业务聚合根，但不能泄露内部目录结构

---

## 8. Database Model

### 8.1 Identity And RBAC

- `users`
- `roles`
- `permissions`
- `user_roles`
- `role_permissions`

### 8.2 Warehouse Core

- `warehouses`
- `zones`
- `bins`
- `skus`
- `sku_categories`

### 8.3 Operations, Returns And Approval Domain

- `return_orders`
- `quality_inspections`
- `disposition_policies`
- `disposition_decisions`
- `historical_case_views`
- `return_attachments`
- `approval_tasks`
- `approval_actions`
- `command_deduplications`
- `outbox_messages`
- `inbox_messages`

### 8.4 SOP And Knowledge Domain

- `sop_documents`
- `sop_versions`
- `sop_steps`
- `sop_step_chunks`
- `knowledge_chunks`
- `knowledge_embeddings`
- `knowledge_index_jobs`
- `knowledge_citations`
- `sop_execution_sessions`
- `sop_step_logs`

### 8.5 AI Runtime

- `workflow_definitions`
- `workflow_instances`
- `workflow_checkpoints`
- `agent_runs`
- `agent_messages`
- `conversation_summaries`
- `tool_invocations`
- `prompt_templates`
- `prompt_versions`
- `model_providers`
- `model_profiles`
- `model_capabilities`
- `tool_policies`
- `risk_policies`
- `audit_logs`

---

## 9. Seed Data Strategy

### 9.1 Migration Seeds

通过 `EF Core migrations` 写入稳定参考数据：

- 默认权限
- 默认角色
- 状态枚举
- 仓库基础字典
- SOP 分类
- workflow 发布元数据
- 默认 prompt 模板
- 默认 model profile
- 默认 tool policy
- 默认 risk policy

### 9.2 Bootstrap Seeds

通过 `bootstrap job` 或 `seed command` 写入环境演示数据：

- 超级管理员
- 仓库主管
- 质检员
- 操作员
- 审计员
- 演示 SKU
- 演示退货单
- 演示 SOP

### 9.3 Default Roles

- `SuperAdmin`
- `WarehouseManager`
- `QualityInspector`
- `Operator`
- `Auditor`
- `AiAdmin`

---

## 10. Model And Prompt Governance

### 10.1 Store In Database

以下内容必须数据库管理：

- provider 名称
- profile 名称
- model 标识
- capability 标签
- 默认温度等推理参数
- prompt 模板
- prompt 版本
- 路由策略
- 压缩策略
- tool 白名单
- 风险阈值

### 10.2 Store In K8s Secret

以下内容只能放 `Secret`：

- API Key
- Access Secret
- Token
- 私有 endpoint 凭证

### 10.3 Runtime Rule

- DB 中只保存 secret 引用名，不保存明文
- 发布新 prompt/model policy 时必须生成审计记录
- 每次 workflow run 都保留当时生效的版本快照
- endpoint URL 与 provider 路由通过 `Helm values / ConfigMap` 注入，不通过 migration 固化

---

## 11. Kubernetes Design

### 11.1 Namespaces

- `wms-platform`
- `observability`
- `infra`

### 11.2 Deployments

- `gateway-yarp`
- `ops-bff`
- `wms-domain-service`
- `agent-runtime`
- `trace-admin-ui`

### 11.3 Stateful Components

- `postgresql`
- `rabbitmq`
- `redis`
- `minio`

### 11.4 Platform Components

- `ingress-nginx`
- `cert-manager`
- `external-secrets` 可选
- `otel-collector`
- `prometheus`
- `grafana`
- `jaeger`

### 11.5 Configuration

- `ConfigMap`: 服务地址、feature flags、模型 endpoint URL、非敏感配置
- `Secret`: 数据库连接、百炼 key、MinIO key
- `HPA`: 面向 `ops-bff`、`agent-runtime`
- `Keycloak realm/client` 配置通过 `ConfigMap` 注入，client secret 走 `Secret`

---

## 12. Security And Audit

- 所有接口走 JWT/OIDC
- 所有 AI 输出都附带证据来源
- 高风险动作必须审批
- 审批、checkpoint、tool call 都可追踪
- 敏感字段做字段级脱敏
- 审计不可被普通业务角色删除
- 原始消息和 tool payload 按角色控制查看权限
- 原始消息默认热存 30 天，归档 180 天，超过保留期按策略清理

---

## 13. Observability

### 13.1 Traces

统一追踪以下 ID：

- `trace_id`
- `request_id`
- `workflow_instance_id`
- `checkpoint_id`
- `agent_run_id`
- `tool_invocation_id`

### 13.2 Metrics

- workflow 成功率
- tool 失败率
- 平均响应时间
- 平均审批耗时
- token 用量
- 压缩命中率
- SOP 完成率
- SOP 检索命中率
- 无证据拒答率
- outbox 积压数
- SSE 在线连接数
- 附件上传失败率

### 13.3 Logs

- 结构化日志
- 业务日志与 AI 日志分流
- 支持根据 workflow instance 检索整条链路

---

## 14. Error Handling

- 业务规则失败返回确定性错误码
- tool 调用失败可重试，但必须记录原始失败
- checkpoint 恢复失败时允许回退到上一个稳定点
- 模型不可用时降级到备用 profile 或仅返回规则结果
- 审批超时自动进入人工待处理队列
- RAG 检索无结果时返回“需要人工确认”，不得生成伪步骤

---

## 15. Testing Strategy

### 15.1 Unit Tests

- 规则引擎
- 处置合法性校验
- SOP 检索过滤器构造
- citation 生成
- SOP 步骤流转
- RBAC 授权判定

### 15.2 Integration Tests

- EF Core 持久化
- outbox 事件
- YARP 路由
- agent-runtime 与业务服务 tool 集成
- checkpoint 恢复
- 幂等命令去重
- SOP RAG 检索与版本过滤

### 15.3 End-To-End

- 退货质检到审批到处置
- SOP 引导到偏差升级到完成

---

## 16. Design Principles

- 能用规则解决的，不交给模型
- 能做成统一流程的，不堆 if/else 特判
- 能保留证据链的，绝不返回黑盒结论
- 能在一个业务服务里说清楚的，不为拆而拆
- AI 是副驾驶，不是库存主写入者

---

## 17. Phase Plan

### Phase 1

- 身份权限
- 仓储基础数据
- 退货质检与处置 workflow
- SOP 文档 ingest 与 RAG 最小链路
- 模型治理最小集
- trace/checkpoint/tool log

### Phase 2

- SOP 辅助执行 workflow
- 审批闭环与恢复体验优化
- 提升前端运营看板与 citations 展示

### Phase 3

- 消息压缩优化
- retention 与归档策略
- trace 检索体验优化

---

## 18. Final Decision

这版方案的最终形态是：

- 前端使用 `Vue`
- 生产部署使用 `Kubernetes`
- 业务逻辑合并到单一 `wms-domain-service`
- AI runtime 独立部署
- 模型治理走数据库，密钥走 Secret
- 必要表包含可执行的种子数据策略

它保留了云原生和 AI 平台化能力，同时没有把复杂度浪费在虚假的服务拆分上。
