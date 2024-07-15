using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web.Data;

public class App
{
    public string Id { get; } = Guid.NewGuid().ToString();

    public required string Slug { get; set; }
    public required string RepositoryOwner { get; set; }
    public required string RepositoryName { get; set; }
    public required string GitHubApiToken { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }

    public required string UserId { get; set; }
    public User User { get; } = default!;

    public static void OnModelCreating(ModelBuilder builder)
    {
        var app = builder.Entity<App>();
        app.HasKey(x => x.Id);
        app.HasIndex(x => x.Slug);

        app.HasOne(x => x.User).WithMany(x => x.Apps).HasForeignKey(x => x.UserId);
    }
}

public class User : IdentityUser
{
    public IReadOnlyCollection<App> Apps { get; } = default!;
}

public class Db : IdentityDbContext<User>
{
    public Db(DbContextOptions options) : base(options)
    {
    }

    protected Db()
    {
    }

    public DbSet<App> Apps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        App.OnModelCreating(modelBuilder);
    }
}