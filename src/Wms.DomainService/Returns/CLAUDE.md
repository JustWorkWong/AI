# Returns

## 文件
- `ReturnOrder.cs`: 退货单根实体。
- `QualityInspection.cs`: 质检记录，保留状态与备注。
- `DispositionDecision.cs`: 最终处置决定。
- `HistoricalCaseView.cs`: 历史案例只读投影。
- `DispositionPolicy.cs`: 质检条件到允许处置的最小策略。

## 原则
- 先做最小闭环，规则和状态分开。
