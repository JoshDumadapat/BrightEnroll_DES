using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using BrightEnroll_DES.Services.Business.HR;

namespace BrightEnroll_DES.Services.QuestPDF;

public class AttendanceReportPdfGenerator
{
    private byte[]? _logoBytes;

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

    public byte[] GenerateAttendanceReport(
        AttendanceReportDto report,
        string generatedBy,
        string userRole,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Enhanced Header - Two Column Layout (same as other reports)
                page.Header()
                    .PaddingBottom(10)
                    .Column(headerColumn =>
                    {
                        // Top Row: Logo/Company Info on Left, Department/Date/Time on Right
                        headerColumn.Item().Height(50).Row(headerRow =>
                        {
                            // Left Side: Logo and Company Information
                            headerRow.RelativeItem(2).Row(leftRow =>
                            {
                                var logoBytes = GetLogoBytes();
                                if (logoBytes.Length > 0)
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

                            // Right Side: Department, Date, and Time
                            headerRow.RelativeItem(1).AlignRight().Column(detailsCol =>
                            {
                                detailsCol.Item().Text("PAYROLL DEPARTMENT").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                detailsCol.Item().PaddingTop(3).Row(row =>
                                {
                                    row.RelativeItem().Text("Date:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                detailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.RelativeItem().Text("Time:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(DateTime.Now.ToString("hh:mm tt")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
                        
                        // Border line below header row
                        headerColumn.Item().PaddingTop(8).BorderBottom(1).BorderColor(global::QuestPDF.Helpers.Colors.Black);
                        
                        // Report Details Below the Line
                        headerColumn.Item().PaddingTop(8).Row(detailsRow =>
                        {
                            // Column 1: Document Type and Period
                            detailsRow.RelativeItem().Column(leftDetailsCol =>
                            {
                                leftDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(75).Text("Document Type:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text("Attendance Report").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                                leftDetailsCol.Item().PaddingTop(2).Row(row =>
                                {
                                    row.ConstantItem(75).Text("Period:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text(report.Period).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                            
                            // Column 2: Generated By
                            detailsRow.RelativeItem().AlignRight().Column(rightDetailsCol =>
                            {
                                rightDetailsCol.Item().Row(row =>
                                {
                                    row.ConstantItem(75).Text("Generated By:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    row.RelativeItem().Text($"{generatedBy} ({userRole})").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            });
                        });
                    });

                // Content
                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        // Report Title
                        column.Item().Text("ATTENDANCE REPORT").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignCenter();
                        column.Item().PaddingTop(5).Text($"Period: {report.Period}").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Black).AlignCenter();
                        if (dateFrom.HasValue && dateTo.HasValue)
                        {
                            column.Item().PaddingTop(2).Text($"Date Range: {dateFrom.Value:MMMM dd, yyyy} - {dateTo.Value:MMMM dd, yyyy}").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1).AlignCenter();
                        }

                        column.Item().PaddingTop(15);

                        // Summary Statistics
                        column.Item().Text("SUMMARY STATISTICS").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                        column.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Padding(8).Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4).Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Blue.Lighten2).Column(col =>
                            {
                                col.Item().Text("Total Employees").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                col.Item().PaddingTop(2).Text(report.TotalEmployees.ToString()).FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                col.Item().PaddingTop(1).Text($"{report.EmployeesWithRecords} with records").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                            row.RelativeItem().Padding(8).Background(global::QuestPDF.Helpers.Colors.Green.Lighten4).Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Green.Lighten2).Column(col =>
                            {
                                col.Item().Text("Total Regular Hours").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                col.Item().PaddingTop(2).Text($"{report.TotalRegularHours:N2}").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Green.Darken3);
                                col.Item().PaddingTop(1).Text($"Avg: {report.AverageRegularHours:N2} hrs").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                            row.RelativeItem().Padding(8).Background(global::QuestPDF.Helpers.Colors.Orange.Lighten4).Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Orange.Lighten2).Column(col =>
                            {
                                col.Item().Text("Total Overtime Hours").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                col.Item().PaddingTop(2).Text($"{report.TotalOvertimeHours:N2}").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Orange.Darken3);
                                col.Item().PaddingTop(1).Text($"Avg: {report.AverageOvertimeHours:N2} hrs").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                            row.RelativeItem().Padding(8).Background(global::QuestPDF.Helpers.Colors.Red.Lighten4).Border(1).BorderColor(global::QuestPDF.Helpers.Colors.Red.Lighten2).Column(col =>
                            {
                                col.Item().Text("Total Late Minutes").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                col.Item().PaddingTop(2).Text(report.TotalLateMinutes.ToString()).FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Red.Darken3);
                                col.Item().PaddingTop(1).Text($"Avg: {report.AverageLateMinutes} mins").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                        });

                        column.Item().PaddingTop(10);

                        // Recommendations Section
                        if (report.Recommendations != null && report.Recommendations.Any())
                        {
                            column.Item().Text("RECOMMENDATIONS").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            column.Item().PaddingTop(5);
                            
                            foreach (var rec in report.Recommendations)
                            {
                                var bgColor = rec.Severity == "High" 
                                    ? global::QuestPDF.Helpers.Colors.Red.Lighten4 
                                    : rec.Severity == "Medium" 
                                        ? global::QuestPDF.Helpers.Colors.Orange.Lighten4 
                                        : global::QuestPDF.Helpers.Colors.Blue.Lighten4;
                                
                                var borderColor = rec.Severity == "High" 
                                    ? global::QuestPDF.Helpers.Colors.Red.Lighten2 
                                    : rec.Severity == "Medium" 
                                        ? global::QuestPDF.Helpers.Colors.Orange.Lighten2 
                                        : global::QuestPDF.Helpers.Colors.Blue.Lighten2;

                                column.Item().PaddingBottom(5).Padding(8).Background(bgColor).Border(1).BorderColor(borderColor).Column(recCol =>
                                {
                                    recCol.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(rec.Title).FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                                        row.ConstantItem(50).Text(rec.Severity).FontSize(8).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black).AlignRight();
                                    });
                                    recCol.Item().PaddingTop(2).Text(rec.Description).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                    if (!string.IsNullOrWhiteSpace(rec.Details))
                                    {
                                        recCol.Item().PaddingTop(2).Text($"Details: {rec.Details}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                    }
                                    recCol.Item().PaddingTop(3).Text($"Action: {rec.Action}").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Black);
                                });
                            }
                            
                            column.Item().PaddingTop(10);
                        }

                        // Employee Details Table
                        if (report.EmployeeDetails != null && report.EmployeeDetails.Any())
                        {
                            column.Item().Text("EMPLOYEE ATTENDANCE DETAILS").FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Black);
                            column.Item().PaddingTop(5);

                            column.Item().Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Employee Name
                                    columns.RelativeColumn(1.5f); // Role
                                    columns.RelativeColumn(1); // Regular Hours
                                    columns.RelativeColumn(1); // Overtime Hours
                                    columns.RelativeColumn(1); // Late Minutes
                                    columns.RelativeColumn(1); // Absent Days
                                    columns.RelativeColumn(1); // Leave Days
                                });

                                // Table Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Employee").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignLeft();
                                    header.Cell().Element(HeaderCellStyle).Text("Role").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignLeft();
                                    header.Cell().Element(HeaderCellStyle).Text("Regular Hours").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignRight();
                                    header.Cell().Element(HeaderCellStyle).Text("Overtime Hours").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignRight();
                                    header.Cell().Element(HeaderCellStyle).Text("Late Minutes").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignRight();
                                    header.Cell().Element(HeaderCellStyle).Text("Absent Days").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignRight();
                                    header.Cell().Element(HeaderCellStyle).Text("Leave Days").FontSize(9).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3).AlignRight();
                                });

                                // Table Rows
                                int rowIndex = 0;
                                foreach (var emp in report.EmployeeDetails)
                                {
                                    var isHighlighted = emp.LateMinutes > 30 || emp.AbsentDays > 2;
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignLeft().Text(emp.EmployeeName).FontSize(8);
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignLeft().Text(emp.Role).FontSize(8);
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignRight().Text($"{emp.RegularHours:N2}").FontSize(8).FontColor(isHighlighted && emp.RegularHours < 150 ? global::QuestPDF.Helpers.Colors.Red.Darken2 : global::QuestPDF.Helpers.Colors.Black);
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignRight().Text($"{emp.OvertimeHours:N2}").FontSize(8);
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignRight().Text(emp.LateMinutes.ToString()).FontSize(8).FontColor(isHighlighted && emp.LateMinutes > 30 ? global::QuestPDF.Helpers.Colors.Red.Darken2 : global::QuestPDF.Helpers.Colors.Black);
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignRight().Text(emp.AbsentDays.ToString()).FontSize(8).FontColor(isHighlighted && emp.AbsentDays > 2 ? global::QuestPDF.Helpers.Colors.Red.Darken2 : global::QuestPDF.Helpers.Colors.Black);
                                    table.Cell().Element(CellStyle).Background(rowIndex % 2 == 0 ? global::QuestPDF.Helpers.Colors.White : global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                                        .AlignRight().Text($"{emp.LeaveDays:N1}").FontSize(8);
                                    rowIndex++;
                                }
                            });
                        }
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                        x.Span(" | Generated by BRIGHTENROLL ENROLLMENT MANAGEMENT SYSTEM");
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
            .PaddingHorizontal(5)
            .Background(global::QuestPDF.Helpers.Colors.White);
    }

    private static global::QuestPDF.Infrastructure.IContainer HeaderCellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Border(0)
            .BorderBottom(1)
            .BorderColor(global::QuestPDF.Helpers.Colors.Grey.Darken1)
            .PaddingVertical(5)
            .PaddingHorizontal(5)
            .ExtendHorizontal()
            .Background(global::QuestPDF.Helpers.Colors.White);
    }
}

