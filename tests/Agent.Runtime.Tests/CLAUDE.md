# Agent.Runtime.Tests

## 结构
```text
Agent.Runtime.Tests/
├─ ToolLoggingMiddlewareTests.cs   # 验证 tool 调用证据链
├─ ConversationCompactorTests.cs   # 验证消息压缩边界
├─ DomainKnowledgeClientTests.cs    # 验证 runtime 读取 domain 端点时的 404 退化
├─ ReturnDispositionWorkflowTests.cs # 验证退货处置 workflow 的审批分支
├─ ReturnDispositionExecutorTests.cs # 验证退货处置执行链、审批与落单
├─ SopAssistWorkflowTests.cs         # 验证 SOP workflow 的证据分支
├─ ReturnDispositionAdvisorTests.cs  # 验证退货建议聚合真实 domain 读边界与 tool 留痕
├─ SopAssistServiceTests.cs          # 验证 SOP workflow、checkpoint 与证据输出
└─ InMemoryToolInvocationStore.cs    # 纯内存测试替身
```

## 原则
- 先测运行时边界，再写实现。
- 测试命名直接说行为，不写实现细节。
