# TestHost

## 文件
- `BffTestApplicationFactory.cs`: BFF 测试统一宿主，收口 DI 替换和测试环境装配。

## 原则
- 测试宿主只负责启动方式与依赖替换，别塞断言逻辑。
- 端点测试应描述场景，不应反复手写宿主装配样板。
