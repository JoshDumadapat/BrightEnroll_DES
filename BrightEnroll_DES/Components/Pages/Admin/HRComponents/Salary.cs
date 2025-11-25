namespace BrightEnroll_DES.Components.Pages.Admin.HRComponents;

/// <summary>
/// Handles salary calculations based on Philippine compensation rules
/// </summary>
public class Salary
{
    // Base salary ranges by role (in PHP)
    private static readonly Dictionary<string, (decimal Min, decimal Max)> BaseSalaryRanges = new()
    {
        { "Registrar", (30000m, 55000m) },
        { "Cashier", (18000m, 25000m) },
        { "Teacher", (20000m, 40000m) },
        { "HR", (35000m, 60000m) },
        { "System Admin", (40000m, 70000m) }, // Estimated range
        { "Janitor", (16000m, 20000m) },
        { "Other", (18000m, 30000m) } // Default range
    };

    // Deduction constants (Philippine rates)
    private const decimal SSS_RATE = 0.11m; // 11% of base salary (capped)
    private const decimal PHILHEALTH_RATE = 0.03m; // 3% of base salary
    private const decimal PAGIBIG_RATE = 0.02m; // 2% of base salary (capped at 200/month)
    private const decimal PAGIBIG_MAX = 200m; // Maximum Pag-IBIG contribution
    private const decimal TAX_THRESHOLD = 20833.33m; // Monthly threshold for income tax (250,000/12)
    
    // Tax brackets (TRAIN Law)
    private static readonly List<(decimal Min, decimal Max, decimal Rate)> TaxBrackets = new()
    {
        (0m, 20833.33m, 0m), // No tax
        (20833.33m, 33333.33m, 0.20m), // 20%
        (33333.33m, 66666.67m, 0.25m), // 25%
        (66666.67m, 166666.67m, 0.30m), // 30%
        (166666.67m, 666666.67m, 0.32m), // 32%
        (666666.67m, decimal.MaxValue, 0.35m) // 35%
    };

    /// <summary>
    /// Calculates the base salary based on role (uses midpoint of range)
    /// </summary>
    public static decimal CalculateBaseSalary(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return 0m;

        if (BaseSalaryRanges.TryGetValue(role, out var range))
        {
            // Return midpoint of the range
            return (range.Min + range.Max) / 2;
        }

        // Default to midpoint of "Other" range
        return (BaseSalaryRanges["Other"].Min + BaseSalaryRanges["Other"].Max) / 2;
    }

    /// <summary>
    /// Calculates 13th Month Pay (mandatory bonus)
    /// Formula: Total Basic Salary Earned within Calendar Year / 12
    /// For monthly calculation, this is Base Salary / 12
    /// </summary>
    public static decimal CalculateBonus(decimal baseSalary)
    {
        if (baseSalary <= 0)
            return 0m;

        // 13th Month Pay = Base Salary / 12
        return Math.Round(baseSalary / 12, 2);
    }

    /// <summary>
    /// Calculates SSS contribution (11% of base salary, with caps)
    /// </summary>
    public static decimal CalculateSSS(decimal baseSalary)
    {
        if (baseSalary <= 0)
            return 0m;

        // Simplified: 11% of base salary (actual calculation uses salary brackets)
        // For transparency, we'll use a simplified calculation
        decimal sssAmount = Math.Round(baseSalary * SSS_RATE, 2);
        
        // Cap at reasonable maximum (SSS has salary brackets)
        if (sssAmount > 2000m)
            sssAmount = 2000m;

        return sssAmount;
    }

    /// <summary>
    /// Calculates PhilHealth contribution (3% of base salary)
    /// </summary>
    public static decimal CalculatePhilHealth(decimal baseSalary)
    {
        if (baseSalary <= 0)
            return 0m;

        return Math.Round(baseSalary * PHILHEALTH_RATE, 2);
    }

    /// <summary>
    /// Calculates Pag-IBIG contribution (2% of base salary, capped at 200/month)
    /// </summary>
    public static decimal CalculatePagIbig(decimal baseSalary)
    {
        if (baseSalary <= 0)
            return 0m;

        decimal pagIbigAmount = Math.Round(baseSalary * PAGIBIG_RATE, 2);
        
        // Cap at 200/month
        if (pagIbigAmount > PAGIBIG_MAX)
            pagIbigAmount = PAGIBIG_MAX;

        return pagIbigAmount;
    }

