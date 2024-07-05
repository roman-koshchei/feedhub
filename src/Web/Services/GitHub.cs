using Octokit;
using Web.Data;

namespace Web.Services;

public class GitHub
{
    private const string FEEDHUB_LABEL = "feedhub";

    private readonly GitHubClient client;
    private readonly string repoOwner;
    private readonly string repoName;

    public GitHub(string owner, string name)
    {
        this.repoOwner = owner;
        this.repoName = name;
        client = new GitHubClient(new ProductHeaderValue("roman-koshchei"));
        var token = Secrets.GitHubApiToken;
        client.Credentials = new Credentials(token);
    }

    public async Task<IReadOnlyList<Issue>?> GetFeedHubIssues()
    {
        try
        {
            var request = new RepositoryIssueRequest();
            request.Labels.Add(FEEDHUB_LABEL);
            var apiOptions = new ApiOptions
            {
                StartPage = 1,
                PageSize = 10
            };
            var res = await client.Issue.GetAllForRepository(repoOwner, repoName, request, apiOptions);
            return res;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Exception?> CreateIssue(string? user, string title, string content)
    {
        try
        {
            var issue = new NewIssue(title);
            issue.Labels.Add("bug");
            issue.Labels.Add(FEEDHUB_LABEL);
            issue.Body = $"By {user ?? "Anonymus"}\n{content}";

            await client.Issue.Create(repoOwner, repoName, issue);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    //public async Task<Exception?> CreateDiscussion(string? user, string title, string content)
    //{
    //    try
    //    {
    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        return ex;
    //    }
    //}
}