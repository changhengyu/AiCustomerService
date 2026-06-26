using System.Text.RegularExpressions;

namespace AiCustomerService.Infrastructure.AI.RAG;

/// <summary>
/// 文本清洗：去除 HTML、Markdown、控制字符、合并空白行
/// </summary>
public class TextCleaner
{
    private static readonly Regex HtmlRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex ControlRegex = new(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled);
    private static readonly Regex MultipleSpaces = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex MultipleNewlines = new(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex MarkdownLink = new(@"\[([^\]]+)\]\([^)]+\)", RegexOptions.Compiled);
    private static readonly Regex MarkdownImage = new(@"!\[([^\]]*)\]\([^)]+\)", RegexOptions.Compiled);

    public string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 去除 HTML 标签
        text = HtmlRegex.Replace(text, " ");
        // Markdown 图片（不保留）
        text = MarkdownImage.Replace(text, "");
        // Markdown 链接只保留文字
        text = MarkdownLink.Replace(text, "$1");
        // 控制字符
        text = ControlRegex.Replace(text, " ");
        // 多余空格
        text = MultipleSpaces.Replace(text, " ");
        // 多余空行
        text = MultipleNewlines.Replace(text, "\n\n");

        return text.Trim();
    }
}