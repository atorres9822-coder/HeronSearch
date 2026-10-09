using HeronSearch.Admin.Data;
using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using HeronSearch.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Controllers;

// The Manage Items pages. Inherits BackOfficeController, so only signed-in Admin and Staff users reach it.
// Index lists, searches, filters, and pages the item reports straight from the database.
public class ItemsController : BackOfficeController
{
    // How many items one page of the list shows.
    private const int PageSize = 10;

    // The database context, supplied by dependency injection.
    private readonly AppDbContext _db;

    // The hosting environment, used only for its WebRootPath (the wwwroot folder where photos are saved).
    private readonly IWebHostEnvironment _env;

    // Receives the database context and the hosting environment from the DI container.
    public ItemsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Shows the Manage Items list for the given search term, Status/Category/Type filters, and page number.
    // Every filter value is validated first (unknown values fall back to the default), the matching rows are
    // sorted newest first, counted, and the requested page is projected read-only into the row type.
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? type,
        string? category,
        int page,
        CancellationToken cancellationToken)
    {
        // Clean the query-string values: unknown filters become null (the default), and the search term is
        // trimmed to its first 100 characters or null when blank.
        var searchTerm = FilterValues.NormalizeSearch(search);
        var statusName = FilterValues.MatchAllowedValue(status, Enum.GetNames<ItemStatus>());
        var typeName = FilterValues.MatchAllowedValue(type, Enum.GetNames<ItemType>());
        var categoryName = FilterValues.MatchAllowedValue(category, ItemLists.Categories);

        // Start from every item and narrow by each applied filter; AsNoTracking because the list is read-only.
        var query = _db.Items.AsNoTracking().AsQueryable();

        // Search: compare the lowercased term against lowercased Name, Description, and Location so matching is
        // case-insensitive whatever the database collation, and the term is always a SQL parameter.
        if (searchTerm != null)
        {
            var lowered = searchTerm.ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(lowered)
                || (i.Description != null && i.Description.ToLower().Contains(lowered))
                || i.Location.ToLower().Contains(lowered));
        }

        // Status filter: the matched name maps back to the enum value (stored as text via HasConversion).
        if (statusName != null)
        {
            var statusValue = Enum.Parse<ItemStatus>(statusName);
            query = query.Where(i => i.Status == statusValue);
        }

        // Type filter: Lost or Found.
        if (typeName != null)
        {
            var typeValue = Enum.Parse<ItemType>(typeName);
            query = query.Where(i => i.Type == typeValue);
        }

        // Category filter: one of the fixed ItemLists.Categories values.
        if (categoryName != null)
        {
            query = query.Where(i => i.Category == categoryName);
        }

        // Newest first, with Id descending as a stable tie-breaker so rows do not shift between pages.
        var ordered = query
            .OrderByDescending(i => i.DateReported)
            .ThenByDescending(i => i.Id);

        // Count the matching rows, build the pager (which clamps the page number), then read just that page.
        var totalCount = await ordered.CountAsync(cancellationToken);
        var pager = PagerViewModel.Create(page, totalCount, PageSize, "items");

        var rows = await ordered
            .Skip(pager.Skip)
            .Take(PageSize)
            .Select(i => new ItemRowViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Location = i.Location,
                DateReported = i.DateReported,
                Description = i.Description,
                ImagePath = i.ImagePath,
                Type = i.Type,
                Status = i.Status,
                ReporterName = i.ReportedByUser!.FullName,
                ClaimCount = i.Claims.Count
            })
            .ToListAsync(cancellationToken);

        // The pure Status checks run in C# after the query so the LINQ translation stays simple.
        foreach (var row in rows)
        {
            row.CanMarkReturned = BusinessRules.CanMarkReturned(row.Status);
            row.CanArchive = BusinessRules.CanArchive(row.Status);
        }

        // Keep the current filters on every page link so paging does not lose the search term or filters.
        pager.AddRouteValue("search", searchTerm);
        pager.AddRouteValue("status", statusName);
        pager.AddRouteValue("type", typeName);
        pager.AddRouteValue("category", categoryName);

        var model = new ItemListViewModel
        {
            Rows = rows,
            Search = searchTerm,
            Status = statusName,
            Category = categoryName,
            Type = typeName,
            Pager = pager,
            IsAdmin = IsAdmin
        };

        return View(model);
    }

    // Shows the empty Add Found Item form, pre-filling Date Found with today.
    [HttpGet]
    public IActionResult Create()
    {
        var model = new ItemFormViewModel { DateFound = DateTime.Today };
        return View(model);
    }

    // Handles the Add Found Item form: validates the choices, saves an optional photo, stores a new Found item,
    // logs "Add Item" with the new Id, and returns to the list with a success alert.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemFormViewModel model)
    {
        // Re-check the dropdown choices against the fixed lists in case a value was tampered with past client validation.
        ValidateCategoryAndLocation(model);

        // Any validation error (annotations or the list checks above) redisplays the form with the entered values.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Save the uploaded photo first; a save failure keeps the user on the form with a field message.
        string? imagePath = null;
        if (model.Photo != null)
        {
            try
            {
                imagePath = await ImageUploadValidator.SaveAsync(model.Photo, _env.WebRootPath);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.Photo), ImageUploadValidator.SaveFailedMessage);
                return View(model);
            }
        }

        // Build the new report: always a Found item that starts Active, reported now by the signed-in user.
        // DateReported keeps the submitted date but takes the current time of day (I8), never later than now.
        var now = DateTime.Now;
        var item = new Item
        {
            Name = model.Name,
            Category = model.Category,
            Location = model.Location,
            Description = model.Description,
            ImagePath = imagePath,
            Type = ItemType.Found,
            Status = ItemStatus.Active,
            DateReported = BusinessRules.CombineDateWithTime(model.DateFound, now.TimeOfDay, now),
            ReportedByUserId = CurrentUserId
        };

        _db.Items.Add(item);

        // Save once so the database assigns the Id, then log "Add Item" with that Id and save the log row.
        await _db.SaveChangesAsync();
        ActivityLogger.Log(_db, CurrentUserId, LogActions.AddItem, item.Id, LogOutcome.Success,
            $"Added item '{item.Name}' (#{item.Id}).");
        await _db.SaveChangesAsync();

        ShowSuccess($"Added '{item.Name}'.");
        return RedirectToAction(nameof(Index));
    }

    // Shows the Edit form prefilled from the stored item; a missing Id returns the Not Found page.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
        {
            return RecordNotFound("Manage Items", Url.Action(nameof(Index)));
        }

        // Date Found shows the stored date only; Type and Status are display-only badges and cannot be edited here.
        var model = new ItemFormViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Location = item.Location,
            Description = item.Description,
            DateFound = item.DateReported.Date,
            CurrentImagePath = item.ImagePath,
            TypeText = item.Type.ToString(),
            StatusText = item.Status.ToString()
        };

        return View(model);
    }

    // Handles the Edit form: validates the choices, swaps the photo when a new one is uploaded, updates the editable
    // fields, logs "Edit Item", and returns to the list with a success alert. Type and Status are never changed here.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ItemFormViewModel model)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
        {
            return RecordNotFound("Manage Items", Url.Action(nameof(Index)));
        }

        // Re-check the dropdown choices against the fixed lists in case a value was tampered with past client validation.
        ValidateCategoryAndLocation(model);

        // Any validation error redisplays the form; restore the read-only display fields the view needs.
        if (!ModelState.IsValid)
        {
            model.Id = item.Id;
            model.CurrentImagePath = item.ImagePath;
            model.TypeText = item.Type.ToString();
            model.StatusText = item.Status.ToString();
            return View(model);
        }

        // Keep the current photo unless a new one is uploaded. On a new photo: save it, point the item at it, then
        // delete the old file afterwards so a save failure never leaves the item without any photo.
        var oldImagePath = item.ImagePath;
        if (model.Photo != null)
        {
            string newImagePath;
            try
            {
                newImagePath = await ImageUploadValidator.SaveAsync(model.Photo, _env.WebRootPath);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.Photo), ImageUploadValidator.SaveFailedMessage);
                model.Id = item.Id;
                model.CurrentImagePath = item.ImagePath;
                model.TypeText = item.Type.ToString();
                model.StatusText = item.Status.ToString();
                return View(model);
            }

            item.ImagePath = newImagePath;
        }

        // Update the editable fields. DateReported keeps the submitted date but reuses the item's original time of
        // day (I8), still clamped so it is never later than now. Type and Status are left exactly as they were.
        item.Name = model.Name;
        item.Category = model.Category;
        item.Location = model.Location;
        item.Description = model.Description;
        item.DateReported = BusinessRules.CombineDateWithTime(model.DateFound, item.DateReported.TimeOfDay, DateTime.Now);

        ActivityLogger.Log(_db, CurrentUserId, LogActions.EditItem, item.Id, LogOutcome.Success,
            $"Edited item '{item.Name}' (#{item.Id}).");
        await _db.SaveChangesAsync();

        // Remove the replaced photo only after the row is safely saved; this ignores missing or locked files.
        if (model.Photo != null)
        {
            ImageUploadValidator.DeleteUploadedFile(_env.WebRootPath, oldImagePath);
        }

        ShowSuccess($"Updated '{item.Name}'.");
        return RedirectToAction(nameof(Index));
    }

    // Handles Mark as Returned: loads the item, counts its Pending claims, and asks BusinessRules.CheckMarkReturned.
    // A refusal keeps the status, logs Blocked, and shows an error alert; otherwise it sets Returned, logs Success, and
    // shows a success alert. Either way it returns to the local returnUrl (the list with its filters) or the Index.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReturned(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item == null)
        {
            return RecordNotFound("Manage Items", Url.Action(nameof(Index)));
        }

        // Count only this item's Pending claims; the status is checked first, then this count (I14).
        var pending = await _db.Claims.CountAsync(c => c.ItemId == id && c.Status == ClaimStatus.Pending, cancellationToken);

        var error = BusinessRules.CheckMarkReturned(item.Status, pending);
        if (error != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.MarkReturned, item.Id, LogOutcome.Blocked,
                $"Mark as returned refused for '{item.Name}' (#{item.Id}): {error}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(error);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        item.Status = ItemStatus.Returned;
        ActivityLogger.Log(_db, CurrentUserId, LogActions.MarkReturned, item.Id, LogOutcome.Success,
            $"Marked item '{item.Name}' (#{item.Id}) as returned.");
        await _db.SaveChangesAsync(cancellationToken);

        ShowSuccess($"Marked '{item.Name}' as returned.");
        return RedirectToLocal(returnUrl, nameof(Index));
    }

    // Handles Archive: loads the item, counts its Pending claims, and asks BusinessRules.CheckArchive.
    // A refusal keeps the status, logs Blocked, and shows an error alert; otherwise it sets Archived, logs Success, and
    // shows a success alert. Either way it returns to the local returnUrl (the list with its filters) or the Index.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item == null)
        {
            return RecordNotFound("Manage Items", Url.Action(nameof(Index)));
        }

        // Count only this item's Pending claims; the status is checked first, then this count (I14).
        var pending = await _db.Claims.CountAsync(c => c.ItemId == id && c.Status == ClaimStatus.Pending, cancellationToken);

        var error = BusinessRules.CheckArchive(item.Status, pending);
        if (error != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.ArchiveItem, item.Id, LogOutcome.Blocked,
                $"Archive refused for '{item.Name}' (#{item.Id}): {error}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(error);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        item.Status = ItemStatus.Archived;
        ActivityLogger.Log(_db, CurrentUserId, LogActions.ArchiveItem, item.Id, LogOutcome.Success,
            $"Archived item '{item.Name}' (#{item.Id}).");
        await _db.SaveChangesAsync(cancellationToken);

        ShowSuccess($"Archived '{item.Name}'.");
        return RedirectToLocal(returnUrl, nameof(Index));
    }

    // Handles Delete, restricted to Admin users. Counts every claim (any status) and asks BusinessRules.CheckItemDelete.
    // A refusal keeps the item, logs Blocked, and shows an error alert. Otherwise it removes the item and logs
    // "Delete Item" with TargetItemId = null (the row is gone) and a Result that names the item and its Id, saves, then
    // deletes the photo file. A DbUpdateException (a claim filed meanwhile) shows the same blocked message.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item == null)
        {
            return RecordNotFound("Manage Items", Url.Action(nameof(Index)));
        }

        // Count claims of every status: an item with any claim history is archived, never deleted.
        var claimCount = await _db.Claims.CountAsync(c => c.ItemId == id, cancellationToken);

        var error = BusinessRules.CheckItemDelete(item.Name, claimCount);
        if (error != null)
        {
            ActivityLogger.Log(_db, CurrentUserId, LogActions.DeleteItem, item.Id, LogOutcome.Blocked,
                $"Delete refused for '{item.Name}' (#{item.Id}): {error}");
            await _db.SaveChangesAsync(cancellationToken);
            ShowError(error);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        // Keep the photo path and the display name before the row is removed, so they are still available for the log
        // message and the file delete that run afterwards.
        var imagePath = item.ImagePath;
        var name = item.Name;
        var deletedId = item.Id;

        _db.Items.Remove(item);

        // TargetItemId is null because the item row no longer exists; the Result keeps the name and Id (I5, 16.6).
        ActivityLogger.Log(_db, CurrentUserId, LogActions.DeleteItem, null, LogOutcome.Success,
            $"Deleted item '{name}' (#{deletedId}).");

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A claim was filed between the count and the save; the Restrict FK blocks the delete. Show the same
            // "archive instead" message the up-front count would have shown.
            ShowError(BusinessRules.CheckItemDelete(name, 1)!);
            return RedirectToLocal(returnUrl, nameof(Index));
        }

        // Remove the stored photo only after the row is safely gone; this stays inside wwwroot/uploads and ignores a
        // missing or locked file.
        ImageUploadValidator.DeleteUploadedFile(_env.WebRootPath, imagePath);

        ShowSuccess($"Deleted '{name}'.");
        return RedirectToLocal(returnUrl, nameof(Index));
    }

    // Adds a ModelState error when Category or Location is not one of the fixed ItemLists values.
    private void ValidateCategoryAndLocation(ItemFormViewModel model)
    {
        if (!ItemLists.Categories.Contains(model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "Choose a category from the list.");
        }

        if (!ItemLists.Locations.Contains(model.Location))
        {
            ModelState.AddModelError(nameof(model.Location), "Choose a location from the list.");
        }
    }
}
