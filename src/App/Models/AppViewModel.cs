using Octokit;

namespace Web.Models;

public record AppViewModel(string Slug, IEnumerable<Issue> Issues);