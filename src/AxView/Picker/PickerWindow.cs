using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using AxView.Building;
using AxView.Hosting;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Picker;

/// <summary>Lets the user open an AXAML file or a recent one. Hides while the preview runs.</summary>
internal sealed class PickerWindow : Window
{
    private readonly RecentFiles recent;
    private readonly TextBlock recentHeader;
    private readonly ListBox recentList;
    private readonly TextBlock noRecentFiles;
    private readonly TextBlock message;

    /// <summary>The hint that the window shows in place of an empty recent file list.</summary>
    public const string NoRecentFilesText = "No recent files yet. Open an AXAML file to start.";

    public PickerWindow(RecentFiles recent)
    {
        this.recent = recent;
        Title = "AxView";
        Icon = AppIcon.Get();
        Width = 640;
        Height = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Button openButton = new() { Content = "Open AXAML file...", HorizontalAlignment = HorizontalAlignment.Left };
        openButton.Click += async (_, _) => await PickAsync();

        Button aboutButton = new() { Content = "About", HorizontalAlignment = HorizontalAlignment.Left };
        aboutButton.Click += async (_, _) => await new AboutDialog(AboutInfo.Read(typeof(AboutInfo).Assembly)).ShowDialog(this);

        recentList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<string>((path, _) => new RecentFileItem(path ?? string.Empty)),
        };
        recentList.DoubleTapped += async (_, _) => await OpenSelectedAsync();
        recentList.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await OpenSelectedAsync();
            }
        };

        noRecentFiles = new TextBlock { Text = NoRecentFilesText, Opacity = 0.7 };
        DockPanel.SetDock(noRecentFiles, Dock.Top);

        message = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.IndianRed, IsVisible = false };

        TextBlock title = new() { Text = "AxView", FontSize = 28, FontWeight = FontWeight.SemiBold };
        TextBlock subtitle = new() { Text = "Render any Avalonia AXAML file. It reloads when you save.", Opacity = 0.7 };
        recentHeader = new TextBlock { Text = "Recent (double-click to open)", FontWeight = FontWeight.SemiBold };
        StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 8, Children = { openButton, aboutButton } };

        DockPanel layout = new() { Margin = new Thickness(24) };
        foreach (Control top in new Control[] { title, subtitle, buttons, message, recentHeader })
        {
            top.Margin = new Thickness(0, 0, 0, 12);
            DockPanel.SetDock(top, Dock.Top);
            layout.Children.Add(top);
        }

        layout.Children.Add(noRecentFiles);
        layout.Children.Add(recentList);
        Content = layout;
        ShowRecentFiles();
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
            HostSession session = HostSession.CreateDefault(recent.SettingsFile, new BuildWindowDisplay());
            int exitCode = await session.RunAsync(path, outputDir: null, snapshotPath: null, recent.Add);
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

        ShowRecentFiles();
        Show();
    }

    private void ShowRecentFiles()
    {
        recentList.ItemsSource = recent.Files.ToList();

        // The first file is the last open file. Enter opens it again.
        recentList.SelectedIndex = 0;
        bool hasFiles = recent.Files.Count > 0;
        recentHeader.IsVisible = hasFiles;
        recentList.IsVisible = hasFiles;
        noRecentFiles.IsVisible = !hasFiles;
    }

    private void ShowMessage(string text)
    {
        message.Text = text;
        message.IsVisible = true;
    }
}
