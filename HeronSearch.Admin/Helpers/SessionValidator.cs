using System.Globalization;
using System.Security.Claims;
using HeronSearch.Admin.Data;
using HeronSearch.Admin.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Helpers;

// The per-request check of the sign-in cookie (Program.cs sets OnValidatePrincipal = SessionValidator.ValidateAsync).
// It confirms in the database that the user still exists, still has the cookie's role, and is still Verified.
public static class SessionValidator
{
    // Runs on every request that carries the cookie. Signs the user out when the check fails or the database cannot
    // answer (fail closed); otherwise renews a browser-session cookie so it expires after 8 hours without activity.
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        // Sign-in stores the user Id (digits only) in NameIdentifier and the role name ("Admin" or "Staff") in Role.
        var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;

        if (!int.TryParse(idClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            || string.IsNullOrEmpty(roleClaim))
        {
            await RejectAsync(context);
            return;
        }

        var requestAborted = context.HttpContext.RequestAborted;
        bool stillValid;

        try
        {
            // Primary-key lookup of just the two columns the check needs, without change tracking.
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var account = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Role, u.VerificationStatus })
                .SingleOrDefaultAsync(requestAborted);

            // Valid only when the row exists, the role still matches the cookie, and the account is Verified.
            stillValid = account != null
                && account.Role.ToString() == roleClaim
                && account.VerificationStatus == VerificationStatus.Verified;
        }
        catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
        {
            // The browser closed the request, so no page is waiting; ASP.NET Core ends it quietly (status 499).
            throw;
        }
        catch (Exception ex)
        {
            // Any other failure to reach or query the database (server down, wrong password, missing table) fails closed.
            // Only the user Id is logged: never the cookie, the email, or the connection string.
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(SessionValidator));
            logger.LogWarning(ex,
                "Could not confirm signed-in user {UserId} in the database, so the session was rejected (fail closed).",
                userId);
            stillValid = false;
        }

        if (!stillValid)
        {
            await RejectAsync(context);
            return;
        }

        // A browser-session cookie is renewed on every validated request, restarting its 8-hour idle timer.
        // A persistent ("Remember me") cookie is not renewed, so it keeps its fixed 14-day expiry.
        if (!context.Properties.IsPersistent)
        {
            context.ShouldRenew = true;
        }
    }

    // Treats this request as signed out and deletes the cookie; a protected page then redirects to the Login page
    // with the requested page as the return URL.
    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
