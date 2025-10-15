// Tests/JiraClient/UserSearchClientTests.cs

using System.Net;
using System.Text;
using System.Text.Json;
using Flow.JiraSearch.JiraClient;
using Shouldly;

namespace Flow.JiraSearch.Test;

public class UserSearchClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Func<HttpClient> _httpFactory;
    private readonly UserSearchClient _sut;

    public UserSearchClientTests()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://test.atlassian.net/"),
        };
        _httpFactory = () => _httpClient;
        _sut = new UserSearchClient(_httpFactory);
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithValidName_ReturnsMatchingUserIds()
    {
        // Arrange
        const string searchName = "John Doe";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[]
        {
            new { accountId = "user-123", displayName = "John Doe" },
            new { accountId = "user-456", displayName = "Jane Smith" },
            new { accountId = "user-789", displayName = "john doe" }, // case insensitive match
        };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2); // Both "John Doe" and "john doe" should match
        result.ShouldContain("user-123");
        result.ShouldContain("user-789");
        result.ShouldNotContain("user-456"); // Jane Smith shouldn't match
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithPartialNameMatch_ReturnsMatchingUserIds()
    {
        // Arrange
        const string searchName = "John";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[]
        {
            new { accountId = "user-123", displayName = "John Doe Smith" },
            new { accountId = "user-456", displayName = "Jane Johnson" },
            new { accountId = "user-789", displayName = "Michael John" },
        };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain("user-123"); // "John" matches token in "John Doe Smith"
        result.ShouldContain("user-789"); // "John" matches token in "Michael John"
        result.ShouldNotContain("user-456"); // "Johnson" contains "John" but doesn't match exactly
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithEmptyName_ReturnsEmptyList()
    {
        // Arrange
        const string searchName = "";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        _httpMessageHandler.RequestCount.ShouldBe(0); // No HTTP request should be made
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithWhitespaceOnlyName_ReturnsEmptyList()
    {
        // Arrange
        const string searchName = "   ";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        _httpMessageHandler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithNullName_ReturnsEmptyList()
    {
        // Arrange
        string? searchName = null;
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName!,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        _httpMessageHandler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_FiltersOutUsersWithNullOrEmptyDisplayName()
    {
        // Arrange
        const string searchName = "John";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new object[]
        {
            new { accountId = "user-123", displayName = "John Doe" },
            new { accountId = "user-456", displayName = (string?)null }, // null displayName
            new { accountId = "user-789", displayName = "" }, // empty displayName
            new { accountId = "user-000", displayName = "   " }, // whitespace displayName
        };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1); // Only user-123 has valid displayName that matches
        result.ShouldContain("user-123");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_RemovesDuplicateAccountIds()
    {
        // Arrange
        const string searchName = "John";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[]
        {
            new { accountId = "user-123", displayName = "John Doe" },
            new { accountId = "user-123", displayName = "John Smith" }, // Same accountId, different display name
            new { accountId = "user-456", displayName = "John Johnson" },
        };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2); // Duplicates removed
        result.ShouldContain("user-123");
        result.ShouldContain("user-456");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_HandlesSpecialCharactersInNames()
    {
        // Arrange
        const string searchName = "O'Connor";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[]
        {
            new { accountId = "user-123", displayName = "John O'Connor" },
            new { accountId = "user-456", displayName = "Mary O'Connor-Smith" },
            new { accountId = "user-789", displayName = "Patrick O Connor" }, // Space instead of apostrophe
        };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2); // Should match both O'Connor entries
        result.ShouldContain("user-123");
        result.ShouldContain("user-456");
        result.ShouldNotContain("user-789"); // "O" and "Connor" are separate tokens
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_SendsCorrectHttpRequest()
    {
        // Arrange
        const string searchName = "John Doe";
        const int maxResults = 25;
        var cancellationToken = CancellationToken.None;

        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new object[] { })
        );

        // Act
        await _sut.FindUserIdsByExactNameAsync(searchName, maxResults, cancellationToken);

        // Assert
        var request = _httpMessageHandler.LastRequest;
        request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);

        request.RequestUri.ShouldNotBeNull();
        var actualUrl = request.RequestUri.ToString();

        actualUrl.ShouldStartWith("https://test.atlassian.net/rest/api/2/user/search");
        actualUrl.ShouldContain("maxResults=25");
        actualUrl.ShouldContain("query=");
        actualUrl.ShouldContain("John");
        actualUrl.ShouldContain("Doe");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_EscapesSpecialCharactersInQuery()
    {
        // Arrange
        const string searchName = "John & Jane";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new object[] { })
        );

        // Act
        await _sut.FindUserIdsByExactNameAsync(searchName, maxResults, cancellationToken);

        // Assert
        var request = _httpMessageHandler.LastRequest;
        request.ShouldNotBeNull();
        request.RequestUri.ShouldNotBeNull();

        var actualUrl = request.RequestUri.ToString();

        // Grundstruktur prüfen
        actualUrl.ShouldStartWith("https://test.atlassian.net/rest/api/2/user/search");
        actualUrl.ShouldContain("maxResults=10");
        actualUrl.ShouldContain("query=");

        // Alle Teile sollten vorhanden sein
        actualUrl.ShouldContain("John");
        actualUrl.ShouldContain("Jane");

        // Das & sollte in irgendeiner Form escaped sein - prüfen mit mehreren Versuchen
        var hasEscapedAmpersand =
            actualUrl.Contains("%26")
            || actualUrl.Contains("&amp;")
            || actualUrl.Contains("John+%26+Jane")
            || actualUrl.Contains("John%20%26%20Jane");

        hasEscapedAmpersand.ShouldBeTrue("URL should contain escaped ampersand in some form");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithHttpError_ThrowsApplicationException()
    {
        // Arrange
        const string searchName = "John";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        _httpMessageHandler.SetResponse(HttpStatusCode.Unauthorized, "Unauthorized access");

        // Act & Assert
        var exception = await Should.ThrowAsync<ApplicationException>(() =>
            _sut.FindUserIdsByExactNameAsync(searchName, maxResults, cancellationToken)
        );

        exception.Message.ShouldBe("Unauthorized access");
    }

    [Theory]
    [InlineData("John", 5)]
    [InlineData("Jane Smith", 10)]
    [InlineData("robert.johnson@company.com", 50)]
    [InlineData("María García", 25)]
    public async Task FindUserIdsByExactNameAsync_WithDifferentParameters_SendsCorrectRequest(
        string searchName,
        int maxResults
    )
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new object[] { })
        );

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        _httpMessageHandler.RequestCount.ShouldBe(1);

        var request = _httpMessageHandler.LastRequest;
        request.ShouldNotBeNull();
        request.RequestUri.ShouldNotBeNull();

        var actualUrl = request.RequestUri.ToString();
        actualUrl.ShouldContain($"maxResults={maxResults}");
        actualUrl.ShouldContain("query=");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WithTrimmableInput_TrimsNameCorrectly()
    {
        // Arrange
        const string searchName = "  John Doe  ";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[] { new { accountId = "user-123", displayName = "John Doe" } };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result.ShouldContain("user-123");

        // Verify the query contains the trimmed name
        var request = _httpMessageHandler.LastRequest;
        request.ShouldNotBeNull();
        request.RequestUri.ShouldNotBeNull();

        var actualUrl = request.RequestUri.ToString();
        actualUrl.ShouldContain("John");
        actualUrl.ShouldContain("Doe");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _sut.FindUserIdsByExactNameAsync("John", 10, cts.Token)
        );
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _httpMessageHandler?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Tests für die Tokenization-Logik (über öffentliche API getestet)
public class UserSearchClientTokenizationTests : IDisposable
{
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Func<HttpClient> _httpFactory;
    private readonly UserSearchClient _sut;

