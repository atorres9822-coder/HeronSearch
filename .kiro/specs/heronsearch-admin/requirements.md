# Requirements Document

## Introduction

HeronSearch Admin is the administrator and staff side of "HeronSearch: UMak's Lost & Found Management System" for the University of Makati, built by AnagolayCorp. Admin and Staff users sign in to review lost and found item reports, register found items, verify ownership claims, manage user accounts, and view statistics and activity history.

The application is a greenfield ASP.NET Core MVC solution (Razor views) in the workspace root `c:\Users\Lenovo\Downloads\Anagolay`. The solution targets net10.0 and uses Entity Framework Core 10 code-first migrations with the Oracle MySQL provider (MySql.EntityFrameworkCore 10.0.9) against a local MySQL Server 8.0.46 on port 3307. The UI uses Bootstrap 5, Bootstrap Icons, and Chart.js with the HeronSearch brand palette. The UI follows the layout of the existing user-side screens: white rounded cards, a light gray background, pill filter chips, a top bar with the logo, and a simple footer. The code stays beginner-friendly: plain MVC folders, a DbContext used directly from controllers, and a short comment above every class and method.

Delivery happens in two phases with a stop checkpoint in between:

| Phase | Scope | Requirements |
|---|---|---|
| Phase 1 | Solution setup, database provider and connection, entities, seed data, InitialCreate migration, sign-in and access control, shared layout and branding, site.css, README, Login page, Dashboard, Manage Items | 1–17, 26, 27 |
| Phase 2 (starts only after the user says "continue") | Manage Claims, Manage Users, Reports / History with System Logs and CSV export, About, full Logout flow, README update | 18–25, 26, 27 |

Decisions confirmed during clarification (these override conflicting parts of the original request):

- Framework and provider: net10.0 with MySql.EntityFrameworkCore 10.0.9 (Oracle). Pomelo.EntityFrameworkCore.MySql has no EF Core 10 release (the newest stable version, 9.0.0, supports EF Core 9 only), and .NET 8 support ends on 2026-11-10. The Pomelo-only setting `new MySqlServerVersion(new Version(8, 0, 46))` cannot be used. The goal behind that setting (creating migrations without a database connection) is still met, because the Oracle provider performs no server-version auto-detection.
- dotnet-ef is installed as a local tool pinned to 10.0.9. Microsoft.EntityFrameworkCore.Design is pinned to 10.0.9.
- Bootstrap Icons and Chart.js are downloaded into wwwroot/lib at pinned versions so the app works offline. Bootstrap 5.3.x, jQuery, and the validation scripts come from the .NET 10 MVC template.
- Deletes are blocked when dependent records exist, and the app suggests Archive (items) or Suspend (users) instead. Activity log references become empty on delete, so logs neither block a delete nor get erased.
- The official logo files exist only as chat attachments. The solution ships an SVG recreation of the HeronSearch mark, and the README explains how to swap in the official file without changing code.

Working assumptions (chosen to remove ambiguity; open for change at the review phase):

- Claims are filed on the user side against Found items. The admin side reviews claims and does not create claims.
- The admin side registers Found items only. Lost items come from the user side and from the seed data.
- "Registered Users" counts all User records.
- Only Verified Admin and Staff accounts can sign in. New Staff accounts created by an Admin start as Verified.
- All dates and times use the server's local time.
- Export to CSV exists on both Reports / History tabs: the Overview tab exports items, and the System Logs tab exports activity logs.

## Glossary

- **HeronSearch_Admin**: The admin-side ASP.NET Core MVC web application specified by this document.
- **Solution**: The Visual Studio solution file (classic .sln format) in the workspace root, together with the Web_Project.
- **Web_Project**: The single ASP.NET Core MVC project (.csproj) referenced by the Solution.
- **Build_Process**: The commands and file edits used to create, restore, build, and verify the Solution during Phase_1 and Phase_2.
- **Allowed_Commands**: The commands the Build_Process may execute: read-only environment checks (`dotnet --version`, `dotnet --list-sdks`), `dotnet new` (including `dotnet new sln --format sln` and `dotnet new tool-manifest`), `dotnet restore`, `dotnet build`, `dotnet tool install dotnet-ef --version 10.0.9` into the Local_Tool_Manifest, one run of `dotnet ef migrations add InitialCreate`, downloads of the Offline_Libraries, and file-system commands that create, copy, move, or list files inside the workspace.
- **Database**: The MySQL Server 8.0.46 database named `heronsearch` at localhost, port 3307, user root.
- **DbContext**: The Entity Framework Core database context class in the Data folder that maps the entities to the Database.
- **Seeder**: The runtime seed routine in the Data folder that inserts demo data when the application starts.
- **Design_Time**: Any run of the EF Core command-line tools (for example `dotnet ef migrations add`) that loads the Web_Project to build the EF Core model.
- **Auth_Service**: The cookie authentication and role-based authorization configuration, plus the sign-in and sign-out logic, of HeronSearch_Admin.
- **Password_Hasher**: The built-in ASP.NET Core Identity `PasswordHasher<User>` class used to hash and verify passwords.
- **Password_Rule**: A password of 8 to 100 characters that contains at least one letter and at least one digit.
- **Admin_User**: A User whose Role is Admin.
- **Staff_User**: A User whose Role is Staff.
- **Student_User**: A User whose Role is Student.
- **Back_Office_User**: A signed-in Admin_User or Staff_User.
- **Admin_Area**: Every page of HeronSearch_Admin except the Login_Page and the Signed_Out_Page.
- **Activity_Logger**: The helper that writes ActivityLog records.
- **Outcome_Word**: The first word of an ActivityLog Result: Success, Failed, or Blocked.
- **Admin_Layout**: The shared page layout of the Admin_Area: Top_Bar, Sidebar, main content area, and Footer.
- **Minimal_Layout**: The shared page layout of the Login_Page and the Signed_Out_Page: Top_Bar with the Brand_Logo, centered content, and Footer, without the Sidebar.
- **Top_Bar**: The white bar at the top of each page that shows the Brand_Logo.
- **Sidebar**: The vertical navigation menu of the Admin_Layout.
- **Footer**: The bottom bar of each page that contains the copyright text.
- **Login_Page**: The admin sign-in page.
- **Signed_Out_Page**: The page shown after a successful sign-out.
- **Dashboard**: The admin home page with stat cards, charts, pending claims, and recent activity.
- **Items_Manager**: The Manage Items pages: item list, Add Found Item form, Edit form, details modal, and item actions.
- **Image_Upload_Validator**: The server-side helper that validates and stores uploaded item photos.
- **Claims_Manager**: The Manage Claims pages: status tabs, claim lists, and the claim review page.
- **Users_Manager**: The Manage Users pages: user list, filters, Add Staff Account form, and user actions.
- **Reports_Page**: The Reports / History page with an Overview tab and a System Logs tab.
- **CSV_Exporter**: The server-side logic that produces downloadable CSV files for the Reports_Page.
- **About_Page**: The page that describes the system, features, roles, logo meaning, and credits.
- **Logout_Flow**: The sign-out confirmation modal, the sign-out request, and the Signed_Out_Page.
- **Placeholder_Page**: A simple Admin_Area page with the text "This page will be available in Phase 2." shown for Phase_2 sections during the Phase_1_Checkpoint.
- **Confirmation_Modal**: A Bootstrap modal dialog that names the target record and asks the user to confirm or cancel an action before the request is sent.
- **Alert_Message**: A dismissible Bootstrap alert at the top of the main content area. Success alerts are green and error alerts are red.
- **Empty_State**: A centered block with an icon and a short sentence, shown in place of a list, table, or chart that has zero records.
- **Status_Badge**: A pill-shaped label that shows a status name as text with a status color.
- **Pill_Chip**: A pill-shaped filter button. The active chip is filled with Maroon, and inactive chips are light gray.
- **Brand_Palette**: Maroon #6B1A1A (primary), Gold #C8943E (accent), Cream #FBF3EA (page tint), Sand #D4B896 (borders), Deep Green #2D4A2D (success).
- **Brand_Logo**: The HeronSearch mark image file in wwwroot/images. The Brand_Logo is an SVG recreation until the user supplies the official file.
- **Logo_Path_Setting**: The single appsettings.json value `Branding:LogoPath` that holds the URL path of the Brand_Logo, with the default value `/images/heronsearch-logo.svg`.
- **Forbidden_Name**: The text "ELIF".
- **Offline_Libraries**: Bootstrap 5.3.x, Bootstrap Icons (CSS plus woff and woff2 fonts), Chart.js (UMD minified build), jQuery, jQuery Validation, and jQuery Validation Unobtrusive, all stored in wwwroot/lib.
- **Local_Tool_Manifest**: The file `.config/dotnet-tools.json` in the workspace root that pins local .NET tools.
- **Category_List**: The fixed list of item categories, defined in one place: ID Card, Tumbler, Umbrella, Calculator, Laptop, Phone & Gadgets, Bag, Wallet, Keys, Books & Notes, Clothing, Other.
- **Location_List**: The fixed list of campus locations, defined in one place, containing at least Library, Canteen, three classroom locations, and Other.
- **Six_Month_Window**: The current calendar month plus the 5 calendar months before the current month.
- **Item_Dependent**: A Claim whose ItemId refers to the item.
- **User_Dependent**: An Item whose ReportedByUserId refers to the user, or a Claim whose ClaimantUserId or ReviewedByUserId refers to the user. ActivityLog records count as neither User_Dependents nor Item_Dependents.
- **Date_Format**: Dates shown as "MMM d, yyyy" (for example "Oct 6, 2026") and date-times shown as "MMM d, yyyy h:mm tt" (for example "Oct 6, 2026 2:30 PM").
- **Phase_1**, **Phase_2**: The two delivery phases listed in the Introduction.
- **Phase_1_Checkpoint**: The stop point after Phase_1 builds with zero errors, lasting until the user sends the message "continue".
- **Phase_1_Summary**: The short message the Build_Process presents at the Phase_1_Checkpoint.
- **README**: The README.md file in the workspace root.

