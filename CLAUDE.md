# WmsAiPlatform

## 结构
```text
.
├─ docs/    # 设计、计划、评审记录
├─ src/     # 运行时代码
├─ tests/   # 单元、集成、架构测试
├─ WmsAiPlatform.sln
├─ Directory.Packages.props
├─ Directory.Build.props
├─ global.json
└─ README.md
```

## 原则
- 先守住单一真相源，再谈微服务拆分的漂亮话。
- 业务状态只进 `Wms.DomainService`，AI 运行状态只进 `Agent.Runtime`。
- `Aspire AppHost` 负责本地编排与 K8s 产物生成，不替业务做判断。
- 文档、代码、测试一起演化，任何一侧滞后都算设计退化。
