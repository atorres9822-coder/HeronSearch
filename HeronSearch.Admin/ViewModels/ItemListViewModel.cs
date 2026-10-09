using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// Everything the Manage Items list page shows: the matching rows, the current search term and filter values
// that the form and page links keep, the pager, and whether the signed-in user is an Admin (for the Delete action).
// ItemsController.Index fills it; Items/Index.cshtml reads it.
public class ItemListViewModel
{
    // The items on the current page, already sorted and projected from the database.
    public List<ItemRowViewModel> Rows { get; set; } = new();

    // The current search term, or null when the search box is empty (echoed back into the search input).
    public string? Search { get; set; }

    // The current Status filter name (Active, Claimed, Returned, Archived), or null for "All".
    public string? Status { get; set; }

    // The current Category filter (an ItemLists.Categories value), or null for "All categories".
    public string? Category { get; set; }

    // The current Type filter name (Lost or Found), or null for "All types".
    public string? Type { get; set; }

    // The paging state for the current filters: page number, range text, and the links that keep the filters.
    public PagerViewModel Pager { get; set; } = PagerViewModel.Create(1, 0, 10, "items");

    // True when the signed-in user is an Admin; the view uses it to show the Admin-only Delete action.
    public bool IsAdmin { get; set; }
}

// One row of the Manage Items table: the item's own fields plus the reporter's name, the number of claims,
// and whether the Mark as Returned and Archive actions are allowed for this item's status.
public class ItemRowViewModel
{
    // The item's Id, used to build the View, Edit, and action links.
    public int Id { get; set; }

    // The item's short name, for example "Blue umbrella".
    public string Name { get; set; } = "";

    // The item's category (an ItemLists.Categories value); also picks the placeholder icon.
    public string Category { get; set; } = "";

    // Where the item was lost or found (an ItemLists.Locations value).
    public string Location { get; set; } = "";

    // When the item was reported; the list sorts by this, newest first.
    public DateTime DateReported { get; set; }

    // Extra details about the item; optional, shown in the details modal.
    public string? Description { get; set; }

    // The photo URL path, or null when there is no photo (the view then shows a category placeholder).
    public string? ImagePath { get; set; }

    // Lost or Found.
    public ItemType Type { get; set; }

    // Active, Claimed, Returned, or Archived.
    public ItemStatus Status { get; set; }

    // The full name of the user who reported the item.
    public string ReporterName { get; set; } = "";

    // How many claims of any status were filed for this item (shown in the details modal).
    public int ClaimCount { get; set; }

    // True when this item's status allows Mark as Returned (BusinessRules.CanMarkReturned).
    public bool CanMarkReturned { get; set; }

    // True when this item's status allows Archive (BusinessRules.CanArchive).
    public bool CanArchive { get; set; }
}
