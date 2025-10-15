using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Flow.JiraSearch.JiraClient;

internal interface IUserSearchClient
{
    Task<IReadOnlyList<string>> FindUserIdsByExactNameAsync(
        string name,
        int maxResults,
        CancellationToken ct = default
    );
}

internal sealed class UserSearchClient(Func<HttpClient> httpFactory) : IUserSearchClient
{
    public async Task<IReadOnlyList<string>> FindUserIdsByExactNameAsync(
        string name,
        int maxResults,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        var query = Uri.EscapeDataString(name.Trim());
        var endpoint = $"/rest/api/2/user/search?query={query}&maxResults={maxResults}";

        using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var http = httpFactory();
        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException(await resp.Content.ReadAsStringAsync(ct));

        var candidates =
            await resp
                .Content.ReadFromJsonAsync<List<JiraUserDto>>(cancellationToken: ct)
                .ConfigureAwait(false) ?? new List<JiraUserDto>();

        var wanted = name.Trim();
        var result = candidates
            .Where(u =>
            {
                if (string.IsNullOrWhiteSpace(u.DisplayName))
                    return false;

                var tokens = TokenizeName(u.DisplayName);
                return tokens.Any(t => string.Equals(t, wanted, StringComparison.OrdinalIgnoreCase))
                    || string.Equals(
                        u.DisplayName.Trim(),
                        wanted,
                        StringComparison.OrdinalIgnoreCase
                    );
            })
            .Where(u => !string.IsNullOrWhiteSpace(u.AccountId))
            .Select(u => u.AccountId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return result;
    }

    private static IReadOnlyList<string> TokenizeName(string displayName)
    {
        var tokens = Regex.Split(
            displayName,
            @"[\s\-\._,;:/\\]+",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
        );
        return tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
    }

    private sealed class JiraUserDto
    {
        [JsonPropertyName("accountId")]
        public string? AccountId { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }
}
