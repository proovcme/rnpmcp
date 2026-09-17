namespace RengaMcp.Models;

public sealed record RengaInstanceInfo(
    int? ProcessId,
    string ApiVersion,
    string Moniker);

public sealed record RengaStatus(
    string Mode,
    bool Installed,
    string? ExecutablePath,
    string? ProductVersion,
    bool Connected,
    int? ConnectedProcessId,
    bool? HasProject,
    IReadOnlyList<RengaInstanceInfo> RunningInstances);

public sealed record ConnectionResult(
    bool Connected,
    int? ProcessId,
    string ApiVersion,
    bool HasProject,
    string Message);

public sealed record ProjectSummary(
    string UniqueId,
    string Name,
    string Code,
    string Description,
    string Stage,
    string ProjectType,
    bool Shared);

public sealed record RengaObjectSummary(
    int LocalId,
    string UniqueId,
    string TypeId,
    string Name,
    bool Pinned);

public sealed record ObjectQuery(
    string? TypeId,
    string? NameContains,
    int Offset,
    int Limit);

public sealed record ObjectPage(
    int TotalMatched,
    int Offset,
    int Limit,
    bool HasMore,
    IReadOnlyList<RengaObjectSummary> Items);

public sealed record ParameterValue(
    string Id,
    string Name,
    string DisplayName,
    int ParameterType,
    int ValueType,
    bool HasValue,
    bool IsReadOnly,
    object? Value);

public sealed record PropertyValue(
    string Id,
    string Name,
    int PropertyType,
    bool HasValue,
    object? Value,
    string? Unit);

public sealed record RengaObjectDetails(
    RengaObjectSummary ModelObject,
    IReadOnlyList<ParameterValue>? Parameters,
    IReadOnlyList<PropertyValue>? Properties,
    bool ParametersTruncated,
    bool PropertiesTruncated);
