# Tools

## 结构
```text
Tools/
├─ GetReturnOrderTool.cs              # 读取退货单与质检上下文
├─ SearchSopTool.cs                   # 查询退货处置相关 SOP
├─ SearchHistoricalCasesTool.cs       # 查询历史处置案例
├─ RequestDispositionApprovalTool.cs  # 发起处置审批
├─ ApplyDispositionDecisionTool.cs    # 写回处置决定
├─ SearchSopCandidatesTool.cs         # 查询 SOP 候选文档
├─ RetrieveSopChunksTool.cs           # 拉取候选文档切块
└─ RankSopEvidenceTool.cs             # 对证据切块重排
```

## 原则
- tool 是边界适配层，不写业务状态机。
- 每个 tool 只做一类 IO，输入输出显式建模。
