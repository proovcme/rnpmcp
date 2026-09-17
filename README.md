# Renga MCP

Local, read-only Model Context Protocol server for the Renga BIM desktop application.

The server attaches to an already running Renga process through the Windows Running Object Table (ROT). It does not launch Renga, open files, save projects, or modify model data.

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

## Codex configuration

Copy `.codex/config.example.toml` into a trusted project's `.codex/config.toml` and replace the DLL path with an absolute path. The example uses `default_tools_approval_mode = "approve"` because this release contains read-only tools only.

## Safety and BIM interpretation

- Renga COM calls are serialized on one STA thread.
- Local numeric IDs are returned for diagnostics; use `UniqueId` GUIDs for durable references.
- Object names are localized labels and must not be treated as stable identifiers.
- Model data and absence of Renga warnings do not prove ergonomic, accessibility, fire-safety, or regulatory compliance.
- Large result sets are paginated and individual object values are bounded.

## Current scope

This is a read-only MVP. The live adapter has been smoke-tested against Renga Standard 8.12 / API 2.46. Write operations, export, screenshots, selection control, and IFC fallback are intentionally out of scope for this release.

## License

MIT. See `LICENSE`.
