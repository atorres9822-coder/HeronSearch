using System.Globalization;
using System.Security.Claims;
using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// The student side served at /Student. The StudentOnly policy lets only signed-in Verified Student accounts in;
// Admin and Staff are kept out of these student-only pages. Students only ever see Active items.
[Authorize(Policy = Policies.StudentOnly)]
public class StudentController : Controller
{
    // How many item cards one page of a student listing shows.
    private const int PageSize = 9;

    // How many rows one page of a student table (My Reports, My Claims) shows.
    private const int TablePageSize = 10;

    // The database context, supplied by dependency injection.
    private readonly AppDbContext _db;

    // The hosting environment, used only for its WebRootPath (the wwwroot folder where photos are saved).
    private readonly IWebHostEnvironment _env;

    // Hashes and verifies passwords; used by the Change Password form on the Profile page.
    private readonly IPasswordHasher<User> _passwordHasher;

    // Receives the database context, the hosting environment, and the password hasher from the DI container.
    public StudentController(AppDbContext db, IWebHostEnvironment env, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _env = env;
        _passwordHasher = passwordHasher;
    }

    // The signed-in student's Id from the NameIdentifier claim; the StudentOnly policy guarantees it is present and numeric.
    private int CurrentUserId
    {
        get
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                throw new InvalidOperationException("The signed-in user has no numeric Id claim.");
            }

