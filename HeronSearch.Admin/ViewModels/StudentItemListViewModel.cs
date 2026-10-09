using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// The state of one student listing page (Lost Items, Found Items, or Search): the cards, the current filters,
// the pager, and which action rendered it so the search/filter form posts back to the same page.
public class StudentItemListViewModel
{
    // The controller action that renders this list ("LostItems", "FoundItems", or "Search"); the GET form targets it.
    public string Action { get; set; } = "";

    // The item Type this list is fixed to (Lost or Found), or null on the Search page which shows both.
    public ItemType? Type { get; set; }

    // The item cards on the current page.
    public IReadOnlyList<StudentItemCardViewModel> Cards { get; set; } = new List<StudentItemCardViewModel>();

    // The current search term, or null when none; it fills the search box and is kept on page links.
    public string? Search { get; set; }

    // The current Category filter (one of ItemLists.Categories), or null for "All categories".
    public string? Category { get; set; }

    // The paging state, which also keeps the search term and Category on every page link.
    public PagerViewModel Pager { get; set; } = default!;
}
