using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Services.QuestPDF;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.QuestPDF;

public class EnrollmentPdfGenerator
{
    private readonly EnrollmentStatisticsService _statisticsService;
    private readonly AppDbContext _context;
    private byte[]? _logoBytes;

    public EnrollmentPdfGenerator(EnrollmentStatisticsService statisticsService, AppDbContext context)
    {
        _statisticsService = statisticsService;
        _context = context;
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

    public async Task<byte[]> GenerateEnrollmentReportAsync(string reportType, string? generatedBy = null)
    {
        EnrollmentStatistics stats;
        var generatedByName = generatedBy ?? "System";

        switch (reportType.ToLower())
        {
            case "newapplicants":
                stats = await _statisticsService.GetNewApplicantsStatisticsAsync();
                return GenerateNewApplicantsReport(stats, generatedByName);
            case "forenrollment":
                stats = await _statisticsService.GetForEnrollmentStatisticsAsync();
                return GenerateForEnrollmentReport(stats, generatedByName);
            case "reenrollment":
                stats = await _statisticsService.GetReEnrollmentStatisticsAsync();
                return GenerateReEnrollmentReport(stats, generatedByName);
            case "enrolled":
                stats = await _statisticsService.GetEnrolledStatisticsAsync();
                return await GenerateEnrolledReportAsync(stats, generatedByName);
            default:
                stats = await _statisticsService.GetEnrollmentStatisticsAsync();
                return GenerateOverallReport(stats, generatedByName);
        }
    }

    private byte[] GenerateNewApplicantsReport(EnrollmentStatistics stats, string generatedBy)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Enhanced Header - Two Column Layout
                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        // Top Row: Logo/Company Info on Left, Department/Date/Time on Right (matched heights)
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
                                    companyCol.Item().Text("BRIGHTENROLL").FontSize(16).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                    companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    companyCol.Item().PaddingTop(3).Text("Elementary School").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                });
                            });

                            // Right Side: Only Department, Date, and Time (matched height)
                            headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("ENROLLMENT DEPARTMENT").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Date:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.RelativeItem().Text("Time:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("hh:mm tt")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
                        
                        // Border line below header row
                        headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(global::QuestPDF.Helpers.Colors.Black);
                        
                        // Report Details Below the Line: 2 Columns - Report Type/Period on left, Generated By on right
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            // Column 1: Report Type and Period (stacked rows)
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(60).Text("Report Type:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text("New Applicants Report").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(60).Text("As of:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                            
                            // Column 2: Generated By
                            detailsRow.RelativeItem().AlignRight().Column(rightDetailsCol =>
                            {
                                rightDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(70).Text("Generated By:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    rightDetailsCol.Item().Text(generatedBy).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
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

    private byte[] GenerateForEnrollmentReport(EnrollmentStatistics stats, string generatedBy)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Enhanced Header - Two Column Layout
                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        // Top Row: Logo/Company Info on Left, Department/Date/Time on Right (matched heights)
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
                                    companyCol.Item().Text("BRIGHTENROLL").FontSize(16).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                    companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    companyCol.Item().PaddingTop(3).Text("Elementary School").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                });
                            });

                            // Right Side: Department, Date, Time, and Report Details
                            headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("ENROLLMENT DEPARTMENT").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Date:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.RelativeItem().Text("Time:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("hh:mm tt")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
                        
                        // Border line below header row
                        headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(global::QuestPDF.Helpers.Colors.Black);
                        
                        // Report Details Below the Line: 2 Columns - Report Type/Period on left, Generated By on right
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            // Column 1: Report Type and Period (stacked rows)
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(60).Text("Report Type:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text("For Enrollment Report").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(60).Text("As of:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                            
                            // Column 2: Generated By
                            detailsRow.RelativeItem().AlignRight().Column(rightDetailsCol =>
                            {
                                rightDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(70).Text("Generated By:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    rightDetailsCol.Item().Text(generatedBy).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
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

    private byte[] GenerateReEnrollmentReport(EnrollmentStatistics stats, string generatedBy)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Enhanced Header - Two Column Layout
                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        // Top Row: Logo/Company Info on Left, Department/Date/Time on Right (matched heights)
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
                                    companyCol.Item().Text("BRIGHTENROLL").FontSize(16).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                    companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    companyCol.Item().PaddingTop(3).Text("Elementary School").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                });
                            });

                            // Right Side: Department, Date, Time, and Report Details
                            headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("ENROLLMENT DEPARTMENT").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Date:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.RelativeItem().Text("Time:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("hh:mm tt")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
                        
                        // Border line below header row
                        headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(global::QuestPDF.Helpers.Colors.Black);
                        
                        // Report Details Below the Line: 2 Columns - Report Type/Period on left, Generated By on right
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            // Column 1: Report Type and Period (stacked rows)
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(60).Text("Report Type:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text("Re-Enrollment Report").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(60).Text("As of:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                            
                            // Column 2: Generated By
                            detailsRow.RelativeItem().AlignRight().Column(rightDetailsCol =>
                            {
                                rightDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(70).Text("Generated By:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    rightDetailsCol.Item().Text(generatedBy).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
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

    private async Task<byte[]> GenerateEnrolledReportAsync(EnrollmentStatistics stats, string generatedBy)
    {
        // Fetch enrolled students grouped by school year
        var enrolledStudents = await _context.Students
            .Where(s => s.Status == "Enrolled")
            .OrderBy(s => s.SchoolYr ?? "")
            .ThenBy(s => s.GradeLevel ?? "")
            .ThenBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new
            {
                s.StudentId,
                s.FirstName,
                s.MiddleName,
                s.LastName,
                s.Suffix,
                s.GradeLevel,
                s.SchoolYr,
                s.Lrn,
                s.StudentType,
                s.DateRegistered
            })
            .ToListAsync();

        var studentsByYear = enrolledStudents
            .GroupBy(s => s.SchoolYr ?? "Not Specified")
            .OrderByDescending(g => g.Key)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Enhanced Header with gradient-like effect
                page.Header()
                    .Background(global::QuestPDF.Helpers.Colors.Blue.Lighten5)
                    .Padding(15)
                    .Column(headerColumn =>
                    {
                        headerColumn.Item().Row(headerRow =>
                        {
                            var logoBytes = GetLogoBytes();
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                headerRow.ConstantItem(50).Height(50).Image(logoBytes).FitArea();
                            }
                            
                            headerRow.RelativeItem().PaddingLeft(10).Column(companyCol =>
                            {
                                companyCol.Item().Text("BRIGHTENROLL").FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken4);
                                companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(11).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                companyCol.Item().PaddingTop(2).Text("Elementary School").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });

                            headerRow.RelativeItem().AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("ENROLLMENT DEPARTMENT").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.ConstantItem(40).Text("Date:").FontSize(9);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).Bold();
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(40).Text("Time:").FontSize(9);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("hh:mm tt")).FontSize(9).Bold();
                                });
                            });
                        });
                        
                        headerColumn.Item().PaddingTop(10).BorderBottom(2).BorderColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(70).Text("Report Type:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                    row.RelativeItem().Text("Enrolled Students Report").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(70).Text("As of:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).Bold();
                                });
                            });
                            
                            detailsRow.RelativeItem().AlignRight().Column(rightDetailsCol =>
                            {
                                rightDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(80).Text("Generated By:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                    rightDetailsCol.Item().Text(generatedBy).FontSize(9).Bold();
                                });
                            });
                        });
                    });

                page.Content()
                    .PaddingVertical(0.8f, Unit.Centimetre)
                    .Column(column =>
                    {
                        // ========== SECTION 1: STATISTICS ==========
                        column.Item().PaddingBottom(0.3f, Unit.Centimetre)
                            .Background(global::QuestPDF.Helpers.Colors.Blue.Darken4)
                            .Padding(8)
                            .Text("📊 STATISTICS OVERVIEW")
                            .FontSize(15)
                            .Bold()
                            .FontColor(global::QuestPDF.Helpers.Colors.White);

                        // Summary Statistics - Enhanced Cards
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(160).Background(global::QuestPDF.Helpers.Colors.Green.Lighten3)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Green.Darken2)
                                .Padding(12).Column(col =>
                                {
                                    col.Item().Text("Total Enrolled").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken3);
                                    col.Item().PaddingTop(3).Text(stats.Enrolled.ToString("N0")).FontSize(28).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken4);
                                });
                            
                            row.ConstantItem(160).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten3)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Blue.Darken2)
                                .Padding(12).Column(col =>
                                {
                                    col.Item().Text("Total Payments").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken3);
                                    col.Item().PaddingTop(3).Text($"₱{stats.TotalPaymentsCollected:N2}").FontSize(22).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken4);
                                });
                        });

                        // Additional Statistics - Enhanced Cards
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Row(row =>
                        {
                            row.ConstantItem(110).Background(global::QuestPDF.Helpers.Colors.Teal.Lighten3)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Teal.Darken2)
                                .Padding(10).Column(col =>
                                {
                                    col.Item().Text("With LRN").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken3);
                                    col.Item().PaddingTop(2).Text(stats.WithLRN.ToString("N0")).FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Teal.Darken4);
                                });
                            
                            row.ConstantItem(110).Background(global::QuestPDF.Helpers.Colors.Orange.Lighten3)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Orange.Darken2)
                                .Padding(10).Column(col =>
                                {
                                    col.Item().Text("Without LRN").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken3);
                                    col.Item().PaddingTop(2).Text(stats.WithoutLRN.ToString("N0")).FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Orange.Darken4);
                                });
                            
                            row.ConstantItem(110).Background(global::QuestPDF.Helpers.Colors.Green.Lighten3)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Green.Darken2)
                                .Padding(10).Column(col =>
                                {
                                    col.Item().Text("Verified Docs").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken3);
                                    col.Item().PaddingTop(2).Text(stats.VerifiedDocuments.ToString("N0")).FontSize(20).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken4);
                                });
                        });

                        // Enrollment by Grade Level - Enhanced Table
                        if (stats.EnrollmentByGrade.Any())
                        {
                            column.Item().PaddingTop(0.3f, Unit.Centimetre).PaddingBottom(0.2f, Unit.Centimetre)
                                .Text("Enrollment by Grade Level").FontSize(12).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Grade Level").FontSize(10).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Count").FontSize(10).Bold();
                                });

                                foreach (var grade in stats.EnrollmentByGrade)
                                {
                                    table.Cell().Element(CellStyle).Text(grade.GradeLevel);
                                    table.Cell().Element(CellStyle).AlignRight().Text(grade.Count.ToString("N0"));
                                }
                            });
                        }

                        // Page break before student list
                        column.Item().PageBreak();

                        // ========== SECTION 2: STUDENT LIST BY SCHOOL YEAR ==========
                        column.Item().PaddingBottom(0.3f, Unit.Centimetre)
                            .Background(global::QuestPDF.Helpers.Colors.Purple.Darken4)
                            .Padding(8)
                            .Text("📋 STUDENT LIST BY SCHOOL YEAR")
                            .FontSize(15)
                            .Bold()
                            .FontColor(global::QuestPDF.Helpers.Colors.White);

                        if (studentsByYear.Any())
                        {
                            foreach (var yearGroup in studentsByYear)
                            {
                                var schoolYear = yearGroup.Key;
                                var students = yearGroup.ToList();

                                // School Year Header
                                column.Item().PaddingTop(0.4f, Unit.Centimetre).PaddingBottom(0.2f, Unit.Centimetre)
                                    .Background(global::QuestPDF.Helpers.Colors.Purple.Lighten4)
                                    .Padding(8)
                                    .Row(row =>
                                    {
                                        row.RelativeItem().Text($"School Year: {schoolYear}").FontSize(13).Bold().FontColor(global::QuestPDF.Helpers.Colors.Purple.Darken4);
                                        row.ConstantItem(80).AlignRight().Text($"({students.Count} students)").FontSize(11).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                    });

                                // Students Table
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(50);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("#").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Student Name").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Grade Level").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("LRN").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Type").FontSize(9).Bold();
                                    });

                                    int studentNumber = 1;
                                    foreach (var student in students)
                                    {
                                        var fullName = $"{student.LastName}, {student.FirstName}" +
                                            (!string.IsNullOrWhiteSpace(student.MiddleName) ? $" {student.MiddleName.Substring(0, 1)}." : "") +
                                            (!string.IsNullOrWhiteSpace(student.Suffix) ? $" {student.Suffix}" : "");

                                        table.Cell().Element(CellStyle).Text(studentNumber.ToString()).FontSize(8);
                                        table.Cell().Element(CellStyle).Text(fullName).FontSize(8);
                                        table.Cell().Element(CellStyle).Text(student.GradeLevel ?? "N/A").FontSize(8);
                                        table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(student.Lrn) || student.Lrn == "N/A" ? "N/A" : student.Lrn).FontSize(8);
                                        table.Cell().Element(CellStyle).Text(student.StudentType ?? "N/A").FontSize(8);
                                        studentNumber++;
                                    }
                                });

                                // Add spacing between school years
                                if (yearGroup != studentsByYear.Last())
                                {
                                    column.Item().PaddingTop(0.3f, Unit.Centimetre);
                                }
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(0.3f, Unit.Centimetre)
                                .Text("No enrolled students found.")
                                .FontSize(10)
                                .FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium)
                                .Italic();
                        }

                        // Page break before insights
                        column.Item().PageBreak();

                        // ========== SECTION 3: INSIGHTS & ANALYSIS ==========
                        column.Item().PaddingBottom(0.3f, Unit.Centimetre)
                            .Background(global::QuestPDF.Helpers.Colors.Green.Darken4)
                            .Padding(8)
                            .Text("💡 INSIGHTS & ANALYSIS")
                            .FontSize(15)
                            .Bold()
                            .FontColor(global::QuestPDF.Helpers.Colors.White);

                        // Key Insights
                        column.Item().PaddingTop(0.3f, Unit.Centimetre).Column(insightsCol =>
                        {
                            // LRN Coverage Insight
                            var lrnCoverage = stats.Enrolled > 0 
                                ? (stats.WithLRN * 100.0 / stats.Enrolled) 
                                : 0;
                            
                            insightsCol.Item().PaddingBottom(0.2f, Unit.Centimetre)
                                .Background(global::QuestPDF.Helpers.Colors.Teal.Lighten5)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Teal.Lighten2)
                                .Padding(10)
                                .Column(insightCol =>
                                {
                                    insightCol.Item().Text("LRN Coverage").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Teal.Darken4);
                                    insightCol.Item().PaddingTop(3).Text($"{lrnCoverage:F1}% of enrolled students have LRN assigned")
                                        .FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                });

                            // Document Verification Insight
                            var docVerification = stats.Enrolled > 0 
                                ? (stats.VerifiedDocuments * 100.0 / stats.Enrolled) 
                                : 0;
                            
                            insightsCol.Item().PaddingBottom(0.2f, Unit.Centimetre)
                                .Background(global::QuestPDF.Helpers.Colors.Green.Lighten5)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Green.Lighten2)
                                .Padding(10)
                                .Column(insightCol =>
                                {
                                    insightCol.Item().Text("Document Verification Status").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken4);
                                    insightCol.Item().PaddingTop(3).Text($"{docVerification:F1}% of students have verified documents")
                                        .FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                });

                            // Average Payment Insight
                            var avgPayment = stats.Enrolled > 0 
                                ? (stats.TotalPaymentsCollected / stats.Enrolled) 
                                : 0;
                            
                            insightsCol.Item().PaddingBottom(0.2f, Unit.Centimetre)
                                .Background(global::QuestPDF.Helpers.Colors.Blue.Lighten5)
                                .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Blue.Lighten2)
                                .Padding(10)
                                .Column(insightCol =>
                                {
                                    insightCol.Item().Text("Average Payment per Student").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken4);
                                    insightCol.Item().PaddingTop(3).Text($"₱{avgPayment:N2} average payment collected per enrolled student")
                                        .FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                });

                            // Grade Distribution Insight
                            if (stats.EnrollmentByGrade.Any())
                            {
                                var topGrade = stats.EnrollmentByGrade.OrderByDescending(g => g.Count).First();
                                var topGradePercentage = stats.Enrolled > 0 
                                    ? (topGrade.Count * 100.0 / stats.Enrolled) 
                                    : 0;
                                
                                insightsCol.Item().PaddingBottom(0.2f, Unit.Centimetre)
                                    .Background(global::QuestPDF.Helpers.Colors.Orange.Lighten5)
                                    .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Orange.Lighten2)
                                    .Padding(10)
                                    .Column(insightCol =>
                                    {
                                        insightCol.Item().Text("Grade Level Distribution").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Orange.Darken4);
                                        insightCol.Item().PaddingTop(3).Text($"{topGrade.GradeLevel} has the highest enrollment with {topGrade.Count} students ({topGradePercentage:F1}%)")
                                            .FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                    });
                            }

                            // School Year Distribution
                            if (studentsByYear.Any())
                            {
                                var yearCounts = studentsByYear.Select(y => new { Year = y.Key, Count = y.Count() }).ToList();
                                var topYear = yearCounts.OrderByDescending(y => y.Count).First();
                                
                                insightsCol.Item().PaddingBottom(0.2f, Unit.Centimetre)
                                    .Background(global::QuestPDF.Helpers.Colors.Purple.Lighten5)
                                    .Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Purple.Lighten2)
                                    .Padding(10)
                                    .Column(insightCol =>
                                    {
                                        insightCol.Item().Text("School Year Distribution").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Purple.Darken4);
                                        insightCol.Item().PaddingTop(3).Text($"Students are distributed across {yearCounts.Count} school year(s). {topYear.Year} has {topYear.Count} enrolled students.")
                                            .FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken2);
                                    });
                            }
                        });
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

    private byte[] GenerateOverallReport(EnrollmentStatistics stats, string generatedBy)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Enhanced Header - Two Column Layout
                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        // Top Row: Logo/Company Info on Left, Department/Date/Time on Right (matched heights)
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
                                    companyCol.Item().Text("BRIGHTENROLL").FontSize(16).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                    companyCol.Item().PaddingTop(2).Text("ENROLLMENT MANAGEMENT SYSTEM").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    companyCol.Item().PaddingTop(3).Text("Elementary School").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                });
                            });

                            // Right Side: Department, Date, Time, and Report Details
                            headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("ENROLLMENT DEPARTMENT").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Date:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.RelativeItem().Text("Time:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("hh:mm tt")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
                        
                        // Border line below header row
                        headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(global::QuestPDF.Helpers.Colors.Black);
                        
                        // Report Details Below the Line: 2 Columns - Report Type/Period on left, Generated By on right
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            // Column 1: Report Type and Period (stacked rows)
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(60).Text("Report Type:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text("Enrollment Overall Report").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(60).Text("As of:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(stats.GeneratedDate.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                            
                            // Column 2: Generated By
                            detailsRow.RelativeItem().AlignRight().Column(rightDetailsCol =>
                            {
                                rightDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(70).Text("Generated By:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    rightDetailsCol.Item().Text(generatedBy).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
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

    private static global::QuestPDF.Infrastructure.IContainer HeaderCellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4)
            .BorderBottom(2)
            .BorderColor(global::QuestPDF.Helpers.Colors.Blue.Darken2)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }
}

