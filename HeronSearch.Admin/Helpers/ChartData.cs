using System.Globalization;
using System.Text;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;

namespace HeronSearch.Admin.Helpers;

// Shapes the chart data for the Dashboard charts and writes each chart's text alternative.
// Every function is pure: no database and no clock. The caller passes "now" and the rows it loaded.
public static class ChartData
{
    // The number of calendar months in the Six_Month_Window: the current month and the 5 months before it.
    private const int WindowMonthCount = 6;

    // The series names shown in the chart legends and text alternatives.
    public const string LostSeriesName = "Lost";
    public const string FoundSeriesName = "Found";
    public const string ItemsSeriesName = "Items";

    // Returns the first day (at 00:00) of each Six_Month_Window month, oldest first; the last entry is now's month.
    public static IReadOnlyList<DateTime> SixMonthWindow(DateTime now)
    {
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        var months = new List<DateTime>(WindowMonthCount);
        for (var monthsBack = WindowMonthCount - 1; monthsBack >= 0; monthsBack--)
        {
            months.Add(currentMonth.AddMonths(-monthsBack));
        }

        return months;
    }

    // Builds the Lost vs Found chart: one "MMM yyyy" label per month in the given order, and for each month the number
    // of Lost rows and of Found rows dated in that calendar month (0 when there are none). Rows outside the months are
    // ignored, and items of every status count. The Dashboard uses the defaults (a line chart); a caller can pass
    // another title or ChartKind.Bar.
    public static ChartViewModel MonthlySeries(
        IReadOnlyList<DateTime> months,
        IEnumerable<(DateTime DateReported, ItemType Type)> rows,
        string title = "Lost vs Found per month",
        ChartKind kind = ChartKind.Line)
    {
        // Finds a month's position in the chart from its year and month number.
        var indexByMonth = new Dictionary<(int Year, int Month), int>();
        for (var i = 0; i < months.Count; i++)
        {
            indexByMonth.TryAdd((months[i].Year, months[i].Month), i);
        }

        var lost = new int[months.Count];
        var found = new int[months.Count];
        foreach (var row in rows)
        {
            if (!indexByMonth.TryGetValue((row.DateReported.Year, row.DateReported.Month), out var index))
            {
                continue;
            }

            if (row.Type == ItemType.Lost)
            {
                lost[index]++;
            }
            else if (row.Type == ItemType.Found)
            {
                found[index]++;
            }
        }

        return new ChartViewModel
        {
            Title = title,
            Kind = kind,
            Labels = months.Select(DisplayFormat.MonthLabel).ToList(),
            Series = new List<ChartSeries>
            {
                new ChartSeries { Name = LostSeriesName, Values = lost.ToList() },
                new ChartSeries { Name = FoundSeriesName, Values = found.ToList() }
            }
        };
    }

    // Counts how often each name occurs and builds a horizontal bar chart with one "Items" series, sorted by count
    // (highest first) and then by name (A to Z, ordinal comparison). take keeps only the first entries of that order;
    // null keeps them all. The title defaults to the Dashboard's "Top 5 locations".
    public static ChartViewModel TopCounts(IEnumerable<string> names, int? take, string title = "Top 5 locations")
    {
        var counts = names
            .GroupBy(name => name, StringComparer.Ordinal)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .ToList();

        if (take.HasValue)
        {
            counts = counts.Take(take.Value).ToList();
        }

        return new ChartViewModel
        {
            Title = title,
            Kind = ChartKind.HorizontalBar,
            Labels = counts.Select(entry => entry.Name).ToList(),
            Series = new List<ChartSeries>
            {
                new ChartSeries { Name = ItemsSeriesName, Values = counts.Select(entry => entry.Count).ToList() }
            }
        };
    }

    // Shown when the Reports date range is invalid because From is later than To.
    public const string InvalidRangeMessage = "From date must be on or before To date.";

    // Returns the first day (at 00:00) of each calendar month from "from" to "to" inclusive, oldest first.
    // A "from" later than "to" gives an empty list; both bounds are reduced to their month.
    public static IReadOnlyList<DateTime> MonthsInRange(DateTime from, DateTime to)
    {
        var start = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);
        var months = new List<DateTime>();
        for (var month = start; month <= end; month = month.AddMonths(1))
        {
            months.Add(month);
        }

        return months;
    }

    // Resolves the Reports date range. With no dates it defaults to the first day of the Six_Month_Window
    // through today. A missing bound keeps its default. When the resolved From is later than To, it returns the
    // default range plus InvalidRangeMessage; otherwise the error is null. Both bounds are whole days.
    public static (DateTime From, DateTime To, string? Error) ResolveRange(DateTime? from, DateTime? to, DateTime now)
    {
        var defaultFrom = SixMonthWindow(now)[0].Date;
        var defaultTo = now.Date;

        var resolvedFrom = (from ?? defaultFrom).Date;
        var resolvedTo = (to ?? defaultTo).Date;

        if (resolvedFrom > resolvedTo)
        {
            return (defaultFrom, defaultTo, InvalidRangeMessage);
        }

        return (resolvedFrom, resolvedTo, null);
    }

    // Builds the hour-of-day bar chart: 24 buckets labeled "00" to "23", each counting how many dates fall in
    // that hour. Every date contributes to exactly one bucket (its Hour, 0-23).
    public static ChartViewModel HourlyCounts(IEnumerable<DateTime> dates, string title = "Reports by hour of day")
    {
        var counts = new int[24];
        foreach (var date in dates)
        {
            counts[date.Hour]++;
        }

        return new ChartViewModel
        {
            Title = title,
            Kind = ChartKind.Bar,
            Labels = Enumerable.Range(0, 24).Select(hour => hour.ToString("D2", CultureInfo.InvariantCulture)).ToList(),
            Series = new List<ChartSeries>
            {
                new ChartSeries { Name = ItemsSeriesName, Values = counts.ToList() }
            }
        };
    }

    // Writes the chart's text alternative: "{Title}: {label} {value}, {label} {value}, ...".
    // With two or more series, each value follows its series name: "{Title}: {label} Lost 3 Found 2, ...".
    // A chart without labels gives the title alone.
    public static string Describe(ChartViewModel chart)
    {
        if (chart.Labels.Count == 0)
        {
            return chart.Title;
        }

        var showSeriesNames = chart.Series.Count >= 2;
        var entries = new List<string>(chart.Labels.Count);
        for (var i = 0; i < chart.Labels.Count; i++)
        {
            var entry = new StringBuilder(chart.Labels[i]);
            foreach (var series in chart.Series)
            {
                // A series without a value for this label adds nothing.
                if (i >= series.Values.Count)
                {
                    continue;
                }

                entry.Append(' ');
                if (showSeriesNames)
                {
                    entry.Append(series.Name).Append(' ');
                }

                entry.Append(series.Values[i].ToString(CultureInfo.InvariantCulture));
            }

            entries.Add(entry.ToString());
        }

        return chart.Title + ": " + string.Join(", ", entries);
    }
}
