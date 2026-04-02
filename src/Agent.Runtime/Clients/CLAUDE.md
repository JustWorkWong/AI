# Clients

## 文件
- `DomainKnowledgeClient.cs`: 访问 `Wms.DomainService` 只读知识边界。
- `DomainDispositionClient.cs`: 发起审批与落处置决定的命令边界。

## 原则
- runtime 只通过 client 读业务真相，不绕过 HTTP 边界直连库。
- 写命令和读查询分开，别把命名搞成一锅粥。
