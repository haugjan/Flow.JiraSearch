using System.Net.Http;
using System.Text;
using Flow.JiraSearch.JiraClient;
using Flow.JiraSearch.Search;
using Flow.JiraSearch.Settings;
using Flow.Launcher.Plugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.JiraSearch;

public static class ServiceProvider
{
    public static void ConfigureServices(
        this ServiceCollection serviceCollection,
        PluginInitContext context,
        PluginSettings settings
    )
    {
        serviceCollection.AddSingleton(context);
        serviceCollection.AddSingleton(settings);
        serviceCollection.AddSingleton<Func<HttpClient>>(_ =>
        {
            return () =>
            {
                var config = settings;
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"{config.BaseUrl}/"),
                    Timeout = TimeSpan.FromSeconds(Math.Clamp(config.Timeout.TotalSeconds, 3, 30)),
                };
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(config.ApiToken));
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                return httpClient;
            };
        });
        serviceCollection.AddScoped<IIssueSearchClient, IssueSearchClient>();
        serviceCollection.AddScoped<IUserSearchClient, UserSearchClient>();
        serviceCollection.AddScoped<IIssueQueryBuilder, IssueQueryBuilder>();
        serviceCollection.AddScoped<IResultCreator, ResultCreator>();
        serviceCollection.AddScoped<IConfigurator, Configurator>();
        serviceCollection.AddScoped<ISearcher, Searcher>();
        serviceCollection.AddScoped<SettingsViewModel>();
    }
}
