# Implementation Plan: HeronSearch Admin

## Overview

HeronSearch Admin is built in C# as one ASP.NET Core MVC project (Razor views) on net10.0, with EF Core 10 and the Oracle provider MySql.EntityFrameworkCore 10.0.9, Bootstrap 5, Bootstrap Icons, and Chart.js. The work runs in two phases:

- Phase 1 (tasks 1–20): solution, packages, and offline libraries; the complete data model and seed data; helpers; sign-in and access control; layout and branding; Login, Dashboard, and Manage Items; placeholders for the Phase 2 sections; the single InitialCreate migration; the README. Phase 1 ends at the STOP checkpoint (task 20).
- Phase 2 (tasks 21–29): Manage Claims, Manage Users, Reports / History, About, the full Logout flow, and the README update, with zero new migrations. Phase 2 starts only after the user replies "continue".

Read the Notes section before starting: it lists the only commands allowed. Required verification is `dotnet build` with zero errors. Test tasks are optional (`*`) and never block the required path.

Ground rules for every code task:

- Plain MVC folders: Models, Controllers, Views, ViewModels, Data (AppDbContext, DbSeeder), Helpers, wwwroot.
- `AppDbContext` is injected straight into controllers; rules live in small static helpers. No repositories, service layers, MediatR, AutoMapper, or generic layers.
- A short comment directly above every class and every method that says what it does, including the generated migration classes and methods.
- LINQ queries only (always parameterized); Razor `@` output only, never `Html.Raw` with user data; every POST form carries an anti-forgery token.
- Exact versions for every package and library, zero CDN URLs in pages, and never the Forbidden_Name (see Notes).

## Tasks

### Phase 1: Setup, data model, sign-in, layout, Dashboard, Manage Items

- [x] 1. Check the tools and set up the solution, packages, and offline libraries
  - [x] 1.1 Check the installed .NET SDK
    - Run `dotnet --version` and `dotnet --list-sdks` (read-only).
    - Confirm a 10.0.x SDK is listed. If none is, stop and ask the user to install the .NET 10 SDK; install nothing yourself.
    - _Requirements: 27.1, 1.2_
  - [x] 1.2 Create the classic solution and the MVC project
    - In the workspace root run `dotnet new sln --format sln --name HeronSearch` (classic `HeronSearch.sln`, not `.slnx`) and `dotnet new mvc --name HeronSearch.Admin --output HeronSearch.Admin --framework net10.0`.
    - Edit `HeronSearch.sln` by hand: one `Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "HeronSearch.Admin", "HeronSearch.Admin\HeronSearch.Admin.csproj", "{new GUID}"` entry plus its Debug/Release `Any CPU` lines in `ProjectConfigurationPlatforms`. Do not use `dotnet sln add`.
    - Keep the template files for now so 1.4 can build them; they are removed in 6.3 and 9.1 once their replacements exist.
    - _Requirements: 1.1, 1.4, 27.1, 27.3_
  - [x] 1.3 Add the local tool manifest with dotnet-ef 10.0.9
    - In the workspace root run `dotnet new tool-manifest`, then `dotnet tool install dotnet-ef --version 10.0.9` (local tool, never `--global`).
    - Confirm `.config/dotnet-tools.json` pins `dotnet-ef` at `10.0.9`.
    - _Requirements: 2.3, 27.1_
  - [x] 1.4 Pin the NuGet packages by hand and restore
    - Edit `HeronSearch.Admin.csproj` to match the design: exactly one `<TargetFramework>net10.0</TargetFramework>` (no `TargetFrameworks`), Nullable, ImplicitUsings, RootNamespace `HeronSearch.Admin`, `MySql.EntityFrameworkCore` 10.0.9, `Microsoft.EntityFrameworkCore.Design` 10.0.9 (`PrivateAssets` all), and `<Content Remove="wwwroot\uploads\**" />`. Do not use `dotnet add package`.
    - Run `dotnet restore`, then `dotnet build` once to confirm the template compiles with the pinned packages. Note any NuGet audit warnings for the Phase 1 summary.
    - _Requirements: 1.2, 1.3, 2.1, 2.2, 27.3, 1.8_
  - [x] 1.5 Download Bootstrap Icons and Chart.js into wwwroot/lib
    - Check the latest stable Bootstrap Icons 1.x and Chart.js 4.x versions at implementation time (for example the npm registry `latest` tag) and pin them.
    - Bootstrap Icons: `font/bootstrap-icons.min.css`, `font/fonts/bootstrap-icons.woff2`, `font/fonts/bootstrap-icons.woff`, and `LICENSE` into `wwwroot/lib/bootstrap-icons/`, keeping the folder structure so the CSS font URLs resolve.
    - Chart.js: the UMD minified build and `LICENSE.md` into `wwwroot/lib/chart.js/dist/`, saved as `chart.umd.min.js`. If the package names the minified UMD file `chart.umd.js`, save it as `chart.umd.min.js` and note this for the README.
    - Keep Bootstrap 5.3.x, jQuery, jQuery Validation, and jQuery Validation Unobtrusive from the template; read their exact versions from the file headers for the README and the summary.
    - _Requirements: 1.5, 9.17, 27.4_
  - [x] 1.6 Write appsettings.json
    - Set `ConnectionStrings:DefaultConnection` to exactly `Server=localhost;Port=3307;Database=heronsearch;User=root;Password=YOUR_PASSWORD_HERE;` and `Branding:LogoPath` to `/images/heronsearch-logo.svg`; keep the template `Logging` and `AllowedHosts` sections.
    - No XAMPP or MariaDB settings anywhere; `appsettings.Development.json` holds no connection string.
    - _Requirements: 2.4, 2.6, 9.14_

- [x] 2. Create the data model and AppDbContext
  - [x] 2.1 Create the enums, entities, and fixed lists
    - `Models/Enums.cs` (UserRole, VerificationStatus, ItemType, ItemStatus, ClaimStatus) and `Models/User.cs`, `Item.cs`, `Claim.cs`, `ActivityLog.cs` with the design's fields and nullable navigation properties.
    - `Models/ItemLists.cs`: `Categories`, `Locations`, and `IconFor(category)`, the only place these values are defined.
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 1.4_
  - [x] 2.2 Configure AppDbContext with the complete schema
    - `Data/AppDbContext.cs`: DbSets `Users`, `Items`, `Claims`, `ActivityLogs`. In `OnModelCreating`, follow the design tables: required and optional fields, max lengths (including PasswordHash 256 and enum columns 20), `HasConversion<string>()` for the five enums, a unique index on `User.Email`, indexes on `Item.DateReported` and `ActivityLog.Timestamp`, and `Claim.Status` as a concurrency token.
    - Foreign keys: Item.ReportedByUserId, Claim.ItemId, Claim.ClaimantUserId, and Claim.ReviewedByUserId use `Restrict`; ActivityLog.UserId and ActivityLog.TargetItemId use `SetNull` (no inverse collections). No `HasData`.
    - This is the final schema; Phase 2 adds zero migrations.
    - _Requirements: 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12, 1.7, 27.6_

