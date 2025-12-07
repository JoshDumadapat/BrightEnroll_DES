using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BrightEnroll_DES.Services.Business.Academic;
using QuestPdfContainer = QuestPDF.Infrastructure.IContainer;
using QuestPdfColors = QuestPDF.Helpers.Colors;

namespace BrightEnroll_DES.Services.QuestPDF;

/// <summary>
/// Generates DepEd SF9 (Learner's Progress Report Card) compliant PDF documents.
/// Optimized for A4 page size with automatic scaling and overflow prevention.
/// </summary>
public class ReportCardPdfGenerator
{
    private readonly AppDbContext _context;
    private readonly GradeService _gradeService;
    private readonly IConfiguration _configuration;

    public ReportCardPdfGenerator(AppDbContext context, GradeService gradeService, IConfiguration configuration)
    {
        _context = context;
        _gradeService = gradeService;
        _configuration = configuration;
    }

    public async Task<byte[]> GenerateReportCardAsync(string studentId, int sectionId, string schoolYear)
    {
        // Fetch student data
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

        // Format student name (Last Name, First Name M.)
        var studentName = $"{student.LastName}, {student.FirstName}";
        if (!string.IsNullOrWhiteSpace(student.MiddleName))
        {
            studentName += $" {student.MiddleName[0]}.";
        }
        if (!string.IsNullOrWhiteSpace(student.Suffix))
        {
            studentName += $" {student.Suffix}";
        }

        var age = DateTime.Now.Year - student.Birthdate.Year - (DateTime.Now.DayOfYear < student.Birthdate.DayOfYear ? 1 : 0);

        // Get classroom info if available
        var classroom = section.ClassroomId.HasValue
            ? await _context.Classrooms.FirstOrDefaultAsync(c => c.RoomId == section.ClassroomId.Value)
            : null;

        // Get school information from configuration
        var schoolName = _configuration["School:Name"] ?? "BRIGHTENROL ELEMENTARY SCHOOL";
        var schoolAddress = _configuration["School:Address"] ?? "School Address, City, Province";
        var region = _configuration["School:Region"] ?? "Region XI – Davao Region";
        var division = _configuration["School:Division"] ?? "Schools Division of Davao City";
        var district = _configuration["School:District"] ?? "District Name";
        var schoolId = _configuration["School:SchoolId"] ?? "";

        // Format adviser and principal names
        var adviserName = adviser != null 
            ? $"{adviser.LastName}, {adviser.FirstName} {adviser.MidName}".Replace("  ", " ").Trim()
            : "";
        var principalName = principal != null
            ? $"{principal.LastName}, {principal.FirstName} {principal.MidName}".Replace("  ", " ").Trim()
            : "";

        return Document.Create(container =>
        {
            // PAGE 1: Header, Learner Info, Learning Progress, Grading Scale
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(8));

                // Consistent Header on every page - Flexible height
                page.Header()
                    .Column(headerColumn =>
                    {
                        BuildCompactHeader(headerColumn, region, division, district, schoolName, schoolId);
                    });

                // Consistent Footer on every page
                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text($"SF9 - ES | {schoolName} | School Year {schoolYear}");

                page.Content()
                    .Column(column =>
                    {
                        // ========== TITLE ==========
                        column.Item().PaddingTop(4).Text("LEARNER'S PROGRESS REPORT CARD")
                            .FontSize(11).Bold().AlignCenter();
                        column.Item().PaddingTop(1).Text($"School Year {schoolYear}")
                            .FontSize(9).AlignCenter().Underline();

                        // ========== LEARNER INFORMATION ==========
                        BuildCompactLearnerInformation(column, student, studentName, age, gradeLevel, section, classroom);

                        // ========== PARENT MESSAGE ==========
                        column.Item().PaddingTop(4).Text("Dear Parent,").FontSize(7);
                        column.Item().PaddingTop(1).PaddingLeft(6)
                            .Text("This report card shows the ability and the progress your child has made in the different learning areas as well as his/her progress in core values.")
                            .FontSize(6.5f);
                        column.Item().PaddingTop(0.5f).PaddingLeft(6)
                            .Text("The school welcomes you should you desire to know more about your child's progress.")
                            .FontSize(6.5f);

                        // ========== LEARNING PROGRESS TABLE ==========
                        BuildCompactLearningProgressTable(column, gradeLevel, reportCardData, transmutedGeneralAverage, descriptiveRating);

                        // ========== GRADING SCALE / DESCRIPTORS TABLE ==========
                        BuildCompactGradingScaleTable(column);

                        // ========== SIGNATURES (PAGE 1) ==========
                        column.Item().PaddingTop(4).Row(signatureRow =>
                        {
                            signatureRow.RelativeItem().Column(teacherCol =>
                            {
                                teacherCol.Item().LineHorizontal(0.8f).LineColor(QuestPdfColors.Black);
                                teacherCol.Item().PaddingTop(1).Text(adviserName).FontSize(6.5f).AlignCenter();
                                teacherCol.Item().Text("Class Adviser").FontSize(6.5f).AlignCenter();
                            });
                            signatureRow.RelativeItem(0.1f);
                            signatureRow.RelativeItem().Column(principalCol =>
                            {
                                principalCol.Item().LineHorizontal(0.8f).LineColor(QuestPdfColors.Black);
                                principalCol.Item().PaddingTop(1).Text(principalName).FontSize(6.5f).AlignCenter();
                                principalCol.Item().Text("School Head").FontSize(6.5f).AlignCenter();
                            });
                        });
                    });
            });