## Requirements

### Requirement 1: Solution and project setup (Phase 1)

**User Story:** As a student developer, I want a standard Visual Studio solution with one simple MVC project, so that I can open, build, and understand the admin application.

#### Acceptance Criteria

1. THE Solution SHALL contain one solution file in the classic .sln format in the workspace root that references the Web_Project.
2. THE Web_Project SHALL declare the target framework `net10.0` in exactly one `<TargetFramework>` element of the .csproj file.
3. THE Web_Project SHALL reference every NuGet package with an exact version number.
4. THE Web_Project SHALL organize source code in the folders Models, Controllers, Views, ViewModels, Data (DbContext and Seeder), and wwwroot.
5. THE Web_Project SHALL use C#, ASP.NET Core MVC Razor views, HTML5, CSS3, JavaScript, Bootstrap 5, Bootstrap Icons, and Chart.js as the only user-interface technologies.
6. THE Web_Project SHALL contain a short comment directly above every class and every method that states what the class or method does.
7. THE Web_Project SHALL limit the data-access design to the DbContext injected into controllers and simple helper classes.
8. WHEN `dotnet build` runs on the Solution, THE Solution SHALL build with zero errors.

### Requirement 2: Database provider and connection (Phase 1)

**User Story:** As a student developer, I want the app wired to my local MySQL 8.0.46 server on port 3307 through an EF Core 10 provider, so that migrations work offline and the app connects once I type my password.

#### Acceptance Criteria

1. THE Web_Project SHALL reference the NuGet package MySql.EntityFrameworkCore version 10.0.9 as the Entity Framework Core database provider.
2. THE Web_Project SHALL reference the NuGet package Microsoft.EntityFrameworkCore.Design version 10.0.9.
3. THE Local_Tool_Manifest SHALL pin the dotnet-ef tool at version 10.0.9.
4. THE appsettings.json file SHALL contain the connection string `ConnectionStrings:DefaultConnection` with the exact value `Server=localhost;Port=3307;Database=heronsearch;User=root;Password=YOUR_PASSWORD_HERE;`.
5. THE Web_Project SHALL register the DbContext through the Oracle provider's `UseMySQL` option with the `DefaultConnection` connection string.
6. THE Web_Project SHALL use the MySQL Server settings of criterion 4 as the only database configuration, excluding XAMPP and MariaDB settings.
7. WHEN `dotnet ef migrations add InitialCreate` runs, THE DbContext SHALL build the EF Core model with zero connections opened to the Database.
8. THE Web_Project SHALL contain one migration named InitialCreate that creates the complete schema for User, Item, Claim, and ActivityLog.

### Requirement 3: Data model (Phase 1)

**User Story:** As an administrator, I want users, items, claims, and activity logs stored in MySQL, so that lost-and-found records persist between sessions.

#### Acceptance Criteria

1. THE DbContext SHALL map a User entity with the fields Id, FullName, Email, PasswordHash, StudentId, Role, VerificationStatus, and CreatedAt.
2. THE DbContext SHALL map an Item entity with the fields Id, Name, Category, Location, DateReported, Description, ImagePath, Type, Status, and ReportedByUserId.
3. THE DbContext SHALL map a Claim entity with the fields Id, ItemId, ClaimantUserId, ProofDescription, DateFiled, Status, RejectionReason, ReviewedByUserId, and ReviewedAt.
4. THE DbContext SHALL map an ActivityLog entity with the fields Id, Timestamp, UserId, Action, TargetItemId, and Result.
5. THE DbContext SHALL limit User.Role to Admin, Staff, or Student; User.VerificationStatus to Pending, Verified, or Suspended; Item.Type to Lost or Found; Item.Status to Active, Claimed, Returned, or Archived; and Claim.Status to Pending, Approved, or Rejected.
6. THE DbContext SHALL store User.Role, User.VerificationStatus, Item.Type, Item.Status, and Claim.Status as readable text values (for example "Pending") in the Database.
7. THE DbContext SHALL enforce a unique index on User.Email.
8. THE DbContext SHALL treat User.StudentId, Item.Description, Item.ImagePath, Claim.RejectionReason, Claim.ReviewedByUserId, Claim.ReviewedAt, ActivityLog.UserId, and ActivityLog.TargetItemId as optional fields and every other field listed in criteria 1 to 4 as required.
9. THE DbContext SHALL set these maximum lengths in characters: User.FullName 100, User.Email 150, User.StudentId 20, Item.Name 100, Item.Category 50, Item.Location 100, Item.Description 1000, Item.ImagePath 260, Claim.ProofDescription 1000, Claim.RejectionReason 500, ActivityLog.Action 50, and ActivityLog.Result 500.
10. THE DbContext SHALL configure Item.ReportedByUserId, Claim.ItemId, Claim.ClaimantUserId, and Claim.ReviewedByUserId as foreign keys with restrict-on-delete behavior.
11. THE DbContext SHALL configure ActivityLog.UserId and ActivityLog.TargetItemId as foreign keys with set-null-on-delete behavior.
12. WHEN a User or an Item referenced by ActivityLog records is deleted, THE Database SHALL keep the referencing ActivityLog records and set the matching ActivityLog.UserId or ActivityLog.TargetItemId value to null.

