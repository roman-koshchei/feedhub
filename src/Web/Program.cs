using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Lib;
using Web.Routes;
using Web.Services;

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

    options.LoginPath = new PathString("/dashboard/login");
    options.AccessDeniedPath = new PathString("/dashboard/login");
    options.LogoutPath = new PathString("/dashboard/logout");
    options.ReturnUrlParameter = "return";

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
    limiter.OnRejected = ErrorHandlers.RateLimitReached;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

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

app.Run();