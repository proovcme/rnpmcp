# Renga API connection and operation basics

This is a concise working guide distilled from the official Renga API documentation and SDK samples. Verify version-sensitive behavior against the documentation shipped with the installed SDK.

Official starting points:

- https://help.rengabim.com/api/overview-api-sdk-plugin.html
- https://help.rengabim.com/api/overview-general-principles.html
- https://help.rengabim.com/api/how-to-local-server.html
- https://help.rengabim.com/api/how-to-rot.html
- https://help.rengabim.com/api/how-to-dt-language.html
- https://help.rengabim.com/api/interface_i_project.html
- https://help.rengabim.com/api/interface_i_operation.html

## External controller

Renga exposes a COM API. An external helper should be x64, initialize COM as STA, keep COM access on that thread, and attach to the intended running instance through the Running Object Table. Human-readable monikers have the form `!Renga Application, ver: <version>, pid: <pid>`; a CLSID moniker can also exist.

`Marshal.GetActiveObject("Renga.Application.1")` returns the first registered instance and is unsuitable when more than one Renga process is open. Creating `new Renga.Application()` uses Renga as a local COM server and may launch a new invisible process; set `Visible` explicitly and own its close/quit lifecycle only when the task asks for a new instance.

Use the official type library or a locally generated interop assembly when available, but never publish Renga SDK files or generated Interop binaries in a public skill or repository. Typed interop handles GUIDs, structs, SAFEARRAYs, and casts more reliably than late-bound dispatch. For dynamic access, prefer API members whose names end in `S` for string GUIDs and `GetInterfaceByName()` for additional interfaces.

## Models and objects

- `IApplication.Project` gives the active project only when a project is open.
- `IProject.Model` is the building model. Assemblies and drawings have their own models and undo stacks.
- `IModel.GetObjects()` returns model objects; inspect `ObjectType`, `Id`, and `UniqueId`.
- Query optional behavior with `IModelObject.GetInterfaceByName`, such as `IBaseline2DObject`, `ILevelObject`, or `IRoofSlopes`. A type GUID alone does not guarantee that an assumed interface or parameter is available in every version.

## Mutations and undo

The official pattern is:

```csharp
var op = project.StartOperationWithUndo(model.Id);
try
{
    // edit model objects
    op.Apply();
}
catch
{
    op.Rollback();
    throw;
}
```

`StartOperation*` creates and starts an operation. `CreateOperation*` requires an explicit `Start()`. Check `HasActiveOperation()` first because operations cannot be nested. Undo stacks belong to individual models. Changes to project-level entities such as styles and materials are not covered by model undo; use a regular operation and disclose the recovery limitation.

Late-bound clients may fail to marshal `IModel.Id` as a GUID in some installed API builds. Do not invent a model ID. Fall back to a regular operation only when an undoable operation is genuinely unavailable, and return that fact to the user.

`DeleteObjectById` may also remove dependent objects. Resolve and report the exact target set before deletion.

## State and error handling

- Check every integer result code; zero commonly means success, but verify the called method's documentation.
- When object creation returns null or a cast fails, capture `IApplication.LastError` before issuing unrelated calls.
- Re-query the project and model after apply rather than trusting cached wrappers.
- Release COM wrappers deterministically in long-running helpers, without releasing objects still owned by another live wrapper.
- Detect API capabilities by version and interface availability. Consult the official changelog for version-sensitive code rather than assuming the newest interfaces exist.
- Never interpret `HasUnsavedChanges` as authorization to save.

Saving, Save As, closing, quitting, IFC/DWG/PDF export, collaboration publish, and synchronization are separate external effects. Perform them only when explicitly requested, use an explicit destination or target, and return the API result.
