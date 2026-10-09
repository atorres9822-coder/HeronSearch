namespace HeronSearch.Admin.ViewModels;

// The whole Profile page: the profile form, the change-password form, and which change-password errors to show.
// StudentController.Profile and ChangePassword fill it; Student/Profile.cshtml reads it.
public class StudentProfilePageViewModel
{
    // The read-only email and editable Full Name / Student ID form.
    public StudentProfileViewModel Profile { get; set; } = new();

    // The Change Password form; its fields stay empty except when it is being redisplayed after an error.
    public ChangePasswordViewModel Password { get; set; } = new();

    // True when a change-password attempt failed, so the view scrolls to and highlights that form.
    public bool ShowPasswordErrors { get; set; }
}
