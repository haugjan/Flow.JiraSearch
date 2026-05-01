# Architecture

Flow.JiraSearch is a [Flow Launcher](https://flowlauncher.com/) plugin
written in C# / .NET 9 (WPF). It searches Jira Cloud through the REST
API and is invoked with the `jira` action keyword.

## Solution layout

```
Flow.JiraSearch.sln
├── Flow.JiraSearch/                 Plugin assembly
│   ├── Main.cs                      Entry point (IAsyncPlugin, ISettingProvider)
│   ├── ServiceProvider.cs           DI container wiring
│   ├── plugin.json                  Flow Launcher manifest
│   ├── JiraClient/                  HTTP + JQL builder
│   │   ├── IssueSearchClient.cs     POST rest/api/2/search/jql
│   │   ├── UserSearchClient.cs      GET rest/api/2/user/search (name → accountId)
│   │   ├── IssueQueryBuilder.cs     Tokens → JQL via the fluent DSL
│   │   ├── QueryBuilderExtensions.cs The DSL itself (When/Then/Aggregate/…)
│   │   └── IssueResponse.cs         Response DTOs
│   ├── Search/
│   │   ├── Searcher.cs              Orchestrates query → JQL → results
│   │   ├── ResultCreator.cs         Builds Flow Launcher Result items
│   │   └── IssueContext.cs
│   ├── Settings/                    WPF settings panel
│   │   ├── SettingsView.xaml(.cs)
│   │   ├── SettingsViewModel.cs
│   │   ├── PluginSettings.cs
│   │   └── Configurator.cs
│   ├── Build-Plugin.ps1             Packages plugin into a ZIP
│   └── Start.ps1                    Local hot-swap dev script
└── Flow.JiraSearch.Test/            xUnit v3 + Shouldly + NSubstitute
```

## Request lifecycle

1. Flow Launcher invokes `Main.QueryAsync(query, ct)`.
2. `Searcher.QueryAsync`
   - if the query is empty, returns the static "hint" results
   - otherwise calls `IssueQueryBuilder.BuildTextJql` to turn the user input
     into JQL (this may call out to `UserSearchClient` to resolve
     `@username` → account IDs)
   - then debounces 300 ms (`Task.Delay`, cancelled on further typing) and
     issues the search under a linked CTS combining the caller's token with
     a per-call timeout (`PluginSettings.Timeout`, clamped to ≥ 3 s)
3. `IssueSearchClient.SearchJqlAsync` POSTs `{ jql, maxResults, fields }`
   to `/rest/api/2/search/jql` and deserializes `IssueResponse`.
4. `ResultCreator.CreateResult` wraps each issue as a Flow Launcher
   `Result`, picking a status-category badge (`Images/done|progress|open.png`)
   and the assignee avatar (`Fields.Assignee.AvatarUrls.Size48`,
   falling back to `Images/icon.png`). Activating a result opens its URL
   via `Process.Start(... UseShellExecute=true)`.
5. A trailing "open in browser" / "More results in browser" entry is always
   appended.

## Dependency injection

`ServiceProvider.ConfigureServices` (called from `Main.InitAsync`) registers:

| Lifetime  | Registration |
|-----------|--------------|
| Singleton | `PluginInitContext`, `PluginSettings` |
| Singleton | `Func<HttpClient>` factory (see below) |
| Scoped    | `IIssueSearchClient`, `IUserSearchClient`, `IIssueQueryBuilder`, `IResultCreator`, `IConfigurator`, `ISearcher`, `SettingsViewModel` |

### Why a `Func<HttpClient>`?

`BaseUrl`, `ApiToken`, and `Timeout` come from settings the user can edit at
runtime. A captured singleton `HttpClient` would freeze stale values, so the
factory creates a fresh client per call, configured with the current
settings. Each created client is `using`-disposed after the request.

Authentication uses HTTP Basic with the API token base64-encoded in the
`Authorization` header.

## Visibility & testability

Most types in the plugin assembly are `internal`. The csproj exposes them to
the test assembly and to NSubstitute via `InternalsVisibleTo`:

- `Flow.JiraSearch.Test`
- `Castle.Core`
- `DynamicProxyGenAssembly2`

This lets tests substitute `internal` interfaces (e.g. `IUserSearchClient`,
which `IssueQueryBuilderTest` mocks to make username resolution
deterministic) without making them public.

## Settings

`PluginSettings` is loaded via Flow Launcher's `LoadSettingJsonStorage<T>()`
and contains:

| Field             | Default                     |
|-------------------|-----------------------------|
| `BaseUrl`         | `"https://www.example.com"` |
| `ApiToken`        | `""`                        |
| `Timeout`         | `00:00:10`                  |
| `MaxResults`      | `10`                        |
| `DefaultProjects` | `[]`                        |

The settings UI (`SettingsView.xaml`) binds directly to `Settings.*`. The
API token uses a `PasswordBox` whose `PasswordChanged` handler writes to
`Settings.ApiToken` in the code-behind (passwords cannot be two-way-bound
in WPF without unsafe workarounds). `DefaultProjects` is exposed by
`SettingsViewModel` as a comma-joined string for editing.
