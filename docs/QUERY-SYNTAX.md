# Query syntax

The `jira` action keyword takes a free-form query made of space-separated
tokens. `IssueQueryBuilder.BuildTextJql` parses the tokens (in any order)
and produces a JQL query that is POSTed to `/rest/api/2/search/jql`.

## Tokens

| Token                  | Effect on JQL                                               |
|------------------------|-------------------------------------------------------------|
| `#all`                 | Disables the default-projects filter.                       |
| `#KEY`                 | Restricts to project `KEY`. Repeatable (`#A #B` → `project IN (A, B)`). |
| `!`                    | `statusCategory = Done` (closed/completed only).            |
| `?`                    | `statusCategory = "In Progress"`.                           |
| `*`                    | Drops the default `statusCategory != Done` filter (show any status). |
| `@?`                   | `assignee IS EMPTY` (unassigned).                           |
| `@me`                  | `assignee IN (currentUser())`.                              |
| `@name`                | `assignee IN (<accountIds>)`. Names are resolved via `UserSearchClient` → exact-name match against `displayName` tokens. Combined with `@me` via the same `IN (...)` clause. |
| `@reporter:me`         | `reporter IN (currentUser())`.                              |
| `@reporter:name`       | `reporter IN (<accountIds>)`.                               |
| `@was:me`              | `assignee WAS (currentUser())` — issues previously assigned to you. |
| `@was:name`            | `assignee WAS (<accountIds>)`.                              |
| `+label`               | `labels IN (label)`. Repeatable.                            |
| `KEY-123`              | `issuekey IN (KEY-123)` (uppercase project key, dash, number). Repeatable. |
| anything else          | Free-text → `(summary ~ "<terms>" OR text ~ "<terms>")`.    |

The default behaviour, with no tokens at all, is:
`project IN (<DefaultProjects>) AND statusCategory != Done`.

If the user types `#all`, the project filter is dropped. If they type `*`,
the status filter is dropped.

The fragments are joined with ` AND `. There is no explicit
`order by` clause — Jira's default ordering applies.

## Examples

```
jira authentication login
→ project IN (DEFAULTS) AND statusCategory != Done
   AND (summary ~ "authentication login" OR text ~ "authentication login")

jira @me #SUP +critical
→ project IN (SUP) AND statusCategory != Done
   AND assignee IN (currentUser())
   AND labels IN (critical)

jira @reporter:john ! +bugfix
→ project IN (DEFAULTS) AND statusCategory = Done
   AND reporter IN (<accountIds for "john">)
   AND labels IN (bugfix)

jira @was:me * authentication
→ project IN (DEFAULTS)
   AND assignee WAS (currentUser())
   AND (summary ~ "authentication" OR text ~ "authentication")

jira WEB-456
→ project IN (DEFAULTS) AND statusCategory != Done
   AND issuekey IN (WEB-456)
```

The full list of expected JQL outputs is encoded as `[InlineData]` rows on
`Flow.JiraSearch.Test/IssueQueryBuilderTest.cs`. Treat that file as the
canonical specification.

## Username resolution

For `@name`, `@reporter:name`, and `@was:name`, the builder calls
`UserSearchClient.FindUserIdsByExactNameAsync(name, 5, ct)`:

1. `GET /rest/api/2/user/search?query=<name>&maxResults=5`
2. From the candidate list, keep only users whose `displayName` (whole
   string or whitespace/`-_./\,;:`-separated token) **case-insensitively
   equals** the input.
3. Return the matching `accountId`s, deduplicated.

If multiple account IDs match, all of them go into the `IN (...)` clause —
the search treats them as alternatives. If none match, the operator
contributes no IDs and the resulting clause becomes `assignee IN ()` (which
Jira will return as 0 results), so misspelt names produce empty queries
rather than wrong ones.

## Internals

The builder is implemented as a small fluent pipeline in
`QueryBuilderExtensions.cs`. Each `When(regex)` step matches whole tokens,
optionally captures groups, and feeds them into `Then…` actions:

- `Then(part)` — append a literal JQL fragment.
- `ThenRemember(value)` — push a literal value into a memory buffer.
- `ThenRemember()` — push the captured group(s) into the buffer.
- `ThenRemember(convert)` — async-transform captures before pushing
  (used for username → account-ID resolution).
- `ThenDoNothing()` — match-and-consume without side effects (used for `*`,
  `#all`).
- `Aggregate(fn)` — fold the buffer into one JQL fragment and clear it.
- `Else(part)` — fallback fragment if no match in the preceding `When`
  group (used for the project-default and status-default clauses).
- `BuildJql()` — join all collected parts with ` AND `.

Tokens are matched whole (the regex is wrapped in `^…$`), so `@johnsmith`
is one token and won't accidentally match `@john`. Free-text tokens are
collected by the catch-all `When(".*")` at the end of the pipeline.
