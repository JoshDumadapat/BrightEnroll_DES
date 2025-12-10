using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Services.Business.Reports;
using BrightEnroll_DES.Services.Business.Academic;
using Microsoft.Extensions.Configuration;
using QuestPdfColors = QuestPDF.Helpers.Colors;
using QuestPdfContainer = QuestPDF.Infrastructure.IContainer;

namespace BrightEnroll_DES.Services.QuestPDF;

public class FinanceReportsPdfGenerator
{
    private readonly IConfiguration _configuration;
    private readonly SchoolYearService _schoolYearService;
    private byte[]? _logoBytes;

    public FinanceReportsPdfGenerator(IConfiguration configuration, SchoolYearService schoolYearService)
    {
        _configuration = configuration;
        _schoolYearService = schoolYearService;
    }

    private byte[] GetLogoBytes()
    {
        if (_logoBytes != null) return _logoBytes;
        
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "reportPDF.png");
        if (File.Exists(logoPath))
        {
            _logoBytes = File.ReadAllBytes(logoPath);
        }
        return _logoBytes ?? Array.Empty<byte>();
    }

    private void BuildHeader(ColumnDescriptor headerColumn, string reportType, DateTime fromDate, DateTime toDate, string? schoolYear = null)
    {
        // Top Row: Logo/Company Info on Left, Department/Date/Time on Right
        headerColumn.Item().Height(50).Row(headerRow =>
            {
                // Left Side: Logo and Company Information
                headerRow.RelativeItem(2).Row(leftRow =>
                {
                    var logoBytes = GetLogoBytes();
                    if (logoBytes != null && logoBytes.Length > 0)
                    {
                        leftRow.ConstantItem(50).Height(50).Image(logoBytes).FitArea();
                    }
                    
                    leftRow.RelativeItem().PaddingLeft(10).Column(companyCol =>
                    {
                        companyCol.Item().Text("BRIGHTENROLL").FontSize(16).Bold().FontColor(QuestPdfColors.Blue.Darken3);
                        companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(10).FontColor(QuestPdfColors.Black);
                        companyCol.Item().PaddingTop(3).Text("Elementary School").FontSize(9).FontColor(QuestPdfColors.Grey.Darken1);
                    });
                });

                // Right Side: Department, Date, Time
                headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                {
                    detailsCol.Item().Text("FINANCE DEPARTMENT").FontSize(10).Bold().FontColor(QuestPdfColors.Black);
                    detailsCol.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("Date:").FontSize(9).FontColor(QuestPdfColors.Black);
                        row.RelativeItem().Text(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(QuestPdfColors.Black);
                    });
                    detailsCol.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text("Time:").FontSize(9).FontColor(QuestPdfColors.Black);
                        row.RelativeItem().Text(DateTime.Now.ToString("hh:mm tt")).FontSize(9).FontColor(QuestPdfColors.Black);
                    });
                });
            });
            
            // Border line below header row
            headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(QuestPdfColors.Black);
            
            // Report Details Below the Line
            headerColumn.Item().PaddingTop(8).Row(detailsRow =>
            {
                // Column 1: Report Type and Period
                detailsRow.RelativeItem().Column(leftDetailsCol =>
                {
                    leftDetailsCol.Item().Row(row =>
                    {
                        row.ConstantItem(80).Text("Report Type:").FontSize(9).FontColor(QuestPdfColors.Black);
                        row.RelativeItem().Text(reportType).FontSize(9).Bold().FontColor(QuestPdfColors.Black);
                    });
                    leftDetailsCol.Item().PaddingTop(2).Row(row =>
                    {
                        row.ConstantItem(80).Text("Period:").FontSize(9).FontColor(QuestPdfColors.Black);
                        row.RelativeItem().Text($"{fromDate:MMMM dd, yyyy} - {toDate:MMMM dd, yyyy}").FontSize(9).FontColor(QuestPdfColors.Black);
                    });
                    if (!string.IsNullOrEmpty(schoolYear))
                    {
                        leftDetailsCol.Item().PaddingTop(2).Row(row =>
                        {
                            row.ConstantItem(80).Text("School Year:").FontSize(9).FontColor(QuestPdfColors.Black);
                            row.RelativeItem().Text(schoolYear).FontSize(9).Bold().FontColor(QuestPdfColors.Black);
                        });
                    }
                });
            });
    }

    public async Task<byte[]> GenerateIncomeStatementPdfAsync(IncomeStatement incomeStatement, DateTime fromDate, DateTime toDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "Income Statement (Profit & Loss)";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        BuildHeader(headerColumn, reportType, fromDate, toDate, schoolYear);
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("INCOME STATEMENT").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"For the period: {fromDate:MMMM dd, yyyy} to {toDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        column.Item().PaddingTop(15);

                        // Revenue Section
                        column.Item().Text("REVENUE").FontSize(12).Bold().FontColor(QuestPdfColors.Green.Darken2);
                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem(3).Text("Total Revenue").FontSize(10);
                            row.ConstantItem(150).Text(FormatCurrency(incomeStatement.Revenue)).FontSize(10).Bold().AlignRight();
                        });
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);

                        // Expenses Section
                        column.Item().PaddingTop(10).Text("EXPENSES").FontSize(12).Bold().FontColor(QuestPdfColors.Red.Darken2);
                        
                        if (incomeStatement.ExpensesByCategory != null && incomeStatement.ExpensesByCategory.Any())
                        {
                            foreach (var expense in incomeStatement.ExpensesByCategory.OrderByDescending(e => e.Amount))
                            {
                                column.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem(3).PaddingLeft(10).Text(expense.Category).FontSize(10);
                                    row.ConstantItem(150).Text(FormatCurrency(expense.Amount)).FontSize(10).AlignRight();
                                });
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(3).PaddingLeft(10).Text("No expenses recorded").FontSize(10).FontColor(QuestPdfColors.Grey.Medium);
                        }

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem(3).Text("Total Expenses").FontSize(10).Bold();
                            row.ConstantItem(150).Text(FormatCurrency(incomeStatement.TotalExpenses)).FontSize(10).Bold().AlignRight();
                        });
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);

                        // Net Income Section
                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem(3).Text("NET INCOME").FontSize(12).Bold().FontColor(incomeStatement.NetIncome >= 0 ? QuestPdfColors.Blue.Darken2 : QuestPdfColors.Red.Darken2);
                            row.ConstantItem(150).Text(FormatCurrency(incomeStatement.NetIncome)).FontSize(12).Bold().FontColor(incomeStatement.NetIncome >= 0 ? QuestPdfColors.Blue.Darken2 : QuestPdfColors.Red.Darken2).AlignRight();
                        });
                        column.Item().PaddingTop(5).LineHorizontal(2).LineColor(QuestPdfColors.Black);

                        // Profit Margin
                        if (incomeStatement.Revenue > 0)
                        {
                            var profitMargin = (incomeStatement.NetIncome / incomeStatement.Revenue) * 100;
                            column.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem(3).Text("Profit Margin").FontSize(10);
                                row.ConstantItem(150).Text($"{profitMargin:N2}%").FontSize(10).AlignRight();
                            });
                        }

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateCashFlowPdfAsync(CashFlowReport cashFlowReport, DateTime fromDate, DateTime toDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "Cash Flow Statement";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        BuildHeader(headerColumn, reportType, fromDate, toDate, schoolYear);
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("CASH FLOW STATEMENT").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"For the period: {fromDate:MMMM dd, yyyy} to {toDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        column.Item().PaddingTop(15);

                        // Cash Inflows
                        column.Item().Text("CASH INFLOWS").FontSize(12).Bold().FontColor(QuestPdfColors.Green.Darken2);
                        
                        if (cashFlowReport.CashInflows != null && cashFlowReport.CashInflows.Any())
                        {
                            foreach (var inflow in cashFlowReport.CashInflows.OrderByDescending(i => i.Amount))
                            {
                                column.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem(3).PaddingLeft(10).Text(inflow.Category).FontSize(10);
                                    row.ConstantItem(150).Text(FormatCurrency(inflow.Amount)).FontSize(10).AlignRight();
                                });
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(3).PaddingLeft(10).Text("No cash inflows recorded").FontSize(10).FontColor(QuestPdfColors.Grey.Medium);
                        }

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem(3).Text("Total Cash Inflow").FontSize(10).Bold();
                            row.ConstantItem(150).Text(FormatCurrency(cashFlowReport.TotalInflow)).FontSize(10).Bold().AlignRight();
                        });
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);

                        // Cash Outflows
                        column.Item().PaddingTop(10).Text("CASH OUTFLOWS").FontSize(12).Bold().FontColor(QuestPdfColors.Red.Darken2);
                        
                        if (cashFlowReport.CashOutflows != null && cashFlowReport.CashOutflows.Any())
                        {
                            foreach (var outflow in cashFlowReport.CashOutflows.OrderByDescending(o => o.Amount))
                            {
                                column.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem(3).PaddingLeft(10).Text(outflow.Category).FontSize(10);
                                    row.ConstantItem(150).Text(FormatCurrency(outflow.Amount)).FontSize(10).AlignRight();
                                });
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(3).PaddingLeft(10).Text("No cash outflows recorded").FontSize(10).FontColor(QuestPdfColors.Grey.Medium);
                        }

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem(3).Text("Total Cash Outflow").FontSize(10).Bold();
                            row.ConstantItem(150).Text(FormatCurrency(cashFlowReport.TotalOutflow)).FontSize(10).Bold().AlignRight();
                        });
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);

                        // Net Cash Flow
                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem(3).Text("NET CASH FLOW").FontSize(12).Bold().FontColor(cashFlowReport.NetCashFlow >= 0 ? QuestPdfColors.Blue.Darken2 : QuestPdfColors.Red.Darken2);
                            row.ConstantItem(150).Text(FormatCurrency(cashFlowReport.NetCashFlow)).FontSize(12).Bold().FontColor(cashFlowReport.NetCashFlow >= 0 ? QuestPdfColors.Blue.Darken2 : QuestPdfColors.Red.Darken2).AlignRight();
                        });
                        column.Item().PaddingTop(5).LineHorizontal(2).LineColor(QuestPdfColors.Black);

                        // Monthly Breakdown if available
                        if (cashFlowReport.MonthlyBreakdown != null && cashFlowReport.MonthlyBreakdown.Any())
                        {
                            column.Item().PaddingTop(15).Text("MONTHLY BREAKDOWN").FontSize(11).Bold();
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Month").FontSize(9).Bold();
                                    header.Cell().Element(CellStyle).Text("Cash In").FontSize(9).Bold().AlignRight();
                                    header.Cell().Element(CellStyle).Text("Cash Out").FontSize(9).Bold().AlignRight();
                                    header.Cell().Element(CellStyle).Text("Net Flow").FontSize(9).Bold().AlignRight();
                                });

                                foreach (var month in cashFlowReport.MonthlyBreakdown.OrderBy(m => m.Month))
                                {
                                    table.Cell().Element(CellStyle).Text(month.MonthName).FontSize(9);
                                    table.Cell().Element(CellStyle).Text(FormatCurrency(month.CashIn)).FontSize(9).AlignRight();
                                    table.Cell().Element(CellStyle).Text(FormatCurrency(month.CashOut)).FontSize(9).AlignRight();
                                    table.Cell().Element(CellStyle).Text(FormatCurrency(month.NetCashFlow)).FontSize(9).AlignRight();
                                }
                            });
                        }

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateAccountsReceivablePdfAsync(List<AccountsReceivableRecord> arRecords, DateTime asOfDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "Accounts Receivable Aging Report";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        BuildHeader(headerColumn, reportType, asOfDate, asOfDate, schoolYear);
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("ACCOUNTS RECEIVABLE AGING REPORT").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"As of: {asOfDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        // Summary
                        var totalAR = arRecords.Sum(r => r.OutstandingBalance);
                        var current = arRecords.Where(r => r.AgingBucket == "Current").Sum(r => r.OutstandingBalance);
                        var overdue = arRecords.Where(r => r.AgingBucket != "Current").Sum(r => r.OutstandingBalance);

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Background(QuestPdfColors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total A/R").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(totalAR)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Current").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(current)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Overdue").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(overdue)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Grey.Lighten3).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Accounts").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text($"{arRecords.Count}").FontSize(12).Bold();
                            });
                        });

                        column.Item().PaddingTop(15).Text("AGING DETAILS").FontSize(12).Bold();

                        // Table
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Student Name").FontSize(8).Bold();
                                header.Cell().Element(CellStyle).Text("Grade Level").FontSize(8).Bold();
                                header.Cell().Element(CellStyle).Text("Total Fee").FontSize(8).Bold().AlignRight();
                                header.Cell().Element(CellStyle).Text("Amount Paid").FontSize(8).Bold().AlignRight();
                                header.Cell().Element(CellStyle).Text("Outstanding").FontSize(8).Bold().AlignRight();
                                header.Cell().Element(CellStyle).Text("Aging").FontSize(8).Bold();
                            });

                            foreach (var record in arRecords.OrderByDescending(r => r.OutstandingBalance))
                            {
                                table.Cell().Element(CellStyle).Text(record.StudentName).FontSize(8);
                                table.Cell().Element(CellStyle).Text(record.GradeLevel).FontSize(8);
                                table.Cell().Element(CellStyle).Text(FormatCurrency(record.TotalFee)).FontSize(8).AlignRight();
                                table.Cell().Element(CellStyle).Text(FormatCurrency(record.AmountPaid)).FontSize(8).AlignRight();
                                table.Cell().Element(CellStyle).Text(FormatCurrency(record.OutstandingBalance)).FontSize(8).Bold().AlignRight();
                                table.Cell().Element(CellStyle).Text(record.AgingBucket).FontSize(8);
                            }

                            // Total Row
                            table.Cell().Element(CellStyle).Text("TOTAL").FontSize(9).Bold();
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle).Text(FormatCurrency(arRecords.Sum(r => r.TotalFee))).FontSize(9).Bold().AlignRight();
                            table.Cell().Element(CellStyle).Text(FormatCurrency(arRecords.Sum(r => r.AmountPaid))).FontSize(9).Bold().AlignRight();
                            table.Cell().Element(CellStyle).Text(FormatCurrency(totalAR)).FontSize(9).Bold().AlignRight();
                            table.Cell().Element(CellStyle);
                        });

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GeneratePaymentHistoryPdfAsync(List<PaymentHistoryRecord> paymentHistory, DateTime fromDate, DateTime toDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "Payment History Report";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        BuildHeader(headerColumn, reportType, fromDate, toDate, schoolYear);
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("PAYMENT HISTORY REPORT").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"For the period: {fromDate:MMMM dd, yyyy} to {toDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        // Summary
                        var totalPayments = paymentHistory.Sum(p => p.Amount);
                        var paymentCount = paymentHistory.Count;
                        var avgPayment = paymentCount > 0 ? totalPayments / paymentCount : 0;
                        var cashPayments = paymentHistory.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Background(QuestPdfColors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Payments").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(totalPayments)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Payment Count").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text($"{paymentCount}").FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Purple.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Average Payment").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(avgPayment)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Indigo.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Cash Payments").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(cashPayments)).FontSize(12).Bold();
                            });
                        });

                        column.Item().PaddingTop(15).Text("PAYMENT DETAILS").FontSize(12).Bold();

                        // Table
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date").FontSize(8).Bold();
                                header.Cell().Element(CellStyle).Text("Student Name").FontSize(8).Bold();
                                header.Cell().Element(CellStyle).Text("Grade Level").FontSize(8).Bold();
                                header.Cell().Element(CellStyle).Text("Amount").FontSize(8).Bold().AlignRight();
                                header.Cell().Element(CellStyle).Text("Method").FontSize(8).Bold();
                                header.Cell().Element(CellStyle).Text("OR Number").FontSize(8).Bold();
                            });

                            foreach (var payment in paymentHistory.OrderByDescending(p => p.PaymentDate))
                            {
                                table.Cell().Element(CellStyle).Text(payment.PaymentDate.ToString("MMM dd, yyyy")).FontSize(8);
                                table.Cell().Element(CellStyle).Text(payment.StudentName).FontSize(8);
                                table.Cell().Element(CellStyle).Text(payment.GradeLevel).FontSize(8);
                                table.Cell().Element(CellStyle).Text(FormatCurrency(payment.Amount)).FontSize(8).AlignRight();
                                table.Cell().Element(CellStyle).Text(payment.PaymentMethod).FontSize(8);
                                table.Cell().Element(CellStyle).Text(payment.OrNumber).FontSize(8);
                            }

                            // Total Row
                            table.Cell().Element(CellStyle).Text("TOTAL").FontSize(9).Bold();
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle).Text(FormatCurrency(totalPayments)).FontSize(9).Bold().AlignRight();
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle);
                        });

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateExpenseAnalysisPdfAsync(ExpenseAnalysisReport expenseAnalysis, DateTime fromDate, DateTime toDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "Expense Analysis Report";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        BuildHeader(headerColumn, reportType, fromDate, toDate, schoolYear);
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("EXPENSE ANALYSIS REPORT").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"For the period: {fromDate:MMMM dd, yyyy} to {toDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        // Summary
                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Background(QuestPdfColors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Expenses").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(expenseAnalysis.TotalExpenses)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Grey.Lighten3).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Count").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text($"{expenseAnalysis.TotalCount} items").FontSize(12).Bold();
                            });
                            var approved = expenseAnalysis.ByStatus?.FirstOrDefault(s => s.Status == "Approved")?.Amount ?? 0;
                            var pending = expenseAnalysis.ByStatus?.FirstOrDefault(s => s.Status == "Pending")?.Amount ?? 0;
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Approved").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(approved)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Yellow.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Pending").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(pending)).FontSize(12).Bold();
                            });
                        });

                        // By Category
                        if (expenseAnalysis.ByCategory != null && expenseAnalysis.ByCategory.Any())
                        {
                            column.Item().PaddingTop(15).Text("EXPENSES BY CATEGORY").FontSize(12).Bold();
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Category").FontSize(9).Bold();
                                    header.Cell().Element(CellStyle).Text("Amount").FontSize(9).Bold().AlignRight();
                                    header.Cell().Element(CellStyle).Text("Count").FontSize(9).Bold().AlignRight();
                                });

                                foreach (var category in expenseAnalysis.ByCategory.OrderByDescending(c => c.Amount))
                                {
                                    table.Cell().Element(CellStyle).Text(category.Category).FontSize(9);
                                    table.Cell().Element(CellStyle).Text(FormatCurrency(category.Amount)).FontSize(9).AlignRight();
                                    table.Cell().Element(CellStyle).Text($"{category.Count}").FontSize(9).AlignRight();
                                }

                                // Total
                                table.Cell().Element(CellStyle).Text("TOTAL").FontSize(9).Bold();
                                table.Cell().Element(CellStyle).Text(FormatCurrency(expenseAnalysis.TotalExpenses)).FontSize(9).Bold().AlignRight();
                                table.Cell().Element(CellStyle).Text($"{expenseAnalysis.TotalCount}").FontSize(9).Bold().AlignRight();
                            });
                        }

                        // By Status
                        if (expenseAnalysis.ByStatus != null && expenseAnalysis.ByStatus.Any())
                        {
                            column.Item().PaddingTop(15).Text("EXPENSES BY STATUS").FontSize(12).Bold();
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Status").FontSize(9).Bold();
                                    header.Cell().Element(CellStyle).Text("Amount").FontSize(9).Bold().AlignRight();
                                    header.Cell().Element(CellStyle).Text("Count").FontSize(9).Bold().AlignRight();
                                });

                                foreach (var status in expenseAnalysis.ByStatus.OrderByDescending(s => s.Amount))
                                {
                                    table.Cell().Element(CellStyle).Text(status.Status).FontSize(9);
                                    table.Cell().Element(CellStyle).Text(FormatCurrency(status.Amount)).FontSize(9).AlignRight();
                                    table.Cell().Element(CellStyle).Text($"{status.Count}").FontSize(9).AlignRight();
                                }
                            });
                        }

                        // Monthly Trends
                        if (expenseAnalysis.MonthlyTrends != null && expenseAnalysis.MonthlyTrends.Any())
                        {
                            column.Item().PaddingTop(15).Text("MONTHLY TRENDS").FontSize(12).Bold();
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Month").FontSize(9).Bold();
                                    header.Cell().Element(CellStyle).Text("Amount").FontSize(9).Bold().AlignRight();
                                    header.Cell().Element(CellStyle).Text("Count").FontSize(9).Bold().AlignRight();
                                });

                                foreach (var trend in expenseAnalysis.MonthlyTrends.OrderBy(t => t.Year).ThenBy(t => t.Month))
                                {
                                    table.Cell().Element(CellStyle).Text(trend.MonthName).FontSize(9);
                                    table.Cell().Element(CellStyle).Text(FormatCurrency(trend.Amount)).FontSize(9).AlignRight();
                                    table.Cell().Element(CellStyle).Text($"{trend.Count}").FontSize(9).AlignRight();
                                }
                            });
                        }

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateGeneralLedgerPdfAsync(GeneralLedgerDataDto generalLedger, DateTime fromDate, DateTime toDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "General Ledger";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        BuildHeader(headerColumn, reportType, fromDate, toDate, schoolYear);
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("GENERAL LEDGER").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"For the period: {fromDate:MMMM dd, yyyy} to {toDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        // Summary
                        var totalCredits = generalLedger.Entries.Sum(e => e.Credit);
                        var totalDebits = generalLedger.Entries.Sum(e => e.Debit);
                        var finalBalance = generalLedger.Entries.LastOrDefault()?.Balance ?? 0;

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Background(QuestPdfColors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Credits").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(totalCredits)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Debits").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(totalDebits)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Net Balance").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text(FormatCurrency(finalBalance)).FontSize(12).Bold();
                            });
                            row.RelativeItem().PaddingLeft(10).Background(QuestPdfColors.Grey.Lighten3).Padding(10).Column(col =>
                            {
                                col.Item().Text("Transactions").FontSize(9).FontColor(QuestPdfColors.Grey.Darken2);
                                col.Item().PaddingTop(2).Text($"{generalLedger.Entries.Count}").FontSize(12).Bold();
                            });
                        });

                        column.Item().PaddingTop(15).Text("LEDGER ENTRIES").FontSize(12).Bold();

                        // Table
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date").FontSize(7).Bold();
                                header.Cell().Element(CellStyle).Text("Entry #").FontSize(7).Bold();
                                header.Cell().Element(CellStyle).Text("Account Code").FontSize(7).Bold();
                                header.Cell().Element(CellStyle).Text("Account Name").FontSize(7).Bold();
                                header.Cell().Element(CellStyle).Text("Description").FontSize(7).Bold();
                                header.Cell().Element(CellStyle).Text("Debit").FontSize(7).Bold().AlignRight();
                                header.Cell().Element(CellStyle).Text("Credit").FontSize(7).Bold().AlignRight();
                                header.Cell().Element(CellStyle).Text("Balance").FontSize(7).Bold().AlignRight();
                            });

                            foreach (var entry in generalLedger.Entries)
                            {
                                table.Cell().Element(CellStyle).Text(entry.Date.ToString("MMM dd, yyyy")).FontSize(7);
                                table.Cell().Element(CellStyle).Text(entry.EntryNumber).FontSize(7);
                                table.Cell().Element(CellStyle).Text(entry.AccountCode).FontSize(7);
                                table.Cell().Element(CellStyle).Text(entry.AccountName).FontSize(7);
                                table.Cell().Element(CellStyle).Text(entry.Description).FontSize(7);
                                table.Cell().Element(CellStyle).Text(entry.Debit > 0 ? FormatCurrency(entry.Debit) : "").FontSize(7).AlignRight();
                                table.Cell().Element(CellStyle).Text(entry.Credit > 0 ? FormatCurrency(entry.Credit) : "").FontSize(7).AlignRight();
                                table.Cell().Element(CellStyle).Text(FormatCurrency(entry.Balance)).FontSize(7).AlignRight();
                            }

                            // Total Row
                            table.Cell().Element(CellStyle).Text("TOTAL").FontSize(8).Bold();
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle);
                            table.Cell().Element(CellStyle).Text(FormatCurrency(totalDebits)).FontSize(8).Bold().AlignRight();
                            table.Cell().Element(CellStyle).Text(FormatCurrency(totalCredits)).FontSize(8).Bold().AlignRight();
                            table.Cell().Element(CellStyle).Text(FormatCurrency(finalBalance)).FontSize(8).Bold().AlignRight();
                        });

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    private QuestPdfContainer CellStyle(QuestPdfContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(QuestPdfColors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5);
    }

    public async Task<byte[]> GenerateBalanceSheetPdfAsync(BalanceSheetReport balanceSheet, DateTime asOfDate, string? generatedBy = null)
    {
        var schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
        var reportType = "Balance Sheet";
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        // Custom header for Balance Sheet (uses "As of" date instead of date range)
                        var logoBytes = GetLogoBytes();
                        headerColumn.Item().Height(50).Row(headerRow =>
                        {
                            // Left Side: Logo and Company Information
                            headerRow.RelativeItem(2).Row(leftRow =>
                            {
                                if (logoBytes != null && logoBytes.Length > 0)
                                {
                                    leftRow.ConstantItem(50).Height(50).Image(logoBytes).FitArea();
                                }
                                
                                leftRow.RelativeItem().PaddingLeft(10).Column(companyCol =>
                                {
                                    companyCol.Item().Text("BRIGHTENROL").FontSize(16).Bold().FontColor(QuestPdfColors.Blue.Darken3);
                                    companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(10).FontColor(QuestPdfColors.Black);
                                    companyCol.Item().PaddingTop(3).Text("Elementary School").FontSize(9).FontColor(QuestPdfColors.Grey.Darken1);
                                });
                            });

                            // Right Side: Department, Date, Time
                            headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("FINANCE DEPARTMENT").FontSize(10).Bold().FontColor(QuestPdfColors.Black);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Date:").FontSize(9).FontColor(QuestPdfColors.Black);
                                    row.RelativeItem().Text(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(QuestPdfColors.Black);
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.RelativeItem().Text("Time:").FontSize(9).FontColor(QuestPdfColors.Black);
                                    row.RelativeItem().Text(DateTime.Now.ToString("hh:mm tt")).FontSize(9).FontColor(QuestPdfColors.Black);
                                });
                            });
                        });
                        
                        headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(QuestPdfColors.Black);
                        
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(80).Text("Report Type:").FontSize(9).FontColor(QuestPdfColors.Black);
                                    row.RelativeItem().Text(reportType).FontSize(9).Bold().FontColor(QuestPdfColors.Black);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(80).Text("As of:").FontSize(9).FontColor(QuestPdfColors.Black);
                                    row.RelativeItem().Text($"{asOfDate:MMMM dd, yyyy}").FontSize(9).FontColor(QuestPdfColors.Black);
                                });
                                if (!string.IsNullOrEmpty(schoolYear))
                                {
                                    leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                    {
                                        row.ConstantItem(80).Text("School Year:").FontSize(9).FontColor(QuestPdfColors.Black);
                                        row.RelativeItem().Text(schoolYear).FontSize(9).Bold().FontColor(QuestPdfColors.Black);
                                    });
                                }
                            });
                        });
                    });

                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" | Generated: ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Title
                        column.Item().Text("BALANCE SHEET").FontSize(16).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"As of {asOfDate:MMMM dd, yyyy}").FontSize(11).AlignCenter();
                        if (!string.IsNullOrEmpty(schoolYear))
                        {
                            column.Item().PaddingTop(2).Text($"School Year: {schoolYear}").FontSize(11).Bold().AlignCenter();
                        }

                        column.Item().PaddingTop(15);

                        // Three Column Layout: Assets | Liabilities | Equity
                        column.Item().Row(mainRow =>
                        {
                            // ASSETS Column
                            mainRow.RelativeItem(1).Column(assetsCol =>
                            {
                                assetsCol.Item().Text("ASSETS").FontSize(12).Bold().FontColor(QuestPdfColors.Blue.Darken2);
                                
                                if (balanceSheet.Assets != null && balanceSheet.Assets.Any(a => a.Balance != 0))
                                {
                                    foreach (var asset in balanceSheet.Assets.Where(a => a.Balance != 0))
                                    {
                                        assetsCol.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem().Column(accountCol =>
                                            {
                                                accountCol.Item().Text($"{asset.AccountCode} - {asset.AccountName}").FontSize(9).FontColor(QuestPdfColors.Grey.Darken1);
                                            });
                                            row.ConstantItem(120).Text(FormatCurrency(asset.Balance)).FontSize(10).AlignRight();
                                        });
                                    }
                                }
                                else
                                {
                                    assetsCol.Item().PaddingTop(3).PaddingLeft(10).Text("No assets recorded").FontSize(9).FontColor(QuestPdfColors.Grey.Medium);
                                }

                                assetsCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                assetsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Total Assets").FontSize(10).Bold();
                                    row.ConstantItem(120).Text(FormatCurrency(balanceSheet.TotalAssets)).FontSize(10).Bold().AlignRight();
                                });
                            });

                            // LIABILITIES Column
                            mainRow.RelativeItem(1).PaddingLeft(20).Column(liabilitiesCol =>
                            {
                                liabilitiesCol.Item().Text("LIABILITIES").FontSize(12).Bold().FontColor(QuestPdfColors.Red.Darken2);
                                
                                if (balanceSheet.Liabilities != null && balanceSheet.Liabilities.Any(l => l.Balance != 0))
                                {
                                    foreach (var liability in balanceSheet.Liabilities.Where(l => l.Balance != 0))
                                    {
                                        liabilitiesCol.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem().Column(accountCol =>
                                            {
                                                accountCol.Item().Text($"{liability.AccountCode} - {liability.AccountName}").FontSize(9).FontColor(QuestPdfColors.Grey.Darken1);
                                            });
                                            row.ConstantItem(120).Text(FormatCurrency(liability.Balance)).FontSize(10).AlignRight();
                                        });
                                    }
                                }
                                else
                                {
                                    liabilitiesCol.Item().PaddingTop(3).PaddingLeft(10).Text("No liabilities recorded").FontSize(9).FontColor(QuestPdfColors.Grey.Medium);
                                }

                                liabilitiesCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                liabilitiesCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Total Liabilities").FontSize(10).Bold();
                                    row.ConstantItem(120).Text(FormatCurrency(balanceSheet.TotalLiabilities)).FontSize(10).Bold().AlignRight();
                                });
                            });

                            // EQUITY Column
                            mainRow.RelativeItem(1).PaddingLeft(20).Column(equityCol =>
                            {
                                equityCol.Item().Text("EQUITY").FontSize(12).Bold().FontColor(QuestPdfColors.Green.Darken2);
                                
                                if (balanceSheet.Equity != null && balanceSheet.Equity.Any(e => e.Balance != 0))
                                {
                                    foreach (var equity in balanceSheet.Equity.Where(e => e.Balance != 0))
                                    {
                                        equityCol.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem().Column(accountCol =>
                                            {
                                                accountCol.Item().Text($"{equity.AccountCode} - {equity.AccountName}").FontSize(9).FontColor(QuestPdfColors.Grey.Darken1);
                                            });
                                            row.ConstantItem(120).Text(FormatCurrency(equity.Balance)).FontSize(10).AlignRight();
                                        });
                                    }
                                }
                                else
                                {
                                    equityCol.Item().PaddingTop(3).PaddingLeft(10).Text("No equity recorded").FontSize(9).FontColor(QuestPdfColors.Grey.Medium);
                                }

                                equityCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                equityCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Total Equity").FontSize(10).Bold();
                                    row.ConstantItem(120).Text(FormatCurrency(balanceSheet.TotalEquity)).FontSize(10).Bold().AlignRight();
                                });
                            });
                        });

                        // Balance Check
                        column.Item().PaddingTop(20).Row(balanceRow =>
                        {
                            balanceRow.RelativeItem().Column(balanceCol =>
                            {
                                balanceCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Total Liabilities + Equity:").FontSize(10).Bold();
                                    row.ConstantItem(150).Text(FormatCurrency(balanceSheet.TotalLiabilities + balanceSheet.TotalEquity)).FontSize(10).Bold().AlignRight();
                                });
                                
                                if (balanceSheet.IsBalanced)
                                {
                                    balanceCol.Item().PaddingTop(5).Text("✓ Balance Sheet is Balanced").FontSize(10).Bold().FontColor(QuestPdfColors.Green.Darken2);
                                }
                                else
                                {
                                    balanceCol.Item().PaddingTop(5).Text($"✗ Balance Sheet is Unbalanced (Difference: {FormatCurrency(Math.Abs(balanceSheet.Difference))})").FontSize(10).Bold().FontColor(QuestPdfColors.Red.Darken2);
                                }
                            });
                        });

                        // Generated By
                        if (!string.IsNullOrEmpty(generatedBy))
                        {
                            column.Item().PaddingTop(20).AlignRight().Text($"Generated by: {generatedBy}").FontSize(8).FontColor(QuestPdfColors.Grey.Medium);
                        }
                    });
            });
        }).GeneratePdf();
    }

    private string FormatCurrency(decimal amount)
    {
        return $"₱{amount:N2}";
    }
}

// DTO classes for General Ledger
public class GeneralLedgerDataDto
{
    public List<LedgerEntryDto> Entries { get; set; } = new();
}

public class LedgerEntryDto
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

