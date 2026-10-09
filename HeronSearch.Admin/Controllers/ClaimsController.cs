using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// The Manage Claims pages. Inherits BackOfficeController, so only signed-in Admin and Staff users reach it.
// Index lists claims by tab; Review shows one claim and handles the Approve and Reject decisions.
public class ClaimsController : BackOfficeController
{
    // How many claims one page of the list shows.
    private const int PageSize = 10;

    // The database context, supplied by dependency injection.
    private readonly AppDbContext _db;

    // Receives the database context from the DI container.
    public ClaimsController(AppDbContext db)
    {
        _db = db;
    }

    // Shows the claims list for the chosen tab (Pending, Approved, or Rejected). Pending claims sort by
    // DateFiled (newest first); reviewed claims sort by ReviewedAt (newest first), then Id. 10 per page.
    public async Task<IActionResult> Index(string? tab, int page, CancellationToken cancellationToken)
    {
        // The tab defaults to Pending; an unknown value also falls back to Pending.
        var tabName = FilterValues.MatchAllowedValue(tab, Enum.GetNames<ClaimStatus>()) ?? nameof(ClaimStatus.Pending);
        var tabStatus = Enum.Parse<ClaimStatus>(tabName);

        // The three badge counts across all claims.
        var pendingCount = await _db.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Pending, cancellationToken);
        var approvedCount = await _db.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Approved, cancellationToken);
        var rejectedCount = await _db.Claims.AsNoTracking().CountAsync(c => c.Status == ClaimStatus.Rejected, cancellationToken);

        // Only the claims on the active tab, sorted by the tab's rule.
        var query = _db.Claims.AsNoTracking().Where(c => c.Status == tabStatus);
        query = tabStatus == ClaimStatus.Pending
            ? query.OrderByDescending(c => c.DateFiled).ThenByDescending(c => c.Id)
            : query.OrderByDescending(c => c.ReviewedAt).ThenByDescending(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, PageSize, "claims");

        var rows = await query
            .Skip(pager.Skip)
            .Take(PageSize)
            .Select(c => new ClaimRowViewModel
            {
                Id = c.Id,
                ItemName = c.Item!.Name,
                ItemCategory = c.Item.Category,
                ItemImagePath = c.Item.ImagePath,
                ClaimantName = c.ClaimantUser!.FullName,
                ClaimantStudentId = c.ClaimantUser.StudentId,
                DateFiled = c.DateFiled,
                Status = c.Status
            })
            .ToListAsync(cancellationToken);

        // Keep the active tab on every page link.
        pager.AddRouteValue("tab", tabName);

        var model = new ClaimListViewModel
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

    // Shows one claim with its item, claimant, and reviewer. A missing Id returns the Not Found page.
    // The page is read-only unless the claim is still Pending.
    [HttpGet]
    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var model = await LoadReviewAsync(id, cancellationToken);
        if (model == null)
        {
            return RecordNotFound("Manage Claims", Url.Action(nameof(Index)));
        }

        return View(model);
    }

    // Approves a claim after the four checks. Refused (CheckApprove) → Blocked log and an error alert, back to
    // Review. Otherwise ApplyApproval auto-rejects the item's other Pending claims and sets the item Claimed.
    [HttpPost]
    public async Task<IActionResult> Approve(ApproveClaimViewModel form, CancellationToken cancellationToken)
    {
        // Load the claim with the item and claimant (tracked, so the status changes are saved).
        var claim = await _db.Claims
            .Include(c => c.Item)
            .Include(c => c.ClaimantUser)
            .FirstOrDefaultAsync(c => c.Id == form.Id, cancellationToken);
        if (claim == null)
        {
            return RecordNotFound("Manage Claims", Url.Action(nameof(Index)));
        }

        // The reviewer name used in the already-reviewed message, or a safe fallback when none is set.
        var reviewerName = await ReviewerNameAsync(claim.ReviewedByUserId, cancellationToken);
        var refusal = BusinessRules.CheckApprove(claim.Status, reviewerName,
            claim.ClaimantUser!.VerificationStatus, claim.Item!.Status, form.AllChecked);
        if (refusal != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.ApproveClaim, claim.ItemId, LogOutcome.Blocked,
                $"Approve refused for claim #{claim.Id}: {refusal}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(refusal);
            return RedirectToAction(nameof(Review), new { id = claim.Id });
        }

        // The item's other Pending claims are auto-rejected inside ApplyApproval.
        var otherPending = await _db.Claims
            .Where(c => c.ItemId == claim.ItemId && c.Id != claim.Id && c.Status == ClaimStatus.Pending)
            .ToListAsync(cancellationToken);
        var now = DateTime.Now;
        var autoRejected = BusinessRules.ApplyApproval(claim, claim.Item, otherPending, CurrentUserId, now);

        ActivityLogger.Log(_db, CurrentUserId, LogActions.ApproveClaim, claim.ItemId, LogOutcome.Success,
            $"Approved claim #{claim.Id} for '{claim.Item.Name}' and auto-rejected {autoRejected} other claim(s).");

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another reviewer decided this claim first; name them and send the user back to the list.
            var currentReviewer = await ReviewerNameAsync(null, cancellationToken, claim.Id);
            ShowError($"This claim was already reviewed by {currentReviewer}.");
            return RedirectToAction(nameof(Index), new { tab = nameof(ClaimStatus.Pending) });
        }

        ShowSuccess($"Claim #{claim.Id} for '{claim.Item.Name}' was approved.");
        return RedirectToAction(nameof(Index), new { tab = nameof(ClaimStatus.Pending) });
    }

    // Rejects a claim with a reason. An invalid reason redisplays Review with the modal reopened; a CheckReject
    // refusal logs Blocked and shows an error alert; otherwise ApplyRejection records the rejection (item unchanged).
    [HttpPost]
    public async Task<IActionResult> Reject(RejectClaimViewModel form, CancellationToken cancellationToken)
    {
        var claim = await _db.Claims
            .Include(c => c.Item)
            .FirstOrDefaultAsync(c => c.Id == form.Id, cancellationToken);
        if (claim == null)
        {
            return RecordNotFound("Manage Claims", Url.Action(nameof(Index)));
        }

        // An invalid reason (trimmed length outside 10-500) redisplays the Review page with the modal reopened.
        if (!BusinessRules.IsValidRejectReason(form.Reason))
        {
            ModelState.AddModelError($"{nameof(ClaimReviewViewModel.Reject)}.{nameof(RejectClaimViewModel.Reason)}",
                BusinessRules.RejectReasonMessage);
            var model = await LoadReviewAsync(claim.Id, cancellationToken);
            if (model == null)
            {
                return RecordNotFound("Manage Claims", Url.Action(nameof(Index)));
            }

            model.Reject = form;
            model.OpenRejectModal = true;
            return View(nameof(Review), model);
        }

        var reviewerName = await ReviewerNameAsync(claim.ReviewedByUserId, cancellationToken);
        var refusal = BusinessRules.CheckReject(claim.Status, reviewerName);
        if (refusal != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.RejectClaim, claim.ItemId, LogOutcome.Blocked,
                $"Reject refused for claim #{claim.Id}: {refusal}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(refusal);
            return RedirectToAction(nameof(Review), new { id = claim.Id });
        }

        BusinessRules.ApplyRejection(claim, form.Reason, CurrentUserId, DateTime.Now);
        ActivityLogger.Log(_db, CurrentUserId, LogActions.RejectClaim, claim.ItemId, LogOutcome.Success,
            $"Rejected claim #{claim.Id} for '{claim.Item!.Name}'.");

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentReviewer = await ReviewerNameAsync(null, cancellationToken, claim.Id);
            ShowError($"This claim was already reviewed by {currentReviewer}.");
            return RedirectToAction(nameof(Index), new { tab = nameof(ClaimStatus.Pending) });
        }

        ShowSuccess($"Claim #{claim.Id} for '{claim.Item!.Name}' was rejected.");
        return RedirectToAction(nameof(Index), new { tab = nameof(ClaimStatus.Pending) });
    }

    // Loads one claim into a review view model with its item, reporter, claimant, and reviewer; null when the Id is unknown.
    private async Task<ClaimReviewViewModel?> LoadReviewAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Claims
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClaimReviewViewModel
            {
                Id = c.Id,
                Status = c.Status,
                ProofDescription = c.ProofDescription,
                DateFiled = c.DateFiled,
                ItemName = c.Item!.Name,
                ItemCategory = c.Item.Category,
                ItemLocation = c.Item.Location,
                ItemType = c.Item.Type,
                ItemStatus = c.Item.Status,
                ItemImagePath = c.Item.ImagePath,
                ReporterName = c.Item.ReportedByUser!.FullName,
                ClaimantName = c.ClaimantUser!.FullName,
                ClaimantEmail = c.ClaimantUser.Email,
                ClaimantStudentId = c.ClaimantUser.StudentId,
                ClaimantStatus = c.ClaimantUser.VerificationStatus,
                ReviewerName = c.ReviewedByUser != null ? c.ReviewedByUser.FullName : null,
                ReviewedAt = c.ReviewedAt,
                RejectionReason = c.RejectionReason
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Returns the reviewer's full name for a claim's reviewer Id, or (when reviewerId is null and claimId is given)
    // the reviewer currently recorded on that claim. Falls back to "another reviewer" when no name is found.
    private async Task<string> ReviewerNameAsync(int? reviewerId, CancellationToken cancellationToken, int? claimId = null)
    {
        if (reviewerId == null && claimId != null)
        {
            reviewerId = await _db.Claims.AsNoTracking()
                .Where(c => c.Id == claimId)
                .Select(c => c.ReviewedByUserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (reviewerId == null)
        {
            return "another reviewer";
        }

        var name = await _db.Users.AsNoTracking()
            .Where(u => u.Id == reviewerId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
        return name ?? "another reviewer";
    }
}
