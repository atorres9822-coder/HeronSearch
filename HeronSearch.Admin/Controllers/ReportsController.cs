using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// The Reports / History pages. Inherits BackOfficeController, so only signed-in Admin and Staff users reach it.
// Index shows the Overview charts or the System Logs for a date range; ExportItems and ExportLogs download CSV.
public class ReportsController : BackOfficeController
{
    // How many log rows one page of the System Logs tab shows.
    private const int LogPageSize = 20;

    // The database context, supplied by dependency injection.
    private readonly AppDbContext _db;

    // Receives the database context from the DI container.
    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    // Shows the Reports / History page for the chosen tab (overview or logs) and date range. Overview builds the
    // six counts and four charts; System Logs lists the range's logs, newest first, with search and paging.
    public async Task<IActionResult> Index(
        string? tab,
        DateTime? from,
        DateTime? to,
        string? search,
        int page,
        CancellationToken cancellationToken)
    {
        var tabName = FilterValues.MatchAllowedValue(tab, new[] { "overview", "logs" }) ?? "overview";
        var range = ChartData.ResolveRange(from, to, DateTime.Now);

        var model = new ReportsViewModel
        {
            Tab = tabName,
            From = range.From,
            To = range.To,
            RangeError = range.Error
        };

        // The inclusive-by-day upper bound: everything strictly before the day after To.
        var toExclusive = range.To.AddDays(1);

        if (tabName == "logs")
        {
            await FillLogsAsync(model, search, page, toExclusive, cancellationToken);
        }
        else
        {
            await FillOverviewAsync(model, toExclusive, cancellationToken);
        }

        return View(model);
    }

    // Downloads the items reported in the range as a CSV file, reusing the same range without paging.
    [HttpGet]
    public async Task<IActionResult> ExportItems(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var range = ChartData.ResolveRange(from, to, DateTime.Now);
        var toExclusive = range.To.AddDays(1);

        var items = await _db.Items
            .AsNoTracking()
            .Where(i => i.DateReported >= range.From && i.DateReported < toExclusive)
            .OrderByDescending(i => i.DateReported).ThenByDescending(i => i.Id)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Category,
                i.Location,
                i.Type,
                i.Status,
                i.DateReported,
                Reporter = i.ReportedByUser!.FullName
            })
            .ToListAsync(cancellationToken);

        var header = new[] { "Id", "Name", "Category", "Location", "Type", "Status", "Date Reported", "Reporter" };
        var rows = items.Select(i => (IReadOnlyList<string>)new[]
        {
            i.Id.ToString(),
            i.Name,
            i.Category,
            i.Location,
            i.Type.ToString(),
            i.Status.ToString(),
            DisplayFormat.DateAndTime(i.DateReported),
            i.Reporter
        });

        var bytes = CsvExporter.Build(header, rows);
        var fileName = CsvExporter.FileName("items", range.From, range.To);
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // Downloads the activity logs in the range as a CSV file, reusing the same range without paging.
    [HttpGet]
    public async Task<IActionResult> ExportLogs(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var range = ChartData.ResolveRange(from, to, DateTime.Now);
        var toExclusive = range.To.AddDays(1);

        var logs = await _db.ActivityLogs
            .AsNoTracking()
            .Where(l => l.Timestamp >= range.From && l.Timestamp < toExclusive)
            .OrderByDescending(l => l.Timestamp).ThenByDescending(l => l.Id)
            .Select(l => new
            {
                l.Timestamp,
                UserName = l.User != null ? l.User.FullName : null,
                l.Action,
                ItemName = l.TargetItem != null ? l.TargetItem.Name : null,
                l.Result
            })
            .ToListAsync(cancellationToken);

        var header = new[] { "Timestamp", "User", "Action", "Item", "Result" };
        var rows = logs.Select(l => (IReadOnlyList<string>)new[]
        {
            DisplayFormat.DateAndTime(l.Timestamp),
            l.UserName ?? "Unknown user",
            l.Action,
            l.ItemName ?? "",
            l.Result
        });

        var bytes = CsvExporter.Build(header, rows);
        var fileName = CsvExporter.FileName("logs", range.From, range.To);
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // Fills the six Overview counts and the four charts from the items and claims in the range.
    private async Task FillOverviewAsync(ReportsViewModel model, DateTime toExclusive, CancellationToken cancellationToken)
    {
        var from = model.From;

        // Items reported in the range, projected to just the fields the counts and charts need.
        var items = await _db.Items
            .AsNoTracking()
            .Where(i => i.DateReported >= from && i.DateReported < toExclusive)
            .Select(i => new { i.DateReported, i.Type, i.Status, i.Location, i.Category })
            .ToListAsync(cancellationToken);

        // Claims filed in the range, projected to their status.
        var claims = await _db.Claims
            .AsNoTracking()
            .Where(c => c.DateFiled >= from && c.DateFiled < toExclusive)
            .Select(c => new { c.Status })
            .ToListAsync(cancellationToken);

        model.Counts = new ReportsOverviewCounts
        {
            TotalItems = items.Count,
            LostItems = items.Count(i => i.Type == ItemType.Lost),
            FoundItems = items.Count(i => i.Type == ItemType.Found),
            ReturnedItems = items.Count(i => i.Status == ItemStatus.Returned),
            TotalClaims = claims.Count,
            ApprovedClaims = claims.Count(c => c.Status == ClaimStatus.Approved)
        };

        // The four charts, all built by the pure ChartData helpers.
        model.LocationChart = ChartData.TopCounts(items.Select(i => i.Location), null, "Items by location");
        model.MonthChart = ChartData.MonthlySeries(
            ChartData.MonthsInRange(model.From, model.To),
            items.Select(i => (i.DateReported, i.Type)));
        model.HourChart = ChartData.HourlyCounts(items.Select(i => i.DateReported));
        model.CategoryChart = ChartData.TopCounts(items.Select(i => i.Category), null, "Items by category");
    }

    // Fills the System Logs rows: logs in the range, optional search on the user's full name, Action, and Result,
    // newest first, 20 per page.
    private async Task FillLogsAsync(ReportsViewModel model, string? search, int page, DateTime toExclusive, CancellationToken cancellationToken)
    {
        var from = model.From;
        var term = FilterValues.NormalizeSearch(search);

        var query = _db.ActivityLogs
            .AsNoTracking()
            .Where(l => l.Timestamp >= from && l.Timestamp < toExclusive);

        if (term != null)
        {
            var lowered = term.ToLower();
            query = query.Where(l =>
                (l.User != null && l.User.FullName.ToLower().Contains(lowered))
                || l.Action.ToLower().Contains(lowered)
                || l.Result.ToLower().Contains(lowered));
        }

        var ordered = query.OrderByDescending(l => l.Timestamp).ThenByDescending(l => l.Id);
        var totalCount = await ordered.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, LogPageSize, "logs");

        model.Logs = await ordered
            .Skip(pager.Skip)
            .Take(LogPageSize)
            .Select(l => new ReportLogRow
            {
                Timestamp = l.Timestamp,
                UserName = l.User != null ? l.User.FullName : null,
                Action = l.Action,
                ItemName = l.TargetItem != null ? l.TargetItem.Name : null,
                Result = l.Result
            })
            .ToListAsync(cancellationToken);

        // Keep the tab, the range, and the search term on every page link.
        pager.AddRouteValue("tab", "logs");
        pager.AddRouteValue("from", model.From.ToString("yyyy-MM-dd"));
        pager.AddRouteValue("to", model.To.ToString("yyyy-MM-dd"));
        pager.AddRouteValue("search", term);

        model.LogSearch = term;
        model.LogPager = pager;
    }
}
