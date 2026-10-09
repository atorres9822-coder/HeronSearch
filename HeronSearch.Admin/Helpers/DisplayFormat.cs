using System.Globalization;

namespace HeronSearch.Admin.Helpers;

// Formats dates for display and picks the CSS class of each status badge.
public static class DisplayFormat
{
    // Formats a date as "Oct 6, 2026" with the invariant (English) culture.
    public static string ShortDate(DateTime date)
    {
        return date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    // Formats a date and time as "Oct 6, 2026 2:30 PM" with the invariant (English) culture.
    public static string DateAndTime(DateTime date)
    {
        return date.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
    }

    // Formats a month as "Oct 2026" with the invariant (English) culture, for chart labels.
    public static string MonthLabel(DateTime date)
    {
        return date.ToString("MMM yyyy", CultureInfo.InvariantCulture);
    }

    // Returns the hs-badge-* CSS class for a status name; an unknown name gets the gray Archived style.
    public static string StatusBadgeClass(string? status)
    {
        return status switch
        {
            "Pending" => "hs-badge-pending",     // amber
            "Approved" => "hs-badge-approved",   // green
            "Returned" => "hs-badge-returned",   // green
            "Verified" => "hs-badge-verified",   // green
            "Rejected" => "hs-badge-rejected",   // red
            "Suspended" => "hs-badge-suspended", // red
            "Active" => "hs-badge-active",       // Maroon
            "Claimed" => "hs-badge-claimed",     // Gold with dark text
            "Archived" => "hs-badge-archived",   // gray
            _ => "hs-badge-archived"             // unknown name: gray
        };
    }
}
