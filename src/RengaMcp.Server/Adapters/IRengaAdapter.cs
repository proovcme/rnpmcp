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

    Task<StylePage> ListStylesAsync(
        string collection,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CreateObjectResult> CreateObjectAsync(
        CreateObjectRequest request,
        CancellationToken cancellationToken = default);

    Task<ParameterUpdateResult> SetParameterAsync(
        string objectUniqueId,
        string parameterId,
        string value,
        bool preview,
        CancellationToken cancellationToken = default);

    Task<ObjectPortsResult> GetPortsAsync(
        string objectUniqueId,
        CancellationToken cancellationToken = default);

    Task<PipeConnectionResult> CreatePipeConnectionAsync(
        PipeConnectionRequest request,
        CancellationToken cancellationToken = default);
}
