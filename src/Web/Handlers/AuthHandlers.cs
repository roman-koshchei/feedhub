using Lib;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Handlers;

public static class AuthHandlers
{
    public static readonly WaveRoute GetStartedRoute = DashboardHandlers.DashboardRoute.Add("auth");

    public class AuthFormBody
    {
        [Required, EmailAddress, MinLength(1)]
        public required string Email { get; set; }

        [Required, MinLength(1)]
        public required string Password { get; set; }
    }

    public static string AuthForm(
        string? email, string? emailError, string? passwordError, string btnLabel
    )
    {
        return $@"<form method='post'>
            {UI.Input(
                name: nameof(AuthFormBody.Email), type: "email",
                label: "Email", placeholder: "Your email",
                isRequired: true, error: emailError, value: email
            )}
            {UI.Input(
                name: nameof(AuthFormBody.Password), type: "password",
                label: "Password", placeholder: "Password",
                isRequired: true, error: passwordError
            )}
            <button>{btnLabel}</button>
        </form>";
    }

    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(GetStartedRoute.Pattern, async (HttpResponse res) =>
        {
            var wave = Wave.Html(res, StatusCodes.Status200OK);
            await GetStartedPage(wave, null, null, null);
        });

        builder.MapPost(GetStartedRoute.Pattern, GetStartedHandler).DisableAntiforgery();
        builder.MapGet(LogoutRoute.Pattern, Logout).RequireAuthorization().DisableAntiforgery();
    }

    public static async Task GetStartedPage(
        WaveHtml html, string? email, string? emailError, string? passwordError
    )
    {
        await html.Add(UI.Layout("Get Started - Feedhub").Wrap(
            UI.Heading("Get Started", "Start getting feedback from your users right now!"),
            AuthForm(email, emailError, passwordError, "Confirm")
        ));
    }

    public static async Task GetStartedHandler(
        HttpResponse res,
        [FromForm] AuthFormBody form,
        [FromServices] UserManager<User> userManager,
        [FromServices] SignInManager<User> signInManager,
        [FromQuery] string? comeback = null
    )
    {
        if (!form.IsValid())
        {
            await GetStartedPage(Wave.Html(res, StatusCodes.Status400BadRequest),
                form.Email, "Email is required", "Password is required"
            );
            return;
        }

        var user = await userManager.FindByEmailAsync(form.Email);
        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(user, form.Password, true, false);
            if (!result.Succeeded)
            {
                var wave = Wave.Html(res, StatusCodes.Status400BadRequest);
                await GetStartedPage(wave, form.Email, "Looks good", "We can't login you with provided password");
                return;
            }

            res.Redirect(DashboardHandlers.DashboardRoute.Url());
            return;
        }
        else
        {
            var newUser = new User { UserName = form.Email, Email = form.Email };
            var createRes = await userManager.CreateAsync(newUser, form.Password);
            var signInRes = await signInManager.PasswordSignInAsync(newUser, form.Password, true, false);
            if (createRes.Succeeded && signInRes.Succeeded)
            {
                res.Redirect(DashboardHandlers.DashboardRoute.Url());
                return;
            }

            string? emailError = null;
            List<string> passwordErrors = [];
            foreach (var error in createRes.Errors)
            {
                if (error.Code == nameof(IdentityErrorDescriber.DuplicateEmail))
                {
                    emailError = "Email is already take by another account";
                }
                else if (error.Code == nameof(IdentityErrorDescriber.InvalidEmail))
                {
                    emailError = "Email is invalid";
                }
                else if (error.Code.StartsWith("Password"))
                {
                    passwordErrors.Add(error.Description);
                }
            }

            await GetStartedPage(Wave.Html(res, StatusCodes.Status400BadRequest),
                form.Email, emailError, string.Join(".", passwordErrors)
            );
        }
    }

    public static readonly WaveRoute LogoutRoute = DashboardHandlers.DashboardRoute.Add("logout");

    public static async Task Logout(
        HttpContext ctx,
        [FromServices] SignInManager<User> signInManager
    )
    {
        await signInManager.SignOutAsync();
        Wave.Redirect(ctx, HomeHandlers.HomeRoute.Url());
    }
}