### Requirement 4: Seed data (Phase 1)

**User Story:** As a student developer, I want realistic demo data inserted automatically on the first run, so that the dashboard, charts, and tables look real in my demo.

#### Acceptance Criteria

1. WHEN HeronSearch_Admin starts and the User table contains zero records, THE Seeder SHALL insert the demo data set described in this requirement.
2. WHEN HeronSearch_Admin starts and the User table contains one or more records, THE Seeder SHALL leave the Database unchanged.
3. WHILE Design_Time is active, THE Seeder SHALL skip seeding and open zero connections to the Database.
4. THE Seeder SHALL create exactly one demo Admin_User with Email `admin@heronsearch.local`, VerificationStatus Verified, and a demo-only password that meets the Password_Rule and is documented in the README.
5. THE Seeder SHALL create at least 2 Staff_Users and at least 8 Student_Users with fictional full names, fictional email addresses on the `heronsearch.local` domain, and fictional student IDs.
6. THE Seeder SHALL give at least one seeded Staff_User VerificationStatus Verified and a demo-only password that meets the Password_Rule and is documented in the README.
7. THE Seeder SHALL assign seeded Student_Users a mix of VerificationStatus values that includes at least one Pending, one Verified, and one Suspended user.
8. THE Seeder SHALL hash every seeded password with the Password_Hasher.
9. THE Seeder SHALL create at least 30 items that use only Category_List and Location_List values and that include the categories ID Card, Tumbler, Umbrella, Calculator, and Laptop and the locations Library, Canteen, and at least two classroom locations.
10. THE Seeder SHALL distribute Item.DateReported values across the Six_Month_Window, relative to the current date, with at least 2 Lost items and at least 2 Found items in each month and with every value at or before the current date and time.
11. THE Seeder SHALL assign Item.DateReported times of day between 07:00 and 20:00 that cover at least 8 different hours.
12. THE Seeder SHALL include at least one seeded item for each Item.Status value: Active, Claimed, Returned, and Archived.
13. THE Seeder SHALL create claims on Found items only, with at least 3 Pending claims, at least 2 Approved claims, and at least 2 Rejected claims.
14. THE Seeder SHALL keep seeded claims consistent with seeded items: each Pending claim refers to an item with Status Active, each Approved claim refers to an item with Status Claimed or Returned, each Approved or Rejected claim has ReviewedByUserId and ReviewedAt values, each Rejected claim has a RejectionReason, and each DateFiled falls after the item's DateReported and before the claim's ReviewedAt.
15. THE Seeder SHALL leave at least 3 seeded items with zero Item_Dependents and at least 2 seeded users with zero User_Dependents.
16. THE Seeder SHALL set each seeded User.CreatedAt value inside the Six_Month_Window and earlier than the first seeded item or claim of the same user.
17. THE Seeder SHALL create at least 15 ActivityLog records spread across the Six_Month_Window that include the Actions Login, Approve Claim, and Reject Claim.
18. IF the Seeder cannot connect to the Database or the Database tables are missing, THEN THE HeronSearch_Admin SHALL write a console error message that tells the user to check the password in appsettings.json and to run `dotnet ef database update`, and continue the startup.

### Requirement 5: Sign-in (Phase 1)

**User Story:** As an Admin or Staff member, I want to sign in with my email and password, so that only authorized personnel reach the admin area.

#### Acceptance Criteria

1. THE Login_Page SHALL display, in a centered white card on the Minimal_Layout, the Brand_Logo, an "Email Address" field, a "Password" field with a show/hide toggle button inside the field, a "Remember me" checkbox, and a full-width "Sign In" button.
2. THE Login_Page SHALL limit the card's controls to the controls listed in criterion 1, excluding Google sign-in, sign-up, and forgot-password controls.
3. WHEN the show/hide toggle is activated, THE Login_Page SHALL switch the Password field between masked and plain text and switch the toggle's accessible name between "Show password" and "Hide password".
4. WHEN a user submits an email and password that match a Verified Admin_User or a Verified Staff_User, THE Auth_Service SHALL sign the user in with an authentication cookie and redirect to the Dashboard.
5. WHEN a successful sign-in request carries a local return URL, THE Auth_Service SHALL redirect to the return URL instead of the Dashboard.
6. IF a sign-in request carries a return URL that points outside HeronSearch_Admin, THEN THE Auth_Service SHALL redirect to the Dashboard after a successful sign-in.
7. IF the submitted email matches no User or the submitted password fails verification, THEN THE Login_Page SHALL display the error "Invalid email or password." and keep the entered email in the Email Address field.
8. IF the submitted credentials match a Student_User, THEN THE Login_Page SHALL display the error "This portal is for Admin and Staff accounts only." and keep the user signed out.
9. IF the submitted credentials match an Admin_User or a Staff_User whose VerificationStatus is Pending or Suspended, THEN THE Login_Page SHALL display an error that names the account status and keep the user signed out.
10. IF the Email Address field is empty, has an invalid email format, or exceeds 150 characters, or the Password field is empty, THEN THE Login_Page SHALL display a field-level validation message and keep the user signed out.
11. THE Auth_Service SHALL match email addresses case-insensitively after trimming leading and trailing spaces.
12. THE Auth_Service SHALL verify passwords with the Password_Hasher.
13. WHEN a user signs in with "Remember me" selected, THE Auth_Service SHALL issue a persistent authentication cookie that stays valid for 14 days.
14. WHEN a user signs in with "Remember me" cleared, THE Auth_Service SHALL issue a browser-session authentication cookie that expires after 8 hours without activity.
15. THE Auth_Service SHALL mark the authentication cookie as HttpOnly.
16. WHEN a sign-in attempt completes, THE Activity_Logger SHALL record an ActivityLog record with Action "Login", the matched UserId (null when no User matches), and a Result that starts with Success or Failed and names the attempted email address, excluding the entered password.
17. WHILE a Back_Office_User is signed in, WHEN the Back_Office_User opens the Login_Page, THE Auth_Service SHALL redirect to the Dashboard.

