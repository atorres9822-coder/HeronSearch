using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.Helpers;

// The business rules for items and accounts as small pure functions, shared by controllers and views.
// Each Check method returns null when the action is allowed, or the exact error text to show; each Can method agrees with its Check method.
public static class BusinessRules
{
    // The WHATWG HTML email pattern (the one jQuery Validation uses). It uses ASCII classes only, so .NET and JavaScript agree.
    public const string EmailPattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

    // Shown when an email address does not match EmailPattern.
    public const string EmailMessage = "Enter a valid email address.";

    // The Password_Rule: 8 to 100 characters with at least one letter (A-Z or a-z) and at least one digit (0-9).
    // [\s\S] means any character, line breaks included, so .NET and JavaScript count every character the same way.
    public const string PasswordPattern = @"^(?=[\s\S]*[A-Za-z])(?=[\s\S]*[0-9])[\s\S]{8,100}$";

    // Shown when a password does not match PasswordPattern.
    public const string PasswordMessage = "Password must be 8 to 100 characters and include a letter and a number.";

    // Shown when Mark as Returned or Archive is refused because the item still has Pending claims.
    public const string PendingClaimsMessage = "Resolve the pending claim(s) for this item in Manage Claims first.";

    // Returns the email trimmed and in lowercase (invariant culture), the form that is stored and compared; null counts as "".
    public static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    // Returns the date part of date plus timeOfDay, or now when that moment would be later than now.
    // Add Found Item passes the current time of day; Edit passes the item's original time of day.
    public static DateTime CombineDateWithTime(DateTime date, TimeSpan timeOfDay, DateTime now)
    {
        var combined = date.Date + timeOfDay;
        return combined > now ? now : combined;
    }

    // Returns true when an item with this status may be marked as returned: Active or Claimed.
    public static bool CanMarkReturned(ItemStatus status)
    {
        return status == ItemStatus.Active || status == ItemStatus.Claimed;
    }

    // Returns null when Mark as Returned is allowed, or the error text; pending is the item's number of Pending claims.
    // The status is checked first, then the Pending claims.
    public static string? CheckMarkReturned(ItemStatus status, int pending)
    {
        if (!CanMarkReturned(status))
        {
            return $"This item is {status}, so it cannot be marked as returned.";
        }

        if (pending > 0)
        {
            return PendingClaimsMessage;
        }

        return null;
    }

    // Returns true when an item with this status may be archived: Active, Claimed, or Returned.
    public static bool CanArchive(ItemStatus status)
    {
        return status == ItemStatus.Active || status == ItemStatus.Claimed || status == ItemStatus.Returned;
    }

    // Returns null when Archive is allowed, or the error text; pending is the item's number of Pending claims.
    // The status is checked first, then the Pending claims.
    public static string? CheckArchive(ItemStatus status, int pending)
    {
        if (!CanArchive(status))
        {
            return $"This item is {status}, so it cannot be archived.";
        }

        if (pending > 0)
        {
            return PendingClaimsMessage;
        }

        return null;
    }

    // Returns null when the item has no claims of any status and may be deleted; otherwise the error text that suggests Archive.
    public static string? CheckItemDelete(string name, int claimCount)
    {
        if (claimCount > 0)
        {
            return $"Cannot delete '{name}' because the item has {claimCount} claim(s). Archive the item instead.";
        }

        return null;
    }

    // The four verification checklist labels an Admin or Staff member confirms before approving a claim (19.2).
    public static readonly IReadOnlyList<string> ClaimChecklistLabels = new[]
    {
        "The claimant's identity matches the student ID on record.",
        "The proof matches the item's description.",
        "The claimant named identifying details that are not in the public listing.",
        "The date and location of loss match the item record."
    };

    // Shown when every other Pending claim on an item is auto-rejected because one claim was approved.
    public const string AutoRejectReason = "Another claim for this item was approved.";

    // Returns null when the claim may be approved, or the first failing check's message in the fixed order
    // (I13): already reviewed, claimant suspended, item not Active, checklist incomplete. reviewerName names
    // the reviewer in the already-reviewed message; allChecks is true only when all four boxes are ticked.
    public static string? CheckApprove(ClaimStatus claimStatus, string reviewerName,
        VerificationStatus claimantStatus, ItemStatus itemStatus, bool allChecks)
    {
        if (claimStatus != ClaimStatus.Pending)
        {
            return $"This claim was already reviewed by {reviewerName}.";
        }

        if (claimantStatus == VerificationStatus.Suspended)
        {
            return "The claimant's account is suspended.";
        }

        if (itemStatus != ItemStatus.Active)
        {
            return $"The item is {itemStatus}, so this claim cannot be approved.";
        }

        if (!allChecks)
        {
            return "Complete every verification check before approving.";
        }

        return null;
    }

