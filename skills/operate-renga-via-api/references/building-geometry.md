# Building geometry patterns

Use this reference for schematic architectural creation. It supplements, not replaces, the official API pages:

- https://help.rengabim.com/api/interface_i_baseline2_d_object.html
- https://help.rengabim.com/api/interface_i_roof_slopes.html
- https://help.rengabim.com/api/interface_i_new_entity_args.html

## Walls

Create a wall with the target level as `HostObjectId`. Retrieve `IBaseline2DObject` and set a line segment made with `IApplication.Math.CreateLineSegment2D`. Order the rectangle edges consistently so wall-local directions and hosted placements are predictable.

After apply, inspect corner joins and resulting height. Do not assume the default wall style, thickness, or height matches the design intent.

The standard wall API exposes a uniform height; `IWallContour` is inspection-only. Do not claim that a rectangular wall closes a pitched gable. For a schematic model, short contiguous wall segments with heights sampled from the roof underside can approximate a gable, but keep the step error inside the roof thickness and verify the entire edge in 3D. For production geometry, use an exact native-Renga/manual solution or a purpose-built element rather than presenting a stepped approximation as exact.

## Doors and windows

Doors and windows require a valid wall host. Choose a compatible style before creation and inspect its actual dimensions. Place openings relative to the wall baseline and confirm the result in elevation or 3D; global coordinates alone are not enough evidence that the opening sits correctly in the host.

For hosted doors and windows, the creation placement X-axis controls which side is treated as outside relative to the host baseline. Reversing this axis can reverse the side/handing. Renga may project the supplied origin onto the host and preserve a style-defined vertical offset, so verify the resulting `ILevelObject` values. For a window, check the actual sill and head elevations after setting width and height; do not add the requested Z offset twice.

For each door, verify sill/floor level, clear opening, swing/handing where represented, and collisions. For windows, verify sill/head elevations, spacing from corners, alignment, and separation from the roof or floor.

If a host was created in the same operation and Renga reports an invalid `HostObjectId`, apply the host operation first, re-query its ID, then create hosted objects in a second undoable operation.

## Floors, ceilings, and roofs

Closed planar contours use `IBaseline2DObject` with a composite curve. Keep segment order continuous and close the last endpoint exactly.

A roof is not a ceiling. When rooms require a flat ceiling, create and verify a separate slab/ceiling plane at the intended elevation. Inspect its exported `minZ` and `maxZ`, and set internal partition height to the ceiling underside. Check that the ceiling neither cuts door/window heads nor leaves a gap above partitions.

For a roof, obtain `IRoofSlopes`. Edge indices correspond to baseline segments, so establish and document the segment order before setting:

- edge shape (`SingleSlope` or `Gable`),
- slope level in millimetres,
- overhang in millimetres,
- slope angle in degrees for sloping edges.

Validate ridge direction, equal eaves, gable ends, wall coverage, and unintended penetrations. A successful `SetSlopeAngle` call does not prove that the correct baseline edge was selected.

Set the eave-level wall height and roof slope level deliberately. Then verify wall-to-roof contact in exported geometry and in 3D. Equal numeric levels prove contact only at the eaves; they do not prove that gable walls close the triangular space or that internal partitions meet the roof without protruding through it.

## Furniture, fixtures, and collision checks

Never infer the occupied footprint from the insertion point or style name. After creation, export the 3D geometry and compute a world-coordinate axis-aligned bounding box for every wall, door, window, furniture element, and fixture.

Use those bounds as a broad-phase check:

- flag positive-volume overlap between movable objects and wall solids unless the penetration is intentional hosting;
- allow face contact when the back of furniture or a fixture is deliberately placed against a wall; for wall-mounted fixtures, also prove that the fixture's back-face interval overlaps the actual wall segment in plan rather than merely sharing the same X or Y coordinate;
- check floor-mounted objects for `minZ` near the floor and wall-mounted basins/sinks for a deliberate mounting elevation;
- verify wardrobes, baths, and other long objects against both adjacent walls because their insertion point may be clear while their geometry crosses a partition;
- check furniture-to-furniture clearance and the continuous circulation path, not only pairwise non-overlap;
- treat tucked dining chairs as a deliberate configuration, then also test or reason about the pulled-out/occupied state;
- check door swing areas separately: the leaf arc is not reliably represented by the closed-leaf geometry bounds.

An AABB overlap is only broad-phase evidence: rotated or concave objects may need mesh-level or visual inspection. Conversely, no static overlap does not prove usability; preserve approach, operating, and maintenance clearances.

Place sanitary fixtures as a coordinated wet core. A WC should back onto an internal service wall or modeled riser/shaft when practicable, not blindly onto an exterior wall or window. Model enough of the shaft to make the service assumption visible, and verify that it does not obstruct the fixture approach or a door swing.

## Minimum post-check

- Expected object counts and types.
- Every hosted object's host ID and actual visible placement.
- Continuous wall loop and clean corners.
- Openings do not extend below the level or into the roof.
- Roof is closed, symmetrical where intended, and has the requested number of slopes.
- Furniture and fixture world bounds do not penetrate walls or each other unintentionally.
- Door swing zones and primary routes remain usable with furniture in operating positions.
- One or more undo items exist for the change.
