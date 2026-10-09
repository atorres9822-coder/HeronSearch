# HeronSearch Admin

HeronSearch Admin is the staff and administrator side of the University of Makati (UMak) Lost & Found Management System, built by AnagolayCorp. It is one ASP.NET Core MVC web app (Razor views) running on **.NET 10 (net10.0)**. Staff use it to sign in, register found items, review ownership claims, and track activity; administrators also manage user accounts and view reports. Both phases are now complete: Phase 1 (sign-in, dashboard, and item management) and Phase 2 (claims, users, reports / history, about, and the full logout flow). Phase 2 added no database migration — the schema is unchanged from Phase 1.

---

## 1. What you need before you start (prerequisites)

Install these three things first:

1. **Visual Studio Community 2026** – the editor you will open the project in.
2. **The .NET 10 SDK** – the toolkit that builds and runs the app. Visual Studio 2026 usually installs this for you, but you can also install it on its own.
3. **MySQL Server 8.0.46**, running on **port 3307** – the database that stores users, items, claims, and activity logs.

> This project connects to MySQL on **port 3307** (not the usual 3306). Make sure your MySQL Server is listening on 3307 before you run the app.

---

## 2. Setup, step by step

Follow these steps in order.

1. **Open the solution.** Double-click `HeronSearch.sln` in the workspace root. Visual Studio 2026 opens the project.

2. **Add your MySQL password.** Open `HeronSearch.Admin/appsettings.json`. Find the `DefaultConnection` line and replace `YOUR_PASSWORD_HERE` with the real password for your MySQL `root` user. The line should look like this (with your own password in place):

   ```json
   "DefaultConnection": "Server=localhost;Port=3307;Database=heronsearch;User=root;Password=YOUR_PASSWORD_HERE;"
   ```

3. **Restore the local tools.** Open a terminal in the **workspace root** (the folder that holds `HeronSearch.sln`) and run:

   ```
   dotnet tool restore
   ```

   This installs the `dotnet-ef` tool that the next step needs.

4. **Create the database tables.** Open a terminal in the **`HeronSearch.Admin`** folder and run:

   ```
   dotnet ef database update
   ```

   This creates the `heronsearch` database (if it does not exist) and all of its tables.

5. **Start the app.** Back in Visual Studio, press **F5** (run with debugging) or **Ctrl+F5** (run without debugging). Your browser opens on the Login page.

6. **Sign in as the demo Admin.** Use the demo Admin account below. The first time the app starts, it fills the database with demo data so you have something to look at.

---

## 3. Demo accounts