    /// <summary>
    /// Calculates Withholding Tax (Income Tax) based on TRAIN Law
    /// Only applies if monthly taxable income exceeds 20,833.33
    /// </summary>
    public static decimal CalculateWithholdingTax(decimal baseSalary, decimal allowance)
    {
        // Taxable income = Base Salary + Taxable Allowance
        decimal taxableIncome = baseSalary + allowance;

        // No tax if below threshold
        if (taxableIncome <= TAX_THRESHOLD)
            return 0m;

        // Calculate tax using progressive brackets (TRAIN Law)
        decimal tax = 0m;
        
        // 20,833.33 and below: 0%
        if (taxableIncome <= 20833.33m)
            return 0m;
        
        // 20,833.34 to 33,333.33: 20% of excess over 20,833.33
        if (taxableIncome <= 33333.33m)
        {
            tax = (taxableIncome - 20833.33m) * 0.20m;
            return Math.Round(tax, 2);
        }
        
        // 33,333.34 to 66,666.67: 2,500 + 25% of excess over 33,333.33
        tax = 2500m; // Base tax for first bracket
        if (taxableIncome <= 66666.67m)
        {
            tax += (taxableIncome - 33333.33m) * 0.25m;
            return Math.Round(tax, 2);
        }
        
        // 66,666.68 to 166,666.67: 10,833.33 + 30% of excess over 66,666.67
        tax = 10833.33m; // Base tax for previous brackets
        if (taxableIncome <= 166666.67m)
        {
            tax += (taxableIncome - 66666.67m) * 0.30m;
            return Math.Round(tax, 2);
        }
        
        // 166,666.68 to 666,666.67: 40,833.33 + 32% of excess over 166,666.67
        tax = 40833.33m; // Base tax for previous brackets
        if (taxableIncome <= 666666.67m)
        {
            tax += (taxableIncome - 166666.67m) * 0.32m;
            return Math.Round(tax, 2);
        }
        
        // Above 666,666.67: 200,833.33 + 35% of excess over 666,666.67
        tax = 200833.33m; // Base tax for previous brackets
        tax += (taxableIncome - 666666.67m) * 0.35m;
        
        return Math.Round(tax, 2);
    }

    /// <summary>
    /// Calculates total deductions (SSS + PhilHealth + Pag-IBIG + Withholding Tax)
    /// </summary>
    public static decimal CalculateTotalDeductions(decimal baseSalary, decimal allowance)
    {
        decimal sss = CalculateSSS(baseSalary);
        decimal philHealth = CalculatePhilHealth(baseSalary);
        decimal pagIbig = CalculatePagIbig(baseSalary);
        decimal withholdingTax = CalculateWithholdingTax(baseSalary, allowance);

        return Math.Round(sss + philHealth + pagIbig + withholdingTax, 2);
    }

    /// <summary>
    /// Gets breakdown of deductions for transparency
    /// </summary>
    public static DeductionBreakdown GetDeductionBreakdown(decimal baseSalary, decimal allowance)
    {
        return new DeductionBreakdown
        {
            SSS = CalculateSSS(baseSalary),
            PhilHealth = CalculatePhilHealth(baseSalary),
            PagIbig = CalculatePagIbig(baseSalary),
            WithholdingTax = CalculateWithholdingTax(baseSalary, allowance),
            Total = CalculateTotalDeductions(baseSalary, allowance)
        };
    }

    /// <summary>
    /// Calculates total salary (Base + Allowance + Bonus - Deductions)
    /// </summary>
    public static decimal CalculateTotalSalary(decimal baseSalary, decimal allowance, decimal bonus, decimal deductions)
    {
        return Math.Round(baseSalary + allowance + bonus - deductions, 2);
    }

    /// <summary>
    /// Formats a decimal value as Philippine Peso with 2 decimals
    /// </summary>
    public static string FormatPeso(decimal value)
    {
        return $"₱{value:N2}";
    }

    /// <summary>
    /// Formats a decimal value with thousand separators (commas) and 2 decimals
    /// </summary>
    public static string FormatNumber(decimal value)
    {
        return value.ToString("N2");
    }

    /// <summary>
    /// Parses a formatted number string back to decimal
    /// </summary>
    public static decimal ParseNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        // Remove peso sign, commas, and spaces
        string cleaned = value.Replace("₱", "").Replace(",", "").Replace(" ", "").Trim();
        
        if (decimal.TryParse(cleaned, out decimal result))
            return result;

        return 0m;
    }
}

/// <summary>
/// Breakdown of deductions for transparency
/// </summary>
public class DeductionBreakdown
{
    public decimal SSS { get; set; }
    public decimal PhilHealth { get; set; }
    public decimal PagIbig { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal Total { get; set; }
}

