using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// One Active item shown as a card in a student listing (Lost Items, Found Items, or Search). Read-only.
public class StudentItemCardViewModel
{
    // The item's primary key, used to link to its Details page.
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

    // Lost or Found.
    public ItemType Type { get; set; }

    // When the item was reported (server local time).
    public DateTime DateReported { get; set; }
}
