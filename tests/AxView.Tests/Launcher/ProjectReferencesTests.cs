using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class ProjectReferencesTests
{
    [Fact]
    public void FindDirectories_FollowsTheProjectReferencesOfAProject()
    {
        using TempDir dir = new();
        string ui = dir.Write("src/Ui/Ui.csproj", References("../Core/Core.csproj"));
        _ = dir.Write("src/Core/Core.csproj", "<Project />");

        IReadOnlyList<string> directories = ProjectReferences.FindDirectories([ui]);

        Assert.Equal([Path.Combine(dir.Path, "src", "Core")], directories);
    }

    [Fact]
    public void FindDirectories_FollowsTheReferencesOfAReference()
    {
        using TempDir dir = new();
        string ui = dir.Write("src/Ui/Ui.csproj", References("../Core/Core.csproj"));
        _ = dir.Write("src/Core/Core.csproj", References("../Common/Common.csproj"));
        _ = dir.Write("src/Common/Common.csproj", "<Project />");

        IReadOnlyList<string> directories = ProjectReferences.FindDirectories([ui]);

        Assert.Equal([Path.Combine(dir.Path, "src", "Core"), Path.Combine(dir.Path, "src", "Common")], directories);
    }

    [Fact]
    public void FindDirectories_ListsAProjectOnce_AndNotTheProjectsThatItStartsFrom()
    {
        using TempDir dir = new();
        string app = dir.Write("src/App/App.csproj", References("../Ui/Ui.csproj", "../Core/Core.csproj"));
        string ui = dir.Write("src/Ui/Ui.csproj", References("../Core/Core.csproj"));
        _ = dir.Write("src/Core/Core.csproj", References("../Ui/Ui.csproj"));

        IReadOnlyList<string> directories = ProjectReferences.FindDirectories([app, ui]);

        Assert.Equal([Path.Combine(dir.Path, "src", "Core")], directories);
    }

    [Fact]
    public void FindDirectories_SkipsAReferenceWithoutAFile()
    {
        using TempDir dir = new();
        string ui = dir.Write("src/Ui/Ui.csproj", References("../Missing/Missing.csproj"));

        IReadOnlyList<string> directories = ProjectReferences.FindDirectories([ui]);

        Assert.Empty(directories);
    }

    [Fact]
    public void FindDirectories_AcceptsABackslashInTheInclude()
    {
        using TempDir dir = new();
        string ui = dir.Write("src/Ui/Ui.csproj", References(@"..\Core\Core.csproj"));
        _ = dir.Write("src/Core/Core.csproj", "<Project />");

        IReadOnlyList<string> directories = ProjectReferences.FindDirectories([ui]);

        Assert.Equal([Path.Combine(dir.Path, "src", "Core")], directories);
    }

    private static string References(params string[] includes) =>
        $"<Project>{string.Concat(includes.Select(i => $"<ItemGroup><ProjectReference Include='{i}' /></ItemGroup>"))}</Project>";
}
