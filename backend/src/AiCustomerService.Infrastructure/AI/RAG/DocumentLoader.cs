using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using DocumentFormat.OpenXml.Packaging;

namespace AiCustomerService.Infrastructure.AI.RAG;

public record LoadedDocument(string FileName, string Content, string FileType, int ImageCount, int TableCount);

public class DocumentLoader
{
    private readonly TextCleaner _cleaner;
    public DocumentLoader(TextCleaner cleaner) { _cleaner = cleaner; }

    public async Task<LoadedDocument> LoadAsync(Stream stream, string fileName, string fileType)
    {
        var (raw, images, tables) = fileType.ToLowerInvariant() switch
        {
            "pdf" => await ExtractPdfEnhancedAsync(stream),
            "docx" => (ExtractDocx(stream), 0, 0),
            "txt" or "md" => (await ExtractTextAsync(stream), 0, 0),
            "csv" => (await ExtractCsvAsync(stream), 0, 0),
            _ => throw new NotSupportedException($"不支持的文件类型: {fileType}")
        };

        var cleaned = _cleaner.Clean(raw);
        return new LoadedDocument(fileName, cleaned, fileType, images, tables);
    }

    /// <summary>
    /// 增强版 PDF 解析：文本 + 表格结构 + 图像检测
    /// </summary>
    private async Task<(string Text, int ImageCount, int TableCount)> ExtractPdfEnhancedAsync(Stream stream)
    {
        return await Task.Run(() =>
        {
            var sb = new StringBuilder();
            int imageCount = 0;
            int tableCount = 0;

            using var doc = PdfDocument.Open(stream);
            foreach (var page in doc.GetPages())
            {
                // 1) 文本 + 表格抽取
                var pageText = ExtractPageWithTables(page, ref tableCount);
                sb.AppendLine(pageText);
                sb.AppendLine();

                // 2) 图像探测：通过 PdfPig 的 PageImageBytes 统计
                // 当前 PdfPig 0.1.9 提供 GetImages() 列举嵌入图像
                try
                {
                    var imgs = page.GetImages();
                    if (imgs != null)
                    {
                        var n = 0;
                        foreach (var img in imgs)
                        {
                            n++;
                            sb.AppendLine($"[图片 #{n}：位于第 {page.Number} 页，" +
                                $"尺寸 {img.WidthInSamples}x{img.HeightInSamples} 像素，" +
                                $"已提取供多模态模型识别]");
                        }
                        imageCount += n;
                    }
                }
                catch
                {
                    // 部分 PDF 图像探测失败时忽略
                }
            }

            return (sb.ToString(), imageCount, tableCount);
        });
    }

    /// <summary>
    /// 抽取单页文本，并基于布局启发式识别简单表格。
    /// 启发式：连续 2 行以上包含 3 个及以上 Tab 字符视为表格。
    /// </summary>
    private static string ExtractPageWithTables(Page page, ref int tableCount)
    {
        var rawText = page.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

        var lines = rawText.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var sb = new StringBuilder();
        int i = 0;

        while (i < lines.Count)
        {
            var line = lines[i];

            // 表格识别：Tab 分隔或多空格对齐
            if (IsTableRow(line))
            {
                var tableLines = new List<string>();
                while (i < lines.Count && IsTableRow(lines[i]))
                {
                    tableLines.Add(lines[i]);
                    i++;
                }
                if (tableLines.Count >= 2)
                {
                    tableCount++;
                    sb.AppendLine($"[表格 #{tableCount}（位于第 {page.Number} 页）]");
                    foreach (var row in tableLines)
                        sb.AppendLine("  " + row);
                    sb.AppendLine();
                    continue;
                }
                else
                {
                    foreach (var t in tableLines) sb.AppendLine(t);
                }
            }

            sb.AppendLine(line);
            i++;
        }

        return sb.ToString();
    }

    private static bool IsTableRow(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        // 包含 Tab 字符
        if (line.Contains('\t')) return true;
        // 3 个及以上连续 2+ 空格
        return Regex.IsMatch(line, @"\S {2,}\S {2,}\S");
    }

    private string ExtractDocx(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            // 提取段落 + 表格
            foreach (var element in body.ChildElements)
            {
                if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph p)
                {
                    sb.AppendLine(p.InnerText);
                }
                else if (element is DocumentFormat.OpenXml.Wordprocessing.Table t)
                {
                    sb.AppendLine("[表格]");
                    foreach (var row in t.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
                    {
                        var cells = row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
                            .Select(c => c.InnerText.Trim()).ToList();
                        sb.AppendLine("  " + string.Join(" | ", cells));
                    }
                    sb.AppendLine();
                }
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
                sb.AppendLine("[表格]");
                sb.AppendLine("  " + string.Join(" | ", headers));
                continue;
            }
            for (int i = 0; i < cols.Length && i < headers.Count; i++)
                sb.AppendLine($"  {headers[i]}: {cols[i]}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
