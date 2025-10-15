using System.Net;
using System.Text.Json;
using Flow.JiraSearch.JiraClient;
using Shouldly;

namespace Flow.JiraSearch.Test;

public class IssueSearchClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IssueSearchClient _sut;

    public IssueSearchClientTests()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            // BaseAddress für relative URIs setzen
            BaseAddress = new Uri("https://test.atlassian.net/"),
        };
        var httpFactory = () => _httpClient;
        _sut = new IssueSearchClient(httpFactory);
    }

    [Fact]
    public async Task SearchJqlAsync_WithValidJql_ReturnsIssueResponse()
    {
        // Arrange
        const string jql = "project = TEST";
        const int maxResults = 50;
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new IssueResponse
        {
            Issues = new List<JiraIssue>
            {
                new()
                {
                    Key = "TEST-1",
                    Fields = new JiraFields
                    {
                        Summary = "Test Issue Summary",
                        Status = new JiraStatus
                        {
                            Name = "Open",
                            IconUrl = "https://example.com/status.png",
                        },
                        Priority = new JiraPriority
                        {
                            Name = "High",
                            IconUrl = "https://example.com/priority.png",
                        },
                        Assignee = new JiraUser
                        {
                            DisplayName = "John Doe",
                            AvatarUrls = new JiraAvatarUrls
                            {
                                Size16 = "https://example.com/avatar16.png",
                                Size24 = "https://example.com/avatar24.png",
                            },
                        },
                        Project = new JiraProject { Key = "TEST" },
                        IssueType = new JiraIssueType
                        {
                            Name = "Bug",
                            IconUrl = "https://example.com/bug.png",
                        },
                    },
                },
            },
            Total = 1,
            StartAt = 0,
            MaxResults = 50,
        };

        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse)
        );

        // Act
        var result = await _sut.SearchJqlAsync(jql, maxResults, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Issues.ShouldNotBeEmpty();
        result.Issues.Count.ShouldBe(1);
        result.Total.ShouldBe(1);
        result.StartAt.ShouldBe(0);
        result.MaxResults.ShouldBe(50);

        // Test issue details
        var issue = result.Issues[0];
        issue.Key.ShouldBe("TEST-1");
        issue.Fields.Summary.ShouldBe("Test Issue Summary");
        issue.Fields.Status.Name.ShouldBe("Open");
        issue.Fields.Priority!.Name.ShouldBe("High");
        issue.Fields.Assignee!.DisplayName.ShouldBe("John Doe");
        issue.Fields.Project!.Key.ShouldBe("TEST");
        issue.Fields.IssueType!.Name.ShouldBe("Bug");
    }

    [Fact]
    public async Task SearchJqlAsync_SendsCorrectRequestBody()
    {
        // Arrange
        const string jql = "project = TEST AND status = 'In Progress'";
        const int maxResults = 100;
        var cancellationToken = CancellationToken.None;

        var responseJson = JsonSerializer.Serialize(
            new IssueResponse { Issues = new List<JiraIssue>(), Total = 0 }
        );

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, responseJson);

        // Act
        await _sut.SearchJqlAsync(jql, maxResults, cancellationToken);

        // Assert
        var request = _httpMessageHandler.LastRequest;
        ShouldBeNullExtensions.ShouldNotBeNull<HttpRequestMessage>(request);
        ShouldBeTestExtensions.ShouldBe(request.Method, HttpMethod.Post);

        // Vollständige URI prüfen (BaseAddress + relative URI)
        ShouldBeNullExtensions.ShouldNotBeNull<Uri>(request.RequestUri);
        ShouldBeStringTestExtensions.ShouldBe(request.RequestUri.ToString(), "https://test.atlassian.net/rest/api/2/search/jql");

        var requestBody = _httpMessageHandler.LastRequestBody;
        ShouldBeNullExtensions.ShouldNotBeNull<string>(requestBody);

        var requestJson = JsonDocument.Parse(requestBody);

        requestJson.RootElement.GetProperty("jql").GetString().ShouldBe(jql);
        requestJson.RootElement.GetProperty("maxResults").GetInt32().ShouldBe(maxResults);

        var fields = requestJson.RootElement.GetProperty("fields");
        fields.GetArrayLength().ShouldBe(6);

        var fieldNames = new List<string>();
        for (int i = 0; i < fields.GetArrayLength(); i++)
        {
            fieldNames.Add(fields[i].GetString()!);
        }

        fieldNames.ShouldContain("summary");
        fieldNames.ShouldContain("status");
        fieldNames.ShouldContain("priority");
        fieldNames.ShouldContain("issuetype");
        fieldNames.ShouldContain("assignee");
        fieldNames.ShouldContain("project");
    }

    [Fact]
    public async Task SearchJqlAsync_WithHttpError_ReturnsNull()
    {
        // Arrange
        const string jql = "project = INVALID";
        const int maxResults = 50;
        var cancellationToken = CancellationToken.None;

        _httpMessageHandler.SetResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _sut.SearchJqlAsync(jql, maxResults, cancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SearchJqlAsync_WithEmptyResults_ReturnsEmptyResponse()
    {
        // Arrange
        const string jql = "project = EMPTY";
        const int maxResults = 50;
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new IssueResponse
        {
            Issues = new List<JiraIssue>(),
            Total = 0,
            StartAt = 0,
            MaxResults = 50,
        };

        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse)
        );

        // Act
        var result = await _sut.SearchJqlAsync(jql, maxResults, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Issues.ShouldBeEmpty();
        result.Total.ShouldBe(0);
        result.StartAt.ShouldBe(0);
        result.MaxResults.ShouldBe(50);
    }

    [Theory]
    [InlineData("project = TEST", 10)]
    [InlineData("assignee = currentUser()", 50)]
    [InlineData("status = 'In Progress'", 100)]
    public async Task SearchJqlAsync_WithDifferentJqlQueries_CallsHttpClientCorrectly(
        string jql,
        int maxResults
    )
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var responseJson = JsonSerializer.Serialize(
            new IssueResponse { Issues = new List<JiraIssue>(), Total = 0 }
        );

        _httpMessageHandler.SetResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.SearchJqlAsync(jql, maxResults, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        ShouldBeTestExtensions.ShouldBe(_httpMessageHandler.RequestCount, 1);
    }

    [Fact]
    public void JiraIssue_BrowseUrl_ReturnsCorrectUrl()
    {
        // Arrange
        const string baseUrl = "https://company.atlassian.net";
        var issue = new JiraIssue { Key = "PROJ-123" };

        // Act
        var browseUrl = issue.BrowseUrl(baseUrl);

        // Assert
        browseUrl.ShouldBe("https://company.atlassian.net/browse/PROJ-123");
    }

    [Fact]
    public async Task SearchJqlAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _sut.SearchJqlAsync("project = TEST", 50, cts.Token)
        );
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _httpMessageHandler?.Dispose();
        GC.SuppressFinalize(this);
    }
}
