# Payroll Calculation System - Legal Basis & Documentation

This document provides the legal basis, formulas, and authoritative sources for all payroll calculations implemented in the BrightEnroll Payroll Management System.

---

## 📋 Table of Contents

1. [Base Salary & Gross Pay](#base-salary--gross-pay)
2. [13th Month Pay](#13th-month-pay)
3. [SSS Contribution](#sss-contribution)
4. [PhilHealth Contribution](#philhealth-contribution)
5. [Pag-IBIG Contribution](#pag-ibig-contribution)
6. [Withholding Tax (Income Tax)](#withholding-tax-income-tax)
7. [Net Pay Calculation](#net-pay-calculation)

---

## 💰 Base Salary & Gross Pay

**Legal Basis:** Republic Act No. 6727 (Wage Rationalization Act)

**Formula:**
```
Gross Pay = Base Salary + Allowance
```

**Source:**
- [RA 6727 - LawPhil](https://lawphil.net/statutes/repacts/ra1989/ra_6727_1989.html)
- Regional Wage Orders (varies by region)

**Notes:**
- Base salary must comply with regional minimum wage requirements
- Allowances may be taxable or non-taxable based on BIR classification

---

## 🎁 13th Month Pay

**Legal Basis:** Presidential Decree No. 851 (PD 851)

**Formula:**
```
13th Month Pay = Base Salary ÷ 12
```

**Source:**
- [PD 851 - LawPhil](https://lawphil.net/statutes/presdecs/pd1975/pd_851_1975.html)

**Requirements:**
- Mandatory for all rank-and-file employees
- Employee must have rendered at least one (1) month of service during the calendar year
- Tax-exempt up to ₱90,000 annually (combined with other bonuses)

**Calculation:**
- Based on total basic salary earned within the calendar year
- Divided by 12 to get monthly equivalent

---

## 🏛️ SSS Contribution

**Legal Basis:** Republic Act No. 11199 (Social Security Act of 2018)

**Formula:**
```
SSS Contribution = Base Salary × 11% (capped at ₱2,000/month)
```

**Source:**
- [RA 11199 - Official Gazette](https://www.officialgazette.gov.ph/2018/02/07/republic-act-no-11199/)
- [SSS Official Website](https://www.sss.gov.ph/)

**Notes:**
- Employee share: 11% of monthly salary credit
- Employer share: 8.5% (not included in employee deductions)
- Uses salary brackets (simplified calculation in this system)
- Maximum monthly contribution: ₱2,000

**Salary Brackets (Simplified):**
- Actual SSS uses specific salary brackets with fixed contribution amounts
- This system uses a simplified 11% calculation for transparency

---

## 🏥 PhilHealth Contribution

**Legal Basis:** Republic Act No. 11223 (Universal Health Care Act)

**Formula:**
```
PhilHealth Contribution = Base Salary × 3%
```

**Source:**
- [RA 11223 - Official Gazette](https://www.officialgazette.gov.ph/2019/02/20/republic-act-no-11223/)
- [PhilHealth Official Website](https://www.philhealth.gov.ph/)

**Notes:**
- Premium rate: 3% of monthly salary
- Employee share: 1.5% (employer matches 1.5%)
- Minimum premium: ₱300/month
- Maximum premium: ₱1,800/month (for salaries ≥ ₱60,000)

---

## 🏠 Pag-IBIG Contribution

**Legal Basis:** Republic Act No. 9679 (Pag-IBIG Fund Law of 2009)

**Formula:**
```
Pag-IBIG Contribution = Base Salary × 2% (capped at ₱200/month)
```

**Source:**
- [RA 9679 - Official Gazette](https://www.officialgazette.gov.ph/2009/07/21/republic-act-no-9679/)
- [Pag-IBIG Official Website](https://www.pagibigfund.gov.ph/)

**Notes:**
- Employee contribution: 2% of monthly salary
- Maximum employee contribution: ₱200/month
- Employer matches employee contribution (up to ₱200)

---

## 📊 Withholding Tax (Income Tax)

**Legal Basis:** Republic Act No. 10963 (TRAIN Law - Tax Reform for Acceleration and Inclusion)

**Formula:**
```
Taxable Income = Gross Pay - (SSS + PhilHealth + Pag-IBIG)
Withholding Tax = Calculated based on progressive tax brackets
```

**Source:**
- [RA 10963 - Official Gazette](https://www.officialgazette.gov.ph/downloads/2017/12dec/20171219-RA-10963-RRD.pdf)
- [BIR Revenue Regulations](https://www.bir.gov.ph/)

### Tax Brackets (Monthly - TRAIN Law)

| Taxable Income Range | Tax Rate | Base Tax + Rate on Excess |
|---------------------|----------|---------------------------|
| ₱0 - ₱20,833.33 | **0%** | No tax |
| ₱20,833.34 - ₱33,333.33 | **20%** | 20% of excess over ₱20,833.33 |
| ₱33,333.34 - ₱66,666.67 | **25%** | ₱2,500 + 25% of excess over ₱33,333.33 |
| ₱66,666.68 - ₱166,666.67 | **30%** | ₱10,833.33 + 30% of excess over ₱66,666.67 |
| ₱166,666.68 - ₱666,666.67 | **32%** | ₱40,833.33 + 32% of excess over ₱166,666.67 |
| Above ₱666,666.67 | **35%** | ₱200,833.33 + 35% of excess over ₱666,666.67 |

**Annual Threshold:** ₱250,000/year (₱20,833.33/month)

**Calculation Method:**
```
If Taxable Income ≤ ₱20,833.33:
    Withholding Tax = ₱0

If ₱20,833.34 ≤ Taxable Income ≤ ₱33,333.33:
    Withholding Tax = (Taxable Income - ₱20,833.33) × 0.20

If ₱33,333.34 ≤ Taxable Income ≤ ₱66,666.67:
    Withholding Tax = ₱2,500 + (Taxable Income - ₱33,333.33) × 0.25

If ₱66,666.68 ≤ Taxable Income ≤ ₱166,666.67:
    Withholding Tax = ₱10,833.33 + (Taxable Income - ₱66,666.67) × 0.30

If ₱166,666.68 ≤ Taxable Income ≤ ₱666,666.67:
    Withholding Tax = ₱40,833.33 + (Taxable Income - ₱166,666.67) × 0.32

If Taxable Income > ₱666,666.67:
    Withholding Tax = ₱200,833.33 + (Taxable Income - ₱666,666.67) × 0.35
```

**Notes:**
- Tax exemption threshold: ₱250,000 annual income (₱20,833.33/month)
- Uses progressive tax system (higher income = higher rate)
- Based on BIR Monthly Tax Table under TRAIN Law

---

## 💵 Net Pay Calculation

**Formula:**
```
Total Deductions = SSS + PhilHealth + Pag-IBIG + Withholding Tax
Net Pay = Gross Pay - Total Deductions
```

**Breakdown:**
1. Calculate Gross Pay (Base Salary + Allowance)
2. Calculate Mandatory Deductions (SSS + PhilHealth + Pag-IBIG)
3. Calculate Taxable Income (Gross Pay - Mandatory Deductions)
4. Calculate Withholding Tax (based on Taxable Income and tax brackets)
5. Calculate Total Deductions (SSS + PhilHealth + Pag-IBIG + Withholding Tax)
6. Calculate Net Pay (Gross Pay - Total Deductions)

---

## 📚 Additional Resources

### Official Government Websites
- **BIR (Bureau of Internal Revenue):** https://www.bir.gov.ph/
- **SSS (Social Security System):** https://www.sss.gov.ph/
- **PhilHealth (Philippine Health Insurance Corporation):** https://www.philhealth.gov.ph/
- **Pag-IBIG Fund:** https://www.pagibigfund.gov.ph/
- **DOLE (Department of Labor and Employment):** https://www.dole.gov.ph/

### Legal References
- **Official Gazette:** https://www.officialgazette.gov.ph/
- **LawPhil (Philippine Laws and Jurisprudence):** https://lawphil.net/

---

## ⚠️ Important Notes

1. **This system uses simplified calculations** for transparency and educational purposes. Actual payroll processing may use more detailed salary brackets (especially for SSS).

2. **Tax calculations are based on monthly income.** Annual tax returns may have different calculations and exemptions.

3. **Allowances may be taxable or non-taxable** based on BIR classification. This system treats all allowances as taxable unless specified otherwise.

4. **Regional variations** may apply for minimum wage and some contribution rates. Always verify with local regulations.

5. **This system does not connect to a database** - all calculations are performed client-side for demonstration purposes.

6. **For production use**, consult with:
   - Certified Public Accountant (CPA)
   - BIR-accredited tax consultant
   - Legal counsel specializing in labor law

---

## 📝 Version Information

**Last Updated:** 2025  
**Calculation Basis:** Philippine Labor Laws and Tax Regulations (2025)  
**System Version:** 1.0

---

*This documentation is provided for informational purposes only and should not be considered as legal or tax advice. Always consult with qualified professionals for actual payroll processing.*

