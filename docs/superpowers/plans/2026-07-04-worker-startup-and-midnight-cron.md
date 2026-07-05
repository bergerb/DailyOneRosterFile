# Worker Startup + Midnight Cron Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run OneRoster generation once at startup and then every night at local midnight using cron scheduling.

**Architecture:** Keep the existing `DailyFileGenerationWorker` and add a startup execution path plus cron-driven wait loop. Use Cronos (`0 0 * * *`) with local time for next-occurrence calculation, recomputing after each run. Keep existing generation + retention flow unchanged.

**Tech Stack:** .NET 10, ASP.NET Core `BackgroundService`, Cronos NuGet package, xUnit

## Global Constraints

- Run generation once on application startup before first wait.
- Use cron expression `0 0 * * *`.
- Use local server/container timezone for scheduling.
- Keep existing one-file retention behavior.
- On generation failure, log and continue to next cron occurrence.

---

## File Structure Map

- `src/BackgroundServices/DailyFileGenerationWorker.cs` — startup run + cron wait loop.
- `src/DailyOneRosterFile.Api.csproj` — add Cronos package reference.
- `src/DailyOneRosterFile.Api.Tests/DailyFileGenerationWorkerScheduleTests.cs` — cron schedule tests.
- `src/DailyOneRosterFile.sln` — include test project if not already included.
- `docs/plan.md` — source-of-truth status update.

### Task 1: Add cron scheduling dependency and startup+midnight run flow

**Files:**
- Modify: `src/DailyOneRosterFile.Api.csproj`
- Modify: `src/BackgroundServices/DailyFileGenerationWorker.cs`

**Interfaces:**
- Consumes: `IOneRosterFileGenerator.GenerateDailyFileAsync()`, existing retention cleanup method.
- Produces: `ExecuteAsync` behavior that runs once immediately, then waits by cron occurrence.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void NextOccurrence_FromAfternoon_IsMidnight()
{
    var now = new DateTime(2026, 7, 4, 15, 30, 0, DateTimeKind.Local);
    var next = DailyFileGenerationWorker.GetNextOccurrence(now);
    Assert.Equal(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Local), next);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter NextOccurrence_FromAfternoon_IsMidnight`
Expected: FAIL because `GetNextOccurrence` does not exist.

- [ ] **Step 3: Write minimal implementation**

```xml
<!-- src/DailyOneRosterFile.Api.csproj -->
<PackageReference Include="Cronos" Version="0.11.1" />
```

```csharp
// src/BackgroundServices/DailyFileGenerationWorker.cs
private static readonly CronExpression MidnightCron = CronExpression.Parse("0 0 * * *");

internal static DateTime GetNextOccurrence(DateTime nowLocal)
{
    return MidnightCron.GetNextOccurrence(nowLocal, TimeZoneInfo.Local)
        ?? throw new InvalidOperationException("Unable to compute next midnight occurrence.");
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Daily File Generation Worker started.");

    while (!stoppingToken.IsCancellationRequested)
    {
        await RunGenerationCycle(stoppingToken); // immediate on startup + each loop

        var now = DateTime.Now;
        var next = GetNextOccurrence(now);
        var delay = next - now;
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, stoppingToken);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter NextOccurrence_FromAfternoon_IsMidnight`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DailyOneRosterFile.Api.csproj src/BackgroundServices/DailyFileGenerationWorker.cs src/DailyOneRosterFile.Api.Tests
git commit -m "feat: schedule worker with startup run and midnight cron"
```

### Task 2: Add schedule boundary tests and verify app behavior contract

**Files:**
- Create: `src/DailyOneRosterFile.Api.Tests/DailyFileGenerationWorkerScheduleTests.cs`
- Modify: `src/DailyOneRosterFile.sln`
- Modify: `docs/plan.md`

**Interfaces:**
- Consumes: `DailyFileGenerationWorker.GetNextOccurrence(DateTime nowLocal)`.
- Produces: tests proving midnight scheduling boundaries and updated project plan status.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void NextOccurrence_FromJustBeforeMidnight_IsSameNightMidnight()
{
    var now = new DateTime(2026, 7, 4, 23, 59, 30, DateTimeKind.Local);
    var next = DailyFileGenerationWorker.GetNextOccurrence(now);
    Assert.Equal(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Local), next);
}

[Fact]
public void NextOccurrence_FromExactlyMidnight_IsNextDayMidnight()
{
    var now = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Local);
    var next = DailyFileGenerationWorker.GetNextOccurrence(now);
    Assert.Equal(new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Local), next);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter "NextOccurrence_FromJustBeforeMidnight_IsSameNightMidnight|NextOccurrence_FromExactlyMidnight_IsNextDayMidnight"`
Expected: FAIL before helper behavior is final/exposed.

- [ ] **Step 3: Write minimal implementation**

```csharp
// src/BackgroundServices/DailyFileGenerationWorker.cs
internal static DateTime GetNextOccurrence(DateTime nowLocal) { ... }
```

```markdown
<!-- docs/plan.md -->
- [x] Configure worker to run once on startup and then at local midnight via cron.
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj`
Expected: PASS.

Run: `dotnet build src/DailyOneRosterFile.sln -nologo`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DailyOneRosterFile.Api.Tests src/DailyOneRosterFile.sln docs/plan.md
git commit -m "test: add midnight cron schedule boundary coverage"
```
