using System.Diagnostics;
using System.Globalization;
using System.Reflection;
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

    public Task<StylePage> ListStylesAsync(
        string collection,
        int offset,
        int limit,
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(() => ListStylesCore(collection, offset, limit), cancellationToken);

    public Task<CreateObjectResult> CreateObjectAsync(
        CreateObjectRequest request,
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(() => CreateObjectCore(request), cancellationToken);

    public Task<ParameterUpdateResult> SetParameterAsync(
        string objectUniqueId,
        string parameterId,
        string value,
        bool preview,
        CancellationToken cancellationToken = default) =>
        _worker.InvokeAsync(
            () => SetParameterCore(objectUniqueId, parameterId, value, preview),
            cancellationToken);

    private StylePage ListStylesCore(string collection, int offset, int limit)
    {
        ValidatePage(offset, limit);
        if (!CreationCatalog.StyleCollections.TryGetValue(collection, out var propertyName))
        {
            throw new ArgumentException(
                $"Unknown style collection '{collection}'. Allowed values: {string.Join(", ", CreationCatalog.StyleCollections.Keys)}.",
                nameof(collection));
        }

        var application = RequireApplicationWithProject();
        var project = RequireComObject(ComDispatch.Get(application, "Project"), "Project");
        var styles = RequireComObject(ComDispatch.Get(project, propertyName), propertyName);
        try
        {
            var count = ComDispatch.Get<int>(styles, "Count");
            var items = new List<StyleSummary>();
            for (var index = offset; index < Math.Min(count, offset + limit); index++)
            {
                var style = RequireComObject(ComDispatch.Call(styles, "GetByIndex", index), "style");
                try
                {
                    items.Add(new StyleSummary(
                        ComDispatch.Get<int>(style, "Id"),
                        NormalizeGuid(ComDispatch.GetOrDefault(style, "UniqueIdS", string.Empty)),
                        ComDispatch.GetOrDefault(style, "Name", string.Empty)));
                }
                finally
                {
                    ComDispatch.Release(style);
                }
            }

            return new StylePage(collection, count, offset, limit, offset + limit < count, items);
        }
        finally
        {
            ComDispatch.Release(styles);
            ComDispatch.Release(project);
        }
    }

    private CreateObjectResult CreateObjectCore(CreateObjectRequest request)
    {
        if (!Guid.TryParse(request.TypeId, out var typeId))
        {
            throw new ArgumentException("type_id must be a GUID.", nameof(request));
        }

        if (request.HostObjectId is < 1 || request.StyleId is < 0 || request.CategoryId is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Host IDs must be positive; style and category IDs cannot be negative.");
        }

        var application = RequireApplicationWithProject();
        var project = RequireComObject(ComDispatch.Get(application, "Project"), "Project");
        var model = RequireComObject(ComDispatch.Get(project, "Model"), "Model");
        object? arguments = null;
        object? operation = null;
        object? created = null;
        var operationStarted = false;
        var operationFinished = false;
        var undoRecorded = false;

        try
        {
            if (ComDispatch.Call<bool>(project, "HasActiveOperation"))
            {
                throw new InvalidOperationException("Renga already has an active edit operation. Finish it before calling a write tool.");
            }

            ValidateHostObject(model, request.HostObjectId);
            arguments = RequireComObject(ComDispatch.Call(model, "CreateNewEntityArgs"), "new entity arguments");
            ComDispatch.Set(arguments, "TypeIdS", typeId.ToString("B"));
            if (request.HostObjectId is int hostId)
            {
                ComDispatch.Set(arguments, "HostObjectId", hostId);
            }

            if (request.StyleId is int styleId)
            {
                ComDispatch.Set(arguments, "StyleId", styleId);
            }

            if (request.CategoryId is int categoryId)
            {
                ComDispatch.Set(arguments, "CategoryId", categoryId);
            }

            operation = CreateEditOperation(project, model, out undoRecorded);
            ComDispatch.Call(operation, "Start");
            operationStarted = true;

            created = ComDispatch.Call(model, "CreateObject", arguments);
            if (created is null)
            {
                var lastError = ComDispatch.GetOrDefault(application, "LastError", "Unknown Renga creation error.");
                throw new InvalidOperationException($"Renga did not create the object: {lastError}");
            }

            var summary = ReadObjectSummary(created);
            if (request.Preview)
            {
                ComDispatch.Call(operation, "Rollback");
                operationFinished = true;
                return new CreateObjectResult(false, false, "Preview succeeded; the operation was rolled back.", summary);
            }

            ComDispatch.Call(operation, "Apply");
            operationFinished = true;
            return new CreateObjectResult(
                true,
                undoRecorded,
                undoRecorded
                    ? "Object created in an undoable Renga operation."
                    : "Object created in a Renga operation; this API build did not expose the model GUID through late-bound COM, so no undo item was recorded.",
                summary);
        }
        catch
        {
            if (operationStarted && !operationFinished && operation is not null)
            {
                TryRollback(operation);
            }

            throw;
        }
        finally
        {
            ComDispatch.Release(created);
            ComDispatch.Release(operation);
            ComDispatch.Release(arguments);
            ComDispatch.Release(model);
            ComDispatch.Release(project);
        }
    }

    private ParameterUpdateResult SetParameterCore(
        string objectUniqueId,
        string parameterId,
        string value,
        bool preview)
    {
        if (!Guid.TryParse(objectUniqueId, out var objectId) || !Guid.TryParse(parameterId, out var parsedParameterId))
        {
            throw new ArgumentException("object_unique_id and parameter_id must be GUIDs.");
        }

        var application = RequireApplicationWithProject();
        var project = RequireComObject(ComDispatch.Get(application, "Project"), "Project");
        var model = RequireComObject(ComDispatch.Get(project, "Model"), "Model");
        var objects = RequireComObject(ComDispatch.Call(model, "GetObjects"), "model objects");
        object? modelObject = null;
        object? parameters = null;
        object? parameter = null;
        object? operation = null;
        var operationStarted = false;
        var operationFinished = false;
        var undoRecorded = false;

        try
        {
            modelObject = FindModelObject(objects, objectId);
            parameters = RequireComObject(ComDispatch.Call(modelObject, "GetParameters"), "parameter container");
            parameter = RequireComObject(
                ComDispatch.Call(parameters, "GetS", parsedParameterId.ToString("B")),
                "parameter");

            if (ComDispatch.Get<bool>(parameter, "IsReadOnly"))
            {
                throw new InvalidOperationException($"Parameter {parsedParameterId:B} is read-only.");
            }

            var valueType = ComDispatch.Get<int>(parameter, "ValueType");
            var previousValue = ComDispatch.Get<bool>(parameter, "HasValue")
                ? ReadParameterValue(parameter, valueType)
                : null;
            var parsedValue = ParseParameterValue(value, valueType);

            if (ComDispatch.Call<bool>(project, "HasActiveOperation"))
            {
                throw new InvalidOperationException("Renga already has an active edit operation. Finish it before calling a write tool.");
            }

            operation = CreateEditOperation(project, model, out undoRecorded);
            ComDispatch.Call(operation, "Start");
            operationStarted = true;
            WriteParameterValue(parameter, valueType, parsedValue);
            var newValue = ReadParameterValue(parameter, valueType);

            if (preview)
            {
                ComDispatch.Call(operation, "Rollback");
                operationFinished = true;
                return new ParameterUpdateResult(false, false, objectId.ToString("B"), parsedParameterId.ToString("B"), valueType, previousValue, newValue);
            }

            ComDispatch.Call(operation, "Apply");
            operationFinished = true;
            return new ParameterUpdateResult(true, undoRecorded, objectId.ToString("B"), parsedParameterId.ToString("B"), valueType, previousValue, newValue);
        }
        catch
        {
            if (operationStarted && !operationFinished && operation is not null)
            {
                TryRollback(operation);
            }

            throw;
        }
        finally
        {
            ComDispatch.Release(operation);
            ComDispatch.Release(parameter);
            ComDispatch.Release(parameters);
            ComDispatch.Release(modelObject);
            ComDispatch.Release(objects);
            ComDispatch.Release(model);
            ComDispatch.Release(project);
        }
    }

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

    private static object FindModelObject(object objects, Guid uniqueId)
    {
        object? modelObject = null;
        try
        {
            modelObject = ComDispatch.Call(objects, "GetByUniqueIdS", uniqueId.ToString("B"));
        }
        catch (COMException)
        {
            // Older API builds may expose lookup only through iteration.
        }

        modelObject ??= FindObjectByUniqueId(objects, uniqueId);
        return modelObject
            ?? throw new KeyNotFoundException($"Renga object {uniqueId:B} was not found in the current project snapshot.");
    }

    private static void ValidateHostObject(object model, int? hostObjectId)
    {
        if (hostObjectId is not int hostId)
        {
            return;
        }

        var objects = RequireComObject(ComDispatch.Call(model, "GetObjects"), "model objects");
        object? host = null;
        try
        {
            host = ComDispatch.Call(objects, "GetById", hostId);
            if (host is null)
            {
                throw new KeyNotFoundException($"Host object with local ID {hostId} was not found.");
            }
        }
        finally
        {
            ComDispatch.Release(host);
            ComDispatch.Release(objects);
        }
    }

    private static object ParseParameterValue(string value, int valueType) => valueType switch
    {
        1 when bool.TryParse(value, out var booleanValue) => booleanValue,
        1 when value == "1" => true,
        1 when value == "0" => false,
        1 => throw new FormatException("Boolean parameter values must be true, false, 1, or 0."),
        2 when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue) => integerValue,
        2 => throw new FormatException("Integer parameter value is invalid. Use invariant digits without a unit."),
        3 when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue) => doubleValue,
        3 => throw new FormatException("Double parameter value is invalid. Use a dot as decimal separator and Renga API base units."),
        4 => value,
        _ => throw new NotSupportedException($"Renga parameter ValueType {valueType} is not supported for writing.")
    };

    private static void WriteParameterValue(object parameter, int valueType, object value)
    {
        switch (valueType)
        {
            case 1:
                ComDispatch.Call(parameter, "SetBoolValue", value);
                break;
            case 2:
                ComDispatch.Call(parameter, "SetIntValue", value);
                break;
            case 3:
                ComDispatch.Call(parameter, "SetDoubleValue", value);
                break;
            case 4:
                ComDispatch.Call(parameter, "SetStringValue", value);
                break;
            default:
                throw new NotSupportedException($"Renga parameter ValueType {valueType} is not supported for writing.");
        }
    }

    private static void TryRollback(object operation)
    {
        try
        {
            ComDispatch.Call(operation, "Rollback");
        }
        catch (COMException)
        {
            // Preserve the original exception; Renga may already have aborted the operation.
        }
    }

    private static object CreateEditOperation(object project, object model, out bool undoRecorded)
    {
        object? modelId;
        try
        {
            modelId = ComDispatch.Get(model, "Id");
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or ArgumentException or InvalidCastException)
        {
            undoRecorded = false;
            return RequireComObject(ComDispatch.Call(project, "CreateOperation"), "edit operation");
        }

        if (modelId is null)
        {
            undoRecorded = false;
            return RequireComObject(ComDispatch.Call(project, "CreateOperation"), "edit operation");
        }

        try
        {
            var operation = ComDispatch.Call(project, "CreateOperationWithUndo", modelId);
            undoRecorded = true;
            return RequireComObject(operation, "undoable operation");
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or ArgumentException or InvalidCastException)
        {
            undoRecorded = false;
            return RequireComObject(ComDispatch.Call(project, "CreateOperation"), "edit operation");
        }
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
