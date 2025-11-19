using System.ComponentModel.DataAnnotations;

namespace BrightEnroll_DES.Models.ValidationAttributes;

// Makes a field required when "SameAsCurrentAddress" is false
public class RequiredIfNotSameAddressAttribute : ValidationAttribute
{
    public RequiredIfNotSameAddressAttribute()
    {
        ErrorMessage = "{0} is required";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Get the model instance
        var model = validationContext.ObjectInstance;
        
        // Get the SameAsCurrentAddress property
        var sameAsCurrentAddressProperty = model.GetType().GetProperty("SameAsCurrentAddress");
        if (sameAsCurrentAddressProperty == null)
        {
            return ValidationResult.Success; // If property doesn't exist, skip validation
        }

        var sameAsCurrentAddress = (bool)(sameAsCurrentAddressProperty.GetValue(model) ?? false);

        // If SameAsCurrentAddress is true, the field is not required
        if (sameAsCurrentAddress)
        {
            return ValidationResult.Success;
        }

        // If SameAsCurrentAddress is false, check if the field has a value
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

