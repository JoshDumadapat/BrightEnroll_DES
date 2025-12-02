using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrightEnroll_DES.Services.QuestPDF;

public class PayslipPdfGenerator
{
    public byte[] GeneratePayslip(PayslipData payslipData, DateTime payPeriodStart, DateTime payPeriodEnd)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // Use a smaller page size for receipt-like format (A5 or custom small size)
                page.Size(PageSizes.A5);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header
                page.Header()
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Column(headerCol =>
                        {
                            headerCol.Item().Text("BRIGHTENROLL").FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            headerCol.Item().Text("Enrollment Management System").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            headerCol.Item().PaddingTop(3).Text("PAYSLIP").FontSize(12).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        });
                    });

                // Content
                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Employee Information
                        column.Item().Column(empCol =>
                        {
                            empCol.Item().Text("Employee Information").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                            empCol.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text("Name:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(payslipData.EmployeeName).FontSize(8).Bold();
                            });
                            empCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Employee ID:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(payslipData.EmployeeId).FontSize(8);
                            });
                            empCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Position:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(payslipData.Position).FontSize(8);
                            });
                            empCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Pay Period:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text($"{payPeriodStart:MMM dd} - {payPeriodEnd:MMM dd, yyyy}").FontSize(8);
                            });
                        });

                        column.Item().PaddingTop(8).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(5);

                        // Payment Details
                        column.Item().Column(paymentCol =>
                        {
                            paymentCol.Item().Text("Payment Details").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                            paymentCol.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text("Payslip #:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(payslipData.PayslipNumber).FontSize(8).Bold();
                            });
                            paymentCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Date Generated:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("MMM dd, yyyy")).FontSize(8);
                            });
                            paymentCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Status:").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(payslipData.Status).FontSize(8).Bold()
                                    .FontColor(global::QuestPDF.Helpers.Colors.Green.Darken2);
                            });
                        });

                        column.Item().PaddingTop(8).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(5);

                        // Earnings Section
                        column.Item().Column(earningsCol =>
                        {
                            earningsCol.Item().Text("EARNINGS").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken2);
                            earningsCol.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text("Monthly Salary:").FontSize(8);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.BaseSalary:N2}").FontSize(8).Bold();
                            });
                            earningsCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Allowance:").FontSize(8);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.Allowance:N2}").FontSize(8).Bold();
                            });
                            earningsCol.Item().PaddingTop(3).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Green.Lighten2).PaddingTop(3);
                            earningsCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Total Earnings:").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken2);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.GrossPay:N2}").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken2);
                            });
                        });

                        column.Item().PaddingTop(8).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(5);

                        // Deductions Section
                        column.Item().Column(deductionsCol =>
                        {
                            deductionsCol.Item().Text("DEDUCTIONS").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Red.Darken2);
                            deductionsCol.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text("SSS:").FontSize(8);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.SSS:N2}").FontSize(8);
                            });
                            deductionsCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("PhilHealth:").FontSize(8);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.PhilHealth:N2}").FontSize(8);
                            });
                            deductionsCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Pag-IBIG:").FontSize(8);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.PagIbig:N2}").FontSize(8);
                            });
                            deductionsCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Withholding Tax:").FontSize(8);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.WithholdingTax:N2}").FontSize(8);
                            });
                            deductionsCol.Item().PaddingTop(3).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Red.Lighten2).PaddingTop(3);
                            deductionsCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Total Deductions:").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Red.Darken2);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.TotalDeductions:N2}").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Red.Darken2);
                            });
                        });

                        column.Item().PaddingTop(8).BorderTop(2).BorderColor(global::QuestPDF.Helpers.Colors.Green.Medium).PaddingTop(5);

                        // Net Pay Section
                        column.Item().Column(netPayCol =>
                        {
                            netPayCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("NET PAY").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken2);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.NetPay:N2}").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken2);
                            });
                            netPayCol.Item().PaddingTop(2).Text("Amount to be paid").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                            netPayCol.Item().PaddingTop(1).Text($"PESOS {payslipData.NetPay:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });

                        // Footer
                        column.Item().PaddingTop(10).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Medium).PaddingTop(5)
                            .AlignCenter().Column(footerCol =>
                            {
                                footerCol.Item().Text("This is a computer-generated payslip. No signature required.").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                footerCol.Item().PaddingTop(2).Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                    });
            });
        }).GeneratePdf();
    }

    public class PayslipData
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal GrossPay { get; set; }
        public decimal SSS { get; set; }
        public decimal PhilHealth { get; set; }
        public decimal PagIbig { get; set; }
        public decimal WithholdingTax { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public string PayslipNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

