# .github

## 结构
```text
.github/
└─ workflows/   # CI 与发布流水线
```

## 原则
- workflow 只编排验证与产物生成，不藏业务逻辑。
- CI 复用本地命令，别造第二套构建方式。
