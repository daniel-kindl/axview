using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private const double HeaderIconSize = 48;

    private readonly RecentFiles recent;
    private readonly TextBlock recentHeader;
    private readonly ListBox recentList;
    private readonly TextBlock noRecentFiles;
    private readonly TextBlock message;
    private readonly Border messageCard;

    /// <summary>The hint that the window shows in place of an empty recent file list.</summary>
    public const string NoRecentFilesText = "No recent files yet. Open an AXAML file to start.";

    /// <summary>The line below the app name.</summary>
    public const string SubtitleText = "Render any Avalonia AXAML file. It reloads when you save.";

    public PickerWindow(RecentFiles recent)
    {
        this.recent = recent;
        Title = "AxView";
        Icon = AppIcon.Get();
        Width = 640;
        Height = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Resources.MergedDictionaries.Add(ChromeStyle.CreateResources());

        // The accent class gives the primary button of the Fluent theme.
        Button openButton = new() { Content = "Open AXAML file...", HorizontalAlignment = HorizontalAlignment.Left, Classes = { "accent" } };
        openButton.Click += async (_, _) => await PickAsync();

        Button aboutButton = ChromeControls.CreateIconButton(ChromeIcon.About, "About AxView");
        aboutButton.Click += async (_, _) => await new AboutDialog(AboutInfo.Read(typeof(AboutInfo).Assembly)).ShowDialog(this);

        recentList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<string>((path, _) => new RecentFileItem(path ?? string.Empty)),
        };
        // A long folder trims at the start. It does not scroll the list to the side.
        ScrollViewer.SetHorizontalScrollBarVisibility(recentList, ScrollBarVisibility.Disabled);
        recentList.DoubleTapped += async (_, _) => await OpenSelectedAsync();
        recentList.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                await OpenSelectedAsync();
            }
        };

        noRecentFiles = new TextBlock { Text = NoRecentFilesText };
        ChromeStyle.SetBrush(noRecentFiles, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        DockPanel.SetDock(noRecentFiles, Dock.Top);

        message = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        messageCard = CreateMessageCard(message);
        recentHeader = new TextBlock { Text = "Recent (double-click to open)", FontWeight = FontWeight.SemiBold };

        DockPanel layout = new() { Margin = new Thickness(ChromeStyle.SpacingExtraLarge) };
        foreach (Control top in new Control[] { CreateHeader(aboutButton), openButton, messageCard, recentHeader })
        {
            top.Margin = new Thickness(0, 0, 0, ChromeStyle.SpacingLarge);
            DockPanel.SetDock(top, Dock.Top);
            layout.Children.Add(top);
        }

        layout.Children.Add(noRecentFiles);
        layout.Children.Add(recentList);
        Content = layout;
        ShowRecentFiles();
    }

    private static DockPanel CreateHeader(Button aboutButton)
    {
        Control face = AppIcon.CreateFace(HeaderIconSize);
        face.Margin = new Thickness(0, 0, ChromeStyle.SpacingLarge, 0);
        TextBlock title = new() { Text = "AxView", FontSize = ChromeStyle.FontSizeTitle, FontWeight = FontWeight.SemiBold };
        TextBlock subtitle = new() { Text = SubtitleText };
        ChromeStyle.SetBrush(subtitle, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        aboutButton.VerticalAlignment = VerticalAlignment.Top;
        DockPanel.SetDock(aboutButton, Dock.Right);
        DockPanel.SetDock(face, Dock.Left);
        return new DockPanel
        {
            Children =
            {
                aboutButton,
                face,
                new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { title, subtitle } },
            },
        };
    }

    private static Border CreateMessageCard(TextBlock message)
    {
        PathIcon icon = ChromeControls.CreateIcon(ChromeIcon.Error);
        icon.Margin = new Thickness(0, 0, ChromeStyle.SpacingMedium, 0);
        icon.VerticalAlignment = VerticalAlignment.Top;
        ChromeStyle.SetBrush(icon, PathIcon.ForegroundProperty, ChromeStyle.ErrorBrushKey);
        DockPanel.SetDock(icon, Dock.Left);
        Border card = new()
        {
            IsVisible = false,
            Padding = new Thickness(ChromeStyle.SpacingLarge),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ChromeStyle.CornerRadius),
            Child = new DockPanel { Children = { icon, message } },
        };
        ChromeStyle.SetBrush(card, Border.BackgroundProperty, ChromeStyle.CardBrushKey);
        ChromeStyle.SetBrush(card, Border.BorderBrushProperty, ChromeStyle.ErrorBrushKey);
        return card;
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
        messageCard.IsVisible = false;
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

    /// <summary>Shows an error message, for example when the preview stops with an error.</summary>
    /// <param name="text">The message.</param>
    public void ShowMessage(string text)
    {
        message.Text = text;
        messageCard.IsVisible = true;
    }
}
