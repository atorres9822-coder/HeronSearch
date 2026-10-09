using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// Everything the Manage Claims list page shows: the active tab, the three tab counts, the rows on the
// current page, and the pager. ClaimsController.Index fills it; Claims/Index.cshtml reads it.
public class ClaimListViewModel
{
    // The active tab name (Pending, Approved, or Rejected); the table and the empty state follow it.
    public string Tab { get; set; } = nameof(ClaimStatus.Pending);

    // The number of Pending claims, shown on the Pending tab badge.
    public int PendingCount { get; set; }

    // The number of Approved claims, shown on the Approved tab badge.
    public int ApprovedCount { get; set; }

    // The number of Rejected claims, shown on the Rejected tab badge.
    public int RejectedCount { get; set; }

    // The claims on the current page, already sorted and projected from the database.
    public List<ClaimRowViewModel> Rows { get; set; } = new();

    // The paging state for the current tab: page number, range text, and the links that keep the tab.
    public PagerViewModel Pager { get; set; } = PagerViewModel.Create(1, 0, 10, "claims");
}

// One row of the Manage Claims table: the claim, its item (with the photo), the claimant, the filing date,
// and the status. The Action column shows Review for Pending claims and View for reviewed ones.
public class ClaimRowViewModel
{
    // The claim's Id, used to build the Review/View link and shown as "Claim #{Id}".
    public int Id { get; set; }

    // The claimed item's name.
    public string ItemName { get; set; } = "";

    // The claimed item's category, used to pick the placeholder icon when there is no photo.
    public string ItemCategory { get; set; } = "";

    // The claimed item's photo URL path, or null when there is no photo.
    public string? ItemImagePath { get; set; }

    // The claimant's full name.
    public string ClaimantName { get; set; } = "";

    // The claimant's Student ID, or null when the account has none ("—" in the view).
    public string? ClaimantStudentId { get; set; }

    // When the claim was filed.
    public DateTime DateFiled { get; set; }

    // Pending, Approved, or Rejected.
    public ClaimStatus Status { get; set; }
}
