using System.ComponentModel.DataAnnotations;
using HeronSearch.Admin.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HeronSearch.Admin.ViewModels;

// Validation attribute for an optional uploaded item photo (an IFormFile).
// Server rule: a null file passes (Required/optional is handled elsewhere); a non-null file is checked with
// ImageUploadValidator.Check, which runs the empty, size, then type checks in that order and returns the exact message.
// Client: emits the data-* attributes below so js/validation-rules.js (task 14.2) can run the same checks before upload.
//
// Emitted client attributes (task 14.2 must read these exact names):
//   data-val                   = "true"
//   data-val-photo             = ImageUploadValidator.InvalidTypeMessage   (primary/extension message)
//   data-val-photo-maxbytes    = ImageUploadValidator.MaxBytes             (number, e.g. 5242880)
//   data-val-photo-extensions  = ImageUploadValidator.AllowedExtensionsText (e.g. ".jpg,.jpeg,.png,.webp")
//   data-val-photo-empty       = ImageUploadValidator.EmptyMessage
//   data-val-photo-toolarge    = ImageUploadValidator.TooLargeMessage
public sealed class PhotoFileAttribute : ValidationAttribute, IClientModelValidator
{
    // The data-val rule key the client adapter registers for this attribute.
    public const string RuleName = "photo";

    // Sets the default error message (the extension/type message) used when no explicit message is supplied.
    public PhotoFileAttribute() : base(ImageUploadValidator.InvalidTypeMessage)
    {
    }

    // Returns Success when there is no file, or when the file passes ImageUploadValidator.Check; otherwise the exact error.
    // IsValid is synchronous, so the header bytes are read synchronously (up to HeaderLength) from the upload stream.
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            // Null (no photo chosen) or any non-file value passes; required/optional is enforced elsewhere.
            return ValidationResult.Success;
        }

        // Read the leading bytes synchronously so the signature check in Check can run.
        var header = new byte[ImageUploadValidator.HeaderLength];
        int bytesRead;
        using (var stream = file.OpenReadStream())
        {
            bytesRead = stream.ReadAtLeast(header, ImageUploadValidator.HeaderLength, throwOnEndOfStream: false);
        }

        var error = ImageUploadValidator.Check(file.FileName, file.Length, header.AsSpan(0, bytesRead));
        return error is null
            ? ValidationResult.Success
            : new ValidationResult(error, new[] { validationContext.MemberName ?? string.Empty });
    }

    // Adds data-val plus the photo rule's limits and the three messages so the client adapter can enforce the same checks.
    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-" + RuleName, ImageUploadValidator.InvalidTypeMessage);
        MergeAttribute(context.Attributes, "data-val-" + RuleName + "-maxbytes", ImageUploadValidator.MaxBytes.ToString());
        MergeAttribute(context.Attributes, "data-val-" + RuleName + "-extensions", ImageUploadValidator.AllowedExtensionsText);
        MergeAttribute(context.Attributes, "data-val-" + RuleName + "-empty", ImageUploadValidator.EmptyMessage);
        MergeAttribute(context.Attributes, "data-val-" + RuleName + "-toolarge", ImageUploadValidator.TooLargeMessage);
    }

    // Adds the attribute only when the key is not already present, as the ASP.NET samples do.
    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
