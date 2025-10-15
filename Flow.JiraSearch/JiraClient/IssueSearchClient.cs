using System.Net.Http;
using System.Net.Http.Json;

namespace Flow.JiraSearch.JiraClient;

internal interface IIssueSearchClient
{
    Task<IssueResponse?> SearchJqlAsync(string jql, int maxResults, CancellationToken ct);
}

internal sealed class IssueSearchClient(Func<HttpClient> httpFactory) : IIssueSearchClient
{
    public async Task<IssueResponse?> SearchJqlAsync(
        string jql,
        int maxResults,
        CancellationToken ct
    )
    {
        var body = new
        {
            jql,
            maxResults,
            fields = new[] { "summary", "status", "priority", "issuetype", "assignee", "project" },
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "rest/api/2/search/jql");
        req.Content = JsonContent.Create(body);
        using var http = httpFactory();
        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }
        return await resp
            .Content.ReadFromJsonAsync<IssueResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
    }
}
