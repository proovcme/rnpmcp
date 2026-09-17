# Renga geometry export and spatial evidence

Official sources:

- https://help.rengabim.com/api/how-to-export-geometry.html
- https://help.rengabim.com/api/interface_i_data_exporter.html
- https://help.rengabim.com/api/interface_i_exported_object3_d.html
- https://help.rengabim.com/api/interface_i_grid.html

## Choose the export form

`IProject.DataExporter` provides two principal views:

- `GetObjects3D()` returns geometry grouped by exported object. Use it for object bounds, identity-aware review, clash broad phase, and per-object rendering.
- `GetGrids()` returns grids aggregated with material information. Use it when object ownership is unnecessary and material/rendering grouping is primary.

Do not try to reconstruct object identity from material-grouped grids.

## Object-grouped traversal

For each `IExportedObject3D`, retain its model object ID and type, then traverse every mesh and every grid. Read all grid vertices and apply any coordinate transform required by the interface/version before computing world-space results. Include every mesh: style geometry can be split into multiple grids or materials.

Match the exported model-object ID back to the correct `IModel`; local IDs are not globally unique across building, assembly, and drawing models.

## Bounds and collisions

For an axis-aligned world bounding box, initialize extrema from the first valid vertex, then update min/max X, Y, and Z over all grids. Treat an object with no valid vertices as missing geometry, not a zero-size object.

When testing two boxes, distinguish:

- positive-volume overlap;
- face or edge contact within tolerance;
- separation;
- intended host penetration.

Select a documented tolerance appropriate to model units and numerical precision. AABB is broad phase only: rotated, sparse, nested, or concave geometry may require grid/triangle checks or visual inspection.

## Evidence and repeatability

Record API version, model ID, object `UniqueId`, export method, coordinate system, units, tolerance, and any excluded categories. If the model changes after export, discard cached geometry and export again.

Geometry export gives rendered/triangulated evidence. It does not replace semantic checks such as host relationships, room boundaries, door operation, required clearances, or regulatory review.
