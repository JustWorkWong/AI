# TestDoubles

## 文件
- `StubDomainServiceClient.cs`: BFF 测试共用的领域服务替身。
- `StubAgentRuntimeClient.cs`: BFF 测试共用的 runtime 替身，可按场景注入委托。

## 原则
- 测试替身只表达输入输出，不重写业务逻辑。
- 共用替身放这里，别在每个测试文件里重复抄接口实现。
