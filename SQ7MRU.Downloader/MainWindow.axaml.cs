using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Collections.Specialized;
namespace SQ7MRU.Downloader;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"SQ7MRU Downloader {typeof(MainWindow).Assembly.GetName().Version?.ToString(3)}";
        ResultsList.Items.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0)
                Dispatcher.UIThread.Post(() =>
                {
                    if (ResultsList.ItemCount > 0) ResultsList.ScrollIntoView(0);
                }, DispatcherPriority.Loaded);
        };
    }
    private async void ChooseDirectory(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Output directory", AllowMultiple = false });
        if (folders.Count > 0 && DataContext is MainViewModel vm && folders[0].TryGetLocalPath() is { } path) vm.OutputDirectory = path;
    }
}
