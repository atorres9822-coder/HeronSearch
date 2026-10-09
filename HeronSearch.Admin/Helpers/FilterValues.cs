namespace HeronSearch.Admin.Helpers;

// Cleans query-string values (filters, tabs, and search terms) before a page uses them.
public static class FilterValues
{
    // The longest search term, in characters, that a page sends to the database.
    private const int MaxSearchLength = 100;

    // Returns the allowed value that equals the trimmed input ignoring case, or null so the caller uses its default.
    public static string? MatchAllowedValue(string? input, IEnumerable<string> allowed)
    {
        if (input == null)
        {
            return null;
        }

        var trimmed = input.Trim();
        foreach (var value in allowed)
        {
            if (string.Equals(value, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    // Returns the trimmed search term cut to its first 100 characters, or null when the term is blank.
    public static string? NormalizeSearch(string? term)
    {
        if (term == null)
        {
            return null;
        }

        var trimmed = term.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length > MaxSearchLength ? trimmed.Substring(0, MaxSearchLength) : trimmed;
    }
}
