using Microsoft.Extensions.DependencyInjection;
using SQ7MRU.Utils;

if (args.Length < 3 || !Enum.TryParse<QslService>(args[0], true, out var service))
{
    Console.WriteLine("Usage: SampleApp <Eqsl|HrdLog|Epc> <login> <output-directory>");
    Console.WriteLine("Set SQ7MRU_PASSWORD for services requiring authentication.");
    return 2;
}
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
await using var provider = new ServiceCollection().AddSq7MruUtils().BuildServiceProvider();
try
{
    var result = await provider.GetRequiredService<IDownloadService>().DownloadAsync(
        new(service, new(args[1], Environment.GetEnvironmentVariable("SQ7MRU_PASSWORD")), args[2]),
        new Progress<DownloadProgress>(p => Console.WriteLine($"{p.Stage}: {p.Completed}/{p.Total} {p.Message}")), cancellation.Token);
    Console.WriteLine($"Downloaded: {result.Downloaded}; skipped: {result.Skipped}; failed: {result.Failed}");
    foreach (var item in result.Items.Where(x => x.Status == ItemStatus.Failed)) Console.WriteLine($"{item.Name}: {item.Failure} - {item.Message}");
    return result.Failed > 0 ? 1 : 0;
}
catch (OperationCanceledException) { Console.WriteLine("Cancelled."); return 130; }
catch (ServiceException ex) { Console.Error.WriteLine($"{ex.Kind}: {ex.Message}"); return 1; }
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or FormatException)
{ Console.Error.WriteLine("Cannot complete the operation. Check configuration, output directory and service response format."); return 1; }
