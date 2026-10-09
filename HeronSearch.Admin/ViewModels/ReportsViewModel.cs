using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// Everything the Reports / History page shows: the active tab, the date range and any range error, the Overview
// counts and charts, and the System Logs rows. ReportsController.Index fills it; Reports/Index.cshtml reads it.
public class ReportsViewModel
{
    // The active tab name: "overview" or "logs".
    public string Tab { get; set; } = "overview";

    // The resolved date range (inclusive, whole days) that both tabs and the exports use.
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    // The range error ("From date must be on or before To date."), or null when the range is valid.
    public string? RangeError { get; set; }

    // The Overview summary counts, for the six cards.
    public ReportsOverviewCounts Counts { get; set; } = new();

    // The four Overview charts: items per location, Lost vs Found per month, reports per hour, and items per category.
    public ChartViewModel LocationChart { get; set; } = new();
    public ChartViewModel MonthChart { get; set; } = new();
    public ChartViewModel HourChart { get; set; } = new();
    public ChartViewModel CategoryChart { get; set; } = new();

    // The System Logs rows on the current page, and the search term and pager that go with them.
    public List<ReportLogRow> Logs { get; set; } = new();
    public string? LogSearch { get; set; }
    public PagerViewModel LogPager { get; set; } = PagerViewModel.Create(1, 0, 20, "logs");
}

// The six Overview summary counts for the chosen date range (items by DateReported, claims by DateFiled).
public class ReportsOverviewCounts
{
    // Items reported in the range (any type and status).
    public int TotalItems { get; set; }

    // Lost items reported in the range.
    public int LostItems { get; set; }

    // Found items reported in the range.
    public int FoundItems { get; set; }

    // Items returned to their owner (Status Returned) that were reported in the range.
    public int ReturnedItems { get; set; }

    // Claims filed in the range (any status).
    public int TotalClaims { get; set; }

    // Claims approved that were filed in the range.
    public int ApprovedClaims { get; set; }
}

// One row of the System Logs table: when it happened, who did it, the action, the item, and the result.
public class ReportLogRow
{
    // When the action happened.
    public DateTime Timestamp { get; set; }

    // The acting user's full name, or null when the log has no user ("Unknown user" in the view).
    public string? UserName { get; set; }

    // The action name, for example "Approve Claim".
    public string Action { get; set; } = "";

    // The target item's name, or null when the log has no item ("—" in the view).
    public string? ItemName { get; set; }

    // The outcome text, starting with Success, Failed, or Blocked.
    public string Result { get; set; } = "";
}