- [x] 3. Write the core helpers
  - [x] 3.1 Create DisplayFormat, FilterValues, and Policies
    - `Helpers/DisplayFormat.cs`: `ShortDate` ("MMM d, yyyy"), `DateAndTime` ("MMM d, yyyy h:mm tt"), and `MonthLabel` ("MMM yyyy") with the invariant culture; `StatusBadgeClass(status)` maps each status name to its `hs-badge-*` class (unknown → archived gray).
    - `Helpers/FilterValues.cs`: `MatchAllowedValue(input, allowed)` and `NormalizeSearch(term)` (trim, first 100 characters, null when blank).
    - `Helpers/Policies.cs`: the `BackOffice` and `AdminOnly` policy names.
    - _Requirements: 9.12, 9.13, 9.18, 7.10, 12.6, 6.2, 6.3_
  - [ ]* 3.2 Create the optional test project
    - Only when the user opts in to tests. Write `HeronSearch.Admin.Tests/HeronSearch.Admin.Tests.csproj` by hand: one `<TargetFramework>net10.0</TargetFramework>`, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, exact pinned versions of Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, and FsCheck.Xunit, and a project reference to `..\HeronSearch.Admin\HeronSearch.Admin.csproj`. Add it to `HeronSearch.sln` by editing the file, then run `dotnet restore`.
    - Conventions: one file per property (`PropertyNN_<Title>Tests.cs`), at least 100 generated cases each, the tag comment `// Feature: heronsearch-admin, Property {n}: {title}`, a fake `IPasswordHasher<User>`, no database, no web host. `dotnet test` runs only with the user's approval.
    - _Requirements: 1.3, 27.1_
  - [ ]* 3.3 Write property test for query-string values
    - **Property 3: Query values fall back to the default**
    - **Validates: Requirements 7.10, 12.7, 18.1, 20.3**
  - [ ]* 3.4 Write property test for search term normalization
    - **Property 4: Search term normalization**
    - **Validates: Requirements 12.6**
  - [ ]* 3.5 Write property test for status badges
    - **Property 24: Status badges**
    - **Validates: Requirements 9.12, 9.13**
  - [ ]* 3.6 Write property test for the date format
    - **Property 25: Date format is culture-independent**
    - **Validates: Requirements 9.18**
  - [x] 3.7 Create BusinessRules with the Phase 1 rules
    - `Helpers/BusinessRules.cs`: `EmailPattern` and `PasswordPattern` with their messages, `NormalizeEmail`, `CombineDateWithTime`, `CanMarkReturned`/`CheckMarkReturned`, `CanArchive`/`CheckArchive`, `CheckItemDelete`, and the pending-claims message, using the exact texts and check order of the design's Business rules table.
    - The claim and user rules are added in Phase 2 (tasks 21.1 and 22.1).
    - _Requirements: 5.10, 5.11, 7.14, 12.3, 14.4, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 16.4_
  - [ ]* 3.8 Write property test for email normalization
    - **Property 1: Email normalization**
    - **Validates: Requirements 5.11, 7.14**
  - [ ]* 3.9 Write property test for the password rule
    - **Property 2: Password rule**
    - **Validates: Requirements 21.9, 4.4, 4.6**
  - [ ]* 3.10 Write property test for the Mark as Returned and Archive rules
    - **Property 6: Mark as Returned and Archive rules**
    - **Validates: Requirements 12.3, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6**
  - [ ]* 3.11 Write property test for the item delete rule
    - **Property 7: Deletes are blocked exactly when dependents exist** (item part, `CheckItemDelete`; the user part is added in 22.3)
    - **Validates: Requirements 16.3, 16.4**
  - [x] 3.12 Create ActivityLogger
    - `Helpers/ActivityLogger.cs`: `LogOutcome` (Success, Failed, Blocked), `LogActions` constants for all 14 actions, a pure `FormatResult(outcome, description)` ("{Outcome}: {description}", cut to 500 characters), and `Log(db, userId, action, targetItemId, outcome, description)`, which adds the row with `DateTime.Now` and does not save.
    - No code path edits or deletes ActivityLog rows.
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 5.16, 16.6_
  - [ ]* 3.13 Write property test for the log Result format
    - **Property 16: Activity log Result format**
    - **Validates: Requirements 8.3, 8.2, 16.6**

- [x] 4. Write the upload, paging, and chart helpers
  - [x] 4.1 Create ImageUploadValidator
    - `Helpers/ImageUploadValidator.cs`: pure `Check(fileName, length, header)` (order: empty, then over 5,242,880 bytes, then extension or signature mismatch, with the exact messages), `ValidateAsync(IFormFile)`, pure `BuildStoredName(extension)` (GUID "N" plus the lowercase extension), `SaveAsync(file, webRootPath)` (creates `wwwroot/uploads` when missing, `FileMode.CreateNew`, returns `/uploads/{name}`), pure `ResolveUploadPath`, and `DeleteUploadedFile` (uploads folder only, a missing file is ignored).
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7, 16.5, 14.8_
  - [ ]* 4.2 Write property test for photo validation
    - **Property 12: Photo validation**
    - **Validates: Requirements 17.1, 17.2, 17.3, 17.4**
  - [ ]* 4.3 Write property test for stored file names
    - **Property 13: Stored names ignore the original file name**
    - **Validates: Requirements 17.5, 17.6**
  - [ ]* 4.4 Write property test for photo deletion paths
    - **Property 14: Photo deletion stays inside wwwroot/uploads**
    - **Validates: Requirements 16.5, 14.8, 16.3**
  - [ ]* 4.5 Write unit tests for saving and deleting photos in a temp folder
    - `SaveAsync` creates the missing uploads folder and returns `/uploads/<name>`; `DeleteUploadedFile` ignores paths outside the folder and missing files.
    - _Requirements: 17.5, 17.6, 17.7, 16.5_
  - [x] 4.6 Create PagerViewModel, ChartViewModel, and ChartData (Phase 1 part)
    - `ViewModels/PagerViewModel.cs` with `Create(requestedPage, totalCount, pageSize, noun)` and the design's clamping formulas, plus `RouteValues` for the current filters.
    - `ViewModels/ChartViewModel.cs` (Title, Kind, Labels, Series, IsEmpty, AltText, `ToJson()` per the chart data contract).
    - `Helpers/ChartData.cs`: `SixMonthWindow(now)`, `MonthlySeries(months, rows)`, `TopCounts(names, take)`, and `Describe(chart)`. `MonthsInRange`, `ResolveRange`, and `HourlyCounts` are added in Phase 2 (task 24.1).
    - _Requirements: 12.10, 12.12, 11.3, 11.4, 11.7, 10.13_
  - [ ]* 4.7 Write property test for paging
    - **Property 5: Paging is clamped and the range text is consistent**
    - **Validates: Requirements 12.10, 12.12, 18.4, 20.4, 23.4**
  - [ ]* 4.8 Write property test for the monthly series
    - **Property 17: Monthly series**
    - **Validates: Requirements 11.2, 11.3, 22.5**
  - [ ]* 4.9 Write property test for grouped counts
    - **Property 18: Grouped counts are complete and ordered**
    - **Validates: Requirements 11.4, 22.4, 22.7**
  - [ ]* 4.10 Write property test for chart text alternatives
    - **Property 23: Chart text alternative**
    - **Validates: Requirements 10.13**

