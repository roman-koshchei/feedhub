using System.Collections.Concurrent;

namespace Web.Data;

public class User(string email, string password)
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = email;
    public string Password { get; set; } = password;
}

public record Feedback(string? User, string Content);

public class DbApp(string slug, string repoOwner, string repoName)
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Slug { get; set; } = slug;

    public string RepositoryOwner { get; set; } = repoOwner;
    public string RepositoryName { get; set; } = repoName;
}

public class Db
{
    public ConcurrentBag<User> Users { get; } = [];

    public ConcurrentBag<DbApp> Apps { get; } = [new DbApp("feedhub", "roman-koshchei", "feedhub")];
}