            return id;
        }
    }

    // Shows the student dashboard with real counts (My Reports, My Claims, Unread Notifications) and the student's
    // newest activity (their recent reports and reviewed claims), newest first.
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        // The three stat counts, each scoped to the signed-in student only.
        var myReports = await _db.Items.AsNoTracking().CountAsync(i => i.ReportedByUserId == userId, cancellationToken);
        var myClaims = await _db.Claims.AsNoTracking().CountAsync(c => c.ClaimantUserId == userId, cancellationToken);

        // Unread notifications: claims of this student reviewed (Approved or Rejected) within the last 7 days.
        var cutoff = DateTime.Now.AddDays(-7);
        var unread = await _db.Claims.AsNoTracking().CountAsync(c =>
            c.ClaimantUserId == userId
            && c.Status != ClaimStatus.Pending
            && c.ReviewedAt != null
            && c.ReviewedAt >= cutoff, cancellationToken);

        // The student's five newest reports as activity rows.
        var reportRows = await _db.Items.AsNoTracking()
            .Where(i => i.ReportedByUserId == userId)
            .OrderByDescending(i => i.DateReported).ThenByDescending(i => i.Id)
            .Take(5)
            .Select(i => new { i.Name, i.DateReported })
            .ToListAsync(cancellationToken);

        // The student's five newest reviewed claims as activity rows.
        var claimRows = await _db.Claims.AsNoTracking()
            .Where(c => c.ClaimantUserId == userId && c.Status != ClaimStatus.Pending && c.ReviewedAt != null)
            .OrderByDescending(c => c.ReviewedAt)
            .Take(5)
            .Select(c => new { ItemName = c.Item!.Name, c.Status, c.ReviewedAt })
            .ToListAsync(cancellationToken);

        // Merge the two activity sources, newest first, and keep the five most recent overall.
        var activity = reportRows
            .Select(r => new StudentActivityRow
            {
                Icon = "bi-box-arrow-in-down",
                Message = $"You reported '{r.Name}'.",
                When = r.DateReported
            })
            .Concat(claimRows.Select(c => new StudentActivityRow
            {
                Icon = c.Status == ClaimStatus.Approved ? "bi-check-circle" : "bi-x-circle",
                Message = $"Your claim for '{c.ItemName}' was {c.Status.ToString().ToLowerInvariant()}.",
                When = c.ReviewedAt!.Value
            }))
            .OrderByDescending(a => a.When)
            .Take(5)
            .ToList();

        var model = new StudentDashboardViewModel
        {
            MyReportsCount = myReports,
            MyClaimsCount = myClaims,
            UnreadNotifications = unread,
            RecentActivity = activity
        };

        return View(model);
    }

    // Shows the Lost Items listing: Active Lost items for the search term, Category filter, and page number.
    [HttpGet]
    public async Task<IActionResult> LostItems(string? search, string? category, int page, CancellationToken cancellationToken)
    {
        var model = await BuildListAsync(ItemType.Lost, search, category, page, nameof(LostItems), cancellationToken);
        return View(model);
    }

    // Shows the Found Items listing: Active Found items for the search term, Category filter, and page number.
    [HttpGet]
    public async Task<IActionResult> FoundItems(string? search, string? category, int page, CancellationToken cancellationToken)
    {
        var model = await BuildListAsync(ItemType.Found, search, category, page, nameof(FoundItems), cancellationToken);
        return View(model);
    }

    // Shows the Search page: one search box over Active items of both types, with a Category filter and paging.
    [HttpGet]
    public async Task<IActionResult> Search(string? search, string? category, int page, CancellationToken cancellationToken)
    {
        var model = await BuildListAsync(null, search, category, page, nameof(Search), cancellationToken);
        return View(model);
    }

    // Shows a read-only Details page for one Active item; a missing or non-Active item returns Not Found.
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Where(i => i.Id == id && i.Status == ItemStatus.Active)
            .Select(i => new StudentItemDetailsViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Location = i.Location,
                Description = i.Description,
                ImagePath = i.ImagePath,
                Type = i.Type,
                DateReported = i.DateReported,
                ReportedByUserId = i.ReportedByUserId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    // Shows the empty Report Lost Item form, pre-filling the date with today.
    [HttpGet]
    public IActionResult ReportLost()
    {
        var model = new ItemFormViewModel { DateFound = DateTime.Today };
        return View(model);
    }

    // Handles the Report Lost Item form: validates it, saves an optional photo, stores a new Active Lost item owned by
    // the student, logs "Add Item", and returns to the Lost Items listing with a success alert.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportLost(ItemFormViewModel model)
    {
        return await SaveReportAsync(model, ItemType.Lost, nameof(ReportLost), nameof(LostItems));
    }

    // Shows the empty Report Found Item form, pre-filling the date with today.
    [HttpGet]
    public IActionResult ReportFound()
    {
        var model = new ItemFormViewModel { DateFound = DateTime.Today };
        return View(model);
    }

    // Handles the Report Found Item form: validates it, saves an optional photo, stores a new Active Found item owned by
    // the student, logs "Add Item", and returns to the Found Items listing with a success alert.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportFound(ItemFormViewModel model)
    {
        return await SaveReportAsync(model, ItemType.Found, nameof(ReportFound), nameof(FoundItems));
    }

    // Shows the Claim this item form for one Active Found item; a missing, non-Active, or non-Found item returns Not Found.
    // A student cannot claim an item they reported, so that case redirects to Details with an error alert.
    [HttpGet]
    public async Task<IActionResult> Claim(int id, CancellationToken cancellationToken)
    {
        // Look up the item's reporter first so a student is blocked from even opening the form for their own report.
        var reportedBy = await _db.Items.AsNoTracking()
            .Where(i => i.Id == id && i.Status == ItemStatus.Active && i.Type == ItemType.Found)
            .Select(i => (int?)i.ReportedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (reportedBy == CurrentUserId)
        {
            TempData["Error"] = "You can't claim an item you reported.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = await LoadClaimFormAsync(id, cancellationToken);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    // Handles the Claim this item form: re-checks the item is still claimable, blocks claiming an item the student
    // reported and a second Pending claim by the same student, creates a Pending Claim, logs it, and redirects to MyClaims.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(ClaimFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        // Re-load the item fresh from the Id in the form; it must still be an Active Found item to be claimable.
        var item = await _db.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == model.ItemId && i.Status == ItemStatus.Active && i.Type == ItemType.Found,
                cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        // A student cannot claim an item they reported themselves; block it before anything else and show an alert.
        if (item.ReportedByUserId == userId)
        {
            TempData["Error"] = "You can't claim an item you reported.";
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }

        // Any validation error (the proof text) redisplays the form with the item details re-filled.
        if (!ModelState.IsValid)
        {
            return View(await FillClaimFormAsync(model, item, cancellationToken));
        }

        // A student cannot file a second Pending claim on the same item.
        var alreadyPending = await _db.Claims.AsNoTracking().AnyAsync(c =>
            c.ItemId == item.Id && c.ClaimantUserId == userId && c.Status == ClaimStatus.Pending, cancellationToken);
        if (alreadyPending)
        {
            ModelState.AddModelError(string.Empty, "You already have a pending claim for this item.");
            return View(await FillClaimFormAsync(model, item, cancellationToken));
        }

        // Create the Pending claim owned by the signed-in student, filed now.
        var claim = new HeronSearch.Admin.Models.Claim
        {
            ItemId = item.Id,
            ClaimantUserId = userId,
            ProofDescription = model.ProofDescription.Trim(),
            Status = ClaimStatus.Pending,
            DateFiled = DateTime.Now
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(cancellationToken);

        // Log the claim with the existing "Add Item" action name (no new LogActions constant is required here).
        ActivityLogger.Log(_db, userId, LogActions.AddItem, item.Id, LogOutcome.Success,
            $"Filed claim #{claim.Id} for '{item.Name}' (#{item.Id}).");
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Your claim for '{item.Name}' was filed.";
        return RedirectToAction(nameof(MyClaims));
    }

    // Shows My Reports: the signed-in student's own items (all statuses), newest first, 10 per page with the pager.
    [HttpGet]
    public async Task<IActionResult> MyReports(int page, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var query = _db.Items.AsNoTracking().Where(i => i.ReportedByUserId == userId)
            .OrderByDescending(i => i.DateReported).ThenByDescending(i => i.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, TablePageSize, "reports");

        var rows = await query
            .Skip(pager.Skip)
            .Take(TablePageSize)
            .Select(i => new StudentReportRowViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                ImagePath = i.ImagePath,
                Type = i.Type,
                Status = i.Status,
                DateReported = i.DateReported
            })
            .ToListAsync(cancellationToken);

        return View(new StudentReportListViewModel { Rows = rows, Pager = pager });
    }

    // Shows My Claims: the signed-in student's own claims for the chosen tab (Pending, Approved, or Rejected),
    // newest first, 10 per page, with the item name, filing date, status badge, and rejection reason when Rejected.
    [HttpGet]
    public async Task<IActionResult> MyClaims(string? tab, int page, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        // The tab defaults to Pending; an unknown value also falls back to Pending.
        var tabName = FilterValues.MatchAllowedValue(tab, Enum.GetNames<ClaimStatus>()) ?? nameof(ClaimStatus.Pending);
        var tabStatus = Enum.Parse<ClaimStatus>(tabName);

        // The three tab counts, each scoped to this student only.
        var mine = _db.Claims.AsNoTracking().Where(c => c.ClaimantUserId == userId);
        var pendingCount = await mine.CountAsync(c => c.Status == ClaimStatus.Pending, cancellationToken);
        var approvedCount = await mine.CountAsync(c => c.Status == ClaimStatus.Approved, cancellationToken);
        var rejectedCount = await mine.CountAsync(c => c.Status == ClaimStatus.Rejected, cancellationToken);

        // Only this student's claims on the active tab; Pending by filing date, reviewed by review date, newest first.
        var query = _db.Claims.AsNoTracking().Where(c => c.ClaimantUserId == userId && c.Status == tabStatus);
        query = tabStatus == ClaimStatus.Pending
            ? query.OrderByDescending(c => c.DateFiled).ThenByDescending(c => c.Id)
            : query.OrderByDescending(c => c.ReviewedAt).ThenByDescending(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, TablePageSize, "claims");

        var rows = await query
            .Skip(pager.Skip)
            .Take(TablePageSize)
            .Select(c => new StudentClaimRowViewModel
            {
                Id = c.Id,
                ItemName = c.Item!.Name,
                ItemId = c.ItemId,
                DateFiled = c.DateFiled,
                Status = c.Status,
                RejectionReason = c.RejectionReason
            })
            .ToListAsync(cancellationToken);

        pager.AddRouteValue("tab", tabName);

        var model = new StudentClaimListViewModel
        {
            Tab = tabName,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            Rows = rows,
            Pager = pager
        };

        return View(model);
    }

    // Shows Notifications: the student's reviewed claims (Approved or Rejected), newest first by ReviewedAt, as
    // friendly messages. Nothing is stored in the database; the list is derived from the Claims table at read time.
    [HttpGet]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        var rows = await _db.Claims.AsNoTracking()
            .Where(c => c.ClaimantUserId == userId && c.Status != ClaimStatus.Pending && c.ReviewedAt != null)
            .OrderByDescending(c => c.ReviewedAt)
            .Select(c => new StudentNotificationViewModel
            {
                Status = c.Status,
                Message = $"Your claim for '{c.Item!.Name}' was "
                    + (c.Status == ClaimStatus.Approved ? "approved." : "rejected."),
                ReviewedAt = c.ReviewedAt!.Value
            })
            .ToListAsync(cancellationToken);

        return View(new StudentNotificationsViewModel { Rows = rows });
    }

    // Shows the Profile page: read-only Email with editable Full Name and Student ID, plus the Change Password form.
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var model = await LoadProfilePageAsync(cancellationToken);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    // Handles the Profile form: validates it and saves the new Full Name and Student ID (Email is never changed).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(StudentProfileViewModel form, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        // Invalid input redisplays the whole page with the profile form's errors and an empty password form.
        if (!ModelState.IsValid)
        {
            return View(new StudentProfilePageViewModel
            {
                Profile = new StudentProfileViewModel { Email = user.Email, FullName = form.FullName, StudentId = form.StudentId }
            });
        }

        user.FullName = form.FullName.Trim();
        user.StudentId = string.IsNullOrWhiteSpace(form.StudentId) ? null : form.StudentId.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }

    // Handles the Change Password form: verifies the current password with the hasher, checks the new password rule,
    // stores the new hash, and returns to Profile. Any failure redisplays Profile with the password form's errors.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel form, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        // Verify the current password before anything else; a wrong one is a field error on the password form.
        if (ModelState.IsValid)
        {
            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, form.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError($"{nameof(StudentProfilePageViewModel.Password)}.{nameof(ChangePasswordViewModel.CurrentPassword)}",
                    "Your current password is incorrect.");
            }
        }

        // Any validation error redisplays the whole page with the current profile values and the password form reopened.
        if (!ModelState.IsValid)
        {
            var page = await LoadProfilePageAsync(cancellationToken);
            if (page == null)
            {
                return NotFound();
            }

            page.ShowPasswordErrors = true;
            return View(nameof(Profile), page);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, form.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Your password was changed.";
        return RedirectToAction(nameof(Profile));
    }

    // Loads one Active Found item into a Claim form view model; null when the item is missing, not Active, or not Found.
    private async Task<ClaimFormViewModel?> LoadClaimFormAsync(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.Status == ItemStatus.Active && i.Type == ItemType.Found,
                cancellationToken);
        if (item == null)
        {
            return null;
        }

        return await FillClaimFormAsync(new ClaimFormViewModel(), item, cancellationToken);
    }

    // Fills a Claim form's display fields (item name, category, photo, and the student's contact email) from the item.
    private async Task<ClaimFormViewModel> FillClaimFormAsync(ClaimFormViewModel model, Item item, CancellationToken cancellationToken)
    {
        var email = await _db.Users.AsNoTracking()
            .Where(u => u.Id == CurrentUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        model.ItemId = item.Id;
        model.ItemName = item.Name;
        model.ItemCategory = item.Category;
        model.ItemImagePath = item.ImagePath;
        model.ContactEmail = email ?? "";
        return model;
    }

    // Loads the Profile page view model (profile form filled, empty password form); null when the account is missing.
    private async Task<StudentProfilePageViewModel?> LoadProfilePageAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.Users.AsNoTracking()
            .Where(u => u.Id == CurrentUserId)
            .Select(u => new StudentProfileViewModel
            {
                Email = u.Email,
                FullName = u.FullName,
                StudentId = u.StudentId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (profile == null)
        {
            return null;
        }

        return new StudentProfilePageViewModel { Profile = profile };
    }

    // Builds a student listing view model: Active items (optionally of one Type) matching the search term and Category,
    // sorted newest first, read-only, 9 per page with a pager that keeps the filters.
    private async Task<StudentItemListViewModel> BuildListAsync(
        ItemType? type, string? search, string? category, int page, string action, CancellationToken cancellationToken)
    {
        // Clean the query-string values: a blank search becomes null, and an unknown Category falls back to null.
        var searchTerm = FilterValues.NormalizeSearch(search);
        var categoryName = FilterValues.MatchAllowedValue(category, ItemLists.Categories);

        // Students only ever see Active items; narrow by Type when the page is one list, by search, and by Category.
        var query = _db.Items.AsNoTracking().Where(i => i.Status == ItemStatus.Active);

        if (type != null)
        {
            query = query.Where(i => i.Type == type);
        }

        // Case-insensitive search over Name, Description, and Location, always passed as a SQL parameter.
        if (searchTerm != null)
        {
            var lowered = searchTerm.ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(lowered)
                || (i.Description != null && i.Description.ToLower().Contains(lowered))
                || i.Location.ToLower().Contains(lowered));
        }

        if (categoryName != null)
        {
            query = query.Where(i => i.Category == categoryName);
        }

        // Newest first, Id descending as a stable tie-breaker so cards do not shift between pages.
        var ordered = query.OrderByDescending(i => i.DateReported).ThenByDescending(i => i.Id);

        // Count the matches, build the pager (which clamps the page number), then read just that page as cards.
        var totalCount = await ordered.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, PageSize, "items");

        var cards = await ordered
            .Skip(pager.Skip)
            .Take(PageSize)
            .Select(i => new StudentItemCardViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Location = i.Location,
                Description = i.Description,
                ImagePath = i.ImagePath,
                Type = i.Type,
                DateReported = i.DateReported
            })
            .ToListAsync(cancellationToken);

        // Keep the search term and Category on every page link and point them back at this list's action.
        pager.AddRouteValue("search", searchTerm);
        pager.AddRouteValue("category", categoryName);

        return new StudentItemListViewModel
        {
            Action = action,
            Type = type,
            Cards = cards,
            Search = searchTerm,
            Category = categoryName,
            Pager = pager
        };
    }

    // Validates and stores a new report of the given Type: re-checks the dropdowns, saves an optional photo, creates an
    // Active item owned by the student dated by the submitted date, logs "Add Item", and redirects to its listing.
    private async Task<IActionResult> SaveReportAsync(ItemFormViewModel model, ItemType type, string formAction, string listAction)
    {
        // Re-check the dropdown choices against the fixed lists in case a value was tampered with past client validation.
        if (!ItemLists.Categories.Contains(model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "Choose a category from the list.");
        }

        if (!ItemLists.Locations.Contains(model.Location))
        {
            ModelState.AddModelError(nameof(model.Location), "Choose a location from the list.");
        }

        // Any validation error (annotations or the list checks above) redisplays the form with the entered values.
        if (!ModelState.IsValid)
        {
            return View(formAction, model);
        }

        // Save the uploaded photo first; a save failure keeps the user on the form with a field message.
        string? imagePath = null;
        if (model.Photo != null)
        {
            try
            {
                imagePath = await ImageUploadValidator.SaveAsync(model.Photo, _env.WebRootPath);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.Photo), ImageUploadValidator.SaveFailedMessage);
                return View(formAction, model);
            }
        }

        // Build the new report: the given Type, starting Active, reported now by the signed-in student. DateReported
        // keeps the submitted date but takes the current time of day, never later than now.
        var now = DateTime.Now;
        var item = new Item
        {
            Name = model.Name,
            Category = model.Category,
            Location = model.Location,
            Description = model.Description,
            ImagePath = imagePath,
            Type = type,
            Status = ItemStatus.Active,
            DateReported = BusinessRules.CombineDateWithTime(model.DateFound, now.TimeOfDay, now),
            ReportedByUserId = CurrentUserId
        };

        _db.Items.Add(item);

        // Save once so the database assigns the Id, then log "Add Item" with that Id and save the log row.
        await _db.SaveChangesAsync();
        ActivityLogger.Log(_db, CurrentUserId, LogActions.AddItem, item.Id, LogOutcome.Success,
            $"Added item '{item.Name}' (#{item.Id}).");
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Reported '{item.Name}'.";
        return RedirectToAction(listAction);
    }
}
