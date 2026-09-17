using System.ComponentModel;
using ModelContextProtocol.Server;
using RengaMcp.Adapters;
using RengaMcp.Models;

namespace RengaMcp.Tools;

[McpServerToolType]
public sealed class RengaTools(IRengaAdapter adapter)
{
    [McpServerTool(Name = "renga_status", ReadOnly = true, Idempotent = true)]
    [Description("Inspect Renga installation, connection, open-project state, and running instances. Does not launch Renga.")]
    public Task<RengaStatus> GetStatus(CancellationToken cancellationToken) =>
        adapter.GetStatusAsync(cancellationToken);

    [McpServerTool(Name = "renga_list_instances", ReadOnly = true, Idempotent = true)]
    [Description("List running Renga instances registered in the Windows Running Object Table.")]
    public Task<IReadOnlyList<RengaInstanceInfo>> ListInstances(CancellationToken cancellationToken) =>
        adapter.ListInstancesAsync(cancellationToken);

    [McpServerTool(Name = "renga_connect", ReadOnly = true, Idempotent = true)]
    [Description("Attach to an already running Renga instance. Omit process_id to select the first instance. Never launches or closes Renga.")]
    public Task<ConnectionResult> Connect(
        [Description("Optional Renga process ID returned by renga_list_instances.")] int? processId = null,
        CancellationToken cancellationToken = default) =>
        adapter.ConnectAsync(processId, cancellationToken);

    [McpServerTool(Name = "renga_disconnect", ReadOnly = true, Idempotent = true)]
    [Description("Release the current COM connection without closing Renga or its project.")]
    public async Task<object> Disconnect(CancellationToken cancellationToken)
    {
        await adapter.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        return new { disconnected = true };
    }

    [McpServerTool(Name = "renga_get_project_info", ReadOnly = true, Idempotent = true)]
    [Description("Read identity and descriptive fields of the project open in the connected Renga instance.")]
    public Task<ProjectSummary> GetProjectInfo(CancellationToken cancellationToken) =>
        adapter.GetProjectInfoAsync(cancellationToken);

    [McpServerTool(Name = "renga_query_objects", ReadOnly = true, Idempotent = true)]
    [Description("Query a paginated snapshot of Renga model objects by optional type GUID and name substring.")]
    public Task<ObjectPage> QueryObjects(
        [Description("Optional Renga entity type GUID, including or excluding braces.")] string? typeId = null,
        [Description("Optional case-insensitive substring of the localized object name.")] string? nameContains = null,
        [Description("Zero-based offset in the filtered result.")] int offset = 0,
        [Description("Number of items to return, from 1 to 500.")] int limit = 100,
        CancellationToken cancellationToken = default) =>
        adapter.QueryObjectsAsync(new ObjectQuery(typeId, nameContains, offset, limit), cancellationToken);

    [McpServerTool(Name = "renga_get_object", ReadOnly = true, Idempotent = true)]
    [Description("Read one Renga model object by stable UniqueId, optionally including parameter and custom-property values.")]
    public Task<RengaObjectDetails> GetObject(
        [Description("Stable model object GUID returned by renga_query_objects.")] string uniqueId,
        [Description("Include the object's parameter values.")] bool includeParameters = true,
        [Description("Include the object's assigned custom properties.")] bool includeProperties = true,
        [Description("Maximum parameters and maximum properties returned independently, from 1 to 500.")] int maxValues = 200,
        CancellationToken cancellationToken = default) =>
        adapter.GetObjectAsync(uniqueId, includeParameters, includeProperties, maxValues, cancellationToken);

    [McpServerTool(Name = "renga_creation_types", ReadOnly = true, Idempotent = true)]
    [Description("List common Renga entity type GUIDs supported by the generic creation tool and their host requirements.")]
    public static IReadOnlyList<CreationType> GetCreationTypes() => CreationCatalog.Types;

    [McpServerTool(Name = "renga_list_styles", ReadOnly = true, Idempotent = true)]
    [Description("List style IDs from a safe named Renga style collection for use with renga_create_object.")]
    public Task<StylePage> ListStyles(
        [Description("Style collection key such as beam, column, door, window, plate, element, equipment, mechanical_equipment, plumbing_fixture, lighting_fixture, or wiring_accessory.")] string collection,
        [Description("Zero-based offset.")] int offset = 0,
        [Description("Number of styles to return, from 1 to 500.")] int limit = 100,
        CancellationToken cancellationToken = default) =>
        adapter.ListStylesAsync(collection, offset, limit, cancellationToken);

    [McpServerTool(Name = "renga_create_object", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Create one Renga model object in a transaction. Preview defaults to true and performs a real creation followed by rollback; set preview=false only after checking type, host, style, and units. Inspect undoRecorded because some late-bound API builds cannot expose the model GUID needed for an undo item.")]
    public Task<CreateObjectResult> CreateObject(
        [Description("Renga entity type GUID; use renga_creation_types for common values.")] string typeId,
        [Description("Optional local ID of the host object. Required for doors, windows, openings, and other dependent objects.")] int? hostObjectId = null,
        [Description("Optional local style ID returned by renga_list_styles.")] int? styleId = null,
        [Description("Optional Renga category ID.")] int? categoryId = null,
        [Description("When true, create then roll back without persisting. Defaults to true for safety.")] bool preview = true,
        CancellationToken cancellationToken = default) =>
        adapter.CreateObjectAsync(
            new CreateObjectRequest(typeId, hostObjectId, styleId, categoryId, preview),
            cancellationToken);

    [McpServerTool(Name = "renga_set_parameter", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Set one writable Renga object parameter in a transaction. The current parameter type controls parsing. Preview defaults to true and rolls the test change back; inspect undoRecorded after commit.")]
    public Task<ParameterUpdateResult> SetParameter(
        [Description("Stable object GUID returned by renga_query_objects.")] string objectUniqueId,
        [Description("Parameter GUID returned by renga_get_object.")] string parameterId,
        [Description("New value using invariant format: true/false, integer, dot-decimal number in Renga API units, or text.")] string value,
        [Description("When true, set then roll back without persisting. Defaults to true for safety.")] bool preview = true,
        CancellationToken cancellationToken = default) =>
        adapter.SetParameterAsync(objectUniqueId, parameterId, value, preview, cancellationToken);
}
