# Daily File Retention Volume Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure generated OneRoster ZIP files are stored in the Docker volume path and only the newest ZIP is retained after each worker run.

**Architecture:** Use one canonical storage path for both generation and download (`GeneratedFiles` locally, `/app/GeneratedFiles` in container). Update generator and worker so generation returns the created ZIP path and worker retention deletes all other ZIPs. Keep API reads aligned to the same path.

**Tech Stack:** .NET 10 Web API, BackgroundService, xUnit test project, Docker Compose named volume

## Global Constraints

- Keep retention policy: exactly one ZIP total after each generation cycle.
- Keep named Docker volume `generated_files` mounted to `/app/GeneratedFiles`.
- Keep API endpoint contract `GET /api/files/latest-oneroster`.
- Keep behavior safe on generation failure: do not delete existing ZIP files if no new ZIP is produced.

---

## File Structure Map

- `src/Program.cs` — service registration and canonical storage path wiring.
- `src/Services/OneRosterFileGenerator.cs` — generation contract and generated file discovery.
- `src/BackgroundServices/DailyFileGenerationWorker.cs` — run cycle, retention cleanup, logging.
- `src/Controllers/FilesController.cs` — latest-file API read path alignment.
- `src/appsettings.json` + `src/appsettings.Development.json` — storage path config.
- `src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj` — test project.
- `src/DailyOneRosterFile.Api.Tests/Retention/FileRetentionTests.cs` — retention behavior tests.
- `src/DailyOneRosterFile.Api.Tests/Controllers/FilesControllerTests.cs` — download endpoint tests.
- `src/DailyOneRosterFile.sln` — include test project.
- `docker-compose.yml` — verify `/app/GeneratedFiles` volume mapping.
- `docs/plan.md` — source-of-truth status update.

### Task 1: Introduce canonical storage path configuration

**Files:**
- Modify: `src/Program.cs`
- Modify: `src/appsettings.json`
- Modify: `src/appsettings.Development.json`

**Interfaces:**
- Consumes: existing `IOneRosterFileGenerator` DI registration.
- Produces: `storagePath` string used by generator, worker, and controller constructors.

- [ ] **Step 1: Write the failing configuration test**

```csharp
[Fact]
public void UsesConfiguredStoragePathForServices()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:GeneratedFilesPath"] = "GeneratedFiles"
        })
        .Build();

    var services = new ServiceCollection();
    var storagePath = config["Storage:GeneratedFilesPath"]!;
    services.AddSingleton<IOneRosterFileGenerator>(_ => new OneRosterFileGenerator(storagePath));

    using var provider = services.BuildServiceProvider();
    var gen = provider.GetRequiredService<IOneRosterFileGenerator>();
    Assert.NotNull(gen);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter UsesConfiguredStoragePathForServices`
Expected: FAIL because constructor/signature wiring is not yet aligned.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Program.cs
var storagePath = builder.Configuration["Storage:GeneratedFilesPath"] ?? "GeneratedFiles";
builder.Services.AddSingleton<IOneRosterFileGenerator>(_ => new OneRosterFileGenerator(storagePath));
builder.Services.AddHostedService(sp =>
    new DailyFileGenerationWorker(
        sp.GetRequiredService<IOneRosterFileGenerator>(),
        sp.GetRequiredService<ILogger<DailyFileGenerationWorker>>(),
        storagePath));
