namespace BrightEnroll_DES.Services.Business.Academic;

public class SchoolYearService
{
    private List<string> _schoolYears;

    public SchoolYearService()
    {
        _schoolYears = InitializeSchoolYears();
    }

    public List<string> GetAvailableSchoolYears()
    {
        return _schoolYears.OrderBy(sy => sy).ToList();
    }

    public bool AddSchoolYear(string schoolYear)
    {
        if (string.IsNullOrWhiteSpace(schoolYear))
            return false;

        if (!System.Text.RegularExpressions.Regex.IsMatch(schoolYear, @"^\d{4}-\d{4}$"))
            return false;

        if (!_schoolYears.Contains(schoolYear))
        {
            _schoolYears.Add(schoolYear);
            return true;
        }

        return false;
    }

    public bool RemoveSchoolYear(string schoolYear)
    {
        return _schoolYears.Remove(schoolYear);
    }

    public void RemoveFinishedSchoolYears()
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        int currentEndYear;
        if (currentMonth >= 6)
        {
            currentEndYear = currentYear + 1;
        }
        else
        {
            currentEndYear = currentYear;
        }

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

    public string GetCurrentSchoolYear()
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        if (currentMonth >= 6)
        {
            return $"{currentYear}-{currentYear + 1}";
        }
        else
        {
            return $"{currentYear - 1}-{currentYear}";
        }
    }

    private List<string> InitializeSchoolYears()
    {
        var schoolYears = new List<string>();
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        int startYear;
        if (currentMonth >= 6)
        {
            startYear = currentYear;
        }
        else
        {
            startYear = currentYear - 1;
        }

        var sy = $"{startYear}-{startYear + 1}";
        schoolYears.Add(sy);

        return schoolYears;
    }
}

