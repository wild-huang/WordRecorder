using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WordRecorder.Models;
using DocDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using PdfDocument = QuestPDF.Fluent.Document;

namespace WordRecorder.Services;

public class ExportService
{
    public async Task ExportToTxtAsync(IEnumerable<Word> words, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("生词本");
        await writer.WriteLineAsync("======");
        await writer.WriteLineAsync();
        foreach (var word in words)
        {
            await writer.WriteLineAsync($"{word.Term}  {word.Phonetic}");
            await writer.WriteLineAsync($"  {word.Translation}");
            await writer.WriteLineAsync();
        }
    }

    public async Task ExportToXlsxAsync(IEnumerable<Word> words, string filePath)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("生词本");

            worksheet.Cell(1, 1).Value = "单词";
            worksheet.Cell(1, 2).Value = "音标";
            worksheet.Cell(1, 3).Value = "释义";
            worksheet.Cell(1, 4).Value = "添加时间";

            var headerRange = worksheet.Range("A1:D1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var word in words)
            {
                worksheet.Cell(row, 1).Value = word.Term;
                worksheet.Cell(row, 2).Value = word.Phonetic;
                worksheet.Cell(row, 3).Value = word.Translation;
                worksheet.Cell(row, 4).Value = word.AddedTime.ToString("yyyy-MM-dd HH:mm");
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        });
    }

    public async Task ExportToDocxAsync(IEnumerable<Word> words, string filePath)
    {
        await Task.Run(() =>
        {
            using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocDocument();
            var body = mainPart.Document.AppendChild(new Body());

            var titlePara = body.AppendChild(new Paragraph());
            var titleRun = titlePara.AppendChild(new Run());
            titleRun.AppendChild(new Text("生词本"));
            titleRun.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "36" });
            titlePara.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });

            body.AppendChild(new Paragraph());

            foreach (var word in words)
            {
                var wordPara = body.AppendChild(new Paragraph());
                var wordRun = wordPara.AppendChild(new Run());
                wordRun.AppendChild(new Text($"{word.Term}  {word.Phonetic}"));
                wordRun.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "28" });

                var transPara = body.AppendChild(new Paragraph());
                var transRun = transPara.AppendChild(new Run());
                transRun.AppendChild(new Text($"    {word.Translation}"));
                transRun.RunProperties = new RunProperties(new FontSize() { Val = "24" });

                body.AppendChild(new Paragraph());
            }

            mainPart.Document.Save();
        });
    }

    public async Task ExportToPdfAsync(IEnumerable<Word> words, string filePath)
    {
        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header()
                        .Text("生词本")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                    page.Content().Column(col =>
                    {
                        foreach (var word in words)
                        {
                            col.Item().Row(row =>
                            {
                                row.AutoItem()
                                    .Text($"{word.Term}  {word.Phonetic}")
                                    .Bold().FontSize(14);

                                row.AutoItem()
                                    .PaddingLeft(10)
                                    .Text(word.Translation)
                                    .FontSize(12);
                            });

                            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("第 ");
                            x.CurrentPageNumber();
                            x.Span(" 页");
                        });
                });
            }).GeneratePdf(filePath);
        });
    }

    public async Task ExportToPngAsync(IEnumerable<Word> words, string filePath)
    {
        await Task.Run(() =>
        {
            int lineHeight = 30;
            int padding = 40;
            int width = 800;
            int height = padding * 2 + (words.Count() + 2) * lineHeight;

            using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
            var canvas = surface.Canvas;

            canvas.Clear(SkiaSharp.SKColors.White);

            using var titlePaint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.Black,
                TextSize = 28,
                IsAntialias = true,
                Typeface = SkiaSharp.SKTypeface.FromFamilyName("Microsoft YaHei", SkiaSharp.SKFontStyle.Bold)
            };

            using var wordPaint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.DarkBlue,
                TextSize = 18,
                IsAntialias = true,
                Typeface = SkiaSharp.SKTypeface.FromFamilyName("Microsoft YaHei", SkiaSharp.SKFontStyle.Bold)
            };

            using var transPaint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.Gray,
                TextSize = 16,
                IsAntialias = true,
                Typeface = SkiaSharp.SKTypeface.FromFamilyName("Microsoft YaHei")
            };

            float y = padding + 28;
            canvas.DrawText("生词本", padding, y, titlePaint);
            y += lineHeight * 2;

            foreach (var word in words)
            {
                canvas.DrawText($"{word.Term}  {word.Phonetic}", padding, y, wordPaint);
                y += lineHeight;
                canvas.DrawText($"    {word.Translation}", padding, y, transPaint);
                y += lineHeight;
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        });
    }
}
