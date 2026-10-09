using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// Everything the Manage Users list page shows: the matching rows, the current search term and filters, and the pager.
// UsersController.Index fills it; Users/Index.cshtml reads it.
public class UserListViewModel
{
    // The accounts on the current page, already sorted and projected from the database.
    public List<UserRowViewModel> Rows { get; set; } = new();

    // The current search term, or null when the search box is empty.
    public string? Search { get; set; }

    // The current Status filter name (Pending, Verified, Suspended), or null for "All".
    public string? Status { get; set; }

    // The current Role filter name (Admin, Staff, Student), or null for "All roles".
    public string? Role { get; set; }

    // The paging state for the current filters.
    public PagerViewModel Pager { get; set; } = PagerViewModel.Create(1, 0, 10, "users");
}

// One row of the Manage Users table: the account's fields plus whether it is the signed-in Admin's own row and
// which actions its status allows. The self row hides Suspend, Change role, and Delete.
public class UserRowViewModel
{
    // The account's Id, used to build the action forms.
    public int Id { get; set; }

    // The account's full name.
    public string FullName { get; set; } = "";

    // The sign-in email.
    public string Email { get; set; } = "";

    // The Student or Staff ID, or null when none ("—" in the view).
    public string? StudentId { get; set; }

    // Admin, Staff, or Student.
    public UserRole Role { get; set; }

    // Pending, Verified, or Suspended.
    public VerificationStatus VerificationStatus { get; set; }

    // When the account was created.
    public DateTime CreatedAt { get; set; }

    // True when this row is the signed-in Admin's own account; the view shows "(you)" and hides the risky actions.
    public bool IsSelf { get; set; }

    // True when the account's status allows Verify (Pending or Suspended).
    public bool CanVerify { get; set; }

    // True when the account's status allows Suspend (Pending or Verified) and it is not the signed-in Admin's own row.
    public bool CanSuspend { get; set; }
}
