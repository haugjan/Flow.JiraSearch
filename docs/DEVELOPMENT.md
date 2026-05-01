# Development

## Prerequisites

- Windows 10/11
- .NET SDK 9.0
- Flow Launcher (only required for end-to-end testing)

## Build & test

```powershell
dotnet restore
dotnet build Flow.JiraSearch.sln -c Release
dotnet test  Flow.JiraSearch.sln
```

The solution targets `net9.0-windows`, has nullable reference types enabled,
and uses WPF (`UseWpf=true`).

Running `dotnet run` against the plugin project is **not** meaningful —
there is no `Main()`. The plugin only loads inside Flow Launcher.

## Test stack

Tests live in `Flow.JiraSearch.Test/`. The stack:

- **xUnit v3** (`xunit.v3`) — note that v3 uses
  `TestContext.Current.CancellationToken` inside `[Theory]`/`[Fact]` methods
  rather than injecting a `CancellationToken` parameter.
- **Shouldly** for assertions (`x.ShouldBe(...)`, `Should.ThrowAsync<...>`).
- **NSubstitute** for mocks. Internal interfaces are mockable thanks to the
  `InternalsVisibleTo` attributes on the production project.

When changing `IssueQueryBuilder`, add or update `[InlineData]` rows on
`TestBuildQuery` in `IssueQueryBuilderTest.cs`. Those rows are the spec for
the JQL output; the user-facing operator table in
[`docs/QUERY-SYNTAX.md`](QUERY-SYNTAX.md) and the README's "Search Operators
Reference" should be updated in the same change.

## Local hot-swap loop

For interactive testing inside Flow Launcher, run from the project directory:

```powershell
.\Flow.JiraSearch\Start.ps1
```

The script:

1. Stops the `Flow.Launcher` process.
2. Runs `dotnet build` (Debug by default; pass `-BuildConfig Release` to
   switch).
3. Copies `bin\Debug\net9.0-windows\*` into
   `%APPDATA%\FlowLauncher\Plugins\Jira Search-1.1.0`.
4. Restarts `%LOCALAPPDATA%\FlowLauncher\Flow.Launcher.exe`.

> The default plugin folder name embeds the version (`1.1.0`) and is now
> stale — the manifest is at `1.2.0`. After Flow Launcher first installs
> `1.2.0`, it creates a new folder and `Start.ps1` keeps writing to the
> old one. Either pass `-PluginFolderName "Jira Search-1.2.0"` or update
> the script default; see the heads-up below.

## Producing a release ZIP

```powershell
.\Flow.JiraSearch\Build-Plugin.ps1
```

This builds in Release, copies the required DLLs and assets into
`dist\temp\Flow.JiraSearch\`, zips them, and prints the resulting path. The
ZIP can be installed via Flow Launcher → Settings → Plugins → Install Plugin.

## CI / release

- `.github/workflows/build-action.yml` — builds every PR via
  `dotnet publish ... -r win-x64 --no-self-contained` and uploads the result
  as a 14-day artifact named `JiraSearch-<version>`.
- `.github/workflows/publish-action.yml` — on push to `main`, reads
  `Version` from `Flow.JiraSearch/plugin.json`, compares it against the
  latest GitHub release tag (stripping `v`), and if they differ, publishes
  a Release named `v<Version>` with the `JiraSearch-<Version>.zip` attached.

## Versioning

The single source of truth for the plugin version is the `Version` field in
`Flow.JiraSearch/plugin.json`. Bump it before merging a release-worthy
change. The publish workflow tags `v<Version>` and uploads the ZIP under
that name.

## Known wrinkles in the build tooling

These don't affect the plugin at runtime, just the build/dev/release
tooling. Worth fixing the next time the relevant script is touched.

1. **`Build-Plugin.ps1` hardcodes the ZIP version.** The line
   `$ZipFileName = "$PluginName-v1.1.0.zip"` is out of sync with
   `plugin.json:Version`. Fix:
   ```powershell
   $pluginVersion = (Get-Content plugin.json -Raw | ConvertFrom-Json).Version
   $ZipFileName   = "$PluginName-v$pluginVersion.zip"
   ```
2. **`Start.ps1` hardcodes the plugin folder name.** Same root cause —
   `Jira Search-1.1.0` no longer matches the version Flow Launcher creates
   on a fresh install of newer versions. Either derive `Name + Version`
   from `plugin.json` or pass `-PluginFolderName` at the call site each
   time the version changes.

## Code conventions

- Primary constructors are used throughout for DI (e.g.
  `Searcher(IIssueSearchClient ..., …) : ISearcher`).
- Most types are `internal` and exposed to tests via `InternalsVisibleTo`;
  prefer `internal` for new types unless they're genuinely part of the
  public surface.
- DTOs use `System.Text.Json` with `[JsonPropertyName(...)]`. Don't pull in
  Newtonsoft.Json.
