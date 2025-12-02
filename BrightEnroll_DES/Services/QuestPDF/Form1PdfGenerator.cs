using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.QuestPDF;

public class Form1PdfGenerator
{
    private readonly AppDbContext _context;

    public Form1PdfGenerator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateForm1Async(string studentId, string schoolYear)
    {
        // Fetch student data with all related information
        var student = await _context.Students
            .Include(s => s.Guardian)
            .Include(s => s.SectionEnrollments)
                .ThenInclude(e => e.Section)
                    .ThenInclude(sec => sec.GradeLevel)
            .Include(s => s.SectionEnrollments)
                .ThenInclude(e => e.Section)
                    .ThenInclude(sec => sec.Classroom)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student == null)
        {
            throw new Exception($"Student {studentId} not found");
        }

        // Get enrollment for the school year
        var enrollment = student.SectionEnrollments
            .FirstOrDefault(e => e.SchoolYear == schoolYear && e.Status == "Enrolled");

        if (enrollment == null)
        {
            throw new Exception($"Student {studentId} is not enrolled for school year {schoolYear}");
        }

        var section = enrollment.Section;
        if (section == null)
        {
            throw new Exception($"Section not found for enrollment of student {studentId}");
        }

        var gradeLevel = section.GradeLevel;
        var classroom = section.Classroom;
        var buildingName = classroom?.BuildingName;

        // Get subject schedules for the section through ClassSchedule (actual section schedule)
        // First get subject sections for this section
        var subjectSections = await _context.SubjectSections
            .Include(ss => ss.Subject)
            .Where(ss => ss.SectionId == section.SectionId)
            .ToListAsync();

        // Get class schedules (actual section schedules) for teacher assignments in this section
        var classSchedulesQuery = _context.ClassSchedules
            .Where(cs => cs.Assignment != null && cs.Assignment.SectionId == section.SectionId && !cs.Assignment.IsArchived);
        
        var classSchedules = await classSchedulesQuery
            .Include(cs => cs.Assignment!)
                .ThenInclude(a => a.Teacher)
            .Include(cs => cs.Assignment!)
                .ThenInclude(a => a.Subject)
            .OrderBy(cs => cs.DayOfWeek)
            .ThenBy(cs => cs.StartTime)
            .ToListAsync();

        // Get fees for the grade level
        Fee? fee = null;
        if (gradeLevel != null)
        {
            fee = await _context.Fees
                .Include(f => f.GradeLevel)
                .FirstOrDefaultAsync(f => f.GradeLevelId == gradeLevel.GradeLevelId);
        }

        // Get fee breakdown
        var feeBreakdown = fee != null
            ? await _context.FeeBreakdowns
                .Where(fb => fb.FeeId == fee.FeeId)
                .OrderBy(fb => fb.ItemName)
                .ToListAsync()
            : new List<FeeBreakdown>();

        // Get teacher assignments for the section
        var teacherAssignments = await _context.TeacherSectionAssignments
            .Include(tsa => tsa.Teacher)
            .Include(tsa => tsa.Section)
            .Include(tsa => tsa.Subject)
            .Where(tsa => tsa.SectionId == section.SectionId && !tsa.IsArchived)
            .ToListAsync();

        var adviser = teacherAssignments
            .FirstOrDefault(ta => ta.Role == "Adviser" || ta.Role == "Advisor");

        // Build student full name
        var studentName = $"{student.FirstName} {student.MiddleName} {student.LastName}".Replace("  ", " ").Trim();
        if (!string.IsNullOrWhiteSpace(student.Suffix))
        {
            studentName += $" {student.Suffix}";
        }

        // Build guardian name
        var guardianName = "N/A";
        if (student.Guardian != null)
        {
            guardianName = $"{student.Guardian.FirstName} {student.Guardian.LastName}".Replace("  ", " ").Trim();
        }

