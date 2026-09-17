# Architectural validation after Renga API changes

## Envelope and levels

- Confirm level elevations and each object's actual `LevelId` and elevation.
- Verify wall loops, corner joins, thicknesses, heights, and gaps in plan and 3D.
- A roof is not a ceiling. Model and verify a separate ceiling when the brief requires one.
- Set internal partition height to the ceiling underside or intended construction interface.
- Equal wall-top and roof-level numbers prove contact only at the eaves. Check the complete roof underside and gable closure.
- The standard wall height is uniform and `IWallContour` is inspection-only. A stepped gable approximation is schematic; keep its error inside the roof build-up and label it honestly.

## Openings

- Verify the actual host, sill or threshold, head, width, height, outside direction, and visible cut.
- Check the complete door-leaf swing, handle approach, residual clear passage, nearby doors, furniture, fixtures, and equipment doors.
- For every window, keep both the aperture projection and an interior access/cleaning zone clear. Host-wall intersection is intentional; overlap with another wall, shaft, ceiling, roof edge, fixture, or furniture is not.
- Ensure openings do not extend below their level, above the wall, into the ceiling, or through the roof.

## Furniture, fixtures, and services

Export actual 3D geometry; never infer occupied size from a style name or insertion point.

- Floor-mounted objects need deliberate floor contact.
- Wall-mounted fixtures need a mounting elevation and back-face overlap with a real wall segment, not merely the same X or Y coordinate.
- Long objects such as wardrobes and baths must be checked against both ends and adjacent partitions.
- Sanitary fixtures should form a plausible wet core. Place a WC against an internal service wall or visible riser/shaft where practicable, not blindly against a window or exterior wall.
- Show enough service-zone geometry to make drainage and maintenance assumptions visible.

## Collision and usability checks

Use exported world-coordinate bounds as broad phase:

- flag positive-volume overlap between objects and walls unless it is intentional hosting;
- allow deliberate face contact but verify the contacting intervals;
- check object-to-object separation and continuous routes;
- test chairs, appliance doors, wardrobes, and other movable parts in an operating state;
- inspect rotated or concave objects beyond AABB when a reported overlap is ambiguous.

No static collision does not prove usability. Measure continuous circulation, local restrictions, approaches, working clearances, door operation, and maintenance access.

## Completion checklist

- Expected object counts, types, styles, and hosts.
- Actual post-apply placement and elevations.
- Closed contours and clean wall joins.
- Walls and partitions meet their intended upper construction.
- Separate floor, ceiling, and roof geometry where required.
- Door swings and window keep-out zones are usable.
- Furniture and fixtures do not penetrate walls or each other unintentionally.
- Primary routes remain continuous in operating conditions.
- Plan and 3D visual inspection agree with exported geometry.
- Undo items and schematic approximations are reported.
