using System.ComponentModel.DataAnnotations;
using HeronSearch.Admin.Helpers;

namespace HeronSearch.Admin.ViewModels;

// The form model for Add Found Item (Create) and Edit.
// Name, DateFound, Photo, and Description are validated here; Category and Location carry [Required] and are
// re-checked against ItemLists in the controller action. Type and Status are shown read-only on Edit only.
public class ItemFormViewModel
{
    // The item's primary key: 0 (default) when creating a new item, the existing Id when editing.
    public int Id { get; set; }

    // A short name for the item, for example "Blue umbrella".
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    // One value from ItemLists.Categories; the chosen value is re-checked against that list in the action.
    [Required]
    [Display(Name = "Category")]
    public string Category { get; set; } = "";

    // One value from ItemLists.Locations; the chosen value is re-checked against that list in the action.
    [Required]
    [Display(Name = "Location")]
    public string Location { get; set; } = "";

    // The date the item was found; it may not be later than today (NotInFuture) and renders as a date input.
    [Required]
    [DataType(DataType.Date)]
    [NotInFuture]
    [Display(Name = "Date Found")]
    public DateTime DateFound { get; set; }

    // Extra details about the item; optional, at most 1000 characters.
    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    // The optional uploaded photo; when present it must be a valid JPG, PNG, or WEBP within the size limit.
    [PhotoFile]
    [Display(Name = "Photo")]
    public IFormFile? Photo { get; set; }

    // Edit display only: the current stored photo URL path (for example "/uploads/<guid>.jpg"), or null.
    public string? CurrentImagePath { get; set; }

    // Edit display only: the item's Type (Lost or Found) as text, shown as a read-only badge.
    public string? TypeText { get; set; }

    // Edit display only: the item's Status (Active, Claimed, Returned, Archived) as text, shown as a read-only badge.
    public string? StatusText { get; set; }
}
