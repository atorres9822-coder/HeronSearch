using System.ComponentModel.DataAnnotations;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// Everything the Review Claim page shows: the claim with its item, claimant, and reviewer, whether it is
// still editable, the suspended warning, the approve checklist, and the reject form. ClaimsController.Review fills it.
public class ClaimReviewViewModel
{
    // The claim's Id.
    public int Id { get; set; }

    // The claim's current status; the page is read-only unless this is Pending.
    public ClaimStatus Status { get; set; }

    // True when the claim is still Pending, so the approve checklist and reject form are shown.
    public bool IsPending => Status == ClaimStatus.Pending;

    // The claimant's proof description.
    public string ProofDescription { get; set; } = "";

    // When the claim was filed.
    public DateTime DateFiled { get; set; }

    // The claimed item's name, category, location, type, status, photo path, and reporter name.
    public string ItemName { get; set; } = "";
    public string ItemCategory { get; set; } = "";
    public string ItemLocation { get; set; } = "";
    public ItemType ItemType { get; set; }
    public ItemStatus ItemStatus { get; set; }
    public string? ItemImagePath { get; set; }
    public string ReporterName { get; set; } = "";

    // The claimant's name, email, Student ID, and verification status (the view warns when Suspended).
    public string ClaimantName { get; set; } = "";
    public string ClaimantEmail { get; set; } = "";
    public string? ClaimantStudentId { get; set; }
    public VerificationStatus ClaimantStatus { get; set; }

    // True when the claimant's account is Suspended; the view shows a warning and the server blocks approval.
    public bool ClaimantIsSuspended => ClaimantStatus == VerificationStatus.Suspended;

    // Read-only fields for a reviewed claim: the reviewer's name, when it was reviewed, and the rejection reason.
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    // The four verification checklist labels, shown as checkboxes above the Approve button.
    public IReadOnlyList<string> ChecklistLabels { get; set; } = BusinessRules.ClaimChecklistLabels;

    // Set when a rejection was submitted with an invalid reason, so the reject modal reopens with the message.
    public RejectClaimViewModel Reject { get; set; } = new();

    // True when the reject modal should open on page load (an invalid reject reason redisplayed the page).
    public bool OpenRejectModal { get; set; }
}

// The approve form: the four checklist booleans. Approve is refused on the server unless all four are true.
public class ApproveClaimViewModel
{
    // The claim being approved.
    public int Id { get; set; }

    // The claimant's identity matches the student ID on record.
    public bool Check1 { get; set; }

    // The proof matches the item's description.
    public bool Check2 { get; set; }

    // The claimant named identifying details that are not in the public listing.
    public bool Check3 { get; set; }

    // The date and location of loss match the item record.
    public bool Check4 { get; set; }

    // True only when all four verification checks are ticked.
    public bool AllChecked => Check1 && Check2 && Check3 && Check4;
}

// The reject form: the reviewer's reason, required and 10 to 500 characters after trimming.
public class RejectClaimViewModel
{
    // The claim being rejected.
    public int Id { get; set; }

    // Why the claim is rejected; 10 to 500 characters. The server trims before checking (I11).
    [Required(ErrorMessage = BusinessRules.RejectReasonMessage)]
    [StringLength(500, MinimumLength = 10, ErrorMessage = BusinessRules.RejectReasonMessage)]
    [Display(Name = "Reason for rejection")]
    public string Reason { get; set; } = "";
}
