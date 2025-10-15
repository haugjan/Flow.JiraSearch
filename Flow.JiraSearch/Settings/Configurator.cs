using System.Windows.Controls;

namespace Flow.JiraSearch.Settings;

internal interface IConfigurator
{
    Control CreateSettingPanel();
}

public class Configurator(SettingsViewModel viewModel) : IConfigurator
{
    public Control CreateSettingPanel() => new SettingsView { DataContext = viewModel };
}
