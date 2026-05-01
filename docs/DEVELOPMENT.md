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
   `%APPDATA%\FlowLauncher\Plugins\<Name>-<Version>`. The `<Name>` and
   `<Version>` come from `plugin.json` automatically; pass
   `-PluginFolderName` to override.
4. Restarts `%LOCALAPPDATA%\FlowLauncher\Flow.Launcher.exe`.

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
that name. `Build-Plugin.ps1` and `Start.ps1` read `plugin.json` at runtime
for the version and id, so a single bump propagates everywhere.

## Code conventions

- Primary constructors are used throughout for DI (e.g.
  `Searcher(IIssueSearchClient ..., …) : ISearcher`).
- Most types are `internal` and exposed to tests via `InternalsVisibleTo`;
  prefer `internal` for new types unless they're genuinely part of the
  public surface.
- DTOs use `System.Text.Json` with `[JsonPropertyName(...)]`. Don't pull in
  Newtonsoft.Json.
