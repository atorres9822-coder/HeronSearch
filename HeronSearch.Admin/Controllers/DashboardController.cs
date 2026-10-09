using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// The admin home page. Inherits BackOfficeController, so only signed-in Admin and Staff users reach it.
// It reads totals, chart data, pending claims, and recent activity straight from the database.
public class DashboardController : BackOfficeController
{
    // The database context, supplied by dependency injection.
    private readonly AppDbContext _db;

    // Receives the database context from the DI container.
    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    // Builds the Dashboard: five stat-card counts, the Lost vs Found monthly line chart and the top-5 locations
    // bar chart, the 5 newest Pending claims, and the 10 newest activity logs. Every query is read-only.
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        // The five stat-card counts. Total Lost and Total Found count every status (no status filter).
        var totalLost = await _db.Items
            .AsNoTracking()
            .CountAsync(i => i.Type == ItemType.Lost, cancellationToken);

        var totalFound = await _db.Items
            .AsNoTracking()
            .CountAsync(i => i.Type == ItemType.Found, cancellationToken);

        var pendingClaims = await _db.Claims
            .AsNoTracking()
            .CountAsync(c => c.Status == ClaimStatus.Pending, cancellationToken);

        var returnedItems = await _db.Items
            .AsNoTracking()
            .CountAsync(i => i.Status == ItemStatus.Returned, cancellationToken);

        var registeredUsers = await _db.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // The monthly chart: load (DateReported, Type) for items dated in the Six_Month_Window, then shape the series.
        var months = ChartData.SixMonthWindow(now);
        var windowStart = months[0];
        var monthlyRows = await _db.Items
            .AsNoTracking()
            .Where(i => i.DateReported >= windowStart)
            .Select(i => new { i.DateReported, i.Type })
            .ToListAsync(cancellationToken);
        var monthlyChart = ChartData.MonthlySeries(
            months,
            monthlyRows.Select(r => (r.DateReported, r.Type)));

        // The top-5 locations chart: load every item's Location, then count and keep the 5 highest.
        var locations = await _db.Items
            .AsNoTracking()
            .Select(i => i.Location)
            .ToListAsync(cancellationToken);
        var locationChart = ChartData.TopCounts(locations, 5);

        // The 5 newest Pending claims (newest DateFiled first), with item and claimant names for the list.
        var pendingClaimRows = await _db.Claims
            .AsNoTracking()
            .Where(c => c.Status == ClaimStatus.Pending)
            .OrderByDescending(c => c.DateFiled)
            .Take(5)
            .Select(c => new PendingClaimRow
            {
                ClaimId = c.Id,
                ItemName = c.Item!.Name,
                ClaimantName = c.ClaimantUser!.FullName,
                DateFiled = c.DateFiled
            })
            .ToListAsync(cancellationToken);

        // The 10 newest activity logs (newest first); UserName is null for a log without a user ("Unknown user" in the view).
        var recentActivity = await _db.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Take(10)
            .Select(l => new RecentActivityRow
            {
                Timestamp = l.Timestamp,
                UserName = l.User != null ? l.User.FullName : null,
                Action = l.Action,
                Result = l.Result
            })
            .ToListAsync(cancellationToken);

        var model = new DashboardViewModel
        {
            TotalLost = totalLost,
            TotalFound = totalFound,
            PendingClaims = pendingClaims,
            ReturnedItems = returnedItems,
            RegisteredUsers = registeredUsers,
            MonthlyChart = monthlyChart,
            LocationChart = locationChart,
            PendingClaimRows = pendingClaimRows,
            RecentActivity = recentActivity
        };

        return View(model);
    }
}
