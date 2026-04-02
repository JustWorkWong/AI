# Wms AI Platform

WMS AI 面试项目，当前聚焦两条生产型流程：

- `退货质检与处置`
- `SOP 辅助执行`

## 架构

- `Wms.DomainService` 持有业务真相。
- `Agent.Runtime` 持有 workflow、checkpoint、tool 观测、AG-UI 事件。
- `Ops.Bff` 面向 Vue 页面做 typed 聚合与 SSE 桥接。
- `Wms.AppHost` 用 Aspire 做本地编排和 Kubernetes Helm 产物生成。

## 前提

- `.NET SDK 10.0.201+`
- Docker Desktop
- Node.js 22+
- Aspire CLI

Windows 安装 Aspire CLI:

```powershell
irm https://aspire.dev/install.ps1 | iex
```

## 本地开发

启动 Aspire 本地编排:

```powershell
dotnet run --project .\src\Wms.AppHost\Wms.AppHost.csproj
```

启动 Vue 前端:

```powershell
Set-Location .\web\wms-web
npm install
npm run dev
```

## 验证

```powershell
dotnet test .\WmsAiPlatform.sln
Set-Location .\web\wms-web
npm run build
```

## 用 Aspire 生成 Kubernetes 产物

Windows:

```powershell
.\build\publish-k8s.ps1
helm upgrade --install wms-ai-platform .\artifacts\k8s -f .\deploy\values\production.values.yaml
```

Linux / CI:

```bash
./build/publish-k8s.sh ./artifacts/k8s Production
```

脚本底层调用 `aspire publish --apphost ... --output-path ... --environment Production`，生成 Helm chart，再由你自己的 Helm / GitOps 流程部署。

## 文档

- 设计文档: `docs/superpowers/specs/`
- 实现计划: `docs/superpowers/plans/`
