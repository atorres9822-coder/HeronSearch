using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// The Manage Users pages. Inherits BackOfficeController and adds the AdminOnly policy, so only signed-in Admin
// users reach it. Lists accounts, adds Staff accounts, and verifies, suspends, re-roles, and deletes accounts.
[Authorize(Policy = Policies.AdminOnly)]
public class UsersController : BackOfficeController
{
    // How many users one page of the list shows.
    private const int PageSize = 10;

    // Shown when the submitted email already belongs to an account.
    private const string DuplicateEmailMessage = "An account with this email already exists.";

    // The database context, supplied by dependency injection.
    private readonly AppDbContext _db;

    // Hashes the password of a new Staff account; registered as a singleton in Program.cs.
    private readonly IPasswordHasher<User> _passwordHasher;

    // Receives the database context and the password hasher from the DI container.
    public UsersController(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    // Shows the Manage Users list for the given search term, Status/Role filters, and page number. Search matches
    // FullName, Email, or StudentId (case-insensitive); the list sorts by CreatedAt (newest first), then Id.
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? role,
        int page,
        CancellationToken cancellationToken)
    {
        var searchTerm = FilterValues.NormalizeSearch(search);
        var statusName = FilterValues.MatchAllowedValue(status, Enum.GetNames<VerificationStatus>());
        var roleName = FilterValues.MatchAllowedValue(role, Enum.GetNames<UserRole>());

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (searchTerm != null)
        {
            var lowered = searchTerm.ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(lowered)
                || u.Email.ToLower().Contains(lowered)
                || (u.StudentId != null && u.StudentId.ToLower().Contains(lowered)));
        }

        if (statusName != null)
        {
            var statusValue = Enum.Parse<VerificationStatus>(statusName);
            query = query.Where(u => u.VerificationStatus == statusValue);
        }

        if (roleName != null)
        {
            var roleValue = Enum.Parse<UserRole>(roleName);
            query = query.Where(u => u.Role == roleValue);
        }

