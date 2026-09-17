using RengaMcp.Infrastructure;

namespace RengaMcp.Tests;

public sealed class RunningObjectTableTests
{
    [Fact]
    public void ParsesRengaMoniker()
    {
        var result = RunningObjectTable.Parse("!Renga Application, ver: 2.46, pid: 1234");

        Assert.Equal("2.46", result.ApiVersion);
        Assert.Equal(1234, result.ProcessId);
    }
}
