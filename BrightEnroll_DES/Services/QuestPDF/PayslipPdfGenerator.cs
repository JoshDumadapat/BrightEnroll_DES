using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace BrightEnroll_DES.Services.QuestPDF;

public class PayslipPdfGenerator
{
    private byte[]? _logoBytes;

    private byte[] GetLogoBytes()
    {
        if (_logoBytes != null) return _logoBytes;
        
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "lightlogo.png");
        if (File.Exists(logoPath))
        {
            _logoBytes = File.ReadAllBytes(logoPath);
        }
        return _logoBytes ?? Array.Empty<byte>();
    }

    public byte[] GeneratePayslip(PayslipData payslipData, DateTime payPeriodStart, DateTime payPeriodEnd)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // Receipt size: 80mm width (standard receipt width), using A5 as base
                // A5 is 148mm x 210mm, we'll use a custom width of 80mm
                page.Size(PageSizes.A5);
                page.Margin(0.5f, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Black));

                // Receipt Header
                page.Header()
                    .PaddingBottom(5)
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Row(row =>
                        {
                            var logoBytes = GetLogoBytes();
                            if (logoBytes.Length > 0)
                            {
                                row.ConstantItem(40).Height(40).Image(logoBytes).FitArea();
                            }
                            
                            row.ConstantItem(5); // Spacer
                            
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item().Text("BRIGHTENROLL").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                headerCol.Item().Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                headerCol.Item().PaddingTop(2).Text("PAYSLIP RECEIPT").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                        });
                        
                        column.Item().PaddingTop(5).BorderBottom(0.5f).BorderColor(global::QuestPDF.Helpers.Colors.Black);
                    });

                // Content - Receipt Style
                page.Content()
                    .PaddingVertical(0.3f, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Company Info Line
                        column.Item().AlignCenter().Text("--------------------------------").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                        
                        // Employee Information
                        column.Item().PaddingTop(3).Column(empCol =>
                        {
                            empCol.Item().AlignCenter().Text("EMPLOYEE INFORMATION").FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            empCol.Item().PaddingTop(2).AlignCenter().Text("--------------------------------").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            
                            empCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Name:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(payslipData.EmployeeName).FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            empCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("ID:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(payslipData.EmployeeId).FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            empCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Position:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(payslipData.Position).FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            empCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Pay Period:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"{payPeriodStart:MMMM dd} - {payPeriodEnd:MMMM dd, yyyy}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                        });

                        column.Item().PaddingTop(3).AlignCenter().Text("--------------------------------").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);

                        // Payment Details
                        column.Item().PaddingTop(2).Column(paymentCol =>
                        {
                            paymentCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Payslip #:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(payslipData.PayslipNumber).FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            paymentCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Date:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            paymentCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Time:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("hh:mm tt")).FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            paymentCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Status:").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text(payslipData.Status).FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                        });

                        column.Item().PaddingTop(3).AlignCenter().Text("--------------------------------").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);

                        // Earnings Section
                        column.Item().PaddingTop(2).Column(earningsCol =>
                        {
                            earningsCol.Item().Text("EARNINGS").FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            earningsCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Monthly Salary").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.BaseSalary:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            earningsCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Allowance").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.Allowance:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            earningsCol.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(global::QuestPDF.Helpers.Colors.Black).PaddingTop(2);
                            earningsCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("TOTAL EARNINGS").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.GrossPay:N2}").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                        });

                        column.Item().PaddingTop(3).AlignCenter().Text("--------------------------------").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);

                        // Deductions Section
                        column.Item().PaddingTop(2).Column(deductionsCol =>
                        {
                            deductionsCol.Item().Text("DEDUCTIONS").FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            deductionsCol.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("SSS").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.SSS:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            deductionsCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("PhilHealth").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.PhilHealth:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            deductionsCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Pag-IBIG").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.PagIbig:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            deductionsCol.Item().PaddingTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("Withholding Tax").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.WithholdingTax:N2}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            deductionsCol.Item().PaddingTop(2).BorderTop(0.5f).BorderColor(global::QuestPDF.Helpers.Colors.Black).PaddingTop(2);
                            deductionsCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("TOTAL DEDUCTIONS").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.TotalDeductions:N2}").FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                        });

                        column.Item().PaddingTop(3).AlignCenter().Text("=================================").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);

                        // Net Pay Section
                        column.Item().PaddingTop(2).Column(netPayCol =>
                        {
                            netPayCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("NET PAY").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                row.RelativeItem().AlignRight().Text($"₱{payslipData.NetPay:N2}").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            });
                            netPayCol.Item().PaddingTop(1).AlignCenter().Text("Amount to be paid").FontSize(6).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            netPayCol.Item().AlignCenter().Text($"PESOS {payslipData.NetPay:N2}").FontSize(6).FontColor(global::QuestPDF.Helpers.Colors.Black);
                        });

                        column.Item().PaddingTop(3).AlignCenter().Text("=================================").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);

                        // Footer
                        column.Item().PaddingTop(3).AlignCenter().Column(footerCol =>
                        {
                            footerCol.Item().Text("This is a computer-generated payslip.").FontSize(6).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            footerCol.Item().PaddingTop(1).Text("No signature required.").FontSize(6).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            footerCol.Item().PaddingTop(2).Text($"Generated: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}").FontSize(6).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            footerCol.Item().PaddingTop(3).AlignCenter().Text("--------------------------------").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Black);
                            footerCol.Item().PaddingTop(2).AlignCenter().Text("Thank you!").FontSize(7).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
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

