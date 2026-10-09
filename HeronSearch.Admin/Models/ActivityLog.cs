namespace HeronSearch.Admin.Models;

// One line of the activity history: who did what, to which item, when, and the outcome.
public class ActivityLog
{
    // Primary key, filled in by the database.
    public int Id { get; set; }

    // When the action happened (server local time).
    public DateTime Timestamp { get; set; }

    // The Id of the user who acted; null when unknown or when that user was deleted.
    public int? UserId { get; set; }

    // The user who acted; filled in only when a query loads it.
    public User? User { get; set; }

    // The action name, for example "Login" or "Add Item".
    public string Action { get; set; } = "";

    // The Id of the item the action was about; null when there is none or it was deleted.
    public int? TargetItemId { get; set; }

    // The item the action was about; filled in only when a query loads it.
    public Item? TargetItem { get; set; }

    // The outcome text, starting with Success, Failed, or Blocked.
    public string Result { get; set; } = "";
}
