# Wms AI Platform

WMS AI 面试项目骨架，当前聚焦两个流程：

- `退货质检与处置`
- `SOP 辅助执行`

## 架构原则

- `Wms.DomainService` 是业务真相源。
- `Agent.Runtime` 负责 MAF workflow、checkpoint、tool 观测与模型路由。
- `Ops.Bff` 面向 Vue 页面聚合，不承载领域规则。
- `Wms.AppHost` 负责 Aspire 本地编排与 Kubernetes 发布产物生成。

## 当前状态

- `Task 1` 仓库骨架进行中。
- 设计与实现计划位于 `docs/superpowers/`。

## 启动前提

- `.NET SDK 10.0.201+`
- Docker Desktop
- Node.js 22+

## 近期执行顺序

1. 完成 solution 与项目骨架。
2. 落 `Aspire AppHost` 与 `ServiceDefaults`。
3. 接 `Auth.Service` 和 `Keycloak` 基线。
