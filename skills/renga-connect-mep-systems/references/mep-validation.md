# MEP topology and coordination validation

Run these checks after Renga generates a route and again after any architectural or equipment move.

## Topology

- Confirm every intended endpoint is connected through the expected port.
- Traverse or inspect all generated route segments and fittings; look for disconnected islands, duplicated parallel connections, zero-length segments, abrupt termination, and unintended system-category changes.
- Confirm branch and main roles match the intended system, not merely the generated shape.
- Verify that route styles, fitting styles, insulation, and sizes are consistent through transitions.

## Geometry and clashes

- Use actual exported geometry where available. Bounding boxes are only a broad-phase filter and can produce false positives and false negatives for detailed fittings.
- Check routes, fittings, insulation, and required access zones against walls, slabs, roofs, ceilings, shafts, doors, windows, furniture, equipment, and other systems.
- A route passing through an enclosing structure needs an intentional penetration or opening and downstream fire, acoustic, waterproofing, and structural coordination as applicable.
- Check the whole continuous path, including bends and local bottlenecks, rather than a few favorable sections.

## Discipline checks

- Gravity drainage: verify actual slope, invert continuity, connection elevation, cleanouts, and feasible discharge. Do not assume hidden depth below a modeled floor.
- Water and hydronic pipework: verify design flow, diameter, pressure, insulation, valves, drainage, and access using the applicable discipline design workflow.
- Ventilation: verify calculated airflow, velocity, pressure loss, fitting space, fire/smoke requirements, insulation, and service access.
- Electrical: verify circuit intent, containment capacity, separation, bend constraints, fire stopping, and access. A geometric route is not a calculated circuit.

## Building usability

- Preserve clear door swings, window apertures, escape routes, access panels, equipment service zones, furniture use zones, and ceiling maintenance access.
- Check installation and replacement paths for large equipment and long route components.
- Confirm ceiling and floor build-ups provide real construction depth. Flag any route that relies on an unmodeled void.
- Coordinate risers and shafts as maintainable spaces; do not hide disconnected or inaccessible equipment in walls.

## Completion evidence

Report:

- endpoints, selected port indexes, and system category;
- generated routes and fittings found after apply;
- styles, elevations, offsets, insulation, and any assumed inputs;
- collision and continuity checks performed;
- penetrations and discipline calculations still required;
- screenshots or view checks when visual access exists.

Do not call the network coordinated solely because Renga created it or because no application warning appeared.