- [x] 5. Write the DbSeeder
  - [x] 5.1 Create DbSeeder.BuildDemoData
    - `Data/DbSeeder.cs`: pure `BuildDemoData(DateTime now, IPasswordHasher<User> hasher)` that returns the linked object graph (no Ids, no database), following the design's tables and its date placement, claim timing, and CreatedAt rules.
    - 12 users: demo Admin `admin@heronsearch.local` / `HeronAdmin2026`, Staff `ramon.delacruz@heronsearch.local` / `HeronStaff2026`, Staff Liza Mercado with zero dependents, and 9 fictional students (6 Verified, 2 Pending, 1 Suspended).
    - 36 items (6 per window month: 3 Lost and 3 Found, ItemLists values only, every status), 10 claims on Found items (4 Pending, 3 Approved, 3 Rejected), and at least 20 logs.
    - Every password goes through `hasher.HashPassword`; log Results use `ActivityLogger.FormatResult`; only fictional names and `heronsearch.local` emails.
    - _Requirements: 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11, 4.12, 4.13, 4.14, 4.15, 4.16, 4.17_
  - [x] 5.2 Create DbSeeder.SeedAsync
    - Create a scope; return without changes when `db.Users.AnyAsync()` is true; otherwise add the whole graph and save once.
    - Catch every exception and `LogError` the hint: check the MySQL password in appsettings.json, then run `dotnet tool restore` and `dotnet ef database update`. Startup continues.
    - _Requirements: 4.1, 4.2, 4.18_
  - [ ]* 5.3 Write property test for demo data dates
    - **Property 21: Demo data dates** (generate `now` values that include the 1st of a month before 07:00)
    - **Validates: Requirements 4.10, 4.11, 4.16, 4.17**
  - [ ]* 5.4 Write property test for demo data consistency
    - **Property 22: Demo data consistency**
    - **Validates: Requirements 4.4, 4.5, 4.6, 4.7, 4.9, 4.12, 4.13, 4.14, 4.15**
  - [ ]* 5.5 Write unit tests for the demo passwords
    - Each demo password verifies with the real `PasswordHasher<User>` and matches `PasswordPattern`.
    - _Requirements: 4.4, 4.6, 4.8_

- [x] 6. Wire Program.cs, authentication, and the base controllers
  - [x] 6.1 Create SessionValidator
    - `Helpers/SessionValidator.cs`: `ValidateAsync(CookieValidatePrincipalContext)` reads the Id and Role claims and loads the user by primary key with `AsNoTracking`. It rejects the principal and signs out when the row is missing, the role differs, or the status is not Verified. A database exception is logged as a warning and fails closed. A valid non-persistent ticket sets `ShouldRenew = true`.
    - _Requirements: 6.6, 6.7, 5.14_
  - [x] 6.2 Write Program.cs
    - Services, following the design pipeline: `AddControllersWithViews` with the global `AutoValidateAntiforgeryTokenAttribute`; `AddDbContext` with `UseMySQL(DefaultConnection)` (add `using MySql.EntityFrameworkCore.Extensions;` if needed, never `MySqlServerVersion`); the `PasswordHasher<User>` singleton; cookie auth (HttpOnly, SameSite Lax, 8-hour lifetime, no sliding expiration, `OnValidatePrincipal = SessionValidator.ValidateAsync`, status 403 on access denied); the BackOffice and AdminOnly policies.
    - After `app.Build()`: `/Error/500` exception handler and HSTS outside Development, `UseStatusCodePagesWithReExecute("/Error/{0}")`, HTTPS redirection, the `/uploads` static-file provider with `nosniff`, routing, authentication, authorization, `MapStaticAssets`, the default route `{controller=Dashboard}/{action=Index}/{id?}`, then `DbSeeder.SeedAsync` inside `if (!EF.IsDesignTime)`, then `app.Run()`.
    - Comment each block of the top-level statements.
    - _Requirements: 2.5, 2.7, 4.3, 5.14, 5.15, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.7, 7.12, 7.13, 1.7_
  - [x] 6.3 Create BackOfficeController, ErrorController, and NotFoundViewModel
    - `Controllers/BackOfficeController.cs`: abstract, `[Authorize(Policy = Policies.BackOffice)]`, with `CurrentUserId`, `IsAdmin`, `ShowSuccess`/`ShowError` (TempData `Success`/`Error`), `RecordNotFound(listName, listUrl)` (404 and the NotFound view), and `RedirectToLocal(returnUrl, fallbackAction)`.
    - `Controllers/ErrorController.cs`: `[AllowAnonymous]`, `[IgnoreAntiforgeryToken]`, any verb, route `/Error/{code:int}`, returning NotFound (404), AccessDenied (403), BadRequest (400), or the generic Error view.
    - `ViewModels/NotFoundViewModel.cs` (`ListName`, `ListUrl`).
    - Delete the template `Controllers/HomeController.cs`, `Models/ErrorViewModel.cs`, `Views/Home/*`, and `Views/Shared/Error.cshtml` (the new error views come in 9.4).
    - _Requirements: 6.2, 6.4, 7.2, 7.11, 7.12, 7.13, 10.1, 10.2_

- [x] 7. Checkpoint: build the data model, helpers, and pipeline
  - Ensure `dotnet build` reports zero errors; fix every error and rebuild until it does. Ask the user if questions arise.