        var ordered = query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id);
        var totalCount = await ordered.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, PageSize, "users");

        var selfId = CurrentUserId;
        var rows = await ordered
            .Skip(pager.Skip)
            .Take(PageSize)
            .Select(u => new UserRowViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                StudentId = u.StudentId,
                Role = u.Role,
                VerificationStatus = u.VerificationStatus,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // The self flag and the per-status action flags run in C# after the query.
        foreach (var row in rows)
        {
            row.IsSelf = row.Id == selfId;
            row.CanVerify = BusinessRules.CanVerify(row.VerificationStatus);
            row.CanSuspend = !row.IsSelf && BusinessRules.CanSuspend(row.VerificationStatus);
        }

        pager.AddRouteValue("search", searchTerm);
        pager.AddRouteValue("status", statusName);
        pager.AddRouteValue("role", roleName);

        var model = new UserListViewModel
        {
            Rows = rows,
            Search = searchTerm,
            Status = statusName,
            Role = roleName,
            Pager = pager
        };

        return View(model);
    }

    // Shows the empty Add Staff Account form.
    [HttpGet]
    public IActionResult Create()
    {
        return View(new AddStaffViewModel());
    }

    // Handles the Add Staff Account form: validates it, rejects a duplicate email, hashes the password, and creates
    // a Verified Staff account dated now. Logs "Add Staff" and returns to the list with a success alert.
    [HttpPost]
    public async Task<IActionResult> Create(AddStaffViewModel model, CancellationToken cancellationToken)
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
            StudentId = string.IsNullOrWhiteSpace(model.StaffId) ? null : model.StaffId.Trim(),
            Role = UserRole.Staff,
            VerificationStatus = VerificationStatus.Verified,
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

        ActivityLogger.Log(_db, CurrentUserId, LogActions.AddStaff, null, LogOutcome.Success,
            $"Added Staff account '{user.FullName}' ({user.Email}).");
        await _db.SaveChangesAsync(cancellationToken);

        ShowSuccess($"Added Staff account '{user.FullName}'.");
        return RedirectToAction(nameof(Index));
    }

    // Verifies an account. Refused (already Verified) → Blocked log and error alert; otherwise sets Verified,
    // logs Success, and shows a success alert.
    [HttpPost]
    public async Task<IActionResult> Verify(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            return RecordNotFound("Manage Users", Url.Action(nameof(Index)));
        }

        var refusal = BusinessRules.CheckVerify(user.VerificationStatus, user.FullName);
        if (refusal != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.VerifyUser, null, LogOutcome.Blocked,
                $"Verify refused for '{user.FullName}': {refusal}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(refusal);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        user.VerificationStatus = VerificationStatus.Verified;
        ActivityLogger.Log(_db, CurrentUserId, LogActions.VerifyUser, null, LogOutcome.Success,
            $"Verified '{user.FullName}' ({user.Email}).");
        await _db.SaveChangesAsync(cancellationToken);
        ShowSuccess($"Verified '{user.FullName}'.");
        return RedirectToLocal(returnUrl, nameof(Index));
    }

    // Suspends an account. Refused (self or already Suspended) → Blocked log and error alert; otherwise sets
    // Suspended, logs Success, and shows a success alert.
    [HttpPost]
    public async Task<IActionResult> Suspend(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            return RecordNotFound("Manage Users", Url.Action(nameof(Index)));
        }

        var refusal = BusinessRules.CheckSuspend(CurrentUserId, user.Id, user.VerificationStatus, user.FullName);
        if (refusal != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.SuspendUser, null, LogOutcome.Blocked,
                $"Suspend refused for '{user.FullName}': {refusal}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(refusal);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        user.VerificationStatus = VerificationStatus.Suspended;
        ActivityLogger.Log(_db, CurrentUserId, LogActions.SuspendUser, null, LogOutcome.Success,
            $"Suspended '{user.FullName}' ({user.Email}).");
        await _db.SaveChangesAsync(cancellationToken);
        ShowSuccess($"Suspended '{user.FullName}'.");
        return RedirectToLocal(returnUrl, nameof(Index));
    }

    // Changes an account's role. Refused (self or same role) → Blocked log and error alert; otherwise sets the new
    // role, logs Success naming the old and new role, and shows a success alert.
    [HttpPost]
    public async Task<IActionResult> ChangeRole(ChangeRoleViewModel form, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == form.Id, cancellationToken);
        if (user == null)
        {
            return RecordNotFound("Manage Users", Url.Action(nameof(Index)));
        }

        var oldRole = user.Role;
        var refusal = BusinessRules.CheckChangeRole(CurrentUserId, user.Id, oldRole, form.Role, user.FullName);
        if (refusal != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.ChangeRole, null, LogOutcome.Blocked,
                $"Change role refused for '{user.FullName}': {refusal}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(refusal);
            return RedirectToLocal(form.ReturnUrl, nameof(Index));
        }

        user.Role = form.Role;
        ActivityLogger.Log(_db, CurrentUserId, LogActions.ChangeRole, null, LogOutcome.Success,
            $"Changed role of '{user.FullName}' from {oldRole} to {form.Role}.");
        await _db.SaveChangesAsync(cancellationToken);
        ShowSuccess($"Changed role of '{user.FullName}' to {form.Role}.");
        return RedirectToLocal(form.ReturnUrl, nameof(Index));
    }

    // Deletes an account. Refused (self or dependents exist) → Blocked log and error alert; otherwise removes the
    // account, logs Success, and shows a success alert. Dependents are reported items (N) and filed+reviewed claims (M).
    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            return RecordNotFound("Manage Users", Url.Action(nameof(Index)));
        }

        // Count the account's dependents: reported items, plus claims it filed or reviewed.
        var items = await _db.Items.CountAsync(i => i.ReportedByUserId == id, cancellationToken);
        var claims = await _db.Claims.CountAsync(c => c.ClaimantUserId == id || c.ReviewedByUserId == id, cancellationToken);

        var refusal = BusinessRules.CheckUserDelete(CurrentUserId, user.Id, user.FullName, items, claims);
        if (refusal != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.DeleteUser, null, LogOutcome.Blocked,
                $"Delete refused for '{user.FullName}': {refusal}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(refusal);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        var name = user.FullName;
        var deletedEmail = user.Email;
        _db.Users.Remove(user);
        ActivityLogger.Log(_db, CurrentUserId, LogActions.DeleteUser, null, LogOutcome.Success,
            $"Deleted account '{name}' ({deletedEmail}).");

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A foreign-key constraint stopped the delete; treat it like the dependents refusal and suggest Suspend.
            _db.ChangeTracker.Clear();
            var blocked = $"Cannot delete '{name}' because the account still has related records. Suspend the account instead.";
            ActivityLogger.Log(_db, CurrentUserId, LogActions.DeleteUser, null, LogOutcome.Blocked,
                $"Delete failed for '{name}': the account still has related records.");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(blocked);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        ShowSuccess($"Deleted account '{name}'.");
        return RedirectToLocal(returnUrl, nameof(Index));
    }
}
