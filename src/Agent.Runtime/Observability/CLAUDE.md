# Observability

## 结构
```text
Observability/
├─ IToolInvocationStore.cs   # tool 调用落库抽象
├─ EfToolInvocationStore.cs  # 基于 EF 的调用存储
├─ ConversationCompactor.cs  # 会话压缩器
└─ ToolLoggingMiddleware.cs  # tool 调用记录中间件
```

## 原则
- 观测记录是证据链，不是日志噪音。
- 压缩保留上下文，不覆盖原始消息。
