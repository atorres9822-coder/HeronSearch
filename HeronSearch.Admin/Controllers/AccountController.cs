using System.Security.Claims;
using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// Handles sign-in and sign-out. The Login actions are open to anonymous visitors ([AllowAnonymous] on each);
// Logout requires a signed-in BackOffice session. Only Verified Admin and Staff accounts get in.
// The security claim type is written as System.Security.Claims.Claim throughout, because the entity Claim shares the name.
public class AccountController : Controller
{
    // The database, injected so the controller can look up the account and write the sign-in log row.
    private readonly AppDbContext _db;

    // Hashes and verifies passwords; registered as a singleton in Program.cs.
    private readonly IPasswordHasher<User> _passwordHasher;

    // Receives the database context and the password hasher from dependency injection.
    public AccountController(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    // Shows the sign-in page. A visitor who is already signed in as Admin or Staff is sent straight to the Dashboard;
    // everyone else sees the form, carrying the page they were heading to so sign-in can return them there.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && (User.IsInRole("Admin") || User.IsInRole("Staff")))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Student"))
        {
            return RedirectToAction("Index", "Student");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // Checks the sign-in form and signs the user in. It follows the seven steps in the design: it rejects invalid input,
    // wrong credentials, Student accounts, and accounts that are not Verified, and only then issues the cookie. One Login
    // row is logged for every attempt that reaches the credential check (success or failure); the password is never logged.
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // Step 1: invalid input redisplays the form with field messages and logs nothing.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Step 2: normalize the email (trim + lowercase) and look up the one account that could match.
        var email = BusinessRules.NormalizeEmail(model.Email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Step 3: no such account, or the password does not verify, is one "Invalid email or password." error.
        var verifyResult = user == null
            ? PasswordVerificationResult.Failed
            : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

        if (user == null || verifyResult == PasswordVerificationResult.Failed)
        {
            ActivityLogger.Log(_db, user?.Id, LogActions.Login, null, LogOutcome.Failed,
                $"Failed sign-in for '{email}'.");
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            model.Password = "";
            return View(model);
        }

        // Step 4: a correct password stored with an outdated hash counts as success; refresh the stored hash now.
        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        // Step 5: only Verified accounts may sign in. Students see "awaiting verification"; Admin and Staff get
        // the Pending/Suspended reason. (A Student can now sign in, so there is no admin-portal-only block here.)
        if (user.VerificationStatus != VerificationStatus.Verified)
        {
            string reason;
            if (user.Role == UserRole.Student)
            {
                reason = user.VerificationStatus == VerificationStatus.Pending
                    ? "Your account is awaiting verification."
                    : "Your account is Suspended. Contact an Admin.";
            }
            else
            {
                reason = user.VerificationStatus == VerificationStatus.Pending
                    ? "Your account is Pending verification. Ask an Admin to verify it."
                    : "Your account is Suspended. Contact an Admin.";
            }

            ActivityLogger.Log(_db, user.Id, LogActions.Login, null, LogOutcome.Failed,
                $"Sign-in for '{email}' blocked: account is {user.VerificationStatus}.");
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, reason);
            model.Password = "";
            return View(model);
        }

        // Step 7: build the cookie identity from the account's claims and sign in.
        var claims = new List<System.Security.Claims.Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Remember me makes the cookie persistent for 14 days; otherwise it ends with the browser session.
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
        };

        ActivityLogger.Log(_db, user.Id, LogActions.Login, null, LogOutcome.Success,
            $"Signed in as '{email}'.");
        await _db.SaveChangesAsync();

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        // Return the user to the local page they were heading to, or their home: Students land on /Student,
        // Admin and Staff on /Dashboard.
        if (Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl!);
        }

        if (user.Role == UserRole.Student)
        {
            return RedirectToAction("Index", "Student");
        }

        return RedirectToAction("Index", "Dashboard");
    }

    // Signs the signed-in user out and returns them to the SignedOut page. Any signed-in user reaches here (Admin,
    // Staff, or Student), so a Logout row is always logged against a real user before the cookie is cleared.
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // An authenticated request guarantees a numeric Id and a name claim; read them for the log row.
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var fullName = User.FindFirstValue(ClaimTypes.Name);

        ActivityLogger.Log(_db, userId, LogActions.Logout, null, LogOutcome.Success,
            $"Signed out '{fullName}'.");
        await _db.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(SignedOut));
    }

    // Shows the "You've been signed out" page. Open to anonymous visitors, because the cookie is already cleared.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult SignedOut()
    {
        return View();
    }

    // The message shown when someone registers with an email an account already uses.
    private const string DuplicateEmailMessage = "An account with this email already exists.";

    // Shows the student Create Account page. A visitor already signed in as a Student goes to their dashboard;
    // an Admin or Staff session goes to the admin Dashboard; everyone else sees the form.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Student"))
        {
            return RedirectToAction("Index", "Student");
        }

        if (User.Identity?.IsAuthenticated == true && (User.IsInRole("Admin") || User.IsInRole("Staff")))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    // Handles the Create Account form: validates it, rejects a duplicate email, hashes the password, and creates a
    // Pending Student account dated now. A new student cannot sign in until an Admin verifies the account.
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Normalize the email and reject it when an account already uses it.
        var email = BusinessRules.NormalizeEmail(model.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), DuplicateEmailMessage);
            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            StudentId = string.IsNullOrWhiteSpace(model.StudentId) ? null : model.StudentId.Trim(),
            Role = UserRole.Student,
            VerificationStatus = VerificationStatus.Pending,
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A race on the unique Email index: treat it as a duplicate email and redisplay the form.
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(nameof(model.Email), DuplicateEmailMessage);
            return View(model);
        }

        // Send the new student to Login with a message that their account awaits verification.
        TempData["Success"] = "Your account is awaiting verification.";
        return RedirectToAction(nameof(Login));
    }
}
