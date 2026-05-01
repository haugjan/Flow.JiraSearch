# Flow.JiraSearch

Flow Launcher plugin (C# / .NET 9, WPF) that searches Jira Cloud via the
REST API and opens issues in the browser. Triggered with the `jira` action
keyword.

For deeper docs see [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md),
[`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md), and
[`docs/QUERY-SYNTAX.md`](docs/QUERY-SYNTAX.md).

## Repository layout

- `Flow.JiraSearch/` — plugin assembly
  - `Main.cs` — entry point, implements `IAsyncPlugin`, `ISettingProvider`
  - `ServiceProvider.cs` — DI registration (`Microsoft.Extensions.DependencyInjection`)
  - `JiraClient/`
    - `IssueSearchClient.cs` — POSTs JQL to `/rest/api/2/search/jql`
    - `UserSearchClient.cs` — `/rest/api/2/user/search`, resolves names to
      account IDs by exact match against `displayName` tokens
    - `IssueQueryBuilder.cs` — parses the user query into JQL using the
      fluent pipeline in `QueryBuilderExtensions.cs`
    - `IssueResponse.cs` — DTOs for the API response
  - `Search/`
    - `Searcher.cs` — orchestrates query → JQL → API call → `Result` list,
      with a 300 ms debounce and per-call timeout
    - `ResultCreator.cs` — builds `Flow.Launcher.Plugin.Result` items, picks
      a status-category badge icon, opens URLs via
      `Process.Start(... UseShellExecute=true)`
    - `IssueContext.cs` — data carried on each result
  - `Settings/` — WPF settings panel (`SettingsView.xaml(.cs)`,
    `SettingsViewModel.cs`, `PluginSettings.cs`, `Configurator.cs`)
  - `plugin.json` — Flow Launcher manifest (action keyword `jira`, ID,
    version, icon)
  - `Build-Plugin.ps1` — packages the plugin into a ZIP for manual install
  - `Start.ps1` — local dev helper: stops Flow Launcher, builds, copies the
    output into `%APPDATA%\FlowLauncher\Plugins\Jira Search-1.1.0`,
    restarts Flow Launcher
- `Flow.JiraSearch.Test/` — xUnit v3 + Shouldly + NSubstitute tests
- `.github/workflows/`
  - `build-action.yml` — PR build (`dotnet publish` win-x64, uploads
    artifact `JiraSearch-<version>`)
  - `publish-action.yml` — release on push to `main`, tags `v<plugin.json
    Version>`, attaches the published ZIP

## Build & test

```powershell
dotnet restore
dotnet build Flow.JiraSearch.sln -c Release
dotnet test  Flow.JiraSearch.sln
```

For interactive plugin development, run `Flow.JiraSearch\Start.ps1` from
the project directory — it stops Flow Launcher, rebuilds, copies the DLLs
into the plugin folder and relaunches.

For producing an installable ZIP, run `Flow.JiraSearch\Build-Plugin.ps1`.

The publish workflow tags releases from the `Version` field in
`Flow.JiraSearch/plugin.json` — bumping that field on `main` is what
triggers a new GitHub release.

## Query language (handled by `IssueQueryBuilder`)

The user types a query after `jira`. Tokens are space-separated and
order-independent:

| Token              | Effect                                                                |
|--------------------|-----------------------------------------------------------------------|
| `#all`             | Drop the default-projects filter                                      |
| `#KEY`             | Restrict to project `KEY` (repeatable)                                |
| `!`                | `statusCategory = Done` (closed only)                                 |
| `?`                | `statusCategory = "In Progress"`                                      |
| `*`                | Drop the default `statusCategory != Done` filter                      |
| `@?`               | `assignee IS EMPTY`                                                   |
| `@me`              | `assignee = currentUser()`                                            |
| `@name`            | `assignee IN (<accountIds>)` — names resolved via `UserSearchClient`  |
| `@reporter:me`     | `reporter = currentUser()`                                            |
| `@reporter:name`   | `reporter IN (<accountIds>)`                                          |
| `@was:me`          | `assignee WAS currentUser()`                                          |
| `@was:name`        | `assignee WAS (<accountIds>)`                                         |
| `+label`           | `labels IN (...)` (repeatable)                                        |
| `KEY-123`          | `issuekey IN (...)` (repeatable, uppercase key + dash + number)       |
| anything else      | Free text → `(summary ~ "tok" OR text ~ "tok")`                       |

If no `#KEY` is given, `PluginSettings.DefaultProjects` is applied. The
default status filter (`statusCategory != Done`) is applied unless `*`,
`!`, or `?` overrides it.

`IssueQueryBuilderTest.cs` is the canonical reference for expected JQL
strings — when changing the builder, update or add an `[InlineData]` row
there.

## Architecture notes

- DI is wired in `ServiceProvider.ConfigureServices`. `PluginInitContext` and
  `PluginSettings` are singletons; everything else is scoped.
- `HttpClient` is created per call via a `Func<HttpClient>` factory because
  `BaseUrl`, `ApiToken`, and `Timeout` come from settings that the user can
  edit at runtime — capturing a single `HttpClient` would freeze stale
  values.
- Auth is HTTP Basic with the API token base64-encoded in
  `Authorization: Basic`. For Atlassian Cloud the username portion is the
  account email; the plugin currently sends only the token. Verify against
  the user's tenant if 401s appear.
- `Searcher.QueryAsync` builds the JQL first (which may call
  `UserSearchClient` for `@name` resolution) and then `Task.Delay(300, ct)`
  debounces the actual issue search. The search runs under a linked CTS
  combining Flow Launcher's cancellation token with a per-call timeout
  (clamped to ≥ 3 s).
- `internalsVisibleTo` is set for the test assembly and for
  Castle/DynamicProxy so NSubstitute can mock `internal` interfaces (the
  query-builder tests substitute `IUserSearchClient` to make username
  resolution deterministic).

## Conventions

- Target framework: `net9.0-windows`, nullable enabled, WPF (`UseWpf`).
- Public API surface is intentionally small — most types are `internal` and
  exposed to the test project via `InternalsVisibleTo`.
- Tests use **xUnit v3** (`xunit.v3` package, `TestContext.Current.CancellationToken`),
  Shouldly for assertions, NSubstitute for mocks. Don't introduce other
  frameworks.
- Code style follows the default .NET conventions; primary constructors are
  used throughout for DI (e.g. `Searcher(...)`, `IssueQueryBuilder(...)`).
- The plugin GUID in `plugin.json` (`BD32A62C-…`) matches the one in
  `Build-Plugin.ps1` — keep them in sync if either is changed.

## Branch naming

The global `feature/firefly/OPA-…` / `hotfix/firefly/SUP-…` rules in
`~/.claude/CLAUDE.md` are scoped to firefly work and do **not** apply to
this personal repo. Use short `feature/<topic>` or `fix/<topic>` branch
names. Commit subjects are plain imperative sentences with no ticket prefix.

## Known wrinkles

- `Build-Plugin.ps1` hardcodes the ZIP version (`v1.1.0`) while
  `plugin.json:Version` has moved on. Locally built ZIPs are misnamed.
- `Start.ps1` hardcodes the plugin folder name (`Jira Search-1.1.0`) and
  no longer matches the folder Flow Launcher creates after installing a
  newer version — local debug builds land in the wrong directory unless
  `-PluginFolderName` is passed explicitly.
