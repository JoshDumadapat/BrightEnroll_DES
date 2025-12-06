using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Services.Business.Academic;
using QuestPdfContainer = QuestPDF.Infrastructure.IContainer;
using QuestPdfColors = QuestPDF.Helpers.Colors;

namespace BrightEnroll_DES.Services.QuestPDF;

public class ReportCardPdfGenerator
{
    private readonly AppDbContext _context;
    private readonly GradeService _gradeService;

    public ReportCardPdfGenerator(AppDbContext context, GradeService gradeService)
    {
        _context = context;
        _gradeService = gradeService;
    }

    public async Task<byte[]> GenerateReportCardAsync(string studentId, int sectionId, string schoolYear)
    {
        var student = await _context.Students
            .Include(s => s.Guardian)
            .Include(s => s.SectionEnrollments)
                .ThenInclude(e => e.Section)
                    .ThenInclude(sec => sec!.GradeLevel)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student == null)
        {
            throw new Exception($"Student {studentId} not found");
        }

        var enrollment = student.SectionEnrollments
            .FirstOrDefault(e => e.SectionId == sectionId && e.SchoolYear == schoolYear && e.Status == "Enrolled");

        if (enrollment == null)
        {
            throw new Exception($"Student {studentId} is not enrolled in section {sectionId} for school year {schoolYear}");
        }

        var section = enrollment.Section;
        if (section == null)
        {
            throw new Exception($"Section {sectionId} not found");
        }

        var gradeLevel = section.GradeLevel;
        if (gradeLevel == null)
        {
            throw new Exception($"Grade level not found for section {sectionId}");
        }

        // Get teacher (adviser) and principal information
        var adviser = section.AdviserId.HasValue 
            ? await _context.Users.FirstOrDefaultAsync(u => u.UserId == section.AdviserId.Value)
            : null;
        
        var principal = await _context.Users
            .Where(u => u.UserRole == "Principal" || u.UserRole == "Admin")
            .FirstOrDefaultAsync();

        // Calculate school year dates
        var yearParts = schoolYear.Split('-');
        var schoolYearStart = yearParts.Length > 0 && int.TryParse(yearParts[0], out var startYear) 
            ? new DateTime(startYear, 6, 1) 
            : new DateTime(DateTime.Now.Year, 6, 1);
        var schoolYearEnd = yearParts.Length > 1 && int.TryParse(yearParts[1], out var endYear)
            ? new DateTime(endYear, 5, 31)
            : schoolYearStart.AddYears(1).AddDays(-1);

        // Fetch attendance data by month
        var attendanceData = await GetAttendanceByMonthAsync(studentId, sectionId, schoolYear, schoolYearStart, schoolYearEnd);
        var schoolDaysByMonth = CalculateSchoolDaysByMonth(schoolYearStart, schoolYearEnd);

        // Get grades and process subjects
        var studentGrades = await _gradeService.GetStudentGradesAsync(studentId, schoolYear);
        var gradesForSection = studentGrades.Where(g => g.SectionId == sectionId).ToList();

        var allSubjectsForGradeLevel = await _context.Subjects
            .Where(s => s.GradeLevelId == gradeLevel.GradeLevelId && s.IsActive)
            .OrderBy(s => s.SubjectName)
            .ToListAsync();

        // Process subjects with MAPEH aggregation
        var reportCardData = await ProcessSubjectsWithMAPEHAsync(allSubjectsForGradeLevel, gradesForSection);

        // Calculate general average
        var generalAverage = await _gradeService.CalculateGeneralAverageAsync(studentId, sectionId, schoolYear);
        var transmutedGeneralAverage = generalAverage > 0 ? _gradeService.GetTransmutedGrade(generalAverage) : 0;
        var descriptiveRating = generalAverage > 0 ? _gradeService.GetDescriptiveRating(transmutedGeneralAverage) : "Incomplete";

        // Format student name
        var studentName = $"{student.FirstName} {student.MiddleName} {student.LastName}".Replace("  ", " ").Trim();
        if (!string.IsNullOrWhiteSpace(student.Suffix))
        {
            studentName += $" {student.Suffix}";
        }

        var age = DateTime.Now.Year - student.Birthdate.Year - (DateTime.Now.DayOfYear < student.Birthdate.DayOfYear ? 1 : 0);

