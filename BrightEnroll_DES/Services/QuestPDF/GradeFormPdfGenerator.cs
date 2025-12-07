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

public class GradeFormPdfGenerator
{
    private readonly AppDbContext _context;
    private readonly GradeService _gradeService;
    private readonly IConfiguration _configuration;

    public GradeFormPdfGenerator(AppDbContext context, GradeService gradeService, IConfiguration configuration)
    {
        _context = context;
        _gradeService = gradeService;
        _configuration = configuration;
    }

    public async Task<byte[]> GenerateGradeFormAsync(int sectionId, int subjectId, string schoolYear, int teacherId)
    {
        // Get section information
        var section = await _context.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.SectionId == sectionId);

        if (section == null)
        {
            throw new Exception($"Section {sectionId} not found");
        }

        // Get subject information
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

        if (subject == null)
        {
            throw new Exception($"Subject {subjectId} not found");
        }

        // Get teacher information
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == teacherId);

        if (teacher == null)
        {
            throw new Exception($"Teacher {teacherId} not found");
        }

        // Get grade records
        var gradeRecords = await _gradeService.GetGradeRecordsAsync(
            teacherId,
            schoolYear,
            sectionId,
            subjectId,
            null
        );

        // Get students enrolled in the section
        var enrollments = await _context.StudentSectionEnrollments
            .Where(e => e.SectionId == sectionId 
                     && e.SchoolYear == schoolYear 
                     && e.Status == "Enrolled")
            .Include(e => e.Student)
            .OrderBy(e => e.Student != null ? e.Student.LastName : "")
            .ThenBy(e => e.Student != null ? e.Student.FirstName : "")
            .ToListAsync();

        // Format teacher name
        var teacherName = $"{teacher.FirstName} {teacher.MidName} {teacher.LastName}".Replace("  ", " ").Trim();
        if (!string.IsNullOrWhiteSpace(teacher.Suffix))
        {
            teacherName += $" {teacher.Suffix}";
        }

        // Format grade level and section
        var gradeLevelName = section.GradeLevel?.GradeLevelName ?? "";
        var sectionName = section.SectionName ?? "";
        var gradeSectionTerm = $"{gradeLevelName} {sectionName}";

        // Get school information from configuration
        var schoolName = _configuration["School:Name"] ?? "BRIGHTENROLL ELEMENTARY SCHOOL";
        var schoolAddress = _configuration["School:Address"] ?? "School Address, City, Province";
        var region = _configuration["School:Region"] ?? "Caraga Administrative Region";
        var division = _configuration["School:Division"] ?? "Division of Surigao del Norte";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content()
     .Column(column =>
     {
         // ---------- CENTERED SCHOOL NAME + ADDRESS ----------
         column.Item().Column(schoolHeaderCol =>
         {
             schoolHeaderCol.Item().Text(schoolName.ToUpper())
                 .FontSize(14)
                 .Bold()
                 .AlignCenter();

             schoolHeaderCol.Item().PaddingTop(2).Text(schoolAddress)
                 .FontSize(10)
                 .AlignCenter();
         });

         // ---------- HEADER WITH ONLY RIGHT SEAL ----------
         column.Item().PaddingTop(6).Row(headerRow =>
         {
             // Remove left seal � leave empty space for alignment
             headerRow.AutoItem().Width(60).Height(60);

             // Center spacing
             headerRow.RelativeItem().PaddingHorizontal(10);

             // RIGHT SEAL (kept)
             headerRow.AutoItem()
                 .Width(60).Height(60)
                 .Border(1).BorderColor(QuestPdfColors.Black)
                 .Column(sealCol =>
                 {
                     sealCol.Item()
                         .Padding(2)
                         .Text("Kagawaran ng Edukasyon")
                         .FontSize(6)
                         .AlignCenter();

                     sealCol.Item()
                         .Text("Department of Education")
                         .FontSize(7)
                         .Bold()
                         .AlignCenter();
                 });
         });

         // ---------- TITLE ----------
         column.Item().PaddingTop(8)
             .Text("GRADE FORM")
             .FontSize(12)
             .Bold()
             .AlignCenter();

         // ---------- TEACHER / SUBJECT / SY / GRADE-SECTION ----------
         column.Item().PaddingTop(6).Column(detailsCol =>
         {
             detailsCol.Item().Row(r =>
             {
                 r.AutoItem().Text("Teacher:").Bold();
                 r.RelativeItem().PaddingLeft(5).Text(teacherName);
             });

             detailsCol.Item().PaddingTop(2).Row(r =>
             {
                 r.AutoItem().Text("Subject:").Bold();
                 var subjectDisplay = !string.IsNullOrWhiteSpace(subject.SubjectCode)
                     ? $"{subject.SubjectCode} - {subject.SubjectName}"
                     : subject.SubjectName;
                 r.RelativeItem().PaddingLeft(5).Text(subjectDisplay);
             });

             detailsCol.Item().PaddingTop(2).Row(r =>
             {
                 r.AutoItem().Text("School Year:").Bold();
                 r.RelativeItem().PaddingLeft(5).Text(schoolYear);
             });

             detailsCol.Item().PaddingTop(2).Row(r =>
             {
                 r.AutoItem().Text("Grade & Section:").Bold();
                 r.RelativeItem().PaddingLeft(5).Text(gradeSectionTerm);
             });
         });

         // ---------- GRADE TABLE ----------
         column.Item().PaddingTop(8).Table(table =>
         {
             table.ColumnsDefinition(columns =>
             {
                 columns.ConstantColumn(80);
                 columns.RelativeColumn(2);
                 columns.ConstantColumn(60);
                 columns.ConstantColumn(60);
                 columns.ConstantColumn(60);
                 columns.ConstantColumn(60);
                 columns.ConstantColumn(60);
                 columns.RelativeColumn(1.5f);
             });

             table.Header(header =>
             {
                 header.Cell().Element(HeaderCellStyle).Text("Student No").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("Name").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("1st Quarter").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("2nd Quarter").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("3rd Quarter").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("4th Quarter").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("Final").FontSize(8).Bold().AlignCenter();
                 header.Cell().Element(HeaderCellStyle).Text("Remarks").FontSize(8).Bold().AlignCenter();
             });

             foreach (var enrollment in enrollments)
             {
                 var student = enrollment.Student;
                 if (student == null) continue;

                 var gradeRecord = gradeRecords.FirstOrDefault(g => g.StudentId == student.StudentId);

                 var studentName = $"{student.LastName}, {student.FirstName}";
                 if (!string.IsNullOrWhiteSpace(student.MiddleName))
                     studentName += $" {student.MiddleName[0]}.";
                 if (!string.IsNullOrWhiteSpace(student.Suffix))
                     studentName += $" {student.Suffix}";

                 decimal finalGrade = 0;
                 if (gradeRecord != null &&
                     gradeRecord.Q1 > 0 && gradeRecord.Q2 > 0 &&
                     gradeRecord.Q3 > 0 && gradeRecord.Q4 > 0)
                 {
                     finalGrade = (gradeRecord.Q1 + gradeRecord.Q2 + gradeRecord.Q3 + gradeRecord.Q4) / 4m;
                 }

                 string remarks = "No Grade";
                 if (gradeRecord != null)
                 {
                     if (finalGrade > 0)
                         remarks = finalGrade >= 75 ? "Passed" : "Failed";
                     else if (gradeRecord.Q1 > 0 ||
                              gradeRecord.Q2 > 0 ||
                              gradeRecord.Q3 > 0 ||
                              gradeRecord.Q4 > 0)
                         remarks = "Incomplete";
                 }

                 table.Cell().Element(CellStyle).Text(student.StudentId).FontSize(8);
                 table.Cell().Element(CellStyle).Text(studentName).FontSize(8);
                 table.Cell().Element(CellStyle).Text(gradeRecord?.Q1 > 0 ? gradeRecord.Q1.ToString("F1") : "").FontSize(8).AlignCenter();
                 table.Cell().Element(CellStyle).Text(gradeRecord?.Q2 > 0 ? gradeRecord.Q2.ToString("F1") : "").FontSize(8).AlignCenter();
                 table.Cell().Element(CellStyle).Text(gradeRecord?.Q3 > 0 ? gradeRecord.Q3.ToString("F1") : "").FontSize(8).AlignCenter();
                 table.Cell().Element(CellStyle).Text(gradeRecord?.Q4 > 0 ? gradeRecord.Q4.ToString("F1") : "").FontSize(8).AlignCenter();
                 table.Cell().Element(CellStyle).Text(finalGrade > 0 ? finalGrade.ToString("F1") : "").FontSize(8).AlignCenter();
                 table.Cell().Element(CellStyle).Text(remarks).FontSize(8).AlignCenter();
             }
         });
     });

            });
        }).GeneratePdf();
    }

    private static QuestPdfContainer CellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .Padding(4)
            .Background(QuestPdfColors.White);
    }

    private static QuestPdfContainer HeaderCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.5f)
            .BorderColor(QuestPdfColors.Black)
            .Padding(4)
            .Background(QuestPdfColors.Grey.Lighten3);
    }
}

