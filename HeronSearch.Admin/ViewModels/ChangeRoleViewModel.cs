using HeronSearch.Admin.Models;

namespace HeronSearch.Admin.ViewModels;

// The Change Role form: the requested role for one account, plus the page to return to afterward.
// UsersController.ChangeRole binds it.
public class ChangeRoleViewModel
{
    // The account whose role is changing.
    public int Id { get; set; }

    // The requested role (Admin, Staff, or Student).
    public UserRole Role { get; set; }

    // The local page to return to after the change, so the list keeps its filters and page.
    public string? ReturnUrl { get; set; }
}
