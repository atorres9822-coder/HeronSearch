using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// The signed-in student's own reported items (My Reports), all statuses, newest first, paged. StudentController.MyReports fills it.
public class StudentReportListViewModel
{
    // The report rows on the current page.
    public IReadOnlyList<StudentReportRowViewModel> Rows { get; set; } = new List<StudentReportRowViewModel>();

    // The paging state: page number, range text, and the page links.
    public PagerViewModel Pager { get; set; } = PagerViewModel.Create(1, 0, 10, "reports");
}

// One row of My Reports: the student's own item with its type, status, date, and a link to Details.
public class StudentReportRowViewModel
{
    // The item's primary key, used to link to its Details page.
    public int Id { get; set; }

    // A short name for the item, for example "Blue umbrella".
    public string Name { get; set; } = "";

    // The item's category (one of ItemLists.Categories), used for the placeholder icon.
    public string Category { get; set; } = "";

    // The item's photo URL path, or null when there is no photo.
    public string? ImagePath { get; set; }

    // Lost or Found.
    public ItemType Type { get; set; }

    // Active, Claimed, Returned, or Archived.
    public ItemStatus Status { get; set; }

    // When the item was reported (server local time).
    public DateTime DateReported { get; set; }
}
