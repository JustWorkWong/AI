# Sop

## 文件
- `SopDocument.cs`: SOP 文档元数据与发布状态。
- `SopChunk.cs`: 面向检索的 SOP 切块。
- `SopChunker.cs`: 将原始 SOP 正文切成带步骤号的稳定片段。

## 原则
- 文档元数据和切块分开，候选筛选与证据读取各做一件事。
