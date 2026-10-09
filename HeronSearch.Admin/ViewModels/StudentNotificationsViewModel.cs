using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// The student's Notifications, derived at read time from their reviewed claims (Approved or Rejected), newest first.
// Nothing is stored in the database; StudentController.Notifications builds the list from the Claims table.
public class StudentNotificationsViewModel
{
    // The notification rows, newest first (by the claim's ReviewedAt).
    public IReadOnlyList<StudentNotificationViewModel> Rows { get; set; } = new List<StudentNotificationViewModel>();
}

// One notification: a short message about a reviewed claim and when it was reviewed.
public class StudentNotificationViewModel
{
    // Approved or Rejected; used to pick the status badge and icon.
    public ClaimStatus Status { get; set; }

    // The friendly message, for example "Your claim for 'Blue umbrella' was approved.".
    public string Message { get; set; } = "";

    // When the claim was reviewed (server local time); null is never passed here because only reviewed claims appear.
    public DateTime ReviewedAt { get; set; }
}