        // Get classroom info if available
        var classroom = section.ClassroomId.HasValue
            ? await _context.Classrooms.FirstOrDefaultAsync(c => c.RoomId == section.ClassroomId.Value)
            : null;

        return Document.Create(container =>
        {
            // PAGE 1: Student Info, Learning Areas & Grades
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0.8f, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content()
                    .Column(column =>
                    {
                        // Header Section
                        column.Item().Row(headerRow =>
                        {
                            headerRow.AutoItem().Height(40).Width(40).Border(1).BorderColor(QuestPdfColors.Black);
                            headerRow.RelativeItem().PaddingLeft(8).Column(headerCol =>
                            {
                                headerCol.Item().Text("Republic of the Philippines").FontSize(8).AlignCenter();
                                headerCol.Item().Text("DEPARTMENT OF EDUCATION").FontSize(9).Bold().AlignCenter();
                            });
                        });

                        // School Information
                        column.Item().PaddingTop(6).Column(schoolInfoCol =>
                        {
                            schoolInfoCol.Item().Row(r =>
                            {
                                r.AutoItem().Text("Region").FontSize(7);
                                r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                            });
                            schoolInfoCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.AutoItem().Text("Division").FontSize(7);
                                r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                            });
                            schoolInfoCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.AutoItem().Text("District").FontSize(7);
                                r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                            });
                            schoolInfoCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.AutoItem().Text("School").FontSize(7);
                                r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                            });
                        });

                        column.Item().PaddingTop(6).Text("LEARNER'S PROGRESS REPORT CARD").FontSize(10).Bold().AlignCenter();
                        column.Item().PaddingTop(2).Text($"School Year {schoolYear}").FontSize(8).AlignCenter().Underline();

                        // Student Information
                        column.Item().PaddingTop(6).Column(studentInfoCol =>
                        {
                            studentInfoCol.Item().Row(r =>
                            {
                                r.AutoItem().Text("Name:").FontSize(7);
                                r.RelativeItem().PaddingLeft(3).Text(studentName).FontSize(7).Bold();
                            });
                            studentInfoCol.Item().PaddingTop(3).Row(r =>
                            {
                                r.AutoItem().Text("LRN:").FontSize(7);
                                r.RelativeItem(1).PaddingLeft(3).Text(student.Lrn ?? "").FontSize(7);
                                r.RelativeItem(0.1f);
                                r.AutoItem().Text("Age:").FontSize(7);
                                r.RelativeItem(0.5f).PaddingLeft(3).Text(age.ToString()).FontSize(7);
                                r.RelativeItem(0.1f);
                                r.AutoItem().Text("Sex:").FontSize(7);
                                r.RelativeItem(0.5f).PaddingLeft(3).Text(student.Sex ?? "").FontSize(7);
                            });
                            studentInfoCol.Item().PaddingTop(3).Row(r =>
                            {
                                r.AutoItem().Text("Grade:").FontSize(7);
                                r.RelativeItem(1).PaddingLeft(3).Text(gradeLevel.GradeLevelName).FontSize(7);
                                r.RelativeItem(0.1f);
                                r.AutoItem().Text("Section:").FontSize(7);
                                r.RelativeItem(1).PaddingLeft(3).Text(section.SectionName).FontSize(7);
                            });
                            studentInfoCol.Item().PaddingTop(3).Row(r =>
                            {
                                r.AutoItem().Text("Admitted to Grade").FontSize(7);
                                r.RelativeItem(1).PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                r.RelativeItem(0.1f);
                                r.AutoItem().Text("Section").FontSize(7);
                                r.RelativeItem(0.8f).PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                r.RelativeItem(0.1f);
                                r.AutoItem().Text("Room").FontSize(7);
                                r.RelativeItem(0.8f).PaddingLeft(3).Text(classroom?.RoomName ?? "").FontSize(7);
                            });
                        });

                        column.Item().PaddingTop(6).Text("Dear Parent,").FontSize(7);
                        column.Item().PaddingTop(2).PaddingLeft(4).Text("This report card shows the ability and the progress your child has made in the different learning areas as well as his/her progress in core values.").FontSize(6);
                        column.Item().PaddingTop(1).PaddingLeft(4).Text("The school welcomes you should you desire to know more about your child's progress.").FontSize(6);

                        // Learning Areas Table
                        column.Item().PaddingTop(8).Text($"GRADE {gradeLevel.GradeLevelName.ToUpper()}").FontSize(8).Bold();
                        column.Item().Text("REPORT ON LEARNING PROGRESS AND ACHIEVEMENT").FontSize(7).Bold();

                        column.Item().PaddingTop(4).Table(gradesTable =>
                        {
                            gradesTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f); // Learning Areas
                                columns.RelativeColumn(1);     // Q1
                                columns.RelativeColumn(1);     // Q2
                                columns.RelativeColumn(1);     // Q3
                                columns.RelativeColumn(1);     // Q4
                                columns.RelativeColumn(1.2f);  // Final Rating
                                columns.RelativeColumn(1.5f);  // Remarks
                            });

                            // Header
                            gradesTable.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Learning Areas").FontSize(7).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("1").FontSize(7).Bold().AlignCenter();
                                header.Cell().Element(HeaderCellStyle).Text("2").FontSize(7).Bold().AlignCenter();
                                header.Cell().Element(HeaderCellStyle).Text("3").FontSize(7).Bold().AlignCenter();
                                header.Cell().Element(HeaderCellStyle).Text("4").FontSize(7).Bold().AlignCenter();
                                header.Cell().Element(HeaderCellStyle).Text("Final Rating").FontSize(7).Bold().AlignCenter();
                                header.Cell().Element(HeaderCellStyle).Text("Remarks").FontSize(7).Bold().AlignCenter();
                            });

                            // Subject rows
                            foreach (var subject in reportCardData)
                            {
                                gradesTable.Cell().Element(CellStyle).Text(subject.SubjectName).FontSize(7);
                                gradesTable.Cell().Element(CellStyle).Text(subject.Q1?.ToString("F1") ?? "").FontSize(7).AlignCenter();
                                gradesTable.Cell().Element(CellStyle).Text(subject.Q2?.ToString("F1") ?? "").FontSize(7).AlignCenter();
                                gradesTable.Cell().Element(CellStyle).Text(subject.Q3?.ToString("F1") ?? "").FontSize(7).AlignCenter();
                                gradesTable.Cell().Element(CellStyle).Text(subject.Q4?.ToString("F1") ?? "").FontSize(7).AlignCenter();
                                gradesTable.Cell().Element(CellStyle).Text(subject.FinalRating?.ToString("F1") ?? "").FontSize(7).AlignCenter();
                                gradesTable.Cell().Element(CellStyle).Text(subject.Remarks).FontSize(6).AlignCenter();
                            }

                            // General Average row
                            gradesTable.Cell().Element(GeneralAverageCellStyle).Text("General Average").FontSize(7).Bold();
                            gradesTable.Cell().Element(GeneralAverageCellStyle).Text("").FontSize(7);
                            gradesTable.Cell().Element(GeneralAverageCellStyle).Text("").FontSize(7);
                            gradesTable.Cell().Element(GeneralAverageCellStyle).Text("").FontSize(7);
                            gradesTable.Cell().Element(GeneralAverageCellStyle).Text(transmutedGeneralAverage > 0 ? transmutedGeneralAverage.ToString("F1") : "").FontSize(7).Bold().AlignCenter();
                            gradesTable.Cell().Element(GeneralAverageCellStyle).Text(descriptiveRating).FontSize(7).Bold().AlignCenter();
                        });

                        // Descriptors Table
                        column.Item().PaddingTop(6).Table(descriptorsTable =>
                        {
                            descriptorsTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            descriptorsTable.Cell().Element(CellStyle).Text("Descriptors").FontSize(6).Bold();
                            descriptorsTable.Cell().Element(CellStyle).Text("Grading Scale").FontSize(6).Bold();
                            descriptorsTable.Cell().Element(CellStyle).Text("Remarks").FontSize(6).Bold();

                            descriptorsTable.Cell().Element(CellStyle).Text("Outstanding").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("90-100").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("Passed").FontSize(6);

                            descriptorsTable.Cell().Element(CellStyle).Text("Very Satisfactory").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("85-89").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("Passed").FontSize(6);

                            descriptorsTable.Cell().Element(CellStyle).Text("Satisfactory").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("80-84").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("Passed").FontSize(6);

                            descriptorsTable.Cell().Element(CellStyle).Text("Fairly Satisfactory").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("75-79").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("Passed").FontSize(6);

                            descriptorsTable.Cell().Element(CellStyle).Text("Did Not Meet Expectations").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("Below 75").FontSize(6);
                            descriptorsTable.Cell().Element(CellStyle).Text("Failed").FontSize(6);
                        });

                        // Signatures
                        column.Item().PaddingTop(8).Row(signatureRow =>
                        {
                            signatureRow.RelativeItem().Column(teacherCol =>
                            {
                                teacherCol.Item().LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                teacherCol.Item().PaddingTop(2).Text("Teacher").FontSize(6).AlignCenter();
                            });
                            signatureRow.RelativeItem(0.1f);
                            signatureRow.RelativeItem().Column(principalCol =>
                            {
                                principalCol.Item().LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                principalCol.Item().PaddingTop(2).Text("Head Teacher/Principal").FontSize(6).AlignCenter();
                            });
                        });
                    });
            });

            // PAGE 2: Attendance, Core Values, Signatures
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0.8f, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content()
                    .Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            // LEFT COLUMN - Attendance Record & Parent Signature
                            row.RelativeItem(0.4f).Column(leftColumn =>
                            {
                                leftColumn.Item().Text("Sf9 - ES").FontSize(6).FontColor(QuestPdfColors.Grey.Medium);
                                
                                leftColumn.Item().PaddingTop(6).Text("ATTENDANCE RECORD").FontSize(8).Bold().AlignCenter();
                                
                                leftColumn.Item().PaddingTop(4).Table(attendanceTable =>
                                {
                                    attendanceTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f);
                                        for (int i = 0; i < 12; i++)
                                        {
                                            columns.RelativeColumn(1);
                                        }
                                    });

                                    // Header row
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("").FontSize(6);
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Jun").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Jul").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Aug").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Sept").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Oct").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Nov").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Dec").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Jan").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Feb").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Mar").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Apr").FontSize(6).AlignCenter();
                                    attendanceTable.Cell().Element(AttendanceHeaderCellStyle).Text("Total").FontSize(6).AlignCenter();

                                    // No. of School Days row
                                    attendanceTable.Cell().Element(AttendanceCellStyle).Text("No. of School Days").FontSize(6);
                                    var totalSchoolDays = 0;
                                    foreach (var month in new[] { 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4 })
                                    {
                                        var days = schoolDaysByMonth.ContainsKey(month) ? schoolDaysByMonth[month] : 0;
                                        totalSchoolDays += days;
                                        attendanceTable.Cell().Element(AttendanceCellStyle).Text(days > 0 ? days.ToString() : "").FontSize(6).AlignCenter();
                                    }
                                    attendanceTable.Cell().Element(AttendanceCellStyle).Text(totalSchoolDays > 0 ? totalSchoolDays.ToString() : "").FontSize(6).AlignCenter();

                                    // No. of Days Present row
                                    attendanceTable.Cell().Element(AttendanceCellStyle).Text("No. of Days Present").FontSize(6);
                                    var totalPresent = 0;
                                    foreach (var month in new[] { 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4 })
                                    {
                                        var present = attendanceData.ContainsKey(month) ? attendanceData[month].DaysPresent : 0;
                                        totalPresent += present;
                                        attendanceTable.Cell().Element(AttendanceCellStyle).Text(present > 0 ? present.ToString() : "").FontSize(6).AlignCenter();
                                    }
                                    attendanceTable.Cell().Element(AttendanceCellStyle).Text(totalPresent > 0 ? totalPresent.ToString() : "").FontSize(6).AlignCenter();

                                    // No. of Times Absent row
                                    attendanceTable.Cell().Element(AttendanceCellStyle).Text("No. of Times Absent").FontSize(6);
                                    var totalAbsent = 0;
                                    foreach (var month in new[] { 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4 })
                                    {
                                        var absent = attendanceData.ContainsKey(month) ? attendanceData[month].DaysAbsent : 0;
                                        totalAbsent += absent;
                                        attendanceTable.Cell().Element(AttendanceCellStyle).Text(absent > 0 ? absent.ToString() : "").FontSize(6).AlignCenter();
                                    }
                                    attendanceTable.Cell().Element(AttendanceCellStyle).Text(totalAbsent > 0 ? totalAbsent.ToString() : "").FontSize(6).AlignCenter();
                                });

                                leftColumn.Item().PaddingTop(12).Text("PARENT/GUARDIAN'S SIGNATURE").FontSize(7).Bold().AlignCenter();
                                leftColumn.Item().PaddingTop(4).Row(q1Row =>
                                {
                                    q1Row.AutoItem().Text("1st Quarter").FontSize(6);
                                    q1Row.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                });
                                leftColumn.Item().PaddingTop(6).Row(q2Row =>
                                {
                                    q2Row.AutoItem().Text("2nd Quarter").FontSize(6);
                                    q2Row.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                });
                                leftColumn.Item().PaddingTop(6).Row(q3Row =>
                                {
                                    q3Row.AutoItem().Text("3rd Quarter").FontSize(6);
                                    q3Row.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                });
                                leftColumn.Item().PaddingTop(6).Row(q4Row =>
                                {
                                    q4Row.AutoItem().Text("4th Quarter").FontSize(6);
                                    q4Row.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                });
                            });

                            // RIGHT COLUMN - Core Values & Certificates
                            row.RelativeItem(0.6f).PaddingLeft(10).Column(rightColumn =>
                            {
                                rightColumn.Item().Text("Sf9 - ES").FontSize(6).FontColor(QuestPdfColors.Grey.Medium);
                                
                                rightColumn.Item().PaddingTop(6).Text("CORE VALUES").FontSize(8).Bold().AlignCenter();
                                
                                // Core Values Table
                                rightColumn.Item().PaddingTop(4).Table(coreValuesTable =>
                                {
                                    coreValuesTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    coreValuesTable.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Core Values").FontSize(6).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Q1").FontSize(6).Bold().AlignCenter();
                                        header.Cell().Element(HeaderCellStyle).Text("Q2").FontSize(6).Bold().AlignCenter();
                                        header.Cell().Element(HeaderCellStyle).Text("Q3").FontSize(6).Bold().AlignCenter();
                                        header.Cell().Element(HeaderCellStyle).Text("Q4").FontSize(6).Bold().AlignCenter();
                                    });

                                    // Core Values rows (standard DepEd core values)
                                    var coreValues = new[] { "Maka-Diyos", "Maka-tao", "Makakalikasan", "Makabansa" };
                                    foreach (var value in coreValues)
                                    {
                                        coreValuesTable.Cell().Element(CellStyle).Text(value).FontSize(6);
                                        coreValuesTable.Cell().Element(CellStyle).Text("").FontSize(6).AlignCenter();
                                        coreValuesTable.Cell().Element(CellStyle).Text("").FontSize(6).AlignCenter();
                                        coreValuesTable.Cell().Element(CellStyle).Text("").FontSize(6).AlignCenter();
                                        coreValuesTable.Cell().Element(CellStyle).Text("").FontSize(6).AlignCenter();
                                    }
                                });

                                rightColumn.Item().PaddingTop(8).Text("Certificate of Transfer").FontSize(7).Bold().AlignCenter();
                                rightColumn.Item().PaddingTop(4).Column(transferCol =>
                                {
                                    transferCol.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Admitted to Grade").FontSize(6);
                                        r.RelativeItem(2).PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                        r.RelativeItem(0.05f);
                                        r.AutoItem().Text("Section").FontSize(6);
                                        r.RelativeItem(1).PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                        r.RelativeItem(0.05f);
                                        r.AutoItem().Text("Room").FontSize(6);
                                        r.RelativeItem(1).PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                    });
                                    transferCol.Item().PaddingTop(3).Row(r =>
                                    {
                                        r.AutoItem().Text("Eligible for Admission to Grade").FontSize(6);
                                        r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                    });
                                });

                                rightColumn.Item().PaddingTop(6).Text("Approved:").FontSize(6).Bold();
                                rightColumn.Item().PaddingTop(4).Row(approvalRow =>
                                {
                                    approvalRow.RelativeItem().Column(principalApprovalCol =>
                                    {
                                        principalApprovalCol.Item().LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                        principalApprovalCol.Item().PaddingTop(1).Text("Head Teacher/Principal").FontSize(6).AlignCenter();
                                    });
                                    approvalRow.RelativeItem(0.1f);
                                    approvalRow.RelativeItem().Column(teacherApprovalCol =>
                                    {
                                        teacherApprovalCol.Item().LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                        teacherApprovalCol.Item().PaddingTop(1).Text("Teacher").FontSize(6).AlignCenter();
                                    });
                                });

                                rightColumn.Item().PaddingTop(8).Text("Cancellation of Eligibility to Transfer").FontSize(6).Bold().AlignCenter();
                                rightColumn.Item().PaddingTop(4).Column(cancellationCol =>
                                {
                                    cancellationCol.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Admitted in").FontSize(6);
                                        r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                    });
                                    cancellationCol.Item().PaddingTop(3).Row(r =>
                                    {
                                        r.AutoItem().Text("Date:").FontSize(6);
                                        r.RelativeItem().PaddingLeft(3).LineHorizontal(0.5f).LineColor(QuestPdfColors.Black);
                                    });
                                });

                                rightColumn.Item().PaddingTop(6).Row(principalCancelRow =>
                                {
                                    principalCancelRow.RelativeItem();
                                    principalCancelRow.RelativeItem().Column(principalCancelCol =>
                                    {
                                        principalCancelCol.Item().LineHorizontal(1).LineColor(QuestPdfColors.Black);
                                        principalCancelCol.Item().PaddingTop(1).Text("Principal").FontSize(6).AlignCenter();
                                    });
                                });
                            });
                        });
                    });
            });
        }).GeneratePdf();
    }

    private async Task<List<SubjectGradeData>> ProcessSubjectsWithMAPEHAsync(
        List<Subject> allSubjects, 
        List<Grade> gradesForSection)
    {
        var reportCardData = new List<SubjectGradeData>();
        
        // Identify MAPEH components
        var mapehComponents = allSubjects.Where(s => 
            s.SubjectName.Contains("Music", StringComparison.OrdinalIgnoreCase) ||
            s.SubjectName.Contains("Arts", StringComparison.OrdinalIgnoreCase) ||
            s.SubjectName.Contains("Physical Education", StringComparison.OrdinalIgnoreCase) ||
            s.SubjectName.Contains("PE", StringComparison.OrdinalIgnoreCase) ||
            s.SubjectName.Contains("Health", StringComparison.OrdinalIgnoreCase) ||
            s.SubjectName.Contains("MAPEH", StringComparison.OrdinalIgnoreCase)).ToList();

        var otherSubjects = allSubjects.Except(mapehComponents).ToList();

        // Process non-MAPEH subjects
        foreach (var subject in otherSubjects)
        {
            var subjectData = await ProcessSubjectGradesAsync(subject, gradesForSection);
            if (subjectData != null)
            {
                reportCardData.Add(subjectData);
            }
        }

        // Aggregate MAPEH if components exist
        if (mapehComponents.Any())
        {
            var mapehData = await AggregateMAPEHAsync(mapehComponents, gradesForSection);
            if (mapehData != null)
            {
                reportCardData.Add(mapehData);
            }
        }

        return reportCardData;
    }

    private async Task<SubjectGradeData?> ProcessSubjectGradesAsync(
        Subject subject, 
        List<Grade> gradesForSection)
    {
        var subjectGrades = gradesForSection.Where(g => g.SubjectId == subject.SubjectId).ToList();
        
        var q1Grade = subjectGrades.FirstOrDefault(g => g.GradingPeriod == "Q1");
        var q2Grade = subjectGrades.FirstOrDefault(g => g.GradingPeriod == "Q2");
        var q3Grade = subjectGrades.FirstOrDefault(g => g.GradingPeriod == "Q3");
        var q4Grade = subjectGrades.FirstOrDefault(g => g.GradingPeriod == "Q4");

        var q1 = q1Grade?.FinalGrade ?? 0;
        var q2 = q2Grade?.FinalGrade ?? 0;
        var q3 = q3Grade?.FinalGrade ?? 0;
        var q4 = q4Grade?.FinalGrade ?? 0;

        decimal finalRating = 0;
        string remarks = "No Grade";

        if (q1 > 0 && q2 > 0 && q3 > 0 && q4 > 0)
        {
            finalRating = (q1 + q2 + q3 + q4) / 4.0m;
            var transmutedGrade = _gradeService.GetTransmutedGrade(finalRating);
            remarks = _gradeService.GetDescriptiveRating(transmutedGrade);
        }
        else if (q1 > 0 || q2 > 0 || q3 > 0 || q4 > 0)
        {
            remarks = "Incomplete";
        }

        return new SubjectGradeData
        {
            SubjectName = subject.SubjectName,
            Q1 = q1 > 0 ? _gradeService.GetTransmutedGrade(q1) : (decimal?)null,
            Q2 = q2 > 0 ? _gradeService.GetTransmutedGrade(q2) : (decimal?)null,
            Q3 = q3 > 0 ? _gradeService.GetTransmutedGrade(q3) : (decimal?)null,
            Q4 = q4 > 0 ? _gradeService.GetTransmutedGrade(q4) : (decimal?)null,
            FinalRating = finalRating > 0 ? _gradeService.GetTransmutedGrade(finalRating) : (decimal?)null,
            Remarks = remarks
        };
    }

    private async Task<SubjectGradeData?> AggregateMAPEHAsync(
        List<Subject> mapehComponents, 
        List<Grade> gradesForSection)
    {
        var mapehGrades = new List<decimal>();
        var q1Grades = new List<decimal>();
        var q2Grades = new List<decimal>();
        var q3Grades = new List<decimal>();
        var q4Grades = new List<decimal>();

        foreach (var component in mapehComponents)
        {
            var componentGrades = gradesForSection.Where(g => g.SubjectId == component.SubjectId).ToList();
            
            var q1 = componentGrades.FirstOrDefault(g => g.GradingPeriod == "Q1")?.FinalGrade ?? 0;
            var q2 = componentGrades.FirstOrDefault(g => g.GradingPeriod == "Q2")?.FinalGrade ?? 0;
            var q3 = componentGrades.FirstOrDefault(g => g.GradingPeriod == "Q3")?.FinalGrade ?? 0;
            var q4 = componentGrades.FirstOrDefault(g => g.GradingPeriod == "Q4")?.FinalGrade ?? 0;

            if (q1 > 0) q1Grades.Add(q1);
            if (q2 > 0) q2Grades.Add(q2);
            if (q3 > 0) q3Grades.Add(q3);
            if (q4 > 0) q4Grades.Add(q4);
        }

        decimal? q1Avg = q1Grades.Any() ? q1Grades.Average() : null;
        decimal? q2Avg = q2Grades.Any() ? q2Grades.Average() : null;
        decimal? q3Avg = q3Grades.Any() ? q3Grades.Average() : null;
        decimal? q4Avg = q4Grades.Any() ? q4Grades.Average() : null;

        decimal finalRating = 0;
        string remarks = "No Grade";

        if (q1Avg.HasValue && q2Avg.HasValue && q3Avg.HasValue && q4Avg.HasValue)
        {
            finalRating = (q1Avg.Value + q2Avg.Value + q3Avg.Value + q4Avg.Value) / 4.0m;
            var transmutedGrade = _gradeService.GetTransmutedGrade(finalRating);
            remarks = _gradeService.GetDescriptiveRating(transmutedGrade);
        }
        else if (q1Avg.HasValue || q2Avg.HasValue || q3Avg.HasValue || q4Avg.HasValue)
        {
            remarks = "Incomplete";
        }

        return new SubjectGradeData
        {
            SubjectName = "MAPEH",
            Q1 = q1Avg.HasValue ? _gradeService.GetTransmutedGrade(q1Avg.Value) : null,
            Q2 = q2Avg.HasValue ? _gradeService.GetTransmutedGrade(q2Avg.Value) : null,
            Q3 = q3Avg.HasValue ? _gradeService.GetTransmutedGrade(q3Avg.Value) : null,
            Q4 = q4Avg.HasValue ? _gradeService.GetTransmutedGrade(q4Avg.Value) : null,
            FinalRating = finalRating > 0 ? _gradeService.GetTransmutedGrade(finalRating) : null,
            Remarks = remarks
        };
    }

    private async Task<Dictionary<int, AttendanceMonthData>> GetAttendanceByMonthAsync(
        string studentId, 
        int sectionId, 
        string schoolYear, 
        DateTime schoolYearStart, 
        DateTime schoolYearEnd)
    {
        var attendanceRecords = await _context.Attendances
            .Where(a => a.StudentId == studentId 
                && a.SectionId == sectionId 
                && a.SchoolYear == schoolYear
                && a.AttendanceDate >= schoolYearStart 
                && a.AttendanceDate <= schoolYearEnd)
            .ToListAsync();

        var result = new Dictionary<int, AttendanceMonthData>();

        // Group by month
        for (int month = 6; month <= 12; month++) // June to December
        {
            var monthRecords = attendanceRecords
                .Where(a => a.AttendanceDate.Month == month)
                .GroupBy(a => a.AttendanceDate.Date)
                .Select(g => g.OrderByDescending(a => a.AttendanceId).First())
                .ToList();

            var daysPresent = monthRecords.Count(a => a.Status == "Present");
            var daysAbsent = monthRecords.Count(a => a.Status == "Absent");

            result[month] = new AttendanceMonthData
            {
                DaysPresent = daysPresent,
                DaysAbsent = daysAbsent
            };
        }

        for (int month = 1; month <= 4; month++) // January to April
        {
            var monthRecords = attendanceRecords
                .Where(a => a.AttendanceDate.Month == month)
                .GroupBy(a => a.AttendanceDate.Date)
                .Select(g => g.OrderByDescending(a => a.AttendanceId).First())
                .ToList();

            var daysPresent = monthRecords.Count(a => a.Status == "Present");
            var daysAbsent = monthRecords.Count(a => a.Status == "Absent");

            result[month] = new AttendanceMonthData
            {
                DaysPresent = daysPresent,
                DaysAbsent = daysAbsent
            };
        }

        return result;
    }

    private Dictionary<int, int> CalculateSchoolDaysByMonth(DateTime schoolYearStart, DateTime schoolYearEnd)
    {
        var result = new Dictionary<int, int>();

        // Calculate for each month in the school year (June to April)
        for (int month = 6; month <= 12; month++)
        {
            var year = schoolYearStart.Year;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            if (startDate < schoolYearStart) startDate = schoolYearStart;
            if (endDate > schoolYearEnd) endDate = schoolYearEnd;

            var schoolDays = CountWeekdays(startDate, endDate);
            result[month] = schoolDays;
        }

        for (int month = 1; month <= 4; month++)
        {
            var year = schoolYearEnd.Year;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            if (startDate < schoolYearStart) startDate = schoolYearStart;
            if (endDate > schoolYearEnd) endDate = schoolYearEnd;

            var schoolDays = CountWeekdays(startDate, endDate);
            result[month] = schoolDays;
        }

        return result;
    }

    private int CountWeekdays(DateTime startDate, DateTime endDate)
    {
        int count = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }
        }
        return count;
    }

    private static QuestPdfContainer CellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(3)
            .PaddingHorizontal(3);
    }

    private static QuestPdfContainer HeaderCellStyle(QuestPdfContainer container)
    {
        return container
            .Background(QuestPdfColors.Grey.Lighten3)
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(4)
            .PaddingHorizontal(3);
    }

    private static QuestPdfContainer GeneralAverageCellStyle(QuestPdfContainer container)
    {
        return container
            .Background(QuestPdfColors.Grey.Lighten4)
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(4)
            .PaddingHorizontal(3);
    }

    private static QuestPdfContainer AttendanceCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(2)
            .PaddingHorizontal(2);
    }

    private static QuestPdfContainer AttendanceHeaderCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .Background(QuestPdfColors.Grey.Lighten4)
            .PaddingVertical(2)
            .PaddingHorizontal(2);
    }

    private class SubjectGradeData
    {
        public string SubjectName { get; set; } = "";
        public decimal? Q1 { get; set; }
        public decimal? Q2 { get; set; }
        public decimal? Q3 { get; set; }
        public decimal? Q4 { get; set; }
        public decimal? FinalRating { get; set; }
        public string Remarks { get; set; } = "";
    }

    private class AttendanceMonthData
    {
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
    }
}
