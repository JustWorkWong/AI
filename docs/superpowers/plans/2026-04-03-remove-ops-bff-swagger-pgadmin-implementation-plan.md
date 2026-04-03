# Remove Ops.Bff, Keep Swagger, Add pgAdmin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove `Ops.Bff`, route the frontend directly to `Wms.DomainService` and `Agent.Runtime` through `Gateway.Yarp`, keep backend Swagger available, and add `pgAdmin 4` to Aspire for PostgreSQL inspection.

**Architecture:** `Gateway.Yarp` becomes a pure reverse proxy, `Vue` owns page aggregation, `Wms.DomainService` and `Agent.Runtime` stay as the only backend truth boundaries, and `Wms.AppHost` adds `pgAdmin 4` as a development-only data inspection tool with persisted configuration. Swagger remains on the real backend services instead of being re-aggregated.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, YARP, Aspire, Vue 3, TypeScript, Vite, PostgreSQL, pgAdmin 4, xUnit, Vitest, OpenAPI.

---

## Planned File Structure

### Backend

- `d:\AI\src\Gateway.Yarp\Program.cs`: reverse proxy bootstrap only.
- `d:\AI\src\Gateway.Yarp\appsettings.json`: direct `domain`/`runtime` route map.
- `d:\AI\src\Wms.AppHost\AppHost.cs`: remove `ops-bff`, add `pgadmin`.
- `d:\AI\src\Wms.DomainService\Program.cs`: keep OpenAPI reachable.
- `d:\AI\src\Agent.Runtime\Program.cs`: keep OpenAPI reachable.

### Frontend

- `d:\AI\web\wms-web\src\lib\api.ts`: direct calls to `/api/domain/...` and `/api/runtime/...`.
- `d:\AI\web\wms-web\src\lib\agui.ts`: direct runtime SSE endpoint.
- `d:\AI\web\wms-web\src\composables\useReturnWorkbench.ts`: frontend-side aggregation.
- `d:\AI\web\wms-web\src\pages\SopAssistPage.vue`: direct runtime/domain reads.

### Deletion Targets

- `d:\AI\src\Ops.Bff\`
- `d:\AI\tests\Ops.Bff.Tests\`

### Docs

- `d:\AI\README.md`
- `d:\AI\src\CLAUDE.md`
- `d:\AI\src\Gateway.Yarp\CLAUDE.md`
- `d:\AI\src\Wms.AppHost\CLAUDE.md`
- `d:\AI\web\wms-web\src\CLAUDE.md`

---

### Task 1: Route Gateway Directly To Domain And Runtime

**Files:**
- Modify: `d:\AI\src\Gateway.Yarp\appsettings.json`
- Modify: `d:\AI\src\Gateway.Yarp\Program.cs`
- Modify: `d:\AI\tests\Architecture.Tests\GatewayRouteTests.cs`
- Modify: `d:\AI\src\Gateway.Yarp\CLAUDE.md`

- [ ] **Step 1: Write a failing architecture test that `ops-bff` is no longer in gateway config**

```csharp
[Fact]
public void Gateway_routes_should_target_domain_and_runtime_only()
{
    var json = File.ReadAllText(@"d:\AI\src\Gateway.Yarp\appsettings.json");
    Assert.DoesNotContain("ops-bff", json);
    Assert.Contains("\"domain\"", json);
    Assert.Contains("\"runtime\"", json);
}
```

- [ ] **Step 2: Run the failing route test**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj --filter Gateway_routes_should_target_domain_and_runtime_only`

Expected: FAIL because gateway still points all `/api/**` traffic to `ops-bff`.

- [ ] **Step 3: Replace gateway config with explicit direct routes**

```json
"Routes": {
  "domain-api": {
    "ClusterId": "domain",
    "Match": { "Path": "/api/domain/{**catch-all}" }
  },
  "runtime-api": {
    "ClusterId": "runtime",
    "Match": { "Path": "/api/runtime/{**catch-all}" }
  }
},
"Clusters": {
  "domain": {
    "Destinations": {
      "default": { "Address": "http://wms-domain-service" }
    }
  },
  "runtime": {
    "Destinations": {
      "default": { "Address": "http://agent-runtime" }
    }
  }
}
```

