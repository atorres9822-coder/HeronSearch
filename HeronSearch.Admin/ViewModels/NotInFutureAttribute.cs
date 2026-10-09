using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HeronSearch.Admin.ViewModels;

// Validation attribute for a DateTime that may not be later than today's date.
// Server rule: valid when value.Date <= DateTime.Today. Dates are compared by day, so the time of day is ignored.
// Client: emits data-val-notfuture (and its message) so js/validation-rules.js can compare the yyyy-MM-dd value with the browser's today.
public sealed class NotInFutureAttribute : ValidationAttribute, IClientModelValidator
{
    // The message shown when the date is in the future; matches Requirement 7.3 exactly.
    public const string DefaultMessage = "Date cannot be in the future.";

    // The data-val rule key the client adapter registers for this attribute.
    public const string RuleName = "notfuture";

    // Sets the default error message used when the attribute is applied without an explicit one.
    public NotInFutureAttribute() : base(DefaultMessage)
    {
    }

    // Returns Success when the value is null (Required handles emptiness) or its date part is today or earlier; otherwise an error.
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is DateTime date)
        {
            return date.Date <= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(validationContext.DisplayName), new[] { validationContext.MemberName ?? string.Empty });
        }

        // Any non-DateTime value cannot be a future date, so it passes this rule.
        return ValidationResult.Success;
    }

    // Adds data-val, data-val-notfuture (the message) so the client adapter can enforce the same rule.
    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-" + RuleName, FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
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
