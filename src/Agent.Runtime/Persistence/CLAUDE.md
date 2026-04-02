# Persistence

## 结构
```text
Persistence/
├─ AgentRuntimeDbContext.cs   # Runtime 持久化入口与表映射
├─ RuntimeDatabaseInitializer.cs # 开发环境 schema 自举
├─ WorkflowInstance.cs        # workflow 实例主表
├─ WorkflowCheckpoint.cs      # superstep 与审批前恢复点
├─ AgentRun.cs                # 单次 agent 运行记录
├─ AgentMessage.cs            # 会话消息明细
├─ ConversationSummary.cs     # 压缩后的摘要快照
└─ ToolInvocation.cs          # tool 调用证据链
```

## 原则
- 一个表只表达一个事实，别让审批和领域状态混进来。
- checkpoint 是恢复点，不是第二业务状态机。
- 开发环境先自举 schema，再谈 tool log 和 checkpoint。
