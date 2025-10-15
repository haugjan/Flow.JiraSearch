using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Flow.Launcher.Plugin;

namespace Flow.JiraSearch.Settings;

public sealed class SettingsViewModel(PluginSettings settings)
{
    public PluginSettings Settings { get; } = settings;

    public string DefaultProjects
    {
        get => string.Join(",", Settings.DefaultProjects);
        set => Settings.DefaultProjects = value.Split(",").ToList();
    }
}
