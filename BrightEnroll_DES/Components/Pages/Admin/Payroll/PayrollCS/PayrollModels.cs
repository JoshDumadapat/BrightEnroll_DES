namespace BrightEnroll_DES.Components.Pages.Admin.Payroll.PayrollCS;

using BrightEnroll_DES.Components.Pages.Admin.HumanResource.HRCS;

public class PayrollRoleData
{
    public string Role { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal ThresholdPercentage { get; set; } = 0.00m;
    
    // Calculated values
    public decimal GrossPay => BaseSalary + Allowance;
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
        // Calculate 13th Month Pay (Base Salary / 12)
        ThirteenthMonthPay = Salary.CalculateBonus(BaseSalary);
        
        // Calculate mandatory deductions
        SSS = Salary.CalculateSSS(BaseSalary);
        PhilHealth = Salary.CalculatePhilHealth(BaseSalary);
        PagIbig = Salary.CalculatePagIbig(BaseSalary);
        
        // Calculate taxable income (Gross Pay - Mandatory Deductions)
        TaxableIncome = GrossPay - (SSS + PhilHealth + PagIbig);
        
        // Calculate withholding tax
        WithholdingTax = CalculateWithholdingTaxDetailed(TaxableIncome);
        
        // Total deductions
        TotalDeductions = SSS + PhilHealth + PagIbig + WithholdingTax;
        
        // Net Pay = Gross Pay - Total Deductions
        NetPay = GrossPay - TotalDeductions;
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

