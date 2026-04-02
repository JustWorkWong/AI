# Realtime

## 文件
- `AgUiEvents.cs`: AG-UI / SSE 统一事件契约。

## 原则
- 实时事件只表达运行时事实，不表达前端派生状态。
- 事件名稳定，payload 可演进。
