using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrightEnroll_DES.Services.QuestPDF;

/// <summary>
/// Generates a payment slip PDF for students with "For Payment" status.
/// This slip is given to parents to present to the cashier for downpayment.
/// </summary>
public class PaymentSlipPdfGenerator
{
    public byte[] GeneratePaymentSlip(PaymentSlipData slipData)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(global::QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Header
                page.Header()
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Column(headerCol =>
                        {
                            headerCol.Item().Text("BRIGHTENROLL").FontSize(24).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            headerCol.Item().Text("Enrollment Management System").FontSize(12).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            headerCol.Item().PaddingTop(5).Text("PAYMENT SLIP").FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            headerCol.Item().PaddingTop(2).Text("For Cashier - Downpayment Processing").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                        });
                    });

                // Content
                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Date
                        column.Item().AlignRight().Text($"Date: {slipData.DateGenerated:MMMM dd, yyyy}").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);

                        column.Item().PaddingTop(15).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(10);

                        // Student Information Section
                        column.Item().Column(studentCol =>
                        {
                            studentCol.Item().Text("STUDENT INFORMATION").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            
                            studentCol.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem(2).Text("Student ID:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                row.RelativeItem(3).Text(slipData.StudentId).FontSize(11).Bold();
                            });

                            studentCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem(2).Text("Student Name:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                row.RelativeItem(3).Text(slipData.StudentName).FontSize(11).Bold();
                            });

                            studentCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem(2).Text("Grade Level:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                row.RelativeItem(3).Text(slipData.GradeLevel).FontSize(11).Bold();
                            });

                            studentCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem(2).Text("School Year:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                row.RelativeItem(3).Text(slipData.SchoolYear).FontSize(11).Bold();
                            });

                            studentCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem(2).Text("Status:").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                row.RelativeItem(3).Text(slipData.Status).FontSize(11).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken2);
                            });
                        });

                        column.Item().PaddingTop(20).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(10);

                        // Instructions Section
                        column.Item().Column(instructionsCol =>
                        {
                            instructionsCol.Item().Text("INSTRUCTIONS FOR CASHIER").FontSize(10).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            
                            instructionsCol.Item().PaddingTop(8).Text("1. Verify the student information above.").FontSize(9);
                            instructionsCol.Item().PaddingTop(3).Text("2. Process the downpayment payment.").FontSize(9);
                            instructionsCol.Item().PaddingTop(3).Text("3. Update the student's payment status in the system.").FontSize(9);
                            instructionsCol.Item().PaddingTop(3).Text("4. Provide the official receipt to the parent/guardian.").FontSize(9);
                        });

                        column.Item().PaddingTop(20).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(10);

                        // Footer Message
                        column.Item().AlignCenter().Column(footerCol =>
                        {
                            footerCol.Item().Text("Please present this slip to the cashier for payment processing.").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                            footerCol.Item().PaddingTop(5).Text("This document is valid for payment processing only.").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                    });
            });
        }).GeneratePdf();
    }

    public class PaymentSlipData
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public string SchoolYear { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DateGenerated { get; set; } = DateTime.Now;
    }
}

