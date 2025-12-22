using System.ComponentModel.DataAnnotations;

namespace BrightEnroll_DES.Models.ValidationAttributes;

public class RequiredIfNotSameAddressAttribute : ValidationAttribute
{
    public RequiredIfNotSameAddressAttribute()
    {
        ErrorMessage = "{0} is required";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var model = validationContext.ObjectInstance;
        
        var sameAsCurrentAddressProperty = model.GetType().GetProperty("SameAsCurrentAddress");
        if (sameAsCurrentAddressProperty == null)
        {
            return ValidationResult.Success;
        }

        var sameAsCurrentAddress = (bool)(sameAsCurrentAddressProperty.GetValue(model) ?? false);

        if (sameAsCurrentAddress)
        {
            return ValidationResult.Success;
        }

        if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
        {
            var displayName = validationContext.DisplayName;
            if (string.IsNullOrEmpty(ErrorMessageResourceName) && string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = $"{displayName} is required";
            }
            return new ValidationResult(FormatErrorMessage(displayName));
        }

        return ValidationResult.Success;
    }
}

