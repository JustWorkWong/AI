# lib

## 文件
- `agui.ts`: EventSource 订阅与事件分发。
- `api.ts`: BFF 请求封装与 DTO 类型，包括执行、审批与执行轨迹查询。
- `errors.ts`: 把未知错误规整成前端可展示文案。

## 原则
- lib 只做协议适配，不存页面状态。
