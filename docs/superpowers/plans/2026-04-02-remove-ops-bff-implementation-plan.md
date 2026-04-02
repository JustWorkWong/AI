# Remove Ops.Bff Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove `Ops.Bff` completely and let the Vue frontend call `Wms.DomainService` and `Agent.Runtime` through `Gateway.Yarp` directly.

**Architecture:** Keep `Gateway.Yarp` as a pure reverse proxy, move page aggregation into the frontend state layer, and make `Wms.DomainService` plus `Agent.Runtime` expose clear, stable, directly consumable contracts. The migration succeeds only if runtime/domain error semantics are explicit and the frontend can run both return-disposition and SOP flows without any `BFF`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, YARP, Aspire, Vue 3, TypeScript, Vite, xUnit, Vitest, PostgreSQL, OpenTelemetry.

---

## Planned File Structure

### Backend

- `d:\AI\src\Gateway.Yarp\Program.cs`: keep only proxy bootstrapping and health endpoints.
- `d:\AI\src\Gateway.Yarp\appsettings.json`: replace `ops-bff` cluster with explicit `domain` and `runtime` routes.
- `d:\AI\src\Wms.AppHost\AppHost.cs`: stop creating `ops-bff`; wire gateway directly to `domain` and `runtime`.
- `d:\AI\src\Wms.DomainService\Endpoints\*.cs`: expose direct read/write endpoints with clear `ProblemDetails` behavior.
- `d:\AI\src\Agent.Runtime\Program.cs`: expose direct suggestion, execution, approval, trace, SOP, and SSE endpoints with the same error contract.

### Frontend

- `d:\AI\web\wms-web\src\lib\api.ts`: replace BFF endpoints with direct `domain` and `runtime` clients.
- `d:\AI\web\wms-web\src\composables\useReturnWorkbench.ts`: aggregate order + suggestion in the frontend.
- `d:\AI\web\wms-web\src\pages\SopAssistPage.vue`: call runtime/domain directly.
- `d:\AI\web\wms-web\src\lib\agui.ts`: point SSE subscription to runtime path.

### Deletion Targets

- `d:\AI\src\Ops.Bff\`
- `d:\AI\tests\Ops.Bff.Tests\`
- `d:\AI\WmsAiPlatform.sln` project entries for both deleted projects.

### Docs

- `d:\AI\src\Gateway.Yarp\CLAUDE.md`
- `d:\AI\src\Wms.AppHost\CLAUDE.md`
- `d:\AI\web\wms-web\src\CLAUDE.md`
- remove stale `CLAUDE.md` files under deleted directories as part of directory deletion.

---

### Task 1: Freeze Direct Backend Contracts Before Deleting BFF

**Files:**
- Modify: `d:\AI\src\Wms.DomainService\Program.cs`
- Modify: `d:\AI\src\Wms.DomainService\Endpoints\ReturnReadEndpoints.cs`
- Modify: `d:\AI\src\Wms.DomainService\Endpoints\ApprovalEndpoints.cs`
- Modify: `d:\AI\src\Wms.DomainService\Endpoints\SopReadEndpoints.cs`
- Modify: `d:\AI\src\Agent.Runtime\Program.cs`
- Test: `d:\AI\tests\Wms.DomainService.IntegrationTests\*.cs`
- Test: `d:\AI\tests\Agent.Runtime.Tests\*.cs`

- [ ] **Step 1: Write failing integration tests for direct error semantics**

```csharp
[Fact]
public async Task Get_return_should_emit_problem_details_when_missing()
{
    var response = await _client.GetAsync("/internal/returns/00000000-0000-0000-0000-000000000000");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal(404, body!.Status);
    Assert.NotNull(body.Detail);
}
```

```csharp
[Fact]
public async Task Get_suggestion_should_emit_problem_details_when_return_missing()
{
    var response = await _client.GetAsync("/internal/runtime/dispositions/00000000-0000-0000-0000-000000000000");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal(404, body!.Status);
}
```

- [ ] **Step 2: Run only the new failing tests**

Run: `dotnet test .\tests\Wms.DomainService.IntegrationTests\Wms.DomainService.IntegrationTests.csproj --filter ProblemDetails`

Expected: FAIL because current endpoints still rely on BFF-era assumptions or inconsistent payloads.

- [ ] **Step 3: Add one shared ProblemDetails helper pattern to both services**

```csharp
static IResult NotFoundProblem(HttpContext httpContext, string title, string detail)
{
    return Results.Problem(
        title: title,
        detail: detail,
        statusCode: StatusCodes.Status404NotFound,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = httpContext.TraceIdentifier
        });
}
```

- [ ] **Step 4: Update endpoints to use explicit 404/409/422/5xx semantics**

```csharp
app.MapGet("/internal/returns/{returnOrderId:guid}", async (
    Guid returnOrderId,
    WmsDbContext db,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var order = await db.ReturnOrders.FindAsync([returnOrderId], cancellationToken);
    return order is null
        ? NotFoundProblem(httpContext, "Return order not found", $"Return order '{returnOrderId}' does not exist.")
        : Results.Ok(order.ToDto());
});
```

- [ ] **Step 5: Re-run focused backend tests**

Run:
- `dotnet test .\tests\Wms.DomainService.IntegrationTests\Wms.DomainService.IntegrationTests.csproj`
- `dotnet test .\tests\Agent.Runtime.Tests\Agent.Runtime.Tests.csproj`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add src/Wms.DomainService src/Agent.Runtime tests/Wms.DomainService.IntegrationTests tests/Agent.Runtime.Tests
git commit -m "feat: standardize direct domain and runtime error contracts"
```

