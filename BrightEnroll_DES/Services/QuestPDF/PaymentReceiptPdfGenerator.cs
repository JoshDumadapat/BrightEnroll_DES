using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrightEnroll_DES.Services.QuestPDF;

public class PaymentReceiptPdfGenerator
{
    public byte[] GeneratePaymentReceipt(ReceiptData receiptData)
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
                            headerCol.Item().PaddingTop(5).Text("Official Receipt").FontSize(10).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                        });
                    });

                // Content
                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Receipt Number and Date
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Official Receipt Number").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                col.Item().PaddingTop(2).Text(receiptData.OrNumber).FontSize(14).Bold().FontColor(global::QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                            
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("Date").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                col.Item().PaddingTop(2).Text(receiptData.PaymentDate.ToString("MMMM dd, yyyy")).FontSize(11).Bold();
                                col.Item().Text(receiptData.PaymentDate.ToString("hh:mm tt")).FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                        });

                        column.Item().PaddingTop(15).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(10);

                        // Student Information
                        column.Item().Column(studentCol =>
                        {
                            studentCol.Item().Text("Student Information").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                            studentCol.Item().PaddingTop(3).Text(receiptData.StudentName).FontSize(11).Bold();
                            studentCol.Item().Text($"Student ID: {receiptData.StudentId}").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            studentCol.Item().Text($"Grade Level: {receiptData.GradeLevel}").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                        });

                        column.Item().PaddingTop(15).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(10);

                        // Payment Details
                        column.Item().Column(paymentCol =>
                        {
                            paymentCol.Item().Text("Payment Details").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                            paymentCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text("Payment Amount:").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"₱{receiptData.PaymentAmount:N2}").FontSize(9).Bold();
                            });
                            paymentCol.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem().Text("Payment Method:").FontSize(9);
                                row.RelativeItem().AlignRight().Text(receiptData.PaymentMethod).FontSize(9).Bold();
                            });
                        });

                        column.Item().PaddingTop(15).BorderTop(2).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Medium).PaddingTop(10);

                        // Summary of Charges
                        column.Item().Column(summaryCol =>
                        {
                            summaryCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Total Assessment:").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"₱{receiptData.TotalFee:N2}").FontSize(9).Bold();
                            });
                            summaryCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text("Previous Payments:").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"₱{receiptData.PreviousPaid:N2}").FontSize(9).Bold();
                            });
                            summaryCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text("This Payment:").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"₱{receiptData.PaymentAmount:N2}").FontSize(9).Bold();
                            });
                            
                            summaryCol.Item().PaddingTop(10).BorderTop(1).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(5);
                            
                            summaryCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Total Paid:").FontSize(11).Bold();
                                row.RelativeItem().AlignRight().Text($"₱{receiptData.TotalPaid:N2}").FontSize(11).Bold();
                            });
                            
                            summaryCol.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text("Remaining Balance:").FontSize(11).Bold();
                                row.RelativeItem().AlignRight().Text($"₱{receiptData.Balance:N2}").FontSize(11).Bold()
                                    .FontColor(receiptData.Balance > 0 ? global::QuestPDF.Helpers.Colors.Red.Darken2 : global::QuestPDF.Helpers.Colors.Green.Darken2);
                            });
                        });

                        // Footer Message
                        column.Item().PaddingTop(20).BorderTop(2).BorderColor(global::QuestPDF.Helpers.Colors.Grey.Medium).PaddingTop(15)
                            .AlignCenter().Column(footerCol =>
                            {
                                footerCol.Item().Text("Thank you for your payment!").FontSize(9).FontColor(global::QuestPDF.Helpers.Colors.Grey.Medium);
                                footerCol.Item().PaddingTop(3).Text("This is an official receipt. Please keep this for your records.").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                                footerCol.Item().PaddingTop(10).Text($"Processed by: {receiptData.ProcessedBy}").FontSize(8).FontColor(global::QuestPDF.Helpers.Colors.Grey.Darken1);
                            });
                    });
            });
        }).GeneratePdf();
    }

    public class ReceiptData
    {
        public string OrNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalFee { get; set; }
        public decimal PreviousPaid { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public string ProcessedBy { get; set; } = string.Empty;
    }
}

