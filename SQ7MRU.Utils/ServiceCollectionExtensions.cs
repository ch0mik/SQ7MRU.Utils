using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSq7MruUtils(this IServiceCollection services,
        Action<DownloadOptions>? configureDownloads = null, Action<ServiceHttpOptions>? configureHttp = null)
    {
        services.AddLogging();
        services.AddOptions<DownloadOptions>().Validate(o => o.MaxConcurrency is >= 1 and <= 32 &&
            o.MaxConcurrencyByService is not null && o.MaxConcurrencyByService.All(x => Enum.IsDefined(x.Key) && x.Value is >= 1 and <= 32),
            "Concurrency settings must be between 1 and 32 for supported services.");
        services.AddOptions<ServiceHttpOptions>().Validate(o => o.MaxRetries is >= 0 and <= 20 &&
            o.RequestTimeout > TimeSpan.Zero && o.OperationTimeout >= o.RequestTimeout &&
            o.EqslCardInterval >= TimeSpan.FromSeconds(1) && o.BaseDelay > TimeSpan.Zero && o.MaxDelay >= o.BaseDelay && o.MinimumRequestInterval >= TimeSpan.Zero,
            "HTTP timeouts, delays and retry count are invalid.");
        if (configureDownloads is not null) services.Configure(configureDownloads);
        if (configureHttp is not null) services.Configure(configureHttp);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ServiceRequestScheduler>();
        services.TryAddSingleton<IServiceHttpSessionFactory, ServiceHttpSessionFactory>();
        services.TryAddSingleton<IArtifactStore, ArtifactStore>();
        services.TryAddTransient<IDownloadService, DownloadService>();
        services.TryAddTransient<IEqslAccountService, EqslAccountService>();
        services.TryAddTransient<IAdifUploader, AdifUploader>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IDownloadSessionFactory, EqslSessionFactory>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IDownloadSessionFactory, HrdSessionFactory>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IDownloadSessionFactory, EpcSessionFactory>());
        return services;
    }
}
