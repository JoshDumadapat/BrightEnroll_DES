using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BrightEnroll_DES.Components.Pages.Admin.StudentRecord.StudentRecordCS;
using Microsoft.Extensions.Configuration;
using QuestPdfContainer = QuestPDF.Infrastructure.IContainer;
using QuestPdfColors = QuestPDF.Helpers.Colors;

namespace BrightEnroll_DES.Services.QuestPDF;


public class Form137PdfGenerator
{
    private readonly IConfiguration _configuration;

    private class GroupedAcademicRecord
    {
        public string SchoolYear { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public List<AcademicRecordData> Records { get; set; } = new();
    }

    public Form137PdfGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<byte[]> GenerateForm137Async(StudentDetailData studentData, List<AcademicRecordData> academicRecords)
    {
        // Get school information from configuration
        var schoolName = _configuration["School:Name"] ?? "BRIGHTENROLL ELEMENTARY SCHOOL";
        var schoolAddress = _configuration["School:Address"] ?? "School Address, City, Province";
        var region = _configuration["School:Region"] ?? "Region XI – Davao Region";
        var division = _configuration["School:Division"] ?? "Schools Division of Davao City";
        var district = _configuration["School:District"] ?? "District Name";
        var schoolId = _configuration["School:SchoolId"] ?? "";

        // Group academic records by school year and grade
        var groupedRecords = academicRecords
            .GroupBy(r => new { r.SchoolYear, r.Grade })
            .OrderBy(g => g.Key.Grade)
            .ThenBy(g => g.Key.SchoolYear)
            .Select(g => new GroupedAcademicRecord
            {
                SchoolYear = g.Key.SchoolYear,
                Grade = g.Key.Grade,
                Records = g.ToList()
            })
            .ToList();

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(QuestPdfColors.White);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Content()
                    .Column(column =>
                    {
                        // ========== HEADER SECTION ==========
                        BuildForm137Header(column, region, division, district, schoolName, schoolId, studentData.LRN ?? "");

                        // ========== STUDENT AND PARENT/GUARDIAN INFORMATION ==========
                        BuildStudentAndParentInfo(column, studentData, schoolName, division);

                        // ========== ACADEMIC PROGRESS RECORDS ==========
                        BuildAcademicProgressRecords(column, groupedRecords, schoolName);
                    });
            });
        }).GeneratePdf();

        return Task.FromResult(pdfBytes);
    }

    private void BuildForm137Header(ColumnDescriptor column,
     string region, string division, string district,
     string schoolName, string schoolId, string lrn)
    {
        column.Item().PaddingBottom(8).Column(header =>
        {
            // --- Top: Republic + DepEd seal + department name ---
            header.Item().Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(col =>
                {
                    col.Item().Text("Republic of the Philippines")
                        .FontSize(8)
                        .AlignCenter();

                    col.Item().Text("Department of Education")
                        .FontSize(9)
                        .Bold()
                        .AlignCenter();

                    col.Item().Text(region)
                        .FontSize(8)
                        .AlignCenter();

                });
            });

            // --- School name + address centered ---
            header.Item().PaddingTop(5).Column(col =>
            {
                col.Item().Text(schoolName)
                    .FontSize(11)
                    .Bold()
                    .AlignCenter();

                col.Item().Text(division)
                    .FontSize(8)
                    .AlignCenter();

                col.Item().Text(district)
                    .FontSize(8)
                    .AlignCenter();

                col.Item().PaddingTop(2)
                    .Text("SCHOOL ID: " + schoolId)
                    .FontSize(8)
                    .AlignCenter();

                col.Item().PaddingTop(4)
                    .Text("ELEMENTARY SCHOOL PERMANENT RECORD (Form 137-E)")
                    .FontSize(9)
                    .Bold()
                    .AlignCenter();
            });

            // --- LRN Field ---
            header.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().AlignCenter().Row(lrnRow =>
                {
                    lrnRow.AutoItem().Text("LRN:").Bold().FontSize(8);
                    lrnRow.RelativeItem()
                        .PaddingLeft(3)
                        .BorderBottom(0.6f)
                        .BorderColor(QuestPdfColors.Black)
                        .Text(lrn)
                        .FontSize(8);
                });
            });
        });
    }


    private void BuildStudentAndParentInfo(
     ColumnDescriptor column,
     StudentDetailData studentData,
     string schoolName,
     string division)
    {
        column.Item().PaddingTop(10).Border(0.8f)
            .BorderColor(QuestPdfColors.Black)
            .Padding(6)
            .Column(info =>
            {
                // --- Title ---
                info.Item().AlignCenter().PaddingBottom(4).Text("STUDENT INFORMATION")
                    .Bold()
                    .FontSize(8);

                // --- Student Name ---
                info.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Surname").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text(studentData.LastName).FontSize(7);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("First Name").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text(studentData.FirstName).FontSize(7);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Middle Initial").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text(!string.IsNullOrWhiteSpace(studentData.MiddleName)
                                ? studentData.MiddleName[0].ToString()
                                : "")
                            .FontSize(7);
                    });
                });

                info.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Sex").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text(studentData.Sex).FontSize(7);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Date of Birth").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text(studentData.BirthDate?.ToString("MMM dd, yyyy"))
                            .FontSize(7);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Place of Birth").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text(studentData.PlaceOfBirth).FontSize(7);
                    });
                });

                // --- Address ---
                info.Item().PaddingTop(5).Column(col =>
                {
                    col.Item().Text("Address").FontSize(6).Bold();
                    col.Item().BorderBottom(0.4f).PaddingBottom(2)
                        .Text($"{studentData.CurrentHouseNo} {studentData.CurrentStreetName}, {studentData.CurrentBarangay}, {studentData.CurrentCity}, {studentData.CurrentProvince}")
                        .FontSize(7);
                });

                // --- Parent / Guardian ---
                info.Item().PaddingTop(7).AlignCenter()
                    .Text("PARENT / GUARDIAN INFORMATION")
                    .Bold()
                    .FontSize(8);

                info.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Name").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text($"{studentData.GuardianFirstName} {studentData.GuardianLastName}")
                            .FontSize(7);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Occupation").FontSize(6).Bold();
                        col.Item().BorderBottom(0.4f).PaddingBottom(2)
                            .Text("")  // Add if available
                            .FontSize(7);
                    });
                });
            });
    }


    private void BuildAcademicProgressRecords(ColumnDescriptor column, 
        List<GroupedAcademicRecord> groupedRecords, string schoolName)
    {
        column.Item().PaddingTop(4).Text("ACADEMIC PROGRESS RECORDS")
            .FontSize(8)
            .Bold()
            .AlignCenter();

        // Process records in pairs (two columns side by side)
        for (int i = 0; i < groupedRecords.Count; i += 2)
        {
            var leftGroup = groupedRecords[i];
            var rightGroup = i + 1 < groupedRecords.Count ? groupedRecords[i + 1] : null;

            column.Item().PaddingTop(3).Row(gradeRow =>
            {
                // Left Column
                gradeRow.RelativeItem(0.48f).Column(leftGradeCol =>
                {
                    BuildGradeAcademicTable(leftGradeCol, leftGroup.SchoolYear, leftGroup.Grade, leftGroup.Records, schoolName);
                });

                // Spacing
                gradeRow.RelativeItem(0.04f);

                // Right Column (if exists)
                if (rightGroup != null)
                {
                    gradeRow.RelativeItem(0.48f).Column(rightGradeCol =>
                    {
                        BuildGradeAcademicTable(rightGradeCol, rightGroup.SchoolYear, rightGroup.Grade, rightGroup.Records, schoolName);
                    });
                }
                else
                {
                    gradeRow.RelativeItem(0.48f); // Empty space
                }
            });
        }
    }

    private void BuildGradeAcademicTable(ColumnDescriptor column, string schoolYear, string grade, List<AcademicRecordData> records, string schoolName)
    {
        var orderedRecords = records.OrderBy(r => r.Subject).ToList();

        // Grade label
        column.Item().Row(gradeLabelRow =>
        {
            gradeLabelRow.AutoItem().Text($"Grade {grade}").FontSize(7.5f).Bold();
            gradeLabelRow.RelativeItem().PaddingLeft(2).PaddingBottom(1)
                .BorderBottom(0.3f)
                .BorderColor(QuestPdfColors.Black)
                .Text("")
                .FontSize(6);
        });

        // School field
        column.Item().PaddingTop(1).Row(schoolRow =>
        {
            schoolRow.AutoItem().Text("School:").FontSize(6.5f).Bold();
            schoolRow.RelativeItem().PaddingLeft(2).PaddingBottom(1)
                .BorderBottom(0.3f)
                .BorderColor(QuestPdfColors.Black)
                .Text(schoolName)
                .FontSize(6);
        });

        // School Year field
        column.Item().PaddingTop(1).Row(syRow =>
        {
            syRow.AutoItem().Text("School Year:").FontSize(6.5f).Bold();
            syRow.RelativeItem().PaddingLeft(2).PaddingBottom(1)
                .BorderBottom(0.3f)
                .BorderColor(QuestPdfColors.Black)
                .Text(schoolYear)
                .FontSize(6);
        });

        // Academic Records Table
        column.Item().PaddingTop(2).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.2f); // Learning Areas
                columns.ConstantColumn(18);   // Q1
                columns.ConstantColumn(18);   // Q2
                columns.ConstantColumn(18);   // Q3
                columns.ConstantColumn(18);   // Q4
                columns.ConstantColumn(25);   // Final Rating
                columns.RelativeColumn(1.2f);  // Remarks
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(Form137TableHeaderStyle).Text("Learning Areas").FontSize(6).Bold().AlignCenter();
                header.Cell().Element(Form137TableHeaderStyle).Text("1").FontSize(5.5f).Bold().AlignCenter();
                header.Cell().Element(Form137TableHeaderStyle).Text("2").FontSize(5.5f).Bold().AlignCenter();
                header.Cell().Element(Form137TableHeaderStyle).Text("3").FontSize(5.5f).Bold().AlignCenter();
                header.Cell().Element(Form137TableHeaderStyle).Text("4").FontSize(5.5f).Bold().AlignCenter();
                header.Cell().Element(Form137TableHeaderStyle).Text("FINAL RATING").FontSize(6).Bold().AlignCenter();
                header.Cell().Element(Form137TableHeaderStyle).Text("REMARKS").FontSize(6).Bold().AlignCenter();
            });

            // Process subjects and handle MAPEH grouping
            var processedSubjects = new HashSet<string>();
            var mapehSubjects = new List<AcademicRecordData>();
            decimal? mapehQ1 = null, mapehQ2 = null, mapehQ3 = null, mapehQ4 = null;
            decimal? mapehFinal = null;
            string mapehRemarks = "";

            foreach (var record in orderedRecords)
            {
                var subjectLower = record.Subject.ToLower();
                var isMapehComponent = subjectLower.Contains("music") || 
                                      subjectLower.Contains("arts") || 
                                      subjectLower.Contains("physical education") || 
                                      subjectLower.Contains("pe") || 
                                      subjectLower.Contains("health") ||
                                      subjectLower.Contains("mapeh");

                if (isMapehComponent && !processedSubjects.Contains(record.Subject))
                {
                    mapehSubjects.Add(record);
                    processedSubjects.Add(record.Subject);

                    // Parse and accumulate MAPEH grades
                    if (decimal.TryParse(record.Q1 == "-" ? "0" : record.Q1, out var q1))
                        mapehQ1 = (mapehQ1 ?? 0) + q1;
                    if (decimal.TryParse(record.Q2 == "-" ? "0" : record.Q2, out var q2))
                        mapehQ2 = (mapehQ2 ?? 0) + q2;
                    if (decimal.TryParse(record.Q3 == "-" ? "0" : record.Q3, out var q3))
                        mapehQ3 = (mapehQ3 ?? 0) + q3;
                    if (decimal.TryParse(record.Q4 == "-" ? "0" : record.Q4, out var q4))
                        mapehQ4 = (mapehQ4 ?? 0) + q4;
                    if (decimal.TryParse(record.FinalGrade == "-" ? "0" : record.FinalGrade, out var final))
                        mapehFinal = (mapehFinal ?? 0) + final;
                }
                else if (!isMapehComponent && !processedSubjects.Contains(record.Subject))
                {
                    // Regular subject
                    processedSubjects.Add(record.Subject);
                    AddSubjectRow(table, record);
                }
            }

            // Add MAPEH as aggregated row with sub-rows
            if (mapehSubjects.Any())
            {
                var mapehCount = mapehSubjects.Count;
                var mapehQ1Avg = mapehQ1.HasValue && mapehCount > 0 ? mapehQ1.Value / mapehCount : 0;
                var mapehQ2Avg = mapehQ2.HasValue && mapehCount > 0 ? mapehQ2.Value / mapehCount : 0;
                var mapehQ3Avg = mapehQ3.HasValue && mapehCount > 0 ? mapehQ3.Value / mapehCount : 0;
                var mapehQ4Avg = mapehQ4.HasValue && mapehCount > 0 ? mapehQ4.Value / mapehCount : 0;
                var mapehFinalAvg = mapehFinal.HasValue && mapehCount > 0 ? mapehFinal.Value / mapehCount : 0;
                
                // Calculate MAPEH remarks based on final average
                if (mapehFinalAvg >= 90) mapehRemarks = "Outstanding";
                else if (mapehFinalAvg >= 85) mapehRemarks = "Very Satisfactory";
                else if (mapehFinalAvg >= 80) mapehRemarks = "Satisfactory";
                else if (mapehFinalAvg >= 75) mapehRemarks = "Fairly Satisfactory";
                else if (mapehFinalAvg > 0) mapehRemarks = "Did Not Meet Expectations";
                else mapehRemarks = "";

                // MAPEH main row
                table.Cell().Element(Form137TableCellStyle).Text("MAPEH").FontSize(6).Bold();
                table.Cell().Element(Form137TableCellStyle).Text(mapehQ1Avg > 0 ? mapehQ1Avg.ToString("F1") : "").FontSize(6).AlignCenter();
                table.Cell().Element(Form137TableCellStyle).Text(mapehQ2Avg > 0 ? mapehQ2Avg.ToString("F1") : "").FontSize(6).AlignCenter();
                table.Cell().Element(Form137TableCellStyle).Text(mapehQ3Avg > 0 ? mapehQ3Avg.ToString("F1") : "").FontSize(6).AlignCenter();
                table.Cell().Element(Form137TableCellStyle).Text(mapehQ4Avg > 0 ? mapehQ4Avg.ToString("F1") : "").FontSize(6).AlignCenter();
                table.Cell().Element(Form137TableCellStyle).Text(mapehFinalAvg > 0 ? mapehFinalAvg.ToString("F1") : "").FontSize(6).AlignCenter().Bold();
                table.Cell().Element(Form137TableCellStyle).Text(mapehRemarks).FontSize(5.5f).AlignCenter();

                // MAPEH sub-rows (Music, Arts, PE, Health)
                foreach (var mapehSub in mapehSubjects.OrderBy(s => s.Subject))
                {
                    table.Cell().Element(Form137TableCellStyle).PaddingLeft(4).Text(mapehSub.Subject).FontSize(5.5f);
                    table.Cell().Element(Form137TableCellStyle).Text(mapehSub.Q1 == "-" ? "" : mapehSub.Q1).FontSize(5.5f).AlignCenter();
                    table.Cell().Element(Form137TableCellStyle).Text(mapehSub.Q2 == "-" ? "" : mapehSub.Q2).FontSize(5.5f).AlignCenter();
                    table.Cell().Element(Form137TableCellStyle).Text(mapehSub.Q3 == "-" ? "" : mapehSub.Q3).FontSize(5.5f).AlignCenter();
                    table.Cell().Element(Form137TableCellStyle).Text(mapehSub.Q4 == "-" ? "" : mapehSub.Q4).FontSize(5.5f).AlignCenter();
                    table.Cell().Element(Form137TableCellStyle).Text(mapehSub.FinalGrade == "-" ? "" : mapehSub.FinalGrade).FontSize(5.5f).AlignCenter();
                    table.Cell().Element(Form137TableCellStyle).Text(mapehSub.Remarks).FontSize(5).AlignCenter();
                }
            }

            // General Average row - calculate from all subjects including MAPEH average
            var allFinalGrades = new List<decimal>();
            
            // Add non-MAPEH subjects
            foreach (var record in orderedRecords)
            {
                var subjectLower = record.Subject.ToLower();
                var isMapehComponent = subjectLower.Contains("music") || 
                                      subjectLower.Contains("arts") || 
                                      subjectLower.Contains("physical education") || 
                                      subjectLower.Contains("pe") || 
                                      subjectLower.Contains("health") ||
                                      subjectLower.Contains("mapeh");
                
                if (!isMapehComponent && record.FinalGrade != "-" && decimal.TryParse(record.FinalGrade, out var grade))
                {
                    allFinalGrades.Add(grade);
                }
            }
            
            // Add MAPEH average if it exists
            if (mapehSubjects.Any())
            {
                var mapehCount = mapehSubjects.Count;
                var mapehFinalAvg = mapehFinal.HasValue && mapehCount > 0 ? mapehFinal.Value / mapehCount : 0;
                if (mapehFinalAvg > 0)
                {
                    allFinalGrades.Add(mapehFinalAvg);
                }
            }
            
            var generalAverage = allFinalGrades.Any() ? allFinalGrades.Average() : 0;

            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4)
                .Text("General Average").FontSize(6.5f).Bold();
            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4).Text("").FontSize(6);
            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4).Text("").FontSize(6);
            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4).Text("").FontSize(6);
            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4).Text("").FontSize(6);
            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4)
                .Text(generalAverage > 0 ? generalAverage.ToString("F1") : "").FontSize(6.5f).Bold().AlignCenter();
            table.Cell().Element(Form137TableCellStyle).Background(QuestPdfColors.Grey.Lighten4).Text("").FontSize(6);
        });

        // Eligible for Admission field
        column.Item().PaddingTop(2).Row(eligibleRow =>
        {
            eligibleRow.AutoItem().Text("Eligible for Admission to Grade:").FontSize(6.5f).Bold();
            eligibleRow.RelativeItem().PaddingLeft(2).PaddingBottom(1)
                .BorderBottom(0.3f)
                .BorderColor(QuestPdfColors.Black)
                .Text("")
                .FontSize(6);
        });
    }

    private void AddSubjectRow(TableDescriptor table, AcademicRecordData record)
    {
        table.Cell().Element(Form137TableCellStyle).Text(record.Subject).FontSize(6);
        table.Cell().Element(Form137TableCellStyle).Text(record.Q1 == "-" ? "" : record.Q1).FontSize(6).AlignCenter();
        table.Cell().Element(Form137TableCellStyle).Text(record.Q2 == "-" ? "" : record.Q2).FontSize(6).AlignCenter();
        table.Cell().Element(Form137TableCellStyle).Text(record.Q3 == "-" ? "" : record.Q3).FontSize(6).AlignCenter();
        table.Cell().Element(Form137TableCellStyle).Text(record.Q4 == "-" ? "" : record.Q4).FontSize(6).AlignCenter();
        table.Cell().Element(Form137TableCellStyle).Text(record.FinalGrade == "-" ? "" : record.FinalGrade).FontSize(6).AlignCenter().Bold();
        table.Cell().Element(Form137TableCellStyle).Text(record.Remarks).FontSize(5.5f).AlignCenter();
    }

    #region Cell Styles

    private static QuestPdfContainer Form137TableCellStyle(QuestPdfContainer container)
    {
        return container
            .Border(0.3f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(1.5f)
            .PaddingHorizontal(1);
    }

    private static QuestPdfContainer Form137TableHeaderStyle(QuestPdfContainer container)
    {
        return container
            .Background(QuestPdfColors.Grey.Lighten3)
            .Border(0.3f)
            .BorderColor(QuestPdfColors.Black)
            .PaddingVertical(2)
            .PaddingHorizontal(1);
    }

    private static QuestPdfContainer Form137InfoLabelStyle(QuestPdfContainer container)
    {
        return container
            .PaddingVertical(1)
            .PaddingHorizontal(2);
    }

    private static QuestPdfContainer Form137InfoValueStyle(QuestPdfContainer container)
    {
        return container
            .PaddingVertical(1)
            .PaddingHorizontal(2);
    }

    #endregion
}
