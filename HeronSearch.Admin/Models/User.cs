namespace HeronSearch.Admin.Models;

// A HeronSearch account: an Admin, a Staff member, or a Student.
public class User
{
    // Primary key, filled in by the database.
    public int Id { get; set; }

    // The person's full name, for example "Juan Dela Cruz".
    public string FullName { get; set; } = "";

    // The sign-in email, stored trimmed and in lowercase.
    public string Email { get; set; } = "";

    // The hashed password (the plain password is never stored).
    public string PasswordHash { get; set; } = "";

    // The Student ID for students or the Staff ID for staff; optional.
    public string? StudentId { get; set; }

    // Admin, Staff, or Student.
    public UserRole Role { get; set; }

    // Pending, Verified, or Suspended.
    public VerificationStatus VerificationStatus { get; set; }

    // When the account was created (server local time).
    public DateTime CreatedAt { get; set; }

    // Items this user reported.
    public List<Item> ReportedItems { get; set; } = new();

    // Claims this user filed as the claimant.
    public List<Claim> FiledClaims { get; set; } = new();

    // Claims this user approved or rejected as a reviewer.
    public List<Claim> ReviewedClaims { get; set; } = new();
}
