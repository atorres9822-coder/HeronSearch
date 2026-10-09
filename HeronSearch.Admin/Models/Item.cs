namespace HeronSearch.Admin.Models;

// A lost or found item report.
public class Item
{
    // Primary key, filled in by the database.
    public int Id { get; set; }

    // A short name for the item, for example "Blue umbrella".
    public string Name { get; set; } = "";

    // One value from ItemLists.Categories.
    public string Category { get; set; } = "";

    // One value from ItemLists.Locations.
    public string Location { get; set; } = "";

    // When the item was reported (server local time).
    public DateTime DateReported { get; set; }

    // Extra details about the item; optional.
    public string? Description { get; set; }

    // The photo URL path, such as "/uploads/<guid>.jpg", or null when there is no photo.
    public string? ImagePath { get; set; }

    // Lost or Found.
    public ItemType Type { get; set; }

    // Active, Claimed, Returned, or Archived.
    public ItemStatus Status { get; set; }

    // The Id of the user who reported the item (required).
    public int ReportedByUserId { get; set; }

    // The user who reported the item; filled in only when a query loads it.
    public User? ReportedByUser { get; set; }

    // Ownership claims filed for this item.
    public List<Claim> Claims { get; set; } = new();
}
