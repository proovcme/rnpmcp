using RengaMcp.Adapters;
using RengaMcp.Models;

namespace RengaMcp.Tests;

public sealed class MockRengaAdapterTests
{
    [Fact]
    public async Task RequiresConnectionBeforeReadingProject()
    {
        using var adapter = new MockRengaAdapter();
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.GetProjectInfoAsync());
    }

    [Fact]
    public async Task ConnectsAndReturnsProject()
    {
        using var adapter = new MockRengaAdapter();
        var connection = await adapter.ConnectAsync(null);
        var project = await adapter.GetProjectInfoAsync();

        Assert.True(connection.Connected);
        Assert.Equal("Mock BIM project", project.Name);
    }

    [Fact]
    public async Task QueryIsFilteredAndPaginated()
    {
        using var adapter = new MockRengaAdapter();
        await adapter.ConnectAsync(null);

        var page = await adapter.QueryObjectsAsync(new ObjectQuery(null, "сервер", 0, 10));

        var item = Assert.Single(page.Items);
        Assert.Equal("Серверная", item.Name);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task ReadsObjectDetails()
    {
        using var adapter = new MockRengaAdapter();
        await adapter.ConnectAsync(null);

        var details = await adapter.GetObjectAsync(
            "{11111111-1111-1111-1111-111111111111}",
            includeParameters: true,
            includeProperties: true,
            maxValues: 100);

        Assert.Single(details.Parameters!);
        Assert.Single(details.Properties!);
    }

    [Fact]
    public async Task CreationPreviewDoesNotCommit()
    {
        using var adapter = new MockRengaAdapter();
        await adapter.ConnectAsync(null);

        var result = await adapter.CreateObjectAsync(new CreateObjectRequest(
            CreationCatalog.Types[0].TypeId,
            null,
            null,
            null,
            Preview: true));

        Assert.False(result.Committed);
        Assert.False(result.UndoRecorded);
    }

    [Fact]
    public async Task CreationCommitIsUndoable()
    {
        using var adapter = new MockRengaAdapter();
        await adapter.ConnectAsync(null);

        var result = await adapter.CreateObjectAsync(new CreateObjectRequest(
            CreationCatalog.Types[0].TypeId,
            null,
            null,
            null,
            Preview: false));

        Assert.True(result.Committed);
        Assert.True(result.UndoRecorded);
    }

    [Fact]
    public async Task ParameterPreviewDoesNotCommit()
    {
        using var adapter = new MockRengaAdapter();
        await adapter.ConnectAsync(null);

        var result = await adapter.SetParameterAsync(
            "{11111111-1111-1111-1111-111111111111}",
            "{10000000-0000-0000-0000-000000000001}",
            "new",
            preview: true);

        Assert.False(result.Committed);
        Assert.Equal("new", result.NewValue);
    }
}
