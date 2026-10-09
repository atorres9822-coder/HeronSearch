namespace HeronSearch.Admin.ViewModels;

// Everything the student dashboard shows from real data: the student's own counts (My Reports, My Claims, Unread
// Notifications) and the newest activity rows. StudentController.Index fills it; Student/Index.cshtml reads it.
public class StudentDashboardViewModel
{
    // The number of items this student has reported, counting every status.
    public int MyReportsCount { get; set; }

    // The number of claims this student has filed, counting every status.
    public int MyClaimsCount { get; set; }

    // The number of this student's claims reviewed (Approved or Rejected) in the last 7 days; the "unread" badge.
    public int UnreadNotifications { get; set; }

    // The student's newest activity rows (recent reports and reviewed claims), newest first.
    public IReadOnlyList<StudentActivityRow> RecentActivity { get; set; } = new List<StudentActivityRow>();
}

// One row of the student dashboard Recent Activity list: when it happened and a short message about it.
public class StudentActivityRow
{
    // The Bootstrap Icons class for the row (for example "bi-box-arrow-in-down" for a report).
    public string Icon { get; set; } = "";

    // The friendly message, for example "You reported 'Blue umbrella'." or "Your claim for 'Wallet' was approved.".
    public string Message { get; set; } = "";

    // When the activity happened (server local time).
    public DateTime When { get; set; }
}
