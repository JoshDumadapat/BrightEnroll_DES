using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Services.QuestPDF;

namespace BrightEnroll_DES.Services.QuestPDF;

public class EnrollmentPdfGenerator
{
    private readonly EnrollmentStatisticsService _statisticsService;

    public EnrollmentPdfGenerator(EnrollmentStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task<byte[]> GenerateEnrollmentReportAsync(string reportType)
    {
        EnrollmentStatistics stats;

        switch (reportType.ToLower())
        {
            case "newapplicants":
                stats = await _statisticsService.GetNewApplicantsStatisticsAsync();
                return GenerateNewApplicantsReport(stats);
            case "forenrollment":
                stats = await _statisticsService.GetForEnrollmentStatisticsAsync();
                return GenerateForEnrollmentReport(stats);
            case "reenrollment":
                stats = await _statisticsService.GetReEnrollmentStatisticsAsync();
                return GenerateReEnrollmentReport(stats);
            case "enrolled":
                stats = await _statisticsService.GetEnrolledStatisticsAsync();
                return GenerateEnrolledReport(stats);
            default:
                stats = await _statisticsService.GetEnrollmentStatisticsAsync();
                return GenerateOverallReport(stats);
        }
    }

    private byte[] GenerateNewApplicantsReport(EnrollmentStatistics stats)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("NEW APPLICANTS REPORT").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            column.Item().Text("BrightEnroll Enrollment Management System").FontSize(12).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                        row.ConstantItem(50).AlignRight().Text($"Generated: {stats.GeneratedDate:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Summary Statistics
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Text("SUMMARY STATISTICS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total New Applicants").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.NewApplicants.ToString("N0")).FontSize(24).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Payments Collected").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text($"₱{stats.TotalPaymentsCollected:N2}").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                        });

                        // Enrollment by Grade
                        if (stats.EnrollmentByGrade.Any())
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ENROLLMENT BY GRADE LEVEL").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Grade Level").FontSize(10).Bold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Count").FontSize(10).Bold();
                                });

                                foreach (var grade in stats.EnrollmentByGrade)
                                {
                                    table.Cell().Element(CellStyle).Text(grade.GradeLevel);
                                    table.Cell().Element(CellStyle).AlignRight().Text(grade.Count.ToString("N0"));
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateForEnrollmentReport(EnrollmentStatistics stats)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("FOR ENROLLMENT REPORT").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            column.Item().Text("BrightEnroll Enrollment Management System").FontSize(12).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                        row.ConstantItem(50).AlignRight().Text($"Generated: {stats.GeneratedDate:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Summary Statistics
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Text("SUMMARY STATISTICS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Students For Enrollment").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.ForEnrollment.ToString("N0")).FontSize(24).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Payments Collected").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text($"₱{stats.TotalPaymentsCollected:N2}").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                        });

                        // Payment Status Breakdown
                        column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("PAYMENT STATUS BREAKDOWN").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Fully Paid").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.FullyPaidCount.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Orange.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Partially Paid").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.PartiallyPaidCount.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Orange.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Unpaid").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.UnpaidCount.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Red.Darken3);
                            });
                        });

                        // Enrollment by Grade
                        if (stats.EnrollmentByGrade.Any())
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ENROLLMENT BY GRADE LEVEL").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Grade Level").FontSize(10).Bold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Count").FontSize(10).Bold();
                                });

                                foreach (var grade in stats.EnrollmentByGrade)
                                {
                                    table.Cell().Element(CellStyle).Text(grade.GradeLevel);
                                    table.Cell().Element(CellStyle).AlignRight().Text(grade.Count.ToString("N0"));
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateReEnrollmentReport(EnrollmentStatistics stats)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("RE-ENROLLMENT REPORT").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            column.Item().Text("BrightEnroll Enrollment Management System").FontSize(12).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                        row.ConstantItem(50).AlignRight().Text($"Generated: {stats.GeneratedDate:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Summary Statistics
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Text("SUMMARY STATISTICS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Purple.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Re-Enrollment Students").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.ReEnrollment.ToString("N0")).FontSize(24).Bold().FontColor(global::QuestPDF.Helpers.Colors.Purple.Darken3);
                            });
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Payments Collected").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text($"₱{stats.TotalPaymentsCollected:N2}").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                        });

                        // Enrollment by Grade
                        if (stats.EnrollmentByGrade.Any())
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ENROLLMENT BY GRADE LEVEL").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Grade Level").FontSize(10).Bold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Count").FontSize(10).Bold();
                                });

                                foreach (var grade in stats.EnrollmentByGrade)
                                {
                                    table.Cell().Element(CellStyle).Text(grade.GradeLevel);
                                    table.Cell().Element(CellStyle).AlignRight().Text(grade.Count.ToString("N0"));
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateEnrolledReport(EnrollmentStatistics stats)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("ENROLLED STUDENTS REPORT").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            column.Item().Text("BrightEnroll Enrollment Management System").FontSize(12).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                        row.ConstantItem(50).AlignRight().Text($"Generated: {stats.GeneratedDate:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Summary Statistics
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Text("SUMMARY STATISTICS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Enrolled").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.Enrolled.ToString("N0")).FontSize(24).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                            row.ConstantItem(150).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Payments Collected").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text($"₱{stats.TotalPaymentsCollected:N2}").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                        });

                        // Additional Statistics
                        column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ADDITIONAL STATISTICS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Teal.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("With LRN").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.WithLRN.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Teal.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Orange.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Without LRN").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.WithoutLRN.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Orange.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Verified Documents").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.VerifiedDocuments.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                        });

                        // Enrollment by Grade
                        if (stats.EnrollmentByGrade.Any())
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ENROLLMENT BY GRADE LEVEL").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Grade Level").FontSize(10).Bold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Count").FontSize(10).Bold();
                                });

                                foreach (var grade in stats.EnrollmentByGrade)
                                {
                                    table.Cell().Element(CellStyle).Text(grade.GradeLevel);
                                    table.Cell().Element(CellStyle).AlignRight().Text(grade.Count.ToString("N0"));
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateOverallReport(EnrollmentStatistics stats)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("ENROLLMENT OVERALL REPORT").FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            column.Item().Text("BrightEnroll Enrollment Management System").FontSize(12).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                        row.ConstantItem(50).AlignRight().Text($"Generated: {stats.GeneratedDate:MMM dd, yyyy HH:mm}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Overall Statistics
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Text("OVERALL STATISTICS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(120).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Students").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.TotalStudents.ToString("N0")).FontSize(22).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                            row.ConstantItem(120).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Enrolled").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.Enrolled.ToString("N0")).FontSize(22).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                            row.ConstantItem(120).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Payments Collected").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text($"₱{stats.TotalPaymentsCollected:N2}").FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                        });

                        // Enrollment Status Breakdown
                        column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ENROLLMENT STATUS BREAKDOWN").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Yellow.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("New Applicants").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.NewApplicants.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Yellow.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("For Enrollment").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.ForEnrollment.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Purple.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Re-Enrollment").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.ReEnrollment.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Purple.Darken3);
                            });
                        });

                        // Payment Status
                        column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("PAYMENT STATUS").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        column.Item().PaddingBottom(1, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Fully Paid").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.FullyPaidCount.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Orange.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Partially Paid").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.PartiallyPaidCount.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Orange.Darken3);
                            });
                            row.ConstantItem(100).Background(global::QuestPDF.Helpers.Colors.Red.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("Unpaid").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                col.Item().Text(stats.UnpaidCount.ToString("N0")).FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Red.Darken3);
                            });
                        });

                        // Enrollment by Grade
                        if (stats.EnrollmentByGrade.Any())
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("ENROLLMENT BY GRADE LEVEL").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Grade Level").FontSize(10).Bold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Count").FontSize(10).Bold();
                                });

                                foreach (var grade in stats.EnrollmentByGrade)
                                {
                                    table.Cell().Element(CellStyle).Text(grade.GradeLevel);
                                    table.Cell().Element(CellStyle).AlignRight().Text(grade.Count.ToString("N0"));
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    private static global::QuestPDF.Infrastructure.IContainer CellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(10);
    }
}

