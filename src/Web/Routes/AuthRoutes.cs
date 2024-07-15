using Lib;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Web.Data;
using Web.Lib;
using Web.Services;

namespace Web.Routes;

public static class AuthRoutes
{
    public static readonly URoute LoginRoute = DashboardRoutes.DashboardRoute.Add("login");
    public static readonly URoute RegisterRoute = DashboardRoutes.DashboardRoute.Add("register");

    public class AuthForm
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }

    public static async Task LoginPage(
        HtmlWave html, string? email, string? emailError, string? passwordError
    )
    {
        await html.Complete(UI.Layout("Login - Feedhub").Wrap($@"
            {UI.Heading("Login", "Start getting feedback from your users right now!")}
            <form method='post' action='/dashboard/login'>
                {UI.Input(
                    name: nameof(AuthForm.Email), type: "email",
                    label: "Email", placeholder: "Your email",
                    isRequired: true, error: emailError, value: email
                )}
                {UI.Input(
                    name: nameof(AuthForm.Password), type: "password",
                    label: "Password", placeholder: "Password",
                    isRequired: true, error: passwordError
                )}

                <button>Login</button>
            </form>
            <p></p>
            <p><a href='{RegisterRoute.Url()}'>Don't have account yet? Register</a></p>
        "));
    }

    public static void Map(IEndpointRouteBuilder builder)
    {
        builder.MapGet(LoginRoute.Pattern, async (HttpResponse res) =>
        {
            HtmlWave wave = new(res);
            await LoginPage(wave, null, null, null);
        });

        builder.MapPost(LoginRoute.Pattern,
        async (
            HttpResponse res,
            [FromForm] AuthForm form,
            [FromServices] UserManager<User> userManager,
            [FromServices] SignInManager<User> signInManager,
            [FromQuery] string? comeback = null
        ) =>
        {
            HtmlWave wave = new(res);

            if (!form.IsValid()) return Results.Redirect(LoginRoute.Url());

            var user = await userManager.FindByEmailAsync(form.Email);
            if (user == null)
            {
                await LoginPage(wave, form.Email, "User with such email isn't found", null);
                return Results.NotFound();
            }

            var result = await signInManager.PasswordSignInAsync(user, form.Password, true, false);
            if (!result.Succeeded)
            {
                await LoginPage(wave, form.Email, "Loogs good", "We can't login you with provided password");
                return Results.BadRequest();
            }

            return Results.Redirect(DashboardRoutes.DashboardRoute.Url());
        }).DisableAntiforgery();

        builder.MapGet(RegisterRoute.Pattern, async (HttpResponse res) =>
        {
            var wave = new HtmlWave(res);
            var layout = UI.Layout("Register - Feedhub");
            await wave.Write(layout.Start);

            await wave.Write($@"
                <h1>Register</h1>
                <form method='post'>
                    <input name='{nameof(AuthForm.Email)}' placeholder='Email' />
                    <input name='{nameof(AuthForm.Password)}' type='password' placeholder='Password' />
                    <button>Register</button>
                </form>
            ");

            await wave.Complete(layout.End);
        });

        builder.MapPost(RegisterRoute.Pattern,
        async (
            HttpResponse res,
            [FromForm] AuthForm form,
            [FromServices] UserManager<User> userManager
        ) =>
        {
            if (!form.IsValid()) return Results.Redirect(RegisterRoute.Url());

            var user = new User { UserName = form.Email, Email = form.Email };
            var createRes = await userManager.CreateAsync(user, form.Password);

            if (createRes.Succeeded) return Results.Redirect(LoginRoute.Url());
            return Results.Redirect(RegisterRoute.Url());
        }).DisableAntiforgery();
    }
}