            // PAGE 2: Attendance, Core Values, Certificates, Signatures
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(8));

                // Consistent Header - Flexible height
                page.Header()
                    .Column(headerColumn =>
                    {
                        BuildCompactHeader(headerColumn, region, division, district, schoolName, schoolId);
                    });

                // Consistent Footer
                page.Footer()
                    .Height(20)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7).FontColor(QuestPdfColors.Grey.Medium))
                    .Text($"SF9 - ES | {schoolName} | School Year {schoolYear}");

                page.Content()
                    .Column(column =>
                    {
                        column.Item().PaddingTop(4).Row(row =>
                        {
                            // ========== LEFT COLUMN: ATTENDANCE & PARENT SIGNATURE ==========
                            row.RelativeItem(0.48f).Column(leftColumn =>
                            {
                                BuildCompactAttendanceTable(leftColumn, attendanceData, schoolDaysByMonth);
                                BuildCompactParentSignatureSection(leftColumn);
                            });

                            row.RelativeItem(0.04f); // Spacing

                            // ========== RIGHT COLUMN: CORE VALUES & CERTIFICATES ==========
                            row.RelativeItem(0.48f).Column(rightColumn =>
                            {
                                BuildCompactCoreValuesTable(rightColumn);
                                BuildCompactCertificateOfTransferSection(rightColumn, gradeLevel, section, classroom);
                                BuildCompactApprovalSection(rightColumn, adviserName, principalName);
                                BuildCompactCancellationSection(rightColumn);
                            });
                        });
                    });
            });
        }).GeneratePdf();
    }

    #region Compact Header Section

    private void BuildCompactHeader(ColumnDescriptor column, string region, string division, 
        string district, string schoolName, string schoolId)
    {
        // Main header row with flexible seals
        column.Item().Row(headerRow =>
        {
            // Left Seal (DepEd Logo Placeholder) - Flexible width, no fixed height
            headerRow.RelativeItem(0.18f).MinWidth(40).Border(0.8f).BorderColor(QuestPdfColors.Black)
                .Padding(2)
                .Column(sealCol =>
                {
                    sealCol.Item().Text("Republic of the Philippines")
                        .FontSize(5)
                        .AlignCenter()
                        .WrapAnywhere();
                    sealCol.Item().PaddingTop(1).Text("Department of Education")
                        .FontSize(5.5f)
                        .Bold()
                        .AlignCenter()
                        .WrapAnywhere();
                    sealCol.Item().PaddingTop(1).Text(region)
                        .FontSize(5)
                        .AlignCenter()
                        .WrapAnywhere();
                });

            // Center: School Information - Flexible, takes remaining space
            headerRow.RelativeItem(0.64f).PaddingHorizontal(4).Column(schoolCol =>
            {
                schoolCol.Item().Text(schoolName.ToUpper())
                    .FontSize(9)
                    .Bold()
                    .AlignCenter()
                    .WrapAnywhere();
                schoolCol.Item().PaddingTop(1).Text($"School ID: {schoolId}")
                    .FontSize(7)
                    .AlignCenter();
            });

            // Right Seal (DepEd Logo Placeholder) - Flexible width, no fixed height
            headerRow.RelativeItem(0.18f).MinWidth(40).Border(0.8f).BorderColor(QuestPdfColors.Black)
                .Padding(2)
                .Column(sealCol =>
                {
                    sealCol.Item().Text("Kagawaran ng Edukasyon")
                        .FontSize(5)
                        .AlignCenter()
                        .WrapAnywhere();
                    sealCol.Item().PaddingTop(1).Text("Department of Education")
                        .FontSize(5.5f)
                        .Bold()
                        .AlignCenter()
                        .WrapAnywhere();
                });
        });

        // School Information Lines - Compact, with wrapping
        column.Item().PaddingTop(2).Row(infoRow =>
        {
            infoRow.RelativeItem().Text($"Region: {region}")
                .FontSize(6)
                .WrapAnywhere();
            infoRow.RelativeItem().Text($"Division: {division}")
                .FontSize(6)
                .WrapAnywhere();
            infoRow.RelativeItem().Text($"District: {district}")
                .FontSize(6)
                .WrapAnywhere();
        });
    }

    #endregion

    #region Compact Learner Information Section

    private void BuildCompactLearnerInformation(ColumnDescriptor column, Student student, 
        string studentName, int age, GradeLevel gradeLevel, Section section, Classroom? classroom)
    {
        column.Item().PaddingTop(3).Table(infoTable =>
        {
            infoTable.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(2);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(1.5f);
            });

            // Row 1: Name and LRN
            infoTable.Cell().Element(CompactInfoCellStyle).Text("Name:").FontSize(7).Bold();
            infoTable.Cell().Element(CompactInfoCellStyle).Text(studentName).FontSize(7);
            infoTable.Cell().Element(CompactInfoCellStyle).Text("LRN:").FontSize(7).Bold();
            infoTable.Cell().Element(CompactInfoCellStyle).Text(student.Lrn ?? "").FontSize(7);

            // Row 2: Grade & Section, Sex, Age
            infoTable.Cell().Element(CompactInfoCellStyle).Text("Grade & Section:").FontSize(7).Bold();
            infoTable.Cell().Element(CompactInfoCellStyle).Text($"{gradeLevel.GradeLevelName} - {section.SectionName}").FontSize(7);
            infoTable.Cell().Element(CompactInfoCellStyle).Text("Sex:").FontSize(7).Bold();
            infoTable.Cell().Element(CompactInfoCellStyle).Text($"{student.Sex ?? ""} | Age: {age}").FontSize(7);
        });
    }

    #endregion

    #region Compact Learning Progress Table

    private void BuildCompactLearningProgressTable(ColumnDescriptor column, GradeLevel gradeLevel, 
        List<SubjectGradeData> reportCardData, decimal transmutedGeneralAverage, string descriptiveRating)
    {
        column.Item().PaddingTop(4).Text($"GRADE {gradeLevel.GradeLevelName.ToUpper()}")
            .FontSize(8).Bold();
        column.Item().Text("REPORT ON LEARNING PROGRESS AND ACHIEVEMENT")
            .FontSize(7).Bold();

        column.Item().PaddingTop(2).Table(gradesTable =>
        {
            gradesTable.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.2f); // Learning Areas
                columns.ConstantColumn(35);   // Q1
                columns.ConstantColumn(35);   // Q2
                columns.ConstantColumn(35);   // Q3
                columns.ConstantColumn(35);   // Q4
                columns.ConstantColumn(40);   // Final Rating
                columns.RelativeColumn(1.2f);  // Remarks
            });

            // Header
            gradesTable.Header(header =>
            {
                header.Cell().Element(CompactHeaderCellStyle).Text("Learning Areas").FontSize(7).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q1").FontSize(6.5f).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q2").FontSize(6.5f).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q3").FontSize(6.5f).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q4").FontSize(6.5f).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Final").FontSize(6.5f).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Remarks").FontSize(6.5f).Bold().AlignCenter();
            });

            // Subject rows
            foreach (var subject in reportCardData)
            {
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.SubjectName).FontSize(6.5f);
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.Q1?.ToString("F1") ?? "").FontSize(6.5f).AlignCenter();
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.Q2?.ToString("F1") ?? "").FontSize(6.5f).AlignCenter();
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.Q3?.ToString("F1") ?? "").FontSize(6.5f).AlignCenter();
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.Q4?.ToString("F1") ?? "").FontSize(6.5f).AlignCenter();
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.FinalRating?.ToString("F1") ?? "").FontSize(6.5f).AlignCenter();
                gradesTable.Cell().Element(CompactCellStyle).Text(subject.Remarks).FontSize(6).AlignCenter();
            }

            // General Average row
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text("General Average").FontSize(7).Bold();
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text("").FontSize(6.5f); // Q1 - empty
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text("").FontSize(6.5f); // Q2 - empty
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text("").FontSize(6.5f); // Q3 - empty
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text("").FontSize(6.5f); // Q4 - empty
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text(transmutedGeneralAverage > 0 ? transmutedGeneralAverage.ToString("F1") : "").FontSize(7).Bold().AlignCenter(); // Final
            gradesTable.Cell().Element(CompactGeneralAverageCellStyle).Text(descriptiveRating).FontSize(7).Bold().AlignCenter(); // Remarks
        });
    }

    #endregion

    #region Compact Grading Scale Table

    private void BuildCompactGradingScaleTable(ColumnDescriptor column)
    {
        column.Item().PaddingTop(3).Text("Grading Scale / Descriptors").FontSize(7).Bold();
        
        column.Item().PaddingTop(1).Table(descriptorsTable =>
        {
            descriptorsTable.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            // Header
            descriptorsTable.Cell().Element(CompactHeaderCellStyle).Text("Descriptors").FontSize(6.5f).Bold();
            descriptorsTable.Cell().Element(CompactHeaderCellStyle).Text("Grading Scale").FontSize(6.5f).Bold().AlignCenter();
            descriptorsTable.Cell().Element(CompactHeaderCellStyle).Text("Remarks").FontSize(6.5f).Bold().AlignCenter();

            // Rows
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Outstanding").FontSize(6);
            descriptorsTable.Cell().Element(CompactCellStyle).Text("90-100").FontSize(6).AlignCenter();
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Passed").FontSize(6).AlignCenter();

            descriptorsTable.Cell().Element(CompactCellStyle).Text("Very Satisfactory").FontSize(6);
            descriptorsTable.Cell().Element(CompactCellStyle).Text("85-89").FontSize(6).AlignCenter();
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Passed").FontSize(6).AlignCenter();

            descriptorsTable.Cell().Element(CompactCellStyle).Text("Satisfactory").FontSize(6);
            descriptorsTable.Cell().Element(CompactCellStyle).Text("80-84").FontSize(6).AlignCenter();
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Passed").FontSize(6).AlignCenter();

            descriptorsTable.Cell().Element(CompactCellStyle).Text("Fairly Satisfactory").FontSize(6);
            descriptorsTable.Cell().Element(CompactCellStyle).Text("75-79").FontSize(6).AlignCenter();
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Passed").FontSize(6).AlignCenter();

            descriptorsTable.Cell().Element(CompactCellStyle).Text("Did Not Meet Expectations").FontSize(6);
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Below 75").FontSize(6).AlignCenter();
            descriptorsTable.Cell().Element(CompactCellStyle).Text("Failed").FontSize(6).AlignCenter();
        });
    }

    #endregion

    #region Compact Attendance Table

    private void BuildCompactAttendanceTable(ColumnDescriptor column, 
        Dictionary<int, AttendanceMonthData> attendanceData, Dictionary<int, int> schoolDaysByMonth)
    {
        column.Item().Text("ATTENDANCE RECORD").FontSize(8).Bold().AlignCenter();
        
        column.Item().PaddingTop(2).Table(attendanceTable =>
        {
            // Use relative columns that scale automatically
            attendanceTable.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.5f); // Label column
                // 12 months - use relative columns that will scale
                for (int i = 0; i < 12; i++)
                {
                    columns.RelativeColumn(1); // Each month gets equal space
                }
            });

            // Header row
            attendanceTable.Cell().Element(CompactAttendanceHeaderCellStyle).Text("").FontSize(5.5f);
            var months = new[] { "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "Jan", "Feb", "Mar", "Apr", "May" };
            foreach (var month in months)
            {
                attendanceTable.Cell().Element(CompactAttendanceHeaderCellStyle).Text(month).FontSize(5.5f).AlignCenter();
            }

            // No. of School Days row
            attendanceTable.Cell().Element(CompactAttendanceCellStyle).Text("School Days").FontSize(5.5f);
            var totalSchoolDays = 0;
            foreach (var month in new[] { 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5 })
            {
                var days = schoolDaysByMonth.ContainsKey(month) ? schoolDaysByMonth[month] : 0;
                totalSchoolDays += days;
                attendanceTable.Cell().Element(CompactAttendanceCellStyle).Text(days > 0 ? days.ToString() : "").FontSize(5.5f).AlignCenter();
            }

            // No. of Days Present row
            attendanceTable.Cell().Element(CompactAttendanceCellStyle).Text("Days Present").FontSize(5.5f);
            var totalPresent = 0;
            foreach (var month in new[] { 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5 })
            {
                var present = attendanceData.ContainsKey(month) ? attendanceData[month].DaysPresent : 0;
                totalPresent += present;
                attendanceTable.Cell().Element(CompactAttendanceCellStyle).Text(present > 0 ? present.ToString() : "").FontSize(5.5f).AlignCenter();
            }

            // No. of Times Absent row
            attendanceTable.Cell().Element(CompactAttendanceCellStyle).Text("Days Absent").FontSize(5.5f);
            var totalAbsent = 0;
            foreach (var month in new[] { 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 4, 5 })
            {
                var absent = attendanceData.ContainsKey(month) ? attendanceData[month].DaysAbsent : 0;
                totalAbsent += absent;
                attendanceTable.Cell().Element(CompactAttendanceCellStyle).Text(absent > 0 ? absent.ToString() : "").FontSize(5.5f).AlignCenter();
            }
        });
    }

    #endregion

    #region Compact Parent Signature Section

    private void BuildCompactParentSignatureSection(ColumnDescriptor column)
    {
        column.Item().PaddingTop(6).Text("PARENT/GUARDIAN'S SIGNATURE").FontSize(7).Bold().AlignCenter();
        
        column.Item().PaddingTop(2).Row(q1Row =>
        {
            q1Row.AutoItem().Text("1st Q:").FontSize(6);
            q1Row.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
        });
        
        column.Item().PaddingTop(3).Row(q2Row =>
        {
            q2Row.AutoItem().Text("2nd Q:").FontSize(6);
            q2Row.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
        });
        
        column.Item().PaddingTop(3).Row(q3Row =>
        {
            q3Row.AutoItem().Text("3rd Q:").FontSize(6);
            q3Row.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
        });
        
        column.Item().PaddingTop(3).Row(q4Row =>
        {
            q4Row.AutoItem().Text("4th Q:").FontSize(6);
            q4Row.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
        });
    }

    #endregion

    #region Compact Core Values Table

    private void BuildCompactCoreValuesTable(ColumnDescriptor column)
    {
        column.Item().Text("CORE VALUES / OBSERVED VALUES").FontSize(8).Bold().AlignCenter();
        
        column.Item().PaddingTop(2).Table(coreValuesTable =>
        {
            coreValuesTable.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.ConstantColumn(25);
                columns.ConstantColumn(25);
                columns.ConstantColumn(25);
                columns.ConstantColumn(25);
                columns.RelativeColumn(1.2f);
            });

            // Header
            coreValuesTable.Header(header =>
            {
                header.Cell().Element(CompactHeaderCellStyle).Text("Core Values").FontSize(6.5f).Bold();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q1").FontSize(6).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q2").FontSize(6).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q3").FontSize(6).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Q4").FontSize(6).Bold().AlignCenter();
                header.Cell().Element(CompactHeaderCellStyle).Text("Remarks").FontSize(6).Bold().AlignCenter();
            });

            // Core Values rows
            var coreValues = new[] { "Maka-Diyos", "Maka-tao", "Makakalikasan", "Makabansa" };
            foreach (var value in coreValues)
            {
                coreValuesTable.Cell().Element(CompactCellStyle).Text(value).FontSize(6);
                coreValuesTable.Cell().Element(CompactCellStyle).Text("").FontSize(6).AlignCenter();
                coreValuesTable.Cell().Element(CompactCellStyle).Text("").FontSize(6).AlignCenter();
                coreValuesTable.Cell().Element(CompactCellStyle).Text("").FontSize(6).AlignCenter();
                coreValuesTable.Cell().Element(CompactCellStyle).Text("").FontSize(6).AlignCenter();
                coreValuesTable.Cell().Element(CompactCellStyle).Text("").FontSize(6).AlignCenter();
            }
        });
    }

    #endregion

    #region Compact Certificate of Transfer Section

    private void BuildCompactCertificateOfTransferSection(ColumnDescriptor column, 
        GradeLevel gradeLevel, Section section, Classroom? classroom)
    {
        column.Item().PaddingTop(4).Text("CERTIFICATE OF TRANSFER / ELIGIBILITY").FontSize(7).Bold().AlignCenter();
        
        column.Item().PaddingTop(2).Column(transferCol =>
        {
            transferCol.Item().Row(r =>
            {
                r.AutoItem().Text("Admitted to Grade:").FontSize(6);
                r.RelativeItem(1.5f).PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
                r.RelativeItem(0.2f);
                r.AutoItem().Text("Section:").FontSize(6);
                r.RelativeItem(1).PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
                r.RelativeItem(0.2f);
                r.AutoItem().Text("Room:").FontSize(6);
                r.RelativeItem(1).PaddingLeft(2).Text(classroom?.RoomName ?? "").FontSize(6);
            });
            
            transferCol.Item().PaddingTop(2).Row(r =>
            {
                r.AutoItem().Text("Eligible for Admission to Grade:").FontSize(6);
                r.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
            });
        });
    }

    #endregion

    #region Compact Approval Section

    private void BuildCompactApprovalSection(ColumnDescriptor column, string adviserName, string principalName)
    {
        column.Item().PaddingTop(4).Text("Approved:").FontSize(6.5f).Bold();
        
        column.Item().PaddingTop(2).Row(approvalRow =>
        {
            approvalRow.RelativeItem().Column(principalApprovalCol =>
            {
                principalApprovalCol.Item().LineHorizontal(0.8f).LineColor(QuestPdfColors.Black);
                principalApprovalCol.Item().PaddingTop(0.5f).Text(principalName).FontSize(6).AlignCenter();
                principalApprovalCol.Item().Text("School Head").FontSize(6).AlignCenter();
            });
            approvalRow.RelativeItem(0.1f);
            approvalRow.RelativeItem().Column(teacherApprovalCol =>
            {
                teacherApprovalCol.Item().LineHorizontal(0.8f).LineColor(QuestPdfColors.Black);
                teacherApprovalCol.Item().PaddingTop(0.5f).Text(adviserName).FontSize(6).AlignCenter();
                teacherApprovalCol.Item().Text("Class Adviser").FontSize(6).AlignCenter();
            });
        });
    }

    #endregion

    #region Compact Cancellation Section

    private void BuildCompactCancellationSection(ColumnDescriptor column)
    {
        column.Item().PaddingTop(4).Text("CANCELLATION OF ELIGIBILITY TO TRANSFER").FontSize(6.5f).Bold().AlignCenter();
        
        column.Item().PaddingTop(2).Column(cancellationCol =>
        {
            cancellationCol.Item().Row(r =>
            {
                r.AutoItem().Text("Admitted in:").FontSize(6);
                r.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
            });
            cancellationCol.Item().PaddingTop(2).Row(r =>
            {
                r.AutoItem().Text("Date:").FontSize(6);
                r.RelativeItem().PaddingLeft(2).LineHorizontal(0.4f).LineColor(QuestPdfColors.Black);
            });
        });

        column.Item().PaddingTop(3).Row(principalCancelRow =>
        {
            principalCancelRow.RelativeItem();
            principalCancelRow.RelativeItem().Column(principalCancelCol =>
            {
                principalCancelCol.Item().LineHorizontal(0.8f).LineColor(QuestPdfColors.Black);
                principalCancelCol.Item().PaddingTop(0.5f).Text("Principal").FontSize(6).AlignCenter();
            });
        });
    }

    #endregion

    #region Helper Methods

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

        // Group by month (June to December)
        for (int month = 6; month <= 12; month++)
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

        // Group by month (January to May)
        for (int month = 1; month <= 5; month++)
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

        // Calculate for each month in the school year (June to December)
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

        // Calculate for each month (January to May)
        for (int month = 1; month <= 5; month++)
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

    #endregion

    #region Compact Cell Styles

    private static QuestPdfContainer CompactCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.4f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(2)
            .PaddingHorizontal(2);
    }

    private static QuestPdfContainer CompactHeaderCellStyle(QuestPdfContainer container)
    {
        return container
            .Background(QuestPdfColors.Grey.Lighten3)
            .Border(0.4f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(2.5f)
            .PaddingHorizontal(2);
    }

    private static QuestPdfContainer CompactGeneralAverageCellStyle(QuestPdfContainer container)
    {
        return container
            .Background(QuestPdfColors.Grey.Lighten4)
            .Border(0.4f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(2.5f)
            .PaddingHorizontal(2);
    }

    private static QuestPdfContainer CompactAttendanceCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.4f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(1.5f)
            .PaddingHorizontal(1);
    }

    private static QuestPdfContainer CompactAttendanceHeaderCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.4f)
            .BorderColor(QuestPdfColors.Black)
            .Background(QuestPdfColors.Grey.Lighten4)
            .PaddingVertical(1.5f)
            .PaddingHorizontal(1);
    }

    private static QuestPdfContainer CompactInfoCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.4f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(1.5f)
            .PaddingHorizontal(2);
    }

    #endregion

    #region Data Classes

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

    #endregion
}