### Requirement 6: Access control (Phase 1)

**User Story:** As an Admin, I want role-based access, so that Staff handle daily work while only Admins manage users and delete records.

#### Acceptance Criteria

1. WHEN a user who is signed out requests an Admin_Area page, THE Auth_Service SHALL redirect to the Login_Page with the requested page as the return URL.
2. THE Auth_Service SHALL grant Back_Office_Users access to the Dashboard, Items_Manager, Claims_Manager, Reports_Page, About_Page, and Logout_Flow.
3. THE Auth_Service SHALL grant access to the Users_Manager and to every delete action to Admin_Users only.
4. IF a Staff_User requests an Admin-only page or action, THEN THE Auth_Service SHALL respond with HTTP status 403 and display an Access Denied page with a link to the Dashboard.
5. THE Sidebar SHALL display the Manage Users link to Admin_Users only.
6. WHEN a signed-in user sends a request, THE Auth_Service SHALL confirm in the Database that the user still exists, still has the Role stored in the authentication cookie, and has VerificationStatus Verified.
7. IF the check of criterion 6 fails, THEN THE Auth_Service SHALL sign the user out and redirect to the Login_Page.

### Requirement 7: Request and input security (Phase 1, all pages)

**User Story:** As an administrator, I want the app protected against common web attacks, so that records and accounts stay safe.

#### Acceptance Criteria

1. THE HeronSearch_Admin SHALL include an anti-forgery token in every HTML form.
2. IF a POST request has a missing or invalid anti-forgery token, THEN THE HeronSearch_Admin SHALL reject the request with HTTP status 400 and leave the Database unchanged.
3. THE HeronSearch_Admin SHALL accept state-changing actions (create, edit, status change, approve, reject, role change, delete, and sign-out) as HTTP POST requests only.
4. WHEN a form is submitted, THE HeronSearch_Admin SHALL validate every submitted field on the server against the rules in this document before saving.
5. IF server-side validation fails, THEN THE HeronSearch_Admin SHALL redisplay the form with a message next to each invalid field and keep the other entered values.
6. THE HeronSearch_Admin SHALL show client-side validation messages that use the same rules as the server-side validation.
7. THE HeronSearch_Admin SHALL store passwords as Password_Hasher hashes only.
8. THE HeronSearch_Admin SHALL HTML-encode all user-supplied text when rendering pages.
9. THE HeronSearch_Admin SHALL send search and filter values to the Database as query parameters.
10. IF a query-string filter, tab, or page value is outside the allowed values, THEN THE HeronSearch_Admin SHALL ignore the invalid value and apply the default value.
11. IF a request refers to a record Id that matches no record, THEN THE HeronSearch_Admin SHALL display a Not Found page with a link back to the related list.
12. IF a requested URL matches no page, THEN THE HeronSearch_Admin SHALL display a Not Found page with a link to the Dashboard.
13. IF an unhandled error occurs outside the Development environment, THEN THE HeronSearch_Admin SHALL display a generic error page that hides technical details.
14. THE HeronSearch_Admin SHALL store email addresses trimmed and in lowercase.

### Requirement 8: Activity logging (Phase 1 helper, actions in both phases)

**User Story:** As an administrator, I want important actions recorded, so that I can trace who did what and when.

#### Acceptance Criteria

1. WHEN a Back_Office_User completes one of the actions Add Item, Edit Item, Mark Returned, Archive Item, Delete Item, Approve Claim, Reject Claim, Verify User, Suspend User, Change Role, Add Staff, Delete User, or Logout, THE Activity_Logger SHALL record an ActivityLog record with the current Timestamp, the acting user's UserId, the Action name, the TargetItemId of the affected item (null for actions without an existing item), and a Result.
2. WHEN HeronSearch_Admin blocks one of the actions of criterion 1 because of a business rule in this document, THE Activity_Logger SHALL record the attempt with the Outcome_Word Blocked.
3. THE Activity_Logger SHALL start every Result with an Outcome_Word followed by a description that names the affected record, for example "Success: Deleted item 'Blue umbrella' (#12)".
4. THE HeronSearch_Admin SHALL provide read-only access to ActivityLog records.

### Requirement 9: Shared layout, navigation, and branding (Phase 1)

**User Story:** As a Back_Office_User, I want a consistent branded layout with clear navigation, so that I always know where I am.

#### Acceptance Criteria

1. THE Admin_Layout SHALL display the Top_Bar, the Sidebar, the main content area, and the Footer on every Admin_Area page.
2. THE Top_Bar SHALL display the Brand_Logo, the name "HeronSearch", and the signed-in user's full name and role.
3. THE Sidebar SHALL list the links Dashboard, Manage Items, Manage Claims, Manage Users, Reports / History, About, and Logout in this order, each with a Bootstrap Icon.
4. THE Sidebar SHALL highlight the link of the current section, including sub-pages of the section, and mark the highlighted link with `aria-current="page"`.
5. WHILE the viewport is narrower than 992 pixels, THE Sidebar SHALL stay collapsed and open from a menu toggle button in the Top_Bar.
6. THE Minimal_Layout SHALL display the Top_Bar with the Brand_Logo, the centered page content, and the Footer.
7. THE Footer SHALL display the text "© 2026 HeronSearch · AnagolayCorp" on every page.
8. THE wwwroot/css/site.css file SHALL define each Brand_Palette color as a CSS variable.
9. THE wwwroot/css/site.css file SHALL override Bootstrap's primary color with Maroon #6B1A1A for buttons, outline buttons, links, background and text utilities, form focus rings, checked checkboxes, active pagination items, and active nav pills.
10. THE wwwroot/css/site.css file SHALL override Bootstrap's success color with Deep Green #2D4A2D.
11. THE Admin_Layout SHALL render a light gray page background, white content cards with rounded corners and a soft shadow, Sand #D4B896 borders on cards and tables, and Cream #FBF3EA as the tint for secondary surfaces such as table headers and the highlighted Sidebar link.
12. THE Status_Badge SHALL use amber for Pending, green for Approved, Returned, and Verified, red for Rejected and Suspended, Maroon for Active, Gold with dark text for Claimed, and gray for Archived.
13. THE Status_Badge SHALL show the status name as text in addition to the status color.
14. THE Admin_Layout and the Login_Page SHALL load the Brand_Logo from the Logo_Path_Setting.
15. THE Web_Project SHALL include an SVG recreation of the HeronSearch mark (maroon tall rounded arch outline, gold light rays and a gold four-point star at the top, a maroon heart rising out of an open maroon-outlined box, and a gold arc at the base) at `wwwroot/images/heronsearch-logo.svg` as the default Brand_Logo.
16. THE HeronSearch_Admin SHALL exclude the Forbidden_Name from all source files, documentation, and rendered pages.
17. THE Admin_Layout and the Minimal_Layout SHALL load every CSS, JavaScript, and font file from wwwroot, with zero external CDN URLs.
18. THE HeronSearch_Admin SHALL display dates and date-times in the Date_Format using the server's local time.

### Requirement 10: Feedback, accessibility, and responsiveness (Phase 1, all pages)