The app ships with ready-made accounts so you can log in right away:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@heronsearch.local` | `HeronAdmin2026` |
| Staff | `ramon.delacruz@heronsearch.local` | `HeronStaff2026` |
| Students | `firstname.lastname@heronsearch.local` | `HeronStudent2026` |

> **⚠️ Demo only — change these before production.** These accounts and passwords exist to make the demo easy to try. They are publicly documented here, so **never** use them on a real or public deployment. Replace the demo Admin (see the next section) and set strong, private passwords before going live.

---

## 4. Replacing the demo Admin

You can replace the demo Admin account in either of two ways:

- **Before the first run:** open `HeronSearch.Admin/Data/DbSeeder.cs` and change the demo Admin's email and password there, then start the app for the first time so the seed data is created with your values.
- **From inside the running app:** sign in as the demo Admin, then
  1. **Add Staff Account** – create a new account for yourself.
  2. **Change role to Admin** – promote that new account to Admin.
  3. **Sign in as the new account.**
  4. **Suspend the demo Admin** so it can no longer be used.

---

## 5. Why .NET 10 and MySql.EntityFrameworkCore 10.0.9

This project uses **net10.0** with the Oracle MySQL provider **MySql.EntityFrameworkCore 10.0.9** instead of the popular Pomelo provider, for these reasons:

- **Pomelo has no EF Core 10 release.** Its newest stable version (9.0.0) supports EF Core 9 only, so it cannot be used with EF Core 10.
- **.NET 8 support ends on 2026-11-10,** so staying on an older framework is not a good long-term choice; .NET 10 is the current long-term release.
- **The Oracle provider does no server-version detection,** which means migrations can be created and scripted offline, without a live database connection.
- **`new MySqlServerVersion(new Version(8, 0, 46))` is a Pomelo-only type.** It does not exist in the Oracle provider and is deliberately **absent from this code**.

**License:** MySql.EntityFrameworkCore is distributed under **GPL-2.0-only WITH Universal-FOSS-exception-1.0**.

---

## 6. Exact versions

These are the exact pinned versions used in this project (read from the project file and the library file headers).

| Component | Version |
|---|---|
| MySql.EntityFrameworkCore (EF Core provider) | 10.0.9 |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 |
| dotnet-ef (local tool) | 10.0.9 |
| Bootstrap | 5.3.3 |
| Bootstrap Icons | 1.13.1 |
| Chart.js | 4.5.1 |
| jQuery | 3.7.1 |
| jQuery Validation | 1.21.0 |
| jQuery Validation Unobtrusive | 4.0.0 |

> **Note on the Chart.js file name.** The Chart.js minified UMD build is stored at `wwwroot/lib/chart.js/dist/chart.umd.min.js` so the pages can reference a single, consistent name.

---

## 7. Project files and folders

Here is the layout of the solution, with one line describing each part.

```
Anagolay\                               the workspace root
├── HeronSearch.sln                     the Visual Studio solution file (classic .sln, one project)
├── README.md                           this guide
├── .config\
│   └── dotnet-tools.json               local tool manifest (pins dotnet-ef 10.0.9)
└── HeronSearch.Admin\                  the web application project
    ├── HeronSearch.Admin.csproj        project file: target framework and NuGet packages
    ├── Program.cs                      app startup: services, authentication, request pipeline, seeding
    ├── appsettings.json                settings, including the MySQL connection string and logo path
    ├── appsettings.Development.json    extra settings used only while developing (no connection string)
    ├── Properties\
    │   └── launchSettings.json         local run settings (URLs and profiles)
    ├── Models\                         database entities, enums, and the fixed category/location lists
    ├── Data\                           AppDbContext (the database shape) and DbSeeder (demo data)
    ├── Migrations\                     the generated InitialCreate migration that builds the tables
    ├── Controllers\                    one controller per section (handles requests)
    ├── ViewModels\                     the data each page needs, plus two validation attributes
    ├── Helpers\                        small, focused helpers (formatting, rules, logging, uploads)
    ├── Views\                          Razor views and shared partials (the HTML)
    └── wwwroot\                        files served to the browser
        ├── css\site.css                the app's styles and brand colors
        ├── js\site.js                  small page behaviors (password toggle, confirm modal)
        ├── js\charts.js                builds the dashboard charts
        ├── js\validation-rules.js      client-side rules for the item form
        ├── images\heronsearch-logo.svg the app logo
        ├── lib\bootstrap\              Bootstrap CSS and JS (from the template)
        ├── lib\bootstrap-icons\        Bootstrap Icons font and CSS
        ├── lib\chart.js\               the Chart.js library
        ├── lib\jquery\                 jQuery
        ├── lib\jquery-validation\      jQuery Validation
        ├── lib\jquery-validation-unobtrusive\  jQuery Validation Unobtrusive
        └── uploads\                    photos uploaded at runtime (created automatically)
