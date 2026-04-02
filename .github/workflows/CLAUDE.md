# workflows

## 文件
- `ci.yml`: 解构建、测试、前端构建。
- `publish-k8s.yml`: 通过 Aspire 生成 K8s Helm 产物并上传。

## 原则
- CI 先验证，再发布。
- 发布只生成产物，不直接部署到集群。