builder.Services.AddSingleton(_ => storagePath);
```

```json
// appsettings.json
{
  "Storage": {
    "GeneratedFilesPath": "GeneratedFiles"
  }
}
```

```json
// appsettings.Development.json
{
  "Storage": {
    "GeneratedFilesPath": "GeneratedFiles"
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter UsesConfiguredStoragePathForServices`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Program.cs src/appsettings.json src/appsettings.Development.json src/DailyOneRosterFile.Api.Tests
git commit -m "feat: add canonical storage path configuration"
```

### Task 2: Return generated file path and enforce one-file retention in worker

**Files:**
- Modify: `src/Services/OneRosterFileGenerator.cs`
- Modify: `src/BackgroundServices/DailyFileGenerationWorker.cs`

**Interfaces:**
- Consumes: `storagePath` from Task 1.
- Produces:
  - `Task<string> IOneRosterFileGenerator.GenerateDailyFileAsync()`
  - worker retention behavior that keeps only returned file.

- [ ] **Step 1: Write the failing retention test**

```csharp
[Fact]
public async Task CleanupKeepsOnlyGeneratedZip()
{
    var dir = Directory.CreateTempSubdirectory();
    var keep = Path.Combine(dir.FullName, "new.zip");
    var old1 = Path.Combine(dir.FullName, "old1.zip");
    var old2 = Path.Combine(dir.FullName, "old2.zip");
    await File.WriteAllBytesAsync(keep, new byte[] { 1 });
    await File.WriteAllBytesAsync(old1, new byte[] { 1 });
    await File.WriteAllBytesAsync(old2, new byte[] { 1 });

    DailyFileGenerationWorker.DeleteOldZipFiles(dir.FullName, keep);

    Assert.True(File.Exists(keep));
    Assert.False(File.Exists(old1));
    Assert.False(File.Exists(old2));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter CleanupKeepsOnlyGeneratedZip`
Expected: FAIL because worker has no deterministic cleanup method.

- [ ] **Step 3: Write minimal implementation**

```csharp
// IOneRosterFileGenerator
Task<string> GenerateDailyFileAsync();

// OneRosterFileGenerator
public async Task<string> GenerateDailyFileAsync()
{
    Directory.CreateDirectory(_storagePath);
    var before = Directory.GetFiles(_storagePath, "*.zip").ToHashSet(StringComparer.OrdinalIgnoreCase);

    var generator = new OneRoster();
    generator.OutputOneRosterZipFile();

    var created = Directory.GetFiles(_storagePath, "*.zip")
        .Except(before, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .FirstOrDefault()
        ?? throw new InvalidOperationException("Generator did not create a zip file.");

    return created;
}
```

```csharp
// DailyFileGenerationWorker
public static void DeleteOldZipFiles(string storagePath, string keepFilePath)
{
    foreach (var file in Directory.GetFiles(storagePath, "*.zip"))
    {
        if (!string.Equals(file, keepFilePath, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(file);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter CleanupKeepsOnlyGeneratedZip`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Services/OneRosterFileGenerator.cs src/BackgroundServices/DailyFileGenerationWorker.cs src/DailyOneRosterFile.Api.Tests/Retention/FileRetentionTests.cs
git commit -m "feat: enforce single-zip retention in daily worker"
```

### Task 3: Align download API to canonical storage path

**Files:**
- Modify: `src/Controllers/FilesController.cs`
- Test: `src/DailyOneRosterFile.Api.Tests/Controllers/FilesControllerTests.cs`

**Interfaces:**
- Consumes: canonical `storagePath` registered in Task 1.
- Produces: controller constructor `FilesController(string storagePath)` and endpoint behavior returning latest ZIP or 404.

- [ ] **Step 1: Write the failing API tests**

```csharp
[Fact]
public void ReturnsNotFoundWhenNoZipExists()
{
    var dir = Directory.CreateTempSubdirectory();
    var controller = new FilesController(dir.FullName);

    var result = controller.DownloadLatestFile();

    Assert.IsType<NotFoundObjectResult>(result);
}

[Fact]
public async Task ReturnsLatestZipWhenFilesExist()
{
    var dir = Directory.CreateTempSubdirectory();
    var older = Path.Combine(dir.FullName, "older.zip");
    var latest = Path.Combine(dir.FullName, "latest.zip");
    await File.WriteAllBytesAsync(older, new byte[] { 1 });
    await Task.Delay(20);
    await File.WriteAllBytesAsync(latest, new byte[] { 2 });

    var controller = new FilesController(dir.FullName);
    var result = controller.DownloadLatestFile();

    var fileResult = Assert.IsType<FileContentResult>(result);
    Assert.Equal("latest.zip", fileResult.FileDownloadName);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter "ReturnsNotFoundWhenNoZipExists|ReturnsLatestZipWhenFilesExist"`
Expected: FAIL because controller currently hardcodes base directory and has ordering bug.

- [ ] **Step 3: Write minimal implementation**

```csharp
public class FilesController : ControllerBase
{
    private readonly string _storagePath;

    public FilesController(string storagePath)
    {
        _storagePath = storagePath;
    }

    [HttpGet("latest-oneroster")]
    public IActionResult DownloadLatestFile()
    {
        var files = Directory.GetFiles(_storagePath, "*.zip");
        if (files.Length == 0) return NotFound("No files generated yet.");

        var latestFile = files
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();

        var bytes = System.IO.File.ReadAllBytes(latestFile);
        return File(bytes, "application/zip", Path.GetFileName(latestFile));
    }
}
```

```csharp
// Program.cs
builder.Services.AddSingleton(sp => new FilesController(sp.GetRequiredService<string>()));
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj --filter "ReturnsNotFoundWhenNoZipExists|ReturnsLatestZipWhenFilesExist"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Controllers/FilesController.cs src/Program.cs src/DailyOneRosterFile.Api.Tests/Controllers/FilesControllerTests.cs
git commit -m "fix: align files API with canonical storage path"
```

### Task 4: Add test project, wire solution, and verify compose volume contract

**Files:**
- Create: `src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj`
- Modify: `src/DailyOneRosterFile.sln`
- Modify: `docker-compose.yml`
- Modify: `docs/plan.md`

**Interfaces:**
- Consumes: all behavior from Tasks 1-3.
- Produces: runnable test suite and documented source-of-truth progress for this feature.

- [ ] **Step 1: Write failing compose contract check (scripted assertion)**

```powershell
$compose = docker compose config
if ($compose -notmatch "/app/GeneratedFiles") { throw "Missing volume mount path" }
```

- [ ] **Step 2: Run check to verify it fails if contract missing**

Run: `powershell -NoProfile -Command "$compose = docker compose config; if ($compose -notmatch '/app/GeneratedFiles') { throw 'Missing volume mount path' }"`
Expected: PASS only when mount exists; fail if removed.

- [ ] **Step 3: Write minimal implementation**

```xml
<!-- src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\DailyOneRosterFile.Api.csproj" />
  </ItemGroup>
</Project>
```

```yaml
# docker-compose.yml
services:
  app:
    volumes:
      - generated_files:/app/GeneratedFiles
volumes:
  generated_files:
```

```markdown
<!-- docs/plan.md -->
- [x] Implement canonical volume-backed storage + one-file retention in worker.
```

- [ ] **Step 4: Run full verification**

Run: `dotnet test src/DailyOneRosterFile.Api.Tests/DailyOneRosterFile.Api.Tests.csproj`
Expected: PASS for retention + controller tests.

Run: `docker compose config`
Expected: output contains `/app/GeneratedFiles` volume mount.

- [ ] **Step 5: Commit**

```bash
git add src/DailyOneRosterFile.sln src/DailyOneRosterFile.Api.Tests docker-compose.yml docs/plan.md
git commit -m "test: add retention tests and verify volume contract"
```
