using HeronSearch.Admin.Helpers;
using HeronSearch.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HeronSearch.Admin.Data;

// Fills an empty database with demo data on the first start. BuildDemoData is a pure function that
// returns the linked object graph; SeedAsync adds it inside a scope and saves once.
public static class DbSeeder
{
    // The demo object graph that BuildDemoData returns: users (with their items, claims, and reviews
    // linked through navigation properties) and the activity logs (which have no inverse collection on User).
    public sealed class DemoData
    {
        // The 12 demo accounts, each carrying its reported items, filed claims, and reviewed claims.
        public List<User> Users { get; set; } = new();

        // The activity log rows; each one points at its user and item through navigation properties.
        public List<ActivityLog> Logs { get; set; } = new();
    }

    // Seeds the demo data when the database has no users yet. Creates a scope, reads the context and the
    // password hasher, and returns unchanged when any user already exists. Otherwise it builds the whole graph,
    // adds it, and saves once (a single transaction, so a failure leaves no partial data). Every exception is
    // caught and logged with a hint; startup then continues.
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            // A scoped provider so the pooled/ scoped AppDbContext is resolved correctly outside a request.
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            // Leave an already-seeded (or user-populated) database untouched.
            if (await db.Users.AnyAsync())
            {
                return;
            }

            // Build the linked graph for the current time and add it. Adding the users cascades their items,
            // claims, and reviews through navigation fixup; the logs are added on their own because User has
            // no inverse log collection. One SaveChangesAsync inserts everything in a single transaction.
            var data = BuildDemoData(DateTime.Now, hasher);
            db.Users.AddRange(data.Users);
            db.ActivityLogs.AddRange(data.Logs);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Never block startup. The hint covers the usual first-run causes: a wrong MySQL password or a
            // database that has not been migrated yet.
            logger.LogError(
                ex,
                "Could not prepare demo data: {reason}. Check the MySQL password in appsettings.json, then run 'dotnet tool restore' and 'dotnet ef database update'.",
                ex.Message);
        }
    }

    // Builds the demo object graph in memory for the given time, using the hasher for every password.
    // Pure: it touches no database and sets no Ids, so tests can call it with any now and a fake hasher.
    // Produces 12 users, 36 items (6 per window month), 10 claims on Found items, and 24 activity logs,
    // all linked through navigation properties and following the design's date, claim-timing, and CreatedAt rules.
    public static DemoData BuildDemoData(DateTime now, IPasswordHasher<User> hasher)
    {
        // The first day (00:00) of the earliest window month; every seeded date lands in [windowStart, now].
        var windowStart = FirstOfMonth(now).AddMonths(-5);

        // The 12 accounts. Admin and two Staff first, then 6 Verified, 2 Pending, and 1 Suspended student.
        var admin = MakeUser("Mateo Villanueva", "admin@heronsearch.local", null,
            UserRole.Admin, VerificationStatus.Verified, "HeronAdmin2026", hasher);
        var ramon = MakeUser("Ramon Dela Cruz", "ramon.delacruz@heronsearch.local", "STAFF-0012",
            UserRole.Staff, VerificationStatus.Verified, "HeronStaff2026", hasher);
        var liza = MakeUser("Liza Mercado", "liza.mercado@heronsearch.local", "STAFF-0028",
            UserRole.Staff, VerificationStatus.Verified, "HeronStaff2026", hasher);

        // Nine students; all share the demo student password. StudentIds follow the "YYYY-NNNNN" pattern.
        var carlo = MakeUser("Carlo Bautista", "carlo.bautista@heronsearch.local", "2024-10451",
            UserRole.Student, VerificationStatus.Verified, "HeronStudent2026", hasher);
        var maria = MakeUser("Maria Santos", "maria.santos@heronsearch.local", "2024-10452",
            UserRole.Student, VerificationStatus.Verified, "HeronStudent2026", hasher);
        var joshua = MakeUser("Joshua Reyes", "joshua.reyes@heronsearch.local", "2023-10188",
            UserRole.Student, VerificationStatus.Verified, "HeronStudent2026", hasher);
        var andrea = MakeUser("Andrea Gonzales", "andrea.gonzales@heronsearch.local", "2023-10190",
            UserRole.Student, VerificationStatus.Verified, "HeronStudent2026", hasher);
        var paolo = MakeUser("Paolo Ramos", "paolo.ramos@heronsearch.local", "2022-09977",
            UserRole.Student, VerificationStatus.Verified, "HeronStudent2026", hasher);
        var bea = MakeUser("Bea Navarro", "bea.navarro@heronsearch.local", "2022-09981",
            UserRole.Student, VerificationStatus.Verified, "HeronStudent2026", hasher);
        var diego = MakeUser("Diego Flores", "diego.flores@heronsearch.local", "2025-11003",
            UserRole.Student, VerificationStatus.Pending, "HeronStudent2026", hasher);
        var kyla = MakeUser("Kyla Domingo", "kyla.domingo@heronsearch.local", "2025-11007",
            UserRole.Student, VerificationStatus.Pending, "HeronStudent2026", hasher);
        var marco = MakeUser("Marco Aquino", "marco.aquino@heronsearch.local", "2024-10620",
            UserRole.Student, VerificationStatus.Suspended, "HeronStudent2026", hasher);

        // The 36 item templates in month-then-slot order (offset 5 oldest first; slots Lost,Found,Lost,Found,Lost,Found).
        // Columns: name, category, location, description, type, status, reporter. ImagePath is left null for all.
        var templates = new (string Name, string Category, string Location, string? Description, ItemType Type, ItemStatus Status, User Reporter)[]
        {
            // Month offset 5 (oldest): older items reach Returned and Archived states.
            ("Student ID card", "ID Card", "Library", "Laminated UMak ID found near the stacks.", ItemType.Lost, ItemStatus.Archived, carlo),
            ("Scientific calculator", "Calculator", "Library", "Casio unit turned in at the circulation desk.", ItemType.Found, ItemStatus.Returned, ramon),
            ("Blue umbrella", "Umbrella", "Canteen", "Folding umbrella left on a bench.", ItemType.Lost, ItemStatus.Archived, maria),
            ("Stainless tumbler", "Tumbler", "Canteen", "Silver tumbler with a dented lid.", ItemType.Found, ItemStatus.Archived, admin),
            ("Set of keys", "Keys", "Classroom 101", "Three keys on a maroon lanyard.", ItemType.Lost, ItemStatus.Archived, joshua),
            ("Laptop charger", "Phone & Gadgets", "Library", "65W USB-C charger with a frayed cable.", ItemType.Found, ItemStatus.Archived, ramon),

            // Month offset 4: a Claimed laptop and more archived/returned stock.
            ("Umbrella", "Umbrella", "Canteen", "Plain black umbrella.", ItemType.Lost, ItemStatus.Archived, andrea),
            ("Laptop", "Laptop", "Library", "Gray 14-inch laptop in a sleeve.", ItemType.Found, ItemStatus.Claimed, ramon),
            ("ID Card", "ID Card", "Classroom 205", "Student ID with a cracked corner.", ItemType.Lost, ItemStatus.Archived, paolo),
            ("Travel tumbler", "Tumbler", "Canteen", "Blue tumbler with stickers.", ItemType.Found, ItemStatus.Returned, admin),
            ("Umbrella", "Umbrella", "Main Lobby", "Compact umbrella, red handle.", ItemType.Lost, ItemStatus.Archived, bea),
            ("Wallet", "Wallet", "Library", "Brown bifold wallet, no cash inside.", ItemType.Found, ItemStatus.Returned, liza),

            // Month offset 3: the Returned calculator and two Claimed items that back approved claims.
            ("Graphing calculator", "Calculator", "Classroom 312", "TI-style calculator with initials inked on the back.", ItemType.Lost, ItemStatus.Returned, carlo),
            ("Calculator", "Calculator", "Library", "Pocket calculator found under a desk.", ItemType.Found, ItemStatus.Returned, ramon),
            ("Phone", "Phone & Gadgets", "Canteen", "Black smartphone, screen locked.", ItemType.Lost, ItemStatus.Archived, maria),
            ("Insulated tumbler", "Tumbler", "Canteen", "Pink tumbler, straw lid.", ItemType.Found, ItemStatus.Claimed, admin),
            ("Notebook", "Books & Notes", "Classroom 101", "Spiral notebook with calculus notes.", ItemType.Lost, ItemStatus.Archived, joshua),
            ("Earbuds case", "Phone & Gadgets", "Canteen", "White wireless earbuds case, no earbuds.", ItemType.Found, ItemStatus.Active, ramon),

            // Month offset 2: a Found tumbler that gets a rejected claim.
            ("Jacket", "Clothing", "Gymnasium", "Navy windbreaker, size medium.", ItemType.Lost, ItemStatus.Archived, andrea),
            ("Tumbler", "Tumbler", "Library", "Green tumbler covered in stickers.", ItemType.Found, ItemStatus.Active, liza),
            ("Umbrella", "Umbrella", "Classroom 205", "Transparent dome umbrella.", ItemType.Lost, ItemStatus.Active, paolo),
            ("Water bottle", "Tumbler", "University Oval", "Clear sports bottle, half full.", ItemType.Found, ItemStatus.Active, admin),
            ("Scientific calculator", "Calculator", "Classroom 312", "Calculator with a sticker on the lid.", ItemType.Lost, ItemStatus.Active, bea),
            ("Backpack", "Bag", "Canteen", "Black backpack with a broken zipper.", ItemType.Found, ItemStatus.Active, ramon),

            // Month offset 1: Active Found items that carry the four pending claims.
            ("Umbrella", "Umbrella", "Main Lobby", "Beige umbrella with a wooden handle.", ItemType.Lost, ItemStatus.Active, carlo),
            ("Umbrella", "Umbrella", "Library", "Yellow umbrella found at the entrance.", ItemType.Found, ItemStatus.Active, liza),
            ("ID Card", "ID Card", "Classroom 101", "Student ID near the whiteboard.", ItemType.Lost, ItemStatus.Active, maria),
            ("Laptop", "Laptop", "Computer Laboratory", "Silver laptop left on a workstation.", ItemType.Found, ItemStatus.Active, ramon),
            ("Calculator", "Calculator", "Classroom 205", "Calculator forgotten after an exam.", ItemType.Lost, ItemStatus.Active, joshua),
            ("ID Card", "ID Card", "Canteen", "Student ID found by the counter.", ItemType.Found, ItemStatus.Active, admin),

            // Month offset 0 (current month): freshest Active reports.
            ("Phone", "Phone & Gadgets", "Classroom 312", "Smartphone with a blue case.", ItemType.Lost, ItemStatus.Active, andrea),
            ("Tumbler", "Tumbler", "Library", "White tumbler with a name sticker.", ItemType.Found, ItemStatus.Active, liza),
            ("Keys", "Keys", "Parking Area", "Car key and two house keys.", ItemType.Lost, ItemStatus.Active, paolo),
            ("Laptop", "Laptop", "Library", "Dark gray laptop in a padded sleeve.", ItemType.Found, ItemStatus.Active, ramon),
            ("Umbrella", "Umbrella", "Canteen", "Red umbrella leaning on a chair.", ItemType.Lost, ItemStatus.Active, bea),
            ("Wallet", "Wallet", "Main Lobby", "Black cardholder with a bus pass.", ItemType.Found, ItemStatus.Active, admin),
        };

        // Build the items with their dates and wire each one to its reporter's ReportedItems collection.
        var items = templates
            .Select((t, index) =>
            {
                var monthOffset = 5 - (index / 6);
                var slot = index % 6;
                var item = new Item
                {
                    Name = t.Name,
                    Category = t.Category,
                    Location = t.Location,
                    Description = t.Description,
                    ImagePath = null,
                    Type = t.Type,
                    Status = t.Status,
                    DateReported = SlotDate(now, windowStart, monthOffset, slot),
                    ReportedByUser = t.Reporter
                };
                t.Reporter.ReportedItems.Add(item);
                return item;
            })
            .ToList();

        // Named handles for the items that carry claims (index = monthOffset-from-5 block * 6 + slot).
        var calcReturned = items[13];   // offset 3, Found calculator (Returned) -> A1 Approved
        var laptopClaimed = items[7];    // offset 4, Found laptop (Claimed)    -> A2 Approved + R1 Rejected
        var tumblerClaimed = items[15];  // offset 3, Found tumbler (Claimed)   -> A3 Approved
        var earbudsActive = items[17];   // offset 3, Found earbuds (Active)    -> R2 Rejected
        var umbrellaActive = items[25];  // offset 1, Found umbrella (Active)   -> P1 Pending
        var laptopActive = items[27];    // offset 1, Found laptop (Active)     -> P2, P3 Pending
        var idCardActive = items[29];    // offset 1, Found ID card (Active)    -> P4 Pending (Suspended student)
        var tumblerR3 = items[19];       // offset 2, Found tumbler (Active)    -> R3 Rejected

        // The 10 claims on Found items: 3 Approved, 3 Rejected, 4 Pending. Each links item, claimant, and reviewer.
        var claims = new List<Claim>
        {
            // A1: approved claim on the returned calculator, reviewed by Staff Ramon.
            MakeApprovedClaim(calcReturned, maria, ramon, now, "The calculator has my initials 'MS' on the back cover.", 2),
            // A2: approved claim on the claimed laptop, reviewed by Staff Ramon.
            MakeApprovedClaim(laptopClaimed, carlo, ramon, now, "Serial number matches the receipt I still have.", 2),
            // A3: approved claim on the claimed tumbler, reviewed by Demo Admin.
            MakeApprovedClaim(tumblerClaimed, andrea, admin, now, "Pink tumbler with the straw lid and my sticker.", 2),
            // R1: rejected claim on the same laptop, decided together with A2 (same reviewer and time).
            MakeRejectedClaim(laptopClaimed, joshua, ramon, now, "I think this laptop is mine.",
                "Another claim for this item was approved.", 2),
            // R2: rejected claim on the active earbuds case.
            MakeRejectedClaim(earbudsActive, paolo, ramon, now, "The earbuds case looks like mine.",
                "Proof did not match the serial number on the item.", 2),
            // R3: rejected claim on an active tumbler.
            MakeRejectedClaim(tumblerR3, bea, admin, now, "That green tumbler is mine.",
                "Claimant could not describe the stickers on the tumbler.", 2),
            // P1: pending claim on an active umbrella.
            MakePendingClaim(umbrellaActive, carlo, now, "Yellow umbrella I lost at the library entrance."),
            // P2, P3: two pending claims on one active laptop (demo of the auto-reject on approval).
            MakePendingClaim(laptopActive, maria, now, "Silver laptop I left in the computer lab."),
            MakePendingClaim(laptopActive, joshua, now, "I believe the laptop in the lab is mine."),
            // P4: pending claim filed by the Suspended student on an active ID card.
            MakePendingClaim(idCardActive, marco, now, "The student ID at the canteen is mine."),
        };

        // Set each user's CreatedAt from their earliest record, or spread the record-free users across the window.
        var withRecords = new[] { admin, ramon, liza, carlo, maria, joshua, andrea, paolo, bea, marco };
        foreach (var user in withRecords)
        {
            // The earliest moment this user appears as reporter, claimant, or reviewer; null when they have none.
            var dates = user.ReportedItems.Select(i => i.DateReported)
                .Concat(user.FiledClaims.Select(c => c.DateFiled))
                .Concat(user.ReviewedClaims.Select(c => c.ReviewedAt ?? c.DateFiled))
                .ToList();
            if (dates.Count > 0)
            {
                var first = dates.Min();
                // A point one quarter of the way from the window start to the first record keeps CreatedAt earlier.
                user.CreatedAt = windowStart.AddTicks((first - windowStart).Ticks / 4);
            }
        }

        // The two record-free users (Liza has records, so the record-free ones are the Pending students) are
        // spread across the window at fixed fractions, keeping CreatedAt inside [windowStart, now].
        diego.CreatedAt = windowStart.AddTicks((long)((now - windowStart).Ticks * 0.55));
        kyla.CreatedAt = windowStart.AddTicks((long)((now - windowStart).Ticks * 0.80));

        // Activity logs (24 >= the required 15), spread across the window, including Login, Approve Claim,
        // and Reject Claim. Reviewed claims each get a Login 10 minutes before the decision, then the decision log.
        var logs = new List<ActivityLog>();

        // One Login + decision log per reviewed claim (A1, A2, A3, R2, R3). R1 shares A2's decision, so it is skipped.
        var reviewedForLogs = new[]
        {
            (Claim: claims[0], Action: LogActions.ApproveClaim, Approved: true),
            (Claim: claims[1], Action: LogActions.ApproveClaim, Approved: true),
            (Claim: claims[2], Action: LogActions.ApproveClaim, Approved: true),
            (Claim: claims[4], Action: LogActions.RejectClaim, Approved: false),
            (Claim: claims[5], Action: LogActions.RejectClaim, Approved: false),
        };
        foreach (var entry in reviewedForLogs)
        {
            var claim = entry.Claim;
            var reviewer = claim.ReviewedByUser!;
            var reviewedAt = claim.ReviewedAt!.Value;
            // Sign-in a few minutes before the review decision.
            logs.Add(MakeLog(reviewedAt.AddMinutes(-10), reviewer, LogActions.Login, null,
                LogOutcome.Success, $"{reviewer.Email} signed in."));
            // The approve or reject decision at the review time.
            var verb = entry.Approved ? "Approved" : "Rejected";
            logs.Add(MakeLog(reviewedAt, reviewer, entry.Action, claim.Item,
                LogOutcome.Success, $"{verb} claim for '{claim.Item!.Name}' filed by {claim.ClaimantUser!.FullName}."));
        }

        // An "Add Item" log at DateReported for every Found item reported by Staff or the Admin, across all months.
        foreach (var item in items.Where(i => i.Type == ItemType.Found
            && i.ReportedByUser != null
            && (i.ReportedByUser.Role == UserRole.Staff || i.ReportedByUser.Role == UserRole.Admin)))
        {
            logs.Add(MakeLog(item.DateReported, item.ReportedByUser, LogActions.AddItem, item,
                LogOutcome.Success, $"Added found item '{item.Name}'."));
        }

        // A few lifecycle logs: Mark Returned / Archive for older Returned and Archived items (7 days after report,
        // but never later than now), from months 3-5.
        foreach (var item in items.Where(i => (i.Status == ItemStatus.Returned || i.Status == ItemStatus.Archived)
            && i.DateReported < windowStart.AddMonths(3)))
        {
            var when = Min(item.DateReported.AddDays(7), now);
            if (item.Status == ItemStatus.Returned)
            {
                logs.Add(MakeLog(when, ramon, LogActions.MarkReturned, item,
                    LogOutcome.Success, $"Marked '{item.Name}' as returned to its owner."));
            }
            else
            {
                logs.Add(MakeLog(when, admin, LogActions.ArchiveItem, item,
                    LogOutcome.Success, $"Archived '{item.Name}'."));
            }
        }

        // One failed sign-in with no matching user (UserId stays null), early in the window.
        logs.Add(MakeLog(windowStart.AddDays(3).AddHours(9), null, LogActions.Login, null,
            LogOutcome.Failed, "unknown@heronsearch.local could not sign in: invalid email or password."));

        return new DemoData
        {
            Users = new List<User> { admin, ramon, liza, carlo, maria, joshua, andrea, paolo, bea, diego, kyla, marco },
            Logs = logs
        };
    }

    // Returns the first day of now's month at 00:00; the window math builds on this anchor.
    private static DateTime FirstOfMonth(DateTime now) => new DateTime(now.Year, now.Month, 1, 0, 0, 0);

    // Returns the smaller of two times; used to clamp lifecycle logs so they never land after now.
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    // Builds a DateReported for a month offset and slot, following the design's day/time/clamp rules, and
    // guarantees the result stays in [windowStart, now].
    private static DateTime SlotDate(DateTime now, DateTime windowStart, int monthOffset, int slot)
    {
        // The first day of the target month at 00:00.
        var monthStart = FirstOfMonth(now).AddMonths(-monthOffset);

        // Day of month per slot; the current month is capped at today's day.
        var slotDays = new[] { 2, 6, 11, 16, 21, 26 };
        var day = monthOffset == 0 ? Math.Min(slotDays[slot], now.Day) : slotDays[slot];

        // Hour cycles through 07..19 and minute is a fixed spread, so months 1-5 alone cover all 13 hours.
        var slotHours = Enumerable.Range(7, 13).ToArray();
        var hour = slotHours[(monthOffset * 6 + slot) % 13];
        var minute = (monthOffset * 17 + slot * 11) % 60;

        var candidate = monthStart.AddDays(day - 1).AddHours(hour).AddMinutes(minute);
        if (candidate <= now)
        {
            return candidate;
        }

        // Clamp for the current month when the slot time would fall after now (design's three cases).
        var sevenToday = new DateTime(now.Year, now.Month, now.Day, 7, 0, 0);
        if (now >= sevenToday)
        {
            return sevenToday.AddTicks((now - sevenToday).Ticks * (slot + 1) / 7);
        }
        if (now.Day > 1)
        {
            return monthStart.AddDays(now.Day - 2).AddHours(hour).AddMinutes(minute);
        }
        return monthStart.AddTicks((now - monthStart).Ticks * (slot + 1) / 7);
    }

    // Creates a User with the given fields and a hashed password; no Id and no CreatedAt yet (set later).
    private static User MakeUser(string fullName, string email, string? studentId,
        UserRole role, VerificationStatus status, string password, IPasswordHasher<User> hasher)
    {
        var user = new User
        {
            FullName = fullName,
            Email = email,
            StudentId = studentId,
            Role = role,
            VerificationStatus = status
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }

    // Builds an Approved claim: filed daysAfter the item's report, reviewed one day later, wired to both users.
    private static Claim MakeApprovedClaim(Item item, User claimant, User reviewer, DateTime now,
        string proof, int daysAfter)
    {
        var filed = ClampToNow(item.DateReported.AddDays(daysAfter), item.DateReported, now);
        var reviewed = ClampToNow(filed.AddDays(1), filed, now);
        var claim = new Claim
        {
            Item = item,
            ClaimantUser = claimant,
            ReviewedByUser = reviewer,
            ProofDescription = proof,
            DateFiled = filed,
            ReviewedAt = reviewed,
            Status = ClaimStatus.Approved
        };
        LinkClaim(claim, item, claimant, reviewer);
        return claim;
    }

    // Builds a Rejected claim with a reason, timed like an Approved claim and wired to both users.
    private static Claim MakeRejectedClaim(Item item, User claimant, User reviewer, DateTime now,
        string proof, string reason, int daysAfter)
    {
        var filed = ClampToNow(item.DateReported.AddDays(daysAfter), item.DateReported, now);
        var reviewed = ClampToNow(filed.AddDays(1), filed, now);
        var claim = new Claim
        {
            Item = item,
            ClaimantUser = claimant,
            ReviewedByUser = reviewer,
            ProofDescription = proof,
            DateFiled = filed,
            ReviewedAt = reviewed,
            RejectionReason = reason,
            Status = ClaimStatus.Rejected
        };
        LinkClaim(claim, item, claimant, reviewer);
        return claim;
    }

    // Builds a Pending claim (no reviewer, no ReviewedAt), filed one day after the item's report.
    private static Claim MakePendingClaim(Item item, User claimant, DateTime now, string proof)
    {
        var filed = ClampToNow(item.DateReported.AddDays(1), item.DateReported, now);
        var claim = new Claim
        {
            Item = item,
            ClaimantUser = claimant,
            ProofDescription = proof,
            DateFiled = filed,
            Status = ClaimStatus.Pending
        };
        LinkClaim(claim, item, claimant, null);
        return claim;
    }

    // Adds a claim to the item's and users' navigation collections so SeedAsync inserts it through fixup.
    private static void LinkClaim(Claim claim, Item item, User claimant, User? reviewer)
    {
        item.Claims.Add(claim);
        claimant.FiledClaims.Add(claim);
        reviewer?.ReviewedClaims.Add(claim);
    }

    // Returns target, or the midpoint between floor and now when the fixed offset would pass now, so the
    // claim timeline stays ordered and never crosses into the future.
    private static DateTime ClampToNow(DateTime target, DateTime floor, DateTime now)
    {
        if (target <= now)
        {
            return target;
        }
        return floor.AddTicks((now - floor).Ticks / 2);
    }

    // Builds an ActivityLog row with a navigation-linked user and item, using ActivityLogger.FormatResult.
    private static ActivityLog MakeLog(DateTime timestamp, User? user, string action, Item? targetItem,
        LogOutcome outcome, string description)
    {
        return new ActivityLog
        {
            Timestamp = timestamp,
            User = user,
            Action = action,
            TargetItem = targetItem,
            Result = ActivityLogger.FormatResult(outcome, description)
        };
    }
}
