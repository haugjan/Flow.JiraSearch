using Flow.JiraSearch.JiraClient;
using Flow.JiraSearch.Settings;
using Flow.Launcher.Plugin;

namespace Flow.JiraSearch.Search;

internal interface ISearcher
{
    Task<List<Result>> QueryAsync(Query query, CancellationToken token);
}

internal sealed class Searcher(
    IIssueSearchClient issueSearch,
    IIssueQueryBuilder issueQueryBuilder,
    IResultCreator resultCreator,
    PluginSettings settings,
    PluginInitContext context
) : ISearcher
{
    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        context.API.LogInfo(nameof(Searcher), $"Query: {query.Search}");

        if (string.IsNullOrWhiteSpace(query.Search))
            return CreateHints();

        var jql = await issueQueryBuilder.BuildTextJql(
            query.Search,
            settings.DefaultProjects,
            token
        );
        context.API.LogInfo(nameof(Searcher), $"JQL: {jql}");

        return await SearchAsync(jql, token);
    }

    private List<Result> CreateHints()
    {
        return
        [
            resultCreator.CreateHint(
                "@me name",
                "Assigned to me (@me) or to a specific person (name)"
            ),
            resultCreator.CreateHint(
                "@reporter:me @reporter:name",
                "Reported by me (@reporter:me) or by name"
            ),
            resultCreator.CreateHint("@was:me @was:name", "Was assigned to me (@was:me) or name"),
            resultCreator.CreateHint("*", "All statuses"),
            resultCreator.CreateHint("!", "Completed issues"),
            resultCreator.CreateHint("#ABC", "Project ABC"),
            resultCreator.CreateHint("+Label1", "Issues with label 'Label1'"),
        ];
    }

    private async Task<List<Result>> SearchAsync(string jql, CancellationToken externalCt)
    {
        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(3, settings.Timeout.TotalSeconds))
        );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalCt,
            timeoutCts.Token
        );

        var data = await issueSearch
            .SearchJqlAsync(jql, settings.MaxResults + 1, linkedCts.Token)
            .ConfigureAwait(false);

        var results = new List<Result>();

        if (data == null || data.Issues.Count == 0)
        {
            results.Add(
                resultCreator.CreateOpenInBrowserAction("No results. Open search in browser", jql)
            );
            return results;
        }

        foreach (var issue in data.Issues.Take(settings.MaxResults))
        {
            var statusName = issue.Fields.Status.Name;
            var assignee = issue.Fields.Assignee?.DisplayName ?? "Unassigned";
            var title = $"{issue.Key} · {issue.Fields.Summary}";
            var subtitle = $"{statusName} · {assignee}";

            var url = issue.BrowseUrl(settings.BaseUrl);
            var badgeIconFile = MapStatusCategoryToBadge(issue.Fields.Status.StatusCategory?.Key);
            var icon = issue.Fields.Assignee?.AvatarUrls?.Size48 ?? "Images/icon.png";

            results.Add(
                resultCreator.CreateResult(
                    title: title,
                    subtitle: subtitle,
                    icon: icon,
                    badgeIcon: badgeIconFile,
                    key: issue.Key,
                    url: url,
                    jql: jql,
                    copyText: url
                )
            );
        }

        if (data.Issues.Count > settings.MaxResults)
        {
            results.Add(
                resultCreator.CreateOpenInBrowserAction("More results in browser ...", jql)
            );
        }

        return results;
    }

    private static string MapStatusCategoryToBadge(string? key) =>
        key switch
        {
            "done" => "Images/done.png",
            "indeterminate" => "Images/progress.png",
            _ => "Images/open.png",
        };
}