**User Story:** As a Back_Office_User, I want clear feedback and an accessible, responsive interface, so that I can work confidently on any device and with assistive technology.

#### Acceptance Criteria

1. WHEN an action succeeds, THE HeronSearch_Admin SHALL display a success Alert_Message that names the affected record.
2. WHEN an action fails or is blocked, THE HeronSearch_Admin SHALL display an error Alert_Message that states the reason and the suggested next step.
3. WHEN a list, table, or chart has zero records to show, THE HeronSearch_Admin SHALL display an Empty_State in place of the list, table, or chart.
4. WHEN a user starts a Delete, Archive, Mark as Returned, Verify, Suspend, or Change Role action, THE HeronSearch_Admin SHALL open a Confirmation_Modal that names the target record before sending the request.
5. WHEN the user cancels a Confirmation_Modal, THE HeronSearch_Admin SHALL close the modal and leave the record unchanged.
6. THE HeronSearch_Admin SHALL give every form field a visible label that is programmatically linked to the field.
7. THE HeronSearch_Admin SHALL give every icon-only button and link an accessible name.
8. THE HeronSearch_Admin SHALL make every interactive element reachable and operable with the keyboard and show a visible focus indicator on the focused element.
9. WHEN a modal opens, THE HeronSearch_Admin SHALL move keyboard focus into the modal.
10. WHEN a modal closes, THE HeronSearch_Admin SHALL return keyboard focus to the control that opened the modal.
11. THE HeronSearch_Admin SHALL render normal-size text with a contrast ratio of at least 4.5:1 against the text background.
12. THE Admin_Layout SHALL provide a "Skip to main content" link as the first focusable element on every Admin_Area page.
13. THE HeronSearch_Admin SHALL give every chart a text alternative that states the chart title and the values shown.
14. THE HeronSearch_Admin SHALL mark the column headers of every data table as header cells with column scope.
15. WHILE the viewport is 360 pixels wide or wider, THE HeronSearch_Admin SHALL display every page without horizontal page scrolling, with wide tables scrolling horizontally inside the table container.

### Requirement 11: Dashboard (Phase 1)

**User Story:** As a Back_Office_User, I want an overview of lost-and-found activity, so that I can see what needs attention.

#### Acceptance Criteria

1. THE Dashboard SHALL display five stat cards: Total Lost (count of Items with Type Lost), Total Found (count of Items with Type Found), Pending Claims (count of Claims with Status Pending), Returned Items (count of Items with Status Returned), and Registered Users (count of all Users).
2. THE Dashboard SHALL count items of every Item.Status, including Archived, in the Total Lost and Total Found cards.
3. THE Dashboard SHALL display a Chart.js line chart with a Lost series and a Found series that shows the item count per DateReported month for each month of the Six_Month_Window, labeled with month name and year, with 0 for months that have zero items.
4. THE Dashboard SHALL display a Chart.js bar chart of the 5 locations with the highest item counts across all items, sorted by count in descending order and then by location name.
5. THE Dashboard SHALL display a Pending Claims list of the 5 most recently filed Pending claims, each row showing the item name, the claimant's full name, the DateFiled, and a Review link, followed by a "View all" link to the Claims_Manager.
6. THE Dashboard SHALL display a Recent Activity list of the 10 most recent ActivityLog records, newest first, each row showing the Timestamp, the user's full name ("Unknown user" when UserId is null), the Action, and the Result.
7. THE Dashboard SHALL draw chart colors from the Brand_Palette.

### Requirement 12: Manage Items list, search, filters, and pagination (Phase 1)

**User Story:** As a Back_Office_User, I want to search and filter all item reports in one table, so that I can find any record fast.

#### Acceptance Criteria

1. THE Items_Manager SHALL display the heading "Manage Items" with a rounded "+ Add Found Item" primary button aligned to the right of the heading.
2. THE Items_Manager SHALL display items in a table with the columns Photo, Name, Category, Location, Date, Type, Status, Reporter, and Actions.
3. THE Items_Manager SHALL show the Actions View, Edit, Mark as Returned, Archive, and Delete for each eligible item, following the eligibility rules of Requirements 15 and 16.
4. WHERE an item has no ImagePath, THE Items_Manager SHALL display a placeholder image with the item category's icon in the Photo column.
5. THE Items_Manager SHALL provide a rounded search input with a search icon that matches the search term, case-insensitively, against Item.Name, Item.Description, and Item.Location.
6. IF the search term exceeds 100 characters, THEN THE Items_Manager SHALL use the first 100 characters of the trimmed search term.
7. THE Items_Manager SHALL provide a Status filter as Pill_Chips (All, Active, Claimed, Returned, Archived), a Category filter as a dropdown of the Category_List with an "All categories" option, and a Type filter as a dropdown (All types, Lost, Found).
8. WHEN the user applies a search term and filters, THE Items_Manager SHALL show only the items that match the search term and every selected filter.
9. THE Items_Manager SHALL sort items by DateReported, newest first.
10. THE Items_Manager SHALL show 10 items per page with Previous, Next, and page-number controls and the text "Showing X–Y of Z items".
11. WHEN the user moves to another page, THE Items_Manager SHALL keep the current search term and filter values.
12. IF the requested page number is below 1 or above the last page, THEN THE Items_Manager SHALL display the nearest valid page.
13. WHEN zero items match the search term and filters, THE Items_Manager SHALL display an Empty_State with a "Clear filters" link.

### Requirement 13: Item details modal (Phase 1)

**User Story:** As a Back_Office_User, I want to see a full item record in a modal on the list page, so that I can check details without losing my place in the list.

#### Acceptance Criteria

1. WHEN the View action is activated for an item, THE Items_Manager SHALL open a details modal that shows the item's photo (or placeholder image), Name, Category, Location, DateReported, Type, Status, Description, the reporter's full name, and the item's claim count.
2. WHEN the user activates the modal's Close button or presses the Escape key, THE Items_Manager SHALL close the details modal.

### Requirement 14: Add and edit items (Phase 1)

**User Story:** As a Back_Office_User, I want to register found items with a photo and correct item records, so that the item list stays accurate.

#### Acceptance Criteria

1. THE Items_Manager SHALL provide an "Add Found Item" form with the fields Name (required, 2 to 100 characters), Category (required, Category_List value), Location (required, Location_List value), Date Found (required), Description (optional, up to 1000 characters), and Photo (optional image file).
2. THE Items_Manager SHALL prefill the Date Found field with the current date.
3. IF the submitted Date Found is later than the current date, THEN THE Items_Manager SHALL display the validation message "Date cannot be in the future." and keep the item unsaved.
4. WHEN a valid Add Found Item form is submitted, THE Items_Manager SHALL save a new Item with Type Found, Status Active, DateReported set to the submitted date, and ReportedByUserId set to the signed-in Back_Office_User.
5. WHEN the Edit action is activated for an item, THE Items_Manager SHALL display an edit form prefilled with the item's Name, Category, Location, DateReported, Description, and current photo, with Type and Status shown as read-only values.
6. THE Items_Manager SHALL validate the edit form with the field rules of criteria 1 and 3.
7. WHEN a valid edit form is submitted without a new photo, THE Items_Manager SHALL save the edited fields and keep the current ImagePath.
8. WHEN a valid edit form is submitted with a new valid photo, THE Items_Manager SHALL save the new photo, update ImagePath, and delete the previous uploaded file from wwwroot/uploads.
9. IF the photo of an add or edit submission fails validation, THEN THE Items_Manager SHALL display the Image_Upload_Validator's error message next to the Photo field and keep the other entered values.