    // Applies an approval: sets the claim to Approved with the reviewer and time, auto-rejects every other
    // Pending claim on the same item (same reviewer and time, with AutoRejectReason), and sets the item to
    // Claimed. Returns how many other claims were auto-rejected. The caller passes the item's other Pending claims.
    public static int ApplyApproval(Claim claim, Item item, IEnumerable<Claim> otherPending,
        int reviewerId, DateTime now)
    {
        claim.Status = ClaimStatus.Approved;
        claim.ReviewedByUserId = reviewerId;
        claim.ReviewedAt = now;

        var autoRejected = 0;
        foreach (var other in otherPending)
        {
            if (other.Id == claim.Id || other.Status != ClaimStatus.Pending)
            {
                continue;
            }

            other.Status = ClaimStatus.Rejected;
            other.RejectionReason = AutoRejectReason;
            other.ReviewedByUserId = reviewerId;
            other.ReviewedAt = now;
            autoRejected++;
        }

        item.Status = ItemStatus.Claimed;
        return autoRejected;
    }

    // Shown when a rejection reason is not between 10 and 500 characters after trimming.
    public const string RejectReasonMessage = "Enter a reason of 10 to 500 characters.";

    // Returns true when the trimmed reason is 10 to 500 characters; the server trims before checking (I11).
    public static bool IsValidRejectReason(string? reason)
    {
        var length = (reason ?? string.Empty).Trim().Length;
        return length >= 10 && length <= 500;
    }

    // Returns null when the claim may be rejected, or the already-reviewed message (naming the reviewer).
    public static string? CheckReject(ClaimStatus claimStatus, string reviewerName)
    {
        if (claimStatus != ClaimStatus.Pending)
        {
            return $"This claim was already reviewed by {reviewerName}.";
        }

        return null;
    }

    // Applies a rejection: sets the claim to Rejected with the trimmed reason, reviewer, and time. The item is unchanged.
    public static void ApplyRejection(Claim claim, string reason, int reviewerId, DateTime now)
    {
        claim.Status = ClaimStatus.Rejected;
        claim.RejectionReason = reason.Trim();
        claim.ReviewedByUserId = reviewerId;
        claim.ReviewedAt = now;
    }

    // Shown when an Admin tries to change or delete their own account; the self check runs before every other user rule.
    public const string SelfActionMessage = "You cannot change or delete your own account.";

    // Returns true when an account with this status may be verified: Pending or Suspended.
    public static bool CanVerify(VerificationStatus status)
    {
        return status == VerificationStatus.Pending || status == VerificationStatus.Suspended;
    }

    // Returns null when the account may be verified, or the "already Verified" message naming the account.
    public static string? CheckVerify(VerificationStatus status, string name)
    {
        if (!CanVerify(status))
        {
            return $"'{name}' is already Verified.";
        }

        return null;
    }

    // Returns true when an account with this status may be suspended: Pending or Verified.
    public static bool CanSuspend(VerificationStatus status)
    {
        return status == VerificationStatus.Pending || status == VerificationStatus.Verified;
    }

    // Returns null when the account may be suspended, or the first failing check: the self check, then "already Suspended".
    public static string? CheckSuspend(int actingId, int targetId, VerificationStatus status, string name)
    {
        if (actingId == targetId)
        {
            return SelfActionMessage;
        }

        if (!CanSuspend(status))
        {
            return $"'{name}' is already Suspended.";
        }

        return null;
    }

    // Returns null when the role change is allowed, or the first failing check: the self check, then "already has the role".
    public static string? CheckChangeRole(int actingId, int targetId, UserRole current, UserRole requested, string name)
    {
        if (actingId == targetId)
        {
            return SelfActionMessage;
        }

        if (requested == current)
        {
            return $"'{name}' already has the {current} role.";
        }

        return null;
    }

    // Returns null when the account may be deleted, or the first failing check: the self check, then the
    // dependents message naming the reported-item count (items) and the filed-plus-reviewed claim count (claims).
    public static string? CheckUserDelete(int actingId, int targetId, string name, int items, int claims)
    {
        if (actingId == targetId)
        {
            return SelfActionMessage;
        }

        if (items + claims > 0)
        {
            return $"Cannot delete '{name}' because the account has {items} reported item(s) and {claims} claim(s). Suspend the account instead.";
        }

        return null;
    }
}
