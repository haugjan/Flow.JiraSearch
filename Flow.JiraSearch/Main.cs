﻿using System.Windows.Controls;
using Flow.JiraSearch.Search;
using Flow.JiraSearch.Settings;
using Flow.Launcher.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.JiraSearch;

public class Main : IAsyncPlugin, ISettingProvider
{
    private ISearcher _search = null!;
    private IConfigurator _configuration = null!;

    public Task InitAsync(PluginInitContext context)
    {
        var serviceCollection = new ServiceCollection();
        var config = context.API.LoadSettingJsonStorage<PluginSettings>();
        context.API.LogInfo(nameof(Main), config.BaseUrl);
        serviceCollection.ConfigureServices(context, config);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _search = serviceProvider.GetRequiredService<ISearcher>();
        _configuration = serviceProvider.GetRequiredService<IConfigurator>();
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token) =>
        _search.QueryAsync(query, token);

    public Control CreateSettingPanel()
    {
        return _configuration.CreateSettingPanel();
    }
}
