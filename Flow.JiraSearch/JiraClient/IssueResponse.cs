using System.Text.Json.Serialization;

namespace Flow.JiraSearch.JiraClient;

public sealed class IssueResponse
{
    [JsonPropertyName("issues")]
    public List<JiraIssue> Issues { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }
}

public sealed class JiraIssue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("fields")]
    public JiraFields Fields { get; set; } = new();

    public string BrowseUrl(string baseUrl) => $"{baseUrl}/browse/{Key}";
}

public sealed class JiraIssueType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }
}

public sealed class JiraPriority
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }
}

public sealed class JiraStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    // In v2 ist das Icon direkt am Statusobjekt vorhanden:
    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    // Optional, falls du Farbcodes/Kategorie brauchst:
    [JsonPropertyName("statusCategory")]
    public JiraStatusCategory? StatusCategory { get; set; }
}

public sealed class JiraStatusCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("colorName")]
    public string ColorName { get; set; } = "";
}

public sealed class JiraAvatarUrls
{
    // Property-Namen dürfen nicht mit Ziffer beginnen – über JsonPropertyName mappen:
    [JsonPropertyName("16x16")]
    public string? Size16 { get; set; }

    [JsonPropertyName("24x24")]
    public string? Size24 { get; set; }

    [JsonPropertyName("32x32")]
    public string? Size32 { get; set; }

    [JsonPropertyName("48x48")]
    public string? Size48 { get; set; }
}

public sealed class JiraUser
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("avatarUrls")]
    public JiraAvatarUrls? AvatarUrls { get; set; }
}

public sealed class JiraProject
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("avatarUrls")]
    public JiraAvatarUrls? AvatarUrls { get; set; }
}

public sealed class JiraFields
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    [JsonPropertyName("status")]
    public JiraStatus Status { get; set; } = new();

    [JsonPropertyName("priority")]
    public JiraPriority? Priority { get; set; }

    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    [JsonPropertyName("project")]
    public JiraProject? Project { get; set; }

    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }
}
