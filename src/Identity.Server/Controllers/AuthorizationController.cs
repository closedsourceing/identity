using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Server.Controllers;

public sealed class AuthorizationController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager) : Controller
{
    // GET/POST /connect/authorize
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        var user = result.Principal;
        var subject = user.GetClaim(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Subject claim is missing.");

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
            ?? throw new InvalidOperationException("The application cannot be found.");

        var applicationId = await applicationManager.GetIdAsync(application, cancellationToken)
            ?? throw new InvalidOperationException("The application ID cannot be retrieved.");

        var authorizations = await authorizationManager.FindAsync(
            subject: subject,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes(),
            cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var consentType = await applicationManager.GetConsentTypeAsync(application, cancellationToken);

        switch (consentType)
        {
            case ConsentTypes.External when !authorizations.Any():
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));

            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Any():
            case ConsentTypes.Explicit when authorizations.Any() && !request.HasPromptValue(PromptValues.Consent):
                var principal = await BuildUserPrincipalAsync(
                    user, subject, request, applicationId, authorizations, cancellationToken);
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            case ConsentTypes.Explicit when request.HasPromptValue(PromptValues.None):
            case ConsentTypes.Systematic when request.HasPromptValue(PromptValues.None):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required."
                    }));

            default:
                return View("Consent", new Models.ConsentViewModel
                {
                    ApplicationName = await applicationManager.GetLocalizedDisplayNameAsync(application)
                        ?? request.ClientId!,
                    Scopes = request.GetScopes().ToList()
                });
        }
    }

    // POST /connect/authorize — user accepted consent
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [FormValueRequired("submit.Accept")]
    [HttpPost("~/connect/authorize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        var user = (await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)).Principal!;
        var subject = user.GetClaim(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Subject claim is missing.");

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
            ?? throw new InvalidOperationException("The application cannot be found.");

        var applicationId = await applicationManager.GetIdAsync(application, cancellationToken)
            ?? throw new InvalidOperationException("The application ID cannot be retrieved.");

        var authorizations = await authorizationManager.FindAsync(
            subject: subject,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes(),
            cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var principal = await BuildUserPrincipalAsync(
            user, subject, request, applicationId, authorizations, cancellationToken);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // POST /connect/authorize — user denied consent
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [FormValueRequired("submit.Deny")]
    [HttpPost("~/connect/authorize")]
    [ValidateAntiForgeryToken]
    public IActionResult Deny() =>
        Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    // POST /connect/token
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            var application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
                ?? throw new InvalidOperationException("The application cannot be found.");

            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject,
                await applicationManager.GetClientIdAsync(application, cancellationToken));
            identity.SetClaim(Claims.Name,
                await applicationManager.GetLocalizedDisplayNameAsync(application));

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            principal.SetResources(
                await scopeManager
                    .ListResourcesAsync(principal.GetScopes(), cancellationToken)
                    .ToListAsync(cancellationToken));

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    // GET/POST /connect/userinfo
    // IgnoreAntiforgeryToken: userinfo is a Bearer-token-protected endpoint per the OIDC spec;
    // CSRF does not apply because cookie authentication is not used here.
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var claimsPrincipal = (await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = claimsPrincipal.GetClaim(Claims.Subject)
                ?? claimsPrincipal.GetClaim(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Subject claim is missing.")
        };

        if (claimsPrincipal.HasScope(Scopes.Email))
        {
            var email = claimsPrincipal.GetClaim(Claims.Email)
                ?? claimsPrincipal.GetClaim(ClaimTypes.Email);
            if (email is not null)
                claims[Claims.Email] = email;
        }

        if (claimsPrincipal.HasScope(Scopes.Profile))
        {
            var name = claimsPrincipal.GetClaim(Claims.Name)
                ?? claimsPrincipal.GetClaim(ClaimTypes.Name);
            if (name is not null)
                claims[Claims.Name] = name;
        }

        return Ok(claims);
    }

    // GET /connect/endsession
    [HttpGet("~/connect/endsession")]
    public IActionResult Logout() => View("Logout");

    // POST /connect/endsession
    [ActionName(nameof(Logout))]
    [HttpPost("~/connect/endsession")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<ClaimsPrincipal> BuildUserPrincipalAsync(
        ClaimsPrincipal user,
        string subject,
        OpenIddictRequest request,
        string applicationId,
        IList<object> existingAuthorizations,
        CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, subject);
        identity.SetClaim(Claims.Name,
            user.GetClaim(ClaimTypes.Name) ?? user.GetClaim(Claims.Name));
        identity.SetClaim(Claims.Email,
            user.GetClaim(ClaimTypes.Email) ?? user.GetClaim(Claims.Email));
        identity.SetClaim(Claims.EmailVerified, "true");

        identity.SetScopes(request.GetScopes());
        identity.SetResources(
            await scopeManager
                .ListResourcesAsync(identity.GetScopes(), cancellationToken)
                .ToListAsync(cancellationToken));

        var authorization = existingAuthorizations.LastOrDefault()
            ?? await authorizationManager.CreateAsync(
                identity: identity,
                subject: subject,
                client: applicationId,
                type: AuthorizationTypes.Permanent,
                scopes: identity.GetScopes(),
                cancellationToken: cancellationToken);

        identity.SetAuthorizationId(
            await authorizationManager.GetIdAsync(authorization, cancellationToken));
        identity.SetDestinations(GetDestinations);

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or ClaimTypes.Name =>
                claim.Subject!.HasScope(Scopes.Profile)
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            Claims.Email or ClaimTypes.Email =>
                claim.Subject!.HasScope(Scopes.Email)
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            Claims.Role or ClaimTypes.Role =>
                claim.Subject!.HasScope(Scopes.Roles)
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [Destinations.AccessToken],

            _ => [Destinations.AccessToken]
        };
    }
}
