namespace BrightEnroll_DES.Services.Infrastructure;

// Convert technical errors to user-friendly messages
public static class ErrorMessageHelper
{
    // Convert error message to readable format
    public static string ToHumanReadable(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return "An unexpected error occurred. Please try again.";

        var message = errorMessage.Trim();

        // Database connection errors
        if (message.Contains("A network-related", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("server was not found", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("provider: Named Pipes Provider", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("provider: TCP Provider", StringComparison.OrdinalIgnoreCase) ||
            (message.Contains("connection", StringComparison.OrdinalIgnoreCase) && 
             (message.Contains("network", StringComparison.OrdinalIgnoreCase) || 
              message.Contains("server", StringComparison.OrdinalIgnoreCase))))
        {
            return "Unable to connect to the database. Please check your database server is running and try again.";
        }
        
        // Timeout errors
        if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase) && 
            !message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "Database operation timed out. Please try again.";
        }
        
        // Table/object not found errors
        if (message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Could not find", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("object", StringComparison.OrdinalIgnoreCase) && message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Database table not found. Please ensure the database is properly initialized.";
        }
        
        // Stored procedure errors
        if (message.Contains("sp_CreateStudent", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("stored procedure", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("could not find", StringComparison.OrdinalIgnoreCase) && message.Contains("procedure", StringComparison.OrdinalIgnoreCase))
        {
            return "The database setup is incomplete. Please contact the administrator to initialize the database.";
        }

        // DbContext threading errors
        if (message.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("second operation", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("threading", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("concurrently", StringComparison.OrdinalIgnoreCase))
        {
            return "The system is processing another request. Please wait a moment and try again.";
        }

        // SQL errors
        if (message.Contains("SQL", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("violation", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("constraint", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return "This record already exists. Please check your input and try again.";
            }
            if (message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                return "The information you entered is invalid. Please check your selections and try again.";
            }
            return "There was a problem saving your data. Please check your input and try again.";
        }

        // Validation errors
        if (message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return "Please fill in all required fields before submitting.";
        }

        // Email errors
        if (message.Contains("email", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return "This email address is already registered. Please use a different email.";
            }
            return "The email address you entered is invalid. Please check and try again.";
        }

        // System ID errors
        if (message.Contains("system", StringComparison.OrdinalIgnoreCase) &&
            (message.Contains("ID", StringComparison.OrdinalIgnoreCase) || message.Contains("Id", StringComparison.OrdinalIgnoreCase)))
        {
            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return "This system ID is already in use. Please contact the administrator.";
            }
        }

        // Generic "Failed to" messages
        if (message.StartsWith("Failed to", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("add", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("register", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("create", StringComparison.OrdinalIgnoreCase))
            {
                return "We couldn't save your information. Please check your input and try again.";
            }
            if (message.Contains("load", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("fetch", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("get", StringComparison.OrdinalIgnoreCase))
            {
                return "We couldn't load the information. Please refresh the page and try again.";
            }
            if (message.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("edit", StringComparison.OrdinalIgnoreCase))
            {
                return "We couldn't update your information. Please check your input and try again.";
            }
            if (message.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("remove", StringComparison.OrdinalIgnoreCase))
            {
                return "We couldn't delete this record. Please try again later.";
            }
        }

        // Registration/Submission errors
        if (message.Contains("registration failed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("submission failed", StringComparison.OrdinalIgnoreCase))
        {
            return "We couldn't process your submission. Please check all fields and try again.";
        }

        // Clean up technical terms
        message = message.Replace("Exception:", "", StringComparison.OrdinalIgnoreCase);
        message = message.Replace("Error:", "", StringComparison.OrdinalIgnoreCase);
        message = message.Replace("System.", "", StringComparison.OrdinalIgnoreCase);
        message = message.Replace("Microsoft.", "", StringComparison.OrdinalIgnoreCase);
        message = message.Replace("EntityFrameworkCore.", "", StringComparison.OrdinalIgnoreCase);

        // Handle technical stack traces
        if (message.Contains(".") && message.Split('.').Length > 3)
        {
            return "An unexpected error occurred. Please try again. If the problem persists, contact support.";
        }

        // Format message capitalization and punctuation
        if (message.Length > 0)
        {
            message = char.ToUpper(message[0]) + message.Substring(1);
            if (!message.EndsWith(".") && !message.EndsWith("!") && !message.EndsWith("?"))
            {
                message += ".";
            }
        }

        return message;
    }
}

