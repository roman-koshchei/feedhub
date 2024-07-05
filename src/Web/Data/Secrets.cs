using Lib;

namespace Web.Data;

[Env]
public static class Secrets
{
    public static readonly string GitHubApiToken = Env.GetRequired("GITHUB_API_TOKEN");
}