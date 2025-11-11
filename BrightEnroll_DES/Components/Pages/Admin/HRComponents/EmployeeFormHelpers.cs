namespace BrightEnroll_DES.Components.Pages.Admin.HRComponents;

// Helper functions for formatting employee names and validating contact numbers
public static class EmployeeFormHelpers
{
    // Formats names so the first letter of each word is capitalized (e.g., "iVan JOsh" becomes "Ivan Josh")
    public static string FormatName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var formattedWords = words.Select(word =>
        {
            if (string.IsNullOrWhiteSpace(word))
                return word;

            // Capitalize first letter, lowercase the rest
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        });

        return string.Join(" ", formattedWords);
    }

    // Checks if a contact number contains only digits and is within the allowed length
    public static bool IsValidContactNumber(string contactNumber, int maxLength = 11)
    {
        if (string.IsNullOrWhiteSpace(contactNumber))
            return false;

        // Remove any spaces or dashes
        var cleaned = contactNumber.Replace(" ", "").Replace("-", "");

        // Check if all characters are digits
        if (!cleaned.All(char.IsDigit))
            return false;

        // Check length
        return cleaned.Length <= maxLength;
    }

    // Removes all non-digit characters from a contact number
    public static string CleanContactNumber(string contactNumber)
    {
        if (string.IsNullOrWhiteSpace(contactNumber))
            return "";

        // Remove all non-digit characters
        return new string(contactNumber.Where(char.IsDigit).ToArray());
    }
}

