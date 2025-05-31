using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AccessLensCli;

public class PdfBuilder
{
    public class Page
    {
        public StringBuilder Content { get; } = new();
        public void Text(double x, double y, int size, string text)
        {
            Content.Append($"BT /F1 {size} Tf {x:F2} {y:F2} Td ({Escape(text)}) Tj ET\n");
        }
        public void Line(double x1, double y1, double x2, double y2)
        {
            Content.Append($"{x1:F2} {y1:F2} m {x2:F2} {y2:F2} l S\n");
        }
        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
        public override string ToString() => Content.ToString();
    }

    private readonly List<Page> _pages = new();

    public Page AddPage()
    {
        var p = new Page();
        _pages.Add(p);
        return p;
    }

    public void Save(string path)
    {
        var objects = new List<string>();
        int fontObj = 3 + _pages.Count;
        int contentStart = fontObj + 1;
        for (int i = 0; i < _pages.Count; i++)
        {
            int contentObj = contentStart + i;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObj} 0 R >> >> /Contents {contentObj} 0 R >>");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        foreach (var p in _pages)
        {
            string stream = p.ToString();
            objects.Add($"<< /Length {stream.Length} >>\nstream\n{stream}endstream");
        }

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new List<int>();
        offsets.Add(sb.Length);
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        offsets.Add(sb.Length);
        var kids = string.Join(" ", Enumerable.Range(0, _pages.Count).Select(i => $"{i + 3} 0 R"));
        sb.Append($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>\nendobj\n");
        for (int i = 0; i < _pages.Count; i++)
        {
            offsets.Add(sb.Length);
            sb.Append($"{i + 3} 0 obj\n{objects[i]}\nendobj\n");
        }
        offsets.Add(sb.Length);
        sb.Append($"{fontObj} 0 obj\n{objects[_pages.Count]}\nendobj\n");
        for (int i = 0; i < _pages.Count; i++)
        {
            offsets.Add(sb.Length);
            sb.Append($"{contentStart + i} 0 obj\n{objects[_pages.Count + 1 + i]}\nendobj\n");
        }
        int xrefOffset = sb.Length;
        sb.Append("xref\n0 " + (objects.Count + 2) + "\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var off in offsets)
        {
            sb.Append(off.ToString("D10") + " 00000 n \n");
        }
        sb.Append("trailer\n<< /Size " + (objects.Count + 2) + " /Root 1 0 R >>\nstartxref\n" + xrefOffset + "\n%%EOF");

        File.WriteAllText(path, sb.ToString());
    }
}
