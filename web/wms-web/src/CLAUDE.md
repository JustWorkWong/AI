# src

## 结构
```text
src/
├─ main.ts        # 应用入口
├─ router.ts      # 路由
├─ App.vue        # 外层壳
├─ styles.css     # 全局风格变量与基础样式
├─ lib/           # AG-UI 与 BFF API 适配
└─ pages/         # Dashboard / Returns / SOP 页面
```

## 原则
- 页面状态留在页面里，协议细节下沉到 lib。
- 页面名称直接对应业务场景。
