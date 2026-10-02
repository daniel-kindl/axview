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
}
