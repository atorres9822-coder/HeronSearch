namespace HeronSearch.Admin.Models;

// An ownership claim: a user says a found item belongs to them and describes the proof.
// (Not the same as System.Security.Claims.Claim, which is used for sign-in.)
public class Claim
{
    // Primary key, filled in by the database.
    public int Id { get; set; }

    // The Id of the claimed item (required).
    public int ItemId { get; set; }

    // The claimed item; filled in only when a query loads it.
    public Item? Item { get; set; }

    // The Id of the user who filed the claim (required).
    public int ClaimantUserId { get; set; }

    // The user who filed the claim; filled in only when a query loads it.
    public User? ClaimantUser { get; set; }

    // The claimant's description of why the item is theirs.
    public string ProofDescription { get; set; } = "";

    // When the claim was filed (server local time).
    public DateTime DateFiled { get; set; }

    // Pending, Approved, or Rejected.
    public ClaimStatus Status { get; set; }

    // Why the claim was rejected; only set for Rejected claims.
    public string? RejectionReason { get; set; }

    // The Id of the Admin or Staff member who reviewed the claim; null while Pending.
    public int? ReviewedByUserId { get; set; }

    // The reviewer; filled in only when a query loads it.
    public User? ReviewedByUser { get; set; }

    // When the claim was approved or rejected; null while Pending.
    public DateTime? ReviewedAt { get; set; }
}
