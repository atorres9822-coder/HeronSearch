using System.ComponentModel.DataAnnotations;

namespace HeronSearch.Admin.ViewModels;

// The student Profile form: the read-only email plus the editable Full Name and Student ID. The change-password
// fields live on a separate form (ChangePasswordViewModel). StudentController.Profile binds and validates it.
public class StudentProfileViewModel
{
    // The student's sign-in email, shown read-only (students cannot change their email here).
    public string Email { get; set; } = "";

    // The student's full name; editable, 2 to 100 characters.
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be 2 to 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    // The student's Student ID; editable, optional, at most 20 characters.
    [StringLength(20, ErrorMessage = "Student ID cannot be longer than 20 characters.")]
    [Display(Name = "Student ID")]
    public string? StudentId { get; set; }
}
