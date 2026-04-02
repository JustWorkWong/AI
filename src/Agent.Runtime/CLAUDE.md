# Agent.Runtime

## 结构
```text
Agent.Runtime/
├─ Program.cs            # 运行时宿主，注册持久化、模型网关、观测与内部查询端点
├─ Persistence/          # workflow、checkpoint、消息、摘要、tool 调用的持久化真相
├─ Models/               # 模型档案与模型网关，隔离百炼 / Ollama 差异
├─ Observability/        # tool logging、消息压缩、运行时可观测能力
├─ Tools/                # workflow 调用的边界工具名与后续适配点
├─ Workflows/            # 退货与 SOP 两条编排流程
└─ Streaming/            # AG-UI 事件映射与 SSE 写出
```

## 原则
- Runtime 只持有 AI 执行状态，不持有业务真相。
- 所有可恢复点都先落库，再做外部副作用。
- 模型接入先抽象，再连接具体供应商，别把厂商差异渗进 workflow。
