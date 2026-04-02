# Models

## 结构
```text
Models/
├─ ModelProfile.cs   # 运行时可选模型档案与能力声明
└─ ModelGateway.cs   # 统一模型入口，按 profile 选择具体接入策略
```

## 原则
- profile 描述能力，gateway 负责接入，别把配置散进调用方。
- endpoint 和 secret 分离，密钥不进持久化模型。
