namespace Flow.JiraSearch.JiraClient;

internal interface IIssueQueryBuilder
{
    Task<string> BuildTextJql(
        string text,
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken
    );
}

internal class IssueQueryBuilder(IUserSearchClient userSearchClient) : IIssueQueryBuilder
{
    public async Task<string> BuildTextJql(
        string text,
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken
    )
    {
        var tokens = text.Tokenize();

        return await tokens
            .When("#all")
            .ThenDoNothing()
            .When("#([a-zA-Z]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"project IN ({string.Join(", ", mem)})")
            .Else(projects.Count > 0 ? $"project IN ({string.Join(", ", projects)})" : string.Empty)
            .When("\\!")
            .Then("statusCategory = Done")
            .When("\\*")
            .ThenDoNothing() // tut nichts, aber resetet den Match-State
            .Else("statusCategory != Done")
            .When("@free")
            .Then("assignee IS EMPTY")
            .When("@me")
            .ThenRemember("currentUser()")
            .When(@"@([\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"assignee IN ({string.Join(", ", mem)})")
            .When(@"\+([a-zA-Z0-9]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"labels IN ({string.Join(", ", mem)})")
            .When(@"[A-Z][A-Z0-9]+-\d+")
            .ThenRemember()
            .Aggregate(mem => $"issuekey IN ({string.Join(", ", mem)})")
            .When("@reporter:me")
            .ThenRemember("currentUser()")
            .When("@reporter:([\\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"reporter IN ({string.Join(", ", mem)})")
            .When("@was:me")
            .ThenRemember("currentUser()")
            .When("@was:([\\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"assignee WAS ({string.Join(", ", mem)})")
            .When(".*")
            .ThenRemember()
            .Aggregate(mem =>
                $"(summary ~ \"{string.Join(" ", mem)}\" OR text ~ \"{string.Join(" ", mem)}\")"
            )
            .BuildJql();
    }
}
