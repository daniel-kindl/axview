using AxView.Protocol;

namespace AxView.Tests.Launcher;

public sealed class ProgramTests
{
    [Fact]
    public void Main_RejectsASnapshotWithASnapshotFolder()
    {
        int exitCode = Program.Main(["Main.axaml", "--snapshot", "shot.png", "--snapshot-all", "shots"]);

        Assert.Equal((int)ExitReason.BadArguments, exitCode);
    }

    [Fact]
    public void Main_RejectsAViewModelWithoutASnapshot()
    {
        int exitCode = Program.Main(["Main.axaml", "--view-model", "Ui.MainViewModel"]);

        Assert.Equal((int)ExitReason.BadArguments, exitCode);
    }

    [Theory]
    [InlineData("--size", "Phone")]
    [InlineData("--theme", "Dark")]
    [InlineData("--tree", "tree.json")]
    [InlineData("--report", "report.json")]
    public void Main_RejectsARenderFlagWithoutASnapshot(string key, string value)
    {
        int exitCode = Program.Main(["Main.axaml", key, value]);

        Assert.Equal((int)ExitReason.BadArguments, exitCode);
    }

    [Fact]
    public void Main_RejectsFitContentWithoutASnapshot()
    {
        int exitCode = Program.Main(["Main.axaml", "--fit-content"]);

        Assert.Equal((int)ExitReason.BadArguments, exitCode);
    }

    [Theory]
    [InlineData("--size", "Huge")]
    [InlineData("--theme", "Blue")]
    public void Main_RejectsARenderFlagWithABadValue(string key, string value)
    {
        int exitCode = Program.Main(["Main.axaml", "--snapshot", "shot.png", key, value]);

        Assert.Equal((int)ExitReason.BadArguments, exitCode);
    }
}
