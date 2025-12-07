using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightEnroll_DES.Services.Business.Payroll
{
    public class PayrollFinancialInsightsService
    {
        public class FinancialInsights
        {
            public List<string> Insights { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();
        }

        public class PayrollMetrics
        {
            public decimal TotalGrossPay { get; set; }
            public decimal TotalNetPay { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal TotalCompanyContributions { get; set; }
            public decimal TotalCompanyExpense { get; set; }
            public int EmployeeCount { get; set; }
            public int PaidCount { get; set; }
            public int CancelledCount { get; set; }
            public decimal AverageGrossPay { get; set; }
            public decimal AverageNetPay { get; set; }
            public decimal CompanyContributionPercentage { get; set; }
        }

        public FinancialInsights GenerateInsights(
            List<PayrollDataItem> currentPeriodData,
            List<PayrollDataItem>? previousPeriodData,
            DateTime periodStart,
            DateTime periodEnd)
        {
            var insights = new FinancialInsights();
            var metrics = CalculateMetrics(currentPeriodData);

            // Generate insights
            insights.Insights.AddRange(GenerateFinancialHealthInsights(metrics));
            insights.Insights.AddRange(GenerateCostAnalysisInsights(metrics));
            
            if (previousPeriodData != null && previousPeriodData.Any())
            {
                var previousMetrics = CalculateMetrics(previousPeriodData);
                insights.Insights.AddRange(GenerateTrendInsights(metrics, previousMetrics));
            }

            // Generate recommendations
            insights.Recommendations.AddRange(GenerateFinancialRecommendations(metrics, previousPeriodData != null && previousPeriodData.Any()));

            return insights;
        }

        private PayrollMetrics CalculateMetrics(List<PayrollDataItem> data)
        {
            var paidData = data.Where(d => d.Status == "Paid").ToList();
            
            return new PayrollMetrics
            {
                TotalGrossPay = paidData.Sum(d => d.GrossSalary),
                TotalNetPay = paidData.Sum(d => d.NetSalary),
                TotalDeductions = paidData.Sum(d => d.TotalDeductions),
                TotalCompanyContributions = paidData.Sum(d => d.TotalCompanyContribution),
                TotalCompanyExpense = paidData.Sum(d => d.GrossSalary) + paidData.Sum(d => d.TotalCompanyContribution),
                EmployeeCount = paidData.Select(d => d.EmployeeName).Distinct().Count(),
                PaidCount = paidData.Count,
                CancelledCount = data.Count(d => d.Status == "Cancelled"),
                AverageGrossPay = paidData.Any() ? paidData.Average(d => d.GrossSalary) : 0,
                AverageNetPay = paidData.Any() ? paidData.Average(d => d.NetSalary) : 0,
                CompanyContributionPercentage = paidData.Sum(d => d.GrossSalary) > 0
                    ? (paidData.Sum(d => d.TotalCompanyContribution) / paidData.Sum(d => d.GrossSalary)) * 100
                    : 0
            };
        }

        private List<string> GenerateFinancialHealthInsights(PayrollMetrics metrics)
        {
            var insights = new List<string>();

            // Total Expense - Most important
            insights.Add($"Total company expense: ₱{metrics.TotalCompanyExpense:N2} (Payroll: ₱{metrics.TotalGrossPay:N2} + Contributions: ₱{metrics.TotalCompanyContributions:N2})");

            // Company Contribution percentage - only if significant
            if (metrics.CompanyContributionPercentage > 0)
            {
                insights.Add($"Company contributions: {metrics.CompanyContributionPercentage:F1}% of gross payroll (₱{metrics.TotalCompanyContributions:N2})");
            }

            return insights;
        }

        private List<string> GenerateCostAnalysisInsights(PayrollMetrics metrics)
        {
            var insights = new List<string>();

            // Only show if there's significant data
            if (metrics.TotalGrossPay > 0 && metrics.EmployeeCount > 0)
            {
                insights.Add($"Average salary: ₱{metrics.AverageGrossPay:N2} gross, ₱{metrics.AverageNetPay:N2} net per employee");
            }

            return insights;
        }

        private List<string> GenerateTrendInsights(PayrollMetrics current, PayrollMetrics previous)
        {
            var insights = new List<string>();

            // Only show most significant trend - Company Expense
            if (previous.TotalCompanyExpense > 0)
            {
                var expenseChange = ((current.TotalCompanyExpense - previous.TotalCompanyExpense) / previous.TotalCompanyExpense) * 100;
                if (Math.Abs(expenseChange) > 5m) // Only show if change is significant (>5%)
                {
                    var direction = expenseChange > 0 ? "increased" : "decreased";
                    insights.Add($"Total expense {direction} by {Math.Abs(expenseChange):F1}% vs previous period");
                }
            }

            return insights;
        }

        private List<string> GenerateFinancialRecommendations(PayrollMetrics metrics, bool hasPreviousData)
        {
            var recommendations = new List<string>();

            // High Company Contribution - only if significant
            if (metrics.CompanyContributionPercentage > 15)
            {
                recommendations.Add($"Review benefit packages - contributions are {metrics.CompanyContributionPercentage:F1}% of payroll");
            }

            // High Total Expense - only if significant
            if (metrics.TotalCompanyExpense > 1000000)
            {
                recommendations.Add($"Monitor cash flow - total expense exceeds ₱1M");
            }

            // Cancellation Impact - only if there are cancellations
            if (metrics.CancelledCount > 0)
            {
                recommendations.Add($"Review {metrics.CancelledCount} cancelled transaction(s) to prevent future issues");
            }

            // Limit to maximum 3 recommendations
            return recommendations.Take(3).ToList();
        }
    }

    // Data transfer object for insights service
    public class PayrollDataItem
    {
        public int TransactionId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string PayPeriod { get; set; } = string.Empty;
        public decimal GrossSalary { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal CompanySssContribution { get; set; }
        public decimal CompanyPhilHealthContribution { get; set; }
        public decimal CompanyPagIbigContribution { get; set; }
        public decimal TotalCompanyContribution { get; set; }
    }
}

