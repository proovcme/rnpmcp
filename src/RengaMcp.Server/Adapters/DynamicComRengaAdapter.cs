using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RengaMcp.Infrastructure;
using RengaMcp.Models;

namespace RengaMcp.Adapters;

public sealed class DynamicComRengaAdapter : IRengaAdapter
{
    private const string RengaClsid = "{C94A380A-02F2-427B-8FD3-7D6572E16556}";
    private readonly ILogger<DynamicComRengaAdapter> _logger;
    private readonly StaWorker _worker = new("Renga MCP COM STA");
    private object? _application;
    private RengaInstanceInfo? _connectedInstance;
    private bool _disposed;

    public DynamicComRengaAdapter(ILogger<DynamicComRengaAdapter> logger)
    {
        _logger = logger;
    }

    public Task<RengaStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(
            () =>
            {
                var installation = DetectInstallation();
                var instances = RunningObjectTable.ListRengaInstances();
                bool? hasProject = _application is null ? null : ReadHasProject(_application);

                return new RengaStatus(
                    "com",
                    installation.Installed,
                    installation.Path,
                    installation.Version,
                    _application is not null,
                    _connectedInstance?.ProcessId,
                    hasProject,
                    instances);
            },
            cancellationToken);

    public Task<IReadOnlyList<RengaInstanceInfo>> ListInstancesAsync(
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(RunningObjectTable.ListRengaInstances, cancellationToken);

    public Task<ConnectionResult> ConnectAsync(
        int? processId,
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(
            () =>
            {
                DisconnectCore();
                var instances = RunningObjectTable.ListRengaInstances();
                var selected = processId is null
                    ? (instances.Count == 0 ? null : instances[0])
                    : instances.FirstOrDefault(instance => instance.ProcessId == processId);

                if (selected is null)
                {
                    throw new InvalidOperationException(
                        processId is null
                            ? "Renga is installed but no running instance is available. Start Renga and open a project first."
                            : $"No running Renga instance with PID {processId} is available.");
                }

                _application = RunningObjectTable.GetRengaApplication(selected.ProcessId);
                _connectedInstance = selected;
                try
                {
                    var hasProject = ReadHasProject(_application);
                    _logger.LogInformation("Connected to Renga PID {ProcessId}, API {ApiVersion}", selected.ProcessId, selected.ApiVersion);

                    return new ConnectionResult(
                        true,
                        selected.ProcessId,
                        selected.ApiVersion,
                        hasProject,
                        hasProject ? "Connected to Renga." : "Connected to Renga, but no project is open.");
                }
                catch
                {
                    DisconnectCore();
                    throw;
                }
            },
            cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(DisconnectCore, cancellationToken);

    public Task<ProjectSummary> GetProjectInfoAsync(CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(
            () =>
            {
                var application = RequireApplicationWithProject();
                var project = RequireComObject(ComDispatch.Get(application, "Project"), "Project");
                var info = RequireComObject(ComDispatch.Get(project, "ProjectInfo"), "ProjectInfo");

                try
                {
                    return new ProjectSummary(
                        ComDispatch.GetOrDefault(info, "UniqueIdS", string.Empty),
                        ComDispatch.GetOrDefault(info, "Name", string.Empty),
                        ComDispatch.GetOrDefault(info, "Code", string.Empty),
                        ComDispatch.GetOrDefault(info, "Description", string.Empty),
                        ComDispatch.GetOrDefault(info, "Stage", string.Empty),
                        Convert.ToString(
                            ComDispatch.GetOrDefault<object?>(project, "ProjectType", null),
                            System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                        ComDispatch.GetOrDefault(project, "Shared", false));
                }
                finally
                {
                    ComDispatch.Release(info);
                    ComDispatch.Release(project);
                }
            },
            cancellationToken);

    public Task<ObjectPage> QueryObjectsAsync(
        ObjectQuery query,
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(() => QueryObjectsCore(query), cancellationToken);

    public Task<RengaObjectDetails> GetObjectAsync(
        string uniqueId,
        bool includeParameters,
        bool includeProperties,
        int maxValues,
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(
            () => GetObjectCore(uniqueId, includeParameters, includeProperties, maxValues),
            cancellationToken);

    private ObjectPage QueryObjectsCore(ObjectQuery query)
    {
        ValidatePage(query.Offset, query.Limit);
        var application = RequireApplicationWithProject();
        var project = RequireComObject(ComDispatch.Get(application, "Project"), "Project");
        var model = RequireComObject(ComDispatch.Get(project, "Model"), "Model");
        var objects = RequireComObject(ComDispatch.Call(model, "GetObjects"), "model objects");
        var matches = new List<RengaObjectSummary>();

        try
        {
            var count = ComDispatch.Get<int>(objects, "Count");
            for (var index = 0; index < count; index++)
            {
                var modelObject = RequireComObject(ComDispatch.Call(objects, "GetByIndex", index), "model object");
                try
                {
                    var summary = ReadObjectSummary(modelObject);
                    if (!string.IsNullOrWhiteSpace(query.TypeId) &&
                        !string.Equals(summary.TypeId, NormalizeGuid(query.TypeId), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(query.NameContains) &&
                        !summary.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    matches.Add(summary);
                }
                finally
                {
                    ComDispatch.Release(modelObject);
                }
            }

            return new ObjectPage(
                matches.Count,
                query.Offset,
                query.Limit,
                query.Offset + query.Limit < matches.Count,
                matches.Skip(query.Offset).Take(query.Limit).ToArray());
        }
        finally
        {
            ComDispatch.Release(objects);
            ComDispatch.Release(model);
            ComDispatch.Release(project);
        }
    }

    private RengaObjectDetails GetObjectCore(
        string uniqueId,
        bool includeParameters,
        bool includeProperties,
        int maxValues)
    {
        if (!Guid.TryParse(uniqueId, out var parsedGuid))
        {
            throw new ArgumentException("unique_id must be a GUID.", nameof(uniqueId));
        }

        if (maxValues is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValues), "max_values must be between 1 and 500.");
        }

        var application = RequireApplicationWithProject();
        var project = RequireComObject(ComDispatch.Get(application, "Project"), "Project");
        var model = RequireComObject(ComDispatch.Get(project, "Model"), "Model");
        var objects = RequireComObject(ComDispatch.Call(model, "GetObjects"), "model objects");
        object? modelObject = null;

        try
        {
            try
            {
                modelObject = ComDispatch.Call(objects, "GetByUniqueIdS", parsedGuid.ToString("B"));
            }
            catch (COMException)
            {
                // Older API builds may expose lookup only through iteration.
            }

            modelObject ??= FindObjectByUniqueId(objects, parsedGuid);
            if (modelObject is null)
            {
                throw new KeyNotFoundException($"Renga object {parsedGuid:B} was not found in the current project snapshot.");
            }

            var parametersTruncated = false;
            var propertiesTruncated = false;
            var parameters = includeParameters
                ? ReadParameters(modelObject, maxValues, out parametersTruncated)
                : null;
            var properties = includeProperties
                ? ReadProperties(modelObject, maxValues, out propertiesTruncated)
                : null;

            return new RengaObjectDetails(
                ReadObjectSummary(modelObject),
                parameters,
                properties,
                includeParameters && parametersTruncated,
                includeProperties && propertiesTruncated);
        }
        finally
        {
            ComDispatch.Release(modelObject);
            ComDispatch.Release(objects);
            ComDispatch.Release(model);
            ComDispatch.Release(project);
        }
    }

    private static List<ParameterValue> ReadParameters(
        object modelObject,
        int maxValues,
        out bool truncated)
    {
        object? container = null;
        object? ids = null;
        var result = new List<ParameterValue>();

        try
        {
            container = ComDispatch.Call(modelObject, "GetParameters");
            if (container is null)
            {
                truncated = false;
                return result;
            }

            ids = ComDispatch.Call(container, "GetIds");
            if (ids is null)
            {
                truncated = false;
                return result;
            }

            var count = ComDispatch.Get<int>(ids, "Count");
            truncated = count > maxValues;
            for (var index = 0; index < Math.Min(count, maxValues); index++)
            {
                var id = ComDispatch.Call<string>(ids, "GetS", index);
                var parameter = RequireComObject(ComDispatch.Call(container, "GetS", id), "parameter");
                object? definition = null;
                try
                {
                    definition = ComDispatch.Get(parameter, "Definition");
                    var valueType = ComDispatch.Get<int>(parameter, "ValueType");
                    var hasValue = ComDispatch.Get<bool>(parameter, "HasValue");
                    result.Add(new ParameterValue(
                        id,
                        definition is null ? id : ComDispatch.Get<string>(definition, "Name"),
                        definition is null ? id : ComDispatch.Get<string>(definition, "Text"),
                        definition is null ? 0 : ComDispatch.Get<int>(definition, "ParameterType"),
                        valueType,
                        hasValue,
                        ComDispatch.Get<bool>(parameter, "IsReadOnly"),
                        hasValue ? ReadParameterValue(parameter, valueType) : null));
                }
                finally
                {
                    ComDispatch.Release(definition);
                    ComDispatch.Release(parameter);
                }
            }

            return result;
        }
        finally
        {
            ComDispatch.Release(ids);
            ComDispatch.Release(container);
        }
    }

    private static List<PropertyValue> ReadProperties(
        object modelObject,
        int maxValues,
        out bool truncated)
    {
        object? container = null;
        object? ids = null;
        var result = new List<PropertyValue>();

        try
        {
            container = ComDispatch.Call(modelObject, "GetProperties");
            if (container is null)
            {
                truncated = false;
                return result;
            }

            ids = ComDispatch.Call(container, "GetIds");
            if (ids is null)
            {
                truncated = false;
                return result;
            }

            var count = ComDispatch.Get<int>(ids, "Count");
            truncated = count > maxValues;
            for (var index = 0; index < Math.Min(count, maxValues); index++)
            {
                var id = ComDispatch.Call<string>(ids, "GetS", index);
                var property = RequireComObject(ComDispatch.Call(container, "GetS", id), "property");
                try
                {
                    var type = ComDispatch.Get<int>(property, "Type");
                    var hasValue = ComDispatch.Call<bool>(property, "HasValue");
                    var (value, unit) = hasValue ? ReadPropertyValue(property, type) : (null, null);
                    result.Add(new PropertyValue(
                        id,
                        ComDispatch.Get<string>(property, "Name"),
                        type,
                        hasValue,
                        value,
                        unit));
                }
                finally
                {
                    ComDispatch.Release(property);
                }
            }

            return result;
        }
        finally
        {
            ComDispatch.Release(ids);
            ComDispatch.Release(container);
        }
    }

    private static object? ReadParameterValue(object parameter, int valueType) => valueType switch
    {
        1 => ComDispatch.Call<bool>(parameter, "GetBoolValue"),
        2 => ComDispatch.Call<int>(parameter, "GetIntValue"),
        3 => ComDispatch.Call<double>(parameter, "GetDoubleValue"),
        4 => ComDispatch.Call<string>(parameter, "GetStringValue"),
        _ => null
    };

    private static (object? Value, string? Unit) ReadPropertyValue(object property, int type) => type switch
    {
        1 => (ComDispatch.Call<double>(property, "GetDoubleValue"), null),
        2 => (ComDispatch.Call<string>(property, "GetStringValue"), null),
        3 => (ComDispatch.Call<double>(property, "GetAngleValue", 1), "deg"),
        4 => (ComDispatch.Call<double>(property, "GetAreaValue", 3), "m2"),
        5 => (ComDispatch.Call<bool>(property, "GetBooleanValue"), null),
        6 => (ComDispatch.Call<string>(property, "GetEnumerationValue"), null),
        7 => (ComDispatch.Call<int>(property, "GetIntegerValue"), null),
        8 => (ComDispatch.Call<double>(property, "GetLengthValue", 4), "m"),
        9 => (ComDispatch.Call<int>(property, "GetLogicalValue"), null),
        10 => (ComDispatch.Call<double>(property, "GetMassValue", 2), "kg"),
        11 => (ComDispatch.Call<double>(property, "GetVolumeValue", 3), "m3"),
        _ => (null, null)
    };

    private static object? FindObjectByUniqueId(object objects, Guid uniqueId)
    {
        var count = ComDispatch.Get<int>(objects, "Count");
        for (var index = 0; index < count; index++)
        {
            var candidate = RequireComObject(ComDispatch.Call(objects, "GetByIndex", index), "model object");
            if (Guid.TryParse(ComDispatch.Get<string>(candidate, "UniqueIdS"), out var candidateId) &&
                candidateId == uniqueId)
            {
                return candidate;
            }

            ComDispatch.Release(candidate);
        }

        return null;
    }

    private static RengaObjectSummary ReadObjectSummary(object modelObject) =>
        new(
            ComDispatch.Get<int>(modelObject, "Id"),
            NormalizeGuid(ComDispatch.Get<string>(modelObject, "UniqueIdS")),
            NormalizeGuid(ComDispatch.Get<string>(modelObject, "ObjectTypeS")),
            ComDispatch.Get<string>(modelObject, "Name"),
            ComDispatch.Get<bool>(modelObject, "Pinned"));

    private object RequireApplicationWithProject()
    {
        if (_application is null)
        {
            throw new InvalidOperationException("Not connected to Renga. Call renga_connect first.");
        }

        if (!ReadHasProject(_application))
        {
            throw new InvalidOperationException("The connected Renga instance has no open project.");
        }

        return _application;
    }

    private static bool ReadHasProject(object application) =>
        ComDispatch.Call<bool>(application, "HasProject");

    private void DisconnectCore()
    {
        if (_application is not null)
        {
            ComDispatch.Release(_application);
            _application = null;
            _connectedInstance = null;
            _logger.LogInformation("Disconnected from Renga without closing the application.");
        }
    }

    private static (bool Installed, string? Path, string? Version) DetectInstallation()
    {
        using var key = Registry.ClassesRoot.OpenSubKey($@"CLSID\{RengaClsid}\LocalServer32");
        var registered = key?.GetValue(null) as string;
        var path = registered?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return (false, path, null);
        }

        return (true, path, FileVersionInfo.GetVersionInfo(path).ProductVersion);
    }

    private static object RequireComObject(object? value, string name) =>
        value ?? throw new InvalidOperationException($"Renga returned no {name} object.");

    private static string NormalizeGuid(string value) =>
        Guid.TryParse(value, out var parsed) ? parsed.ToString("B").ToUpperInvariant() : value;

    private static void ValidatePage(int offset, int limit)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be negative.");
        }

        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "limit must be between 1 and 500.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _worker.InvokeAsync(DisconnectCore).GetAwaiter().GetResult();
        }
        finally
        {
            _worker.Dispose();
        }
    }
}
