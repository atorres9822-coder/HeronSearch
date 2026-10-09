namespace HeronSearch.Admin.Helpers;

// Holds the authorization policy names used in Program.cs and in [Authorize] attributes.
public static class Policies
{
    // Signed-in Admin or Staff users: Dashboard, Manage Items, Manage Claims, Reports, About, and Logout.
    public const string BackOffice = "BackOffice";

    // Signed-in Admin users only: Manage Users and every delete action.
    public const string AdminOnly = "AdminOnly";

    // Signed-in Verified Student users only: the student dashboard and every student-only action.
    public const string StudentOnly = "StudentOnly";
}
