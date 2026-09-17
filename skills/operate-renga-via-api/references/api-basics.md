# Renga API basics

This is a concise working guide distilled from the official Renga API documentation and SDK samples. Verify version-sensitive behavior against the documentation shipped with the installed SDK.

Official starting points:

- https://help.rengabim.com/api/overview-api-sdk-plugin.html
- https://help.rengabim.com/api/overview-general-principles.html
- https://help.rengabim.com/api/how-to-create-and-delete-object.html
- https://help.rengabim.com/api/interface_i_new_entity_args.html
- https://help.rengabim.com/api/interface_i_project.html

## External controller

Renga exposes a COM API. An external helper should be x64, run in an STA thread, and attach to a running Renga instance through the Running Object Table. Renga monikers have the form `!Renga Application, ver: <version>, pid: <pid>`.

Use the official type library or generated `Interop.Renga.dll` when possible. Typed interop handles GUIDs, structs, and SAFEARRAYs more reliably than late-bound `dynamic` dispatch. Keep all COM access on the same STA thread.

## Models and objects

- `IApplication.Project` gives the active project.
- `IProject.Model` is the building model. Assemblies and drawings have their own models and undo stacks.
- `IModel.GetObjects()` returns model objects; inspect `ObjectType`, `Id`, and `UniqueId`.
- Query optional behavior with `IModelObject.GetInterfaceByName`, such as `IBaseline2DObject`, `ILevelObject`, or `IRoofSlopes`. A type GUID alone does not guarantee that an assumed interface or parameter is available in every version.

## Mutations and undo

The official pattern is:

```csharp
var op = project.CreateOperationWithUndo(model.Id);
op.Start();
try
{
    // create, edit, or delete model objects
    op.Apply();
}
catch
{
    op.Rollback();
    throw;
}
```

Use `CreateOperation()` only when an undoable operation is unavailable and disclose that limitation. Undo stacks belong to individual models. Changes to project-level entities such as styles and materials are not covered by model undo.

`DeleteObjectById` may also remove dependent objects. Resolve and report the exact target set before deletion.

## Creation contract

Create arguments with `model.CreateNewEntityArgs()` and set only fields that apply:

- `TypeId`: required entity type GUID.
- `HostObjectId`: level for level-based objects; wall or another allowed host for dependent objects such as doors and windows.
- `StyleId`: optional for many types, mandatory for some equipment types. Choose from the matching project style collection.
- `Placement3D`: origin and orthonormal axes for level-based 3D objects.
- `Placement2D`: drawing objects.
- `FilePath`: linked files and images; use an absolute path.

`model.CreateObject(args)` can return null. Check it immediately and include `application.LastError` in diagnostics.

After creation, obtain the object's specialized interface and set geometry before applying the operation. Some Renga versions do not accept a newly created object as another object's host until the host operation has been applied; if observed, commit the hosts first and create dependants in a second undoable operation.

## Validation

After apply, re-query the model instead of trusting cached COM objects. Check object count/type, host IDs, placement, specialized geometry, and any modified parameters. Then inspect the visible plan/3D result. Do not save automatically.

Requested placement is not necessarily final placement. Renga may project or normalize a dependent object's `Placement3D`; read `ILevelObject.GetPlacement()`, `PlacementElevation`, `VerticalOffset`, and `ElevationAboveLevel` after creation. Validate the resulting geometry, not the input origin.

For exact extents, use `IProject.DataExporter.GetObjects3D()`. Match `IExportedObject3D.ModelObjectId` to the model object, traverse meshes and grids, and calculate world-coordinate bounds from grid vertices. This is the preferred broad-phase check for objects whose style geometry is not known in advance.