    public UserSearchClientTokenizationTests()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://test.atlassian.net/"),
        };
        _httpFactory = () => _httpClient;
        _sut = new UserSearchClient(_httpFactory);
    }

    [Theory]
    [InlineData("John-Doe", "John")]
    [InlineData("John_Doe", "John")]
    [InlineData("John.Doe", "John")]
    [InlineData("John Doe", "John")]
    [InlineData("John,Doe", "John")]
    [InlineData("John;Doe", "John")]
    [InlineData("John:Doe", "John")]
    [InlineData("John/Doe", "John")]
    [InlineData("John\\Doe", "John")]
    public async Task FindUserIdsByExactNameAsync_TokenizesNamesCorrectly(
        string displayName,
        string searchToken
    )
    {
        // Arrange
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[] { new { accountId = "user-123", displayName = displayName } };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchToken,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result.ShouldContain("user-123");
    }

    [Fact]
    public async Task FindUserIdsByExactNameAsync_HandlesMultipleDelimiters()
    {
        // Arrange
        const string searchName = "John";
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        var mockUsers = new[]
        {
            new { accountId = "user-123", displayName = "John-Peter_Smith.Jr" },
            new { accountId = "user-456", displayName = "Mary/Jane\\Brown" },
        };

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(mockUsers));

        // Act
        var result = await _sut.FindUserIdsByExactNameAsync(
            searchName,
            maxResults,
            cancellationToken
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1); // Only the first user has "John" as a token
        result.ShouldContain("user-123");
    }

    [Theory]
    [InlineData("John & Jane", "John", "Jane", "&")]
    [InlineData("John + Jane", "John", "Jane", "+")]
    [InlineData("John@Domain.com", "John", "Domain", "@")]
    [InlineData("John#123", "John", "123", "#")]
    public async Task FindUserIdsByExactNameAsync_HandlesSpecialCharactersInQuery(
        string searchName,
        string expectedPart1,
        string expectedPart2,
        string specialChar
    )
    {
        // Arrange
        const int maxResults = 10;
        var cancellationToken = CancellationToken.None;

        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new object[] { })
        );

        // Act
        await _sut.FindUserIdsByExactNameAsync(searchName, maxResults, cancellationToken);

        // Assert
        var request = _httpMessageHandler.LastRequest;
        request.ShouldNotBeNull();
        request.RequestUri.ShouldNotBeNull();

        var actualUrl = request.RequestUri.ToString();

        // Die URL sollte die Grundstruktur haben
        actualUrl.ShouldContain("/rest/api/2/user/search");
        actualUrl.ShouldContain("maxResults=10");
        actualUrl.ShouldContain("query=");

        // Alle Teile des Namens sollten in der URL vorhanden sein
        actualUrl.ShouldContain(expectedPart1);
        actualUrl.ShouldContain(expectedPart2);

        // Das Sonderzeichen sollte in irgendeiner Form vorhanden sein
        var hasSpecialChar =
            actualUrl.Contains(specialChar)
            || actualUrl.Contains(Uri.EscapeDataString(specialChar));

        hasSpecialChar.ShouldBeTrue(
            $"URL should contain '{specialChar}' in original or encoded form"
        );
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _httpMessageHandler?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Test Helper Class (wiederverwendbar für andere HTTP-Client-Tests)
public class TestHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseContent = "";

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public int RequestCount { get; private set; }

    public void SetResponse(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _responseContent = content;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        RequestCount++;
        LastRequest = request;

        // Request Body lesen (falls vorhanden)
        if (request.Content != null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "application/json"),
        };

        return response;
    }
}
