namespace HeronSearch.Admin.Models;

// The kind of account. Admin and Staff use this admin site; Students use the user side.
// Always set it on purpose: the default value of this enum is Admin.
public enum UserRole { Admin, Staff, Student }

// Whether an account is waiting for approval, approved, or blocked.
// Only Verified Admin and Staff accounts can sign in.
public enum VerificationStatus { Pending, Verified, Suspended }

// Whether an item report is about a lost item or a found item.
public enum ItemType { Lost, Found }

// Where an item is in its life cycle: open, claimed, back with its owner, or archived.
public enum ItemStatus { Active, Claimed, Returned, Archived }

// The review result of an ownership claim.
public enum ClaimStatus { Pending, Approved, Rejected }
