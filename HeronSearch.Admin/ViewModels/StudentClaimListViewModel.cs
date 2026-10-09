using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// The signed-in student's own claims (My Claims): the active tab, the three tab counts, the rows, and the pager.
// StudentController.MyClaims fills it; the Pending/Approved/Rejected tabs follow FilterValues.MatchAllowedValue.
public class StudentClaimListViewModel
{
    // The active tab name (Pending, Approved, or Rejected); the table and empty state follow it.
    public string Tab { get; set; } = nameof(ClaimStatus.Pending);

    // The student's number of Pending claims, shown on the Pending tab badge.
    public int PendingCount { get; set; }

    // The student's number of Approved claims, shown on the Approved tab badge.
    public int ApprovedCount { get; set; }

    // The student's number of Rejected claims, shown on the Rejected tab badge.
    public int RejectedCount { get; set; }

    // The claims on the current page, already sorted and projected from the database.
    public IReadOnlyList<StudentClaimRowViewModel> Rows { get; set; } = new List<StudentClaimRowViewModel>();

    // The paging state for the active tab: page number, range text, and the links that keep the tab.
    public PagerViewModel Pager { get; set; } = PagerViewModel.Create(1, 0, 10, "claims");
}

// One row of My Claims: the claim's item name, filing date, status, and the rejection reason when Rejected.
public class StudentClaimRowViewModel
{
    // The claim's Id, shown as "Claim #{Id}".
    public int Id { get; set; }

    // The claimed item's name.
    public string ItemName { get; set; } = "";

    // The claimed item's Id, used to link to its Details page when the item is still Active.
    public int ItemId { get; set; }

    // When the claim was filed.
    public DateTime DateFiled { get; set; }

    // Pending, Approved, or Rejected.
    public ClaimStatus Status { get; set; }

    // Why the claim was rejected; only set for Rejected claims.
    public string? RejectionReason { get; set; }
}
