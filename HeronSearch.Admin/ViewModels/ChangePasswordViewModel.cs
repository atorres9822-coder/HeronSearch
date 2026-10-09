using System.ComponentModel.DataAnnotations;
using HeronSearch.Admin.Helpers;

namespace HeronSearch.Admin.ViewModels;

// The student Change Password form: the current password (verified with IPasswordHasher) plus the new password
// (which must match BusinessRules.PasswordPattern) typed twice. StudentController.ChangePassword binds it.
public class ChangePasswordViewModel
{
    // The student's current password, verified against the stored hash before any change is made.
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = "";

    // The new password: 8 to 100 characters with at least one letter and one digit (the shared Password_Rule).
    [Required(ErrorMessage = "Enter a new password.")]
    [RegularExpression(BusinessRules.PasswordPattern, ErrorMessage = BusinessRules.PasswordMessage)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = "";

    // The new password again; it must match NewPassword.
    [Required(ErrorMessage = "Confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = "";
}
