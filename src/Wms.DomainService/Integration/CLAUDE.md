# Integration

## 文件
- `OutboxMessage.cs`: 领域事件待发布记录。
- `InboxMessage.cs`: 幂等消费去重记录。
- `OutboxDispatcher.cs`: outbox 发布器占位实现。

## 原则
- 外部消息先落库，再异步分发，别在请求里直接冒险发消息。
