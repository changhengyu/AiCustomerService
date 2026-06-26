using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;

namespace AiCustomerService.Infrastructure.AI.RAG;

public record LoadedDocument(string FileName, string Content, string FileType);

public class DocumentLoader
{
    private readonly TextCleaner _cleaner;

    public DocumentLoader(TextCleaner cleaner)
    {
        _cleaner = cleaner;
    }

    public async Task<LoadedDocument> LoadAsync(Stream stream, string fileName, string fileType)
    {
        var raw = fileType.ToLowerInvariant() switch
        {
            "pdf" => await ExtractPdfAsync(stream),
            "docx" => ExtractDocx(stream),
            "txt" or "md" => await ExtractTextAsync(stream),
            "csv" => await ExtractCsvAsync(stream),
            _ => throw new NotSupportedException($"不支持的文件类型: {fileType}")
        };

        var cleaned = _cleaner.Clean(raw);
        return new LoadedDocument(fileName, cleaned, fileType);
    }

    private async Task<string> ExtractPdfAsync(Stream stream)
    {
        return await Task.Run(() =>
        {
            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(stream);
            foreach (var page in doc.GetPages())
            {
                var text = page.Text ?? string.Empty;
                sb.AppendLine(text);
                sb.AppendLine();
            }
            return sb.ToString();
        });
    }

    private string ExtractDocx(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                sb.AppendLine(para.InnerText);
            }
        }
        return sb.ToString();
    }

    private async Task<string> ExtractTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private async Task<string> ExtractCsvAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var sb = new StringBuilder();
        string? line;
        bool isHeader = true;
        var headers = new List<string>();
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var cols = line.Split(',');
            if (isHeader)
            {
                headers.AddRange(cols);
                isHeader = false;
                continue;
            }
            for (int i = 0; i < cols.Length && i < headers.Count; i++)
                sb.AppendLine($"{headers[i]}: {cols[i]}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}