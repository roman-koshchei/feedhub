using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Web.Data;
using Web.Lib;

namespace Web.Controllers;

public class AuthController : Controller
{
    public static readonly View LoginView = new("/Views/Auth/Login.cshtml");
    public const string LoginPath = "/login";
    public const string RegisterPath = "/register";

    [HttpGet(LoginPath)]
    public IActionResult Login()
    {
        return View();
    }

    public record LoginInput(
        [Required] [EmailAddress] string Email, 
        [Required] string Password
    );

    [HttpPost(LoginPath)]
    public async Task<IActionResult> LoginPost(
        [FromServices] Db db,
        [FromForm] LoginInput input
    )
    {
        if(!ModelState.IsValid) return View(input);

        var user = db.Users.FirstOrDefault(x => x.Email == input.Email);
        if(user == null) return NotFound();

        if(user.Password == input.Password)
        {
            AppendAuthCookie(user.Id);
            return Redirect("/");
        }

        return View(input);
    }

    [HttpGet(RegisterPath)]
    public IActionResult Register()
    {
        return View();
    }

    public class RegisterInput
    {

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required] 
        public string Password { get; set; } = string.Empty;
        
        [Required, Compare(nameof(Password), ErrorMessage = "Passwords should match")] 
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    [HttpPost(RegisterPath)]
    public async Task<IActionResult> RegisterPost(
        [FromServices] Db db,
        [FromForm] RegisterInput input
    )
    {
        if (!ModelState.IsValid) return View("Register", input);

        var user = new User(input.Email, input.Password);
        db.Users.Add(user);

        AppendAuthCookie(user.Id);

        return Redirect("/");
    }

    [NonAction]
    public void AppendAuthCookie(string id)
    {
        Response.Cookies.Append("uid", id, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Secure = true,
            HttpOnly = true
        });
    }
}
