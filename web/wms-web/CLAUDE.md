# wms-web

## 结构
```text
wms-web/
├─ package.json      # 前端依赖与脚本
├─ vite.config.ts    # Vite 开发与构建配置
├─ tsconfig.json     # TypeScript 配置
├─ index.html        # 挂载入口
└─ src/              # 页面、路由、AG-UI 订阅与样式
```

## 原则
- 页面围绕业务动作组织，不围绕组件炫技组织。
- SSE 事件和 BFF DTO 是唯一真相源。