---

### Task 2: Route Gateway Directly To Domain And Runtime

**Files:**
- Modify: `d:\AI\src\Gateway.Yarp\appsettings.json`
- Modify: `d:\AI\src\Gateway.Yarp\Program.cs`
- Modify: `d:\AI\src\Wms.AppHost\AppHost.cs`
- Modify: `d:\AI\tests\Architecture.Tests\AppHostProjectTests.cs`
- Create or Modify: `d:\AI\tests\Architecture.Tests\GatewayRouteTests.cs`
- Modify: `d:\AI\src\Gateway.Yarp\CLAUDE.md`
- Modify: `d:\AI\src\Wms.AppHost\CLAUDE.md`

- [ ] **Step 1: Write a failing route test for domain/runtime direct paths**

```csharp
[Fact]
public void Gateway_routes_should_not_reference_ops_bff()
{
    var json = File.ReadAllText(@"d:\AI\src\Gateway.Yarp\appsettings.json");
    Assert.DoesNotContain("ops-bff", json);
    Assert.Contains("\"domain\"", json);
    Assert.Contains("\"runtime\"", json);
}
```

- [ ] **Step 2: Run the route test**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj --filter Gateway_routes_should_not_reference_ops_bff`

Expected: FAIL because current gateway still forwards `/api/**` to `ops-bff`.

- [ ] **Step 3: Replace the gateway config with explicit route ownership**

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

- [ ] **Step 4: Remove `ops-bff` from AppHost topology**

```csharp
builder.AddProject<Projects.Gateway_Yarp>("gateway-yarp")
    .WithReference(auth)
    .WithReference(domain)
    .WithReference(runtime);
```

- [ ] **Step 5: Re-run architecture tests**

Run:
- `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add src/Gateway.Yarp src/Wms.AppHost tests/Architecture.Tests
git commit -m "refactor: route gateway directly to domain and runtime"
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

- [ ] **Step 1: Write the failing frontend test for frontend-side aggregation**

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

Expected: FAIL because `getReturnWorkbench()` still expects a BFF-shaped payload.

- [ ] **Step 3: Split the API client into direct domain/runtime calls**

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

- [ ] **Step 4: Rewrite `useReturnWorkbench` to aggregate in the composable**

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

- [ ] **Step 5: Re-run frontend unit tests**

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
- Modify: `d:\AI\web\wms-web\src\pages\CLAUDE.md`
- Add or Modify: `d:\AI\web\wms-web\src\pages\SopAssistPage.test.ts`

- [ ] **Step 1: Write the failing SOP page test against direct runtime paths**

```ts
it("advances SOP step through runtime api", async () => {
  global.fetch = vi.fn().mockResolvedValue(okJson(nextStepPayload));
  await advanceSopStep("session-1", "STEP-02", "ack");
  expect(global.fetch).toHaveBeenCalledWith(
    "/api/runtime/internal/sop/sessions/session-1/steps",
    expect.objectContaining({ method: "POST" })
  );
});
```

- [ ] **Step 2: Run the SOP-focused test**

Run: `npm test -- SopAssistPage`

Expected: FAIL because current SOP APIs still target `/api/sop/...`.

- [ ] **Step 3: Switch SOP API and SSE endpoints to runtime-owned paths**

```ts
export async function advanceSopStep(sessionId: string, stepCode: string, userInput: string) {
  const response = await fetch(`/api/runtime/internal/sop/sessions/${sessionId}/steps`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ stepCode, userInput })
  });

  return readJson<SopExecutionViewDto>(response);
}
```

```ts
export function subscribeSopEvents(sessionId: string) {
  return new EventSource(`/api/runtime/internal/sop/sessions/${sessionId}/events`);
}
```

- [ ] **Step 4: Re-run SOP frontend tests**

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

### Task 5: Remove Ops.Bff From Solution And Repository

**Files:**
- Modify: `d:\AI\WmsAiPlatform.sln`
- Delete: `d:\AI\src\Ops.Bff\`
- Delete: `d:\AI\tests\Ops.Bff.Tests\`
- Modify: `d:\AI\src\CLAUDE.md`
- Modify: `d:\AI\web\wms-web\src\CLAUDE.md`

- [ ] **Step 1: Write an architecture test that the solution no longer references `Ops.Bff`**

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

Expected: FAIL because the solution still includes both projects.

- [ ] **Step 3: Remove the projects and delete the directories**

```powershell
dotnet sln .\WmsAiPlatform.sln remove .\src\Ops.Bff\Ops.Bff.csproj
dotnet sln .\WmsAiPlatform.sln remove .\tests\Ops.Bff.Tests\Ops.Bff.Tests.csproj
Remove-Item -Recurse -LiteralPath .\src\Ops.Bff
Remove-Item -Recurse -LiteralPath .\tests\Ops.Bff.Tests
```

- [ ] **Step 4: Update top-level architecture docs to reflect the new shape**

```md
- `Gateway.Yarp`: north-south edge only; no page aggregation.
- `Wms.DomainService`: business truth.
- `Agent.Runtime`: AI workflow truth and SSE surface.
- `web/wms-web`: frontend performs page aggregation.
```

- [ ] **Step 5: Re-run architecture tests**

Run: `dotnet test .\tests\Architecture.Tests\Architecture.Tests.csproj`

Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add WmsAiPlatform.sln src/CLAUDE.md web/wms-web/src/CLAUDE.md tests/Architecture.Tests
git add -u
git commit -m "refactor: remove ops bff from solution"
```

---

### Task 6: Full Regression And Persistence Smoke

**Files:**
- Modify if needed: `d:\AI\web\wms-web\vite.config.ts`
- Modify if needed: `d:\AI\README.md`
- Modify if needed: `d:\AI\src\Gateway.Yarp\Properties\launchSettings.json`

- [ ] **Step 1: Run the full automated suite sequentially**

Run:
- `dotnet build .\WmsAiPlatform.sln`
- `dotnet test .\WmsAiPlatform.sln --no-build`
- `npm test`
- `npm run build`

Expected: PASS

- [ ] **Step 2: Run local apphost and smoke the direct-call flows**

Run:
- `dotnet run --project .\src\Wms.AppHost\Wms.AppHost.csproj`
- `npm run dev -- --host 127.0.0.1`

Smoke path:
- open `/returns/11111111-1111-1111-1111-111111111111`
- execute disposition
- approve disposition
- verify trace renders
- open `/sop/22222222-2222-2222-2222-222222222222`
- advance one step

Expected: UI remains functional without any `Ops.Bff` process.

- [ ] **Step 3: Verify persistence after restart**

Run:
- stop AppHost
- start AppHost again
- query the same return workflow and SOP session

Expected:
- return state still `Disposed`
- approval still visible
- runtime trace still readable

- [ ] **Step 4: Commit**

```powershell
git add README.md src/Gateway.Yarp/Properties/launchSettings.json web/wms-web/vite.config.ts
git commit -m "test: verify direct gateway flow after removing ops bff"
```

---

## Self-Review

### Spec Coverage

- 删除 `Ops.Bff` 与 `Ops.Bff.Tests`: Task 5
- `Gateway -> Domain/Runtime` 直连: Task 2
- 前端承担聚合: Task 3 and Task 4
- 不做 BFF 降级: Task 3
- 统一错误契约: Task 1
- 保持功能、测试、持久化: Task 6

### Placeholder Scan

- 无 `TODO/TBD`
- 每个任务都给出文件、命令、最小代码片段、预期结果

### Type Consistency

- 前端聚合拆成 `getReturnOrder + getDispositionSuggestion`
- 直连路径统一使用 `/api/domain/...` 与 `/api/runtime/...`
- 错误输出统一使用 `ProblemDetails`
