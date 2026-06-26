namespace AiCustomerService.Infrastructure.AI.RAG;

public record TextChunk(string Content, int StartIndex, int EndIndex, int ChunkIndex);

public class TextSplitter
{
    /// <summary>
    /// 按段落 + 滑动窗口切分文本块
    /// 优先按段落切分，长段落按句子滑窗切
    /// </summary>
    public List<TextChunk> Split(string text, int chunkSize = 500, int overlap = 80)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<TextChunk>();
        text = text.Trim();

        var chunks = new List<TextChunk>();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var buffer = new System.Text.StringBuilder();
        int chunkIndex = 0;
        int startIndex = 0;

        foreach (var para in paragraphs)
        {
            var p = para.Trim();
            if (string.IsNullOrEmpty(p)) continue;

            // 段落本身超过 chunkSize，按句子切分
            if (p.Length > chunkSize)
            {
                FlushBuffer(buffer, overlap, chunkSize, startIndex, chunks, ref chunkIndex);
                foreach (var sc in SplitLongParagraph(p, chunkSize, overlap))
                {
                    chunks.Add(new TextChunk(sc.Content, sc.StartIndex, sc.EndIndex, chunkIndex++));
                }
                startIndex += p.Length + 2;
                continue;
            }

            if (buffer.Length + p.Length + 2 > chunkSize)
            {
                FlushBuffer(buffer, overlap, chunkSize, startIndex, chunks, ref chunkIndex);
                startIndex += buffer.Length;
            }
            buffer.Append(p).Append("\n\n");
        }

        FlushBuffer(buffer, overlap, chunkSize, startIndex, chunks, ref chunkIndex);
        return chunks;
    }

    private void FlushBuffer(
        System.Text.StringBuilder buffer, int overlap, int chunkSize,
        int startIndex, List<TextChunk> chunks, ref int chunkIndex)
    {
        if (buffer.Length == 0) return;
        var content = buffer.ToString().TrimEnd();
        if (string.IsNullOrWhiteSpace(content)) return;
        chunks.Add(new TextChunk(content, startIndex, startIndex + content.Length, chunkIndex++));

        // 保留 overlap 字符作为下一块的上下文
        if (content.Length > overlap)
        {
            buffer.Clear();
            buffer.Append(content[^overlap..]).Append("\n\n");
        }
        else
        {
            buffer.Clear();
        }
    }

    private List<TextChunk> SplitLongParagraph(string text, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        int idx = 0;
        int chunkIndex = 0;
        while (idx < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - idx);
            var content = text.Substring(idx, len);
            chunks.Add(new TextChunk(content, idx, idx + len, chunkIndex++));
            idx += chunkSize - overlap;
        }
        return chunks;
    }
}