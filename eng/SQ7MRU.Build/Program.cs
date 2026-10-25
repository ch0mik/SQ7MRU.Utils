namespace SQ7MRU.Build;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            switch (args)
            {
                case ["checksums", var directory]:
                    await DesktopChecksums.WriteManifestAsync(directory, cancellation.Token);
                    Console.WriteLine("Verified eight desktop packages and wrote SHA256SUMS.");
                    return 0;
                case ["adif-metadata", var source, var target]:
                    await AdifMetadataGenerator.WriteAsync(source, target, cancellation.Token);
                    Console.WriteLine("Reviewed ADIF metadata generated. Inspect the diff before committing.");
                    return 0;
                default:
                    Console.Error.WriteLine("Usage: SQ7MRU.Build checksums <directory> | adif-metadata <reviewed.html> <output.json>");
                    return 2;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.Error.WriteLine("Build tool cancelled.");
            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or FormatException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally { Console.CancelKeyPress -= handler; }
    }
}
