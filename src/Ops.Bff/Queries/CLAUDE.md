# Queries

## 文件
- `ReturnWorkbenchQueryService.cs`: 退货工作台的查询编排与降级收口。

## 原则
- 查询服务负责“拿什么数据”和“下游失败时如何降级”。
- endpoint 只负责 HTTP 入口，别再自己写查询流程。
