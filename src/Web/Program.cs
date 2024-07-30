using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Routes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<Db>(options => options.UseSqlite($"Data Source=./feedhub.db"));
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 1;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<Db>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication()
    .AddCookie();

builder.Services.AddAuthorization();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    options.LoginPath = new PathString(AuthRoutes.GetStartedRoute.Url());
    options.AccessDeniedPath = new PathString(AuthRoutes.GetStartedRoute.Url());
    options.LogoutPath = new PathString("/dashboard/logout");
    options.ReturnUrlParameter = "comeback";

    options.SlidingExpiration = true;
});

builder.Services.AddRateLimiter(limiter =>
{
    limiter.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 1;
        options.Window = TimeSpan.FromMinutes(3);
        options.QueueLimit = 0;
        options.AutoReplenishment = true;
    });

    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.OnRejected = ErrorHandlers.RateLimiterOnReject;
});

var app = builder.Build();

app.UseStatusCodePages(ErrorHandlers.StatusCodePages);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

FeedbackRoutes.Map(app);
DashboardRoutes.Map(app);
HomeRoutes.Map(app);
AuthRoutes.Map(app);
ErrorHandlers.Map(app);

app.Run();