# Renga architecture creation and placement

Use the installed API version as the authority. Relevant official pages:

- https://help.rengabim.com/api/how-to-create-and-delete-object.html
- https://help.rengabim.com/api/interface_i_model.html
- https://help.rengabim.com/api/interface_i_new_entity_args.html
- https://help.rengabim.com/api/interface_i_level_object.html
- https://help.rengabim.com/api/interface_i_baseline2_d_object.html
- https://help.rengabim.com/api/interface_i_roof_slopes.html

## Creation contract

Create arguments with `IModel.CreateNewEntityArgs()` and set only fields that apply:

- `TypeId` is required.
- `HostObjectId` is a level for level-based objects and a permitted host for dependent objects. Doors and windows require a wall; floor and roof openings require the corresponding slab or roof. Consult the `INewEntityArgs` host table for other types.
- `StyleId` is optional only where the entity type permits it. Equipment, mechanical equipment, plumbing fixtures, distribution boards, lighting fixtures, wiring accessories, and MEP accessories/fittings require a style before creation.
- `Placement3D` is the global creation placement for level-based 3D objects. `Placement2D` is for supported drawing objects.
- `FilePath` must be absolute for linked images, reference drawings, 3D models, or IFC models.

`CreateObject()` can return null. Capture `IApplication.LastError` immediately. A successful return does not prove that the requested placement was preserved.

## Placement and coordinates

Create right-handed, normalized, orthogonal placements. The placement structure supplies origin, X axis, and Z axis; do not pass zero-length, skewed, or unnormalized axes.

`ILevelObject` exposes the parent `LevelId`, actual placement, `PlacementElevation`, `VerticalOffset`, and `ElevationAboveLevel`. Read them after creation. The resulting elevation is placement elevation plus vertical offset.

Renga may project dependent objects onto their hosts. For windows and doors, placement X-axis controls the outside direction relative to the wall. For holes and hosted electrical objects, the closest host face to the input origin is selected. For route objects, the origin is projected onto the route.

`ILevelObject.SetPlacement()` cannot edit every type and cannot currently move an object with dependants. Doors, windows, openings, rooms, route points, object-based railings, and rebars are among the documented restrictions. Do not treat a rejected move as a transient error; choose a supported reconstruction strategy only with authorization.

## Baselines and contours

Use `IBaseline2DObject` for supported planar baseline or contour geometry and `IBaseline3DObject` for supported 3D baselines. Curves are expressed in an object's coordinate system unless an `InCS` method explicitly accepts another coordinate system.

Build closed contours from continuous ordered segments. Match every endpoint exactly, avoid self-intersections and duplicate zero-length edges, and document edge order when later APIs address edges by index.

Editing a baseline can be unavailable when the object has dependants. Inspect host relationships before deciding between in-place editing and controlled reconstruction.

## Creation order

Create and apply stable hosts before dependants when the installed version rejects same-operation host IDs. Re-query local IDs after applying:

1. levels and primary hosts;
2. walls, floors, roofs, structural hosts;
3. doors, windows, openings, holes, hosted equipment;
4. rooms and contents;
5. annotations or downstream data.

Use the fewest operations consistent with valid host resolution and useful undo boundaries.

## Roofs

For `IRoofSlopes`, slope indices correspond to contour edges. Establish contour order before setting edge shape, level, overhang, or angle. Angles use degrees; levels and overhangs use millimetres. Gable edges do not have a meaningful slope angle or slope level.

Validate the ridge direction, eaves, gable ends, overhangs, wall contact, and penetrations in exported geometry and in 3D. A successful setter call proves neither that the intended edge was addressed nor that the roof closes correctly.