- [ ] **Step 4: Re-run architecture tests**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add src/Gateway.Yarp tests/Architecture.Tests
git commit -m "refactor: route gateway directly to domain and runtime"
```

---

### Task 2: Keep Swagger Available On Domain And Runtime

**Files:**
- Modify: `d:\AI\src\Wms.DomainService\Program.cs`
- Modify: `d:\AI\src\Agent.Runtime\Program.cs`
- Create or Modify: `d:\AI\tests\Architecture.Tests\OpenApiAvailabilityTests.cs`
- Modify: `d:\AI\README.md`

- [ ] **Step 1: Write failing tests for OpenAPI availability**

```csharp
[Fact]
public async Task Domain_service_should_expose_openapi_in_development()
{
    await using var app = await TestAppFactory.CreateDomainServiceAsync(_fixture.ConnectionString);
    var client = app.CreateClient();

    var response = await client.GetAsync("/openapi/v1.json");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

```csharp
[Fact]
public async Task Runtime_service_should_expose_openapi_in_testing()
{
    using var app = await CreateAppAsync();
    var client = app.CreateClient();

    var response = await client.GetAsync("/openapi/v1.json");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

- [ ] **Step 2: Run the failing OpenAPI tests**

Run:
- `dotnet test .\tests\Wms.DomainService.IntegrationTests\Wms.DomainService.IntegrationTests.csproj --filter openapi`
- `dotnet test .\tests\Agent.Runtime.Tests\Agent.Runtime.Tests.csproj --filter openapi`

Expected: FAIL if current env gating hides OpenAPI from testing/development paths used by the factories.

- [ ] **Step 3: Make OpenAPI reachable in the environments used for local dev and tests**

```csharp
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
}
```

- [ ] **Step 4: Update README with real Swagger URLs**

```md
- `Wms.DomainService`: `http://localhost:<port>/openapi/v1.json`
- `Agent.Runtime`: `http://localhost:<port>/openapi/v1.json`
```

- [ ] **Step 5: Re-run focused tests**

Run:
- `dotnet test .\tests\Wms.DomainService.IntegrationTests\Wms.DomainService.IntegrationTests.csproj`
- `dotnet test .\tests\Agent.Runtime.Tests\Agent.Runtime.Tests.csproj`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add src/Wms.DomainService src/Agent.Runtime README.md tests
git commit -m "feat: keep domain and runtime swagger available"
```

---

### Task 3: Move Return Workbench Aggregation Into Frontend

**Files:**
- Modify: `d:\AI\web\wms-web\src\lib\api.ts`
- Modify: `d:\AI\web\wms-web\src\composables\useReturnWorkbench.ts`
- Modify: `d:\AI\web\wms-web\src\composables\useReturnWorkbench.test.ts`
- Modify: `d:\AI\web\wms-web\src\pages\ReturnWorkbenchPage.test.ts`
- Modify: `d:\AI\web\wms-web\src\lib\CLAUDE.md`
- Modify: `d:\AI\web\wms-web\src\composables\CLAUDE.md`

- [ ] **Step 1: Write the failing frontend aggregation test**

```ts
it("loads order from domain and suggestion from runtime", async () => {
  const api = createApiStub({
    getReturnOrder: vi.fn().mockResolvedValue(order),
    getDispositionSuggestion: vi.fn().mockResolvedValue(suggestion)
  });

  const workbench = createReturnWorkbench(api);
  await workbench.load(order.returnOrderId);

  expect(workbench.order.value?.returnOrderId).toBe(order.returnOrderId);
  expect(workbench.suggestion.value?.suggestedOutcome).toBe(suggestion.suggestedOutcome);
});
```

- [ ] **Step 2: Run the failing frontend test**

Run: `npm test -- useReturnWorkbench`

Expected: FAIL because current API still expects BFF-style workbench payload.

- [ ] **Step 3: Split API calls by backend ownership**

```ts
export async function getReturnOrder(returnOrderId: string): Promise<ReturnOrderDto> {
  const response = await fetch(`/api/domain/internal/returns/${returnOrderId}`);
  return readJson<ReturnOrderDto>(response);
}

export async function getDispositionSuggestion(returnOrderId: string): Promise<DispositionSuggestionDto> {
  const response = await fetch(`/api/runtime/internal/runtime/dispositions/${returnOrderId}`);
  return readJson<DispositionSuggestionDto>(response);
}
```

- [ ] **Step 4: Rewrite the composable to aggregate in the frontend**

