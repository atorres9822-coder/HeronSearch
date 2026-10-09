using System.ComponentModel.DataAnnotations;
using HeronSearch.Admin.Helpers;

namespace HeronSearch.Admin.ViewModels;

// The sign-in form: the email and password typed on the Login page, the Remember me choice, and the page the
// user was heading to. AccountController.Login binds it, validates it, and signs the user in when it checks out.
public class LoginViewModel
{
    // The sign-in email. It must be filled in, no longer than 150 characters, and shaped like an email address.
    [Display(Name = "Email Address")]
    [Required(ErrorMessage = "Enter your email address.")]
    [StringLength(150, ErrorMessage = "Email cannot be longer than 150 characters.")]
    [RegularExpression(BusinessRules.EmailPattern, ErrorMessage = BusinessRules.EmailMessage)]
    public string Email { get; set; } = "";

    // The password. It must be filled in; the stored hash decides whether it is correct, so there is no format rule here.
    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    // When true, the sign-in cookie lasts 14 days and survives closing the browser; otherwise it ends with the session.
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    // The local page the user was trying to open before sign-in; used to send them back there afterward. Optional.
    public string? ReturnUrl { get; set; }
}
