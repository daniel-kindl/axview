extern alias host;

using System.Text.Json;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class VisualTreeDumpTests
{
    [AvaloniaFact]
    public void Save_WritesTheTypeNameClassesAndTextOfEachVisual()
    {
        using TempDir dir = new();
        TextBlock text = new() { Name = "lblTitle", Text = "Hello", Classes = { "title" } };
        StackPanel root = new() { Children = { text } };

        JsonElement tree = SaveShown(dir, root);

        Assert.Equal("StackPanel", tree.GetProperty("Type").GetString());
        JsonElement child = Assert.Single(tree.GetProperty("Children").EnumerateArray());
        Assert.Equal("TextBlock", child.GetProperty("Type").GetString());
        Assert.Equal("lblTitle", child.GetProperty("Name").GetString());
        Assert.Equal(["title"], child.GetProperty("Classes").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("Hello", child.GetProperty("Text").GetString());
    }

    [AvaloniaFact]
    public void Save_WritesTheBoundsRelativeToTheRoot()
    {
        using TempDir dir = new();
        Border box = new() { Width = 40, Height = 20, Margin = new Avalonia.Thickness(10, 5, 0, 0) };
        StackPanel root = new() { HorizontalAlignment = HorizontalAlignment.Left, Children = { box } };

        JsonElement child = Assert.Single(SaveShown(dir, root).GetProperty("Children").EnumerateArray());

        Assert.Equal(10, child.GetProperty("X").GetDouble());
        Assert.Equal(5, child.GetProperty("Y").GetDouble());
        Assert.Equal(40, child.GetProperty("Width").GetDouble());
        Assert.Equal(20, child.GetProperty("Height").GetDouble());
    }

    [AvaloniaFact]
    public void Save_WritesIsVisible()
    {
        using TempDir dir = new();
        StackPanel root = new() { Children = { new Border { IsVisible = false } } };

        JsonElement tree = SaveShown(dir, root);

        Assert.True(tree.GetProperty("IsVisible").GetBoolean());
        Assert.False(Assert.Single(tree.GetProperty("Children").EnumerateArray()).GetProperty("IsVisible").GetBoolean());
    }

    [AvaloniaFact]
    public void Save_WritesTheDataContextTypeOnlyWhereItChanges()
    {
        using TempDir dir = new();
        Border inner = new() { DataContext = new FixtureOtherViewModel() };
        StackPanel root = new() { DataContext = new FixtureViewModel(), Children = { new Border(), inner } };

        JsonElement tree = SaveShown(dir, root);

        Assert.Equal(typeof(FixtureViewModel).FullName, tree.GetProperty("DataContext").GetString());
        JsonElement[] children = [.. tree.GetProperty("Children").EnumerateArray()];
        Assert.False(children[0].TryGetProperty("DataContext", out _));
        Assert.Equal(typeof(FixtureOtherViewModel).FullName, children[1].GetProperty("DataContext").GetString());
    }

    [AvaloniaFact]
    public void Save_LeavesOutTheMissingValues()
    {
        using TempDir dir = new();

        JsonElement tree = SaveShown(dir, new Border());

        Assert.False(tree.TryGetProperty("Name", out _));
        Assert.False(tree.TryGetProperty("Text", out _));
        Assert.False(tree.TryGetProperty("DataContext", out _));
        Assert.Empty(tree.GetProperty("Classes").EnumerateArray());
        Assert.Empty(tree.GetProperty("Children").EnumerateArray());
    }

    private static JsonElement SaveShown(TempDir dir, Control root)
    {
        Window window = new() { Width = 300, Height = 200, Content = root };
        window.Show();
        string path = Path.Combine(dir.Path, "tree.json");

        VisualTreeDump.Save(root, path);

        window.Close();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }
}