        // Generate PDF
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Header with Logo and School Name
                page.Header()
                    .Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            // Logo placeholder with BrightEnroll branding
                            row.ConstantItem(80).Height(80).Background(global::QuestPDF.Helpers.Colors.Blue.Darken3)
                                .AlignCenter().AlignMiddle().Column(logoCol =>
                                {
                                    logoCol.Item().Text("BE").FontSize(24).Bold().FontColor(global::QuestPDF.Helpers.Colors.White);
                                    logoCol.Item().Text("BRIGHTENROLL").FontSize(6).FontColor(global::QuestPDF.Helpers.Colors.White);
                                });
                            
                            row.RelativeItem().PaddingLeft(10).Column(col =>
                            {
                                col.Item().Text("BRIGHTENROLL ELEMENTARY SCHOOL").FontSize(18).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                                col.Item().Text("Enrollment Management System").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                col.Item().PaddingTop(5).Text("FORM 1 - ENROLLMENT CERTIFICATE").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            });
                        });
                    });

                // Content
                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Student Information Section
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Text("STUDENT INFORMATION").FontSize(12).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Cell().Element(CellStyle).Text("Student Name:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(studentName).FontSize(9).Bold();

                            table.Cell().Element(CellStyle).Text("Student ID:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(student.StudentId).FontSize(9);

                            table.Cell().Element(CellStyle).Text("LRN:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(student.Lrn ?? "N/A").FontSize(9);

                            table.Cell().Element(CellStyle).Text("Birth Date:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(student.Birthdate.ToString("MMMM dd, yyyy")).FontSize(9);

                            table.Cell().Element(CellStyle).Text("Guardian Name:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(guardianName).FontSize(9);
                        });

                        // Enrollment Details Section
                        column.Item().PaddingTop(0.5f, Unit.Centimetre).PaddingBottom(0.5f, Unit.Centimetre).Text("ENROLLMENT DETAILS").FontSize(12).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                        
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Cell().Element(CellStyle).Text("School Year:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(schoolYear).FontSize(9).Bold();

                            table.Cell().Element(CellStyle).Text("Grade Level:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(gradeLevel?.GradeLevelName ?? "N/A").FontSize(9).Bold();

                            table.Cell().Element(CellStyle).Text("Section:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(section?.SectionName ?? "N/A").FontSize(9).Bold();

                            table.Cell().Element(CellStyle).Text("Classroom:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(classroom?.RoomName ?? "N/A").FontSize(9);

                            table.Cell().Element(CellStyle).Text("Building:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(buildingName ?? "N/A").FontSize(9);

                            table.Cell().Element(CellStyle).Text("Adviser:").FontSize(9);
                            table.Cell().Element(CellStyle).Text(adviser?.Teacher != null 
                                ? $"{adviser.Teacher.FirstName} {adviser.Teacher.LastName}".Replace("  ", " ").Trim() 
                                : "N/A").FontSize(9);
                        });

                        // Subject Schedule Section
                        if (classSchedules.Any())
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).PaddingBottom(0.5f, Unit.Centimetre).Text("SUBJECT SCHEDULE").FontSize(12).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Subject").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Schedule").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Time").FontSize(9).Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Teacher").FontSize(9).Bold();
                                });

                                // Group by subject and time range
                                var groupedSchedules = classSchedules
                                    .Where(cs => cs.Assignment != null && cs.Assignment.Subject != null)
                                    .GroupBy(cs => new 
                                    { 
                                        SubjectId = cs.Assignment!.SubjectId, 
                                        SubjectName = cs.Assignment.Subject!.SubjectName,
                                        StartTime = cs.StartTime,
                                        EndTime = cs.EndTime
                                    })
                                    .ToList();

                                foreach (var group in groupedSchedules)
                                {
                                    var subjectName = group.Key.SubjectName ?? "N/A";
                                    var schedules = group.ToList();
                                    var firstSchedule = schedules.First();
                                    var teacherName = firstSchedule.Assignment?.Teacher != null
                                        ? $"{firstSchedule.Assignment.Teacher.FirstName} {firstSchedule.Assignment.Teacher.LastName}".Replace("  ", " ").Trim()
                                        : "TBA";

                                    var days = schedules.Select(s => s.DayOfWeek).Distinct().OrderBy(d => d).ToList();
                                    var dayString = string.Join(", ", days);
                                    var startTime = DateTime.Today.Add(firstSchedule.StartTime);
                                    var endTime = DateTime.Today.Add(firstSchedule.EndTime);
                                    var timeRange = $"{startTime:hh:mm tt} - {endTime:hh:mm tt}";

                                    table.Cell().Element(CellStyle).Text(subjectName).FontSize(8);
                                    table.Cell().Element(CellStyle).Text(dayString).FontSize(8);
                                    table.Cell().Element(CellStyle).Text(timeRange).FontSize(8);
                                    table.Cell().Element(CellStyle).Text(teacherName).FontSize(8);
                                }
                            });
                        }

                        // Tuition Breakdown Section
                        if (fee != null)
                        {
                            column.Item().PaddingTop(0.5f, Unit.Centimetre).PaddingBottom(0.5f, Unit.Centimetre).Text("TUITION BREAKDOWN").FontSize(12).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });

                                if (feeBreakdown.Any())
                                {
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Item").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount").FontSize(9).Bold();
                                    });

                                    foreach (var item in feeBreakdown)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.ItemName).FontSize(8);
                                        table.Cell().Element(CellStyle).AlignRight().Text($"₱{item.Amount:N2}").FontSize(8);
                                    }
                                }

                                // Summary
                                table.Cell().Element(SummaryCellStyle).Text("Tuition Fee:").FontSize(9).Bold();
                                table.Cell().Element(SummaryCellStyle).AlignRight().Text($"₱{fee.TuitionFee:N2}").FontSize(9).Bold();

                                table.Cell().Element(SummaryCellStyle).Text("Miscellaneous Fee:").FontSize(9).Bold();
                                table.Cell().Element(SummaryCellStyle).AlignRight().Text($"₱{fee.MiscFee:N2}").FontSize(9).Bold();

                                table.Cell().Element(SummaryCellStyle).Text("Other Fees:").FontSize(9).Bold();
                                table.Cell().Element(SummaryCellStyle).AlignRight().Text($"₱{fee.OtherFee:N2}").FontSize(9).Bold();

                                var totalFee = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                                table.Cell().Element(TotalCellStyle).Text("TOTAL:").FontSize(10).Bold();
                                table.Cell().Element(TotalCellStyle).AlignRight().Text($"₱{totalFee:N2}").FontSize(10).Bold();
                            });
                        }

                        // Footer Note
                        column.Item().PaddingTop(1, Unit.Centimetre).Padding(10).Background(global::QuestPDF.Helpers.Colors.Grey.Lighten4)
                            .Column(col =>
                            {
                                col.Item().Text("This document serves as proof of enrollment for the student named above.").FontSize(8).Italic();
                                col.Item().PaddingTop(3).Text($"Generated on: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}").FontSize(7).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium))
                    .Text(x =>
                    {
                        x.Span("BrightEnroll Elementary School - Form 1 - Enrollment Certificate");
                    });
            });
        }).GeneratePdf();
    }

    private static global::QuestPDF.Infrastructure.IContainer CellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(8);
    }

    private static global::QuestPDF.Infrastructure.IContainer HeaderCellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Background(global::QuestPDF.Helpers.Colors.Blue.Lighten5)
            .BorderBottom(1)
            .BorderColor(global::QuestPDF.Helpers.Colors.Blue.Darken1)
            .PaddingVertical(6)
            .PaddingHorizontal(8);
    }

    private static global::QuestPDF.Infrastructure.IContainer SummaryCellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .BorderTop(1)
            .BorderColor(global::QuestPDF.Helpers.Colors.Grey.Medium)
            .PaddingVertical(5)
            .PaddingHorizontal(8);
    }

    private static global::QuestPDF.Infrastructure.IContainer TotalCellStyle(global::QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Background(global::QuestPDF.Helpers.Colors.Blue.Lighten4)
            .BorderTop(1)
            .BorderBottom(1)
            .BorderColor(global::QuestPDF.Helpers.Colors.Blue.Darken2)
            .PaddingVertical(6)
            .PaddingHorizontal(8);
    }
}