```ts
async function load(returnOrderId: string) {
  errorMessage.value = "";
  const [loadedOrder, loadedSuggestion] = await Promise.all([
    api.getReturnOrder(returnOrderId),
    api.getDispositionSuggestion(returnOrderId)
  ]);

  order.value = loadedOrder;
  suggestion.value = loadedSuggestion;
}
```

- [ ] **Step 5: Re-run frontend tests**

Run:
- `npm test -- useReturnWorkbench`
- `npm test -- ReturnWorkbenchPage`
- `npm run build`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add web/wms-web/src/lib web/wms-web/src/composables web/wms-web/src/pages
git commit -m "refactor: move return workbench aggregation to frontend"
```

---

### Task 4: Move SOP Page To Direct Runtime And Domain Calls

**Files:**
- Modify: `d:\AI\web\wms-web\src\lib\api.ts`
- Modify: `d:\AI\web\wms-web\src\lib\agui.ts`
- Modify: `d:\AI\web\wms-web\src\pages\SopAssistPage.vue`
- Add or Modify: `d:\AI\web\wms-web\src\pages\SopAssistPage.test.ts`
- Modify: `d:\AI\web\wms-web\src\pages\CLAUDE.md`

- [ ] **Step 1: Write the failing SOP direct-call test**

```ts
it("advances SOP step through runtime api", async () => {
  global.fetch = vi.fn().mockResolvedValue(okJson(nextStepPayload));
  await advanceSopStep("session-1", "STEP-02", "ack");
  expect(global.fetch).toHaveBeenCalledWith(
    "/api/runtime/internal/runtime/sop/session-1/steps",
    expect.objectContaining({ method: "POST" })
  );
});
```

- [ ] **Step 2: Run the failing SOP test**

Run: `npm test -- SopAssistPage`

Expected: FAIL because current code still targets BFF-shaped endpoints.

- [ ] **Step 3: Point SOP page and SSE helper directly at runtime**

```ts
export async function advanceSopStep(sessionId: string, stepCode: string, userInput: string) {
  const response = await fetch(`/api/runtime/internal/runtime/sop/${sessionId}/steps`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ stepCode, userInput })
  });

  return readJson<SopExecutionViewDto>(response);
}
```

```ts
export function subscribeSopEvents(sessionId: string) {
  return new EventSource(`/api/runtime/internal/runtime/sop/${sessionId}/events`);
}
```

- [ ] **Step 4: Re-run SOP tests**

Run:
- `npm test -- SopAssistPage`
- `npm run build`

Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add web/wms-web/src/lib web/wms-web/src/pages
git commit -m "refactor: move sop page to direct runtime endpoints"
```

---

### Task 5: Remove Ops.Bff From AppHost, Solution, And Repository

**Files:**
- Modify: `d:\AI\src\Wms.AppHost\AppHost.cs`
- Modify: `d:\AI\WmsAiPlatform.sln`
- Delete: `d:\AI\src\Ops.Bff\`
- Delete: `d:\AI\tests\Ops.Bff.Tests\`
- Modify: `d:\AI\src\CLAUDE.md`
- Modify: `d:\AI\web\wms-web\src\CLAUDE.md`
- Create or Modify: `d:\AI\tests\Architecture.Tests\SolutionShapeTests.cs`

- [ ] **Step 1: Write a failing solution-shape test**

```csharp
[Fact]
public void Solution_should_not_reference_ops_bff_projects()
{
    var solution = File.ReadAllText(@"d:\AI\WmsAiPlatform.sln");
    Assert.DoesNotContain("Ops.Bff", solution);
    Assert.DoesNotContain("Ops.Bff.Tests", solution);
}
```

- [ ] **Step 2: Run the failing architecture test**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj --filter Solution_should_not_reference_ops_bff_projects`

Expected: FAIL because both projects still exist in solution and AppHost.

- [ ] **Step 3: Remove `ops-bff` from AppHost**

```csharp
builder.AddProject<Projects.Gateway_Yarp>("gateway-yarp")
    .WithReference(auth)
    .WithReference(domain)
    .WithReference(runtime);
```

- [ ] **Step 4: Remove the projects and delete directories**

```powershell
dotnet sln .\WmsAiPlatform.sln remove .\src\Ops.Bff\Ops.Bff.csproj
dotnet sln .\WmsAiPlatform.sln remove .\tests\Ops.Bff.Tests\Ops.Bff.Tests.csproj
Remove-Item -Recurse -LiteralPath .\src\Ops.Bff
Remove-Item -Recurse -LiteralPath .\tests\Ops.Bff.Tests
```

