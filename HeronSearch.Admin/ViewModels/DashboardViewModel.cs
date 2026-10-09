using HeronSearch.Admin.Helpers;

namespace HeronSearch.Admin.ViewModels;

// Everything the Dashboard page shows: the five stat-card counts, the two charts, the newest Pending claims,
// and the newest activity logs. DashboardController fills it; Dashboard/Index.cshtml reads it.
public class DashboardViewModel
{
    // The number of items reported as Lost, counting every status (including Archived).
    public int TotalLost { get; set; }

    // The number of items reported as Found, counting every status (including Archived).
    public int TotalFound { get; set; }

    // The number of claims still waiting for review (Status Pending).
    public int PendingClaims { get; set; }

    // The number of items that have been returned to their owner (Status Returned).
    public int ReturnedItems { get; set; }

    // The number of all user accounts (Admin, Staff, and Student).
    public int RegisteredUsers { get; set; }

    // The Lost vs Found line chart across the Six_Month_Window.
    public ChartViewModel MonthlyChart { get; set; } = new();

    // The top-5 locations horizontal bar chart across all items.
    public ChartViewModel LocationChart { get; set; } = new();

    // The 5 most recently filed Pending claims, newest first.
    public List<PendingClaimRow> PendingClaimRows { get; set; } = new();

    // The 10 most recent activity logs, newest first.
    public List<RecentActivityRow> RecentActivity { get; set; } = new();
}

// One row of the Dashboard Pending Claims list: the item, who filed it, when, and the Id for the Review link.
public class PendingClaimRow
{
    // The claim's Id, used to build the "/Claims/Review/{id}" link.
    public int ClaimId { get; set; }

    // The name of the claimed item.
    public string ItemName { get; set; } = "";

    // The full name of the user who filed the claim.
    public string ClaimantName { get; set; } = "";

    // When the claim was filed.
    public DateTime DateFiled { get; set; }
}

// One row of the Dashboard Recent Activity list: when it happened, who did it, the action, and the outcome.
// UserName is null when the log has no user; the view then prints "Unknown user".
public class RecentActivityRow
{
    // When the action happened.
    public DateTime Timestamp { get; set; }

    // The acting user's full name, or null when the log has no user.
    public string? UserName { get; set; }

    // The action name, for example "Login" or "Add Item".
    public string Action { get; set; } = "";

    // The outcome text, starting with Success, Failed, or Blocked.
    public string Result { get; set; } = "";
}