### Requirement 15: Mark as Returned and Archive (Phase 1)

**User Story:** As a Back_Office_User, I want to mark items as returned or archive old records, so that item statuses reflect reality.

#### Acceptance Criteria

1. THE Items_Manager SHALL show the Mark as Returned action for items with Status Active or Claimed.
2. WHEN a Back_Office_User confirms Mark as Returned for an item with Status Active or Claimed and zero Pending claims, THE Items_Manager SHALL set the item's Status to Returned.
3. THE Items_Manager SHALL show the Archive action for items with Status Active, Claimed, or Returned.
4. WHEN a Back_Office_User confirms Archive for an item with Status Active, Claimed, or Returned and zero Pending claims, THE Items_Manager SHALL set the item's Status to Archived.
5. IF an item has one or more Pending claims when Mark as Returned or Archive is submitted, THEN THE Items_Manager SHALL keep the item's Status and display the error "Resolve the pending claim(s) for this item in Manage Claims first."
6. IF Mark as Returned or Archive is submitted for an item whose Status is outside the eligible statuses of criteria 1 and 3, THEN THE Items_Manager SHALL keep the item's Status and display an error that names the current Status.

### Requirement 16: Delete items (Phase 1)

**User Story:** As an Admin, I want to delete mistaken item records safely, so that bad data is removed without breaking claim history.

#### Acceptance Criteria

1. THE Items_Manager SHALL show the Delete action to Admin_Users only.
2. WHEN an Admin_User activates Delete for an item, THE Items_Manager SHALL open a Confirmation_Modal with the text "Delete '[item name]'? This cannot be undone."
3. WHEN an Admin_User confirms Delete for an item with zero Item_Dependents, THE Items_Manager SHALL delete the item and delete the item's uploaded photo file from wwwroot/uploads.
4. IF an Admin_User confirms Delete for an item with one or more Item_Dependents, THEN THE Items_Manager SHALL keep the item and display the error "Cannot delete '[item name]' because the item has [N] claim(s). Archive the item instead."
5. THE Items_Manager SHALL limit photo file deletions to files located inside wwwroot/uploads.
6. WHEN an item is deleted, THE Activity_Logger SHALL record a Result that names the deleted item and the item's Id, for example "Success: Deleted item 'Blue umbrella' (#12)".

### Requirement 17: Image upload validation (Phase 1)

**User Story:** As an administrator, I want uploaded photos checked before saving, so that only safe image files reach the server.

#### Acceptance Criteria

1. THE Image_Upload_Validator SHALL accept a file only when the file extension is .jpg, .jpeg, .png, or .webp and the file content starts with the matching JPEG, PNG, or WebP file signature.
2. IF an uploaded file fails the check of criterion 1, THEN THE Image_Upload_Validator SHALL reject the file with the message "Photo must be a JPG, PNG, or WEBP image."
3. IF an uploaded file exceeds 5 MB (5,242,880 bytes), THEN THE Image_Upload_Validator SHALL reject the file with the message "Photo must be 5 MB or smaller."
4. IF an uploaded file has a size of 0 bytes, THEN THE Image_Upload_Validator SHALL reject the file with the message "The selected photo is empty."
5. WHEN an uploaded file passes validation, THE Image_Upload_Validator SHALL save the file in wwwroot/uploads under a newly generated unique file name plus the validated extension, ignoring the original file name.
6. WHEN an uploaded file is saved, THE Image_Upload_Validator SHALL return the relative URL path (for example `/uploads/<generated-name>.jpg`) for storage in Item.ImagePath.
7. WHILE the wwwroot/uploads folder is missing, THE Image_Upload_Validator SHALL create the folder before saving the first file.

### Requirement 18: Manage Claims lists (Phase 2)

**User Story:** As a Back_Office_User, I want claims grouped by status, so that I can focus on pending reviews.

#### Acceptance Criteria

1. THE Claims_Manager SHALL display the tabs Pending, Approved, and Rejected, each with a count of the claims in that status, with the Pending tab selected by default.
2. THE Claims_Manager SHALL list the claims of the selected tab in a table with the columns Claim #, Item (photo and name), Claimant (full name and student ID), Date Filed, Status, and Action.
3. THE Claims_Manager SHALL sort the Pending tab by DateFiled, newest first, and the Approved and Rejected tabs by ReviewedAt, newest first.
4. THE Claims_Manager SHALL show 10 claims per page with the pagination controls of Requirement 12.
5. WHEN the selected tab has zero claims, THE Claims_Manager SHALL display an Empty_State that names the tab.
6. THE Claims_Manager SHALL show a "Review" action for Pending claims and a "View" action for Approved and Rejected claims.

### Requirement 19: Claim review, approval, and rejection (Phase 2)

**User Story:** As a Back_Office_User, I want to compare the item record with the claimant's proof before deciding, so that items go back to their rightful owners.

#### Acceptance Criteria

1. THE Claims_Manager SHALL display the review page with the item record (photo, Name, Category, Location, DateReported, Type, Status, Description, and reporter) next to the claimant's details (full name, email, student ID, and VerificationStatus) and the claimant's ProofDescription and DateFiled.
2. THE Claims_Manager SHALL display a verification checklist with at least 4 checks on the review page, including identity match with the student ID on record, proof matches the item's description, claimant named identifying details absent from the public listing, and the date and location of loss match the item record.
3. WHILE one or more checklist items are unchecked, THE Claims_Manager SHALL keep the Approve button disabled.
4. WHEN a Back_Office_User submits Approve for a Pending claim with every checklist item confirmed on the server, the item at Status Active, and the claimant at VerificationStatus other than Suspended, THE Claims_Manager SHALL set the claim's Status to Approved, ReviewedByUserId to the acting user, ReviewedAt to the current date and time, and the item's Status to Claimed.
5. WHEN a claim is approved, THE Claims_Manager SHALL set every other Pending claim on the same item to Rejected with the RejectionReason "Another claim for this item was approved.", the acting user as reviewer, and the current date and time as ReviewedAt.
6. IF an Approve submission arrives with one or more checklist items unconfirmed, THEN THE Claims_Manager SHALL keep the claim Pending and display the error "Complete every verification check before approving."
7. IF the claimant's VerificationStatus is Suspended, THEN THE Claims_Manager SHALL keep the claim Pending, display the error "The claimant's account is suspended.", and show a warning on the review page.
8. IF the item's Status is other than Active when Approve is submitted, THEN THE Claims_Manager SHALL keep the claim Pending and display an error that names the item's current Status.
9. WHEN a Back_Office_User activates Reject, THE Claims_Manager SHALL open a modal with a required "Reason for rejection" text area.
10. WHEN a Back_Office_User submits Reject with a reason of 10 to 500 characters for a Pending claim, THE Claims_Manager SHALL set the claim's Status to Rejected, RejectionReason to the submitted reason, ReviewedByUserId to the acting user, and ReviewedAt to the current date and time, and keep the item's Status unchanged.
11. IF a Reject submission has a reason shorter than 10 characters or longer than 500 characters after trimming, THEN THE Claims_Manager SHALL keep the claim Pending and display the validation message "Enter a reason of 10 to 500 characters."
12. IF an Approve or Reject submission targets a claim whose Status is no longer Pending, THEN THE Claims_Manager SHALL keep the claim unchanged and display the error "This claim was already reviewed by [reviewer name]."
13. WHEN an Approve or Reject submission succeeds, THE Claims_Manager SHALL redirect to the Pending tab and display a success Alert_Message that names the claim and the item.
14. WHEN the View action is activated for an Approved or Rejected claim, THE Claims_Manager SHALL display the review page in read-only form with the reviewer's full name, the ReviewedAt value, and, for Rejected claims, the RejectionReason.

