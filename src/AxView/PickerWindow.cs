using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace AxView;

/// <summary>Lets the user open an AXAML file or a recent one. Hides while the preview runs.</summary>
internal sealed class PickerWindow : Window
{
    private readonly RecentFiles recent;
    private readonly ListBox recentList;
    private readonly TextBlock message;

    public PickerWindow(RecentFiles recent)
    {
        this.recent = recent;
        Title = "AxView";
        Width = 640;
        Height = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Button openButton = new() { Content = "Open AXAML file...", HorizontalAlignment = HorizontalAlignment.Left };
        openButton.Click += async (_, _) => await PickAsync();

        recentList = new ListBox { ItemsSource = recent.Files };
        recentList.DoubleTapped += async (_, _) => await OpenSelectedAsync();
        recentList.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await OpenSelectedAsync();
            }
        };

        message = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.IndianRed, IsVisible = false };

        TextBlock title = new() { Text = "AxView", FontSize = 28, FontWeight = FontWeight.SemiBold };
        TextBlock subtitle = new() { Text = "Render any Avalonia AXAML file. It reloads when you save.", Opacity = 0.7 };
        TextBlock recentHeader = new() { Text = "Recent (double-click to open)", FontWeight = FontWeight.SemiBold };

        DockPanel layout = new() { Margin = new Thickness(24) };
        foreach (Control top in new Control[] { title, subtitle, openButton, message, recentHeader })
        {
            top.Margin = new Thickness(0, 0, 0, 12);
            DockPanel.SetDock(top, Dock.Top);
            layout.Children.Add(top);
        }

        layout.Children.Add(recentList);
        Content = layout;
    }

    private async Task PickAsync()
    {
        IReadOnlyList<IStorageFile> picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open AXAML file",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("AXAML") { Patterns = ["*.axaml"] }],
        });
        string? path = picked.Count == 1 ? picked[0].TryGetLocalPath() : null;
        if (path is not null)
        {
            await PreviewAsync(path);
        }
    }

    private async Task OpenSelectedAsync()
    {
        if (recentList.SelectedItem is string path)
        {
            await PreviewAsync(path);
        }
    }

    private async Task PreviewAsync(string path)
    {
        message.IsVisible = false;
        Hide();
        try
        {
            int exitCode = await HostSession.CreateDefault().RunAsync(path, outputDir: null, snapshotPath: null, recent.Add);
            if (exitCode == 0)
            {
                Close();
                return;
            }

            ShowMessage($"The preview stopped with exit code {exitCode}. See the console output.");
        }
        catch (PreviewSetupException ex)
        {
            ShowMessage(ex.Message);
        }

        recentList.ItemsSource = recent.Files.ToList();
        Show();
    }

    private void ShowMessage(string text)
    {
        message.Text = text;
        message.IsVisible = true;
    }
}
