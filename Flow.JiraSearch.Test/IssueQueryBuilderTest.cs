using Flow.JiraSearch.JiraClient;
using NSubstitute;
using Shouldly;

namespace Flow.JiraSearch.Test;

public class IssueQueryBuilderTest
{
    [Theory]
    [InlineData("", "project IN (AAA, BBB) AND statusCategory != Done")]
    [InlineData("#all", "statusCategory != Done")]
    [InlineData("#ABC #XYZ", "project IN (ABC, XYZ) AND statusCategory != Done")]
    [InlineData("!", "project IN (AAA, BBB) AND statusCategory = Done")]
    [InlineData("*", "project IN (AAA, BBB)")]
    [InlineData(
        "@me",
        "project IN (AAA, BBB) AND statusCategory != Done AND assignee IN (currentUser())"
    )]
    [InlineData("@john", "project IN (AAA, BBB) AND statusCategory != Done AND assignee IN (JOHN)")]
    [InlineData(
        "@me @john",
        "project IN (AAA, BBB) AND statusCategory != Done AND assignee IN (currentUser(), JOHN)"
    )]
    [InlineData("@free", "project IN (AAA, BBB) AND statusCategory != Done AND assignee IS EMPTY")]
    [InlineData(
        "+label1 +label2",
        "project IN (AAA, BBB) AND statusCategory != Done AND labels IN (label1, label2)"
    )]
    [InlineData(
        "ISSUE-123 ISSUE-456",
        "project IN (AAA, BBB) AND statusCategory != Done AND issuekey IN (ISSUE-123, ISSUE-456)"
    )]
    [InlineData(
        "@reporter:me",
        "project IN (AAA, BBB) AND statusCategory != Done AND reporter IN (currentUser())"
    )]
    [InlineData(
        "@reporter:me @reporter:john",
        "project IN (AAA, BBB) AND statusCategory != Done AND reporter IN (currentUser(), JOHN)"
    )]
    [InlineData(
        "this is a test",
        "project IN (AAA, BBB) AND statusCategory != Done AND (summary ~ \"this is a test\" OR text ~ \"this is a test\")"
    )]
    [InlineData(
        "@me this +label1 is ISSUE-123 a ! test #ABC @john",
        "project IN (ABC) AND statusCategory = Done AND assignee IN (currentUser(), JOHN) AND labels IN (label1) AND issuekey IN (ISSUE-123) AND (summary ~ \"this is a test\" OR text ~ \"this is a test\")"
    )]
    public async Task TestBuildQuery(string input, string expectedOutput)
    {
        // Act
        var jiraUserSearch = Substitute.For<IUserSearchClient>();
        jiraUserSearch
            .FindUserIdsByExactNameAsync(
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(callInfo => new List<string> { callInfo.Arg<string>().ToUpperInvariant() });

        var queryBuilder = new IssueQueryBuilder(jiraUserSearch);

        var output = await queryBuilder.BuildTextJql(
            input,
            ["AAA", "BBB"],
            TestContext.Current.CancellationToken
        );

        output.ShouldBe(expectedOutput);
    }
}