### Requirement 20: Manage Users list and filters (Phase 2, Admin only)

**User Story:** As an Admin, I want to see and filter every account, so that I can manage who uses HeronSearch.

#### Acceptance Criteria

1. THE Users_Manager SHALL display the heading "Manage Users" with a rounded "+ Add Staff Account" primary button aligned to the right of the heading.
2. THE Users_Manager SHALL display users in a table with the columns Name, Email, Student ID ("—" when empty), Role, Status, Joined (CreatedAt), and Actions (Verify, Suspend, Change role, Delete).
3. THE Users_Manager SHALL provide a Status filter as Pill_Chips (All, Pending, Verified, Suspended), a Role filter as a dropdown (All roles, Admin, Staff, Student), and a search input that matches the search term, case-insensitively, against FullName, Email, and StudentId.
4. THE Users_Manager SHALL sort users by CreatedAt, newest first, and show 10 users per page with the pagination controls of Requirement 12.
5. WHEN zero users match the search term and filters, THE Users_Manager SHALL display an Empty_State with a "Clear filters" link.

### Requirement 21: Manage Users actions (Phase 2, Admin only)

**User Story:** As an Admin, I want to verify, suspend, re-role, add, and delete accounts safely, so that access stays correct without losing history.

#### Acceptance Criteria

1. THE Users_Manager SHALL show the Verify action for users with VerificationStatus Pending or Suspended and the Suspend action for users with VerificationStatus Pending or Verified.
2. WHEN an Admin_User confirms Verify for a user, THE Users_Manager SHALL set the user's VerificationStatus to Verified.
3. WHEN an Admin_User confirms Suspend for a user, THE Users_Manager SHALL set the user's VerificationStatus to Suspended.
4. WHEN an Admin_User activates Change role, THE Users_Manager SHALL open a Confirmation_Modal with a Role dropdown (Admin, Staff, Student) preselected with the user's current Role.
5. WHEN an Admin_User confirms a new Role in the Change role modal, THE Users_Manager SHALL save the new Role and record the old and new Role in the ActivityLog Result.
6. WHEN an Admin_User confirms Delete for a user with zero User_Dependents, THE Users_Manager SHALL delete the user.
7. IF an Admin_User confirms Delete for a user with one or more User_Dependents, THEN THE Users_Manager SHALL keep the user and display the error "Cannot delete '[full name]' because the account has [N] reported item(s) and [M] claim(s). Suspend the account instead."
8. IF an Admin_User submits Suspend, Change role, or Delete for the Admin_User's own account, THEN THE Users_Manager SHALL keep the account unchanged and display the error "You cannot change or delete your own account."
9. THE Users_Manager SHALL provide an "Add Staff Account" form with the fields Full Name (required, 2 to 100 characters), Email (required, valid email format, up to 150 characters), Staff ID (optional, up to 20 characters, stored in StudentId), Password (required, meets the Password_Rule), and Confirm Password (required).
10. WHEN a valid Add Staff Account form is submitted, THE Users_Manager SHALL save a new User with Role Staff, VerificationStatus Verified, CreatedAt set to the current date and time, and the Password_Hasher hash of the password.
11. IF the submitted Email already belongs to a User, THEN THE Users_Manager SHALL keep the form open and display the validation message "An account with this email already exists."
12. IF the Confirm Password value differs from the Password value, THEN THE Users_Manager SHALL keep the form open and display the validation message "Passwords do not match."

### Requirement 22: Reports overview and charts (Phase 2)

**User Story:** As a Back_Office_User, I want statistics for a chosen date range, so that I can report lost-and-found trends to the university.

#### Acceptance Criteria

1. THE Reports_Page SHALL provide From and To date filters with the default range from the first day of the earliest Six_Month_Window month to the current date.
2. IF the From date is later than the To date, THEN THE Reports_Page SHALL display the validation message "From date must be on or before To date." and show results for the default range.
3. THE Reports_Page SHALL display, for items with DateReported inside the range and claims with DateFiled inside the range, the summary cards Lost Items, Found Items, Returned Items, Claims Filed, Claims Approved, and Claims Rejected.
4. THE Reports_Page SHALL display a Chart.js chart of item counts by location for items inside the range, sorted by count in descending order.
5. THE Reports_Page SHALL display a Chart.js chart of Lost and Found item counts per month for every month that overlaps the range.
6. THE Reports_Page SHALL display a Chart.js chart of item counts per hour of day (24 buckets, 00 to 23) based on DateReported for items inside the range.
7. THE Reports_Page SHALL display a Chart.js chart of item counts by category for items inside the range.
8. WHEN zero items fall inside the range, THE Reports_Page SHALL display an Empty_State in place of each chart.

### Requirement 23: System logs and CSV export (Phase 2)

**User Story:** As a Back_Office_User, I want to search the activity history and export data to CSV, so that I can audit actions and share reports.

#### Acceptance Criteria

1. THE Reports_Page SHALL provide a "System Logs" tab with a table of ActivityLog records inside the date range, with the columns Timestamp, User, Action, Item, and Result, sorted newest first.
2. THE Reports_Page SHALL display "Unknown user" in the User column when ActivityLog.UserId is null and "—" in the Item column when ActivityLog.TargetItemId is null.
3. THE Reports_Page SHALL provide a search input on the System Logs tab that matches the search term, case-insensitively, against the user's full name, Action, and Result.
4. THE Reports_Page SHALL show 20 ActivityLog records per page on the System Logs tab with the pagination controls of Requirement 12.
5. WHEN the user activates "Export to CSV" on the Overview tab, THE CSV_Exporter SHALL download a file named `heronsearch-items-YYYYMMDD-YYYYMMDD.csv` that lists every item inside the date range with the columns Item #, Name, Category, Location, Date Reported, Type, Status, and Reported By.
6. WHEN the user activates "Export to CSV" on the System Logs tab, THE CSV_Exporter SHALL download a file named `heronsearch-logs-YYYYMMDD-YYYYMMDD.csv` that lists every ActivityLog record matching the date range and search term with the columns Timestamp, User, Action, Item, and Result.
7. THE CSV_Exporter SHALL write UTF-8 text with a byte order mark, a header row, comma separators, and double quotes around values that contain commas, quotes, or line breaks, with inner quotes doubled.
8. THE CSV_Exporter SHALL prefix with a single quote every value that starts with `=`, `+`, `-`, `@`, a tab, or a carriage return.

