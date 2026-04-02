# Storage

## 文件
- `ReturnAttachment.cs`: 附件元数据。
- `IObjectStorage.cs`: 对象存储抽象。
- `MinioObjectStorage.cs`: MinIO 实现。
- `NoOpObjectStorage.cs`: 本地开发与测试默认占位实现。

## 原则
- 领域服务只依赖抽象，不直接依赖具体对象存储厂商。
