using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SQ7MRU.Utils;
using SQ7MRU.Utils.Infrastructure;
namespace SQ7MRU.Downloader;
public partial class App : Avalonia.Application
{
    private ServiceProvider? provider;
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection().AddSq7MruUtils();
            if (Environment.GetEnvironmentVariable("SQ7MRU_TECHNICAL_DIRECTORY") is { } technicalDirectory)
                services.Configure<ArtifactStorageOptions>(options => options.TechnicalDirectoryName = technicalDirectory);
            services.AddSingleton<MainViewModel>();
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            var vm = provider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            desktop.MainWindow.Closing += (_, e) =>
            {
                if (vm.IsBusy)
                {
                    e.Cancel = true;
                    vm.CancelCommand.Execute(null);
                    vm.Status = "Cancelling the operation. Close the window once it has finished.";
                }
            };
            desktop.Exit += (_, _) => provider.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
