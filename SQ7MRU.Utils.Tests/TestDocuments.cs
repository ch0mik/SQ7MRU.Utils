using System.Globalization;
using System.Text;

namespace SQ7MRU.Utils.Tests;

internal static class TestDocuments
{
    internal static byte[] Pdf
    {
        get
        {
            var builder = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            foreach (var value in new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Contents 4 0 R >>",
                "<< /Length 0 >>\nstream\n\nendstream"
            })
            {
                offsets.Add(builder.Length);
                builder.Append(CultureInfo.InvariantCulture, $"{offsets.Count} 0 obj\n{value}\nendobj\n");
            }
            var xref = builder.Length;
            builder.Append("xref\n0 5\n0000000000 65535 f \n");
            foreach (var offset in offsets) builder.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
            builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
