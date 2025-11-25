namespace BrightEnroll_DES.Services.Business.Academic;

// Service that manages school year data and automatically handles finished school years
public class SchoolYearService
{
    private List<string> _schoolYears;

    public SchoolYearService()
    {
        _schoolYears = InitializeSchoolYears();
    }

    // Returns all available school years (current and future years)
    public List<string> GetAvailableSchoolYears()
    {
        return _schoolYears.OrderBy(sy => sy).ToList();
    }

    // Adds a new school year if it doesn't already exist and has valid format (e.g., 2025-2026)
    public bool AddSchoolYear(string schoolYear)
    {
        if (string.IsNullOrWhiteSpace(schoolYear))
            return false;

        // Validate format (e.g., 2025-2026)
        if (!System.Text.RegularExpressions.Regex.IsMatch(schoolYear, @"^\d{4}-\d{4}$"))
            return false;

        if (!_schoolYears.Contains(schoolYear))
        {
            _schoolYears.Add(schoolYear);
            return true;
        }

        return false;
    }

    // Removes a specific school year from the list
    public bool RemoveSchoolYear(string schoolYear)
    {
        return _schoolYears.Remove(schoolYear);
    }

    // Automatically removes school years that have ended (where the end year has already passed)
    public void RemoveFinishedSchoolYears()
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        
        // Determine the current school year's end year
        // If we're in June or later, the current SY ends next year
        // If we're before June, the current SY ends this year
        int currentEndYear;
        if (currentMonth >= 6)
        {
            currentEndYear = currentYear + 1;
        }
        else
        {
            currentEndYear = currentYear;
        }

        // Remove all school years where the end year has passed
        _schoolYears.RemoveAll(sy =>
        {
            if (string.IsNullOrWhiteSpace(sy))
                return false;

            var parts = sy.Split('-');
            if (parts.Length != 2)
                return false;

            if (int.TryParse(parts[1], out int endYear))
            {
                // Remove if the end year is before the current end year
                return endYear < currentEndYear;
            }

            return false;
        });
    }

    // Calculates and returns the current school year based on the system date
    public string GetCurrentSchoolYear()
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        // School year typically starts in June
        if (currentMonth >= 6)
        {
            return $"{currentYear}-{currentYear + 1}";
        }
        else
        {
            return $"{currentYear - 1}-{currentYear}";
        }
    }

    // Sets up the initial school year list with just the current school year
    private List<string> InitializeSchoolYears()
    {
        var schoolYears = new List<string>();
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        // Determine current school year
        int startYear;
        if (currentMonth >= 6)
        {
            startYear = currentYear;
        }
        else
        {
            startYear = currentYear - 1;
        }

        // Add only current school year (for testing - remove other 2 slots)
        var sy = $"{startYear}-{startYear + 1}";
        schoolYears.Add(sy);

        return schoolYears;
    }
}

