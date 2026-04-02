# Streaming

## 文件
- `AgUiEventMapper.cs`: runtime 事实到 AG-UI 事件的映射。
- `SseEventWriter.cs`: SSE 协议写出器。

## 原则
- mapper 不做 IO。
- writer 不做业务拼装，只做协议输出。
