using RengaMcp.Models;

namespace RengaMcp.Adapters;

public interface IRengaAdapter : IDisposable
{
    Task<RengaStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RengaInstanceInfo>> ListInstancesAsync(
        CancellationToken cancellationToken = default);

    Task<ConnectionResult> ConnectAsync(
        int? processId,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<ProjectSummary> GetProjectInfoAsync(CancellationToken cancellationToken = default);

    Task<ObjectPage> QueryObjectsAsync(
        ObjectQuery query,
        CancellationToken cancellationToken = default);

    Task<RengaObjectDetails> GetObjectAsync(
        string uniqueId,
        bool includeParameters,
        bool includeProperties,
        int maxValues,
        CancellationToken cancellationToken = default);
}
