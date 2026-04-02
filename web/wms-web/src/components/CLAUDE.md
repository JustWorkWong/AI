# components

## 结构
```text
components/
└─ return-workbench/  # 退货工作台拆出的展示组件
```

## 原则
- 组件只做展示和事件抛出，不直连路由和 API。
- 页面负责拼装组件，composable 负责状态机。
