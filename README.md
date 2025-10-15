# Flow.JiraSearch

Search for and open Jira issues directly from Flow Launcher.

## Features

- 🔍 **Fast Issue Search**: Search Jira issues by key, summary, or description
- 🎯 **Project Filtering**: Configure default projects for focused searches
- 🚀 **Direct Navigation**: Open issues directly in your browser
- ⚡ **Real-time Results**: Get instant search results as you type
- 🔧 **Configurable**: Customize timeout, result limits, and default projects
- 👤 **User-based Search**: Find issues by assignee, reporter, or previous assignee
- 🏷️ **Label Search**: Filter issues by labels
- 📊 **Status Control**: Search all statuses or only closed issues

## Requirements

- Flow Launcher 1.15.0 or higher
- .NET 9.0 Runtime
- Valid Jira account with API access

## Installation

### From Release
1. Download the latest release from the [Releases](../../releases) page
2. Extract the zip file to your Flow Launcher plugins directory
3. Restart Flow Launcher

### From Source
1. Clone this repository:
   ```bash
   git clone https://github.com/haugjan/Flow.JiraSearch.git
   ```
2. Build the project:
   ```bash
   cd Flow.JiraSearch
   dotnet build --configuration Release
   ```
3. Copy the built files to Flow Launcher's plugin directory

## Configuration

The plugin is configured through Flow Launcher's settings interface:

1. **Open Flow Launcher Settings**:
   - Press `Alt + Space` to open Flow Launcher
   - Type `settings` and select "Flow Launcher Settings"

2. **Navigate to Plugin Settings**:
   - Go to the "Plugins" tab
   - Find "Jira Search" in the plugin list
   - Click the settings icon next to it

3. **Configure the following settings**:

   | Setting | Description | Example |
   |---------|-------------|---------|
   | **Base URL** | Your Jira instance URL | `https://yourcompany.atlassian.net` |
   | **API Token** | Personal API token from Jira | `ATATT3xFfGF0...` |
   | **Timeout** | Request timeout in seconds | `10` |
   | **Max Results** | Maximum number of results to display | `10` |
   | **Default Projects** | Project keys to search by default | `["PROJECT1", "PROJECT2"]` |

### Creating a Jira API Token

1. Go to [Atlassian Account Settings](https://id.atlassian.com/manage-profile/security/api-tokens)
2. Click "Create API token"
3. Give it a descriptive name (e.g., "Flow Launcher Plugin")
4. Copy the generated token and paste it in the plugin settings

## Usage

### User-based Search
Open Flow Launcher (`Alt + Space`) and use the `jira` keyword with these patterns:

**Current Assignee:**
```
jira @me
```
Find issues assigned to me

```
jira @john
```
Find issues assigned to john

**Reporter Search:**
```
jira @reporter:me
```
Find issues reported by me

```
jira @reporter:john
```
Find issues reported by john

**Previous Assignee:**
```
jira @was:me
```
Find issues that were previously assigned to me

```
jira @was:john
```
Find issues that were previously assigned to john

### Project-based Search

```
jira #sup
```
Find issues from project SUP

```
jira #all
```
Search all projects (ignores default project filter from settings)

### Label Search

```
jira +label1
```
Find issues with label "label1"

```
jira +bugfix +urgent
```
Find issues with both "bugfix" and "urgent" labels

### Status Control

```
jira *
```
Show all statuses (open and closed issues)

```
jira !
```
Show only closed/completed issues

### Issue Key Search

```
jira ABC-123
```
Find specific issue by key

### Text Search

```
jira login bug
```
Search for issues containing "login" and "bug" in summary or description

### Combined Search Examples

```
jira @me #web +critical
```
Find critical issues assigned to me in the WEB project

```
jira @reporter:john ! +bugfix
```
Find closed issues reported by john with the bugfix label

```
jira @was:me * authentication
```
Find all issues (any status) that were assigned to me containing "authentication"

```
jira #sup @me !
```
Find closed issues assigned to me in the SUP project

## Default Projects

If you configure default projects in the settings, searches will automatically be limited to those projects unless you use `#all` or specify a different project with `#projectkey`.

## Search Operators Reference

| Operator | Description | Example |
|----------|-------------|---------|
| `@me` | Issues assigned to current user | `jira @me` |
| `@username` | Issues assigned to specific user | `jira @john` |
| `@reporter:me` | Issues reported by current user | `jira @reporter:me` |
| `@reporter:username` | Issues reported by specific user | `jira @reporter:alice` |
| `@was:me` | Issues previously assigned to current user | `jira @was:me` |
| `@was:username` | Issues previously assigned to specific user | `jira @was:bob` |
| `#projectkey` | Issues from specific project | `jira #web` |
| `#all` | Search all projects | `jira #all` |
| `+labelname` | Issues with specific label | `jira +urgent` |
| `*` | All statuses (default: open only) | `jira * bug` |
| `!` | Only closed/completed issues | `jira ! security` |
| `ABC-123` | Search by issue key | `jira PROJECT-456` |

## Troubleshooting

### No Results Appearing
- Verify your Jira URL is correct and accessible
- Check that your API token is valid and hasn't expired
- Ensure you have permission to view the projects you're searching

### Timeout Issues
- Increase the timeout setting in plugin configuration
- Check your internet connection to the Jira instance
- Verify the Jira instance is responsive

### Authentication Errors
- Regenerate your API token in Atlassian Account Settings
- Ensure you're using your email address as the username (for Atlassian Cloud)
- Check that your account has appropriate permissions

### User Search Not Working
- Make sure usernames are spelled correctly
- User search is case-sensitive
- Some users might not be searchable depending on Jira permissions

## Development

### Building from Source
```bash
git clone https://github.com/haugjan/Flow.JiraSearch.git
cd Flow.JiraSearch
dotnet restore
dotnet build --configuration Release
```

### Running Tests
```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: Report bugs or request features on [GitHub Issues](../../issues)
- **Discussions**: Ask questions in [GitHub Discussions](../../discussions)
- **Documentation**: Check the [Wiki](../../wiki) for additional help

## Acknowledgments

- Built for [Flow Launcher](https://flowlauncher.com/)
- Uses the Atlassian Jira REST API
- Icons from the Jira design system