```

---

## 8. Replacing the logo (no code changes)

You can swap in the official logo without touching any code:

1. Copy the official logo file into `HeronSearch.Admin/wwwroot/images`.
2. Open `HeronSearch.Admin/appsettings.json` and set `Branding:LogoPath` to the path of your new file, for example `/images/your-logo.svg`.

The top bar and the login page read the logo path from this setting, so the new logo appears everywhere after you restart the app.

---

## 9. Pages

**Phase 1:**

- **Login** – sign in with an Admin or Staff account.
- **Dashboard** – totals, charts, pending claims, and recent activity.
- **Manage Items** – list, search, filter, add, edit, mark returned, archive, and delete found items.

**Phase 2:**

- **Manage Claims** – Pending/Approved/Rejected tabs; review a claim, approve it through a four-point checklist (which auto-rejects the item's other pending claims), or reject it with a reason.
- **Manage Users** (Admin only) – add staff accounts; verify, suspend, change the role of, or delete accounts; the signed-in Admin cannot suspend, re-role, or delete their own account.
- **Reports / History** – an Overview tab (summary counts and location, month, hour, and category charts for a date range) and a System Logs tab (searchable activity history); both export to CSV.
- **About** – what HeronSearch is, its features, the roles, and the logo meaning.
- **Logout** – a confirmation modal, sign-out, and a "You've been signed out" page.

### Files added or changed in Phase 2

Phase 2 adds **no migration**; the `Migrations` folder still holds only `InitialCreate` and its unchanged snapshot.

```
Helpers\BusinessRules.cs                changed: added the claim rules and the user rules
Helpers\ChartData.cs                    changed: added MonthsInRange, ResolveRange, and HourlyCounts
Helpers\CsvExporter.cs                  added: builds the Reports CSV exports (RFC 4180, UTF-8 BOM)
Controllers\ClaimsController.cs         changed: the real Manage Claims list and review/approve/reject
Controllers\UsersController.cs          changed: the real Manage Users list and account actions
Controllers\ReportsController.cs        changed: the Overview and System Logs tabs and the CSV exports
Controllers\AboutController.cs          changed: shows the real About page
Controllers\AccountController.cs        changed: Logout now lands on the SignedOut page; added SignedOut
ViewModels\ClaimListViewModel.cs        added: the Manage Claims list rows and tab counts
ViewModels\ClaimReviewViewModel.cs      added: the review page, approve checklist, and reject form
ViewModels\UserListViewModel.cs         added: the Manage Users list rows and filters
ViewModels\AddStaffViewModel.cs         added: the Add Staff Account form and its rules
ViewModels\ChangeRoleViewModel.cs       added: the change-role form
ViewModels\ReportsViewModel.cs          added: the Reports counts, charts, and log rows
Views\Claims\Index.cshtml               added: the claims list with tabs
Views\Claims\Review.cshtml              added: the claim review page with the checklist and reject modal
Views\Users\Index.cshtml                added: the users list with actions
Views\Users\Create.cshtml               added: the Add Staff Account form
Views\Users\_ChangeRoleModal.cshtml     added: the shared change-role modal
Views\Reports\Index.cshtml              added: the range form and the Overview/System Logs tabs
Views\Reports\_Overview.cshtml          added: the summary cards and the four charts
Views\Reports\_Logs.cshtml              added: the searchable logs table
Views\About\Index.cshtml                added: the About content
Views\Account\SignedOut.cshtml          added: the signed-out page
Views\Shared\_LogoutModal.cshtml        added: the logout confirmation modal
Views\Shared\_Layout.cshtml             changed: renders the logout modal
Views\Shared\_Sidebar.cshtml            changed: Logout now opens the logout modal
wwwroot\js\site.js                      changed: approve checklist, reject-modal reopen, change-role modal
wwwroot\css\site.css                    changed: tabs, the review layout, and the About tiles
Views\Shared\Placeholder.cshtml         removed: the Phase 1 placeholder is no longer used
```

---

## 10. Troubleshooting

| What you see | What it means and how to fix it |
|---|---|
| `Access denied for user 'root'` | The MySQL password is wrong. Check the `Password=` value in `HeronSearch.Admin/appsettings.json`. |
| `Unable to connect` | MySQL is not reachable. Make sure the MySQL service is running and listening on **port 3307**. |
| `Unknown database` or `Table doesn't exist` | The database or its tables have not been created. Run `dotnet ef database update` in the `HeronSearch.Admin` folder. If needed, create the database first with `CREATE DATABASE heronsearch;`. |
| `dotnet ef not found` | The local tool is not installed yet. Run `dotnet tool restore` in the workspace root. |

---

## 11. Keep your password private

Your real MySQL password lives in `HeronSearch.Admin/appsettings.json`. Before you share this project — zipping it, committing it, or sending it to anyone — reset that value back to `YOUR_PASSWORD_HERE` so your password stays private.
