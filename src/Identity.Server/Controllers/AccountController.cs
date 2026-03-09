using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Server.Models;

namespace Identity.Server.Controllers;

public sealed class AccountController(ILogger<AccountController> logger) : Controller
{
    // GET /account/login
    [HttpGet("~/account/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST /account/login
    [HttpPost("~/account/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        // NOTE: In production, replace this with a real user store / password hash check.
        // This demo accepts any non-empty credentials matching the configured test user.
        if (!IsValidUser(model.Username, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        logger.LogInformation("User '{Username}' logged in.", model.Username);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, model.Username),
            new(ClaimTypes.Name, model.Username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(14)
                : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return Redirect("/");
    }

    // GET /account/logout
    [HttpGet("~/account/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    private static bool IsValidUser(string username, string password) =>
        !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
}
