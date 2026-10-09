using System.ComponentModel.DataAnnotations;
using HeronSearch.Admin.Helpers;

namespace HeronSearch.Admin.ViewModels;

// The student Create Account form: the new student's name, email, optional Student ID, and password (typed twice).
// AccountController.Register binds and validates it, then creates a Pending Student account.
public class RegisterViewModel
{
    // The student's full name, 2 to 100 characters.
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be 2 to 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    // The sign-in email: required, at most 150 characters, and shaped like an email address.
    [Required(ErrorMessage = "Enter your email address.")]
    [StringLength(150, ErrorMessage = "Email cannot be longer than 150 characters.")]
    [RegularExpression(BusinessRules.EmailPattern, ErrorMessage = BusinessRules.EmailMessage)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    // The optional Student ID, at most 20 characters.
    [StringLength(20, ErrorMessage = "Student ID cannot be longer than 20 characters.")]
    [Display(Name = "Student ID")]
    public string? StudentId { get; set; }

    // The password: 8 to 100 characters with at least one letter and one digit (the shared Password_Rule).
    [Required(ErrorMessage = "Enter a password.")]
    [RegularExpression(BusinessRules.PasswordPattern, ErrorMessage = BusinessRules.PasswordMessage)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    // The password again; it must match Password.
    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";
}
