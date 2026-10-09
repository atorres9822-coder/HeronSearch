namespace HeronSearch.Admin.ViewModels;

// The model of the Not Found page: the name and URL of the page that its link opens. A missing record links back
// to its list (for example "Manage Items" and /Items); an unknown URL links to the Dashboard, which is also the default.
public class NotFoundViewModel
{
    // The name of the linked page, such as "Manage Items" or "Dashboard".
    public string ListName { get; set; } = "Dashboard";

    // The local URL of the linked page, such as "/Items". The site root "/" opens the Dashboard.
    public string ListUrl { get; set; } = "/";
}
