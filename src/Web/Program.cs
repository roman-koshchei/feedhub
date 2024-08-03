using Lib;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Handlers;

Env.LoadFile("./.env");
var dbConnectionString = Env.GetRequired("DB_CONNECTION_STRING");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<Db>(options => options.UseNpgsql(dbConnectionString));
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
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

    options.LoginPath = new PathString(AuthHandlers.GetStartedRoute.Url());
    options.AccessDeniedPath = new PathString(AuthHandlers.GetStartedRoute.Url());
    options.LogoutPath = new PathString("/dashboard/logout");
    options.ReturnUrlParameter = "comeback";

    options.SlidingExpiration = true;
});

builder.Services.AddRateLimiter(limiter =>
{
    limiter.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 15;
        options.Window = TimeSpan.FromMinutes(3);
        options.QueueLimit = 0;
        options.AutoReplenishment = true;
    });

    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.OnRejected = ErrorHandlers.RateLimiterOnReject;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");
app.UseStatusCodePages(ErrorHandlers.StatusCodePages);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

FeedbackHandlers.Map(app);
DashboardHandlers.Map(app);
HomeHandlers.Map(app);
AuthHandlers.Map(app);
ErrorHandlers.Map(app);

app.Run();