# Architecture.Tests

## 文件
- `Architecture.Tests.csproj`: 架构约束测试项目。
- `AppHostProjectTests.cs`: 校验 Aspire AppHost 与发布骨架没有退化。

## 原则
- 这里测试结构，不测试业务。
- 任何会影响解决方案骨架、引用关系、发布路径的改动，都先在这里加约束。
