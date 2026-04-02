# Endpoints

## 文件
- `DashboardEndpoints.cs`: 首页工作台聚合接口。
- `ReturnWorkbenchEndpoints.cs`: 退货工作台聚合接口、执行入口、审批入口与执行轨迹查询。
- `SopAssistEndpoints.cs`: SOP 推进与 SSE 事件桥接。

## 原则
- 端点层只做拼装，不做领域判断。