- [x] 8. Create the branding and front-end assets
  - [x] 8.1 Create the SVG logo recreation
    - `wwwroot/images/heronsearch-logo.svg` (`viewBox="0 0 120 160"`, `role="img"`, `<title>HeronSearch</title>`): maroon tall rounded arch outline, gold rays and a gold four-point star at the peak, a maroon heart rising out of an open maroon-outlined box, and a gold arc at the base. Plain paths only, no fonts or external references.
    - _Requirements: 9.15, 9.14_
  - [x] 8.2 Write site.css
    - Replace the template `wwwroot/css/site.css` with the design's sections: brand variables (the five Brand_Palette colors plus the helper colors); Bootstrap overrides (primary Maroon for buttons, outline buttons, links, background and text utilities, focus rings, checked checkboxes, active pagination, and active nav pills; success Deep Green); base (page background, `:focus-visible`, skip link, reduced motion).
    - Layout (top bar, shell, sidebar with the Cream active state and Maroon bar, main, footer), components (`.hs-card`, `.hs-table`, `.hs-search`, `.hs-chip`, `.hs-password`, stat cards, thumbnails, empty state, chart box), status badges, the login card, and responsive rules (offcanvas sidebar below 992px, no page overflow at 360px).
    - _Requirements: 9.5, 9.8, 9.9, 9.10, 9.11, 9.12, 10.8, 10.11, 10.15_
  - [x] 8.3 Write site.js
    - Replace the template `wwwroot/js/site.js` with vanilla JavaScript: password toggles (`[data-password-toggle]` switches the input type, swaps `bi-eye`/`bi-eye-slash`, and switches `aria-label` between "Show password" and "Hide password") and the `#confirmModal` `show.bs.modal` handler (reads the trigger's `data-confirm-*` values, sets text with `textContent`, accepts only actions that start with `/`).
    - _Requirements: 5.3, 10.4, 10.5, 10.9, 10.10_
  - [x] 8.4 Write charts.js
    - `wwwroot/js/charts.js`: builds a Chart.js config for each `canvas.js-chart` from its `data-chart` JSON (`line`, `bar`, and `horizontalBar` as `bar` with `indexAxis: 'y'`), series colors Maroon, Gold, Deep Green, Sand read from the CSS variables, integer ticks, a legend only for two or more series, and no animation under `prefers-reduced-motion`.
    - _Requirements: 11.3, 11.4, 11.7_
  - [ ]* 8.5 Write unit tests for the palette contrast ratios
    - Compute the WCAG contrast ratio of each badge and text color pair from the design and assert at least 4.5:1.
    - _Requirements: 10.11, 9.12_

- [x] 9. Build the layouts, shared partials, and error pages
  - [x] 9.1 Create the layouts, top bar, and footer
    - `Views/_ViewImports.cshtml` (namespaces Models, ViewModels, Helpers; tag helpers) and `Views/_ViewStart.cshtml`.
    - `Views/Shared/_Layout.cshtml` (Admin_Layout: "Skip to main content" link first, `_TopBar`, `_Sidebar`, `main#main-content` with `_Alerts`, `_Footer`, `_ConfirmModal`, local scripts, `RenderSection("Scripts")`) and `_MinimalLayout.cshtml` (top bar with the logo only, centered main, footer). Both load only local CSS, JS, and fonts and drop the `HeronSearch.Admin.styles.css` link.
    - `_TopBar.cshtml`: menu toggle (`d-lg-none`, `aria-label="Open menu"`), logo from `Branding:LogoPath` with the safe fallback, "HeronSearch", and the signed-in full name and role. `_Footer.cshtml`: "© 2026 HeronSearch · AnagolayCorp".
    - Delete the template `Views/Shared/_Layout.cshtml.css` and `wwwroot/favicon.ico`; the layouts use the logo as the page icon.
    - _Requirements: 9.1, 9.2, 9.5, 9.6, 9.7, 9.14, 9.17, 10.12, 1.5_
  - [x] 9.2 Create the Sidebar (Phase 1 version)
    - `Views/Shared/_Sidebar.cshtml`: `nav.offcanvas-lg.offcanvas-start#sidebar` with Dashboard, Manage Items, Manage Claims, Manage Users (Admin only), Reports / History, About, and Logout in this order, each with its Bootstrap Icon (`aria-hidden="true"`).
    - The active section comes from the route's controller value (sub-pages included) and gets `.active` and `aria-current="page"`.
    - Logout in Phase 1: a `<form method="post" asp-controller="Account" asp-action="Logout">` (anti-forgery token included) with a button styled as a nav link.
    - _Requirements: 9.3, 9.4, 9.5, 6.5, 27.8, 7.1, 7.3_
  - [x] 9.3 Create the shared partials
    - `_Alerts`, `_StatusBadge`, `_EmptyState`, `_ConfirmModal` (POST form with `asp-antiforgery="true"` and a hidden `returnUrl`), `_Pager` ("Showing X–Y of Z {noun}", Previous, page numbers, Next, filters kept with `asp-all-route-data`), `_ItemPhoto` (photo or category-icon placeholder), `_Chart` (empty state, or `canvas.js-chart` with `role="img"` and the alt text as `aria-label`), and `_ValidationScriptsPartial` (jQuery Validation, Unobtrusive, `~/js/validation-rules.js`).
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.7, 10.13, 9.12, 9.13, 12.4, 12.10, 12.11, 7.1_
  - [x] 9.4 Create the error views
    - `Views/Shared/NotFound.cshtml` (link to the related list or the Dashboard), `AccessDenied.cshtml` (link to the Dashboard), `BadRequest.cshtml`, and `Error.cshtml` (generic text, no technical details). Each uses `_Layout` for signed-in users and `_MinimalLayout` for signed-out visitors.
    - _Requirements: 6.4, 7.2, 7.11, 7.12, 7.13_

- [x] 10. Build the Login page and sign-in
  - [x] 10.1 Create LoginViewModel and AccountController.Login
    - `ViewModels/LoginViewModel.cs` with the design's annotations ("Email Address", required, 150 characters, `EmailPattern`; Password required; RememberMe; ReturnUrl).
    - `Controllers/AccountController.cs`: `[AllowAnonymous]` Login GET (a signed-in Admin or Staff user goes to the Dashboard) and POST following the design's seven sign-in steps: normalized email, `PasswordHasher` check (rehash on `SuccessRehashNeeded`), the exact error texts for wrong credentials, Student, Pending, and Suspended accounts, the cookie claims, a 14-day persistent cookie only with Remember me, local return URLs only, and one `Login` log (Success or Failed, names the email, never the password). Write `System.Security.Claims.Claim` in full.
    - _Requirements: 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12, 5.13, 5.14, 5.16, 5.17, 7.4, 7.5, 7.14_
  - [x] 10.2 Create the Login view
    - `Views/Account/Login.cshtml` on `_MinimalLayout`: a centered `.hs-card` with the logo (`alt="HeronSearch"`), "Sign in", Email Address, Password with the toggle button inside the field (`type="button"`, `aria-label="Show password"`, `aria-controls`), Remember me, a full-width "Sign In" button, field messages, and the validation scripts. No Google sign-in, sign-up, or forgot-password controls.
    - _Requirements: 5.1, 5.2, 5.3, 5.7, 5.10, 7.1, 7.6, 10.6, 10.7_
  - [ ]* 10.3 Write unit tests for the sign-in input rules
    - LoginViewModel validation (empty, invalid format, over 150 characters, empty password) and the return URL choice (`/Items?page=2` is kept; `https://evil.example`, `//evil.example`, and `/\evil.example` go to the Dashboard).
    - _Requirements: 5.5, 5.6, 5.10_

- [x] 11. Build the Dashboard
  - [x] 11.1 Create DashboardViewModel and DashboardController
    - `ViewModels/DashboardViewModel.cs` and `Controllers/DashboardController.cs` (inherits BackOfficeController, injects AppDbContext): five `CountAsync` totals (Total Lost and Total Found count every status), the monthly chart from `ChartData.MonthlySeries(SixMonthWindow(now), rows)`, the top 5 locations from `TopCounts(locations, 5)`, the 5 newest Pending claims, and the 10 newest logs (a null user shows "Unknown user" in the view).
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 1.7_
  - [x] 11.2 Create the Dashboard view
    - `Views/Dashboard/Index.cshtml`: heading, five stat cards, the line chart and the top 5 horizontal bar chart through `_Chart`, the Pending Claims card (Item, Claimant, Date Filed, a Review link to `/Claims/Review/{id}`, and "View all" to `/Claims`), and the Recent Activity card. Tables use `th scope="col"`; empty lists show the empty state; `@section Scripts` loads `~/lib/chart.js/dist/chart.umd.min.js` and `~/js/charts.js`.
    - _Requirements: 11.1, 11.3, 11.4, 11.5, 11.6, 11.7, 10.3, 10.13, 10.14, 9.18_

- [x] 12. Checkpoint: build sign-in, layout, and Dashboard
  - Ensure `dotnet build` reports zero errors; fix every error and rebuild until it does. Ask the user if questions arise.

- [x] 13. Build the Manage Items list and details modal
  - [x] 13.1 Create ItemListViewModel and ItemsController.Index
    - `ViewModels/ItemListViewModel.cs` (rows with ReporterName, ClaimCount, CanMarkReturned, and CanArchive; the filters; Pager; IsAdmin).
    - `Controllers/ItemsController.cs` (inherits BackOfficeController): GET Index with `search` (`NormalizeSearch`, lowercase `Contains` on Name, Description, and Location), `status` and `type` (`MatchAllowedValue`), `category` (ItemLists), and `page`; sort by DateReported desc, then Id desc; `AsNoTracking` projection, `CountAsync`, 10 per page through `PagerViewModel.Create`.
    - _Requirements: 12.5, 12.6, 12.7, 12.8, 12.9, 12.10, 12.11, 12.12, 7.9, 7.10, 1.7_
  - [x] 13.2 Create the item list view and the details modal
    - `Views/Items/Index.cshtml`: the "Manage Items" heading with the rounded "+ Add Found Item" button on the right; a GET filter form (search with icon and visually-hidden label, status Pill_Chips with `aria-current="true"` on the active chip, Category and Type selects); a `.table-responsive` table with Photo, Name, Category, Location, Date, Type, Status, Reporter, and Actions (`th scope="col"`); View and Edit actions with accessible names; `_Pager`; the empty state with "Clear filters" (`/Items`).
    - `Views/Items/_ItemDetailsModal.cshtml`: photo or placeholder, Name, Category, Location, Date Reported, Type, Status, Description, reporter, and claim count; a Close button; Escape closes it.
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.7, 12.13, 13.1, 13.2, 7.8, 10.3, 10.6, 10.7, 10.14, 10.15_

- [x] 14. Build Add Found Item and Edit with photo upload
  - [x] 14.1 Create the item form model and the validation attributes
    - `ViewModels/NotInFutureAttribute.cs` (server: `value.Date <= DateTime.Today` with "Date cannot be in the future."; client: `data-val-notfuture`) and `ViewModels/PhotoFileAttribute.cs` (server: `ImageUploadValidator`; client: max bytes, extensions, three messages), both implementing `IClientModelValidator`.
    - `ViewModels/ItemFormViewModel.cs` with the design's fields and rules.
    - _Requirements: 14.1, 14.3, 14.6, 14.9, 17.1, 17.2, 17.3, 17.4, 7.4, 7.6_
  - [x] 14.2 Write validation-rules.js
    - `wwwroot/js/validation-rules.js`: jQuery Validation methods and unobtrusive adapters `notfuture` (compares the `yyyy-MM-dd` value with the browser's today) and `photo` (checks empty file, size, then extension, and picks the matching message).
    - _Requirements: 7.6, 14.3, 17.2, 17.3, 17.4_
  - [x] 14.3 Add the Create and Edit actions to ItemsController
    - Create GET (Date Found = today) and POST: check Category and Location against ItemLists, save the photo through `ImageUploadValidator.SaveAsync`, save `Type = Found`, `Status = Active`, `DateReported = CombineDateWithTime(...)`, and `ReportedByUserId = CurrentUserId`, then log `Add Item` with the new Id and show a success alert.
    - Edit GET (prefilled; a missing Id returns `RecordNotFound`) and POST: same rules; without a new photo keep ImagePath; with a new photo save it, update ImagePath, then delete the old file through `DeleteUploadedFile`. Type and Status stay read-only. Log `Edit Item`.
    - A photo save error gives the field error "The photo could not be saved. Try again."; invalid input redisplays the form with the values kept.
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8, 14.9, 7.4, 7.5, 7.11, 8.1, 10.1_
  - [x] 14.4 Create the Create and Edit views
    - `Views/Items/_ItemFormFields.cshtml` (Name, Category select, Location select, Date Found `type="date"` with `max` = today, Description, Photo with `accept=".jpg,.jpeg,.png,.webp"`), `Views/Items/Create.cshtml`, and `Views/Items/Edit.cshtml` (current photo, read-only Type and Status badges). Forms use `enctype="multipart/form-data"`, labels use `asp-for`, and the pages include `_ValidationScriptsPartial`.
    - _Requirements: 14.1, 14.2, 14.5, 7.1, 7.5, 7.6, 10.6_
  - [ ]* 14.5 Write property test for item dates
    - **Property 26: Item dates are never in the future**
    - **Validates: Requirements 14.3, 14.4, 14.6**

- [x] 15. Build Mark as Returned, Archive, and Delete
  - [x] 15.1 Add the status and delete actions to ItemsController
    - POST `MarkReturned` and `Archive`: load the item (missing → `RecordNotFound`), count its Pending claims, and call `CheckMarkReturned` or `CheckArchive`. A refusal logs `Blocked` and shows an error alert; success sets the status, logs `Success`, and shows a success alert. Redirect to the local `returnUrl`.
    - POST `Delete` with `[Authorize(Policy = Policies.AdminOnly)]`: count all claims and call `CheckItemDelete` (refusal → `Blocked` log and error alert); otherwise remove the item, log `Delete Item` with `TargetItemId = null` and a Result that names the item and its Id, save, then delete the photo file. A `DbUpdateException` shows the same blocked message.
    - _Requirements: 15.2, 15.4, 15.5, 15.6, 16.3, 16.4, 16.5, 16.6, 6.3, 6.4, 7.3, 8.1, 8.2, 10.1, 10.2_
  - [x] 15.2 Add the action buttons to the item list
    - In `Views/Items/Index.cshtml`, show Mark as Returned and Archive by `CanMarkReturned`/`CanArchive`, and Delete to Admins only. Each opens `#confirmModal` through `data-confirm-*` attributes that name the item; Delete uses "Delete '[item name]'? This cannot be undone." The hidden `returnUrl` keeps the filters and page.
    - _Requirements: 12.3, 15.1, 15.3, 16.1, 16.2, 10.4, 10.5, 10.7_

- [x] 16. Add the Phase 2 placeholder pages and the direct POST logout
  - [x] 16.1 Create the placeholder controllers and view
    - `ClaimsController` (Index, `Review(id)`), `ReportsController` (Index), and `AboutController` (Index) inherit BackOfficeController; `UsersController` (Index) inherits it and adds the class-level AdminOnly policy. Each action returns `View("Placeholder")` with the section title.
    - `Views/Shared/Placeholder.cshtml`: heading plus "This page will be available in Phase 2."
    - _Requirements: 27.7, 6.2, 6.3, 6.4_
  - [x] 16.2 Add the Phase 1 Logout action
    - `AccountController.Logout`: `[HttpPost]` with the BackOffice policy; log `Logout`, sign out of the cookie scheme, and redirect to the Login page. The Sidebar form from 9.2 posts here.
    - _Requirements: 27.8, 7.3, 8.1_

- [x] 17. Create the InitialCreate migration (exactly once)
  - [x] 17.1 Review the model and build before migrating
    - Compare `AppDbContext` and the entities with the design tables (lengths, required and optional fields, string enums, unique Email, indexes, Restrict and SetNull, concurrency token). Run `dotnet build` until it reports zero errors.
    - _Requirements: 2.8, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 27.6_
  - [x] 17.2 Run `dotnet ef migrations add InitialCreate` once
    - Run it from `HeronSearch.Admin` (the local tool resolves from the root manifest). Never run it a second time, and never run `dotnet ef migrations remove` or `dotnet ef database update`.
    - Check the generated files: tables Users, Items, Claims, and ActivityLogs; varchar lengths; string enum columns; the unique Email index; `ReferentialAction.Restrict` and `SetNull`; nullable columns. If anything is wrong, stop and ask the user before changing anything.
    - _Requirements: 2.7, 2.8, 27.1, 27.2, 27.6_
  - [x] 17.3 Comment the generated migration code and rebuild
    - Add a one-line comment above each class and method in `<timestamp>_InitialCreate.cs` (class, `Up`, `Down`), `<timestamp>_InitialCreate.Designer.cs` (class, `BuildTargetModel`), and `AppDbContextModelSnapshot.cs` (class, `BuildModel`). Change no generated code. Run `dotnet build` until it reports zero errors.
    - _Requirements: 1.6, 1.8_

- [x] 18. Write the Phase 1 README
  - [x] 18.1 Create README.md in the workspace root
    - Plain beginner language with numbered steps, following the design's README outline: what the app is; prerequisites (Visual Studio Community 2026, the .NET 10 SDK, MySQL Server 8.0.46 on port 3307); setup in order (open `HeronSearch.sln`, replace `YOUR_PASSWORD_HERE`, `dotnet tool restore`, `dotnet ef database update` in `HeronSearch.Admin`, start the app, sign in as the demo Admin).
    - Demo accounts with the demo-only warning; both ways to replace the demo Admin; why net10.0 and MySql.EntityFrameworkCore 10.0.9 (Pomelo has no EF Core 10 release, .NET 8 support ends 2026-11-10, the Oracle provider does no server-version detection, and `new MySqlServerVersion(new Version(8, 0, 46))` is Pomelo-only and absent from the code); the license GPL-2.0-only WITH Universal-FOSS-exception-1.0.
    - The exact versions table, every file and folder with one line each, the logo swap with zero code changes, the Phase 1 pages, troubleshooting (wrong password, wrong port, missing database or tables, missing `dotnet ef`), and the reminder to keep the real password private.
    - _Requirements: 26.1, 26.2, 26.3, 26.4, 26.5, 26.6, 26.7, 26.8, 26.9, 26.10, 26.11, 26.12, 26.13_

- [x] 19. Verify Phase 1
  - [x] 19.1 Run the Phase 1 file checks
    - A comment directly above every class and method in every `.cs` file, hand-written and generated.
    - Zero CDN or external URLs in layouts, views, site.css, and the JS files; zero occurrences of the Forbidden_Name in any file written or generated during the build; an anti-forgery token in every POST form; no `Html.Raw` with user data; no `MySqlServerVersion` or Pomelo reference in code; no XAMPP or MariaDB settings; the exact connection string; exactly one `<TargetFramework>net10.0</TargetFramework>`; an exact version on every package.
    - Fix anything that fails.
    - _Requirements: 1.3, 1.6, 2.4, 2.6, 7.1, 7.8, 9.16, 9.17_
  - [x] 19.2 Run the final Phase 1 build
    - Run `dotnet build` on the solution; fix and rebuild until it reports zero errors. Do not run the app or connect to MySQL.
    - _Requirements: 1.8, 27.2, 27.5_

- [x] 20. STOP: Phase 1 checkpoint (wait for "continue")
  - [x] 20.1 Present the Phase 1 summary, then stop and wait
    - Framework: net10.0 and why (.NET 10 LTS; .NET 8 support ends 2026-11-10).
    - Provider: MySql.EntityFrameworkCore 10.0.9 and why Pomelo and `MySqlServerVersion` are not used (Pomelo has no EF Core 10 release; the Oracle provider does no server-version detection, so the migration was created without a database connection).
    - Pinned versions: Microsoft.EntityFrameworkCore.Design, dotnet-ef, Bootstrap, Bootstrap Icons, Chart.js, jQuery, and the validation scripts; any NuGet audit warnings from 1.4.
    - Files created, and what was verified: `dotnet build` with zero errors; the app was not run and never connected to MySQL.
    - Next steps for the user, in order: set the real password in `HeronSearch.Admin/appsettings.json`; run `dotnet tool restore` in the workspace root; run `dotnet ef database update` in `HeronSearch.Admin`; run the app from Visual Studio; sign in as `admin@heronsearch.local` / `HeronAdmin2026`.
    - Logo gap: the logo is an SVG recreation. To use the official PNG, copy it into `wwwroot/images` and set `Branding:LogoPath`; no code changes.
    - Then STOP. Do not mark this task complete and do not start task 21 or any later task until the user replies "continue".
    - _Requirements: 27.9, 27.10, 27.11, 27.12_

### Phase 2: Claims, Users, Reports / History, About, Logout

Do not start any task in this section until the user has replied "continue" at task 20.1.

- [x] 21. Build Manage Claims
  - [x] 21.1 Add the claim rules to BusinessRules
    - `CheckApprove` (fixed order: already reviewed, claimant suspended, item not Active, checklist incomplete), `ApplyApproval` (auto-rejects the other Pending claims with "Another claim for this item was approved.", sets the item to Claimed, returns the count), `IsValidRejectReason` (trimmed length 10 to 500), `CheckReject`, `ApplyRejection` (item unchanged), and the four checklist labels.
    - _Requirements: 19.2, 19.4, 19.5, 19.6, 19.7, 19.8, 19.10, 19.11, 19.12_
  - [ ]* 21.2 Write property test for the approval decision
    - **Property 9: Approval decision**
    - **Validates: Requirements 19.3, 19.4, 19.6, 19.7, 19.8, 19.12**
  - [ ]* 21.3 Write property test for the approval effects
    - **Property 10: Approval effects**
    - **Validates: Requirements 19.4, 19.5**
  - [ ]* 21.4 Write property test for rejection
    - **Property 11: Rejection**
    - **Validates: Requirements 19.10, 19.11**
  - [x] 21.5 Create the claims view models
    - `ViewModels/ClaimListViewModel.cs` (Tab, the three counts, rows, Pager) and `ViewModels/ClaimReviewViewModel.cs` with `ApproveClaimViewModel` (four checklist booleans) and `RejectClaimViewModel` (`Reason` [Required, StringLength(500, MinimumLength = 10)] with "Enter a reason of 10 to 500 characters.").
    - _Requirements: 18.1, 18.2, 19.1, 19.2, 19.9, 19.11, 19.14_
  - [x] 21.6 Replace the placeholder ClaimsController
    - Index: `tab` through `MatchAllowedValue` (default Pending), the three counts, Pending sorted by DateFiled desc and Approved/Rejected by ReviewedAt desc (then Id), 10 per page.
    - Review GET: item, reporter, claimant, and reviewer; read-only when not Pending; the suspended warning; a missing Id returns `RecordNotFound`.
    - Approve POST: `CheckApprove` refusal → `Blocked` log, error alert, back to Review. Success → `ApplyApproval`, `Approve Claim` log, save, redirect to the Pending tab with "Claim #N for 'Item' was approved."
    - Reject POST: trim the reason; an invalid reason redisplays Review with the field message and the modal reopened; a `CheckReject` refusal logs `Blocked`; success → `ApplyRejection`, `Reject Claim` log, Pending tab with a success alert.
    - A `DbUpdateConcurrencyException` shows "This claim was already reviewed by [name]."
    - _Requirements: 18.1, 18.3, 18.4, 19.1, 19.4, 19.5, 19.6, 19.7, 19.8, 19.10, 19.11, 19.12, 19.13, 19.14, 7.3, 7.10, 7.11, 8.1, 8.2_
  - [x] 21.7 Create the claims views
    - `Views/Claims/Index.cshtml`: Pending, Approved, and Rejected tabs with count badges (`aria-current="page"` on the active tab); a table with Claim #, Item (photo and name), Claimant (name and student ID), Date Filed, Status, and Action (Review or View); an empty state that names the tab; `_Pager`.
    - `Views/Claims/Review.cshtml`: the item card next to the claimant card with the proof and date filed; the suspended warning. Pending: the checklist form (Approve disabled until all four boxes are checked) and "Reject", which opens `#rejectModal` with the required "Reason for rejection" text area. Read-only: reviewer, reviewed at, and rejection reason.
    - Extend `site.js` (enable Approve when every `.js-check` is checked; open `#rejectModal` on load when `data-open-on-load` is set) and `site.css` (the review two-column layout).
    - _Requirements: 18.1, 18.2, 18.5, 18.6, 19.1, 19.2, 19.3, 19.7, 19.9, 19.14, 10.3, 10.6, 10.9, 10.10, 10.14_

- [x] 22. Build Manage Users (Admin only)
  - [x] 22.1 Add the user rules to BusinessRules
    - `CanVerify`/`CheckVerify`, `CanSuspend`/`CheckSuspend`, `CheckChangeRole`, and `CheckUserDelete`, with the self check first and the exact texts from the design.
    - _Requirements: 21.1, 21.2, 21.3, 21.5, 21.6, 21.7, 21.8_
  - [ ]* 22.2 Write property test for the account action rules
    - **Property 8: Account action rules**
    - **Validates: Requirements 21.1, 21.2, 21.3, 21.8**
  - [ ]* 22.3 Extend the delete rule property test with the user delete rule
    - **Property 7: Deletes are blocked exactly when dependents exist** (user part, `CheckUserDelete`)
    - **Validates: Requirements 21.6, 21.7**
  - [x] 22.4 Create the users view models
    - `ViewModels/UserListViewModel.cs` (rows with IsSelf, CanVerify, and CanSuspend; the filters; Pager), `ViewModels/AddStaffViewModel.cs` (Full Name 2 to 100, Email with `EmailPattern` and 150 max, Staff ID 20 max, Password with `PasswordPattern`, Confirm Password with `Compare` and "Passwords do not match."), and `ViewModels/ChangeRoleViewModel.cs` (Role, ReturnUrl).
    - _Requirements: 20.2, 21.4, 21.9, 21.12_
  - [x] 22.5 Replace the placeholder UsersController
    - Class-level AdminOnly policy. Index: `status` and `role` through `MatchAllowedValue`, search on FullName, Email, and StudentId, sort by CreatedAt desc then Id, 10 per page.
    - Create GET and POST: normalize the email; an existing email gives "An account with this email already exists." (also on a unique-index `DbUpdateException`); hash the password; Role Staff, Verified, `CreatedAt = DateTime.Now`; log `Add Staff`.
    - Verify, Suspend, ChangeRole, and Delete (POST): call the matching rule; a refusal logs `Blocked` with an error alert; success updates, logs `Success` (Change Role names the old and new role), and shows a success alert. Delete counts reported items (N) and filed plus reviewed claims (M).
    - _Requirements: 20.3, 20.4, 21.2, 21.3, 21.5, 21.6, 21.7, 21.8, 21.10, 21.11, 6.3, 6.4, 7.3, 7.10, 7.11, 7.14, 8.1, 8.2_
  - [x] 22.6 Create the users views
    - `Views/Users/Index.cshtml`: the "Manage Users" heading with the rounded "+ Add Staff Account" button; search, status Pill_Chips (All, Pending, Verified, Suspended), and the role select; a table with Name, Email, Student ID ("—" when empty), Role, Status, Joined, and Actions (Verify, Suspend, Change role, Delete through `#confirmModal`). The signed-in Admin's row shows "(you)" without Suspend, Change role, or Delete. Empty state with "Clear filters"; `_Pager`.
    - `Views/Users/_ChangeRoleModal.cshtml` (Role dropdown preselected with the current role) and `Views/Users/Create.cshtml` (both password fields with the toggle).
    - Extend `site.js` so the Change role modal preselects `data-current-role`.
    - _Requirements: 20.1, 20.2, 20.3, 20.5, 21.1, 21.4, 21.9, 10.4, 10.5, 10.6, 10.7, 10.14_
  - [ ]* 22.7 Write unit tests for Add Staff validation
    - AddStaffViewModel through `Validator.TryValidateObject`: the password rule, "Passwords do not match.", and the email format and length.
    - _Requirements: 21.9, 21.12_

- [x] 23. Checkpoint: build Claims and Users
  - Ensure `dotnet build` reports zero errors; fix every error and rebuild until it does. Ask the user if questions arise.

- [x] 24. Build Reports / History
  - [x] 24.1 Add the report functions to ChartData
    - `MonthsInRange(from, to)`, `ResolveRange(from?, to?, now)` (default: first day of the window to today; `from > to` gives the default range plus "From date must be on or before To date."), and `HourlyCounts(dates)` (24 buckets labeled "00" to "23").
    - _Requirements: 22.1, 22.2, 22.5, 22.6_
  - [ ]* 24.2 Write property test for the hour-of-day buckets
    - **Property 19: Hour-of-day buckets**
    - **Validates: Requirements 22.6**
  - [ ]* 24.3 Write property test for the report range
    - **Property 20: Report range and months**
    - **Validates: Requirements 22.1, 22.2, 22.5**
  - [x] 24.4 Create CsvExporter
    - `Helpers/CsvExporter.cs`: `Guard`, `Escape` (RFC 4180), `Build(header, rows)` (UTF-8 BOM, header row, commas, CRLF), and a pure `FileName(prefix, from, to)` for `heronsearch-items-YYYYMMDD-YYYYMMDD.csv` and `heronsearch-logs-YYYYMMDD-YYYYMMDD.csv`.
    - _Requirements: 23.5, 23.6, 23.7, 23.8_
  - [ ]* 24.5 Write property test for the CSV output
    - **Property 15: CSV round trip with formula guard**
    - **Validates: Requirements 23.7, 23.8**
  - [x] 24.6 Create ReportsViewModel and replace the placeholder ReportsController
    - Index: `tab` (overview or logs) and `from`/`to` through `ResolveRange`. Overview: six counts (items by DateReported, claims by DateFiled, inclusive by day) and four charts (location, month, hour, category). System Logs: logs in the range, search on the user's full name, Action, and Result, newest first, 20 per page.
    - `ExportItems` and `ExportLogs` reuse the filters without paging and return `File(bytes, "text/csv; charset=utf-8", fileName)`.
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6, 22.7, 23.1, 23.2, 23.3, 23.4, 23.5, 23.6, 7.9, 7.10_
  - [x] 24.7 Create the reports views
    - `Views/Reports/Index.cshtml` (heading "Reports / History", From/To GET form with the range error, Overview and System Logs tabs), `_Overview.cshtml` (six summary cards, four `_Chart`s with empty states, "Export to CSV"), and `_Logs.cshtml` (search; table with Timestamp, User ("Unknown user"), Action, Item ("—"), Result; `_Pager`; "Export to CSV"). Chart.js and `charts.js` load in `@section Scripts`.
    - _Requirements: 22.1, 22.2, 22.3, 22.8, 23.1, 23.2, 23.3, 23.4, 23.5, 23.6, 10.3, 10.13, 10.14_
  - [ ]* 24.8 Write unit tests for the CSV file names
    - `FileName` gives `heronsearch-items-20260501-20261006.csv` for that range, and the matching logs name.
    - _Requirements: 23.5, 23.6_

- [x] 25. Build the About page
  - [x] 25.1 Replace the placeholder AboutController and create the About view
    - `Views/About/Index.cshtml`: HeronSearch as UMak's Lost & Found Management System; the five features (Lost Item Reporting, Found Item Registration, Search and Filtering, User and Item Verification, Lost-and-Found Statistics), each with an icon and one sentence; the four roles (Administration, Staff/Personnel, Student/User, Guest), each with one sentence.
    - Logo meaning: Box = keeper of lost things, Light = guide for lost things, Heart = compassion to help, Arch = unity, protection, and a safe space for the community. Credit: "A project of AnagolayCorp, University of Makati." Add the About tile styles to `site.css`.
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5, 24.6_

- [x] 26. Build the Logout flow and update the Sidebar
  - [x] 26.1 Add the logout modal and switch the Sidebar
    - `Views/Shared/_LogoutModal.cshtml`: "Sign out of HeronSearch?" with "Cancel" and "Sign out" (POST form with an anti-forgery token to `/Account/Logout`). Render it in `_Layout.cshtml`; the Sidebar Logout entry becomes a button that opens `#logoutModal`.
    - _Requirements: 25.1, 25.2, 25.4, 9.3, 10.9, 10.10_
  - [x] 26.2 Add the signed-out page
    - `AccountController`: Logout now redirects to `SignedOut`; add the `[AllowAnonymous]` `SignedOut` GET action. `Views/Account/SignedOut.cshtml` on `_MinimalLayout`: "You've been signed out" and a "Sign in again" button that links to the Login page.
    - _Requirements: 25.2, 25.3_

- [x] 27. Clean up and update the README
  - [x] 27.1 Remove the Phase 1 placeholder
    - Delete `Views/Shared/Placeholder.cshtml` and confirm that no controller returns `View("Placeholder")`.
    - _Requirements: 27.7, 27.13_
  - [x] 27.2 Update README.md for Phase 2
    - Describe every page of both phases and list every file added or changed in Phase 2, one line each; note that Phase 2 adds no migration.
    - _Requirements: 26.10, 26.14_

- [x] 28. Verify Phase 2
  - [x] 28.1 Run the Phase 2 file checks
    - Repeat the 19.1 checks on every new and changed file. Confirm the `Migrations` folder still holds only InitialCreate and an unchanged snapshot.
    - _Requirements: 1.6, 7.1, 7.8, 9.16, 9.17, 27.6_
  - [x] 28.2 Run the final Phase 2 build
    - Run `dotnet build` on the solution; fix and rebuild until it reports zero errors.
    - _Requirements: 1.8, 27.5_

- [x] 29. Final checkpoint: present the Phase 2 summary
  - [x] 29.1 Present the Phase 2 summary
    - List the files created and changed in Phase 2 and the user's next steps: no new migration or database update is needed; rebuild and run from Visual Studio, sign in, and try Manage Claims, Manage Users, Reports / History, About, and Logout. Optional tests run with `dotnet test` only if the user wants them.
    - _Requirements: 27.14_

## Notes

Verification limits (every task must obey them):

- Allowed commands only: `dotnet --version`, `dotnet --list-sdks`, `dotnet new` (`sln --format sln`, `mvc`, `tool-manifest`), `dotnet restore`, `dotnet build`, `dotnet tool install dotnet-ef --version 10.0.9` (local tool), ONE run of `dotnet ef migrations add InitialCreate` (task 17.2), downloads of Bootstrap Icons and Chart.js (task 1.5), and file operations inside the workspace.
- Forbidden: `dotnet run`, `dotnet ef database update`, `dotnet test` (unless the user approves), and anything that connects to the database. Also unused: `dotnet sln add`, `dotnet add package`, `dotnet ef migrations remove`, global tool installs, and a second `migrations add`.
- If `dotnet build` reports errors, fix them and rebuild until it reports zero errors.
- Never use the name "ELIF" anywhere: not in code, views, CSS, JS, SVG, config, README, generated migrations, or rendered text. Vendor files in `wwwroot/lib` are not edited; report any hit there in the summary.
- Do not run two `dotnet build` or `dotnet restore` commands at the same time. When sub-tasks of one wave run in parallel, build once after the wave.

Other notes:

- Phase 2 (tasks 21–29) starts only after the user replies "continue" at task 20.1.
- Phase 2 extends `BusinessRules.cs` and `ChartData.cs`, adds `CsvExporter.cs`, and adds zero migrations. If a Phase 2 change seems to need a schema change, stop and ask the user.
- Tasks marked `*` (the test project and every test) are optional and skipped unless the user asks for tests. Nothing in the required path depends on them. They may be compiled, but `dotnet test` runs only with the user's approval.
- Required verification is `dotnet build` with zero errors in 1.4, 17.1, 17.3, 19.2, and 28.2 and at checkpoints 7, 12, and 23.
- Each task references requirement clauses for traceability; the design document holds the exact texts, formulas, and table details.
- Property tests check universal properties of the pure helpers; unit tests check specific examples.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["1.4"] },
    { "id": 3, "tasks": ["1.5", "1.6", "2.1", "3.1", "3.2", "4.1", "8.1", "8.2", "8.3", "8.4"] },
    { "id": 4, "tasks": ["2.2", "3.3", "3.4", "3.5", "3.6", "3.7", "4.2", "4.3", "4.4", "4.5", "4.6", "8.5"] },
    { "id": 5, "tasks": ["3.8", "3.9", "3.10", "3.11", "3.12", "4.7", "4.8", "4.9", "4.10", "6.1", "6.3"] },
    { "id": 6, "tasks": ["3.13", "5.1"] },
    { "id": 7, "tasks": ["5.2", "5.3", "5.4", "5.5"] },
    { "id": 8, "tasks": ["6.2", "9.1"] },
    { "id": 9, "tasks": ["9.2", "9.3", "9.4", "10.1", "11.1", "13.1", "14.1", "16.1"] },
    { "id": 10, "tasks": ["10.2", "10.3", "11.2", "13.2", "14.2", "14.3", "14.4", "14.5", "16.2"] },
    { "id": 11, "tasks": ["15.1", "15.2"] },
    { "id": 12, "tasks": ["17.1"] },
    { "id": 13, "tasks": ["17.2"] },
    { "id": 14, "tasks": ["17.3", "18.1"] },
    { "id": 15, "tasks": ["19.1"] },
    { "id": 16, "tasks": ["19.2"] },
    { "id": 17, "tasks": ["20.1"] },
    { "id": 18, "tasks": ["21.1", "22.4", "24.1", "24.4", "26.1", "26.2"] },
    { "id": 19, "tasks": ["21.2", "21.3", "21.4", "21.5", "22.1", "22.7", "24.2", "24.3", "24.5", "24.8", "25.1"] },
    { "id": 20, "tasks": ["21.6", "21.7", "22.2", "22.3", "22.5", "24.6"] },
    { "id": 21, "tasks": ["22.6", "24.7"] },
    { "id": 22, "tasks": ["27.1", "27.2"] },
    { "id": 23, "tasks": ["28.1"] },
    { "id": 24, "tasks": ["28.2"] },
    { "id": 25, "tasks": ["29.1"] }
  ]
}
```
