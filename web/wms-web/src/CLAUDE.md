# src

## 结构
```text
src/
├─ main.ts        # 应用入口
├─ router.ts      # 路由
├─ App.vue        # 外层壳
├─ styles.css     # 全局风格变量与基础样式
├─ lib/           # AG-UI 订阅
└─ pages/         # Dashboard / Returns / SOP 页面
```

## 原则
- 数据拉取尽量贴近页面，复杂共享逻辑再下沉。
- 页面名称直接对应业务场景。
