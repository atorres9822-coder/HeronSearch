using System.ComponentModel.DataAnnotations;

namespace HeronSearch.Admin.ViewModels;

// The Claim this item form: the item being claimed (shown read-only) and the claimant's proof of ownership text.
// Contact is always the signed-in student's email, so there is no contact field. StudentController.Claim binds it.
public class ClaimFormViewModel
{
    // The Id of the Active Found item being claimed; used to re-load and re-validate the item on POST.
    public int ItemId { get; set; }

    // The item's name, shown at the top of the form so the student sees what they are claiming (display only).
    public string ItemName { get; set; } = "";

    // The item's category, used to pick the placeholder icon in the photo (display only).
    public string ItemCategory { get; set; } = "";

    // The item's photo URL path, or null when there is no photo (display only).
    public string? ItemImagePath { get; set; }

    // The signed-in student's email, shown read-only as the contact for this claim (display only).
    public string ContactEmail { get; set; } = "";

    // The claimant's description of why the item is theirs; required, 10 to 500 characters.
    [Required(ErrorMessage = "Describe why this item is yours.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Proof of ownership must be 10 to 500 characters.")]
    [Display(Name = "Proof of ownership")]
    public string ProofDescription { get; set; } = "";
}
