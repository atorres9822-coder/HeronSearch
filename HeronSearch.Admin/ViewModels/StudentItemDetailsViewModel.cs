using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// One Active item shown read-only on the student Details page. A "Claim this item" button appears for Found items only.
public class StudentItemDetailsViewModel
{
    // The item's primary key, used to build the Claim link.
    public int Id { get; set; }

    // A short name for the item, for example "Blue umbrella".
    public string Name { get; set; } = "";

    // The item's category (one of ItemLists.Categories), used for the placeholder icon.
    public string Category { get; set; } = "";

    // Where the item was lost or found (one of ItemLists.Locations).
    public string Location { get; set; } = "";

    // Extra details about the item; optional.
    public string? Description { get; set; }

    // The photo URL path, such as "/uploads/<guid>.jpg", or null when there is no photo.
    public string? ImagePath { get; set; }

    // Lost or Found; a Found item shows the "Claim this item" button.
    public ItemType Type { get; set; }

    // When the item was reported (server local time).
    public DateTime DateReported { get; set; }

    // The Id of the user who reported the item; the view hides the Claim button when it is the signed-in student.
    public int ReportedByUserId { get; set; }
}
