# Commands

## 文件
- `CommandDeduplication.cs`: 幂等命令去重记录。

## 原则
- 所有副作用命令先过幂等表，再落业务状态。