### Requirement 24: About page (Phase 2)

**User Story:** As a Back_Office_User, I want a page that explains HeronSearch, so that new staff understand the system and the brand.

#### Acceptance Criteria

1. THE About_Page SHALL display a short description of HeronSearch as UMak's Lost & Found Management System.
2. THE About_Page SHALL list the five features Lost Item Reporting, Found Item Registration, Search and Filtering, User and Item Verification, and Lost-and-Found Statistics, each with an icon and a one-sentence description.
3. THE About_Page SHALL list the four user roles Administration, Staff/Personnel, Student/User, and Guest, each with a one-sentence description.
4. THE About_Page SHALL explain the logo meaning: Box = keeper of lost things, Light = guide for lost things, and Heart = compassion to help.
5. THE About_Page SHALL describe the Arch of the logo as unity, protection, and a safe space for the community, as shown on the brand sheet.
6. THE About_Page SHALL display the credit "A project of AnagolayCorp, University of Makati."

### Requirement 25: Logout flow (Phase 2)

**User Story:** As a Back_Office_User, I want to confirm before signing out and see a clear signed-out page, so that I end my session on purpose.

#### Acceptance Criteria

1. WHEN the Logout link in the Sidebar is activated, THE Logout_Flow SHALL open a Confirmation_Modal with the text "Sign out of HeronSearch?" and the buttons "Cancel" and "Sign out".
2. WHEN the user confirms "Sign out", THE Logout_Flow SHALL send a POST request that ends the session by removing the authentication cookie.
3. WHEN the session ends, THE Logout_Flow SHALL display the Signed_Out_Page on the Minimal_Layout with the text "You've been signed out" and a "Sign in again" button that links to the Login_Page.
4. WHEN the user activates "Cancel", THE Logout_Flow SHALL close the modal and keep the user signed in on the current page.

### Requirement 26: README (Phase 1, updated in Phase 2)

**User Story:** As a student developer, I want plain-language setup instructions, so that I can run the app myself.

#### Acceptance Criteria

1. THE README SHALL use plain beginner language and numbered steps.
2. THE README SHALL list the prerequisites: Visual Studio Community 2026, the .NET 10 SDK, and MySQL Server 8.0.46 listening on port 3307.
3. THE README SHALL explain, in order, how to open the .sln file in Visual Studio, replace `YOUR_PASSWORD_HERE` in appsettings.json with the real MySQL password, run `dotnet tool restore`, run `dotnet ef database update`, start the app, and sign in with the demo Admin_User.
4. THE README SHALL list the demo Admin_User email and password and the demo Staff_User email and password, with a note that the passwords are demo-only and must be changed.
5. THE README SHALL explain how to replace the demo Admin_User: either edit the demo password in the Seeder before the first start, or create a new account with Add Staff Account, change the new account's role to Admin, sign in with the new account, and suspend the demo Admin_User.
6. THE README SHALL explain why the Solution uses net10.0 and MySql.EntityFrameworkCore 10.0.9: Pomelo.EntityFrameworkCore.MySql has no EF Core 10 release, .NET 8 support ends on 2026-11-10, and the Oracle provider performs no server-version auto-detection, so migrations are created without a database connection.
7. THE README SHALL state that `new MySqlServerVersion(new Version(8, 0, 46))` belongs to the Pomelo provider and is absent from the code for the reason given in criterion 6.
8. THE README SHALL state the license of MySql.EntityFrameworkCore (GPL-2.0-only with the Universal FOSS Exception 1.0).
9. THE README SHALL list the exact versions of MySql.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Design, dotnet-ef, Bootstrap, Bootstrap Icons, and Chart.js used by the Solution.
10. THE README SHALL list the project files and folders with a one-line description of what each one does.
11. THE README SHALL explain how to replace the Brand_Logo with the official logo file by copying the file into wwwroot/images and setting the Logo_Path_Setting, with zero code changes.
12. THE README SHALL include troubleshooting steps for a wrong MySQL password, a wrong port, a missing `heronsearch` database or tables, and a missing `dotnet ef` command.
13. THE README SHALL remind the user to keep the real MySQL password private when sharing the project files.
14. WHEN Phase_2 is complete, THE README SHALL describe every page of both phases and list every file added in Phase_2.

### Requirement 27: Build process, phases, and checkpoint (Phase 1 and Phase 2)

**User Story:** As a student developer, I want the work built in two verified phases with a stop in between, so that I can check Phase 1 before more code is added.

#### Acceptance Criteria

1. THE Build_Process SHALL execute Allowed_Commands only.
2. THE Build_Process SHALL complete Phase_1 and Phase_2 with zero connections opened to the Database.
3. THE Build_Process SHALL add the Web_Project to the .sln file and add NuGet package references by editing the .sln and .csproj files, then running `dotnet restore`.
4. THE Build_Process SHALL download Bootstrap Icons (CSS plus woff and woff2 fonts) and the Chart.js UMD minified build into wwwroot/lib at pinned versions, after checking the latest stable versions at implementation time.
5. IF `dotnet build` reports one or more errors, THEN THE Build_Process SHALL fix the reported errors and rerun `dotnet build` until the build reports zero errors.
6. THE Build_Process SHALL create the complete schema of Requirement 3 in the InitialCreate migration during Phase_1, so that Phase_2 adds zero migrations.
7. WHILE the Phase_1_Checkpoint is active, THE HeronSearch_Admin SHALL display a Placeholder_Page for Manage Claims, Manage Users, Reports / History, and About.
8. WHILE the Phase_1_Checkpoint is active, THE Sidebar SHALL sign the user out through a POST request from the Logout link and redirect to the Login_Page.
9. WHEN Phase_1 builds with zero errors and the InitialCreate migration exists, THE Build_Process SHALL stop and present the Phase_1_Summary.
10. THE Phase_1_Summary SHALL state the target framework and the reason for choosing net10.0, the provider package and exact version with the reason for choosing the Oracle provider over Pomelo, the exact versions of Microsoft.EntityFrameworkCore.Design, dotnet-ef, and the Offline_Libraries, the list of files created, and the exact next steps the user must take.
11. THE Phase_1_Summary SHALL tell the user that the logo is an SVG recreation and explain how to supply the official logo file.
12. WHILE the Phase_1_Checkpoint is active, THE Build_Process SHALL defer all Phase_2 work.
13. WHEN the user sends the message "continue" during the Phase_1_Checkpoint, THE Build_Process SHALL build the Phase_2 scope listed in the Introduction.
14. WHEN Phase_2 builds with zero errors, THE Build_Process SHALL present a short summary of the files created and changed in Phase_2 and the steps the user must take next.
