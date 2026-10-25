using StbImageSharp;

namespace SQ7MRU.Utils.Infrastructure;

internal static class CardImageValidator
{
    public static string? Extension(byte[] content)
    {
        if (content.Length is < 14 or > 32 * 1024 * 1024) return null;
        string? extension = null;
        if (content[0] == 0xff && content[1] == 0xd8 && content[^2] == 0xff && content[^1] == 0xd9) extension = ".jpg";
        else if (content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            content.AsSpan(content.Length - 8, 4).SequenceEqual("IEND"u8)) extension = ".png";
        else if ((content.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || content.AsSpan(0, 6).SequenceEqual("GIF89a"u8)) && content[^1] == 0x3b) extension = ".gif";
        if (extension is null) return null;
        try
        {
            // Inspect dimensions before allocating the decoded pixels. This caps decompression bombs.
            using var input = new MemoryStream(content, writable: false);
            var info = ImageInfo.FromStream(input);
            if (info is null || info.Value.Width <= 0 || info.Value.Height <= 0 ||
                info.Value.Width > 8192 || info.Value.Height > 8192 || (long)info.Value.Width * info.Value.Height > 4_000_000) return null;
            var image = ImageResult.FromMemory(content, ColorComponents.RedGreenBlueAlpha);
            return image.Data.Length == (long)image.Width * image.Height * 4 ? extension : null;
        }
        // The decoder exposes invalid-input failures as Exception, not a dedicated format exception.
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException) { return null; }
    }
}