- [ ] **Step 5: Re-run architecture tests**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add src/Wms.AppHost WmsAiPlatform.sln src/CLAUDE.md web/wms-web/src/CLAUDE.md tests/Architecture.Tests
git add -u
git commit -m "refactor: remove ops bff from solution and topology"
```

---

### Task 6: Add pgAdmin 4 To Aspire With Persistence

**Files:**
- Modify: `d:\AI\src\Wms.AppHost\AppHost.cs`
- Modify: `d:\AI\src\Wms.AppHost\CLAUDE.md`
- Modify: `d:\AI\README.md`
- Create or Modify: `d:\AI\tests\Architecture.Tests\AppHostProjectTests.cs`

- [ ] **Step 1: Write a failing AppHost test for pgAdmin presence**

```csharp
[Fact]
public void AppHost_should_include_pgadmin_container()
{
    var appHost = File.ReadAllText(@"d:\AI\src\Wms.AppHost\AppHost.cs");
    Assert.Contains("pgadmin", appHost);
    Assert.Contains("dpage/pgadmin4", appHost);
}
```

- [ ] **Step 2: Run the failing AppHost test**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj --filter AppHost_should_include_pgadmin_container`

Expected: FAIL because AppHost has no pgAdmin yet.

- [ ] **Step 3: Add pgAdmin 4 container with persisted volume**

```csharp
_ = builder.AddContainer("pgadmin", "dpage/pgadmin4")
    .WithHttpEndpoint(targetPort: 80, name: "http")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@example.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin123456")
    .WithVolume("wms-pgadmin-data", "/var/lib/pgadmin")
    .WithReference(postgres);
```

- [ ] **Step 4: Update README with pgAdmin access notes**

```md
pgAdmin 4 is available from Aspire local orchestration and should be used to inspect:
- `wmsdb`
- `aidb`
```

- [ ] **Step 5: Re-run architecture tests**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add src/Wms.AppHost README.md tests/Architecture.Tests
git commit -m "feat: add pgadmin to aspire apphost"
```

---

### Task 7: Full Regression And Local Smoke

**Files:**
- Modify if needed: `d:\AI\web\wms-web\vite.config.ts`
- Modify if needed: `d:\AI\README.md`
- Modify if needed: `d:\AI\src\Gateway.Yarp\Properties\launchSettings.json`

- [ ] **Step 1: Run full automated verification**

Run:
- `dotnet build .\WmsAiPlatform.sln`
- `dotnet test .\WmsAiPlatform.sln --no-build`
- `npm test`
- `npm run build`

Expected: PASS

- [ ] **Step 2: Run local AppHost and smoke the system**

Run:
- `dotnet run --project .\src\Wms.AppHost\Wms.AppHost.csproj`
- `npm run dev -- --host 127.0.0.1` in `web/wms-web`

Smoke path:
- open return workbench and run execution flow
- approve disposition
- open SOP page and advance one step
- open Swagger for `Wms.DomainService`
- open Swagger for `Agent.Runtime`
- open `pgAdmin 4` and connect to PostgreSQL

Expected: all paths reachable with no `Ops.Bff` process.

- [ ] **Step 3: Verify persistence after restart**

Run:
- stop AppHost
- start AppHost again
- confirm Postgres data still exists
- confirm pgAdmin server profile still exists

Expected: persistence holds across restart.

- [ ] **Step 4: Commit**

```powershell
git add README.md web/wms-web/vite.config.ts src/Gateway.Yarp/Properties/launchSettings.json
git commit -m "test: verify direct gateway flows swagger and pgadmin"
```

---

## Self-Review

### Spec Coverage

- 删除 `Ops.Bff`: Task 5
- 网关直连 `Domain/Runtime`: Task 1
- 前端直连两个后端: Task 3 and Task 4
- 后端 Swagger 保留: Task 2
- `pgAdmin 4` 接入 Aspire: Task 6
- 回归与持久化 smoke: Task 7

### Placeholder Scan

- 无 `TODO/TBD`
- 每个任务都有文件、命令、最小代码和预期结果

### Type Consistency

- 直连路径统一为 `/api/domain/...` 与 `/api/runtime/...`
- OpenAPI 明确留在真实后端服务
- `pgAdmin 4` 只作为 AppHost 工具容器，不进入业务网关
