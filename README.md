# Renga MCP

Local Model Context Protocol server for the Renga BIM desktop application. Read tools are the default surface; narrowly scoped write tools use preview-by-default and Renga undoable operations.

The server attaches to an already running Renga process through the Windows Running Object Table (ROT). It does not launch Renga, open files, save projects, or close the application.

## Requirements

- Windows x64
- .NET 8 runtime or SDK
- Renga for live mode

The project intentionally uses late-bound COM (`IDispatch`) and does not redistribute the Renga SDK or its binaries.

## Build and test

```powershell
dotnet build .\src\RengaMcp.Server\RengaMcp.Server.csproj -c Release
dotnet test .\tests\RengaMcp.Tests\RengaMcp.Tests.csproj -c Release
```

## Offline mock mode

Mock mode exercises every MCP tool without Renga:

```powershell
$env:RENGA_MCP_MODE = 'mock'
dotnet run --project .\src\RengaMcp.Server\RengaMcp.Server.csproj
```

Do not type ordinary text into that process: stdout is the MCP JSON-RPC transport.

## Live mode

1. Start Renga and open a project.
2. Start the MCP server without `RENGA_MCP_MODE=mock`.
3. Call `renga_status`, then `renga_connect`, then the read tools.

Available tools:

- `renga_status`
- `renga_list_instances`
- `renga_connect`
- `renga_disconnect`
- `renga_get_project_info`
- `renga_query_objects`
- `renga_get_object`
- `renga_creation_types`
- `renga_list_styles`
- `renga_create_object`
- `renga_set_parameter`

`renga_create_object` and `renga_set_parameter` default to `preview=true`. Preview executes the Renga operation and rolls it back. A persistent change requires the caller to explicitly pass `preview=false`. Check the returned `undoRecorded` value: typed Renga interop supports model undo, but some API builds cannot marshal the model GUID through late-bound COM and fall back to a normal transaction.

## Codex configuration

Copy `.codex/config.example.toml` into a trusted project's `.codex/config.toml` and replace the DLL path with an absolute path. Keep `default_tools_approval_mode = "approve"`: write tools must remain visible to the user before execution.

## Safety and BIM interpretation

- Renga COM calls are serialized on one STA thread.
- Local numeric IDs are returned for diagnostics; use `UniqueId` GUIDs for durable references.
- Object names are localized labels and must not be treated as stable identifiers.
- Model data and absence of Renga warnings do not prove ergonomic, accessibility, fire-safety, or regulatory compliance.
- Large result sets are paginated and individual object values are bounded.
- Creation is generic and starts at Renga's default placement. Geometry and semantic parameters must be checked after creation; a successful API call is not a design-quality check.
- Parameter doubles use Renga API base units. Inspect the parameter definition and existing value before writing; never infer a unit from the localized display name.

## Current scope

The read adapter has been smoke-tested against Renga Standard 8.12 / API 2.46. The initial write surface covers generic object creation and typed parameter assignment. Export, screenshots, selection control, direct placement editing, and IFC fallback remain out of scope.

## Agent skill

The reusable `operate-renga-via-api` skill is in `skills/operate-renga-via-api`. It distills the official Renga API creation, transaction, placement, roof, geometry-export, and collision-check patterns. It contains no SDK binaries, project identifiers, or machine-specific paths.

## License

MIT. See `LICENSE`.
