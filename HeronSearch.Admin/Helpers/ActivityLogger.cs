using HeronSearch.Admin.Data;
using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.Helpers;

// The outcome word that starts every ActivityLog Result, for example "Success: Deleted item 'Blue umbrella' (#12)".
public enum LogOutcome
{
    // The action finished, and its change is saved together with the log row.
    Success,

    // The action did not finish, for example a sign-in with a wrong email or password.
    Failed,

    // A business rule refused the action, for example archiving an item that still has pending claims.
    Blocked
}

// The Action names stored in ActivityLog.Action and shown as they are in the activity lists.
public static class LogActions
{
    // A sign-in attempt, successful or failed.
    public const string Login = "Login";

    // A sign-out.
    public const string Logout = "Logout";

    // A found item added in Manage Items.
    public const string AddItem = "Add Item";

    // An item's details changed in Manage Items.
    public const string EditItem = "Edit Item";

    // An item marked as returned to its owner.
    public const string MarkReturned = "Mark Returned";

    // An item moved to the archive.
    public const string ArchiveItem = "Archive Item";

    // An item deleted by an Admin.
    public const string DeleteItem = "Delete Item";

    // A claim approved after review.
    public const string ApproveClaim = "Approve Claim";

    // A claim rejected with a reason.
    public const string RejectClaim = "Reject Claim";

    // An account set to Verified.
    public const string VerifyUser = "Verify User";

    // An account set to Suspended.
    public const string SuspendUser = "Suspend User";

    // An account's role changed, for example from Staff to Admin.
    public const string ChangeRole = "Change Role";

    // A new Staff account created by an Admin.
    public const string AddStaff = "Add Staff";

    // An account deleted by an Admin.
    public const string DeleteUser = "Delete User";
}

// Writes ActivityLog rows for sign-ins and admin actions. Log only adds the row to the context; the caller's
// SaveChangesAsync stores it together with the change. Nothing in this class edits or deletes ActivityLog rows.
public static class ActivityLogger
{
    // The longest Result text, in characters; it matches the ActivityLog.Result column (HasMaxLength(500)).
    public const int MaxResultLength = 500;

    // Returns "{outcome}: {description}" cut to at most 500 characters; a null description counts as "".
    // When the cut would split a surrogate pair (for example an emoji), it moves one character earlier,
    // so the text never ends with half a character.
    public static string FormatResult(LogOutcome outcome, string? description)
    {
        var result = $"{outcome}: {description}";
        if (result.Length <= MaxResultLength)
        {
            return result;
        }

        var length = MaxResultLength;
        if (char.IsHighSurrogate(result[length - 1]))
        {
            length--;
        }

        return result.Substring(0, length);
    }

    // Adds one ActivityLog row stamped with the current server time to db, without saving it.
    // userId is the acting user (null when no user matches, as in a failed sign-in); targetItemId is the
    // affected item (null for actions without an existing item, including Delete Item).
    public static void Log(AppDbContext db, int? userId, string action, int? targetItemId, LogOutcome outcome, string description)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            Timestamp = DateTime.Now,
            UserId = userId,
            Action = action,
            TargetItemId = targetItemId,
            Result = FormatResult(outcome, description)
        });
    }
}
