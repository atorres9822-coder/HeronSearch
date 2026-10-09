namespace HeronSearch.Admin.Models;

// The fixed item categories and campus locations, plus the icon for each category.
// Forms, filters, the seeder, and validation all read these lists, so the values are defined only here.
public static class ItemLists
{
    // Every item category, in the order shown in dropdowns and filters.
    public static readonly IReadOnlyList<string> Categories = new[] { "ID Card", "Tumbler", "Umbrella",
        "Calculator", "Laptop", "Phone & Gadgets", "Bag", "Wallet", "Keys", "Books & Notes", "Clothing", "Other" };

    // Every campus location, in the order shown in dropdowns.
    public static readonly IReadOnlyList<string> Locations = new[] { "Library", "Canteen", "Classroom 101",
        "Classroom 205", "Classroom 312", "Computer Laboratory", "Gymnasium", "University Oval", "Main Lobby",
        "Parking Area", "Other" };

    // Returns the Bootstrap Icons class for a category; "Other" and unknown values get the box icon.
    public static string IconFor(string category) => category switch
    {
        "ID Card" => "bi-person-vcard",
        "Tumbler" => "bi-cup-straw",
        "Umbrella" => "bi-umbrella",
        "Calculator" => "bi-calculator",
        "Laptop" => "bi-laptop",
        "Phone & Gadgets" => "bi-phone",
        "Bag" => "bi-backpack",
        "Wallet" => "bi-wallet2",
        "Keys" => "bi-key",
        "Books & Notes" => "bi-book",
        "Clothing" => "bi-tag",
        "Other" => "bi-box-seam",
        _ => "bi-box-seam"
    };
}
