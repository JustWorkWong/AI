# Clients

## 文件
- `DomainKnowledgeClient.cs`: 访问 `Wms.DomainService` 只读知识边界。

## 原则
- runtime 只通过 client 读业务真相，不绕过 HTTP 边界直连库。
