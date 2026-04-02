# Presenters

## 文件
- `ReturnWorkbenchPresenter.cs`: 退货工作台的降级 suggestion 与展示占位映射规则。

## 原则
- 展示降级策略在这里收口，别把占位文案散落到 endpoint。
- presenter 只组织页面输出形状，不拥有业务规则。
