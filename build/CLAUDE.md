# build

## 文件
- `publish-k8s.ps1`: Windows 下通过 Aspire CLI 生成 K8s Helm 产物。
- `publish-k8s.sh`: Linux CI 下通过 Aspire CLI 生成 K8s Helm 产物。

## 原则
- 脚本围绕 `aspire publish`，不手写工作负载 YAML。
- 参数显式，路径固定在仓库内。
