using System.Globalization;

namespace HeronSearch.Admin.ViewModels;

// The paging state of one list page: the clamped page number, how many rows to skip, the numbers for
// "Showing X-Y of Z items", and the current filters that every page link keeps. Build it with Create;
// the _Pager partial renders it.
public class PagerViewModel
{
    // The most page-number links the pager shows at once.
    public const int MaxPageLinks = 5;

    // Only Create builds a pager, so the numbers below always agree with each other.
    private PagerViewModel()
    {
    }

    // The page being shown (always within 1..TotalPages) and the number of pages (at least 1, even for an empty list).
    public int Page { get; private set; }
    public int TotalPages { get; private set; }

    // The number of matching rows in the whole list and the number of rows per page.
    public int TotalCount { get; private set; }
    public int PageSize { get; private set; }

    // The 1-based positions of the first and last row on this page; both are 0 when the list is empty.
    public int FirstItem { get; private set; }
    public int LastItem { get; private set; }

    // How many rows the query skips before this page: (Page - 1) * PageSize.
    public int Skip { get; private set; }

    // The plural word for the rows, such as "items", used at the end of the range text.
    public string Noun { get; private set; } = "";

    // The current filters (for example search, status, category, and type) that every page link keeps.
    // Keys ignore case, like ASP.NET Core route values. The page number is added per link by RouteValuesFor.
    public Dictionary<string, string> RouteValues { get; } = new(StringComparer.OrdinalIgnoreCase);

    // True when the Previous or Next link has a page to go to.
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    // The range text with an en dash (U+2013), for example "Showing 11" + en dash + "20 of 36 items".
    public string RangeText => $"Showing {FirstItem}\u2013{LastItem} of {TotalCount} {Noun}";

    // Up to five consecutive page numbers centered on the current page and kept within 1..TotalPages,
    // for example 1 2 3 4 5 on page 2, or 6 7 8 9 10 on page 10 of 10.
    public IReadOnlyList<int> PageNumbers
    {
        get
        {
            var count = Math.Min(MaxPageLinks, TotalPages);
            var first = Math.Clamp(Page - MaxPageLinks / 2, 1, TotalPages - count + 1);
            return Enumerable.Range(first, count).ToList();
        }
    }

    // Builds the pager with the design formulas: TotalPages = max(1, ceil(total / size));
    // Page = the requested page clamped to 1..TotalPages; Skip = (Page - 1) * size;
    // FirstItem = 0 for an empty list, otherwise Skip + 1; LastItem = min(Skip + size, total).
    public static PagerViewModel Create(int requestedPage, int totalCount, int pageSize, string noun)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        // Full pages plus one for a partly filled page; written this way so a huge total cannot overflow.
        var totalPages = Math.Max(1, totalCount / pageSize + (totalCount % pageSize == 0 ? 0 : 1));
        var page = Math.Clamp(requestedPage, 1, totalPages);
        var skip = (page - 1) * pageSize;

        return new PagerViewModel
        {
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            Skip = skip,
            FirstItem = totalCount == 0 ? 0 : skip + 1,
            // The sum is computed as a long so Skip + PageSize cannot overflow before the smaller value is taken.
            LastItem = (int)Math.Min((long)skip + pageSize, totalCount),
            Noun = noun
        };
    }

    // Stores one current filter for the page links; a null or blank value is left out so the links stay short.
    public void AddRouteValue(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            RouteValues[name] = value;
        }
    }

    // Returns the route values for a link to the given page: a copy of the current filters plus "page".
    // The _Pager partial passes the result to asp-all-route-data.
    public Dictionary<string, string> RouteValuesFor(int page)
    {
        var values = new Dictionary<string, string>(RouteValues, StringComparer.OrdinalIgnoreCase);
        values["page"] = page.ToString(CultureInfo.InvariantCulture);
        return values;
    }
}
