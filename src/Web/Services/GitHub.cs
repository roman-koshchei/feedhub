using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Octokit;

namespace Web.Services;

public class GitHub
{
    private const string FEEDHUB_LABEL = "feedhub";
    public const string UPVOTES_FIELD = "Upvotes:";

    private readonly GitHubClient client;
    private readonly string repoOwner;
    private readonly string repoName;

    public GitHub(string token, string repoOwner, string repoName)
    {
        this.repoOwner = repoOwner;
        this.repoName = repoName;
        client = new GitHubClient(new ProductHeaderValue("Feedhub"));
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

    public async Task<IEnumerable<IssueComment>?> GetIssueComments(int issue)
    {
        try
        {
            var issues = await client.Issue.Comment.GetAllForIssue(repoOwner, repoName, issue);
            return issues;
        }
        catch
        {
            return null;
        }
    }

    public enum IssueType
    {
        Bug, Feedback
    }

    public string IssueTypeName(IssueType type) => type switch
    {
        IssueType.Bug => "bug",
        _ => "feedback",
    };

    public async Task<Exception?> CreateIssue(string title, string content, IssueType type)
    {
        try
        {
            var issue = new NewIssue(title);
            issue.Labels.Add(IssueTypeName(type));
            issue.Labels.Add(FEEDHUB_LABEL);

            //var name = string.IsNullOrEmpty(user) ? "Anonymus" : user;
            //issue.Body = $"By {name}\n{content}";
            issue.Body = $"{content.Trim()}\n\n{UPVOTES_FIELD} 1";

            await client.Issue.Create(repoOwner, repoName, issue);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<Exception?> UpvoteIssue(int issueNumber)
    {
        try
        {
            var issue = await client.Issue.Get(repoOwner, repoName, issueNumber);
            var body = issue.Body.Trim();

            string newBody;
            var lastLineIndex = body.LastIndexOf('\n');
            if (lastLineIndex < 0)
            {
                newBody = $"{body}\n\n{UPVOTES_FIELD} 1";
            }
            else
            {
                var withoutLastLine = body[..lastLineIndex];
                var lastLine = body[(lastLineIndex + 1)..].Trim();
                if (lastLine.StartsWith(UPVOTES_FIELD))
                {
                    var numStr = lastLine[UPVOTES_FIELD.Length..];
                    if (int.TryParse(numStr, out int upvotes))
                    {
                        newBody = $"{withoutLastLine}\n\n{UPVOTES_FIELD} {upvotes + 1}";
                    }
                    else
                    {
                        newBody = $"{withoutLastLine}\n\n{UPVOTES_FIELD} 1";
                    }
                }
                else
                {
                    newBody = $"{body}\n\n{UPVOTES_FIELD} 1";
                }
            }

            IssueUpdate issueUpdate = new() { Body = newBody };
            await client.Issue.Update(repoOwner, repoName, issueNumber, issueUpdate);

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<IssueComment?> CreateComment(int issue, string comment)
    {
        try
        {
            return await client.Issue.Comment.Create(repoOwner, repoName, issue, comment);
        }
        catch
        {
            return null;
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