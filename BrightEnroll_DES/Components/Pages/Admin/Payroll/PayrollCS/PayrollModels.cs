namespace BrightEnroll_DES.Components.Pages.Admin.Payroll.PayrollCS;

using BrightEnroll_DES.Components.Pages.Admin.HumanResource.HRCS;

public class PayrollRoleData
{
    public string Role { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal ThresholdPercentage { get; set; } = 0.00m;
    
    // Attendance data (MANDATORY - required for payroll calculation)
    public decimal? RegularHours { get; set; } // Actual regular hours worked
    public decimal? OvertimeHours { get; set; } // Overtime hours worked
    public decimal? LeaveDays { get; set; } // Leave days (unpaid leave)
    public int? LateMinutes { get; set; } // Late minutes
    public int? MonthlyWorkingDays { get; set; } // Total working days in the month (typically 22-30, default 28 for Philippine standard)
    public int? PayPeriodWorkingDays { get; set; } // Total working days in the pay period (e.g., 15 days for bi-monthly)
    
    // Calculated values
    public decimal AdjustedBaseSalary { get; private set; }
    public decimal OvertimePay { get; private set; }
    public decimal LateDeduction { get; private set; }
    public decimal GrossPay { get; private set; }
    public decimal ThirteenthMonthPay { get; private set; }
    public decimal SSS { get; private set; }
    public decimal PhilHealth { get; private set; }
    public decimal PagIbig { get; private set; }
    public decimal WithholdingTax { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal NetPay { get; private set; }
    public decimal TaxableIncome { get; private set; }
    
    public bool IsActive { get; set; } = true;
    
    // Tax bracket information
    public string TaxBracket { get; private set; } = string.Empty;
    public decimal TaxRate { get; private set; }
    
    public void Recalculate()
    {
        // Calculate monthly working days (default to 22 if not provided, but typically 28 for Philippine standard)
        int monthlyDays = MonthlyWorkingDays ?? 28;
        
        // Calculate pay period working days
        int payPeriodDays = PayPeriodWorkingDays ?? MonthlyWorkingDays ?? monthlyDays;
        
        // Calculate pay period salary (prorate monthly salary to pay period if needed)
        decimal payPeriodSalary = BaseSalary;
        if (payPeriodDays > 0 && monthlyDays > 0 && payPeriodDays < monthlyDays)
        {
            // Prorate monthly salary to pay period
            // Formula: (Pay Period Working Days / Monthly Working Days) * Monthly Salary
            payPeriodSalary = Math.Round((payPeriodDays / (decimal)monthlyDays) * BaseSalary, 2);
        }
        
        // PHILIPPINE PAYROLL STANDARDS (DOLE) CALCULATION
        // Step 1: Calculate rates
        // Hourly Rate = Monthly Salary / (Working Days × 8 hours)
        decimal hourlyRate = monthlyDays > 0 ? Math.Round(BaseSalary / (monthlyDays * 8m), 2) : 0m;
        
        // Daily Rate = Monthly Salary / Working Days
        decimal dailyRate = monthlyDays > 0 ? Math.Round(BaseSalary / monthlyDays, 2) : 0m;
        
        // Step 2: Start with FULL monthly salary (or prorated pay period salary)
        AdjustedBaseSalary = payPeriodSalary;
        
        // Step 3: Calculate Leave Deduction (if leave days > 0)
        // Leave Deduction = Daily Rate × Leave Days
        decimal leaveDeduction = 0;
        if (LeaveDays.HasValue && LeaveDays.Value > 0 && dailyRate > 0)
        {
            leaveDeduction = Math.Round(dailyRate * LeaveDays.Value, 2);
        }
        
        // Step 4: Calculate Late Deduction
        // Late Deduction = Hourly Rate × (Late Minutes / 60)
        LateDeduction = 0;
        if (LateMinutes.HasValue && LateMinutes.Value > 0 && hourlyRate > 0)
        {
            decimal lateHours = LateMinutes.Value / 60m; // Convert minutes to hours
            LateDeduction = Math.Round(hourlyRate * lateHours, 2);
        }
        
        // Step 5: Calculate Overtime Pay
        // OT Pay = Hourly Rate × 1.25 × Overtime Hours (125% per DOLE standard)
        OvertimePay = 0;
        if (OvertimeHours.HasValue && OvertimeHours.Value > 0 && hourlyRate > 0)
        {
            decimal otRate = hourlyRate * 1.25m; // 125% of hourly rate
            OvertimePay = Math.Round(otRate * OvertimeHours.Value, 2);
        }
        
        // Step 6: Calculate Gross Pay
        // Gross Pay = Monthly Salary - Leave Deduction - Late Deduction + Overtime Pay + Allowance
        // Prorate allowance to pay period if needed
        decimal payPeriodAllowance = Allowance;
        if (payPeriodDays > 0 && monthlyDays > 0 && payPeriodDays < monthlyDays)
        {
            payPeriodAllowance = Math.Round((payPeriodDays / (decimal)monthlyDays) * Allowance, 2);
        }
        
        // Gross Pay Formula (Philippine Standard)
        // Gross Pay = Monthly Salary - Leave Deduction - Late Deduction + Overtime Pay + Allowance
        GrossPay = AdjustedBaseSalary - leaveDeduction - LateDeduction + OvertimePay + payPeriodAllowance;
        if (GrossPay < 0) GrossPay = 0;
        
        // Step 7: Calculate 13th Month Pay (based on gross pay before deductions)
        ThirteenthMonthPay = Salary.CalculateBonus(GrossPay);
        
        // Step 8: Calculate mandatory deductions (based on Gross Pay)
        // Note: In Philippine payroll, deductions are typically based on Gross Pay
        SSS = Salary.CalculateSSS(GrossPay);
        PhilHealth = Salary.CalculatePhilHealth(GrossPay);
        PagIbig = Salary.CalculatePagIbig(GrossPay);
        
        // Step 9: Calculate taxable income (Gross Pay - Mandatory Deductions)
        TaxableIncome = GrossPay - (SSS + PhilHealth + PagIbig);
        if (TaxableIncome < 0) TaxableIncome = 0;
        
        // Step 10: Calculate withholding tax
        WithholdingTax = CalculateWithholdingTaxDetailed(TaxableIncome);
        
        // Step 11: Total deductions (mandatory + tax)
        TotalDeductions = SSS + PhilHealth + PagIbig + WithholdingTax;
        
        // Step 12: Net Pay = Gross Pay - Total Deductions
        NetPay = GrossPay - TotalDeductions;
        if (NetPay < 0) NetPay = 0;
    }
    
    private decimal CalculateWithholdingTaxDetailed(decimal taxableIncome)
    {
        // TRAIN Law Tax Brackets (Monthly)
        if (taxableIncome <= 20833.33m)
        {
            TaxBracket = "0% (Below Threshold)";
            TaxRate = 0m;
            return 0m;
        }
        
        if (taxableIncome <= 33333.33m)
        {
            TaxBracket = "20%";
            TaxRate = 0.20m;
            return Math.Round((taxableIncome - 20833.33m) * 0.20m, 2);
        }
        
        if (taxableIncome <= 66666.67m)
        {
            TaxBracket = "25%";
            TaxRate = 0.25m;
            decimal baseTax = 2500m; // Tax on first bracket
            return Math.Round(baseTax + (taxableIncome - 33333.33m) * 0.25m, 2);
        }
        
        if (taxableIncome <= 166666.67m)
        {
            TaxBracket = "30%";
            TaxRate = 0.30m;
            decimal baseTax = 10833.33m; // Tax on previous brackets
            return Math.Round(baseTax + (taxableIncome - 66666.67m) * 0.30m, 2);
        }
        
        if (taxableIncome <= 666666.67m)
        {
            TaxBracket = "32%";
            TaxRate = 0.32m;
            decimal baseTax = 40833.33m; // Tax on previous brackets
            return Math.Round(baseTax + (taxableIncome - 166666.67m) * 0.32m, 2);
        }
        
        TaxBracket = "35%";
        TaxRate = 0.35m;
        decimal maxBaseTax = 200833.33m; // Tax on previous brackets
        return Math.Round(maxBaseTax + (taxableIncome - 666666.67m) * 0.35m, 2);
    }
}

public class PayrollCalculationDetails
{
    public string Role { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string CalculationFormula { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

