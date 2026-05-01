using System.Diagnostics;
using System.Net;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flow.JiraSearch.Settings;
using Flow.Launcher.Plugin;

namespace Flow.JiraSearch.Search;

internal interface IResultCreator
{
    Result CreateResult(
        string title,
        string subtitle,
        string? icon,
        string badgeIcon,
        string key,
        string url,
        string jql,
        string copyText
    );

    Result CreateHint(string title, string sub);
    Result CreateOpenInBrowserAction(string title, string jql);
    Result CreateAuthError();
    Result CreateApiError(HttpStatusCode? status, string message);
}

internal class ResultCreator(PluginSettings settings) : IResultCreator
{
    public Result CreateResult(
        string title,
        string subtitle,
        string? icon,
        string badgeIcon,
        string key,
        string url,
        string jql,
        string copyText
    ) =>
        new()
        {
            Title = title,
            SubTitle = subtitle,
            BadgeIcoPath = badgeIcon,
            IcoPath = icon,
            ShowBadge = true,
            Action = _ => Open(url),
            ContextData = new IssueContext(key, url, jql),
            CopyText = copyText,
        };

    public Result CreateHint(string title, string sub) =>
        new()
        {
            Title = title,
            SubTitle = sub,
            IcoPath = "Images/gray.png",
            Action = _ => false,
        };

    public Result CreateOpenInBrowserAction(string title, string jql) =>
        new()
        {
            Title = title,
            SubTitle = $"JQL: {jql}",
            IcoPath = "Images/icon.png",
            Action = _ =>
            {
                var url = $"{settings.BaseUrl}/issues/?jql={Uri.EscapeDataString(jql)}";
                return Open(url);
            },
        };

    public Result CreateAuthError() =>
        new()
        {
            Title = "Jira authentication failed (HTTP 401)",
            SubTitle =
                "Did you paste the API Token as 'your-email@example.com:apitoken'? "
                + "Click for setup help.",
            IcoPath = "Images/gray.png",
            Action = _ => Open("https://github.com/haugjan/Flow.JiraSearch#configuration"),
        };

    public Result CreateApiError(HttpStatusCode? status, string message) =>
        new()
        {
            Title = status.HasValue
                ? $"Jira request failed (HTTP {(int)status.Value} {status.Value})"
                : "Jira request failed",
            SubTitle = message,
            IcoPath = "Images/gray.png",
            Action = _ => false,
        };

    private bool Open(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return true;
    }
}
