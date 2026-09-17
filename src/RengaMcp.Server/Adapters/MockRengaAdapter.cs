using RengaMcp.Models;

namespace RengaMcp.Adapters;

public sealed class MockRengaAdapter : IRengaAdapter
{
    private static readonly RengaInstanceInfo MockInstance =
        new(4242, "2.46", "!Renga Application, ver: 2.46, pid: 4242");

    private static readonly IReadOnlyList<RengaObjectSummary> Objects =
    [
        new(1, "{11111111-1111-1111-1111-111111111111}", "{C3CE17FF-6F28-411F-B18D-74FE957B2BA8}", "Этаж 1", false),
        new(2, "{22222222-2222-2222-2222-222222222222}", "{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}", "Наружная стена", false),
        new(3, "{33333333-3333-3333-3333-333333333333}", "{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}", "Серверная", false)
    ];

    private bool _connected;

    public Task<RengaStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new RengaStatus(
            "mock",
            true,
            "C:\\Mock\\Renga.exe",
            "8.12.mock",
            _connected,
            _connected ? MockInstance.ProcessId : null,
            _connected ? true : null,
            [MockInstance]));

    public Task<IReadOnlyList<RengaInstanceInfo>> ListInstancesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RengaInstanceInfo>>([MockInstance]);

    public Task<ConnectionResult> ConnectAsync(
        int? processId,
        CancellationToken cancellationToken = default)
    {
        if (processId is not null && processId != MockInstance.ProcessId)
        {
            throw new InvalidOperationException($"No mock Renga instance with PID {processId} exists.");
        }

        _connected = true;
        return Task.FromResult(new ConnectionResult(true, MockInstance.ProcessId, MockInstance.ApiVersion, true, "Connected to mock Renga."));
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public Task<ProjectSummary> GetProjectInfoAsync(CancellationToken cancellationToken = default)
    {
        RequireConnection();
        return Task.FromResult(new ProjectSummary(
            "{AAAAAAAA-1111-2222-3333-BBBBBBBBBBBB}",
            "Mock BIM project",
            "TEST-001",
            "Synthetic project for offline MCP tests",
            "РД",
            "Building",
            false));
    }

    public Task<ObjectPage> QueryObjectsAsync(
        ObjectQuery query,
        CancellationToken cancellationToken = default)
    {
        RequireConnection();
        if (query.Offset < 0 || query.Limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Invalid pagination arguments.");
        }

        var filtered = Objects
            .Where(item => string.IsNullOrWhiteSpace(query.TypeId) || string.Equals(item.TypeId, query.TypeId, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(query.NameContains) || item.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(new ObjectPage(
            filtered.Length,
            query.Offset,
            query.Limit,
            query.Offset + query.Limit < filtered.Length,
            filtered.Skip(query.Offset).Take(query.Limit).ToArray()));
    }

    public Task<RengaObjectDetails> GetObjectAsync(
        string uniqueId,
        bool includeParameters,
        bool includeProperties,
        int maxValues,
        CancellationToken cancellationToken = default)
    {
        RequireConnection();
        var modelObject = Objects.FirstOrDefault(item => string.Equals(item.UniqueId, uniqueId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Mock object {uniqueId} was not found.");

        IReadOnlyList<ParameterValue>? parameters = includeParameters
            ? [new("{10000000-0000-0000-0000-000000000001}", "Example.Length", "Высота", 1, 3, true, false, 3200d)]
            : null;
        IReadOnlyList<PropertyValue>? properties = includeProperties
            ? [new("{20000000-0000-0000-0000-000000000001}", "Назначение", 2, true, "Тестовое", null)]
            : null;

        return Task.FromResult(new RengaObjectDetails(modelObject, parameters, properties, false, false));
    }

    public Task<StylePage> ListStylesAsync(
        string collection,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        RequireConnection();
        IReadOnlyList<StyleSummary> styles =
        [
            new(101, "{44444444-4444-4444-4444-444444444444}", "Mock style")
        ];
        return Task.FromResult(new StylePage(collection, styles.Count, offset, limit, false, styles));
    }

    public Task<CreateObjectResult> CreateObjectAsync(
        CreateObjectRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireConnection();
        if (!Guid.TryParse(request.TypeId, out var typeId))
        {
            throw new ArgumentException("type_id must be a GUID.", nameof(request));
        }

        var created = new RengaObjectSummary(
            99,
            "{99999999-9999-9999-9999-999999999999}",
            typeId.ToString("B").ToUpperInvariant(),
            "Mock created object",
            false);
        return Task.FromResult(new CreateObjectResult(
            !request.Preview,
            !request.Preview,
            request.Preview ? "Preview succeeded; the operation was rolled back." : "Object created in an undoable mock operation.",
            created));
    }

    public Task<ParameterUpdateResult> SetParameterAsync(
        string objectUniqueId,
        string parameterId,
        string value,
        bool preview,
        CancellationToken cancellationToken = default)
    {
        RequireConnection();
        return Task.FromResult(new ParameterUpdateResult(
            !preview,
            !preview,
            objectUniqueId,
            parameterId,
            4,
            "old",
            value));
    }

    private void RequireConnection()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("Not connected to mock Renga. Call renga_connect first.");
        }
    }

    public void Dispose()
    {
    }
}
