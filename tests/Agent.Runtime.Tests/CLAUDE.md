# Agent.Runtime.Tests

## 结构
```text
Agent.Runtime.Tests/
├─ ToolLoggingMiddlewareTests.cs   # 验证 tool 调用证据链
├─ ConversationCompactorTests.cs   # 验证消息压缩边界
└─ InMemoryToolInvocationStore.cs  # 纯内存测试替身
```

## 原则
- 先测运行时边界，再写实现。
- 测试命名直接说行为，不写实现细节。
